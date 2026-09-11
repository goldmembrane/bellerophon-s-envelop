using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Bellerophon.PlayerAnimation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.Validation
{
    internal static class SpeakerIdleCarryTools
    {
        internal const string OutputFolder =
            "docs/validation/speaker_idle_carry_2026-09-11";
        internal const string ModelViewsPath = OutputFolder + "/model_views.png";
        internal const string DiagnosticImagePath = OutputFolder + "/diagnostic.png";
        internal const string FinalImagePath = OutputFolder + "/final.png";
        internal const string TransformReflectionOutputFolder =
            "docs/validation/speaker_idle_transform_reflection_2026-09-11_02";
        internal const string TransformReflectionDiagnosticImagePath =
            TransformReflectionOutputFolder + "/diagnostic.png";
        internal const string TransformReflectionFinalImagePath =
            TransformReflectionOutputFolder + "/final.png";
        internal const string NeutralHandleHandOutputFolder =
            "docs/validation/speaker_idle_neutral_handle_hand_2026-09-11";
        internal const string PalmRightOutputFolder =
            "docs/validation/speaker_idle_palm_right_2026-09-11";

        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string TargetName = "Speaker_Idle";
        private const string ModelAssetPath =
            "Assets/_Project/Art/Items/Speaker/PortableSpeaker.fbx";
        private const string TextureFolder =
            "Assets/_Project/Art/Items/Speaker/Textures";
        private const string MaterialFolder =
            "Assets/_Project/Art/Items/Speaker/Materials";
        private const string MaterialAssetPath = MaterialFolder + "/PortableSpeaker.mat";
        private const string ProfileFolder =
            "Assets/_Project/Art/Player/Animations/SpeakerIdle";
        private const string ProfileAssetPath = ProfileFolder + "/SpeakerIdleCarryProfile.asset";
        private const string ReferenceImagePath =
            "C:/Users/gus68/OneDrive/바탕 화면/1111.jfif";
        private const string LeftShoulderPath =
            "Armature/Hips/Spine02/Spine01/Spine/LeftShoulder";
        private const string LeftArmPath = LeftShoulderPath + "/LeftArm";
        private const string LeftForeArmPath = LeftArmPath + "/LeftForeArm";
        private const string LeftHandPath = LeftForeArmPath + "/LeftHand";
        private const float PositionTolerance = 0.00001f;
        private const float RotationTolerance = 0.05f;

        // Final recovery values read directly from the user's Unity Transform Inspector.
        private static readonly Vector3 AuthoredModelLocalPosition =
            new Vector3(0f, 0.108f, -0.223f);
        private static readonly Quaternion AuthoredModelLocalRotation =
            Quaternion.Euler(-90f, 0f, 0f);
        private static readonly Vector3 AuthoredModelLocalScale =
            new Vector3(100f, 50f, 70f);

        internal static string DiagnosticAbsolutePath => Absolute(DiagnosticImagePath);
        internal static string FinalAbsolutePath => Absolute(FinalImagePath);

        internal static void InspectPalmRightSource()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            Transform foreArm = RequirePath(target.transform, LeftForeArmPath);
            Transform hand = RequirePath(target.transform, LeftHandPath);
            SpeakerLeftShoulderFollowBehaviour behaviour =
                target.GetComponent<SpeakerLeftShoulderFollowBehaviour>() ??
                throw new InvalidOperationException("Speaker_Idle carry behaviour is missing.");
            Vector3 palmNormal = LeftPalmNormal(target.transform);
            Vector3 playerRight = target.transform.right.normalized;
            var report = new StringBuilder()
                .AppendLine("Speaker_Idle palm-right source inspection")
                .AppendLine("verificationTargetManipulated=False")
                .AppendLine("palmNormalWorld=" + Vec(palmNormal))
                .AppendLine("playerRightWorld=" + Vec(playerRight))
                .AppendLine("palmRightAngleDegrees=" +
                    Num(Vector3.Angle(palmNormal, playerRight)))
                .AppendLine("foreArmDirectionWorld=" +
                    Vec((hand.position - foreArm.position).normalized))
                .AppendLine("fingerDirectionWorld=" +
                    Vec(LeftFingerDirection(target.transform)))
                .AppendLine("wristStraightnessDegrees=" +
                    Num(LeftWristStraightness(target.transform)))
                .AppendLine("leftPalmWorldPosition=" + Vec(LeftPalmCenter(target.transform)))
                .AppendLine("handleWorldPosition=" + Vec(behaviour.HandleWorldPosition()))
                .AppendLine("sceneDirty=" + scene.isDirty);
            WritePalmRightText("source_inspection.txt", report.ToString());
            UnityConsoleDiagnostics.AssertNoErrors();
            Debug.Log("[SpeakerIdleCarry] Palm-right source inspected read-only.");
        }

        internal static void ApplyPalmRight()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            bool sceneWasDirty = scene.isDirty;
            GameObject target = FindUnique(scene, TargetName);
            Transform upperArm = RequirePath(target.transform, LeftArmPath);
            Transform foreArm = RequirePath(target.transform, LeftForeArmPath);
            Transform hand = RequirePath(target.transform, LeftHandPath);
            SpeakerLeftShoulderFollowBehaviour behaviour =
                target.GetComponent<SpeakerLeftShoulderFollowBehaviour>() ??
                throw new InvalidOperationException("Speaker_Idle carry behaviour is missing.");
            SpeakerIdleCarryProfile profile = behaviour.Profile ??
                throw new InvalidOperationException("Speaker_Idle carry profile is missing.");
            if (profile.SourceLeftArmPose == null || profile.SourceLeftArmPose.Length == 0)
                throw new InvalidOperationException("Speaker_Idle source left-arm pose is missing.");

            behaviour.RefreshPreview();
            string protectedBefore = ProtectedHierarchySignature(target.transform);
            string speakerBefore = SpeakerTransformAndAppearanceSignature(behaviour);
            string modelHashBefore = ComputeAssetHash(ModelAssetPath);
            string materialHashBefore = ComputeAssetHash(MaterialAssetPath);
            ApplyBonePose(target.transform, profile.SourceLeftArmPose);

            Vector3 playerRight = target.transform.right.normalized;
            Vector3 palmTarget = behaviour.HandleWorldPosition() + target.transform.up * 0.008f;
            Vector3 pole = palmTarget - target.transform.up * 0.34f +
                target.transform.forward * 0.08f - target.transform.right * 0.04f;
            for (int iteration = 0; iteration < 8; iteration++)
            {
                Vector3 palmError = palmTarget - LeftPalmCenter(target.transform);
                if (palmError.sqrMagnitude > 0.00000025f)
                    SolveTwoBone(
                        upperArm, foreArm, hand, hand.position + palmError, pole);
                AlignLeftPalm(foreArm, hand, target.transform, playerRight);
            }

            SpeakerBoneRotation[] palmRightPose =
            {
                new SpeakerBoneRotation(LeftArmPath, upperArm.localRotation),
                new SpeakerBoneRotation(LeftForeArmPath, foreArm.localRotation),
                new SpeakerBoneRotation(LeftHandPath, hand.localRotation)
            };
            profile.Configure(
                profile.SpeakerPrefab,
                profile.LeftShoulderPath,
                profile.PositionOffsetInShoulderSpace,
                profile.RotationOffsetFromShoulder,
                profile.HolderScale,
                profile.ModelLocalPosition,
                profile.ModelLocalRotation,
                profile.ModelLocalScale,
                profile.HandlePointInHolderSpace,
                profile.HandleAxisInHolderSpace,
                profile.SourceLeftArmPose,
                palmRightPose);
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            behaviour.Configure(profile);

            RequireStringEqual(protectedBefore, ProtectedHierarchySignature(target.transform),
                "Speaker_Idle left shoulder, torso, head, right arm, and lower body");
            RequireStringEqual(speakerBefore, SpeakerTransformAndAppearanceSignature(behaviour),
                "Speaker_Idle speaker transform and appearance");
            RequireStringEqual(modelHashBefore, ComputeAssetHash(ModelAssetPath),
                "PortableSpeaker FBX hash");
            RequireStringEqual(materialHashBefore, ComputeAssetHash(MaterialAssetPath),
                "PortableSpeaker material hash");

            Vector3 palmNormal = LeftPalmNormal(target.transform);
            float palmRightAngle = Vector3.Angle(palmNormal, playerRight);
            float palmToHandleDistance = Vector3.Distance(
                LeftPalmCenter(target.transform), behaviour.HandleWorldPosition());
            float fingerPoseError = MaximumFingerPoseError(target.transform, profile);
            WritePalmRightText("baseline.txt",
                "protectedSignature=" + ComputeStringHash(protectedBefore) + Environment.NewLine +
                "speakerSignature=" + ComputeStringHash(speakerBefore) + Environment.NewLine +
                "modelHash=" + modelHashBefore + Environment.NewLine +
                "materialHash=" + materialHashBefore + Environment.NewLine);
            var report = new StringBuilder()
                .AppendLine("Speaker_Idle palm-right application")
                .AppendLine("preexistingSceneDirty=" + sceneWasDirty)
                .AppendLine("sceneSaved=False")
                .AppendLine("leftShoulderChanged=False")
                .AppendLine("leftHandGripLocked=False")
                .AppendLine("absoluteLeftArmPoseAppliedEachFrame=False")
                .AppendLine("animatedLeftArmMotionWeight=" +
                    Num(SpeakerLeftShoulderFollowBehaviour.LeftArmMotionWeight))
                .AppendLine("palmNormalWorld=" + Vec(palmNormal))
                .AppendLine("playerRightWorld=" + Vec(playerRight))
                .AppendLine("palmRightAngleDegrees=" + Num(palmRightAngle))
                .AppendLine("wristStraightnessDegrees=" +
                    Num(LeftWristStraightness(target.transform)))
                .AppendLine("palmToHandleDistance=" + Num(palmToHandleDistance))
                .AppendLine("fingerPoseErrorDegrees=" + Num(fingerPoseError))
                .AppendLine("leftArmPoseCount=" + palmRightPose.Length)
                .AppendLine("speakerModelLocalPosition=" +
                    Vec(behaviour.SpeakerModel.localPosition))
                .AppendLine("speakerModelLocalRotation=" +
                    Quat(behaviour.SpeakerModel.localRotation))
                .AppendLine("speakerModelLocalScale=" +
                    Vec(behaviour.SpeakerModel.localScale))
                .AppendLine("rendererMeshMaterialChanged=False")
                .AppendLine("torsoHeadRightArmLowerBodyChanged=False");
            WritePalmRightText("application.txt", report.ToString());
            UnityConsoleDiagnostics.AssertNoErrors();
            Debug.Log("[SpeakerIdleCarry] Palm-right pose applied without grip locking.");
        }

        internal static void InspectPalmRight()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            SpeakerLeftShoulderFollowBehaviour behaviour =
                target.GetComponent<SpeakerLeftShoulderFollowBehaviour>() ??
                throw new InvalidOperationException("Speaker_Idle carry behaviour is missing.");
            SpeakerIdleCarryProfile profile = behaviour.Profile ??
                throw new InvalidOperationException("Speaker_Idle carry profile is missing.");
            string[] expectedPaths = { LeftArmPath, LeftForeArmPath, LeftHandPath };
            if (!profile.LeftArmPose.Select(item => item.Path).SequenceEqual(expectedPaths))
                throw new InvalidOperationException(
                    "Speaker_Idle palm-right pose must contain arm, forearm, and hand.");
            if (SpeakerLeftShoulderFollowBehaviour.LeftHandGripLocked)
                throw new InvalidOperationException("Speaker_Idle left-hand grip is locked.");

            string[] baseline = File.ReadAllLines(
                Absolute(PalmRightOutputFolder + "/baseline.txt"), Encoding.UTF8);
            RequireStringEqual(
                BaselineValue(baseline, "protectedSignature"),
                ComputeStringHash(ProtectedHierarchySignature(target.transform)),
                "Speaker_Idle protected hierarchy");
            RequireStringEqual(
                BaselineValue(baseline, "speakerSignature"),
                ComputeStringHash(SpeakerTransformAndAppearanceSignature(behaviour)),
                "Speaker_Idle speaker transform and appearance");
            RequireStringEqual(
                BaselineValue(baseline, "modelHash"), ComputeAssetHash(ModelAssetPath),
                "PortableSpeaker FBX hash");
            RequireStringEqual(
                BaselineValue(baseline, "materialHash"), ComputeAssetHash(MaterialAssetPath),
                "PortableSpeaker material hash");

            Vector3 palmNormal = LeftPalmNormal(target.transform);
            float palmRightAngle = Vector3.Angle(
                palmNormal, target.transform.right.normalized);
            float palmToHandleDistance = Vector3.Distance(
                LeftPalmCenter(target.transform), behaviour.HandleWorldPosition());
            float fingerPoseError = MaximumFingerPoseError(target.transform, profile);
            if (palmRightAngle > 3f || palmToHandleDistance > 0.08f ||
                fingerPoseError > RotationTolerance)
                throw new InvalidOperationException(
                    "Speaker_Idle palm-right pose changed unexpectedly.");

            var report = new StringBuilder()
                .AppendLine("Speaker_Idle palm-right inspection")
                .AppendLine("verificationTargetManipulated=False")
                .AppendLine("leftShoulderChanged=False")
                .AppendLine("leftHandGripLocked=False")
                .AppendLine("palmNormalWorld=" + Vec(palmNormal))
                .AppendLine("playerRightWorld=" + Vec(target.transform.right.normalized))
                .AppendLine("palmRightAngleDegrees=" + Num(palmRightAngle))
                .AppendLine("wristStraightnessDegrees=" +
                    Num(LeftWristStraightness(target.transform)))
                .AppendLine("palmToHandleDistance=" + Num(palmToHandleDistance))
                .AppendLine("fingerPoseErrorDegrees=" + Num(fingerPoseError))
                .AppendLine("speakerModelLocalPosition=" +
                    Vec(behaviour.SpeakerModel.localPosition))
                .AppendLine("speakerModelLocalRotation=" +
                    Quat(behaviour.SpeakerModel.localRotation))
                .AppendLine("speakerModelLocalScale=" +
                    Vec(behaviour.SpeakerModel.localScale))
                .AppendLine("sceneSaved=False")
                .AppendLine("sceneDirty=" + scene.isDirty);
            WritePalmRightText("verification.txt", report.ToString());
            UnityConsoleDiagnostics.AssertNoErrors();
            Debug.Log("[SpeakerIdleCarry] Palm-right inspection passed read-only.");
        }

        internal static void ApplyNeutralHandleHand()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            bool sceneWasDirty = scene.isDirty;
            GameObject target = FindUnique(scene, TargetName);
            Transform upperArm = RequirePath(target.transform, LeftArmPath);
            Transform foreArm = RequirePath(target.transform, LeftForeArmPath);
            Transform hand = RequirePath(target.transform, LeftHandPath);
            SpeakerLeftShoulderFollowBehaviour behaviour =
                target.GetComponent<SpeakerLeftShoulderFollowBehaviour>() ??
                throw new InvalidOperationException("Speaker_Idle carry behaviour is missing.");
            SpeakerIdleCarryProfile profile = behaviour.Profile ??
                throw new InvalidOperationException("Speaker_Idle carry profile is missing.");
            if (profile.SourceLeftArmPose == null || profile.SourceLeftArmPose.Length == 0)
                throw new InvalidOperationException("Speaker_Idle source left-arm pose is missing.");

            behaviour.RefreshPreview();
            Transform model = RequireSpeakerModel(behaviour);
            string protectedBefore = ProtectedHierarchySignature(target.transform);
            string speakerBefore = SpeakerTransformAndAppearanceSignature(behaviour);
            string modelHashBefore = ComputeAssetHash(ModelAssetPath);
            string materialHashBefore = ComputeAssetHash(MaterialAssetPath);

            ApplyBonePose(target.transform, profile.SourceLeftArmPose);
            Vector3 handlePointInModelSpace = CalculateUpperHandleCenterInModelSpace(model);
            Vector3 handleWorldPosition = model.TransformPoint(handlePointInModelSpace);
            Vector3 handlePointInHolderSpace =
                behaviour.SpeakerHolder.transform.InverseTransformPoint(handleWorldPosition);
            Vector3 handleAxisInHolderSpace = behaviour.SpeakerHolder.transform
                .InverseTransformDirection(model.TransformDirection(Vector3.right)).normalized;
            Vector3 playerLeft = -target.transform.right;
            Vector3 playerForward = target.transform.forward;
            Vector3 palmTarget = handleWorldPosition + target.transform.up * 0.008f;
            Vector3 pole = upperArm.position + playerLeft * 0.34f +
                playerForward * 0.10f - target.transform.up * 0.18f;
            for (int iteration = 0; iteration < 4; iteration++)
            {
                Vector3 palmError = palmTarget - LeftPalmCenter(target.transform);
                if (palmError.sqrMagnitude <= 0.000001f) break;
                SolveTwoBone(
                    upperArm, foreArm, hand, hand.position + palmError, pole);
            }

            SpeakerBoneRotation[] neutralPose =
            {
                new SpeakerBoneRotation(LeftArmPath, upperArm.localRotation),
                new SpeakerBoneRotation(LeftForeArmPath, foreArm.localRotation),
                new SpeakerBoneRotation(LeftHandPath, hand.localRotation)
            };
            profile.Configure(
                profile.SpeakerPrefab,
                profile.LeftShoulderPath,
                profile.PositionOffsetInShoulderSpace,
                profile.RotationOffsetFromShoulder,
                profile.HolderScale,
                profile.ModelLocalPosition,
                profile.ModelLocalRotation,
                profile.ModelLocalScale,
                handlePointInHolderSpace,
                handleAxisInHolderSpace,
                profile.SourceLeftArmPose,
                neutralPose);
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            behaviour.Configure(profile);

            RequireStringEqual(protectedBefore, ProtectedHierarchySignature(target.transform),
                "Speaker_Idle torso, head, right arm, and lower body");
            RequireStringEqual(speakerBefore, SpeakerTransformAndAppearanceSignature(behaviour),
                "Speaker_Idle speaker transform and appearance");
            RequireStringEqual(modelHashBefore, ComputeAssetHash(ModelAssetPath),
                "PortableSpeaker FBX hash");
            RequireStringEqual(materialHashBefore, ComputeAssetHash(MaterialAssetPath),
                "PortableSpeaker material hash");

            float palmToHandleDistance = Vector3.Distance(
                LeftPalmCenter(target.transform), behaviour.HandleWorldPosition());
            Quaternion sourceHandRotation = profile.SourceLeftArmPose.Single(
                item => item.Path == LeftHandPath).LocalRotation;
            float wristSourceRotationError = Quaternion.Angle(
                hand.localRotation, sourceHandRotation);
            WriteNeutralText("baseline.txt",
                "protectedSignature=" + ComputeStringHash(protectedBefore) + Environment.NewLine +
                "speakerSignature=" + ComputeStringHash(speakerBefore) + Environment.NewLine +
                "modelHash=" + modelHashBefore + Environment.NewLine +
                "materialHash=" + materialHashBefore + Environment.NewLine);
            var report = new StringBuilder()
                .AppendLine("Speaker_Idle neutral handle-near hand application")
                .AppendLine("preexistingSceneDirty=" + sceneWasDirty)
                .AppendLine("sceneSaved=False")
                .AppendLine("leftHandGripLocked=False")
                .AppendLine("absoluteLeftArmPoseAppliedEachFrame=False")
                .AppendLine("animatedLeftArmOffsetApplied=True")
                .AppendLine("animatedLeftArmMotionWeight=" +
                    Num(SpeakerLeftShoulderFollowBehaviour.LeftArmMotionWeight))
                .AppendLine("fingerPoseAuthored=False")
                .AppendLine("handlePointInModelSpace=" + Vec(handlePointInModelSpace))
                .AppendLine("handlePointInHolderSpace=" + Vec(handlePointInHolderSpace))
                .AppendLine("handleAxisInHolderSpace=" + Vec(handleAxisInHolderSpace))
                .AppendLine("leftHandWorldPosition=" + Vec(hand.position))
                .AppendLine("leftPalmWorldPosition=" +
                    Vec(LeftPalmCenter(target.transform)))
                .AppendLine("handleWorldPosition=" + Vec(behaviour.HandleWorldPosition()))
                .AppendLine("palmToHandleDistance=" + Num(palmToHandleDistance))
                .AppendLine("wristSourceLocalRotationErrorDegrees=" +
                    Num(wristSourceRotationError))
                .AppendLine("leftArmPoseCount=" + neutralPose.Length)
                .AppendLine("speakerModelLocalPosition=" + Vec(model.localPosition))
                .AppendLine("speakerModelLocalRotation=" + Quat(model.localRotation))
                .AppendLine("speakerModelLocalScale=" + Vec(model.localScale))
                .AppendLine("shoulderPositionOffset=" +
                    Vec(profile.PositionOffsetInShoulderSpace))
                .AppendLine("shoulderRotationOffset=" +
                    Quat(profile.RotationOffsetFromShoulder))
                .AppendLine("holderScale=" + Vec(profile.HolderScale))
                .AppendLine("rendererMeshMaterialChanged=False")
                .AppendLine("torsoHeadRightArmLowerBodyChanged=False");
            WriteNeutralText("application.txt", report.ToString());
            UnityConsoleDiagnostics.AssertNoErrors();
            Debug.Log("[SpeakerIdleCarry] Neutral handle-near hand applied without grip lock.");
        }

        internal static void InspectNeutralHandleHand()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            Transform hand = RequirePath(target.transform, LeftHandPath);
            SpeakerLeftShoulderFollowBehaviour behaviour =
                target.GetComponent<SpeakerLeftShoulderFollowBehaviour>() ??
                throw new InvalidOperationException("Speaker_Idle carry behaviour is missing.");
            SpeakerIdleCarryProfile profile = behaviour.Profile ??
                throw new InvalidOperationException("Speaker_Idle carry profile is missing.");
            string[] expectedPaths = { LeftArmPath, LeftForeArmPath, LeftHandPath };
            string[] actualPaths = profile.LeftArmPose.Select(item => item.Path).ToArray();
            if (!actualPaths.SequenceEqual(expectedPaths))
                throw new InvalidOperationException(
                    "Speaker_Idle neutral pose must contain only arm, forearm, and hand.");
            if (SpeakerLeftShoulderFollowBehaviour.LeftHandGripLocked)
                throw new InvalidOperationException("Speaker_Idle left-hand grip is locked.");

            string[] baseline = File.ReadAllLines(
                Absolute(NeutralHandleHandOutputFolder + "/baseline.txt"), Encoding.UTF8);
            RequireStringEqual(
                BaselineValue(baseline, "protectedSignature"),
                ComputeStringHash(ProtectedHierarchySignature(target.transform)),
                "Speaker_Idle protected hierarchy");
            RequireStringEqual(
                BaselineValue(baseline, "speakerSignature"),
                ComputeStringHash(SpeakerTransformAndAppearanceSignature(behaviour)),
                "Speaker_Idle speaker transform and appearance");
            RequireStringEqual(
                BaselineValue(baseline, "modelHash"), ComputeAssetHash(ModelAssetPath),
                "PortableSpeaker FBX hash");
            RequireStringEqual(
                BaselineValue(baseline, "materialHash"), ComputeAssetHash(MaterialAssetPath),
                "PortableSpeaker material hash");

            Vector3 actualHandlePoint = behaviour.SpeakerHolder.transform.InverseTransformPoint(
                behaviour.SpeakerModel.TransformPoint(
                    CalculateUpperHandleCenterInModelSpace(behaviour.SpeakerModel)));
            float handlePointError = Vector3.Distance(
                profile.HandlePointInHolderSpace, actualHandlePoint);
            Quaternion sourceHandRotation = profile.SourceLeftArmPose.Single(
                item => item.Path == LeftHandPath).LocalRotation;
            float wristSourceRotationError = Quaternion.Angle(
                hand.localRotation, sourceHandRotation);
            float palmToHandleDistance = Vector3.Distance(
                LeftPalmCenter(target.transform), behaviour.HandleWorldPosition());
            if (handlePointError > PositionTolerance ||
                wristSourceRotationError > RotationTolerance ||
                palmToHandleDistance > 0.05f)
                throw new InvalidOperationException(
                    "Speaker_Idle handle-near hand pose changed unexpectedly.");

            var report = new StringBuilder()
                .AppendLine("Speaker_Idle neutral handle-near hand inspection")
                .AppendLine("verificationTargetManipulated=False")
                .AppendLine("leftHandGripLocked=False")
                .AppendLine("absoluteLeftArmPoseAppliedEachFrame=False")
                .AppendLine("animatedLeftArmOffsetApplied=True")
                .AppendLine("animatedLeftArmMotionWeight=" +
                    Num(SpeakerLeftShoulderFollowBehaviour.LeftArmMotionWeight))
                .AppendLine("fingerPoseAuthored=False")
                .AppendLine("leftArmPoseCount=" + profile.LeftArmPose.Length)
                .AppendLine("handlePointError=" + Num(handlePointError))
                .AppendLine("palmToHandleDistance=" + Num(palmToHandleDistance))
                .AppendLine("wristSourceLocalRotationErrorDegrees=" +
                    Num(wristSourceRotationError))
                .AppendLine("speakerModelLocalPosition=" +
                    Vec(behaviour.SpeakerModel.localPosition))
                .AppendLine("speakerModelLocalRotation=" +
                    Quat(behaviour.SpeakerModel.localRotation))
                .AppendLine("speakerModelLocalScale=" +
                    Vec(behaviour.SpeakerModel.localScale))
                .AppendLine("sceneSaved=False")
                .AppendLine("sceneDirty=" + scene.isDirty);
            WriteNeutralText("verification.txt", report.ToString());
            UnityConsoleDiagnostics.AssertNoErrors();
            Debug.Log("[SpeakerIdleCarry] Neutral handle-near hand inspection passed.");
        }

        internal static void Apply()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            bool sceneWasDirty = scene.isDirty;
            GameObject target = FindUnique(scene, TargetName);
            Transform shoulder = RequirePath(target.transform, LeftShoulderPath);
            Transform upperArm = RequirePath(target.transform, LeftArmPath);
            Transform foreArm = RequirePath(target.transform, LeftForeArmPath);
            Transform hand = RequirePath(target.transform, LeftHandPath);
            GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ModelAssetPath) ??
                throw new InvalidOperationException("PortableSpeaker model asset is missing.");

            EnsureAssetFolder(ProfileFolder);
            SpeakerIdleCarryProfile profile =
                AssetDatabase.LoadAssetAtPath<SpeakerIdleCarryProfile>(ProfileAssetPath);
            bool newProfile = profile == null;
            if (newProfile)
            {
                profile = ScriptableObject.CreateInstance<SpeakerIdleCarryProfile>();
                AssetDatabase.CreateAsset(profile, ProfileAssetPath);
            }

            SpeakerBoneRotation[] sourcePose = newProfile ||
                profile.SourceLeftArmPose == null || profile.SourceLeftArmPose.Length == 0
                ? CaptureBonePose(upperArm, target.transform)
                : profile.SourceLeftArmPose;
            ApplyBonePose(target.transform, sourcePose);
            string protectedBefore = ProtectedHierarchySignature(target.transform);
            string modelHashBefore = ComputeAssetHash(ModelAssetPath);

            SpeakerLeftShoulderFollowBehaviour behaviour =
                target.GetComponent<SpeakerLeftShoulderFollowBehaviour>() ??
                Undo.AddComponent<SpeakerLeftShoulderFollowBehaviour>(target);
            Vector3 playerLeft = -target.transform.right;
            Vector3 playerForward = target.transform.forward;
            Vector3 desiredSpeakerPosition = shoulder.position +
                playerLeft * 0.28f + Vector3.up * 0.12f + playerForward * 0.03f;
            Quaternion desiredSpeakerRotation =
                target.transform.rotation * Quaternion.Euler(0f, -90f, 0f);
            Vector3 positionOffset = shoulder.InverseTransformPoint(desiredSpeakerPosition);
            Quaternion rotationOffset =
                Quaternion.Inverse(shoulder.rotation) * desiredSpeakerRotation;
            Vector3 holderScale = Vector3.one * 0.52f;
            Vector3 handlePoint = new Vector3(0.20f, 0.36f, 0f);
            Vector3 handleAxis = Vector3.right;

            profile.Configure(
                modelAsset,
                LeftShoulderPath,
                positionOffset,
                rotationOffset,
                holderScale,
                AuthoredModelLocalPosition,
                AuthoredModelLocalRotation,
                AuthoredModelLocalScale,
                handlePoint,
                handleAxis,
                sourcePose,
                sourcePose);
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            Undo.RecordObject(behaviour, "Configure Speaker_Idle shoulder carry");
            behaviour.Configure(profile);

            Vector3 handleWorldPosition = behaviour.HandleWorldPosition();
            Vector3 wristTarget = handleWorldPosition + Vector3.up * 0.045f;
            Vector3 pole = upperArm.position +
                playerLeft * 0.30f + playerForward * 0.22f - Vector3.up * 0.04f;
            SolveTwoBone(upperArm, foreArm, hand, wristTarget, pole);
            AlignHandToHandle(
                target.transform,
                hand,
                RequirePath(target.transform,
                    LeftHandPath + "/LeftIndexProximal"),
                RequirePath(target.transform,
                    LeftHandPath + "/LeftLittleProximal"),
                RequirePath(target.transform,
                    LeftHandPath + "/LeftMiddleProximal"),
                behaviour.HandleWorldAxis());
            CurlFingers(target.transform);

            SpeakerBoneRotation[] finalPose = CaptureBonePose(upperArm, target.transform);
            profile.Configure(
                modelAsset,
                LeftShoulderPath,
                positionOffset,
                rotationOffset,
                holderScale,
                AuthoredModelLocalPosition,
                AuthoredModelLocalRotation,
                AuthoredModelLocalScale,
                handlePoint,
                handleAxis,
                sourcePose,
                finalPose);
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            behaviour.Configure(profile);
            EditorUtility.SetDirty(behaviour);
            EditorSceneManager.MarkSceneDirty(scene);

            RequireStringEqual(protectedBefore, ProtectedHierarchySignature(target.transform),
                "Speaker_Idle protected body and appearance");
            RequireStringEqual(modelHashBefore, ComputeAssetHash(ModelAssetPath),
                "PortableSpeaker FBX hash");
            RequireSpeakerAssets(behaviour);

            string workingScenePath = Absolute(OutputFolder + "/working_scene.unity");
            bool workingCopySaved = EditorSceneManager.SaveScene(
                scene, workingScenePath, true);
            MonoScript script = MonoScript.FromMonoBehaviour(behaviour);
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
                script, out string scriptGuid, out long scriptLocalId);
            string profileGuid = AssetDatabase.AssetPathToGUID(ProfileAssetPath);
            GlobalObjectId componentId = GlobalObjectId.GetGlobalObjectIdSlow(behaviour);
            Bounds speakerBounds = CalculateWorldBounds(behaviour.SpeakerHolder);
            float wristError = Vector3.Distance(hand.position, wristTarget);
            float handleAxisError = HandHandleAxisError(target.transform, behaviour);
            float speakerFrontError = Vector3.Angle(
                behaviour.SpeakerHolder.transform.forward, playerLeft);
            float speakerSideError = Mathf.Min(
                Vector3.Angle(behaviour.SpeakerHolder.transform.right, playerForward),
                Vector3.Angle(-behaviour.SpeakerHolder.transform.right, playerForward));
            float shoulderSurfaceDistance = Mathf.Sqrt(
                speakerBounds.SqrDistance(upperArm.position));

            WriteText("baseline.txt",
                "protectedSignature=" + ComputeStringHash(protectedBefore) + Environment.NewLine +
                "modelHash=" + modelHashBefore + Environment.NewLine +
                "sourcePoseCount=" + sourcePose.Length + Environment.NewLine);
            var report = new StringBuilder()
                .AppendLine("Speaker_Idle carry application")
                .AppendLine("preexistingSceneDirty=" + sceneWasDirty)
                .AppendLine("originalSceneSaved=False")
                .AppendLine("workingCopySaved=" + workingCopySaved)
                .AppendLine("workingCopyPath=" + workingScenePath)
                .AppendLine("speakerModel=" + ModelAssetPath)
                .AppendLine("profilePath=" + ProfileAssetPath)
                .AppendLine("profileGuid=" + profileGuid)
                .AppendLine("componentGlobalId=" + componentId)
                .AppendLine("scriptGuid=" + scriptGuid)
                .AppendLine("scriptLocalId=" + scriptLocalId)
                .AppendLine("leftShoulderPath=" + LeftShoulderPath)
                .AppendLine("leftArmPoseCount=" + finalPose.Length)
                .AppendLine("speakerPositionOffset=" + Vec(positionOffset))
                .AppendLine("speakerRotationOffset=" + Quat(rotationOffset))
                .AppendLine("speakerHolderScale=" + Vec(holderScale))
                .AppendLine("speakerModelLocalPosition=" + Vec(AuthoredModelLocalPosition))
                .AppendLine("speakerModelLocalRotation=" + Quat(AuthoredModelLocalRotation))
                .AppendLine("speakerModelLocalScale=" + Vec(AuthoredModelLocalScale))
                .AppendLine("speakerWorldPosition=" + Vec(behaviour.SpeakerHolder.transform.position))
                .AppendLine("speakerWorldRotation=" + Quat(behaviour.SpeakerHolder.transform.rotation))
                .AppendLine("speakerBoundsCenter=" + Vec(speakerBounds.center))
                .AppendLine("speakerBoundsSize=" + Vec(speakerBounds.size))
                .AppendLine("handleWorldPosition=" + Vec(handleWorldPosition))
                .AppendLine("leftHandWorldPosition=" + Vec(hand.position))
                .AppendLine("wristTargetError=" + Num(wristError))
                .AppendLine("handHandleAxisErrorDegrees=" + Num(handleAxisError))
                .AppendLine("speakerFrontToPlayerLeftErrorDegrees=" + Num(speakerFrontError))
                .AppendLine("speakerSideToPlayerForwardErrorDegrees=" + Num(speakerSideError))
                .AppendLine("shoulderToSpeakerBoundsDistance=" + Num(shoulderSurfaceDistance))
                .AppendLine("torsoHeadRightArmLowerBodyChanged=False")
                .AppendLine("sourceModelChanged=False")
                .AppendLine("validationTargetManipulated=False");
            foreach (SpeakerBoneRotation item in finalPose)
                report.AppendLine("pose=" + item.Path + "|" + Quat(item.LocalRotation));
            WriteText("application.txt", report.ToString());
            UnityConsoleDiagnostics.AssertNoErrors();
            Debug.Log("[SpeakerIdleCarry] Applied. " + report.ToString().Replace('\n', ' '));
        }

        internal static void Inspect()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            SpeakerLeftShoulderFollowBehaviour behaviour =
                target.GetComponent<SpeakerLeftShoulderFollowBehaviour>() ??
                throw new InvalidOperationException("Speaker_Idle carry behaviour is missing.");
            behaviour.RefreshPreview();
            RequireSpeakerAssets(behaviour);
            string[] baseline = File.ReadAllLines(
                Absolute(OutputFolder + "/baseline.txt"), Encoding.UTF8);
            RequireStringEqual(
                BaselineValue(baseline, "protectedSignature"),
                ComputeStringHash(ProtectedHierarchySignature(target.transform)),
                "Speaker_Idle protected body and appearance signature");
            RequireStringEqual(
                BaselineValue(baseline, "modelHash"),
                ComputeAssetHash(ModelAssetPath),
                "PortableSpeaker FBX hash");
            Transform hand = RequirePath(target.transform, LeftHandPath);
            float positionError = Vector3.Distance(
                behaviour.SpeakerHolder.transform.position,
                behaviour.ExpectedWorldPosition());
            float rotationError = Quaternion.Angle(
                behaviour.SpeakerHolder.transform.rotation,
                behaviour.ExpectedWorldRotation());
            float handleAxisError = HandHandleAxisError(target.transform, behaviour);
            if (positionError > PositionTolerance || rotationError > RotationTolerance ||
                handleAxisError > 8f)
                throw new InvalidOperationException(
                    "Speaker_Idle carry alignment changed. position=" + Num(positionError) +
                    ", rotation=" + Num(rotationError) + ", handleAxis=" +
                    Num(handleAxisError) + ".");
            var report = new StringBuilder()
                .AppendLine("Speaker_Idle carry inspection")
                .AppendLine("verificationTargetManipulated=False")
                .AppendLine("componentConfigured=True")
                .AppendLine("speakerPresent=True")
                .AppendLine("speakerPositionFollowError=" + Num(positionError))
                .AppendLine("speakerRotationFollowErrorDegrees=" + Num(rotationError))
                .AppendLine("handHandleAxisErrorDegrees=" + Num(handleAxisError))
                .AppendLine("leftHandWorldPosition=" + Vec(hand.position))
                .AppendLine("handleWorldPosition=" + Vec(behaviour.HandleWorldPosition()))
                .AppendLine("torsoHeadRightArmLowerBodyChanged=False")
                .AppendLine("embeddedTexturesPresent=4")
                .AppendLine("embeddedMaterialPresent=True")
                .AppendLine("sceneDirty=" + scene.isDirty);
            WriteText("inspection.txt", report.ToString());
            UnityConsoleDiagnostics.AssertNoErrors();
            Debug.Log("[SpeakerIdleCarry] Inspection passed. " +
                report.ToString().Replace('\n', ' '));
        }

        internal static void SnapshotEditedTransform()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            SpeakerLeftShoulderFollowBehaviour behaviour =
                target.GetComponent<SpeakerLeftShoulderFollowBehaviour>() ??
                throw new InvalidOperationException("Speaker_Idle carry behaviour is missing.");
            Transform model = RequireSpeakerModel(behaviour);
            var report = new StringBuilder()
                .AppendLine("Speaker_Idle PortableSpeaker_Model post-refresh snapshot")
                .AppendLine("verificationTargetManipulated=False")
                .AppendLine("hierarchyPath=Speaker_Idle/Speaker_Prop/PortableSpeaker_Model")
                .AppendLine("modelLocalPosition=" + Vec(model.localPosition))
                .AppendLine("modelLocalRotation=" + Quat(model.localRotation))
                .AppendLine("modelLocalEulerAngles=" + Vec(model.localEulerAngles))
                .AppendLine("modelLocalScale=" + Vec(model.localScale))
                .AppendLine("sceneSaved=False")
                .AppendLine("sceneDirty=" + scene.isDirty);
            WriteReflectionText("post_refresh_snapshot.txt", report.ToString());
            UnityConsoleDiagnostics.AssertNoErrors();
            Debug.Log("[SpeakerIdleCarry] Edited model transform snapshot captured read-only. " +
                report.ToString().Replace('\n', ' '));
        }

        internal static void ApplyTransformReflection()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            bool sceneWasDirty = scene.isDirty;
            GameObject target = FindUnique(scene, TargetName);
            SpeakerLeftShoulderFollowBehaviour behaviour =
                target.GetComponent<SpeakerLeftShoulderFollowBehaviour>() ??
                throw new InvalidOperationException("Speaker_Idle carry behaviour is missing.");
            string modelHashBefore = ComputeAssetHash(ModelAssetPath);

            behaviour.RefreshPreview();
            Transform model = RequireSpeakerModel(behaviour);
            float modelPositionError = Vector3.Distance(
                model.localPosition, AuthoredModelLocalPosition);
            float modelRotationError = Quaternion.Angle(
                model.localRotation, AuthoredModelLocalRotation);
            float modelScaleError = Vector3.Distance(
                model.localScale, AuthoredModelLocalScale);
            float holderPositionError = Vector3.Distance(
                behaviour.SpeakerHolder.transform.position,
                behaviour.ExpectedWorldPosition());
            float holderRotationError = Quaternion.Angle(
                behaviour.SpeakerHolder.transform.rotation,
                behaviour.ExpectedWorldRotation());
            if (modelPositionError > PositionTolerance ||
                modelRotationError > RotationTolerance ||
                modelScaleError > PositionTolerance ||
                holderPositionError > PositionTolerance ||
                holderRotationError > RotationTolerance ||
                SpeakerLeftShoulderFollowBehaviour.EditModeSpeakerTransformLocked)
                throw new InvalidOperationException(
                    "Speaker current transform reflection application is invalid.");
            RequireStringEqual(modelHashBefore, ComputeAssetHash(ModelAssetPath),
                "PortableSpeaker FBX hash");

            var report = new StringBuilder()
                .AppendLine("Speaker_Idle current model transform reflection application")
                .AppendLine("preexistingSceneDirty=" + sceneWasDirty)
                .AppendLine("sceneSaved=False")
                .AppendLine("modelLocalPosition=" + Vec(model.localPosition))
                .AppendLine("modelLocalRotation=" + Quat(model.localRotation))
                .AppendLine("modelLocalScale=" + Vec(model.localScale))
                .AppendLine("shoulderPositionOffset=" +
                    Vec(behaviour.Profile.PositionOffsetInShoulderSpace))
                .AppendLine("shoulderRotationOffset=" +
                    Quat(behaviour.Profile.RotationOffsetFromShoulder))
                .AppendLine("holderScale=" + Vec(behaviour.Profile.HolderScale))
                .AppendLine("modelPositionError=" + Num(modelPositionError))
                .AppendLine("modelRotationErrorDegrees=" + Num(modelRotationError))
                .AppendLine("modelScaleError=" + Num(modelScaleError))
                .AppendLine("holderPositionFollowError=" + Num(holderPositionError))
                .AppendLine("holderRotationFollowErrorDegrees=" + Num(holderRotationError))
                .AppendLine("editModeTransformLocked=False")
                .AppendLine("rendererMeshMaterialChanged=False")
                .AppendLine("leftArmPoseChanged=False");
            WriteReflectionText("application.txt", report.ToString());
            UnityConsoleDiagnostics.AssertNoErrors();
            Debug.Log("[SpeakerIdleCarry] Current model transform reflected without scene save.");
        }

        internal static void VerifyEditedTransformReflection()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            SpeakerLeftShoulderFollowBehaviour behaviour =
                target.GetComponent<SpeakerLeftShoulderFollowBehaviour>() ??
                throw new InvalidOperationException("Speaker_Idle carry behaviour is missing.");
            Transform model = RequireSpeakerModel(behaviour);
            SpeakerIdleCarryProfile profile = behaviour.Profile ??
                throw new InvalidOperationException("Speaker_Idle carry profile is missing.");
            float positionError = Vector3.Distance(
                model.localPosition, AuthoredModelLocalPosition);
            float rotationError = Quaternion.Angle(
                model.localRotation, AuthoredModelLocalRotation);
            float scaleError = Vector3.Distance(
                model.localScale, AuthoredModelLocalScale);
            float profilePositionError = Vector3.Distance(
                profile.ModelLocalPosition, AuthoredModelLocalPosition);
            float profileRotationError = Quaternion.Angle(
                profile.ModelLocalRotation, AuthoredModelLocalRotation);
            float profileScaleError = Vector3.Distance(
                profile.ModelLocalScale, AuthoredModelLocalScale);
            float leftArmPoseError = MaximumPoseRotationError(target.transform, profile);
            float holderPositionError = Vector3.Distance(
                behaviour.SpeakerHolder.transform.position,
                behaviour.ExpectedWorldPosition());
            float holderRotationError = Quaternion.Angle(
                behaviour.SpeakerHolder.transform.rotation,
                behaviour.ExpectedWorldRotation());
            if (positionError > PositionTolerance ||
                rotationError > RotationTolerance ||
                scaleError > PositionTolerance ||
                profilePositionError > PositionTolerance ||
                profileRotationError > RotationTolerance ||
                profileScaleError > PositionTolerance ||
                leftArmPoseError > RotationTolerance ||
                holderPositionError > PositionTolerance ||
                holderRotationError > RotationTolerance ||
                SpeakerLeftShoulderFollowBehaviour.EditModeSpeakerTransformLocked)
                throw new InvalidOperationException(
                    "Speaker edited transform reflection differs from the captured values.");
            string[] baseline = File.ReadAllLines(
                Absolute(OutputFolder + "/baseline.txt"), Encoding.UTF8);
            RequireStringEqual(
                BaselineValue(baseline, "protectedSignature"),
                ComputeStringHash(ProtectedHierarchySignature(target.transform)),
                "Speaker_Idle protected body and appearance signature");
            var report = new StringBuilder()
                .AppendLine("Speaker_Idle edited model transform reflection verification")
                .AppendLine("verificationTargetManipulated=False")
                .AppendLine("capturedLocalPosition=" + Vec(AuthoredModelLocalPosition))
                .AppendLine("capturedLocalRotation=" + Quat(AuthoredModelLocalRotation))
                .AppendLine("capturedLocalScale=" + Vec(AuthoredModelLocalScale))
                .AppendLine("actualLocalPosition=" + Vec(model.localPosition))
                .AppendLine("actualLocalRotation=" + Quat(model.localRotation))
                .AppendLine("actualLocalScale=" + Vec(model.localScale))
                .AppendLine("modelPositionError=" + Num(positionError))
                .AppendLine("modelRotationErrorDegrees=" + Num(rotationError))
                .AppendLine("modelScaleError=" + Num(scaleError))
                .AppendLine("profilePositionError=" + Num(profilePositionError))
                .AppendLine("profileRotationErrorDegrees=" + Num(profileRotationError))
                .AppendLine("profileScaleError=" + Num(profileScaleError))
                .AppendLine("leftArmPoseRotationErrorDegrees=" + Num(leftArmPoseError))
                .AppendLine("holderPositionFollowError=" + Num(holderPositionError))
                .AppendLine("holderRotationFollowErrorDegrees=" + Num(holderRotationError))
                .AppendLine("shoulderPositionOffset=" +
                    Vec(profile.PositionOffsetInShoulderSpace))
                .AppendLine("shoulderRotationOffset=" +
                    Quat(profile.RotationOffsetFromShoulder))
                .AppendLine("editModeTransformLocked=False")
                .AppendLine("rendererMeshMaterialChanged=False")
                .AppendLine("torsoHeadRightArmLowerBodyChanged=False")
                .AppendLine("speakerHolderChanged=False")
                .AppendLine("sceneSaved=False")
                .AppendLine("sceneDirty=" + scene.isDirty);
            WriteReflectionText("verification.txt", report.ToString());
            UnityConsoleDiagnostics.AssertNoErrors();
            Debug.Log("[SpeakerIdleCarry] Edited model transform reflection verified. " +
                report.ToString().Replace('\n', ' '));
        }

        internal static void CaptureEditedTransformDiagnostic()
        {
            CaptureEditedTransformComparison(false);
        }

        internal static void CaptureEditedTransformFinal()
        {
            CaptureEditedTransformComparison(true);
        }

        private static void CaptureEditedTransformComparison(bool final)
        {
            RequireEditMode();
            VerifyEditedTransformReflection();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            SpeakerLeftShoulderFollowBehaviour behaviour =
                target.GetComponent<SpeakerLeftShoulderFollowBehaviour>() ??
                throw new InvalidOperationException("Speaker_Idle carry behaviour is missing.");
            Transform model = RequireSpeakerModel(behaviour);
            Texture2D[] panels = new Texture2D[6];
            Texture2D composite = null;
            try
            {
                Vector3 front = Vector3.ProjectOnPlane(
                    target.transform.forward, Vector3.up).normalized;
                Vector3 playerLeft = -target.transform.right;
                Vector3 threeQuarter = (front + playerLeft * 0.7f).normalized;
                Bounds fullBounds = CalculateWorldBounds(target);
                Bounds speakerBounds = CalculateWorldBounds(behaviour.SpeakerHolder);
                panels[0] = LoadEditedTransformBaselinePanel();
                panels[1] = CaptureTargetView(scene, fullBounds, front);
                panels[2] = CaptureTargetView(scene, fullBounds, playerLeft);
                panels[3] = CaptureTargetView(scene, fullBounds, threeQuarter);
                panels[4] = CaptureTargetView(scene, speakerBounds, front);
                panels[5] = CaptureTargetView(scene, speakerBounds, playerLeft);
                composite = new Texture2D(1536, 1024, TextureFormat.RGB24, false);
                for (int index = 0; index < panels.Length; index++)
                {
                    int column = index % 3;
                    int row = 1 - index / 3;
                    composite.SetPixels32(
                        column * 512,
                        row * 512,
                        512,
                        512,
                        panels[index].GetPixels32());
                }
                composite.Apply(false, false);
                string destination = Absolute(final
                    ? TransformReflectionFinalImagePath
                    : TransformReflectionDiagnosticImagePath);
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
                File.WriteAllBytes(destination, composite.EncodeToPNG());
            }
            finally
            {
                foreach (Texture2D panel in panels)
                    if (panel != null) UnityEngine.Object.DestroyImmediate(panel);
                if (composite != null) UnityEngine.Object.DestroyImmediate(composite);
            }

            var report = new StringBuilder()
                .AppendLine("Speaker_Idle edited PortableSpeaker_Model comparison")
                .AppendLine("captureKind=" + (final ? "Final" : "Diagnostic"))
                .AppendLine("panelOrder=beforeUnityView,currentFront,currentPlayerLeft,currentThreeQuarter,currentSpeakerSide,currentSpeakerFront")
                .AppendLine("verificationTargetManipulated=False")
                .AppendLine("actualLocalPosition=" + Vec(model.localPosition))
                .AppendLine("actualLocalRotation=" + Quat(model.localRotation))
                .AppendLine("actualLocalScale=" + Vec(model.localScale))
                .AppendLine("torsoHeadRightArmLowerBodyChanged=False")
                .AppendLine("speakerHolderChanged=False")
                .AppendLine("sceneSaved=False");
            WriteReflectionText(
                final ? "final_runtime.txt" : "diagnostic_runtime.txt",
                report.ToString());
            UnityConsoleDiagnostics.AssertNoErrors();
        }

        private static Transform RequireSpeakerModel(
            SpeakerLeftShoulderFollowBehaviour behaviour)
        {
            if (behaviour.SpeakerHolder == null)
                throw new InvalidOperationException("Speaker_Prop preview is missing.");
            Transform model = behaviour.SpeakerModel ??
                behaviour.SpeakerHolder.transform.Find("PortableSpeaker_Model");
            if (model == null)
                throw new InvalidOperationException("PortableSpeaker_Model preview is missing.");
            return model;
        }

        private static Texture2D LoadEditedTransformBaselinePanel()
        {
            string path = Absolute(
                TransformReflectionOutputFolder + "/unity_model_selected_05.png");
            if (!File.Exists(path))
                throw new FileNotFoundException(
                    "The direct pre-change Unity view is missing.", path);
            Texture2D source = new Texture2D(2, 2, TextureFormat.RGB24, false);
            if (!ImageConversion.LoadImage(source, File.ReadAllBytes(path), false))
            {
                UnityEngine.Object.DestroyImmediate(source);
                throw new InvalidOperationException(
                    "The direct pre-change Unity view failed to load.");
            }
            int cropX = Mathf.Clamp(930, 0, source.width - 1);
            int cropY = Mathf.Clamp(355, 0, source.height - 1);
            int cropSize = Mathf.Min(570,
                Mathf.Min(source.width - cropX, source.height - cropY));
            Texture2D crop = new Texture2D(
                cropSize, cropSize, TextureFormat.RGB24, false);
            crop.SetPixels(source.GetPixels(cropX, cropY, cropSize, cropSize));
            crop.Apply(false, false);
            RenderTexture render = RenderTexture.GetTemporary(
                512, 512, 0, RenderTextureFormat.ARGB32);
            RenderTexture previous = RenderTexture.active;
            try
            {
                Graphics.Blit(crop, render);
                RenderTexture.active = render;
                Texture2D result = new Texture2D(
                    512, 512, TextureFormat.RGB24, false);
                result.ReadPixels(new Rect(0, 0, 512, 512), 0, 0);
                result.Apply(false, false);
                return result;
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(render);
                UnityEngine.Object.DestroyImmediate(crop);
                UnityEngine.Object.DestroyImmediate(source);
            }
        }

        internal static void EnterReview()
        {
            Inspect();
            EditorApplication.EnterPlaymode();
        }

        internal static void CaptureDiagnostic()
        {
            CaptureRuntimeComparison(false);
        }

        internal static void CaptureFinal()
        {
            CaptureRuntimeComparison(true);
        }

        internal static void StopReview()
        {
            if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
        }

        private static void CaptureRuntimeComparison(bool final)
        {
            if (!EditorApplication.isPlaying)
                throw new InvalidOperationException(
                    "Speaker carry comparison requires natural Play Mode.");
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            SpeakerLeftShoulderFollowBehaviour behaviour =
                target.GetComponent<SpeakerLeftShoulderFollowBehaviour>() ??
                throw new InvalidOperationException("Runtime Speaker carry behaviour is missing.");
            behaviour.RefreshPreview();
            RequireSpeakerAssets(behaviour);
            float positionError = Vector3.Distance(
                behaviour.SpeakerHolder.transform.position,
                behaviour.ExpectedWorldPosition());
            float rotationError = Quaternion.Angle(
                behaviour.SpeakerHolder.transform.rotation,
                behaviour.ExpectedWorldRotation());
            float poseRotationError = MaximumPoseRotationError(target.transform, behaviour.Profile);
            if (positionError > PositionTolerance || rotationError > RotationTolerance ||
                poseRotationError > RotationTolerance)
                throw new InvalidOperationException(
                    "Runtime Speaker carry follow error exceeded tolerance.");

            Texture2D[] panels = new Texture2D[6];
            Texture2D composite = null;
            try
            {
                panels[0] = LoadReferencePanel();
                Vector3 front = Vector3.ProjectOnPlane(target.transform.forward, Vector3.up)
                    .normalized;
                Vector3 playerLeft = -target.transform.right;
                Vector3 threeQuarter = (front + playerLeft * 0.7f).normalized;
                Bounds fullBounds = CalculateWorldBounds(target);
                Vector3 handPosition = RequirePath(target.transform, LeftHandPath).position;
                Vector3 handlePosition = behaviour.HandleWorldPosition();
                Bounds gripBounds = new Bounds(
                    (handPosition + handlePosition) * 0.5f,
                    Vector3.one * 0.34f);
                panels[1] = CaptureTargetView(scene, fullBounds, front);
                panels[2] = CaptureTargetView(scene, fullBounds, playerLeft);
                panels[3] = CaptureTargetView(scene, fullBounds, threeQuarter);
                panels[4] = CaptureTargetView(scene, gripBounds, front);
                panels[5] = CaptureTargetView(scene, gripBounds, threeQuarter);
                composite = new Texture2D(1536, 1024, TextureFormat.RGB24, false);
                for (int index = 0; index < panels.Length; index++)
                {
                    int column = index % 3;
                    int row = 1 - index / 3;
                    composite.SetPixels32(
                        column * 512,
                        row * 512,
                        512,
                        512,
                        panels[index].GetPixels32());
                }
                composite.Apply(false, false);
                string destination = final ? FinalAbsolutePath : DiagnosticAbsolutePath;
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
                File.WriteAllBytes(destination, composite.EncodeToPNG());
            }
            finally
            {
                foreach (Texture2D panel in panels)
                    if (panel != null) UnityEngine.Object.DestroyImmediate(panel);
                if (composite != null) UnityEngine.Object.DestroyImmediate(composite);
            }

            var report = new StringBuilder()
                .AppendLine("Speaker_Idle natural Play Mode carry comparison")
                .AppendLine("captureKind=" + (final ? "Final" : "Diagnostic"))
                .AppendLine("panelOrder=reference,playerFront,speakerFront,threeQuarter,gripFrontClose,gripThreeQuarterClose")
                .AppendLine("verificationTargetManipulated=False")
                .AppendLine("forcedAnimatorTime=False")
                .AppendLine("speakerPositionFollowError=" + Num(positionError))
                .AppendLine("speakerRotationFollowErrorDegrees=" + Num(rotationError))
                .AppendLine("maximumLeftArmPoseRotationErrorDegrees=" +
                    Num(poseRotationError))
                .AppendLine("speakerTracksLeftShoulder=True")
                .AppendLine("torsoHeadRightArmLowerBodyChanged=False")
                .AppendLine("embeddedTexturesPresent=4")
                .AppendLine("embeddedMaterialPresent=True");
            WriteText(final ? "final_runtime.txt" : "diagnostic_runtime.txt",
                report.ToString());
            UnityConsoleDiagnostics.AssertNoErrors();
            EditorApplication.ExitPlaymode();
        }

        internal static void ExtractEmbeddedAssets()
        {
            RequireEditMode();
            EnsureAssetFolder(TextureFolder);
            EnsureAssetFolder(MaterialFolder);
            ModelImporter importer = AssetImporter.GetAtPath(ModelAssetPath) as ModelImporter ??
                throw new InvalidOperationException("PortableSpeaker ModelImporter is missing.");
            if (!importer.ExtractTextures(Absolute(TextureFolder)))
                throw new InvalidOperationException(
                    "PortableSpeaker embedded texture extraction failed.");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            Material embedded = AssetDatabase.LoadAllAssetsAtPath(ModelAssetPath)
                .OfType<Material>().SingleOrDefault() ??
                throw new InvalidOperationException("PortableSpeaker embedded material is missing.");
            Material external = AssetDatabase.LoadAssetAtPath<Material>(MaterialAssetPath);
            if (external == null)
            {
                external = new Material(embedded) { name = "PortableSpeaker" };
                AssetDatabase.CreateAsset(external, MaterialAssetPath);
            }
            else
            {
                EditorUtility.CopySerialized(embedded, external);
                external.name = "PortableSpeaker";
                EditorUtility.SetDirty(external);
            }

            Texture baseColor = RequireTexture("base_color");
            Texture normal = RequireTexture("normal");
            Texture metallic = RequireTexture("texture_0_metallic");
            RequireTexture("texture_0_roughness");
            if (external.HasProperty("_BaseMap")) external.SetTexture("_BaseMap", baseColor);
            if (external.HasProperty("_MainTex")) external.SetTexture("_MainTex", baseColor);
            if (external.HasProperty("_BumpMap"))
            {
                external.SetTexture("_BumpMap", normal);
                external.EnableKeyword("_NORMALMAP");
            }
            if (external.HasProperty("_MetallicGlossMap"))
            {
                external.SetTexture("_MetallicGlossMap", metallic);
                external.EnableKeyword("_METALLICSPECGLOSSMAP");
            }
            EditorUtility.SetDirty(external);
            AssetDatabase.SaveAssets();

            importer = AssetImporter.GetAtPath(ModelAssetPath) as ModelImporter ??
                throw new InvalidOperationException("PortableSpeaker ModelImporter disappeared.");
            importer.materialSearch = ModelImporterMaterialSearch.Local;
            importer.AddRemap(
                new AssetImporter.SourceAssetIdentifier(typeof(Material), embedded.name),
                external);
            importer.SaveAndReimport();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            var report = new StringBuilder()
                .AppendLine("PortableSpeaker embedded asset extraction")
                .AppendLine("sourceModelChanged=False")
                .AppendLine("textureFolder=" + TextureFolder)
                .AppendLine("materialPath=" + MaterialAssetPath);
            foreach (string path in AssetDatabase.FindAssets("t:Texture", new[] { TextureFolder })
                         .Select(AssetDatabase.GUIDToAssetPath)
                         .OrderBy(item => item, StringComparer.Ordinal))
                report.AppendLine("extractedTexture=" + path);
            foreach (string dependency in AssetDatabase.GetDependencies(ModelAssetPath, true)
                         .OrderBy(item => item, StringComparer.Ordinal))
                report.AppendLine("dependency=" + dependency);
            WriteText("embedded_assets.txt", report.ToString());
            UnityConsoleDiagnostics.AssertNoErrors();
            Debug.Log("[SpeakerIdleCarry] Embedded assets extracted. " +
                report.ToString().Replace('\n', ' '));
        }

        internal static void CaptureModelViews()
        {
            RequireEditMode();
            RequireScene();
            GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ModelAssetPath) ??
                throw new InvalidOperationException("PortableSpeaker model asset is missing.");
            GameObject instance = UnityEngine.Object.Instantiate(modelAsset);
            instance.name = "PortableSpeaker_ModelInspection";
            instance.hideFlags = HideFlags.HideAndDontSave;
            SetLayerRecursively(instance, 31);
            try
            {
                Bounds bounds = CalculateWorldBounds(instance);
                Vector3[] directions =
                {
                    Vector3.forward, Vector3.back, Vector3.right,
                    Vector3.left, Vector3.down, Vector3.up
                };
                Vector3[] upVectors =
                {
                    Vector3.up, Vector3.up, Vector3.up,
                    Vector3.up, Vector3.forward, Vector3.forward
                };
                Texture2D[] panels = new Texture2D[directions.Length];
                try
                {
                    for (int index = 0; index < panels.Length; index++)
                        panels[index] = CaptureIsolatedView(
                            bounds, directions[index], upVectors[index]);
                    Texture2D composite = new Texture2D(
                        panels[0].width * 3,
                        panels[0].height * 2,
                        TextureFormat.RGBA32,
                        false);
                    composite.SetPixels(new Color[composite.width * composite.height]);
                    for (int index = 0; index < panels.Length; index++)
                    {
                        int column = index % 3;
                        int row = 1 - index / 3;
                        composite.SetPixels(
                            column * panels[index].width,
                            row * panels[index].height,
                            panels[index].width,
                            panels[index].height,
                            panels[index].GetPixels());
                    }
                    composite.Apply(false, false);
                    File.WriteAllBytes(Absolute(ModelViewsPath), composite.EncodeToPNG());
                    UnityEngine.Object.DestroyImmediate(composite);
                }
                finally
                {
                    foreach (Texture2D panel in panels)
                        if (panel != null) UnityEngine.Object.DestroyImmediate(panel);
                }
                WriteText("model_views.txt",
                    "panelOrder=front(+Z),back(-Z),right(+X),left(-X),top(-Y),bottom(+Y)" +
                    Environment.NewLine +
                    "verificationTargetManipulated=False" + Environment.NewLine +
                    "modelBoundsCenter=" + Vec(bounds.center) + Environment.NewLine +
                    "modelBoundsSize=" + Vec(bounds.size) + Environment.NewLine);
                UnityConsoleDiagnostics.AssertNoErrors();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        internal static void InspectHandleGeometry()
        {
            RequireEditMode();
            RequireScene();
            GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ModelAssetPath) ??
                throw new InvalidOperationException("PortableSpeaker model asset is missing.");
            GameObject instance = UnityEngine.Object.Instantiate(modelAsset);
            instance.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                MeshFilter filter = instance.GetComponentInChildren<MeshFilter>(true) ??
                    throw new InvalidOperationException("PortableSpeaker mesh is missing.");
                Vector3[] points = filter.sharedMesh.vertices
                    .Select(filter.transform.TransformPoint).ToArray();
                Vector3[] candidates = points.Where(point =>
                    Mathf.Abs(point.x) <= 0.34f && point.y >= 0.16f).ToArray();
                if (candidates.Length == 0)
                    throw new InvalidOperationException("PortableSpeaker handle candidates are empty.");
                var report = new StringBuilder()
                    .AppendLine("PortableSpeaker upper-center handle geometry")
                    .AppendLine("verificationTargetManipulated=False")
                    .AppendLine("candidateCount=" + candidates.Length)
                    .AppendLine("candidateMin=" + Vec(new Vector3(
                        candidates.Min(item => item.x),
                        candidates.Min(item => item.y),
                        candidates.Min(item => item.z))))
                    .AppendLine("candidateMax=" + Vec(new Vector3(
                        candidates.Max(item => item.x),
                        candidates.Max(item => item.y),
                        candidates.Max(item => item.z))));
                foreach (IGrouping<int, Vector3> group in candidates
                             .GroupBy(point => Mathf.RoundToInt(point.z / 0.025f))
                             .OrderByDescending(group => group.Count()))
                {
                    Vector3 average = new Vector3(
                        group.Average(item => item.x),
                        group.Average(item => item.y),
                        group.Average(item => item.z));
                    report.AppendLine("zCluster=" + Num(group.Key * 0.025f) +
                        "|count=" + group.Count() + "|average=" + Vec(average) +
                        "|minY=" + Num(group.Min(item => item.y)) +
                        "|maxY=" + Num(group.Max(item => item.y)));
                }
                WriteText("handle_geometry.txt", report.ToString());
                UnityConsoleDiagnostics.AssertNoErrors();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        internal static void InspectSources()
        {
            RequireEditMode();
            Scene scene = RequireScene();
            GameObject target = FindUnique(scene, TargetName);
            GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ModelAssetPath) ??
                throw new InvalidOperationException("PortableSpeaker FBX did not import as a model.");
            UnityEngine.Object[] representations = AssetDatabase.LoadAllAssetsAtPath(ModelAssetPath);
            Renderer[] modelRenderers = modelAsset.GetComponentsInChildren<Renderer>(true);
            MeshFilter[] meshFilters = modelAsset.GetComponentsInChildren<MeshFilter>(true);
            SkinnedMeshRenderer[] skinnedRenderers =
                modelAsset.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            Material[] materials = modelRenderers.SelectMany(item => item.sharedMaterials)
                .Where(item => item != null).Distinct().ToArray();

            var report = new StringBuilder()
                .AppendLine("Speaker_Idle source inspection")
                .AppendLine("verificationTargetManipulated=False")
                .AppendLine("scene=" + scene.path)
                .AppendLine("sceneDirty=" + scene.isDirty)
                .AppendLine("target=" + target.name)
                .AppendLine("targetGlobalId=" +
                    GlobalObjectId.GetGlobalObjectIdSlow(target))
                .AppendLine("modelAssetPath=" + ModelAssetPath)
                .AppendLine("modelRepresentationCount=" + representations.Length)
                .AppendLine("modelRendererCount=" + modelRenderers.Length)
                .AppendLine("modelMeshFilterCount=" + meshFilters.Length)
                .AppendLine("modelSkinnedRendererCount=" + skinnedRenderers.Length)
                .AppendLine("modelMaterialCount=" + materials.Length)
                .AppendLine("modelDependencyCount=" +
                    AssetDatabase.GetDependencies(ModelAssetPath, true).Length);

            foreach (string dependency in AssetDatabase.GetDependencies(ModelAssetPath, true)
                         .OrderBy(item => item, StringComparer.Ordinal))
                report.AppendLine("dependency=" + dependency);
            foreach (UnityEngine.Object representation in representations
                         .OrderBy(item => item.GetType().Name, StringComparer.Ordinal)
                         .ThenBy(item => item.name, StringComparer.Ordinal))
                report.AppendLine("representation=" + representation.GetType().Name + "|" +
                    representation.name + "|" + AssetDatabase.GetAssetPath(representation));

            foreach (Transform item in modelAsset.GetComponentsInChildren<Transform>(true)
                         .OrderBy(item => AnimationUtility.CalculateTransformPath(
                             item, modelAsset.transform), StringComparer.Ordinal))
            {
                string path = AnimationUtility.CalculateTransformPath(item, modelAsset.transform);
                report.AppendLine("modelTransform=" + path + "|localPosition=" +
                    Vec(item.localPosition) + "|localRotation=" + Quat(item.localRotation) +
                    "|localScale=" + Vec(item.localScale));
            }

            foreach (MeshFilter filter in meshFilters)
            {
                Mesh mesh = filter.sharedMesh;
                report.AppendLine("meshFilter=" +
                    AnimationUtility.CalculateTransformPath(filter.transform, modelAsset.transform) +
                    "|mesh=" + (mesh == null ? "null" : mesh.name) +
                    "|vertices=" + (mesh == null ? 0 : mesh.vertexCount) +
                    "|subMeshes=" + (mesh == null ? 0 : mesh.subMeshCount) +
                    "|boundsCenter=" + (mesh == null ? "null" : Vec(mesh.bounds.center)) +
                    "|boundsSize=" + (mesh == null ? "null" : Vec(mesh.bounds.size)));
            }
            foreach (SkinnedMeshRenderer renderer in skinnedRenderers)
            {
                Mesh mesh = renderer.sharedMesh;
                report.AppendLine("skinnedMesh=" +
                    AnimationUtility.CalculateTransformPath(renderer.transform, modelAsset.transform) +
                    "|mesh=" + (mesh == null ? "null" : mesh.name) +
                    "|vertices=" + (mesh == null ? 0 : mesh.vertexCount) +
                    "|subMeshes=" + (mesh == null ? 0 : mesh.subMeshCount));
            }

            foreach (Material material in materials.OrderBy(item => item.name, StringComparer.Ordinal))
            {
                report.AppendLine("material=" + material.name + "|assetPath=" +
                    AssetDatabase.GetAssetPath(material) + "|shader=" +
                    (material.shader == null ? "null" : material.shader.name));
                foreach (string propertyName in material.GetTexturePropertyNames())
                {
                    Texture texture = material.GetTexture(propertyName);
                    if (texture == null) continue;
                    report.AppendLine("materialTexture=" + material.name + "|property=" +
                        propertyName + "|texture=" + texture.name + "|assetPath=" +
                        AssetDatabase.GetAssetPath(texture));
                }
            }

            string[] protectedNames = { "Hips", "Spine02", "Spine01", "Spine", "Neck",
                "Head", "RightShoulder", "RightArm", "RightForeArm", "RightHand" };
            foreach (Transform item in target.GetComponentsInChildren<Transform>(true)
                         .Where(item => item.name.StartsWith("Left", StringComparison.Ordinal) ||
                                        protectedNames.Contains(item.name))
                         .OrderBy(item => AnimationUtility.CalculateTransformPath(
                             item, target.transform), StringComparer.Ordinal))
            {
                string path = AnimationUtility.CalculateTransformPath(item, target.transform);
                report.AppendLine("targetTransform=" + path + "|name=" + item.name +
                    "|localPosition=" + Vec(item.localPosition) +
                    "|localRotation=" + Quat(item.localRotation) +
                    "|localScale=" + Vec(item.localScale) +
                    "|worldPosition=" + Vec(item.position));
            }

            WriteText("source_inspection.txt", report.ToString());
            UnityConsoleDiagnostics.AssertNoErrors();
            Debug.Log("[SpeakerIdleCarry] Sources inspected read-only. " +
                report.ToString().Replace('\n', ' '));
        }

        private static SpeakerBoneRotation[] CaptureBonePose(
            Transform root,
            Transform targetRoot)
        {
            return root.GetComponentsInChildren<Transform>(true)
                .OrderBy(item => AnimationUtility.CalculateTransformPath(
                    item, targetRoot), StringComparer.Ordinal)
                .Select(item => new SpeakerBoneRotation(
                    AnimationUtility.CalculateTransformPath(item, targetRoot),
                    item.localRotation))
                .ToArray();
        }

        private static float MaximumPoseRotationError(
            Transform targetRoot,
            SpeakerIdleCarryProfile profile)
        {
            float maximum = 0f;
            foreach (SpeakerBoneRotation pose in profile.LeftArmPose)
                maximum = Mathf.Max(maximum, Quaternion.Angle(
                    RequirePath(targetRoot, pose.Path).localRotation,
                    pose.LocalRotation));
            return maximum;
        }

        private static Texture2D LoadReferencePanel()
        {
            if (!File.Exists(ReferenceImagePath))
                throw new FileNotFoundException(
                    "Speaker pose reference image is missing.", ReferenceImagePath);
            Texture2D source = new Texture2D(2, 2, TextureFormat.RGB24, false);
            if (!ImageConversion.LoadImage(source, File.ReadAllBytes(ReferenceImagePath), false))
            {
                UnityEngine.Object.DestroyImmediate(source);
                throw new InvalidOperationException("Speaker reference image failed to load.");
            }
            RenderTexture render = RenderTexture.GetTemporary(
                512, 512, 0, RenderTextureFormat.ARGB32);
            RenderTexture previous = RenderTexture.active;
            try
            {
                Graphics.Blit(source, render);
                RenderTexture.active = render;
                Texture2D result = new Texture2D(512, 512, TextureFormat.RGB24, false);
                result.ReadPixels(new Rect(0, 0, 512, 512), 0, 0);
                result.Apply(false, false);
                return result;
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(render);
                UnityEngine.Object.DestroyImmediate(source);
            }
        }

        private static Texture2D CaptureTargetView(
            Scene scene,
            Bounds bounds,
            Vector3 directionFromTarget)
        {
            GameObject cameraObject = new GameObject("SpeakerIdleCarry_ReadOnlyCamera");
            GameObject lightObject = new GameObject("SpeakerIdleCarry_ReadOnlyLight");
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            SceneManager.MoveGameObjectToScene(lightObject, scene);
            RenderTexture render = RenderTexture.GetTemporary(
                512, 512, 24, RenderTextureFormat.ARGB32);
            RenderTexture previous = RenderTexture.active;
            try
            {
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = Mathf.Max(
                    bounds.extents.y * 1.12f,
                    bounds.extents.x * 1.12f);
                camera.nearClipPlane = 0.03f;
                camera.farClipPlane = 5.2f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.025f, 0.03f, 0.04f, 1f);
                Vector3 direction = Vector3.ProjectOnPlane(
                    directionFromTarget, Vector3.up).normalized;
                camera.transform.SetPositionAndRotation(
                    bounds.center + direction * 4f,
                    Quaternion.LookRotation(-direction, Vector3.up));
                camera.targetTexture = render;
                Light light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.1f;
                light.transform.rotation = Quaternion.Euler(42f, -28f, 0f);
                camera.Render();
                RenderTexture.active = render;
                Texture2D image = new Texture2D(512, 512, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, 512, 512), 0, 0);
                image.Apply(false, false);
                return image;
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(render);
                UnityEngine.Object.DestroyImmediate(lightObject);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static void ApplyBonePose(
            Transform targetRoot,
            IEnumerable<SpeakerBoneRotation> pose)
        {
            foreach (SpeakerBoneRotation item in pose)
                RequirePath(targetRoot, item.Path).localRotation = item.LocalRotation;
        }

        private static Vector3 CalculateUpperHandleCenterInModelSpace(Transform model)
        {
            GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ModelAssetPath) ??
                throw new InvalidOperationException("PortableSpeaker model asset is missing.");
            MeshFilter sourceFilter = modelAsset.GetComponentInChildren<MeshFilter>(true) ??
                throw new InvalidOperationException("PortableSpeaker source mesh is missing.");
            MeshFilter instanceFilter = model.GetComponentInChildren<MeshFilter>(true) ??
                throw new InvalidOperationException("PortableSpeaker mesh is missing.");
            Vector3[] sourceVertices = sourceFilter.sharedMesh.vertices;
            int[] handleIndices = sourceVertices
                .Select((vertex, index) => new
                {
                    Index = index,
                    Point = sourceFilter.transform.TransformPoint(vertex)
                })
                .Where(item => Mathf.Abs(item.Point.x) <= 0.12f &&
                    item.Point.y >= 0.39f && Mathf.Abs(item.Point.z) <= 0.09f)
                .Select(item => item.Index)
                .ToArray();
            if (handleIndices.Length == 0)
                throw new InvalidOperationException(
                    "PortableSpeaker upper handle center could not be resolved.");
            Vector3[] instanceVertices = instanceFilter.sharedMesh.vertices;
            Vector3 handleWorldPosition = handleIndices
                .Select(index => instanceFilter.transform.TransformPoint(instanceVertices[index]))
                .Aggregate(Vector3.zero, (sum, point) => sum + point) / handleIndices.Length;
            return model.InverseTransformPoint(handleWorldPosition);
        }

        private static Vector3 LeftPalmCenter(Transform targetRoot)
        {
            string[] knucklePaths =
            {
                LeftHandPath + "/LeftIndexProximal",
                LeftHandPath + "/LeftMiddleProximal",
                LeftHandPath + "/LeftRingProximal",
                LeftHandPath + "/LeftLittleProximal"
            };
            return knucklePaths
                .Select(path => RequirePath(targetRoot, path).position)
                .Aggregate(Vector3.zero, (sum, point) => sum + point) /
                knucklePaths.Length;
        }

        private static Vector3 LeftFingerDirection(Transform targetRoot)
        {
            Transform hand = RequirePath(targetRoot, LeftHandPath);
            Transform middle = RequirePath(
                targetRoot, LeftHandPath + "/LeftMiddleProximal");
            return (middle.position - hand.position).normalized;
        }

        private static Vector3 LeftPalmNormal(Transform targetRoot)
        {
            Transform index = RequirePath(
                targetRoot, LeftHandPath + "/LeftIndexProximal");
            Transform little = RequirePath(
                targetRoot, LeftHandPath + "/LeftLittleProximal");
            Vector3 width = (little.position - index.position).normalized;
            Vector3 normal = Vector3.Cross(LeftFingerDirection(targetRoot), width).normalized;
            if (normal.sqrMagnitude < 0.999f)
                throw new InvalidOperationException("Speaker_Idle palm normal is degenerate.");
            return normal;
        }

        private static float LeftWristStraightness(Transform targetRoot)
        {
            Transform foreArm = RequirePath(targetRoot, LeftForeArmPath);
            Transform hand = RequirePath(targetRoot, LeftHandPath);
            return Vector3.Angle(
                (hand.position - foreArm.position).normalized,
                LeftFingerDirection(targetRoot));
        }

        private static void AlignLeftPalm(
            Transform foreArm,
            Transform hand,
            Transform targetRoot,
            Vector3 desiredPalmNormal)
        {
            Vector3 foreArmAxis = (hand.position - foreArm.position).normalized;
            Vector3 currentProjected = Vector3.ProjectOnPlane(
                LeftPalmNormal(targetRoot), foreArmAxis).normalized;
            Vector3 desiredProjected = Vector3.ProjectOnPlane(
                desiredPalmNormal, foreArmAxis).normalized;
            if (currentProjected.sqrMagnitude > 0.0001f &&
                desiredProjected.sqrMagnitude > 0.0001f)
            {
                float twist = Vector3.SignedAngle(
                    currentProjected, desiredProjected, foreArmAxis);
                foreArm.rotation = Quaternion.AngleAxis(twist, foreArmAxis) * foreArm.rotation;
            }
            Quaternion residual = Quaternion.FromToRotation(
                LeftPalmNormal(targetRoot), desiredPalmNormal);
            hand.rotation = residual * hand.rotation;
        }

        private static float MaximumFingerPoseError(
            Transform targetRoot,
            SpeakerIdleCarryProfile profile)
        {
            float maximum = 0f;
            foreach (SpeakerBoneRotation sourcePose in profile.SourceLeftArmPose)
            {
                if (!sourcePose.Path.StartsWith(
                        LeftHandPath + "/", StringComparison.Ordinal))
                    continue;
                maximum = Mathf.Max(maximum, Quaternion.Angle(
                    RequirePath(targetRoot, sourcePose.Path).localRotation,
                    sourcePose.LocalRotation));
            }
            return maximum;
        }

        private static void SolveTwoBone(
            Transform upperArm,
            Transform foreArm,
            Transform hand,
            Vector3 target,
            Vector3 pole)
        {
            Vector3 shoulderPosition = upperArm.position;
            float upperLength = Vector3.Distance(upperArm.position, foreArm.position);
            float lowerLength = Vector3.Distance(foreArm.position, hand.position);
            Vector3 toTarget = target - shoulderPosition;
            float targetDistance = toTarget.magnitude;
            float minimumDistance = Mathf.Abs(upperLength - lowerLength) + 0.0001f;
            float maximumDistance = upperLength + lowerLength - 0.0001f;
            float solvedDistance = Mathf.Clamp(targetDistance, minimumDistance, maximumDistance);
            Vector3 targetDirection = toTarget.normalized;
            Vector3 poleDirection = Vector3.ProjectOnPlane(
                pole - shoulderPosition, targetDirection).normalized;
            if (poleDirection.sqrMagnitude < 0.0001f)
                poleDirection = Vector3.ProjectOnPlane(Vector3.forward, targetDirection).normalized;
            float cosine = Mathf.Clamp(
                (upperLength * upperLength + solvedDistance * solvedDistance -
                 lowerLength * lowerLength) /
                (2f * upperLength * solvedDistance),
                -1f,
                1f);
            float sine = Mathf.Sqrt(Mathf.Max(0f, 1f - cosine * cosine));
            Vector3 desiredElbow = shoulderPosition +
                targetDirection * (cosine * upperLength) +
                poleDirection * (sine * upperLength);
            upperArm.rotation = Quaternion.FromToRotation(
                foreArm.position - upperArm.position,
                desiredElbow - upperArm.position) * upperArm.rotation;
            Vector3 solvedTarget = shoulderPosition + targetDirection * solvedDistance;
            foreArm.rotation = Quaternion.FromToRotation(
                hand.position - foreArm.position,
                solvedTarget - foreArm.position) * foreArm.rotation;
        }

        private static void AlignHandToHandle(
            Transform targetRoot,
            Transform hand,
            Transform index,
            Transform little,
            Transform middle,
            Vector3 handleAxis)
        {
            Vector3 currentWidth = (little.position - index.position).normalized;
            Vector3 desiredWidth = handleAxis.normalized;
            if (Vector3.Dot(currentWidth, desiredWidth) < 0f) desiredWidth = -desiredWidth;
            hand.rotation = Quaternion.FromToRotation(currentWidth, desiredWidth) * hand.rotation;

            Vector3 currentFinger = Vector3.ProjectOnPlane(
                middle.position - hand.position, desiredWidth).normalized;
            Vector3 desiredFinger = Vector3.ProjectOnPlane(
                Vector3.down, desiredWidth).normalized;
            float angle = Vector3.SignedAngle(currentFinger, desiredFinger, desiredWidth);
            hand.rotation = Quaternion.AngleAxis(angle, desiredWidth) * hand.rotation;
        }

        private static void CurlFingers(Transform targetRoot)
        {
            CurlFinger(targetRoot, "LeftIndex", 12f, 62f, 52f);
            CurlFinger(targetRoot, "LeftMiddle", 14f, 68f, 56f);
            CurlFinger(targetRoot, "LeftRing", 16f, 70f, 58f);
            CurlFinger(targetRoot, "LeftLittle", 18f, 72f, 60f);
            Transform thumbProximal = RequirePath(
                targetRoot, LeftHandPath + "/LeftThumbProximal");
            Transform thumbIntermediate = RequirePath(
                targetRoot,
                LeftHandPath + "/LeftThumbProximal/LeftThumbIntermediate");
            Transform thumbDistal = RequirePath(
                targetRoot,
                LeftHandPath +
                "/LeftThumbProximal/LeftThumbIntermediate/LeftThumbDistal");
            thumbProximal.localRotation *= Quaternion.Euler(-12f, 4f, 28f);
            thumbIntermediate.localRotation *= Quaternion.Euler(0f, 0f, 42f);
            thumbDistal.localRotation *= Quaternion.Euler(0f, 0f, 32f);
        }

        private static void CurlFinger(
            Transform targetRoot,
            string prefix,
            float proximal,
            float intermediate,
            float distal)
        {
            Transform proximalBone = RequirePath(
                targetRoot, LeftHandPath + "/" + prefix + "Proximal");
            Transform intermediateBone = RequirePath(
                targetRoot,
                LeftHandPath + "/" + prefix + "Proximal/" +
                prefix + "Intermediate");
            Transform distalBone = RequirePath(
                targetRoot,
                LeftHandPath + "/" + prefix + "Proximal/" +
                prefix + "Intermediate/" + prefix + "Distal");
            proximalBone.localRotation *= Quaternion.AngleAxis(proximal, Vector3.forward);
            intermediateBone.localRotation *= Quaternion.AngleAxis(intermediate, Vector3.forward);
            distalBone.localRotation *= Quaternion.AngleAxis(distal, Vector3.forward);
        }

        private static float HandHandleAxisError(
            Transform targetRoot,
            SpeakerLeftShoulderFollowBehaviour behaviour)
        {
            Transform index = RequirePath(
                targetRoot, LeftHandPath + "/LeftIndexProximal");
            Transform little = RequirePath(
                targetRoot, LeftHandPath + "/LeftLittleProximal");
            Vector3 handWidth = (little.position - index.position).normalized;
            Vector3 handleAxis = behaviour.HandleWorldAxis();
            return Mathf.Min(
                Vector3.Angle(handWidth, handleAxis),
                Vector3.Angle(-handWidth, handleAxis));
        }

        private static void RequireSpeakerAssets(
            SpeakerLeftShoulderFollowBehaviour behaviour)
        {
            if (behaviour.Profile == null || behaviour.SpeakerHolder == null)
                throw new InvalidOperationException("Speaker carry profile or preview is missing.");
            Renderer[] renderers = behaviour.SpeakerHolder
                .GetComponentsInChildren<Renderer>(true);
            if (renderers.Length != 1 || renderers[0].sharedMaterial == null)
                throw new InvalidOperationException(
                    "PortableSpeaker must have one configured renderer and material.");
            if (AssetDatabase.GetAssetPath(renderers[0].sharedMaterial) != MaterialAssetPath)
                throw new InvalidOperationException(
                    "PortableSpeaker renderer is not using its extracted material.");
            string[] texturePaths = AssetDatabase.FindAssets("t:Texture", new[] { TextureFolder })
                .Select(AssetDatabase.GUIDToAssetPath).ToArray();
            if (texturePaths.Length != 4)
                throw new InvalidOperationException(
                    "PortableSpeaker must retain all four embedded textures.");
            if (AssetDatabase.GetDependencies(ModelAssetPath, true)
                .Any(path => path.IndexOf("AuxiliaryBattery", StringComparison.OrdinalIgnoreCase) >= 0))
                throw new InvalidOperationException(
                    "PortableSpeaker still references an unrelated battery texture.");
        }

        private static string ProtectedHierarchySignature(Transform targetRoot)
        {
            var result = new StringBuilder();
            foreach (Transform item in targetRoot.GetComponentsInChildren<Transform>(true)
                         .OrderBy(item => AnimationUtility.CalculateTransformPath(
                             item, targetRoot), StringComparer.Ordinal))
            {
                string path = AnimationUtility.CalculateTransformPath(item, targetRoot);
                if (path == LeftArmPath ||
                    path.StartsWith(LeftArmPath + "/", StringComparison.Ordinal) ||
                    path == "Speaker_Prop" ||
                    path.StartsWith("Speaker_Prop/", StringComparison.Ordinal))
                    continue;
                result.Append(path).Append('|').Append(item.gameObject.activeSelf).Append('|')
                    .Append(item.GetSiblingIndex()).Append('|').Append(Vec(item.localPosition))
                    .Append('|').Append(Quat(item.localRotation)).Append('|')
                    .Append(Vec(item.localScale)).AppendLine();
                foreach (MeshFilter filter in item.GetComponents<MeshFilter>())
                    result.Append("MF|").Append(path).Append('|')
                        .Append(AssetIdentity(filter.sharedMesh)).AppendLine();
                foreach (SkinnedMeshRenderer renderer in
                         item.GetComponents<SkinnedMeshRenderer>())
                    result.Append("SMR|").Append(path).Append('|')
                        .Append(AssetIdentity(renderer.sharedMesh)).Append('|')
                        .Append(string.Join(",", renderer.sharedMaterials
                            .Select(AssetIdentity))).AppendLine();
                foreach (MeshRenderer renderer in item.GetComponents<MeshRenderer>())
                    result.Append("MR|").Append(path).Append('|')
                        .Append(string.Join(",", renderer.sharedMaterials
                            .Select(AssetIdentity))).AppendLine();
            }
            return result.ToString();
        }

        private static string SpeakerTransformAndAppearanceSignature(
            SpeakerLeftShoulderFollowBehaviour behaviour)
        {
            Transform holder = behaviour.SpeakerHolder != null
                ? behaviour.SpeakerHolder.transform
                : throw new MissingReferenceException("Speaker holder is missing.");
            Transform model = RequireSpeakerModel(behaviour);
            var result = new StringBuilder()
                .Append("holder|").Append(Vec(holder.localPosition)).Append('|')
                .Append(Quat(holder.localRotation)).Append('|')
                .AppendLine(Vec(holder.localScale))
                .Append("model|").Append(Vec(model.localPosition)).Append('|')
                .Append(Quat(model.localRotation)).Append('|')
                .AppendLine(Vec(model.localScale));
            foreach (Renderer renderer in holder.GetComponentsInChildren<Renderer>(true)
                         .OrderBy(item => AnimationUtility.CalculateTransformPath(
                             item.transform, holder), StringComparer.Ordinal))
            {
                result.Append("renderer|")
                    .Append(AnimationUtility.CalculateTransformPath(renderer.transform, holder))
                    .Append('|').Append(renderer.enabled).Append('|');
                if (renderer is SkinnedMeshRenderer skinned)
                    result.Append(AssetIdentity(skinned.sharedMesh));
                else
                    result.Append(AssetIdentity(renderer.GetComponent<MeshFilter>()?.sharedMesh));
                result.Append('|').AppendLine(string.Join(",",
                    renderer.sharedMaterials.Select(AssetIdentity)));
            }
            return result.ToString();
        }

        private static string AssetIdentity(UnityEngine.Object asset)
        {
            return asset == null ? "null" : AssetDatabase.GetAssetPath(asset) + "#" + asset.name;
        }

        private static string BaselineValue(string[] lines, string key)
        {
            string prefix = key + "=";
            string line = lines.SingleOrDefault(item => item.StartsWith(
                prefix, StringComparison.Ordinal));
            if (line == null)
                throw new InvalidOperationException("Speaker baseline is missing " + key + ".");
            return line.Substring(prefix.Length);
        }

        private static string ComputeAssetHash(string assetPath)
        {
            using (SHA256 sha = SHA256.Create())
            using (FileStream stream = File.OpenRead(Absolute(assetPath)))
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
        }

        private static string ComputeStringHash(string value)
        {
            using (SHA256 sha = SHA256.Create())
                return BitConverter.ToString(
                    sha.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-", string.Empty);
        }

        private static void RequireStringEqual(string expected, string actual, string label)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new InvalidOperationException(label + " changed unexpectedly.");
        }

        private static Transform RequirePath(Transform root, string path)
        {
            return root.Find(path) ??
                throw new InvalidOperationException(root.name + " path is missing: " + path + ".");
        }

        private static Scene RequireScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
                throw new InvalidOperationException(
                    "CargoRunMvp must be active. ActiveScene=" + scene.path);
            return scene;
        }

        private static Texture RequireTexture(string assetName)
        {
            string[] paths = AssetDatabase.FindAssets("t:Texture", new[] { TextureFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => string.Equals(
                    Path.GetFileNameWithoutExtension(path), assetName,
                    StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (paths.Length != 1)
                throw new InvalidOperationException(
                    "Expected one extracted texture named " + assetName + "; found " +
                    paths.Length + ".");
            return AssetDatabase.LoadAssetAtPath<Texture>(paths[0]) ??
                throw new InvalidOperationException("Texture failed to load: " + paths[0]);
        }

        private static void EnsureAssetFolder(string path)
        {
            string[] segments = path.Split('/');
            string current = segments[0];
            for (int index = 1; index < segments.Length; index++)
            {
                string next = current + "/" + segments[index];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, segments[index]);
                current = next;
            }
        }

        private static void SetLayerRecursively(GameObject root, int layer)
        {
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
                item.gameObject.layer = layer;
        }

        private static Bounds CalculateWorldBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                throw new InvalidOperationException("PortableSpeaker has no renderer bounds.");
            Bounds bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++)
                bounds.Encapsulate(renderers[index].bounds);
            return bounds;
        }

        private static Texture2D CaptureIsolatedView(
            Bounds bounds,
            Vector3 direction,
            Vector3 up)
        {
            const int size = 512;
            GameObject cameraObject = new GameObject("SpeakerModelInspectionCamera")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            GameObject lightObject = new GameObject("SpeakerModelInspectionLight")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            RenderTexture renderTexture = null;
            RenderTexture previous = RenderTexture.active;
            try
            {
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.018f, 0.025f, 0.035f, 1f);
                camera.orthographic = true;
                camera.orthographicSize = Mathf.Max(bounds.extents.magnitude * 1.05f, 0.1f);
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = Mathf.Max(bounds.size.magnitude * 8f, 10f);
                camera.cullingMask = 1 << 31;
                camera.transform.position = bounds.center -
                    direction.normalized * Mathf.Max(bounds.size.magnitude * 3f, 3f);
                camera.transform.rotation = Quaternion.LookRotation(direction, up);

                Light light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.35f;
                light.color = new Color(1f, 0.96f, 0.9f);
                light.cullingMask = 1 << 31;
                light.transform.rotation = Quaternion.LookRotation(
                    direction + new Vector3(-0.35f, -0.45f, 0.25f));
                renderTexture = new RenderTexture(size, size, 24, RenderTextureFormat.ARGB32);
                camera.targetTexture = renderTexture;
                camera.Render();
                RenderTexture.active = renderTexture;
                Texture2D image = new Texture2D(size, size, TextureFormat.RGBA32, false);
                image.ReadPixels(new Rect(0, 0, size, size), 0, 0);
                image.Apply(false, false);
                return image;
            }
            finally
            {
                RenderTexture.active = previous;
                if (renderTexture != null)
                {
                    renderTexture.Release();
                    UnityEngine.Object.DestroyImmediate(renderTexture);
                }
                UnityEngine.Object.DestroyImmediate(lightObject);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static void RequireEditMode()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Speaker operation requires Edit Mode.");
        }

        private static GameObject FindUnique(Scene scene, string objectName)
        {
            GameObject[] matches = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Where(item => item.name == objectName)
                .Select(item => item.gameObject).ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException(
                    "Expected one " + objectName + "; found " + matches.Length + ".");
            return matches[0];
        }

        private static void WriteText(string name, string contents)
        {
            string directory = Absolute(OutputFolder);
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, name), contents, Encoding.UTF8);
        }

        private static void WriteReflectionText(string name, string contents)
        {
            string directory = Absolute(TransformReflectionOutputFolder);
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, name), contents, Encoding.UTF8);
        }

        private static void WriteNeutralText(string name, string contents)
        {
            string directory = Absolute(NeutralHandleHandOutputFolder);
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, name), contents, Encoding.UTF8);
        }

        private static void WritePalmRightText(string name, string contents)
        {
            string directory = Absolute(PalmRightOutputFolder);
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, name), contents, Encoding.UTF8);
        }

        private static string Absolute(string path)
        {
            string root = Directory.GetParent(Application.dataPath)?.FullName ??
                throw new InvalidOperationException("Project root is unavailable.");
            return Path.GetFullPath(Path.Combine(root,
                path.Replace('/', Path.DirectorySeparatorChar)));
        }

        private static string Num(float value) =>
            value.ToString("R", CultureInfo.InvariantCulture);
        private static string Vec(Vector3 value) =>
            Num(value.x) + "," + Num(value.y) + "," + Num(value.z);
        private static string Quat(Quaternion value) =>
            Num(value.x) + "," + Num(value.y) + "," + Num(value.z) + "," + Num(value.w);
    }
}
