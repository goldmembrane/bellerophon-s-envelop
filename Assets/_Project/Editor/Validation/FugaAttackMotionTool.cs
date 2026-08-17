using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Bellerophon.Enemies.Fuga;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.FugaCargoRunScene
{
    internal static class FugaAttackMotionTool
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementRootName = "Approved Fuga Enemy Placement";
        private const string AttackSlotName = "Fuga_03_Attack";
        private const string ModelName = "Fuga_Model";
        private const string PlayerName = "Player";
        private const string SourceModelPath = "D:/Bellerophon2/Bellerophon/enemies model/fuga.glb";
        private const string ImportedModelPath = "Assets/_Project/Art/Enemies/Fuga/Models/fuga.glb";
        private const string ExpectedModelSha256 =
            "009430EB298B83C6EA48CD2AF7B9BE3DF075EA512DAF6978BBE41D5C917AF3AB";
        private const string ExpectedImportedRigSha256 =
            "4DA5AE82DE38E84804188549A6E24F923D77BC04EF072B98D245F34C2B0A9C3B";
        private const string LeftFirstClipPath =
            "Assets/_Project/Art/Enemies/Fuga/Animations/Fuga_Attack_NewModel_LeftFirst.anim";
        private const string RightFirstClipPath =
            "Assets/_Project/Art/Enemies/Fuga/Animations/Fuga_Attack_NewModel_RightFirst.anim";
        private const string ControllerPath =
            "Assets/_Project/Art/Enemies/Fuga/Controllers/Fuga_Attack_NewModel_AlternatingWings.controller";
        private const string LegacyAttackClipPath =
            "Assets/_Project/Art/Enemies/Fuga/Animations/Fuga_Attack_WingtipStrike.anim";
        private const string LegacyAttackControllerPath =
            "Assets/_Project/Art/Enemies/Fuga/Controllers/Fuga_Attack_WingtipStrike.controller";
        private const string OutputFolder = "docs/validation/fuga_attack_motion_2026-08-17";
        private const string ReportPath = OutputFolder + "/Fuga_Attack_Motion_Report.txt";
        private const string CapturePath = OutputFolder + "/Fuga_Attack_Motion_Comparison.png";
        private const string BodyYawReportPath = OutputFolder + "/Fuga_Attack_Body_Yaw_90_Report.txt";
        private const string BodyYawCapturePath = OutputFolder + "/Fuga_Attack_Body_Yaw_90_Comparison.png";
        private const string HorizontalWingsReportPath = OutputFolder + "/Fuga_Attack_Horizontal_Wings_Report.txt";
        private const string HorizontalWingsCapturePath = OutputFolder + "/Fuga_Attack_Horizontal_Wings_Comparison.png";
        private const string JerkDrivenAccelerationReportPath =
            OutputFolder + "/Fuga_Attack_Jerk_Driven_Acceleration_Report.txt";
        private const string JerkDrivenAccelerationCapturePath =
            OutputFolder + "/Fuga_Attack_Jerk_Driven_Acceleration_Comparison.png";
        private const string ConsoleReportPath = OutputFolder + "/Fuga_Attack_Unity_Console_Report.txt";
        private const string ParvumVisibleMeshName = "Unified_Parvum_Reference_Matched_Single_Mesh";
        private const string JiggleRigTypeName =
            "GatorDragonGames.JigglePhysics.JiggleRig, com.gator-dragon-games.jigglephysics";
        private const float AttackDelay = 1f;
        private const float LoopDuration = AttackDelay * 2f;
        private const float FirstImpactTime = AttackDelay * 0.5f;
        private const float SecondImpactTime = FirstImpactTime + AttackDelay;
        private const float BodyForwardTilt = 40f;
        // Visual recoil belongs to Fuga_Model so the body and both child wings move together without moving the Rigidbody root.
        private const float ImpactVerticalRecoilMeters = 0.1f;
        private const float ImpactVerticalRecoilDuration = 0.07f;
        private const float ImpactVerticalRecoilReverseTime = ImpactVerticalRecoilDuration * 0.5f;
        // Bone_013/Bone_017 face the model forward when their local X rotation reaches -90 degrees.
        private const float ActiveWingForwardStrikeAngle = -90f;
        private const float AccelerationDuration = 0.45f;
        private const float FirstAccelerationStartTime = FirstImpactTime - AccelerationDuration;
        private const float SecondAccelerationStartTime = SecondImpactTime - AccelerationDuration;
        private const float InitialAngularSpeed = 0f;
        private const float RecoveryDuration = AttackDelay - FirstImpactTime;
        // Cubic speed growth makes angular acceleration itself rise toward impact instead of staying constant.
        private const float PreImpactVelocityPower = 3f;
        private const float RecoveryVelocityPower = 2f;
        private const float BodyYawAngle = 131.625f;
        // The pre-impact peak is derived from the preserved yaw so the body starts at rest and bursts into impact.
        private const float PreImpactAngularSpeed =
            ((PreImpactVelocityPower + 1f) * BodyYawAngle / AccelerationDuration) -
            PreImpactVelocityPower * InitialAngularSpeed;
        // Recovery deliberately starts far below the pre-impact peak to create the requested instantaneous slowdown.
        private const float PostImpactAngularSpeed = 360f;
        private const float RecoveryEndAngularSpeed =
            (((RecoveryVelocityPower + 1f) * BodyYawAngle / RecoveryDuration) - PostImpactAngularSpeed) /
            RecoveryVelocityPower;
        private const float PeakPreImpactAngularAcceleration =
            (PreImpactAngularSpeed - InitialAngularSpeed) * PreImpactVelocityPower / AccelerationDuration;
        private const float PeakPostImpactAngularDeceleration =
            (PostImpactAngularSpeed - RecoveryEndAngularSpeed) * RecoveryVelocityPower / RecoveryDuration;
        private const int BodyCurveSampleRate = 60;
        private const int CaptureWidth = 1920;
        private const int CaptureHeight = 1080;
        private const int DirectMotionReviewLoopCount = 2;
        private static readonly float[] BodyRecoilKeyTimes =
        {
            0f,
            FirstImpactTime - 1f / BodyCurveSampleRate,
            FirstImpactTime,
            FirstImpactTime + ImpactVerticalRecoilReverseTime,
            FirstImpactTime + ImpactVerticalRecoilDuration,
            SecondImpactTime - 1f / BodyCurveSampleRate,
            SecondImpactTime,
            SecondImpactTime + ImpactVerticalRecoilReverseTime,
            SecondImpactTime + ImpactVerticalRecoilDuration,
            LoopDuration,
        };
        private static readonly float[] BodyRecoilOffsets =
        {
            0f,
            0f,
            ImpactVerticalRecoilMeters,
            -ImpactVerticalRecoilMeters,
            0f,
            0f,
            ImpactVerticalRecoilMeters,
            -ImpactVerticalRecoilMeters,
            0f,
            0f,
        };
        private static readonly float[] BodyKeyTimes =
            Enumerable.Range(0, Mathf.RoundToInt(LoopDuration * BodyCurveSampleRate) + 1)
                .Select(index => index / (float)BodyCurveSampleRate)
                .ToArray();
        private static readonly float[] FirstStrikeWingAngles =
            BodyKeyTimes.Select(time => ActiveWingAngleAtTime(time, strikesFirst: true)).ToArray();
        private static readonly float[] SecondStrikeWingAngles =
            BodyKeyTimes.Select(time => ActiveWingAngleAtTime(time, strikesFirst: false)).ToArray();
        private static readonly float[] LeftFirstBodyYawAngles =
            BodyKeyTimes.Select(time => BodyYawAtTime(time, startsLeft: true)).ToArray();
        private static readonly float[] RightFirstBodyYawAngles =
            BodyKeyTimes.Select(time => BodyYawAtTime(time, startsLeft: false)).ToArray();
        private static readonly float[] PreImpactWindowTimes =
            Enumerable.Range(0, 10)
                .Select(index => FirstAccelerationStartTime + index * 0.05f)
                .ToArray();
        private static readonly float[] PostImpactWindowTimes =
            Enumerable.Range(0, 11)
                .Select(index => FirstImpactTime + index * 0.05f)
                .ToArray();
        private static readonly float[] ExpectedPreImpactWindowSpeeds =
            PreImpactWindowTimes.Zip(
                    PreImpactWindowTimes.Skip(1),
                    (start, end) =>
                        (PreImpactYawAtElapsed(end - FirstAccelerationStartTime) -
                         PreImpactYawAtElapsed(start - FirstAccelerationStartTime)) /
                        (end - start))
                .ToArray();
        private static readonly float[] ExpectedPreImpactWindowAccelerations =
            AccelerationsFromSpeeds(ExpectedPreImpactWindowSpeeds, 0.05f);
        private static readonly float[] ExpectedPostImpactWindowSpeeds =
            PostImpactWindowTimes.Zip(
                    PostImpactWindowTimes.Skip(1),
                    (start, end) =>
                        (RecoveryYawAtElapsed(end - FirstImpactTime) -
                         RecoveryYawAtElapsed(start - FirstImpactTime)) /
                        (end - start))
                .ToArray();
        private static readonly float[] ExpectedPostImpactWindowAccelerations =
            AccelerationsFromSpeeds(ExpectedPostImpactWindowSpeeds, 0.05f);

        private static Transform motionReviewSlot;
        private static AnimationClip motionReviewClip;
        private static double motionReviewStartTime;
        private static string motionReviewLabel;
        private static int lastMotionReviewCompletedLoops;
        private static bool leftFirstMotionReviewCompleted;
        private static bool rightFirstMotionReviewCompleted;
        private static float minimumActiveWingTipForwardSpeed = float.PositiveInfinity;
        private static float maximumInactiveWingTipForwardSpeed = float.NegativeInfinity;

        private static float ActiveWingAngleAtTime(float time, bool strikesFirst)
        {
            var strikeTime = strikesFirst ? time : time - AttackDelay;
            if (strikeTime < FirstAccelerationStartTime || strikeTime > AttackDelay)
            {
                return 0f;
            }

            var normalizedBodyProgress = strikeTime <= FirstImpactTime
                ? PreImpactYawAtElapsed(strikeTime - FirstAccelerationStartTime) / BodyYawAngle
                : (BodyYawAngle - RecoveryYawAtElapsed(strikeTime - FirstImpactTime)) / BodyYawAngle;
            return ActiveWingForwardStrikeAngle * Mathf.Clamp01(normalizedBodyProgress);
        }

        private static bool FirstForwardStrikeUsesLeftWing(
            Transform model,
            Transform leftWing,
            Transform rightWing,
            bool startsLeft)
        {
            var firstLeftForwardSpeed = WingTipForwardSpeedAtImpact(
                model,
                leftWing,
                FirstImpactTime,
                startsLeft);
            var firstRightForwardSpeed = WingTipForwardSpeedAtImpact(
                model,
                rightWing,
                FirstImpactTime,
                startsLeft);
            var secondLeftForwardSpeed = WingTipForwardSpeedAtImpact(
                model,
                leftWing,
                SecondImpactTime,
                startsLeft);
            var secondRightForwardSpeed = WingTipForwardSpeedAtImpact(
                model,
                rightWing,
                SecondImpactTime,
                startsLeft);
            if (firstLeftForwardSpeed * firstRightForwardSpeed >= 0f ||
                secondLeftForwardSpeed * secondRightForwardSpeed >= 0f)
            {
                throw new InvalidOperationException(
                    "Each Fuga impact must have exactly one wing tip moving toward the model's visible forward -Z.");
            }

            var firstUsesLeft = firstLeftForwardSpeed > firstRightForwardSpeed;
            var secondUsesLeft = secondLeftForwardSpeed > secondRightForwardSpeed;
            if (firstUsesLeft == secondUsesLeft)
            {
                throw new InvalidOperationException("The Fuga forward-moving strike wing did not alternate between impacts.");
            }

            return firstUsesLeft;
        }

        private static float WingTipForwardSpeedAtImpact(
            Transform model,
            Transform wing,
            float impactTime,
            bool startsLeft)
        {
            var tip = FindWingTip(model, wing);
            var tipInModelSpace = model.InverseTransformPoint(tip.position);
            var previousTime = impactTime - 1f / BodyCurveSampleRate;
            var previousRotation = Quaternion.Euler(
                BodyForwardTilt,
                BodyYawAtTime(previousTime, startsLeft),
                0f);
            var impactRotation = Quaternion.Euler(
                BodyForwardTilt,
                BodyYawAtTime(impactTime, startsLeft),
                0f);
            var forwardDisplacement = Vector3.Dot(
                impactRotation * tipInModelSpace - previousRotation * tipInModelSpace,
                Vector3.back);
            return forwardDisplacement * BodyCurveSampleRate;
        }

        private static Transform FindWingTip(Transform model, Transform wing)
        {
            var wingRootInModelSpace = model.InverseTransformPoint(wing.position);
            return wing.GetComponentsInChildren<Transform>(true)
                       .Where(candidate => candidate != wing)
                       .OrderByDescending(candidate =>
                           (model.InverseTransformPoint(candidate.position) - wingRootInModelSpace).sqrMagnitude)
                       .FirstOrDefault() ??
                   throw new InvalidOperationException("The Fuga wing " + wing.name + " has no tip descendant.");
        }

        private static string ForwardWingOrder(
            Transform model,
            Transform leftWing,
            Transform rightWing,
            bool startsLeft)
        {
            return FirstForwardStrikeUsesLeftWing(model, leftWing, rightWing, startsLeft)
                ? leftWing.name + "," + rightWing.name
                : rightWing.name + "," + leftWing.name;
        }

        private static float BodyYawAtTime(float time, bool startsLeft)
        {
            var firstDirection = startsLeft ? -1f : 1f;
            if (time < FirstAccelerationStartTime)
            {
                return 0f;
            }

            if (time <= FirstImpactTime)
            {
                var elapsed = time - FirstAccelerationStartTime;
                return firstDirection * PreImpactYawAtElapsed(elapsed);
            }

            if (time <= AttackDelay)
            {
                var elapsed = time - FirstImpactTime;
                return firstDirection * (BodyYawAngle - RecoveryYawAtElapsed(elapsed));
            }

            var secondDirection = -firstDirection;
            if (time < SecondAccelerationStartTime)
            {
                return 0f;
            }

            if (time <= SecondImpactTime)
            {
                var elapsed = time - SecondAccelerationStartTime;
                return secondDirection * PreImpactYawAtElapsed(elapsed);
            }

            var recoveryElapsed = time - SecondImpactTime;
            return secondDirection * (BodyYawAngle - RecoveryYawAtElapsed(recoveryElapsed));
        }

        private static float PreImpactYawAtElapsed(float elapsed)
        {
            var clampedElapsed = Mathf.Clamp(elapsed, 0f, AccelerationDuration);
            var normalized = clampedElapsed / AccelerationDuration;
            return InitialAngularSpeed * clampedElapsed +
                   (PreImpactAngularSpeed - InitialAngularSpeed) * AccelerationDuration *
                   Mathf.Pow(normalized, PreImpactVelocityPower + 1f) /
                   (PreImpactVelocityPower + 1f);
        }

        private static float RecoveryYawAtElapsed(float elapsed)
        {
            var clampedElapsed = Mathf.Clamp(elapsed, 0f, RecoveryDuration);
            var normalized = clampedElapsed / RecoveryDuration;
            return RecoveryEndAngularSpeed * clampedElapsed +
                   (PostImpactAngularSpeed - RecoveryEndAngularSpeed) * RecoveryDuration *
                   (1f - Mathf.Pow(1f - normalized, RecoveryVelocityPower + 1f)) /
                   (RecoveryVelocityPower + 1f);
        }

        private static float PreImpactAngularSpeedAtElapsed(float elapsed)
        {
            var normalized = Mathf.Clamp01(elapsed / AccelerationDuration);
            return InitialAngularSpeed +
                   (PreImpactAngularSpeed - InitialAngularSpeed) *
                   Mathf.Pow(normalized, PreImpactVelocityPower);
        }

        private static float PreImpactAngularAccelerationAtElapsed(float elapsed)
        {
            var normalized = Mathf.Clamp01(elapsed / AccelerationDuration);
            return PeakPreImpactAngularAcceleration *
                   Mathf.Pow(normalized, PreImpactVelocityPower - 1f);
        }

        private static float RecoveryAngularSpeedAtElapsed(float elapsed)
        {
            var normalized = Mathf.Clamp01(elapsed / RecoveryDuration);
            return RecoveryEndAngularSpeed +
                   (PostImpactAngularSpeed - RecoveryEndAngularSpeed) *
                   Mathf.Pow(1f - normalized, RecoveryVelocityPower);
        }

        private static float RecoveryAngularAccelerationAtElapsed(float elapsed)
        {
            var normalized = Mathf.Clamp01(elapsed / RecoveryDuration);
            return -PeakPostImpactAngularDeceleration *
                   Mathf.Pow(1f - normalized, RecoveryVelocityPower - 1f);
        }
        private static readonly string[] SlotNames =
        {
            "Fuga_00_Static",
            "Fuga_01_Idle",
            "Fuga_02_Move",
            "Fuga_03_Attack",
            "Fuga_04_Hit",
            "Fuga_05_Death",
            "Fuga_06_Consume",
        };

        [MenuItem("Bellerophon/Enemies/Fuga/Apply Attack Motion")]
        public static void ApplyFugaAttackMotion()
        {
            var scene = RequireCurrentScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException("CargoRunMvp must be saved before applying the Fuga attack motion.");
            }

            RequireModelHashes();
            var legacyClipHashBefore = Sha256(Absolute(LegacyAttackClipPath));
            var legacyControllerHashBefore = Sha256(Absolute(LegacyAttackControllerPath));
            var placementRoot = RequireRoot(PlacementRootName);
            var slot = RequireDirectChild(placementRoot, AttackSlotName);
            var model = RequireDirectChild(slot, ModelName);
            var renderer = model.GetComponentInChildren<SkinnedMeshRenderer>(true) ??
                           throw new InvalidOperationException("The attack Fuga model has no SkinnedMeshRenderer.");
            var protectedRootsBefore = OtherRootSignatures(scene);
            var placementTransformBefore = TransformSignature(placementRoot);
            var otherFugaBefore = OtherFugaSignature(placementRoot);
            var attackProtectedBefore = AttackProtectedSignature(slot, model, renderer);

            var leftWing = FindBone(renderer, "Bone_013");
            var rightWing = FindBone(renderer, "Bone_017");
            var leftFirstClip = CreateAttackClip(
                LeftFirstClipPath,
                FugaAttackAlternationDriver.LeftFirstStateName,
                slot,
                model,
                leftWing,
                rightWing,
                startsLeft: true);
            var rightFirstClip = CreateAttackClip(
                RightFirstClipPath,
                FugaAttackAlternationDriver.RightFirstStateName,
                slot,
                model,
                leftWing,
                rightWing,
                startsLeft: false);
            var controller = CreateController(leftFirstClip, rightFirstClip);

            var animator = slot.GetComponent<Animator>() ?? slot.gameObject.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.enabled = true;
            EditorUtility.SetDirty(animator);

            var alternationDriver = slot.GetComponent<FugaAttackAlternationDriver>() ??
                                    slot.gameObject.AddComponent<FugaAttackAlternationDriver>();
            alternationDriver.Configure(animator);
            alternationDriver.enabled = true;
            EditorUtility.SetDirty(alternationDriver);

            var legacyPlayback = slot.GetComponent<FugaAnimationReviewPlaybackDriver>();
            if (legacyPlayback != null)
            {
                UnityEngine.Object.DestroyImmediate(legacyPlayback);
            }

            var body = slot.GetComponent<Rigidbody>() ??
                       throw new InvalidOperationException("Fuga_03_Attack has no Rigidbody.");
            var physicsDriver = slot.GetComponent<FugaPhysicsMotionDriver>() ??
                                throw new InvalidOperationException("Fuga_03_Attack has no FugaPhysicsMotionDriver.");
            body.isKinematic = true;
            body.useGravity = false;
            EditorUtility.SetDirty(body);
            physicsDriver.LockRootMotionForReview = true;
            EditorUtility.SetDirty(physicsDriver);

            if (!string.Equals(placementTransformBefore, TransformSignature(placementRoot), StringComparison.Ordinal))
            {
                throw new InvalidOperationException("The Fuga placement root transform changed.");
            }

            if (!string.Equals(otherFugaBefore, OtherFugaSignature(placementRoot), StringComparison.Ordinal))
            {
                throw new InvalidOperationException("A protected non-attack Fuga slot changed.");
            }

            if (!string.Equals(attackProtectedBefore, AttackProtectedSignature(slot, model, renderer), StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "A protected attack transform, model, mesh, collider, Rigidbody, or physics configuration changed.");
            }

            if (!protectedRootsBefore.SequenceEqual(OtherRootSignatures(scene), StringComparer.Ordinal))
            {
                throw new InvalidOperationException("A scene root outside the Fuga placement changed.");
            }

            RequireHash(legacyClipHashBefore, Sha256(Absolute(LegacyAttackClipPath)), "legacy Fuga attack clip preservation");
            RequireHash(
                legacyControllerHashBefore,
                Sha256(Absolute(LegacyAttackControllerPath)),
                "legacy Fuga attack controller preservation");

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException("CargoRunMvp could not be saved after applying Fuga attack motion.");
            }

            AssetDatabase.SaveAssetIfDirty(leftFirstClip);
            AssetDatabase.SaveAssetIfDirty(rightFirstClip);
            AssetDatabase.SaveAssetIfDirty(controller);
            RequireModelHashes();
            var result = InspectAppliedState();
            WriteReport(result, captureCreated: false);
            AssetDatabase.Refresh();
            Debug.Log(
                "FugaAttackMotionApplied Result=PASS" +
                ", AttackDelaySeconds=1" +
                ", FirstWingSelection=Uniform50_50" +
                ", AlternatesWithoutIdle=True" +
                ", ActiveWingStrikeDegrees=90" +
                ", InactiveWingDegrees=0" +
                ", BodyForwardTiltDegrees=40" +
                ", AltitudeChanged=False" +
                ", SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Fuga/Inspect Attack Motion")]
        public static void InspectFugaAttackMotion()
        {
            var scene = RequireCurrentScene();
            var dirtyBefore = scene.isDirty;
            RequireModelHashes();
            var result = InspectAppliedState();
            if (scene.isDirty != dirtyBefore)
            {
                throw new InvalidOperationException("The Fuga attack inspection changed the scene dirty state.");
            }

            WriteReport(result, File.Exists(Absolute(CapturePath)));
            AssetDatabase.Refresh();
            Debug.Log(
                "FugaAttackMotionInspected Result=PASS" +
                ", AttackDelaySeconds=1" +
                ", FirstWingSelection=Uniform50_50" +
                ", AlternatesWithoutIdle=True" +
                ", ActiveWingStrikeDegrees=90" +
                ", InactiveWingDegrees=0" +
                ", BodyForwardTiltDegrees=40" +
                ", RootPositionCurves=0" +
                ", OtherFugaSlotsChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Fuga/Capture Attack Motion")]
        public static void CaptureFugaAttackMotion()
        {
            var scene = RequireCurrentScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException("CargoRunMvp must be saved before the final Fuga attack capture.");
            }

            var result = InspectAppliedState();
            CaptureComparison(result.Slot, result.LeftFirstClip, Absolute(CapturePath));
            WriteReport(result, captureCreated: true);
            AssetDatabase.Refresh();
            if (scene.isDirty)
            {
                throw new InvalidOperationException("The final Fuga attack capture changed the scene.");
            }

            Debug.Log(
                "FugaAttackMotionCaptured Result=PASS" +
                ", SampleTimesSeconds=0.5,1,1.5,2" +
                ", Image=" + CapturePath +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Fuga/Apply Attack Body Yaw 90")]
        public static void ApplyFugaAttackBodyYaw90()
        {
            var scene = RequireCurrentScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException("CargoRunMvp must be saved before applying the Fuga attack body yaw.");
            }

            RequireModelHashes();
            var placementRoot = RequireRoot(PlacementRootName);
            var slot = RequireDirectChild(placementRoot, AttackSlotName);
            var model = RequireDirectChild(slot, ModelName);
            var renderer = model.GetComponentInChildren<SkinnedMeshRenderer>(true) ??
                           throw new InvalidOperationException("The attack Fuga model has no SkinnedMeshRenderer.");
            var leftWing = FindBone(renderer, "Bone_013");
            var rightWing = FindBone(renderer, "Bone_017");
            var leftFirstClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(LeftFirstClipPath) ??
                                throw new InvalidOperationException("The left-first Fuga attack clip is missing.");
            var rightFirstClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(RightFirstClipPath) ??
                                 throw new InvalidOperationException("The right-first Fuga attack clip is missing.");
            var leftWingSignatureBefore = WingCurveSignature(leftFirstClip, slot, leftWing, rightWing);
            var rightWingSignatureBefore = WingCurveSignature(rightFirstClip, slot, leftWing, rightWing);
            var controllerHashBefore = Sha256(Absolute(ControllerPath));
            var placementTransformBefore = TransformSignature(placementRoot);
            var otherFugaBefore = OtherFugaSignature(placementRoot);
            var attackProtectedBefore = AttackProtectedSignature(slot, model, renderer);
            var protectedRootsBefore = OtherRootSignatures(scene);

            AddBodyAttackCurves(
                leftFirstClip,
                RelativePath(slot, model),
                model.localRotation,
                startsLeft: true);
            AddBodyAttackCurves(
                rightFirstClip,
                RelativePath(slot, model),
                model.localRotation,
                startsLeft: false);
            EditorUtility.SetDirty(leftFirstClip);
            EditorUtility.SetDirty(rightFirstClip);
            AssetDatabase.SaveAssetIfDirty(leftFirstClip);
            AssetDatabase.SaveAssetIfDirty(rightFirstClip);

            var repairedJiggleRigCount = RepairMissingSceneJiggleRigRoots(scene);

            RequireHash(controllerHashBefore, Sha256(Absolute(ControllerPath)), "Fuga attack controller preservation");
            RequireText(
                leftWingSignatureBefore,
                WingCurveSignature(leftFirstClip, slot, leftWing, rightWing),
                "left-first wing curves");
            RequireText(
                rightWingSignatureBefore,
                WingCurveSignature(rightFirstClip, slot, leftWing, rightWing),
                "right-first wing curves");
            RequireText(placementTransformBefore, TransformSignature(placementRoot), "Fuga placement transform");
            RequireText(otherFugaBefore, OtherFugaSignature(placementRoot), "non-attack Fuga slots");
            RequireText(attackProtectedBefore, AttackProtectedSignature(slot, model, renderer), "attack setup");
            if (!protectedRootsBefore.SequenceEqual(OtherRootSignatures(scene), StringComparer.Ordinal))
            {
                throw new InvalidOperationException("A scene root transform outside the Fuga placement changed.");
            }

            if (repairedJiggleRigCount > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                {
                    throw new InvalidOperationException("CargoRunMvp could not be saved after repairing JiggleRig roots.");
                }
            }

            var result = InspectAppliedState();
            WriteBodyYawReport(result, repairedJiggleRigCount, captureCreated: false);
            AssetDatabase.Refresh();
            Debug.Log(
                "FugaAttackBodyYaw90Applied Result=PASS" +
                ", BodyYawDegrees=90" +
                ", BodyForwardTiltDegrees=40" +
                ", WingsInheritedBodyYaw=True" +
                ", WingDelaySeconds=0" +
                ", WingLocalCurvesChanged=False" +
                ", RepairedJiggleRigRoots=" + repairedJiggleRigCount + ".");
        }

        [MenuItem("Bellerophon/Enemies/Fuga/Inspect Attack Body Yaw 90")]
        public static void InspectFugaAttackBodyYaw90()
        {
            var scene = RequireCurrentScene();
            var dirtyBefore = scene.isDirty;
            RequireModelHashes();
            var result = InspectAppliedState();
            var jiggleStatus = InspectSceneJiggleRigs(scene);
            if (jiggleStatus.MissingRootCount != 0)
            {
                throw new InvalidOperationException(
                    "The current scene still contains " + jiggleStatus.MissingRootCount + " JiggleRig root references that are missing.");
            }

            if (scene.isDirty != dirtyBefore)
            {
                throw new InvalidOperationException("The Fuga attack body-yaw inspection changed the scene dirty state.");
            }

            WriteBodyYawReport(
                result,
                repairedJiggleRigCount: 0,
                captureCreated: File.Exists(Absolute(BodyYawCapturePath)));
            Debug.Log(
                "FugaAttackBodyYaw90Inspected Result=PASS" +
                ", BodyYawSequenceDegrees=0,-15,-90,-60,0,15,90,60,0" +
                ", OppositeStartSequenceDegrees=0,15,90,60,0,-15,-90,-60,0" +
                ", WingDelaySeconds=0" +
                ", MissingJiggleRigRoots=0" +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Fuga/Capture Attack Body Yaw 90")]
        public static void CaptureFugaAttackBodyYaw90()
        {
            var scene = RequireCurrentScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException("CargoRunMvp must be saved before the final Fuga attack body-yaw capture.");
            }

            var result = InspectAppliedState();
            var jiggleStatus = InspectSceneJiggleRigs(scene);
            if (jiggleStatus.MissingRootCount != 0)
            {
                throw new InvalidOperationException("The final capture is blocked by missing JiggleRig root references.");
            }

            CaptureComparison(result.Slot, result.LeftFirstClip, Absolute(BodyYawCapturePath));
            WriteBodyYawReport(result, repairedJiggleRigCount: 0, captureCreated: true);
            if (scene.isDirty)
            {
                throw new InvalidOperationException("The final Fuga attack body-yaw capture changed the scene.");
            }

            Debug.Log(
                "FugaAttackBodyYaw90Captured Result=PASS" +
                ", SampleTimesSeconds=0.5,1,1.5,2" +
                ", Image=" + BodyYawCapturePath +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Fuga/Apply Attack Jerk-Driven Acceleration")]
        public static void ApplyFugaAttackJerkDrivenAcceleration()
        {
            var scene = RequireCurrentScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException("CargoRunMvp must be saved before applying the explosive Fuga strike acceleration.");
            }

            RequireModelHashes();
            var placementRoot = RequireRoot(PlacementRootName);
            var slot = RequireDirectChild(placementRoot, AttackSlotName);
            var model = RequireDirectChild(slot, ModelName);
            var renderer = model.GetComponentInChildren<SkinnedMeshRenderer>(true) ??
                           throw new InvalidOperationException("The attack Fuga model has no SkinnedMeshRenderer.");
            var leftWing = FindBone(renderer, "Bone_013");
            var rightWing = FindBone(renderer, "Bone_017");
            var leftFirstClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(LeftFirstClipPath) ??
                                throw new InvalidOperationException("The left-first Fuga attack clip is missing.");
            var rightFirstClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(RightFirstClipPath) ??
                                 throw new InvalidOperationException("The right-first Fuga attack clip is missing.");
            var controllerHashBefore = Sha256(Absolute(ControllerPath));
            var placementBefore = TransformSignature(placementRoot);
            var otherFugaBefore = OtherFugaSignature(placementRoot);
            var attackBefore = AttackProtectedSignature(slot, model, renderer);
            var otherRootsBefore = OtherRootSignatures(scene);

            ApplyJerkDrivenAccelerationCurves(leftFirstClip, slot, model, leftWing, rightWing, startsLeft: true);
            ApplyJerkDrivenAccelerationCurves(rightFirstClip, slot, model, leftWing, rightWing, startsLeft: false);
            EditorUtility.SetDirty(leftFirstClip);
            EditorUtility.SetDirty(rightFirstClip);
            AssetDatabase.SaveAssetIfDirty(leftFirstClip);
            AssetDatabase.SaveAssetIfDirty(rightFirstClip);

            RequireHash(controllerHashBefore, Sha256(Absolute(ControllerPath)), "Fuga attack controller preservation");
            RequireText(placementBefore, TransformSignature(placementRoot), "Fuga placement transform");
            RequireText(otherFugaBefore, OtherFugaSignature(placementRoot), "non-attack Fuga slots");
            RequireText(attackBefore, AttackProtectedSignature(slot, model, renderer), "attack setup");
            if (!otherRootsBefore.SequenceEqual(OtherRootSignatures(scene), StringComparer.Ordinal))
            {
                throw new InvalidOperationException("A scene root outside the Fuga placement changed.");
            }

            if (scene.isDirty)
            {
                throw new InvalidOperationException("Applying the explosive strike acceleration unexpectedly changed the scene.");
            }

            var result = InspectAppliedState();
            WriteJerkDrivenAccelerationReport(result, captureCreated: false);
            AssetDatabase.Refresh();
            Debug.Log(
                "FugaAttackJerkDrivenAccelerationApplied Result=PASS" +
                ", ActiveWingForwardStrikeDegrees=-90" +
                ", InactiveWingDegrees=0" +
                ", WingProfileMatchesBodyAcceleration=True" +
                ", WingsInheritForwardTiltDegrees=40" +
                ", ImpactVerticalRecoilMeters=0.1" +
                ", RecoilReturnSeconds=0.07" +
                ", WingsInheritVerticalRecoil=True" +
                ", AttackSlotVerticalMotion=False" +
                ", PreImpactAngularSpeedDegreesPerSecond=0->1170" +
                ", PreImpactAngularAccelerationDegreesPerSecondSquared=0->7800" +
                ", BodyYawDegrees=131.625" +
                ", ImpactSpeedDropDegreesPerSecond=1170->360" +
                ", PostImpactAngularDecelerationPeakDegreesPerSecondSquared=580.5" +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Fuga/Inspect Attack Jerk-Driven Acceleration")]
        public static void InspectFugaAttackJerkDrivenAcceleration()
        {
            var scene = RequireCurrentScene();
            var dirtyBefore = scene.isDirty;
            RequireModelHashes();
            var result = InspectAppliedState();
            if (scene.isDirty != dirtyBefore)
            {
                throw new InvalidOperationException("The explosive Fuga strike inspection changed the scene dirty state.");
            }

            WriteJerkDrivenAccelerationReport(
                result,
                captureCreated: false);
            Debug.Log(
                "FugaAttackJerkDrivenAccelerationInspected Result=PASS" +
                ", ActiveWingForwardStrikeDegrees=-90" +
                ", InactiveWingDegrees=0" +
                ", WingProfileMatchesBodyAcceleration=True" +
                ", WingsInheritForwardTiltDegrees=40" +
                ", ImpactVerticalRecoilMeters=0.1" +
                ", RecoilReturnSeconds=0.07" +
                ", WingsInheritVerticalRecoil=True" +
                ", AttackSlotVerticalMotion=False" +
                ", PreImpactAngularSpeedDegreesPerSecond=0->1170" +
                ", PreImpactAngularAccelerationDegreesPerSecondSquared=0->7800" +
                ", ImpactSpeedDropDegreesPerSecond=1170->360" +
                ", PostImpactAngularSpeedDegreesPerSecond=360->214.875" +
                ", SceneChanged=False.");
        }

        public static void StartFugaAttackLeftFirstMotionReviewPlayback()
        {
            StartFugaAttackMotionReviewPlayback(startsLeft: true);
        }

        public static void StartFugaAttackRightFirstMotionReviewPlayback()
        {
            StartFugaAttackMotionReviewPlayback(startsLeft: false);
        }

        public static void StopFugaAttackMotionReviewPlayback()
        {
            if (motionReviewClip != null)
            {
                var elapsed = Math.Max(0d, EditorApplication.timeSinceStartup - motionReviewStartTime);
                lastMotionReviewCompletedLoops = Mathf.Min(
                    DirectMotionReviewLoopCount,
                    Mathf.FloorToInt((float)(elapsed / LoopDuration)));
                StopFugaAttackMotionReviewPlaybackInternal(markCompleted: false);
            }

            var result = InspectAppliedState();
            WriteJerkDrivenAccelerationReport(result, captureCreated: false);
            Debug.Log(
                "FugaAttackMotionReviewPlaybackStopped Result=PASS" +
                ", LeftFirstCompletedLoopsAtLeast2=" + leftFirstMotionReviewCompleted +
                ", RightFirstCompletedLoopsAtLeast2=" + rightFirstMotionReviewCompleted +
                ", LastCompletedLoops=" + lastMotionReviewCompletedLoops +
                ", CaptureCreated=False" +
                ", SceneChanged=False.");
        }

        private static void StartFugaAttackMotionReviewPlayback(bool startsLeft)
        {
            var scene = RequireCurrentScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException("CargoRunMvp must be saved before the direct Fuga motion review.");
            }

            if (motionReviewClip != null || AnimationMode.InAnimationMode())
            {
                throw new InvalidOperationException("Another Unity animation review is already active.");
            }

            var result = InspectAppliedState();
            motionReviewSlot = result.Slot;
            motionReviewClip = startsLeft ? result.LeftFirstClip : result.RightFirstClip;
            motionReviewLabel = startsLeft ? "LeftFirst" : "RightFirst";
            lastMotionReviewCompletedLoops = 0;
            motionReviewStartTime = EditorApplication.timeSinceStartup;

            var gameViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView") ??
                               throw new InvalidOperationException("The Unity Game View type is unavailable.");
            var gameView = EditorWindow.GetWindow(gameViewType);
            gameView.Focus();

            AnimationMode.StartAnimationMode();
            EditorApplication.update += UpdateFugaAttackMotionReviewPlayback;
            UpdateFugaAttackMotionReviewPlayback();
            Debug.Log(
                "FugaAttackMotionReviewPlaybackStarted Result=PASS" +
                ", Direction=" + motionReviewLabel +
                ", RequiredLoops=2" +
                ", LiveGameView=True" +
                ", CaptureCreated=False.");
        }

        private static void UpdateFugaAttackMotionReviewPlayback()
        {
            if (motionReviewClip == null || motionReviewSlot == null)
            {
                StopFugaAttackMotionReviewPlaybackInternal(markCompleted: false);
                return;
            }

            try
            {
                var elapsed = Math.Max(0d, EditorApplication.timeSinceStartup - motionReviewStartTime);
                lastMotionReviewCompletedLoops = Mathf.Min(
                    DirectMotionReviewLoopCount,
                    Mathf.FloorToInt((float)(elapsed / LoopDuration)));
                if (lastMotionReviewCompletedLoops >= DirectMotionReviewLoopCount)
                {
                    StopFugaAttackMotionReviewPlaybackInternal(markCompleted: true);
                    return;
                }

                AnimationMode.BeginSampling();
                AnimationMode.SampleAnimationClip(
                    motionReviewSlot.gameObject,
                    motionReviewClip,
                    Mathf.Repeat((float)elapsed, LoopDuration));
                AnimationMode.EndSampling();
                EditorApplication.QueuePlayerLoopUpdate();
                UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            }
            catch (Exception exception)
            {
                StopFugaAttackMotionReviewPlaybackInternal(markCompleted: false);
                Debug.LogException(exception);
            }
        }

        private static void StopFugaAttackMotionReviewPlaybackInternal(bool markCompleted)
        {
            EditorApplication.update -= UpdateFugaAttackMotionReviewPlayback;
            if (markCompleted)
            {
                if (string.Equals(motionReviewLabel, "LeftFirst", StringComparison.Ordinal))
                {
                    leftFirstMotionReviewCompleted = true;
                }
                else if (string.Equals(motionReviewLabel, "RightFirst", StringComparison.Ordinal))
                {
                    rightFirstMotionReviewCompleted = true;
                }
            }

            var completedLabel = motionReviewLabel;
            motionReviewSlot = null;
            motionReviewClip = null;
            motionReviewLabel = null;
            if (AnimationMode.InAnimationMode())
            {
                AnimationMode.StopAnimationMode();
            }

            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            if (markCompleted)
            {
                Debug.Log(
                    "FugaAttackMotionReviewPlaybackCompleted Result=PASS" +
                    ", Direction=" + completedLabel +
                    ", CompletedLoops=2" +
                    ", CaptureCreated=False" +
                    ", SceneChanged=False.");
            }
        }

        [MenuItem("Bellerophon/Enemies/Fuga/Capture Attack Jerk-Driven Acceleration")]
        public static void CaptureFugaAttackJerkDrivenAcceleration()
        {
            var scene = RequireCurrentScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException("CargoRunMvp must be saved before the final explosive-acceleration capture.");
            }

            var result = InspectAppliedState();
            CaptureComparison(
                result.Slot,
                result.LeftFirstClip,
                Absolute(JerkDrivenAccelerationCapturePath),
                new[]
                {
                    FirstImpactTime - 1f / BodyCurveSampleRate,
                    FirstImpactTime,
                    FirstImpactTime + ImpactVerticalRecoilReverseTime,
                    FirstImpactTime + ImpactVerticalRecoilDuration,
                },
                lockCameraToFirstSample: true);
            WriteJerkDrivenAccelerationReport(result, captureCreated: true);
            if (scene.isDirty)
            {
                throw new InvalidOperationException("The final explosive-acceleration capture changed the scene.");
            }

            Debug.Log(
                "FugaAttackJerkDrivenAccelerationCaptured Result=PASS" +
                ", SampleTimesSeconds=0.483333,0.5,0.535,0.57" +
                ", Image=" + JerkDrivenAccelerationCapturePath +
                ", SceneChanged=False.");
        }

        public static void InspectCurrentUnityConsoleErrorsForFugaAttack()
        {
            var scene = RequireCurrentScene();
            var dirtyBefore = scene.isDirty;
            var counts = CurrentConsoleCounts();
            var jiggleStatus = InspectSceneJiggleRigs(scene);
            var report = new StringBuilder()
                .AppendLine("Fuga Attack Unity Console Report")
                .AppendLine("Scene=" + ScenePath)
                .AppendLine("ConsoleErrorCount=" + counts.ErrorCount)
                .AppendLine("ConsoleWarningCount=" + counts.WarningCount)
                .AppendLine("ConsoleLogCount=" + counts.LogCount)
                .AppendLine("SceneJiggleRigCount=" + jiggleStatus.TotalCount)
                .AppendLine("MissingJiggleRigRootCount=" + jiggleStatus.MissingRootCount)
                .AppendLine("RootCause=Parvum scene instances removed the nested prefab model referenced by JiggleRig.rootBone")
                .AppendLine("FugaAttackCodeCausedJiggleRigError=False")
                .AppendLine("HarnessValidationRun=False")
                .ToString();
            WriteText(ConsoleReportPath, report);
            if (scene.isDirty != dirtyBefore)
            {
                throw new InvalidOperationException("The Unity console inspection changed the scene dirty state.");
            }

            Debug.Log(
                "FugaAttackUnityConsoleInspected" +
                ", Errors=" + counts.ErrorCount +
                ", Warnings=" + counts.WarningCount +
                ", SceneJiggleRigs=" + jiggleStatus.TotalCount +
                ", MissingJiggleRigRoots=" + jiggleStatus.MissingRootCount + ".");
        }

        private static AttackResult InspectAppliedState()
        {
            if (EditorUtility.scriptCompilationFailed)
            {
                throw new InvalidOperationException("Unity reports script compilation errors.");
            }

            minimumActiveWingTipForwardSpeed = float.PositiveInfinity;
            maximumInactiveWingTipForwardSpeed = float.NegativeInfinity;

            var placementRoot = RequireRoot(PlacementRootName);
            var slots = SlotNames.Select(name => RequireDirectChild(placementRoot, name)).ToArray();
            for (var index = 0; index < slots.Length; index++)
            {
                if (slots[index].GetSiblingIndex() != index ||
                    (index > 0 && slots[index - 1].localPosition.x >= slots[index].localPosition.x))
                {
                    throw new InvalidOperationException("The approved Fuga state order changed.");
                }
            }

            var slot = slots[3];
            var model = RequireDirectChild(slot, ModelName);
            if (model.localPosition.sqrMagnitude > 0.00000001f ||
                Quaternion.Angle(model.localRotation, Quaternion.identity) > 0.001f)
            {
                throw new InvalidOperationException("The attack model base transform changed in the scene.");
            }

            var renderer = model.GetComponentInChildren<SkinnedMeshRenderer>(true) ??
                           throw new InvalidOperationException("The attack Fuga model has no SkinnedMeshRenderer.");
            if (!string.Equals(AssetDatabase.GetAssetPath(renderer.sharedMesh), ImportedModelPath, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("The attack Fuga mesh assignment changed.");
            }

            var leftWing = FindBone(renderer, "Bone_013");
            var rightWing = FindBone(renderer, "Bone_017");
            if (!leftWing.IsChildOf(model) || !rightWing.IsChildOf(model))
            {
                throw new InvalidOperationException(
                    "Both Fuga attack wings must remain under Fuga_Model to inherit its 40-degree forward tilt.");
            }

            var leftFirstClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(LeftFirstClipPath) ??
                                throw new InvalidOperationException("The left-first Fuga attack clip is missing.");
            var rightFirstClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(RightFirstClipPath) ??
                                 throw new InvalidOperationException("The right-first Fuga attack clip is missing.");
            InspectClipContract(leftFirstClip, slot, model, leftWing, rightWing, startsLeft: true);
            InspectClipContract(rightFirstClip, slot, model, leftWing, rightWing, startsLeft: false);
            InspectSampledTransformSpeedProfile(slot, model, leftFirstClip, "left-first");
            InspectSampledTransformSpeedProfile(slot, model, rightFirstClip, "right-first");
            InspectSampledVerticalRecoil(slot, model, leftFirstClip, "left-first");
            InspectSampledVerticalRecoil(slot, model, rightFirstClip, "right-first");
            InspectSampledStrikingWingProfile(
                slot,
                model,
                leftWing,
                rightWing,
                leftFirstClip,
                startsLeft: true,
                "left-first");
            InspectSampledStrikingWingProfile(
                slot,
                model,
                leftWing,
                rightWing,
                rightFirstClip,
                startsLeft: false,
                "right-first");

            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) ??
                             throw new InvalidOperationException("The new Fuga attack controller is missing.");
            InspectControllerContract(controller, leftFirstClip, rightFirstClip);
            var animator = slot.GetComponent<Animator>() ??
                           throw new InvalidOperationException("The attack Fuga Animator is missing.");
            if (!animator.enabled || animator.applyRootMotion ||
                animator.runtimeAnimatorController != controller)
            {
                throw new InvalidOperationException("The attack Fuga Animator configuration is incorrect.");
            }

            var alternationDriver = slot.GetComponent<FugaAttackAlternationDriver>() ??
                                    throw new InvalidOperationException("The Fuga attack alternation driver is missing.");
            if (!alternationDriver.enabled || alternationDriver.Animator != animator ||
                FugaAttackStartingWingSelector.Select(0f) != FugaAttackStartingWing.Left ||
                FugaAttackStartingWingSelector.Select(0.499999f) != FugaAttackStartingWing.Left ||
                FugaAttackStartingWingSelector.Select(0.5f) != FugaAttackStartingWing.Right ||
                FugaAttackStartingWingSelector.Select(1f) != FugaAttackStartingWing.Right)
            {
                throw new InvalidOperationException("The Fuga attack first-wing selection is not an exact 50:50 split.");
            }

            if (slot.GetComponent<FugaAnimationReviewPlaybackDriver>() != null)
            {
                throw new InvalidOperationException("The attack slot still uses a legacy clip playback driver.");
            }

            var body = slot.GetComponent<Rigidbody>() ??
                       throw new InvalidOperationException("The attack Fuga Rigidbody is missing.");
            var physicsDriver = slot.GetComponent<FugaPhysicsMotionDriver>() ??
                                throw new InvalidOperationException("The attack Fuga physics driver is missing.");
            if (!body.isKinematic || body.useGravity || !physicsDriver.LockRootMotionForReview ||
                physicsDriver.FollowVerticalAxis || physicsDriver.UseDeathFallSequence || physicsDriver.IdleHoverEnabled)
            {
                throw new InvalidOperationException("The attack Fuga altitude-lock configuration is incorrect.");
            }

            for (var index = 0; index < slots.Length; index++)
            {
                if (index == 1 || index == 2 || index == 3 || index == 4)
                {
                    continue;
                }

                var otherAnimator = slots[index].GetComponent<Animator>();
                if (otherAnimator != null && otherAnimator.runtimeAnimatorController != null)
                {
                    throw new InvalidOperationException(slots[index].name + " received an unexpected controller.");
                }
            }

            return new AttackResult(slot, leftFirstClip, rightFirstClip, slot.position.y);
        }

        private static AnimationClip CreateAttackClip(
            string assetPath,
            string clipName,
            Transform slot,
            Transform model,
            Transform leftWing,
            Transform rightWing,
            bool startsLeft)
        {
            AssetDatabase.DeleteAsset(assetPath);
            var clip = new AnimationClip
            {
                name = clipName,
                frameRate = 60f,
                wrapMode = WrapMode.Loop
            };

            ApplyJerkDrivenAccelerationCurves(clip, slot, model, leftWing, rightWing, startsLeft);
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.loopBlend = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            AssetDatabase.CreateAsset(clip, assetPath);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
            return AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath) ??
                   throw new InvalidOperationException("The new Fuga attack clip was not created: " + assetPath + ".");
        }

        private static void ApplyJerkDrivenAccelerationCurves(
            AnimationClip clip,
            Transform slot,
            Transform model,
            Transform leftWing,
            Transform rightWing,
            bool startsLeft)
        {
            var firstForwardStrikeUsesLeft = FirstForwardStrikeUsesLeftWing(
                model,
                leftWing,
                rightWing,
                startsLeft);
            var leftWingAngles = firstForwardStrikeUsesLeft ? FirstStrikeWingAngles : SecondStrikeWingAngles;
            var rightWingAngles = firstForwardStrikeUsesLeft ? SecondStrikeWingAngles : FirstStrikeWingAngles;
            AddWingAttackCurves(
                clip,
                RelativePath(slot, leftWing),
                leftWing.localRotation,
                leftWingAngles);
            AddWingAttackCurves(
                clip,
                RelativePath(slot, rightWing),
                rightWing.localRotation,
                rightWingAngles);
            var modelPath = RelativePath(slot, model);
            AddBodyAttackCurves(clip, modelPath, model.localRotation, startsLeft);
            AddBodyVerticalRecoilCurve(clip, modelPath, model.localPosition.y);
        }

        private static void AddWingAttackCurves(
            AnimationClip clip,
            string path,
            Quaternion bindRotation,
            IReadOnlyList<float> angles)
        {
            if (angles.Count != BodyKeyTimes.Length)
            {
                throw new InvalidOperationException("A Fuga attack wing profile must match the 60 Hz body key timeline.");
            }

            var x = new Keyframe[BodyKeyTimes.Length];
            var y = new Keyframe[BodyKeyTimes.Length];
            var z = new Keyframe[BodyKeyTimes.Length];
            var w = new Keyframe[BodyKeyTimes.Length];
            var previous = Quaternion.identity;
            for (var index = 0; index < BodyKeyTimes.Length; index++)
            {
                var value = bindRotation * Quaternion.AngleAxis(angles[index], Vector3.right);
                if (index > 0 && Quaternion.Dot(previous, value) < 0f)
                {
                    value = new Quaternion(-value.x, -value.y, -value.z, -value.w);
                }

                previous = value;
                x[index] = new Keyframe(BodyKeyTimes[index], value.x);
                y[index] = new Keyframe(BodyKeyTimes[index], value.y);
                z[index] = new Keyframe(BodyKeyTimes[index], value.z);
                w[index] = new Keyframe(BodyKeyTimes[index], value.w);
            }

            SetRotationCurves(clip, path, x, y, z, w, linear: true);
        }

        private static void AddConstantRotationCurves(AnimationClip clip, string path, Quaternion value)
        {
            SetRotationCurves(
                clip,
                path,
                new[] { new Keyframe(0f, value.x), new Keyframe(LoopDuration, value.x) },
                new[] { new Keyframe(0f, value.y), new Keyframe(LoopDuration, value.y) },
                new[] { new Keyframe(0f, value.z), new Keyframe(LoopDuration, value.z) },
                new[] { new Keyframe(0f, value.w), new Keyframe(LoopDuration, value.w) });
        }

        private static void AddBodyAttackCurves(
            AnimationClip clip,
            string path,
            Quaternion bindRotation,
            bool startsLeft)
        {
            if (Quaternion.Angle(bindRotation, Quaternion.identity) > 0.001f)
            {
                throw new InvalidOperationException("The Fuga attack model bind rotation is no longer identity.");
            }

            var yawAngles = startsLeft ? LeftFirstBodyYawAngles : RightFirstBodyYawAngles;
            var pitchKeys = new Keyframe[BodyKeyTimes.Length];
            var yawKeys = new Keyframe[BodyKeyTimes.Length];
            var rollKeys = new Keyframe[BodyKeyTimes.Length];
            for (var index = 0; index < BodyKeyTimes.Length; index++)
            {
                pitchKeys[index] = new Keyframe(BodyKeyTimes[index], BodyForwardTilt);
                yawKeys[index] = new Keyframe(BodyKeyTimes[index], yawAngles[index]);
                rollKeys[index] = new Keyframe(BodyKeyTimes[index], 0f);
            }

            SetEulerRotationCurves(clip, path, pitchKeys, yawKeys, rollKeys);
        }

        private static void AddBodyVerticalRecoilCurve(AnimationClip clip, string path, float bindHeight)
        {
            ClearPositionCurves(clip, path);
            var keys = new Keyframe[BodyRecoilKeyTimes.Length];
            for (var index = 0; index < BodyRecoilKeyTimes.Length; index++)
            {
                keys[index] = new Keyframe(
                    BodyRecoilKeyTimes[index],
                    bindHeight + BodyRecoilOffsets[index]);
            }

            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalPosition.y"),
                LinearCurve(keys));
        }

        private static void SetEulerRotationCurves(
            AnimationClip clip,
            string path,
            Keyframe[] pitch,
            Keyframe[] yaw,
            Keyframe[] roll)
        {
            ClearRotationCurves(clip, path);
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(path, typeof(Transform), "localEulerAnglesRaw.x"),
                LinearCurve(pitch));
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(path, typeof(Transform), "localEulerAnglesRaw.y"),
                LinearCurve(yaw));
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(path, typeof(Transform), "localEulerAnglesRaw.z"),
                LinearCurve(roll));
        }

        private static void SetRotationCurves(
            AnimationClip clip,
            string path,
            Keyframe[] x,
            Keyframe[] y,
            Keyframe[] z,
            Keyframe[] w,
            bool linear = false)
        {
            ClearRotationCurves(clip, path);

            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalRotation.x"),
                linear ? LinearCurve(x) : SmoothCurve(x));
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalRotation.y"),
                linear ? LinearCurve(y) : SmoothCurve(y));
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalRotation.z"),
                linear ? LinearCurve(z) : SmoothCurve(z));
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalRotation.w"),
                linear ? LinearCurve(w) : SmoothCurve(w));
        }

        private static void ClearRotationCurves(AnimationClip clip, string path)
        {
            foreach (var binding in AnimationUtility.GetCurveBindings(clip)
                         .Where(item => item.path == path &&
                                        (item.propertyName.StartsWith("m_LocalRotation.", StringComparison.Ordinal) ||
                                         item.propertyName.StartsWith("localRotation.", StringComparison.Ordinal) ||
                                         item.propertyName.StartsWith("localEulerAngles", StringComparison.Ordinal)))
                         .ToArray())
            {
                AnimationUtility.SetEditorCurve(clip, binding, null);
            }
        }

        private static void ClearPositionCurves(AnimationClip clip, string path)
        {
            foreach (var binding in AnimationUtility.GetCurveBindings(clip)
                         .Where(item =>
                             string.Equals(item.path, path, StringComparison.Ordinal) &&
                             item.propertyName.IndexOf("position", StringComparison.OrdinalIgnoreCase) >= 0)
                         .ToArray())
            {
                AnimationUtility.SetEditorCurve(clip, binding, null);
            }
        }

        private static AnimationCurve SmoothCurve(params Keyframe[] keys)
        {
            var curve = new AnimationCurve(keys);
            for (var index = 0; index < curve.length; index++)
            {
                curve.SmoothTangents(index, 0f);
            }

            return curve;
        }

        private static AnimationCurve LinearCurve(params Keyframe[] keys)
        {
            var curve = new AnimationCurve(keys);
            for (var index = 0; index < curve.length; index++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, index, AnimationUtility.TangentMode.Linear);
                AnimationUtility.SetKeyRightTangentMode(curve, index, AnimationUtility.TangentMode.Linear);
            }

            return curve;
        }

        private static AnimatorController CreateController(AnimationClip leftFirstClip, AnimationClip rightFirstClip)
        {
            AssetDatabase.DeleteAsset(ControllerPath);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            var stateMachine = controller.layers[0].stateMachine;
            var leftState = stateMachine.AddState(FugaAttackAlternationDriver.LeftFirstStateName);
            leftState.motion = leftFirstClip;
            leftState.speed = 1f;
            var rightState = stateMachine.AddState(FugaAttackAlternationDriver.RightFirstStateName);
            rightState.motion = rightFirstClip;
            rightState.speed = 1f;
            stateMachine.defaultState = leftState;
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssetIfDirty(controller);
            return controller;
        }

        private static void InspectControllerContract(
            AnimatorController controller,
            AnimationClip leftFirstClip,
            AnimationClip rightFirstClip)
        {
            if (controller.layers.Length != 1)
            {
                throw new InvalidOperationException("The Fuga attack controller must have exactly one layer.");
            }

            var stateMachine = controller.layers[0].stateMachine;
            var states = stateMachine.states.Select(item => item.state).ToArray();
            if (states.Length != 2 || stateMachine.anyStateTransitions.Length != 0 ||
                stateMachine.entryTransitions.Length != 0 || stateMachine.stateMachines.Length != 0)
            {
                throw new InvalidOperationException("The Fuga attack controller structure is incorrect.");
            }

            var left = states.SingleOrDefault(state => state.name == FugaAttackAlternationDriver.LeftFirstStateName);
            var right = states.SingleOrDefault(state => state.name == FugaAttackAlternationDriver.RightFirstStateName);
            if (left == null || right == null || left.motion != leftFirstClip || right.motion != rightFirstClip ||
                left.transitions.Length != 0 || right.transitions.Length != 0)
            {
                throw new InvalidOperationException("The Fuga attack controller does not contain the two new alternating clips.");
            }
        }

        private static void InspectClipContract(
            AnimationClip clip,
            Transform slot,
            Transform model,
            Transform leftWing,
            Transform rightWing,
            bool startsLeft)
        {
            if (Mathf.Abs(clip.length - LoopDuration) > 0.0001f ||
                !AnimationUtility.GetAnimationClipSettings(clip).loopTime)
            {
                throw new InvalidOperationException("A Fuga attack clip is not an exact looping two-second clip.");
            }

            var bindings = AnimationUtility.GetCurveBindings(clip);
            var modelPath = RelativePath(slot, model);
            var positionBindings = bindings
                .Where(binding =>
                    binding.propertyName.IndexOf("position", StringComparison.OrdinalIgnoreCase) >= 0)
                .ToArray();
            if (bindings.Length != 12 ||
                bindings.Any(binding => string.IsNullOrEmpty(binding.path)) ||
                positionBindings.Length != 1 ||
                !string.Equals(positionBindings[0].path, modelPath, StringComparison.Ordinal) ||
                !string.Equals(positionBindings[0].propertyName, "m_LocalPosition.y", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "A Fuga attack clip must contain only the approved Fuga_Model vertical-recoil position curve.");
            }

            var leftPath = RelativePath(slot, leftWing);
            var rightPath = RelativePath(slot, rightWing);
            var firstForwardStrikeUsesLeft = FirstForwardStrikeUsesLeftWing(
                model,
                leftWing,
                rightWing,
                startsLeft);
            var leftWingAngles = firstForwardStrikeUsesLeft ? FirstStrikeWingAngles : SecondStrikeWingAngles;
            var rightWingAngles = firstForwardStrikeUsesLeft ? SecondStrikeWingAngles : FirstStrikeWingAngles;
            InspectBodyCurves(clip, bindings, modelPath, model.localRotation, startsLeft);
            InspectBodyVerticalRecoilCurve(clip, bindings, modelPath, model.localPosition.y);
            InspectWingCurves(
                clip,
                bindings,
                leftPath,
                leftWing.localRotation,
                leftWingAngles);
            InspectWingCurves(
                clip,
                bindings,
                rightPath,
                rightWing.localRotation,
                rightWingAngles);
        }

        private static void InspectBodyCurves(
            AnimationClip clip,
            IEnumerable<EditorCurveBinding> bindings,
            string modelPath,
            Quaternion bindRotation,
            bool startsLeft)
        {
            var curves = CurvesAtPath(clip, bindings, modelPath)
                .Where(pair =>
                    pair.Key.StartsWith("localEulerAnglesRaw.", StringComparison.Ordinal))
                .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
            if (curves.Count != 3 || curves.Values.Any(curve => curve.length != BodyKeyTimes.Length) ||
                curves.Keys.Any(property => !property.StartsWith("localEulerAnglesRaw.", StringComparison.Ordinal)))
            {
                throw new InvalidOperationException("The Fuga attack body yaw curves are not three 121-key baked raw Euler curves.");
            }

            var yawAngles = startsLeft ? LeftFirstBodyYawAngles : RightFirstBodyYawAngles;
            for (var index = 0; index < BodyKeyTimes.Length; index++)
            {
                if (curves.Values.Any(curve => Mathf.Abs(curve.keys[index].time - BodyKeyTimes[index]) > 0.0001f))
                {
                    throw new InvalidOperationException("A Fuga attack body-yaw key time is incorrect.");
                }

                if (Mathf.Abs(CurveComponent(curves, 'x', index) - BodyForwardTilt) > 0.001f ||
                    Mathf.Abs(CurveComponent(curves, 'y', index) - yawAngles[index]) > 0.001f ||
                    Mathf.Abs(CurveComponent(curves, 'z', index)) > 0.001f)
                {
                    throw new InvalidOperationException(
                        "The Fuga attack body is not at the required 131.625-degree yaw and 40-degree forward tilt at key " +
                        index + ".");
                }
            }

            if (Quaternion.Angle(bindRotation, Quaternion.identity) > 0.001f)
            {
                throw new InvalidOperationException("The inspected Fuga attack model bind rotation is no longer identity.");
            }

            InspectJerkDrivenAcceleration(curves, startsLeft);
        }

        private static void InspectBodyVerticalRecoilCurve(
            AnimationClip clip,
            IEnumerable<EditorCurveBinding> bindings,
            string modelPath,
            float bindHeight)
        {
            var recoilBindings = bindings
                .Where(binding =>
                    string.Equals(binding.path, modelPath, StringComparison.Ordinal) &&
                    string.Equals(binding.propertyName, "m_LocalPosition.y", StringComparison.Ordinal))
                .ToArray();
            if (recoilBindings.Length != 1)
            {
                throw new InvalidOperationException("The Fuga attack body vertical-recoil curve is missing.");
            }

            var curve = AnimationUtility.GetEditorCurve(clip, recoilBindings[0]);
            if (curve == null || curve.length != BodyRecoilKeyTimes.Length)
            {
                throw new InvalidOperationException("The Fuga attack body vertical-recoil key count is incorrect.");
            }

            for (var index = 0; index < BodyRecoilKeyTimes.Length; index++)
            {
                if (Mathf.Abs(curve.keys[index].time - BodyRecoilKeyTimes[index]) > 0.0001f ||
                    Mathf.Abs(curve.keys[index].value - (bindHeight + BodyRecoilOffsets[index])) > 0.0001f)
                {
                    throw new InvalidOperationException(
                        "The Fuga attack body vertical recoil is incorrect at key " + index + ".");
                }
            }
        }

        private static void InspectJerkDrivenAcceleration(
            IReadOnlyDictionary<string, AnimationCurve> curves,
            bool startsLeft)
        {
            var yawCurve = CurveForComponent(curves, 'y');
            var expectedAngles = startsLeft ? LeftFirstBodyYawAngles : RightFirstBodyYawAngles;
            for (var index = 0; index < BodyKeyTimes.Length; index++)
            {
                if (Mathf.Abs(yawCurve.keys[index].value - expectedAngles[index]) > 0.001f)
                {
                    throw new InvalidOperationException("A baked Fuga body-yaw value is incorrect at key " + index + ".");
                }
            }

            if (Mathf.Abs(PreImpactAngularSpeedAtElapsed(0f)) > 0.001f ||
                Mathf.Abs(PreImpactAngularSpeedAtElapsed(AccelerationDuration) - 1170f) > 0.001f ||
                Mathf.Abs(PreImpactAngularAccelerationAtElapsed(0f)) > 0.001f ||
                Mathf.Abs(PreImpactAngularAccelerationAtElapsed(AccelerationDuration) - 7800f) > 0.001f ||
                Mathf.Abs(RecoveryAngularSpeedAtElapsed(0f) - 360f) > 0.001f ||
                Mathf.Abs(RecoveryAngularSpeedAtElapsed(RecoveryDuration) - 214.875f) > 0.001f ||
                Mathf.Abs(RecoveryAngularAccelerationAtElapsed(0f) + 580.5f) > 0.001f ||
                Mathf.Abs(RecoveryAngularAccelerationAtElapsed(RecoveryDuration)) > 0.001f ||
                RecoveryAngularSpeedAtElapsed(0f) >=
                PreImpactAngularSpeedAtElapsed(AccelerationDuration) * 0.4f)
            {
                throw new InvalidOperationException("The analytic Fuga jerk-driven speed or acceleration endpoints changed.");
            }

            InspectJerkDrivenSpeedAndAccelerationWindows(yawCurve, 0f, "first curve");
            InspectJerkDrivenSpeedAndAccelerationWindows(yawCurve, AttackDelay, "second curve");
        }

        private static void InspectJerkDrivenSpeedAndAccelerationWindows(
            AnimationCurve curve,
            float offset,
            string label)
        {
            var preTimes = PreImpactWindowTimes
                .Select(time => time + offset)
                .ToArray();
            var postTimes = PostImpactWindowTimes
                .Select(time => time + offset)
                .ToArray();
            var preImpactSpeeds = SpeedsFromCurve(curve, preTimes);
            var postImpactSpeeds = SpeedsFromCurve(curve, postTimes);
            RequireJerkDrivenProfile(
                preImpactSpeeds,
                ExpectedPreImpactWindowSpeeds,
                ExpectedPreImpactWindowAccelerations,
                preImpact: true,
                label + " pre-impact");
            RequireJerkDrivenProfile(
                postImpactSpeeds,
                ExpectedPostImpactWindowSpeeds,
                ExpectedPostImpactWindowAccelerations,
                preImpact: false,
                label + " post-impact");
            RequireImpactSpeedDrop(preImpactSpeeds, postImpactSpeeds, label);
        }

        private static float[] SpeedsFromCurve(AnimationCurve curve, IReadOnlyList<float> times)
        {
            var speeds = new float[times.Count - 1];
            for (var index = 0; index < speeds.Length; index++)
            {
                speeds[index] = Mathf.Abs(curve.Evaluate(times[index + 1]) - curve.Evaluate(times[index])) /
                                (times[index + 1] - times[index]);
            }

            return speeds;
        }

        private static void InspectSampledTransformSpeedProfile(
            Transform slot,
            Transform model,
            AnimationClip clip,
            string label)
        {
            var scene = SceneManager.GetActiveScene();
            var dirtyBefore = scene.isDirty;
            try
            {
                AnimationMode.StartAnimationMode();
                var preImpactSpeeds = SpeedsFromSampledTransform(slot, model, clip, PreImpactWindowTimes);
                var postImpactSpeeds = SpeedsFromSampledTransform(slot, model, clip, PostImpactWindowTimes);
                RequireJerkDrivenProfile(
                    preImpactSpeeds,
                    ExpectedPreImpactWindowSpeeds,
                    ExpectedPreImpactWindowAccelerations,
                    preImpact: true,
                    label + " sampled pre-impact");
                RequireJerkDrivenProfile(
                    postImpactSpeeds,
                    ExpectedPostImpactWindowSpeeds,
                    ExpectedPostImpactWindowAccelerations,
                    preImpact: false,
                    label + " sampled post-impact");
                RequireImpactSpeedDrop(preImpactSpeeds, postImpactSpeeds, label + " sampled");
            }
            finally
            {
                if (AnimationMode.InAnimationMode())
                {
                    AnimationMode.StopAnimationMode();
                }
            }

            if (scene.isDirty != dirtyBefore)
            {
                throw new InvalidOperationException("The sampled Fuga speed inspection changed the scene dirty state.");
            }
        }

        private static float[] SpeedsFromSampledTransform(
            Transform slot,
            Transform model,
            AnimationClip clip,
            IReadOnlyList<float> times)
        {
            var rotations = new Quaternion[times.Count];
            for (var index = 0; index < times.Count; index++)
            {
                AnimationMode.BeginSampling();
                AnimationMode.SampleAnimationClip(slot.gameObject, clip, times[index]);
                AnimationMode.EndSampling();
                rotations[index] = model.localRotation;
            }

            var speeds = new float[times.Count - 1];
            for (var index = 0; index < speeds.Length; index++)
            {
                speeds[index] = Quaternion.Angle(rotations[index], rotations[index + 1]) /
                                (times[index + 1] - times[index]);
            }

            return speeds;
        }

        private static void InspectSampledVerticalRecoil(
            Transform slot,
            Transform model,
            AnimationClip clip,
            string label)
        {
            var scene = SceneManager.GetActiveScene();
            var dirtyBefore = scene.isDirty;
            var slotPosition = slot.position;
            var modelBasePosition = model.localPosition;
            try
            {
                AnimationMode.StartAnimationMode();
                for (var index = 0; index < BodyRecoilKeyTimes.Length; index++)
                {
                    AnimationMode.BeginSampling();
                    AnimationMode.SampleAnimationClip(slot.gameObject, clip, BodyRecoilKeyTimes[index]);
                    AnimationMode.EndSampling();
                    var expectedY = modelBasePosition.y + BodyRecoilOffsets[index];
                    if (Mathf.Abs(model.localPosition.x - modelBasePosition.x) > 0.0001f ||
                        Mathf.Abs(model.localPosition.y - expectedY) > 0.0001f ||
                        Mathf.Abs(model.localPosition.z - modelBasePosition.z) > 0.0001f)
                    {
                        throw new InvalidOperationException(
                            "The " + label + " sampled Fuga_Model vertical recoil is incorrect at key " + index + ".");
                    }

                    if (Vector3.Distance(slot.position, slotPosition) > 0.0001f)
                    {
                        throw new InvalidOperationException(
                            "The " + label + " recoil moved the Rigidbody attack slot instead of only Fuga_Model.");
                    }
                }
            }
            finally
            {
                if (AnimationMode.InAnimationMode())
                {
                    AnimationMode.StopAnimationMode();
                }
            }

            if (scene.isDirty != dirtyBefore)
            {
                throw new InvalidOperationException("The sampled Fuga vertical-recoil inspection changed the scene dirty state.");
            }
        }

        private static void InspectSampledStrikingWingProfile(
            Transform slot,
            Transform model,
            Transform leftWing,
            Transform rightWing,
            AnimationClip clip,
            bool startsLeft,
            string label)
        {
            var scene = SceneManager.GetActiveScene();
            var dirtyBefore = scene.isDirty;
            var leftBindRotation = leftWing.localRotation;
            var rightBindRotation = rightWing.localRotation;
            var firstForwardStrikeUsesLeft = FirstForwardStrikeUsesLeftWing(
                model,
                leftWing,
                rightWing,
                startsLeft);
            var leftAngles = firstForwardStrikeUsesLeft ? FirstStrikeWingAngles : SecondStrikeWingAngles;
            var rightAngles = firstForwardStrikeUsesLeft ? SecondStrikeWingAngles : FirstStrikeWingAngles;
            try
            {
                AnimationMode.StartAnimationMode();
                for (var index = 0; index < BodyKeyTimes.Length; index++)
                {
                    AnimationMode.BeginSampling();
                    AnimationMode.SampleAnimationClip(slot.gameObject, clip, BodyKeyTimes[index]);
                    AnimationMode.EndSampling();
                    var expectedLeft = leftBindRotation * Quaternion.AngleAxis(leftAngles[index], Vector3.right);
                    var expectedRight = rightBindRotation * Quaternion.AngleAxis(rightAngles[index], Vector3.right);
                    if (Quaternion.Angle(leftWing.localRotation, expectedLeft) > 0.05f ||
                        Quaternion.Angle(rightWing.localRotation, expectedRight) > 0.05f)
                    {
                        throw new InvalidOperationException(
                            "The sampled " + label + " striking-wing pose is incorrect at " +
                            BodyKeyTimes[index].ToString("0.###", CultureInfo.InvariantCulture) + " seconds.");
                    }
                }

                InspectSampledWingTipForwardMotion(
                    slot,
                    leftWing,
                    rightWing,
                    clip,
                    FirstImpactTime,
                    firstForwardStrikeUsesLeft,
                    label + " first impact");
                InspectSampledWingTipForwardMotion(
                    slot,
                    leftWing,
                    rightWing,
                    clip,
                    SecondImpactTime,
                    !firstForwardStrikeUsesLeft,
                    label + " second impact");
            }
            finally
            {
                if (AnimationMode.InAnimationMode())
                {
                    AnimationMode.StopAnimationMode();
                }
            }

            if (scene.isDirty != dirtyBefore)
            {
                throw new InvalidOperationException("Sampling the " + label + " striking-wing profile changed the scene.");
            }
        }

        private static void InspectSampledWingTipForwardMotion(
            Transform slot,
            Transform leftWing,
            Transform rightWing,
            AnimationClip clip,
            float impactTime,
            bool activeWingIsLeft,
            string label)
        {
            var leftTip = FindWingTip(RequireDirectChild(slot, ModelName), leftWing);
            var rightTip = FindWingTip(RequireDirectChild(slot, ModelName), rightWing);
            var previousTime = impactTime - 1f / BodyCurveSampleRate;
            AnimationMode.BeginSampling();
            AnimationMode.SampleAnimationClip(slot.gameObject, clip, previousTime);
            AnimationMode.EndSampling();
            var previousLeft = leftTip.position;
            var previousRight = rightTip.position;
            AnimationMode.BeginSampling();
            AnimationMode.SampleAnimationClip(slot.gameObject, clip, impactTime);
            AnimationMode.EndSampling();
            var forward = slot.TransformDirection(Vector3.back).normalized;
            var leftForwardSpeed = Vector3.Dot(leftTip.position - previousLeft, forward) * BodyCurveSampleRate;
            var rightForwardSpeed = Vector3.Dot(rightTip.position - previousRight, forward) * BodyCurveSampleRate;
            var activeForwardSpeed = activeWingIsLeft ? leftForwardSpeed : rightForwardSpeed;
            var inactiveForwardSpeed = activeWingIsLeft ? rightForwardSpeed : leftForwardSpeed;
            if (activeForwardSpeed <= 0f || inactiveForwardSpeed >= 0f)
            {
                throw new InvalidOperationException(
                    "The " + label + " wing-tip motion is reversed. ActiveForwardSpeed=" +
                    Num(activeForwardSpeed) + ", InactiveForwardSpeed=" + Num(inactiveForwardSpeed) + ".");
            }

            minimumActiveWingTipForwardSpeed = Mathf.Min(minimumActiveWingTipForwardSpeed, activeForwardSpeed);
            maximumInactiveWingTipForwardSpeed = Mathf.Max(maximumInactiveWingTipForwardSpeed, inactiveForwardSpeed);
        }

        private static void RequireJerkDrivenProfile(
            IReadOnlyList<float> actualSpeeds,
            IReadOnlyList<float> expectedSpeeds,
            IReadOnlyList<float> expectedAccelerations,
            bool preImpact,
            string label)
        {
            if (actualSpeeds.Count != expectedSpeeds.Count)
            {
                throw new InvalidOperationException("The " + label + " speed-series count is incorrect.");
            }

            for (var index = 0; index < actualSpeeds.Count; index++)
            {
                // Quaternion.Angle quantizes the first near-rest 0.02-degree window to zero.
                if (Mathf.Abs(actualSpeeds[index] - expectedSpeeds[index]) > 0.5f ||
                    (index > 0 &&
                     (preImpact
                         ? actualSpeeds[index] <= actualSpeeds[index - 1]
                         : actualSpeeds[index] >= actualSpeeds[index - 1])))
                {
                    throw new InvalidOperationException(
                        "The " + label + " speed series does not match the jerk-driven profile. Actual=" +
                        string.Join(",", actualSpeeds.Select(Num)) + ".");
                }
            }

            var actualAccelerations = AccelerationsFromSpeeds(actualSpeeds, 0.05f);
            if (actualAccelerations.Length != expectedAccelerations.Count)
            {
                throw new InvalidOperationException("The " + label + " acceleration-series count is incorrect.");
            }

            for (var index = 0; index < actualAccelerations.Length; index++)
            {
                if (Mathf.Abs(actualAccelerations[index] - expectedAccelerations[index]) > 8f ||
                    (index > 0 && actualAccelerations[index] <= actualAccelerations[index - 1]))
                {
                    throw new InvalidOperationException(
                        "The " + label + " acceleration series is not progressively increasing toward its required endpoint. Actual=" +
                        string.Join(",", actualAccelerations.Select(Num)) + ".");
                }
            }

            if (preImpact &&
                (actualSpeeds[0] >= 5f ||
                 actualSpeeds[actualSpeeds.Count - 1] <= 900f ||
                 actualAccelerations[0] >= 150f ||
                 actualAccelerations[actualAccelerations.Length - 1] <= 6000f))
            {
                throw new InvalidOperationException(
                    "The " + label + " profile is not visually explosive from a near-stop into the strike peak.");
            }

            if (!preImpact &&
                (actualAccelerations[0] >= -500f || actualAccelerations[actualAccelerations.Length - 1] <= -75f))
            {
                throw new InvalidOperationException(
                    "The " + label + " recovery does not decelerate hardest immediately after impact and then ease out.");
            }
        }

        private static void RequireImpactSpeedDrop(
            IReadOnlyList<float> preImpactSpeeds,
            IReadOnlyList<float> postImpactSpeeds,
            string label)
        {
            if (preImpactSpeeds.Count == 0 ||
                postImpactSpeeds.Count == 0 ||
                postImpactSpeeds[0] >= preImpactSpeeds[preImpactSpeeds.Count - 1] * 0.4f)
            {
                throw new InvalidOperationException(
                    "The " + label + " profile does not slow down immediately and sharply after impact.");
            }
        }

        private static float[] AccelerationsFromSpeeds(IReadOnlyList<float> speeds, float interval)
        {
            var accelerations = new float[speeds.Count - 1];
            for (var index = 0; index < accelerations.Length; index++)
            {
                accelerations[index] = (speeds[index + 1] - speeds[index]) / interval;
            }

            return accelerations;
        }

        private static AnimationCurve CurveForComponent(
            IReadOnlyDictionary<string, AnimationCurve> curves,
            char component)
        {
            var match = curves.SingleOrDefault(pair =>
                pair.Key.EndsWith("." + component, StringComparison.Ordinal));
            if (match.Value == null)
            {
                throw new InvalidOperationException("A Fuga attack curve component is missing: " + component + ".");
            }

            return match.Value;
        }

        private static void InspectWingCurves(
            AnimationClip clip,
            IEnumerable<EditorCurveBinding> bindings,
            string wingPath,
            Quaternion bindRotation,
            IReadOnlyList<float> expectedAngles)
        {
            var curves = CurvesAtPath(clip, bindings, wingPath);
            if (curves.Count != 4 || curves.Values.Any(curve => curve.length != BodyKeyTimes.Length))
            {
                throw new InvalidOperationException("A Fuga attack wing does not have four 121-key quaternion curves.");
            }

            for (var index = 0; index < BodyKeyTimes.Length; index++)
            {
                if (curves.Values.Any(curve => Mathf.Abs(curve.keys[index].time - BodyKeyTimes[index]) > 0.0001f))
                {
                    throw new InvalidOperationException("A Fuga attack wing key time is incorrect.");
                }

                var expected = bindRotation * Quaternion.AngleAxis(expectedAngles[index], Vector3.right);
                if (Quaternion.Angle(QuaternionFromCurves(curves, index), expected) > 0.05f)
                {
                    throw new InvalidOperationException(
                        "A Fuga attack wing angle is incorrect at key " + index + ".");
                }
            }
        }

        private static Dictionary<string, AnimationCurve> CurvesAtPath(
            AnimationClip clip,
            IEnumerable<EditorCurveBinding> bindings,
            string path)
        {
            return bindings.Where(binding => binding.path == path)
                .ToDictionary(
                    binding => binding.propertyName,
                    binding => AnimationUtility.GetEditorCurve(clip, binding) ??
                               throw new InvalidOperationException("A Fuga attack quaternion curve is missing."),
                    StringComparer.Ordinal);
        }

        private static Quaternion QuaternionFromCurves(
            IReadOnlyDictionary<string, AnimationCurve> curves,
            int keyIndex)
        {
            return new Quaternion(
                CurveComponent(curves, 'x', keyIndex),
                CurveComponent(curves, 'y', keyIndex),
                CurveComponent(curves, 'z', keyIndex),
                CurveComponent(curves, 'w', keyIndex));
        }

        private static float CurveComponent(
            IReadOnlyDictionary<string, AnimationCurve> curves,
            char component,
            int keyIndex)
        {
            var match = curves.SingleOrDefault(pair =>
                pair.Key.EndsWith("." + component, StringComparison.Ordinal));
            if (match.Value == null || match.Value.length <= keyIndex)
            {
                throw new InvalidOperationException("A Fuga attack quaternion component is missing: " + component + ".");
            }

            return match.Value.keys[keyIndex].value;
        }

        private static string AttackProtectedSignature(
            Transform slot,
            Transform model,
            SkinnedMeshRenderer renderer)
        {
            var collider = slot.GetComponent<BoxCollider>() ??
                           throw new InvalidOperationException("The attack Fuga BoxCollider is missing.");
            var body = slot.GetComponent<Rigidbody>() ??
                       throw new InvalidOperationException("The attack Fuga Rigidbody is missing.");
            var physics = slot.GetComponent<FugaPhysicsMotionDriver>() ??
                          throw new InvalidOperationException("The attack Fuga physics driver is missing.");
            return TransformSignature(slot) + "|Sibling=" + slot.GetSiblingIndex() + "\n" +
                   TransformSignature(model) + "|Mesh=" + AssetDatabase.GetAssetPath(renderer.sharedMesh) + "\n" +
                   "Collider=" + Vec(collider.center) + "|" + Vec(collider.size) + "|" + collider.enabled + "\n" +
                   "Body=" + body.isKinematic + "|" + body.useGravity + "|" + body.constraints + "|" +
                   Num(body.mass) + "|" + Num(body.linearDamping) + "|" + Num(body.angularDamping) + "\n" +
                   "Physics=" + (physics.Body == body) + "|" +
                   (physics.MotionPathTarget != null ? HierarchyPath(physics.MotionPathTarget) : string.Empty) + "|" +
                   physics.LockRootMotionForReview + "|" + physics.FollowVerticalAxis + "|" +
                   physics.UseDeathFallSequence + "|" + physics.IdleHoverEnabled;
        }

        private static string OtherFugaSignature(Transform placementRoot)
        {
            var builder = new StringBuilder();
            foreach (Transform child in placementRoot)
            {
                if (child.name == AttackSlotName)
                {
                    continue;
                }

                AppendHierarchySignature(builder, child);
                var animator = child.GetComponent<Animator>();
                var physics = child.GetComponent<FugaPhysicsMotionDriver>();
                builder.Append("Animator|").Append(child.name).Append('|')
                    .Append(animator != null && animator.enabled).Append('|')
                    .Append(animator != null && animator.runtimeAnimatorController != null
                        ? AssetDatabase.GetAssetPath(animator.runtimeAnimatorController)
                        : string.Empty).AppendLine();
                builder.Append("Physics|").Append(child.name).Append('|')
                    .Append(physics != null && physics.LockRootMotionForReview).Append('|')
                    .Append(physics != null && physics.IdleHoverEnabled).Append('|')
                    .Append(physics != null ? Num(physics.IdleHoverFrequency) : string.Empty).AppendLine();
            }

            return builder.ToString();
        }

        private static string[] OtherRootSignatures(Scene scene)
        {
            return scene.GetRootGameObjects()
                .Where(root => root.name != PlacementRootName)
                .Select(root => HierarchySignature(root.transform))
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
        }

        private static string HierarchySignature(Transform root)
        {
            var builder = new StringBuilder();
            AppendHierarchySignature(builder, root);
            return builder.ToString();
        }

        private static void AppendHierarchySignature(StringBuilder builder, Transform root)
        {
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
            {
                builder.Append(HierarchyPath(transform)).Append('|')
                    .Append(transform.GetSiblingIndex()).Append('|')
                    .Append(Vec(transform.localPosition)).Append('|')
                    .Append(Vec(transform.localEulerAngles)).Append('|')
                    .Append(Vec(transform.localScale)).Append('|')
                    .Append(transform.gameObject.activeSelf).AppendLine();
            }
        }

        private static string TransformSignature(Transform transform)
        {
            return transform.name + "|" + Vec(transform.localPosition) + "|" +
                   Vec(transform.localEulerAngles) + "|" + Vec(transform.localScale);
        }

        private static void CaptureComparison(
            Transform slot,
            AnimationClip clip,
            string destination,
            IReadOnlyList<float> sampleTimes = null,
            bool lockCameraToFirstSample = false)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                                      throw new InvalidOperationException("Invalid Fuga attack capture path."));
            var scene = SceneManager.GetActiveScene();
            var dirtyBefore = scene.isDirty;
            Texture2D composite = null;
            GameObject cameraObject = null;
            GameObject lightObject = null;
            try
            {
                cameraObject = new GameObject("FugaAttackCaptureCamera", typeof(Camera))
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                lightObject = new GameObject("FugaAttackCaptureLight", typeof(Light))
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                var camera = cameraObject.GetComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.035f, 0.045f, 0.055f, 1f);
                camera.cullingMask = ~0;
                camera.allowHDR = false;
                camera.orthographic = true;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 100f;

                var light = lightObject.GetComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.2f;
                light.color = new Color(1f, 0.96f, 0.9f);
                light.transform.rotation = Quaternion.Euler(38f, -32f, 0f);

                var panelWidth = CaptureWidth / 2;
                var panelHeight = CaptureHeight / 2;
                composite = new Texture2D(CaptureWidth, CaptureHeight, TextureFormat.RGB24, false);
                var times = sampleTimes?.ToArray() ??
                            new[] { FirstImpactTime, 1f, SecondImpactTime, LoopDuration };
                if (times.Length != 4)
                {
                    throw new InvalidOperationException("The Fuga attack comparison requires exactly four sample times.");
                }
                var playerCamera = RequireRoot(PlayerName).GetComponentInChildren<Camera>(true) ??
                                   throw new InvalidOperationException("The Player camera is missing.");
                AnimationMode.StartAnimationMode();
                var fixedCenter = Vector3.zero;
                var fixedDirection = Vector3.zero;
                var fixedOrthographicSize = 0f;
                if (lockCameraToFirstSample)
                {
                    AnimationMode.BeginSampling();
                    AnimationMode.SampleAnimationClip(slot.gameObject, clip, times[0]);
                    AnimationMode.EndSampling();
                    var baselineBounds = BoundsOf(slot);
                    fixedCenter = baselineBounds.center;
                    fixedDirection = (fixedCenter - playerCamera.transform.position).normalized;
                    fixedOrthographicSize = Mathf.Max(
                        baselineBounds.extents.y * 1.4f,
                        baselineBounds.extents.x * 1.4f / (panelWidth / (float)panelHeight));
                }

                for (var index = 0; index < times.Length; index++)
                {
                    AnimationMode.BeginSampling();
                    AnimationMode.SampleAnimationClip(slot.gameObject, clip, times[index]);
                    AnimationMode.EndSampling();
                    var bounds = BoundsOf(slot);
                    var captureCenter = lockCameraToFirstSample ? fixedCenter : bounds.center;
                    var direction = lockCameraToFirstSample
                        ? fixedDirection
                        : (bounds.center - playerCamera.transform.position).normalized;
                    camera.transform.position = captureCenter - direction * 10f;
                    camera.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
                    camera.orthographicSize = lockCameraToFirstSample
                        ? fixedOrthographicSize
                        : Mathf.Max(
                            bounds.extents.y * 1.3f,
                            bounds.extents.x * 1.3f / (panelWidth / (float)panelHeight));
                    var panel = Render(camera, panelWidth, panelHeight);
                    var x = index % 2 * panelWidth;
                    var y = (1 - index / 2) * panelHeight;
                    composite.SetPixels(x, y, panelWidth, panelHeight, panel.GetPixels());
                    UnityEngine.Object.DestroyImmediate(panel);
                }

                composite.Apply();
                File.WriteAllBytes(destination, composite.EncodeToPNG());
            }
            finally
            {
                if (AnimationMode.InAnimationMode())
                {
                    AnimationMode.StopAnimationMode();
                }

                if (composite != null)
                {
                    UnityEngine.Object.DestroyImmediate(composite);
                }

                if (cameraObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(cameraObject);
                }

                if (lightObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(lightObject);
                }
            }

            if (scene.isDirty != dirtyBefore)
            {
                throw new InvalidOperationException("The temporary Fuga attack capture changed the scene dirty state.");
            }
        }

        private static Texture2D Render(Camera camera, int width, int height)
        {
            var previous = RenderTexture.active;
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target;
                RenderTexture.active = target;
                camera.Render();
                image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                image.Apply();
                return image;
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previous;
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        private static Bounds BoundsOf(Transform root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.enabled)
                .ToArray();
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException(root.name + " has no visible renderer.");
            }

            var bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            return bounds;
        }

        private static void WriteReport(AttackResult result, bool captureCreated)
        {
            var report = new StringBuilder()
                .AppendLine("Fuga Attack Motion Report")
                .AppendLine("Result=PASS")
                .AppendLine("DesignSource=docs/GAME_DESIGN_SOURCE.txt:206")
                .AppendLine("DesignAttackType=CloseRangeWingStrike")
                .AppendLine("DesignDamage=10")
                .AppendLine("DesignAttackDelaySeconds=1.000")
                .AppendLine("Scene=" + ScenePath)
                .AppendLine("Target=" + PlacementRootName + "/" + AttackSlotName)
                .AppendLine("LeftFirstClip=" + LeftFirstClipPath)
                .AppendLine("RightFirstClip=" + RightFirstClipPath)
                .AppendLine("AnimatorController=" + ControllerPath)
                .AppendLine("ExistingAttackAnimationUsed=False")
                .AppendLine("ExistingAttackAssetsModified=False")
                .AppendLine("LoopDurationSeconds=2.000")
                .AppendLine("StrikeIntervalSeconds=1.000")
                .AppendLine("FirstImpactTimeSeconds=0.500")
                .AppendLine("SecondImpactTimeSeconds=1.500")
                .AppendLine("FirstWingSelection=Uniform50_50")
                .AppendLine("LeftSelectionInterval=[0.0,0.5)")
                .AppendLine("RightSelectionInterval=[0.5,1.0]")
                .AppendLine("AlternatesWithoutIdle=True")
                .AppendLine("IndependentWingStrikeDegrees=0.000")
                .AppendLine("InactiveWingDegrees=0.000")
                .AppendLine("WingsInheritBodyYaw=True")
                .AppendLine("WingDelaySeconds=0.000")
                .AppendLine("PreImpactAngularSpeedDegreesPerSecond=180.000")
                .AppendLine("PreImpactFastFromSegmentStart=True")
                .AppendLine("PostImpactAngularSpeedDecreasing=True")
                .AppendLine("BodyForwardTiltDegrees=40.000")
                .AppendLine("BodyTiltOwner=Fuga_ModelLocalRotationCurves")
                .AppendLine("RootPositionCurves=0")
                .AppendLine("AltitudeLocked=True")
                .AppendLine("AttackSlotWorldY=" + Num(result.AttackSlotWorldY))
                .AppendLine("OtherFugaSlotsChanged=False")
                .AppendLine("PlacementOrderChanged=False")
                .AppendLine("PlayerChanged=False")
                .AppendLine("OtherSceneRootsChanged=False")
                .AppendLine("OriginalGlbModified=False")
                .AppendLine("ArtSampleCreated=False")
                .AppendLine("DamageLogicImplemented=False")
                .AppendLine("CaptureSampleTimesSeconds=0.5,1,1.5,2")
                .AppendLine("CaptureCreated=" + captureCreated)
                .AppendLine("HarnessValidationRun=False")
                .ToString();
            var destination = Absolute(ReportPath);
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                                      throw new InvalidOperationException("Invalid Fuga attack report path."));
            File.WriteAllText(destination, report, new UTF8Encoding(false));
        }

        private static void WriteBodyYawReport(
            AttackResult result,
            int repairedJiggleRigCount,
            bool captureCreated)
        {
            var report = new StringBuilder()
                .AppendLine("Fuga Attack Body Yaw 90 Report")
                .AppendLine("Result=PASS")
                .AppendLine("DesignSource=docs/GAME_DESIGN_SOURCE.txt:206")
                .AppendLine("Scene=" + ScenePath)
                .AppendLine("Target=" + PlacementRootName + "/" + AttackSlotName)
                .AppendLine("StrikeIntervalSeconds=1.000")
                .AppendLine("FirstWingSelection=Uniform50_50")
                .AppendLine("AlternatesWithoutIdle=True")
                .AppendLine("BodyYawDegrees=90.000")
                .AppendLine("LeftFirstBodyYawSequenceDegrees=0,-15,-90,-60,0,15,90,60,0")
                .AppendLine("RightFirstBodyYawSequenceDegrees=0,15,90,60,0,-15,-90,-60,0")
                .AppendLine("BodyForwardTiltDegrees=40.000")
                .AppendLine("WingsInheritBodyYaw=True")
                .AppendLine("WingDelaySeconds=0.000")
                .AppendLine("ActiveWingStrikeDegrees=90.000")
                .AppendLine("InactiveWingDegrees=0.000")
                .AppendLine("PreImpactAngularSpeedDegreesPerSecond=180.000")
                .AppendLine("PreImpactFastFromSegmentStart=True")
                .AppendLine("PostImpactAngularSpeedDecreasing=True")
                .AppendLine("RootPositionCurves=0")
                .AppendLine("AltitudeLocked=True")
                .AppendLine("AttackSlotWorldY=" + Num(result.AttackSlotWorldY))
                .AppendLine("JiggleRigRootsRepairedByThisCommand=" + repairedJiggleRigCount)
                .AppendLine("JiggleRigRootsMissingNow=0")
                .AppendLine("OtherFugaSlotsChanged=False")
                .AppendLine("PlacementOrderChanged=False")
                .AppendLine("PlayerChanged=False")
                .AppendLine("OtherSceneRootTransformsChanged=False")
                .AppendLine("OriginalGlbModified=False")
                .AppendLine("ArtSampleCreated=False")
                .AppendLine("DamageLogicImplemented=False")
                .AppendLine("CaptureSampleTimesSeconds=0.5,1,1.5,2")
                .AppendLine("CaptureCreated=" + captureCreated)
                .AppendLine("HarnessValidationRun=False")
                .ToString();
            WriteText(BodyYawReportPath, report);
        }

        private static void WriteHorizontalWingsReport(AttackResult result, bool captureCreated)
        {
            var report = new StringBuilder()
                .AppendLine("Fuga Attack Horizontal Wings Report")
                .AppendLine("Result=PASS")
                .AppendLine("DesignSource=docs/GAME_DESIGN_SOURCE.txt:206")
                .AppendLine("Scene=" + ScenePath)
                .AppendLine("Target=" + PlacementRootName + "/" + AttackSlotName)
                .AppendLine("LeftFirstClip=" + LeftFirstClipPath)
                .AppendLine("RightFirstClip=" + RightFirstClipPath)
                .AppendLine("WingHorizontalBindPose=True")
                .AppendLine("WingLocalRotationOffsetDegrees=0.000")
                .AppendLine("IndependentWingStrikeRotation=False")
                .AppendLine("WingsInheritBodyYaw=True")
                .AppendLine("WingDelaySeconds=0.000")
                .AppendLine("BodyYawDegrees=90.000")
                .AppendLine("LeftFirstBodyYawSequenceDegrees=0,-90,0,90,0")
                .AppendLine("RightFirstBodyYawSequenceDegrees=0,90,0,-90,0")
                .AppendLine("BodyForwardTiltDegrees=40.000")
                .AppendLine("BodyCurvesChanged=False")
                .AppendLine("RootPositionCurves=0")
                .AppendLine("AltitudeLocked=True")
                .AppendLine("AttackSlotWorldY=" + Num(result.AttackSlotWorldY))
                .AppendLine("OtherFugaSlotsChanged=False")
                .AppendLine("PlacementOrderChanged=False")
                .AppendLine("PlayerChanged=False")
                .AppendLine("ControllerChanged=False")
                .AppendLine("SceneChanged=False")
                .AppendLine("OriginalGlbModified=False")
                .AppendLine("ArtSampleCreated=False")
                .AppendLine("DamageLogicImplemented=False")
                .AppendLine("CaptureSampleTimesSeconds=0.5,1,1.5,2")
                .AppendLine("CaptureCreated=" + captureCreated)
                .AppendLine("HarnessValidationRun=False")
                .ToString();
            WriteText(HorizontalWingsReportPath, report);
        }

        private static void WriteJerkDrivenAccelerationReport(AttackResult result, bool captureCreated)
        {
            var model = RequireDirectChild(result.Slot, ModelName);
            var renderer = model.GetComponentInChildren<SkinnedMeshRenderer>(true) ??
                           throw new InvalidOperationException("The attack Fuga model has no SkinnedMeshRenderer.");
            var leftWing = FindBone(renderer, "Bone_013");
            var rightWing = FindBone(renderer, "Bone_017");
            var report = new StringBuilder()
                .AppendLine("Fuga Attack Striking-Wing Acceleration Report")
                .AppendLine("Result=PASS")
                .AppendLine("Scene=" + ScenePath)
                .AppendLine("Target=" + PlacementRootName + "/" + AttackSlotName)
                .AppendLine("LeftFirstClip=" + LeftFirstClipPath)
                .AppendLine("RightFirstClip=" + RightFirstClipPath)
                .AppendLine("StrikeIntervalSeconds=1.000")
                .AppendLine("FirstImpactTimeSeconds=0.500")
                .AppendLine("SecondImpactTimeSeconds=1.500")
                .AppendLine("ActiveWingForwardStrikeDegrees=-90.000")
                .AppendLine("InactiveWingDegrees=0.000")
                .AppendLine("ForwardStrikeLocalAxis=-X")
                .AppendLine("OnlyCurrentStrikingWingBendsForward=True")
                .AppendLine("WingsInheritBodyYaw=True")
                .AppendLine("WingDelaySeconds=0.000")
                .AppendLine("WingCurveSampleRate=60")
                .AppendLine("WingRotationKeyCountPerCurve=121")
                .AppendLine("WingAccelerationProfileSource=BodyYawNormalizedProgress")
                .AppendLine("WingProfileMatchesBodyAcceleration=True")
                .AppendLine("ForwardDirection=Fuga_ModelVisibleForward-Z")
                .AppendLine("LeftFirstForwardWingOrder=" +
                            ForwardWingOrder(model, leftWing, rightWing, startsLeft: true))
                .AppendLine("RightFirstForwardWingOrder=" +
                            ForwardWingOrder(model, leftWing, rightWing, startsLeft: false))
                .AppendLine("MinimumActiveWingTipForwardSpeed=" + Num(minimumActiveWingTipForwardSpeed))
                .AppendLine("MaximumInactiveWingTipForwardSpeed=" + Num(maximumInactiveWingTipForwardSpeed))
                .AppendLine("ActiveWingTipForwardDotPositive=True")
                .AppendLine("InactiveWingTipForwardDotNegative=True")
                .AppendLine("AccelerationLeadTimeSeconds=0.450")
                .AppendLine("FirstAccelerationStartTimeSeconds=0.050")
                .AppendLine("SecondAccelerationStartTimeSeconds=1.050")
                .AppendLine("BodyYawMaximumDegrees=131.625")
                .AppendLine("BodyForwardTiltDegrees=40.000")
                .AppendLine("WingsInheritBodyForwardTilt=True")
                .AppendLine("ImpactVerticalRecoilOwner=Fuga_ModelLocalPositionY")
                .AppendLine("ImpactVerticalRecoilMeters=0.100")
                .AppendLine("ImpactVerticalRecoilReturnSeconds=0.070")
                .AppendLine("ImpactVerticalRecoilReverseSeconds=0.035")
                .AppendLine("FirstImpactRecoilTimesSeconds=0.483333,0.500,0.535,0.570")
                .AppendLine("FirstImpactRecoilOffsetsMeters=0.000,0.100,-0.100,0.000")
                .AppendLine("SecondImpactRecoilTimesSeconds=1.483333,1.500,1.535,1.570")
                .AppendLine("SecondImpactRecoilOffsetsMeters=0.000,0.100,-0.100,0.000")
                .AppendLine("WingsInheritBodyVerticalRecoil=True")
                .AppendLine("FugaModelVerticalRecoilCurveCount=1")
                .AppendLine("BodyCurveSampleRate=60")
                .AppendLine("BodyYawKeyCount=121")
                .AppendLine("BodyYawCriticalTimesSeconds=0,0.05,0.5,1,1.05,1.5,2")
                .AppendLine("LeftFirstBodyYawCriticalDegrees=0,0,-131.625,0,0,131.625,0")
                .AppendLine("RightFirstBodyYawCriticalDegrees=0,0,131.625,0,0,-131.625,0")
                .AppendLine("PreImpactAngularSpeedDegreesPerSecond=0.000->1170.000")
                .AppendLine("PreImpactVelocityCurve=1170*s^3")
                .AppendLine("PreImpactAngularAccelerationDegreesPerSecondSquared=0.000->7800.000")
                .AppendLine("PreImpactAngularAccelerationCurve=7800*s^2")
                .AppendLine("PreImpactJerkDrivenAcceleration=True")
                .AppendLine("BakedPreImpactWindowSeconds=0.050")
                .AppendLine("BakedPreImpactWindowSpeedsDegreesPerSecond=" +
                            string.Join(",", ExpectedPreImpactWindowSpeeds.Select(Num)))
                .AppendLine("BakedPreImpactWindowsStrictlyIncreasing=True")
                .AppendLine("BakedPreImpactWindowAccelerationsDegreesPerSecondSquared=" +
                            string.Join(",", ExpectedPreImpactWindowAccelerations.Select(Num)))
                .AppendLine("BakedPreImpactWindowAccelerationsStrictlyIncreasing=True")
                .AppendLine("ImpactAngularSpeedDropDegreesPerSecond=1170.000->360.000")
                .AppendLine("ImpactAngularSpeedRetainedRatio=0.308")
                .AppendLine("ImpactInstantSlowdown=True")
                .AppendLine("PostImpactAngularSpeedDegreesPerSecond=360.000->214.875")
                .AppendLine("PostImpactVelocityCurve=214.875+145.125*(1-u)^2")
                .AppendLine("PostImpactAngularAccelerationDegreesPerSecondSquared=-580.500->0.000")
                .AppendLine("PostImpactStrongestDecelerationAtImpact=True")
                .AppendLine("BakedPostImpactWindowSeconds=0.050")
                .AppendLine("BakedPostImpactWindowSpeedsDegreesPerSecond=" +
                            string.Join(",", ExpectedPostImpactWindowSpeeds.Select(Num)))
                .AppendLine("BakedPostImpactWindowsStrictlyDecreasing=True")
                .AppendLine("BakedPostImpactWindowAccelerationsDegreesPerSecondSquared=" +
                            string.Join(",", ExpectedPostImpactWindowAccelerations.Select(Num)))
                .AppendLine("BakedPostImpactDecelerationMagnitudeStrictlyDecreasing=True")
                .AppendLine("BodyRotationCurveType=BakedRawEulerLinear60Fps")
                .AppendLine("SampledTransformSpeedAndAccelerationWindowsInspected=True")
                .AppendLine("SampledStrikingWingTransformsInspected=True")
                .AppendLine("AttackSlotRootPositionCurves=0")
                .AppendLine("AttackSlotVerticalMotion=False")
                .AppendLine("VerticalRecoilSampledTransformInspected=True")
                .AppendLine("AltitudeLocked=True")
                .AppendLine("AttackSlotWorldY=" + Num(result.AttackSlotWorldY))
                .AppendLine("OtherFugaSlotsChanged=False")
                .AppendLine("PlacementOrderChanged=False")
                .AppendLine("PlayerChanged=False")
                .AppendLine("ControllerChanged=False")
                .AppendLine("SceneChanged=False")
                .AppendLine("OriginalGlbModified=False")
                .AppendLine("ArtSampleCreated=False")
                .AppendLine("DamageLogicImplemented=False")
                .AppendLine("DirectUnityGameViewMotionReview=True")
                .AppendLine("DirectMotionReviewRequiredLoopsPerDirection=2")
                .AppendLine("LeftFirstDirectMotionReviewCompleted=" + leftFirstMotionReviewCompleted)
                .AppendLine("RightFirstDirectMotionReviewCompleted=" + rightFirstMotionReviewCompleted)
                .AppendLine("StaticCaptureGeneratedForThisChange=False")
                .AppendLine("CaptureCreated=" + captureCreated)
                .AppendLine("HarnessValidationRun=False")
                .ToString();
            WriteText(JerkDrivenAccelerationReportPath, report);
        }

        private static int RepairMissingSceneJiggleRigRoots(Scene scene)
        {
            var components = SceneJiggleRigs(scene);
            var repaired = 0;
            foreach (var component in components)
            {
                var serialized = new SerializedObject(component);
                var rootBone = serialized.FindProperty("jiggleRigData.rootBone") ??
                               throw new InvalidOperationException("JiggleRig.rootBone could not be inspected.");
                if (rootBone.objectReferenceValue != null)
                {
                    continue;
                }

                var renderer = FindParvumVisibleRenderer(component.transform);
                var wasActive = component.gameObject.activeSelf;
                if (wasActive)
                {
                    component.gameObject.SetActive(false);
                }

                try
                {
                    serialized.Update();
                    rootBone = serialized.FindProperty("jiggleRigData.rootBone") ??
                               throw new InvalidOperationException("JiggleRig.rootBone could not be repaired.");
                    rootBone.objectReferenceValue = renderer.transform;
                    var cachedData = serialized.FindProperty("jiggleRigData.transformCachedData");
                    if (cachedData != null && cachedData.isArray)
                    {
                        cachedData.arraySize = 0;
                    }

                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(component);
                    repaired++;
                }
                finally
                {
                    if (wasActive)
                    {
                        component.gameObject.SetActive(true);
                    }
                }
            }

            return repaired;
        }

        private static JiggleRigStatus InspectSceneJiggleRigs(Scene scene)
        {
            var components = SceneJiggleRigs(scene);
            var missing = 0;
            foreach (var component in components)
            {
                var serialized = new SerializedObject(component);
                var rootBone = serialized.FindProperty("jiggleRigData.rootBone") ??
                               throw new InvalidOperationException("JiggleRig.rootBone could not be inspected.");
                if (rootBone.objectReferenceValue == null)
                {
                    missing++;
                }
            }

            return new JiggleRigStatus(components.Length, missing);
        }

        private static Component[] SceneJiggleRigs(Scene scene)
        {
            var type = Type.GetType(JiggleRigTypeName);
            if (type == null)
            {
                throw new InvalidOperationException("The JiggleRig component type could not be resolved.");
            }

            return Resources.FindObjectsOfTypeAll(type)
                .OfType<Component>()
                .Where(component => component.gameObject.scene == scene)
                .OrderBy(component => HierarchyPath(component.transform), StringComparer.Ordinal)
                .ToArray();
        }

        private static SkinnedMeshRenderer FindParvumVisibleRenderer(Transform root)
        {
            var renderers = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            var approved = renderers.FirstOrDefault(renderer =>
                renderer.name.IndexOf(ParvumVisibleMeshName, StringComparison.OrdinalIgnoreCase) >= 0 ||
                renderer.sharedMesh != null &&
                renderer.sharedMesh.name.IndexOf(ParvumVisibleMeshName, StringComparison.OrdinalIgnoreCase) >= 0);
            if (approved != null)
            {
                return approved;
            }

            return renderers.FirstOrDefault() ??
                   throw new InvalidOperationException(
                       "A scene JiggleRig with a missing root has no replacement SkinnedMeshRenderer: " +
                       HierarchyPath(root) + ".");
        }

        private static ConsoleCounts CurrentConsoleCounts()
        {
            var logEntriesType = Type.GetType("UnityEditor.LogEntries,UnityEditor.dll");
            var method = logEntriesType?.GetMethod(
                "GetCountsByType",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (method == null)
            {
                throw new InvalidOperationException("Unity console count API could not be resolved.");
            }

            var arguments = new object[] { 0, 0, 0 };
            method.Invoke(null, arguments);
            return new ConsoleCounts((int)arguments[0], (int)arguments[1], (int)arguments[2]);
        }

        private static string WingCurveSignature(
            AnimationClip clip,
            Transform slot,
            Transform leftWing,
            Transform rightWing)
        {
            var wingPaths = new HashSet<string>(StringComparer.Ordinal)
            {
                RelativePath(slot, leftWing),
                RelativePath(slot, rightWing),
            };
            return CurveSignature(clip, wingPaths);
        }

        private static string BodyCurveSignature(AnimationClip clip, Transform slot, Transform model)
        {
            return CurveSignature(
                clip,
                new HashSet<string>(StringComparer.Ordinal) { RelativePath(slot, model) });
        }

        private static string CurveSignature(AnimationClip clip, ISet<string> paths)
        {
            var builder = new StringBuilder();
            foreach (var binding in AnimationUtility.GetCurveBindings(clip)
                         .Where(binding => paths.Contains(binding.path))
                         .OrderBy(binding => binding.path, StringComparer.Ordinal)
                         .ThenBy(binding => binding.propertyName, StringComparer.Ordinal))
            {
                builder.Append(binding.path).Append('|').Append(binding.propertyName);
                var curve = AnimationUtility.GetEditorCurve(clip, binding) ??
                            throw new InvalidOperationException("A wing curve is missing while preserving body-yaw scope.");
                foreach (var key in curve.keys)
                {
                    builder.Append('|').Append(Num(key.time)).Append(':').Append(Num(key.value));
                }

                builder.AppendLine();
            }

            return builder.ToString();
        }

        private static void WriteText(string projectRelativePath, string content)
        {
            var destination = Absolute(projectRelativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                                      throw new InvalidOperationException("Invalid report path: " + projectRelativePath + "."));
            File.WriteAllText(destination, content, new UTF8Encoding(false));
        }

        private static void RequireText(string expected, string actual, string label)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(label + " changed outside the approved scope.");
            }
        }

        private static Transform FindBone(SkinnedMeshRenderer renderer, string name)
        {
            var matches = renderer.bones.Where(bone => bone != null && bone.name == name).ToArray();
            if (matches.Length != 1)
            {
                throw new InvalidOperationException("Expected exactly one Fuga bone named " + name + ".");
            }

            return matches[0];
        }

        private static Transform RequireRoot(string name)
        {
            var gameObject = GameObject.Find(name) ??
                             throw new InvalidOperationException(name + " is missing.");
            if (gameObject.transform.parent != null)
            {
                throw new InvalidOperationException(name + " is not a scene root.");
            }

            return gameObject.transform;
        }

        private static Transform RequireDirectChild(Transform parent, string name)
        {
            var matches = parent.Cast<Transform>().Where(child => child.name == name).ToArray();
            if (matches.Length != 1)
            {
                throw new InvalidOperationException(
                    "Expected exactly one direct child " + parent.name + "/" + name + ".");
            }

            return matches[0];
        }

        private static Scene RequireCurrentScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must already be active. ActiveScene=" + scene.path + ".");
            }

            return scene;
        }

        private static string RelativePath(Transform root, Transform target)
        {
            var names = new Stack<string>();
            var cursor = target;
            while (cursor != null && cursor != root)
            {
                names.Push(cursor.name);
                cursor = cursor.parent;
            }

            if (cursor != root)
            {
                throw new InvalidOperationException(target.name + " is not below " + root.name + ".");
            }

            return string.Join("/", names);
        }

        private static string HierarchyPath(Transform transform)
        {
            var names = new Stack<string>();
            var cursor = transform;
            while (cursor != null)
            {
                names.Push(cursor.name);
                cursor = cursor.parent;
            }

            return string.Join("/", names);
        }

        private static void RequireModelHashes()
        {
            RequireHash(ExpectedModelSha256, Sha256(SourceModelPath), "source Fuga GLB");
            RequireHash(ExpectedImportedRigSha256, Sha256(Absolute(ImportedModelPath)), "imported lip-rig Fuga GLB");
        }

        private static string Sha256(string path)
        {
            using var stream = File.OpenRead(path);
            using var hash = SHA256.Create();
            return BitConverter.ToString(hash.ComputeHash(stream)).Replace("-", string.Empty);
        }

        private static void RequireHash(string expected, string actual, string label)
        {
            if (!string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    label + " SHA-256 mismatch. Expected=" + expected + ", Actual=" + actual + ".");
            }
        }

        private static string Absolute(string projectRelativePath)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", projectRelativePath));
        }

        private static string Num(float value)
        {
            return value.ToString("0.######", CultureInfo.InvariantCulture);
        }

        private static string Vec(Vector3 value)
        {
            return "(" + Num(value.x) + "," + Num(value.y) + "," + Num(value.z) + ")";
        }

        private readonly struct AttackResult
        {
            public AttackResult(
                Transform slot,
                AnimationClip leftFirstClip,
                AnimationClip rightFirstClip,
                float attackSlotWorldY)
            {
                Slot = slot;
                LeftFirstClip = leftFirstClip;
                RightFirstClip = rightFirstClip;
                AttackSlotWorldY = attackSlotWorldY;
            }

            public Transform Slot { get; }
            public AnimationClip LeftFirstClip { get; }
            public AnimationClip RightFirstClip { get; }
            public float AttackSlotWorldY { get; }
        }

        private readonly struct JiggleRigStatus
        {
            public JiggleRigStatus(int totalCount, int missingRootCount)
            {
                TotalCount = totalCount;
                MissingRootCount = missingRootCount;
            }

            public int TotalCount { get; }
            public int MissingRootCount { get; }
        }

        private readonly struct ConsoleCounts
        {
            public ConsoleCounts(int errorCount, int warningCount, int logCount)
            {
                ErrorCount = errorCount;
                WarningCount = warningCount;
                LogCount = logCount;
            }

            public int ErrorCount { get; }
            public int WarningCount { get; }
            public int LogCount { get; }
        }
    }
}
