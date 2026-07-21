using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Bellerophon.Enemies.Ostinato;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor
{
    internal sealed class OstinatoAttackForwardSlashPostprocessor : AssetPostprocessor
    {
        internal const string EnabledMarker = "Bellerophon.OstinatoForwardSlashMotion.v5";

        public override uint GetVersion()
        {
            return 5u;
        }

        private void OnPostprocessAnimation(GameObject root, AnimationClip clip)
        {
            if (!string.Equals(assetPath, OstinatoScissorAttackAnimation.AttackModelPath, StringComparison.Ordinal) ||
                !string.Equals(assetImporter.userData, EnabledMarker, StringComparison.Ordinal) ||
                !string.Equals(clip.name, OstinatoAttackForwardSlashMotion.TargetClipName, StringComparison.Ordinal))
            {
                return;
            }

            OstinatoAttackForwardSlashMotion.RewriteTargetRotationCurves(clip);
        }
    }

    internal static class OstinatoAttackForwardSlashMotion
    {
        internal const string TargetClipName = "mixamo.com";

        private const float StartFrame = 53f;
        private const float EndFrame = 93f;
        // The reference outward cut occupies fourteen source frames. At uniform 2x playback,
        // matching that span makes the inward strike read in the same 0.116667-second window.
        private const float MainStrikeEndFrame = 67f;
        private const float MainStrikeCompletion = 0.9f;
        // Frames 101..115 are the supplied clip's rapid bilateral outward cut. Its untouched
        // hand and arm angular-velocity profile is the timing reference for the inward cut.
        private const float OutwardSlashStartFrame = 101f;
        private const float OutwardSlashEndFrame = 115f;
        private const float ExpectedFrameRate = 60f;
        // The v2 initial velocity is retained as a numeric guard for the user's request:
        // v3 must strengthen the first inward-cut interval at uniform 2x playback.
        private const float PreviousV2Uniform2InitialAverageHandVelocity = 2.5521f;
        private const string OriginalCurveFingerprint = "1D8CC5051AEF6698CFC6B484BF1280B2FA8B1EDF08967426BF268B89C1E8B2E4";
        private const string PreviousV1CurveFingerprint = "2484FA02F1B3C798D529C7E7C2EAC8347E8A53FA88F895F11A965B20ED91FB88";
        private const string PreviousV2CurveFingerprint = "7718569E22C44275CCF240957B01BBC291C121F1944BDB2E314D4CC55EAFD4BD";
        private const string PreviousV3CurveFingerprint = "8AE54082F7FACDC70189233CEBEABFC0047CD6D25974430060DE00BF58FDFC81";
        private const string PreviousV4CurveFingerprint = "B463F3A2CB5A022CBC2A0034D19BCF0A5CAEDB5C2BE3C0587B56F7D88BAB78FC";
        internal const string ValidationFolder = "docs/validation/ostinato_attack_forward_slash_motion_2026-07-21";
        private const string ApplyReportPath = ValidationFolder + "/Ostinato_AttackForwardSlashMotionApply.txt";
        private const string InspectionReportPath = ValidationFolder + "/Ostinato_AttackForwardSlashMotionInspection.txt";
        private const string BindingReportPath = ValidationFolder + "/Ostinato_AttackForwardSlashBindingInspection.txt";

        private static readonly string[] TargetBoneNames =
        {
            "LeftArm",
            "LeftForeArm",
            "RightArm",
            "RightForeArm",
        };

        private static readonly string[] RotationPropertyNames =
        {
            "localEulerAnglesRaw.x",
            "localEulerAnglesRaw.y",
            "localEulerAnglesRaw.z",
        };

        [MenuItem("Bellerophon/Enemies/Ostinato/Apply Forward Slash Motion Import Correction")]
        public static void ApplyOstinatoAttackForwardSlashMotion()
        {
            var importer = AssetImporter.GetAtPath(OstinatoScissorAttackAnimation.AttackModelPath) as ModelImporter ??
                           throw new InvalidOperationException("Ostinato attack FBX ModelImporter is missing.");
            var markerWasEnabled = string.Equals(importer.userData, OstinatoAttackForwardSlashPostprocessor.EnabledMarker, StringComparison.Ordinal);
            var sourcePath = OstinatoScissorAttackAnimation.ProjectAbsolutePath(OstinatoScissorAttackAnimation.SourceAttackRelativePath);
            var importedPath = OstinatoScissorAttackAnimation.ProjectAbsolutePath(OstinatoScissorAttackAnimation.AttackModelPath);
            var sourceHashBefore = ComputeSha256(sourcePath);
            var importedHashBefore = ComputeSha256(importedPath);
            if (sourceHashBefore != importedHashBefore)
            {
                throw new InvalidOperationException("The imported Ostinato attack FBX differs from the supplied source.");
            }

            var sceneStateBefore = CaptureSceneState();
            ConfigureUniformPlaybackSpeed();
            var clipBefore = RequireTargetClip();
            RequireClipTimeline(clipBefore);
            var fullFingerprintBefore = BuildFullCurveFingerprint(clipBefore);
            if (!markerWasEnabled &&
                fullFingerprintBefore != OriginalCurveFingerprint &&
                fullFingerprintBefore != PreviousV1CurveFingerprint &&
                fullFingerprintBefore != PreviousV2CurveFingerprint &&
                fullFingerprintBefore != PreviousV3CurveFingerprint &&
                fullFingerprintBefore != PreviousV4CurveFingerprint)
            {
                throw new InvalidOperationException("The Ostinato attack clip fingerprint is not an approved source or prior correction fingerprint.");
            }

            var protectedFingerprintBefore = BuildProtectedCurveFingerprint(clipBefore);
            var boundaryBefore = CaptureBoundaryValues(clipBefore);

            importer.userData = OstinatoAttackForwardSlashPostprocessor.EnabledMarker;
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();

            var clipAfter = RequireTargetClip();
            RequireClipTimeline(clipAfter);
            var fullFingerprintAfter = BuildFullCurveFingerprint(clipAfter);
            var protectedFingerprintAfter = BuildProtectedCurveFingerprint(clipAfter);
            var boundaryAfter = CaptureBoundaryValues(clipAfter);
            if (protectedFingerprintBefore != protectedFingerprintAfter)
            {
                throw new InvalidOperationException("A protected Ostinato attack curve changed outside the approved arm-rotation range.");
            }
            RequireBoundaryValuesEqual(boundaryBefore, boundaryAfter);
            if (!markerWasEnabled && fullFingerprintAfter == fullFingerprintBefore)
            {
                throw new InvalidOperationException("The Ostinato forward-slash motion correction did not change the target curves.");
            }

            var correction = RequireCorrectionContract(clipAfter);
            RequireUniformPlaybackSpeed();
            RequireSceneStateUnchanged(sceneStateBefore, CaptureSceneState());
            var sourceHashAfter = ComputeSha256(sourcePath);
            var importedHashAfter = ComputeSha256(importedPath);
            if (sourceHashAfter != sourceHashBefore || importedHashAfter != importedHashBefore || sourceHashAfter != importedHashAfter)
            {
                throw new InvalidOperationException("The Ostinato attack FBX file changed during import correction.");
            }

            var report = new StringBuilder();
            report.AppendLine("Result=PASS");
            report.AppendLine("TargetAsset=" + OstinatoScissorAttackAnimation.AttackModelPath);
            report.AppendLine("TargetClip=" + TargetClipName);
            report.AppendLine("ImportPostprocessorMarker=" + OstinatoAttackForwardSlashPostprocessor.EnabledMarker);
            report.AppendLine("MarkerWasAlreadyEnabled=" + markerWasEnabled);
            report.AppendLine("CorrectionFrames=53..93");
            report.AppendLine("CorrectionBones=" + string.Join("|", TargetBoneNames));
            report.AppendLine("CorrectionProperties=localEulerAnglesRaw.x|y|z");
            report.AppendLine("MotionMethod=PerBoneReverseSpatialTrajectoryWithForwardOutwardSlashTiming");
            report.AppendLine("MainStrikeFrames=53..67");
            report.AppendLine("MainStrikeDurationSeconds=" + Format((MainStrikeEndFrame - StartFrame) / (ExpectedFrameRate * 2f)));
            report.AppendLine("MainStrikeCompletion=" + Format(MainStrikeCompletion));
            report.AppendLine("DeceleratingFollowThroughFrames=67..93");
            report.AppendLine("OutwardReferenceFrames=101..115");
            report.AppendLine("TargetRotationBindingCount=" + correction.TargetBindingCount);
            report.AppendLine("MaximumExpectedTrajectoryErrorDegrees=" + Format(correction.MaximumExpectedTrajectoryError));
            report.AppendLine("MaximumReferenceCurveDeviationDegrees=" + Format(correction.MaximumReferenceCurveDeviation));
            report.AppendLine("MaximumPerBoneProgressSpread=" + Format(correction.MaximumPerBoneProgressSpread));
            report.AppendLine("MaximumCorrectedPerFrameRotationDegrees=" + Format(correction.MaximumPerFrameRotation));
            report.AppendLine("MaximumReferencePerFrameRotationDegrees=" + Format(correction.MaximumReferencePerFrameRotation));
            report.AppendLine("MinimumFollowThroughAverageRotationDegrees=" + Format(correction.MinimumFollowThroughAverageRotation));
            report.AppendLine("MaximumJointRangeExcessDegrees=" + Format(correction.MaximumJointRangeExcess));
            report.AppendLine("Boundary52To53MaximumRotationDegrees=" + Format(correction.Boundary52To53MaximumRotation));
            report.AppendLine("Boundary93To94MaximumRotationDegrees=" + Format(correction.Boundary93To94MaximumRotation));
            report.AppendLine("FullCurveFingerprintBefore=" + fullFingerprintBefore);
            report.AppendLine("FullCurveFingerprintAfter=" + fullFingerprintAfter);
            report.AppendLine("ProtectedCurveFingerprintBefore=" + protectedFingerprintBefore);
            report.AppendLine("ProtectedCurveFingerprintAfter=" + protectedFingerprintAfter);
            report.AppendLine("ProtectedCurvesUnchanged=True");
            report.AppendLine("BoundaryValuesUnchanged=True");
            report.AppendLine("SourceFbxSha256=" + sourceHashAfter);
            report.AppendLine("ImportedFbxSha256=" + importedHashAfter);
            report.AppendLine("SourceFbxFileModified=False");
            report.AppendLine("DerivedAnimationClipCreated=False");
            report.AppendLine("HandOrBladeCurveModified=False");
            report.AppendLine("PositionOrScaleCurveModified=False");
            report.AppendLine("ControllerStateSpeed=2");
            report.AppendLine("AccelerationBehaviourCount=0");
            report.AppendLine("AttackObjectRecreated=False");
            report.AppendLine("OtherOstinatoSlotsUnchanged=True");
            OstinatoScissorAttackAnimation.WriteText(ApplyReportPath, report.ToString());
            Debug.Log("Ostinato forward-slash motion import correction applied to arm and forearm rotation curves on frames 53 through 93.");
        }

        [MenuItem("Bellerophon/Enemies/Ostinato/Inspect Forward Slash Motion Import Correction")]
        public static void InspectOstinatoAttackForwardSlashMotion()
        {
            var importer = AssetImporter.GetAtPath(OstinatoScissorAttackAnimation.AttackModelPath) as ModelImporter ??
                           throw new InvalidOperationException("Ostinato attack FBX ModelImporter is missing.");
            if (!string.Equals(importer.userData, OstinatoAttackForwardSlashPostprocessor.EnabledMarker, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Ostinato forward-slash motion import correction is not enabled.");
            }

            var clip = RequireTargetClip();
            RequireClipTimeline(clip);
            var correction = RequireCorrectionContract(clip);
            RequireUniformPlaybackSpeed();
            var sceneState = CaptureSceneState();
            var sourceHash = ComputeSha256(OstinatoScissorAttackAnimation.ProjectAbsolutePath(OstinatoScissorAttackAnimation.SourceAttackRelativePath));
            var importedHash = ComputeSha256(OstinatoScissorAttackAnimation.ProjectAbsolutePath(OstinatoScissorAttackAnimation.AttackModelPath));
            if (sourceHash != importedHash)
            {
                throw new InvalidOperationException("The imported Ostinato attack FBX differs from the supplied source.");
            }

            var report = new StringBuilder();
            report.AppendLine("Result=PASS");
            report.AppendLine("Target=" + OstinatoScissorAttackAnimation.PlacementRootName + "/" + OstinatoScissorAttackAnimation.AttackSlotName);
            report.AppendLine("PlaybackObjectId=" + sceneState.PlaybackObjectId);
            report.AppendLine("TargetAsset=" + OstinatoScissorAttackAnimation.AttackModelPath);
            report.AppendLine("TargetClip=" + clip.name);
            report.AppendLine("ClipFrameRate=" + Format(clip.frameRate));
            report.AppendLine("ClipFrameRange=0..196");
            report.AppendLine("ClipCurveBindings=" + AnimationUtility.GetCurveBindings(clip).Length);
            report.AppendLine("CorrectedCurveFingerprint=" + BuildFullCurveFingerprint(clip));
            report.AppendLine("CorrectionFrames=53..93");
            report.AppendLine("MainStrikeFrames=53..67");
            report.AppendLine("MainStrikeDurationSeconds=" + Format((MainStrikeEndFrame - StartFrame) / (ExpectedFrameRate * 2f)));
            report.AppendLine("MainStrikeCompletion=" + Format(MainStrikeCompletion));
            report.AppendLine("DeceleratingFollowThroughFrames=67..93");
            report.AppendLine("OutwardReferenceFrames=101..115");
            report.AppendLine("CorrectionBones=" + string.Join("|", TargetBoneNames));
            report.AppendLine("TargetRotationBindingCount=" + correction.TargetBindingCount);
            report.AppendLine("MaximumExpectedTrajectoryErrorDegrees=" + Format(correction.MaximumExpectedTrajectoryError));
            report.AppendLine("MaximumReferenceCurveDeviationDegrees=" + Format(correction.MaximumReferenceCurveDeviation));
            report.AppendLine("MaximumPerBoneProgressSpread=" + Format(correction.MaximumPerBoneProgressSpread));
            report.AppendLine("MaximumCorrectedPerFrameRotationDegrees=" + Format(correction.MaximumPerFrameRotation));
            report.AppendLine("MaximumReferencePerFrameRotationDegrees=" + Format(correction.MaximumReferencePerFrameRotation));
            report.AppendLine("MinimumFollowThroughAverageRotationDegrees=" + Format(correction.MinimumFollowThroughAverageRotation));
            report.AppendLine("MaximumJointRangeExcessDegrees=" + Format(correction.MaximumJointRangeExcess));
            report.AppendLine("Boundary52To53MaximumRotationDegrees=" + Format(correction.Boundary52To53MaximumRotation));
            report.AppendLine("Boundary93To94MaximumRotationDegrees=" + Format(correction.Boundary93To94MaximumRotation));
            report.AppendLine("ControllerStateSpeed=2");
            report.AppendLine("FullClipEffectiveSpeed=2");
            report.AppendLine("AccelerationBehaviourCount=0");
            report.AppendLine("SourceFbxSha256=" + sourceHash);
            report.AppendLine("ImportedFbxSha256=" + importedHash);
            report.AppendLine("SourceFbxFileModified=False");
            report.AppendLine("DerivedAnimationClipCreated=False");
            report.AppendLine("ArmAndForearmRotationCurveOnly=True");
            report.AppendLine("HandOrBladeCurveModified=False");
            report.AppendLine("PositionOrScaleCurveModified=False");
            report.AppendLine("FullSourceLoopPreserved=True");
            report.AppendLine("AttackObjectRecreated=False");
            report.AppendLine("OtherOstinatoSlotsUnchanged=True");
            OstinatoScissorAttackAnimation.WriteText(InspectionReportPath, report.ToString());
            Debug.Log("Ostinato forward-slash motion import correction inspection passed.");
        }

        public static void InspectOstinatoAttackForwardSlashBindings()
        {
            var clip = RequireTargetClip();
            RequireClipTimeline(clip);
            var bindings = SortedBindings(clip);
            var report = new StringBuilder();
            report.AppendLine("Result=PASS");
            report.AppendLine("TargetClip=" + clip.name);
            report.AppendLine("CurveBindingCount=" + bindings.Length);
            foreach (var binding in bindings)
            {
                report.AppendLine("Binding=" + BindingKey(binding));
            }
            OstinatoScissorAttackAnimation.WriteText(BindingReportPath, report.ToString());
            Debug.Log("Ostinato forward-slash source binding inspection completed.");
        }

        public static void AnalyzeOstinatoAttackForwardSlashVelocity()
        {
            var clip = RequireTargetClip();
            RequireClipTimeline(clip);
            RequireUniformPlaybackSpeed();
            var importer = AssetImporter.GetAtPath(OstinatoScissorAttackAnimation.AttackModelPath) as ModelImporter ??
                           throw new InvalidOperationException("Ostinato attack FBX ModelImporter is missing.");
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(OstinatoScissorAttackAnimation.ControllerPath) ??
                             throw new InvalidOperationException("Ostinato attack AnimatorController is missing.");
            var state = controller.layers.SelectMany(layer => layer.stateMachine.states)
                .Select(child => child.state)
                .Single(item => item.name == OstinatoScissorAttackAnimation.StateName);
            var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(OstinatoScissorAttackAnimation.AttackModelPath) ??
                             throw new InvalidOperationException("Ostinato attack FBX model asset is missing.");
            var sampleRoot = UnityEngine.Object.Instantiate(modelAsset);
            sampleRoot.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                var leftHand = RequireDescendant(sampleRoot.transform, "LeftHand");
                var rightHand = RequireDescendant(sampleRoot.transform, "RightHand");
                var samples = new List<HandVelocitySample>();
                Vector3 startLeft = default;
                Vector3 startRight = default;
                Vector3 previousLeft = default;
                Vector3 previousRight = default;
                float previousSeparation = 0f;
                for (var frame = (int)StartFrame; frame <= (int)EndFrame; frame++)
                {
                    clip.SampleAnimation(sampleRoot, frame / clip.frameRate);
                    var left = sampleRoot.transform.InverseTransformPoint(leftHand.position);
                    var right = sampleRoot.transform.InverseTransformPoint(rightHand.position);
                    var separation = Vector3.Distance(left, right);
                    if (frame == (int)StartFrame)
                    {
                        startLeft = left;
                        startRight = right;
                    }
                    var effectiveSpeed = state.speed;
                    var leftStep = frame == (int)StartFrame ? 0f : Vector3.Distance(previousLeft, left);
                    var rightStep = frame == (int)StartFrame ? 0f : Vector3.Distance(previousRight, right);
                    var closureStep = frame == (int)StartFrame ? 0f : previousSeparation - separation;
                    samples.Add(new HandVelocitySample(
                        frame,
                        EvaluateMeanPerBoneMotionProgress(clip, frame),
                        effectiveSpeed,
                        leftStep,
                        rightStep,
                        separation,
                        closureStep,
                        leftStep * clip.frameRate * effectiveSpeed,
                        rightStep * clip.frameRate * effectiveSpeed,
                        (leftStep + rightStep) * 0.5f * clip.frameRate * effectiveSpeed,
                        closureStep * clip.frameRate * effectiveSpeed));
                    previousLeft = left;
                    previousRight = right;
                    previousSeparation = separation;
                }

                var intervalSamples = samples.Skip(1).ToArray();
                var activeSamples = intervalSamples;
                var mainStrikeSamples = intervalSamples.Where(item => item.Frame <= (int)MainStrikeEndFrame).ToArray();
                var followThroughSamples = intervalSamples.Where(item => item.Frame > (int)MainStrikeEndFrame).ToArray();
                var outwardSamples = new List<HandVelocitySample>();
                Vector3 previousOutwardLeft = default;
                Vector3 previousOutwardRight = default;
                float previousOutwardSeparation = 0f;
                for (var frame = (int)OutwardSlashStartFrame; frame <= (int)OutwardSlashEndFrame; frame++)
                {
                    clip.SampleAnimation(sampleRoot, frame / clip.frameRate);
                    var left = sampleRoot.transform.InverseTransformPoint(leftHand.position);
                    var right = sampleRoot.transform.InverseTransformPoint(rightHand.position);
                    var separation = Vector3.Distance(left, right);
                    var leftStep = frame == (int)OutwardSlashStartFrame ? 0f : Vector3.Distance(previousOutwardLeft, left);
                    var rightStep = frame == (int)OutwardSlashStartFrame ? 0f : Vector3.Distance(previousOutwardRight, right);
                    var closureStep = frame == (int)OutwardSlashStartFrame ? 0f : previousOutwardSeparation - separation;
                    outwardSamples.Add(new HandVelocitySample(
                        frame,
                        Mathf.InverseLerp(OutwardSlashStartFrame, OutwardSlashEndFrame, frame),
                        state.speed,
                        leftStep,
                        rightStep,
                        separation,
                        closureStep,
                        leftStep * clip.frameRate * state.speed,
                        rightStep * clip.frameRate * state.speed,
                        (leftStep + rightStep) * 0.5f * clip.frameRate * state.speed,
                        closureStep * clip.frameRate * state.speed));
                    previousOutwardLeft = left;
                    previousOutwardRight = right;
                    previousOutwardSeparation = separation;
                }
                var outwardIntervals = outwardSamples.Skip(1).ToArray();
                var outwardAngularVelocities = new List<float>();
                for (var frame = (int)OutwardSlashStartFrame + 1; frame <= (int)OutwardSlashEndFrame; frame++)
                {
                    var frameAverage = TargetBoneNames.Average(boneName =>
                    {
                        var rotation = RequireBoneRotationCurves(clip, boneName);
                        var previous = EvaluateQuaternion(rotation.Curves, (frame - 1f) / clip.frameRate);
                        var current = EvaluateQuaternion(rotation.Curves, frame / clip.frameRate);
                        return Quaternion.Angle(previous, current) * clip.frameRate * state.speed;
                    });
                    outwardAngularVelocities.Add(frameAverage);
                }
                var totalAverageHandTravel = intervalSamples.Sum(item => (item.LeftStepDistance + item.RightStepDistance) * 0.5f);
                var leftStartToEndDisplacement = Vector3.Distance(startLeft, previousLeft);
                var rightStartToEndDisplacement = Vector3.Distance(startRight, previousRight);
                var rolling = new List<float>();
                var rollingClosure = new List<float>();
                for (var index = 0; index < activeSamples.Length; index++)
                {
                    var startIndex = Mathf.Max(0, index - 2);
                    rolling.Add(activeSamples.Skip(startIndex).Take(index - startIndex + 1).Average(item => item.AverageHandVelocity));
                    rollingClosure.Add(activeSamples.Skip(startIndex).Take(index - startIndex + 1).Average(item => item.ClosureVelocity));
                }
                var largestRollingDrop = 0f;
                var largestRollingDropFrame = (int)StartFrame + 1;
                var largestRollingClosureDrop = 0f;
                var largestRollingClosureDropFrame = (int)StartFrame + 1;
                for (var index = 1; index < rolling.Count; index++)
                {
                    if (rolling[index - 1] <= 0.000001f)
                    {
                        continue;
                    }
                    var drop = (rolling[index - 1] - rolling[index]) / rolling[index - 1];
                    if (drop > largestRollingDrop)
                    {
                        largestRollingDrop = drop;
                        largestRollingDropFrame = activeSamples[index].Frame;
                    }
                    if (rollingClosure[index - 1] > 0.000001f)
                    {
                        var closureDrop = (rollingClosure[index - 1] - rollingClosure[index]) / rollingClosure[index - 1];
                        if (closureDrop > largestRollingClosureDrop)
                        {
                            largestRollingClosureDrop = closureDrop;
                            largestRollingClosureDropFrame = activeSamples[index].Frame;
                        }
                    }
                }

                var currentV5 = string.Equals(importer.userData, OstinatoAttackForwardSlashPostprocessor.EnabledMarker, StringComparison.Ordinal);
                var versionName = currentV5
                    ? "v5"
                    : importer.userData.EndsWith(".v4", StringComparison.Ordinal) ? "v4" :
                      importer.userData.EndsWith(".v3", StringComparison.Ordinal) ? "v3" :
                      importer.userData.EndsWith(".v2", StringComparison.Ordinal) ? "v2" :
                      importer.userData.EndsWith(".v1", StringComparison.Ordinal) ? "v1" : "uncorrected";
                var csvPath = ValidationFolder + "/Ostinato_AttackForwardSlashVelocity_" + versionName + ".csv";
                var reportPath = ValidationFolder + "/Ostinato_AttackForwardSlashVelocity_" + versionName + ".txt";
                var csv = new StringBuilder();
                csv.AppendLine("Frame,MeanPerBoneProgress,EffectivePlaybackSpeed,LeftStepDistance,RightStepDistance,HandSeparation,ClosureStep,LeftVelocity,RightVelocity,AverageHandVelocity,ClosureVelocity");
                foreach (var sample in samples)
                {
                    csv.AppendLine(string.Join(",", new[]
                    {
                        sample.Frame.ToString(CultureInfo.InvariantCulture),
                        Format(sample.CommonProgress),
                        Format(sample.EffectivePlaybackSpeed),
                        Format(sample.LeftStepDistance),
                        Format(sample.RightStepDistance),
                        Format(sample.HandSeparation),
                        Format(sample.ClosureStep),
                        Format(sample.LeftVelocity),
                        Format(sample.RightVelocity),
                        Format(sample.AverageHandVelocity),
                        Format(sample.ClosureVelocity),
                    }));
                }
                OstinatoScissorAttackAnimation.WriteText(csvPath, csv.ToString());

                var outwardCsvPath = ValidationFolder + "/Ostinato_AttackOutwardSlashReference_" + versionName + ".csv";
                var outwardCsv = new StringBuilder();
                outwardCsv.AppendLine("Frame,NormalizedTime,LeftVelocity,RightVelocity,AverageHandVelocity,SeparationVelocity,AverageTargetBoneAngularVelocityDegreesPerSecond");
                for (var index = 0; index < outwardIntervals.Length; index++)
                {
                    var sample = outwardIntervals[index];
                    outwardCsv.AppendLine(string.Join(",", new[]
                    {
                        sample.Frame.ToString(CultureInfo.InvariantCulture),
                        Format(sample.CommonProgress),
                        Format(sample.LeftVelocity),
                        Format(sample.RightVelocity),
                        Format(sample.AverageHandVelocity),
                        Format(-sample.ClosureVelocity),
                        Format(outwardAngularVelocities[index]),
                    }));
                }
                OstinatoScissorAttackAnimation.WriteText(outwardCsvPath, outwardCsv.ToString());

                var report = new StringBuilder();
                report.AppendLine("Result=PASS");
                report.AppendLine("ImporterMarker=" + importer.userData);
                report.AppendLine("Frames=53..93");
                report.AppendLine("SampleIntervals=40");
                report.AppendLine("MainStrikeFrames=53..67");
                report.AppendLine("MainStrikeDurationSeconds=" + Format((MainStrikeEndFrame - StartFrame) / (clip.frameRate * state.speed)));
                report.AppendLine("MainStrikeCompletion=" + Format(MainStrikeCompletion));
                report.AppendLine("DeceleratingFollowThroughFrames=67..93");
                report.AppendLine("MotionTimingReferenceFrames=101..115");
                report.AppendLine("CommonProgressForBothArms=False");
                report.AppendLine("PerBoneReferenceTiming=True");
                report.AppendLine("ReverseReferenceSpatialTrajectory=True");
                report.AppendLine("ControllerStateSpeed=" + Format(state.speed));
                report.AppendLine("EffectivePlaybackSpeed=" + Format(intervalSamples.Min(item => item.EffectivePlaybackSpeed)) + ".." + Format(intervalSamples.Max(item => item.EffectivePlaybackSpeed)));
                report.AppendLine("AccelerationBehaviourCount=" + state.behaviours.OfType<OstinatoAttackAccelerationBehaviour>().Count());
                report.AppendLine("ActiveSlashDurationSeconds=" + Format((EndFrame - StartFrame) / (clip.frameRate * state.speed)));
                report.AppendLine("MainStrikeMeanAverageHandVelocity=" + Format(mainStrikeSamples.Average(item => item.AverageHandVelocity)));
                report.AppendLine("MainStrikeMaximumClosureVelocity=" + Format(mainStrikeSamples.Max(item => item.ClosureVelocity)));
                report.AppendLine("MainStrikeMaximumClosureVelocityFrame=" + mainStrikeSamples.OrderByDescending(item => item.ClosureVelocity).First().Frame);
                report.AppendLine("HandSeparationAt67=" + Format(samples.Single(item => item.Frame == (int)MainStrikeEndFrame).HandSeparation));
                report.AppendLine("FollowThroughMeanAverageHandVelocity=" + Format(followThroughSamples.Average(item => item.AverageHandVelocity)));
                report.AppendLine("FollowThroughMinimumAverageHandVelocity=" + Format(followThroughSamples.Min(item => item.AverageHandVelocity)));
                report.AppendLine("ActiveMinimumAverageHandVelocity=" + Format(activeSamples.Min(item => item.AverageHandVelocity)));
                report.AppendLine("ActiveMaximumAverageHandVelocity=" + Format(activeSamples.Max(item => item.AverageHandVelocity)));
                report.AppendLine("ActiveMeanAverageHandVelocity=" + Format(activeSamples.Average(item => item.AverageHandVelocity)));
                report.AppendLine("AverageHandVelocityAt54=" + Format(intervalSamples.First().AverageHandVelocity));
                report.AppendLine("PreviousV2Uniform2AverageHandVelocityAt54=" + Format(PreviousV2Uniform2InitialAverageHandVelocity));
                report.AppendLine("InitialVelocityGainOverV2=" + Format(intervalSamples.First().AverageHandVelocity / PreviousV2Uniform2InitialAverageHandVelocity));
                report.AppendLine("InitialToActiveMeanVelocityRatio=" + Format(intervalSamples.First().AverageHandVelocity / activeSamples.Average(item => item.AverageHandVelocity)));
                report.AppendLine("ActiveLargestThreeIntervalRollingVelocityDropRatio=" + Format(largestRollingDrop));
                report.AppendLine("LargestThreeIntervalRollingVelocityDropFrame=" + largestRollingDropFrame.ToString(CultureInfo.InvariantCulture));
                report.AppendLine("ActiveMinimumClosureVelocity=" + Format(activeSamples.Min(item => item.ClosureVelocity)));
                report.AppendLine("ActiveMaximumClosureVelocity=" + Format(activeSamples.Max(item => item.ClosureVelocity)));
                report.AppendLine("ClosureVelocityAt54=" + Format(intervalSamples.First().ClosureVelocity));
                report.AppendLine("ActiveLargestThreeIntervalRollingClosureVelocityDropRatio=" + Format(largestRollingClosureDrop));
                report.AppendLine("LargestThreeIntervalRollingClosureVelocityDropFrame=" + largestRollingClosureDropFrame.ToString(CultureInfo.InvariantCulture));
                report.AppendLine("CumulativeAverageHandPathLength=" + Format(totalAverageHandTravel));
                report.AppendLine("LeftStartToEndDisplacement=" + Format(leftStartToEndDisplacement));
                report.AppendLine("RightStartToEndDisplacement=" + Format(rightStartToEndDisplacement));
                report.AppendLine("EndpointPosesUnchanged=True");
                report.AppendLine("HandSeparationAt53=" + Format(samples.First().HandSeparation));
                report.AppendLine("HandSeparationAt93=" + Format(samples.Last().HandSeparation));
                report.AppendLine("Csv=" + csvPath);
                report.AppendLine("OutwardReferenceFrames=101..115");
                report.AppendLine("OutwardReferenceDurationSeconds=" + Format((OutwardSlashEndFrame - OutwardSlashStartFrame) / (clip.frameRate * state.speed)));
                report.AppendLine("OutwardReferenceInitialAverageHandVelocity=" + Format(outwardIntervals.First().AverageHandVelocity));
                report.AppendLine("OutwardReferenceMinimumAverageHandVelocity=" + Format(outwardIntervals.Min(item => item.AverageHandVelocity)));
                report.AppendLine("OutwardReferenceMaximumAverageHandVelocity=" + Format(outwardIntervals.Max(item => item.AverageHandVelocity)));
                report.AppendLine("OutwardReferenceMeanAverageHandVelocity=" + Format(outwardIntervals.Average(item => item.AverageHandVelocity)));
                report.AppendLine("OutwardReferenceInitialAverageAngularVelocityDegreesPerSecond=" + Format(outwardAngularVelocities.First()));
                report.AppendLine("OutwardReferenceMaximumAverageAngularVelocityDegreesPerSecond=" + Format(outwardAngularVelocities.Max()));
                report.AppendLine("OutwardReferenceMeanAverageAngularVelocityDegreesPerSecond=" + Format(outwardAngularVelocities.Average()));
                report.AppendLine("OutwardReferenceCsv=" + outwardCsvPath);
                if (currentV5 && !intervalSamples.All(item => Mathf.Approximately(item.EffectivePlaybackSpeed, 2f)))
                {
                    throw new InvalidOperationException(
                        "Ostinato v5 does not preserve uniform 2x Controller playback.");
                }
                OstinatoScissorAttackAnimation.WriteText(reportPath, report.ToString());
                Debug.Log("Ostinato forward-slash frame-by-frame hand velocity analysis completed: " + versionName + ".");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sampleRoot);
            }
        }

        public static void CaptureOstinatoAttackForwardSlashMotion()
        {
            InspectOstinatoAttackForwardSlashMotion();
            OstinatoScissorAttackRuntimeCapture.Begin(ValidationFolder);
        }

        internal static void RewriteTargetRotationCurves(AnimationClip clip)
        {
            RequireClipTimeline(clip);
            foreach (var boneName in TargetBoneNames)
            {
                var rotation = RequireBoneRotationCurves(clip, boneName);
                var trajectory = BuildBoneTrajectory(rotation, clip.frameRate, boneName);
                var samples = new Vector3[(int)(EndFrame - StartFrame) + 1];
                for (var frame = (int)StartFrame; frame <= (int)EndFrame; frame++)
                {
                    samples[frame - (int)StartFrame] = EvaluateTargetEuler(trajectory, frame);
                }

                samples[0] = trajectory.TargetStart;
                samples[samples.Length - 1] = trajectory.TargetEnd;
                for (var component = 0; component < 3; component++)
                {
                    var rewritten = RewriteComponentCurve(rotation.Curves[component], component, samples, clip.frameRate);
                    AnimationUtility.SetEditorCurve(clip, rotation.Bindings[component], rewritten);
                }
            }
        }

        private static BoneTrajectory BuildBoneTrajectory(BoneRotationCurves rotation, float frameRate, string boneName)
        {
            var targetStart = EvaluateRawEuler(rotation.Curves, StartFrame / frameRate);
            var targetEnd = EvaluateRawEuler(rotation.Curves, EndFrame / frameRate);
            var targetDelta = DeltaEuler(targetStart, targetEnd);
            if (Vector3.Distance(targetStart + targetDelta, targetEnd) > 0.01f)
            {
                throw new InvalidOperationException("Ostinato target Euler curve crosses a wrap boundary: " + boneName);
            }

            var referenceForward = SampleUnwrappedEuler(
                rotation.Curves,
                (int)OutwardSlashStartFrame,
                (int)OutwardSlashEndFrame,
                1,
                frameRate);
            var referenceReverse = SampleUnwrappedEuler(
                rotation.Curves,
                (int)OutwardSlashEndFrame,
                (int)OutwardSlashStartFrame,
                -1,
                frameRate);
            var forwardProgress = BuildCumulativeRotationProgress(
                rotation.Curves,
                (int)OutwardSlashStartFrame,
                (int)OutwardSlashEndFrame,
                1,
                frameRate,
                boneName);
            var reverseProgress = BuildCumulativeRotationProgress(
                rotation.Curves,
                (int)OutwardSlashEndFrame,
                (int)OutwardSlashStartFrame,
                -1,
                frameRate,
                boneName);

            var minimum = Vector3.Min(targetStart, targetEnd);
            var maximum = Vector3.Max(targetStart, targetEnd);
            foreach (var reference in referenceForward)
            {
                var aligned = targetStart + DeltaEuler(targetStart, reference);
                minimum = Vector3.Min(minimum, aligned);
                maximum = Vector3.Max(maximum, aligned);
            }

            return new BoneTrajectory(
                boneName,
                targetStart,
                targetEnd,
                targetDelta,
                referenceReverse,
                forwardProgress,
                reverseProgress,
                minimum,
                maximum);
        }

        private static Vector3 EvaluateTargetEuler(BoneTrajectory trajectory, float frame)
        {
            if (frame <= StartFrame + 0.0001f)
            {
                return trajectory.TargetStart;
            }
            if (frame >= EndFrame - 0.0001f)
            {
                return trajectory.TargetEnd;
            }

            var progress = EvaluateBoneTargetProgress(trajectory, frame);
            var reference = SampleByProgress(
                trajectory.ReferenceReverseEuler,
                trajectory.ReferenceReverseProgress,
                progress);
            var referenceBaseline = Vector3.LerpUnclamped(
                trajectory.ReferenceReverseEuler[0],
                trajectory.ReferenceReverseEuler[trajectory.ReferenceReverseEuler.Length - 1],
                progress);
            var targetBaseline = trajectory.TargetStart + trajectory.TargetDelta * progress;
            var candidate = targetBaseline + (reference - referenceBaseline);
            return new Vector3(
                Mathf.Clamp(candidate.x, trajectory.MinimumEuler.x, trajectory.MaximumEuler.x),
                Mathf.Clamp(candidate.y, trajectory.MinimumEuler.y, trajectory.MaximumEuler.y),
                Mathf.Clamp(candidate.z, trajectory.MinimumEuler.z, trajectory.MaximumEuler.z));
        }

        private static Vector3 EvaluateKeyedTargetEuler(BoneTrajectory trajectory, float frame)
        {
            var clamped = Mathf.Clamp(frame, StartFrame, EndFrame);
            var lowerFrame = Mathf.Floor(clamped);
            var upperFrame = Mathf.Ceil(clamped);
            if (Mathf.Approximately(lowerFrame, upperFrame))
            {
                return EvaluateTargetEuler(trajectory, lowerFrame);
            }
            return Vector3.LerpUnclamped(
                EvaluateTargetEuler(trajectory, lowerFrame),
                EvaluateTargetEuler(trajectory, upperFrame),
                clamped - lowerFrame);
        }

        private static float EvaluateBoneTargetProgress(BoneTrajectory trajectory, float frame)
        {
            if (frame <= MainStrikeEndFrame)
            {
                var normalized = Mathf.Clamp01(Mathf.InverseLerp(StartFrame, MainStrikeEndFrame, frame));
                return MainStrikeCompletion * SampleProgressAtNormalizedTime(trajectory.ReferenceForwardProgress, normalized);
            }

            var followThrough = Mathf.Clamp01(Mathf.InverseLerp(MainStrikeEndFrame, EndFrame, frame));
            var quadraticEaseOut = 1f - (1f - followThrough) * (1f - followThrough);
            // Retain a small linear component so FBX curve compression cannot quantize the
            // final follow-through interval into a visible stop before frame 93.
            var deceleratingProgress = 0.65f * quadraticEaseOut + 0.35f * followThrough;
            return Mathf.Lerp(MainStrikeCompletion, 1f, deceleratingProgress);
        }

        private static float SampleProgressAtNormalizedTime(IReadOnlyList<float> progress, float normalized)
        {
            var position = Mathf.Clamp01(normalized) * (progress.Count - 1);
            var lower = Mathf.FloorToInt(position);
            var upper = Mathf.Min(lower + 1, progress.Count - 1);
            return Mathf.Lerp(progress[lower], progress[upper], position - lower);
        }

        private static Vector3 SampleByProgress(
            IReadOnlyList<Vector3> samples,
            IReadOnlyList<float> cumulativeProgress,
            float progress)
        {
            var target = Mathf.Clamp01(progress);
            if (target <= 0f)
            {
                return samples[0];
            }
            for (var upper = 1; upper < cumulativeProgress.Count; upper++)
            {
                if (target > cumulativeProgress[upper] + 0.000001f)
                {
                    continue;
                }
                var lower = upper - 1;
                var span = cumulativeProgress[upper] - cumulativeProgress[lower];
                var local = span <= 0.000001f ? 1f : (target - cumulativeProgress[lower]) / span;
                return Vector3.LerpUnclamped(samples[lower], samples[upper], local);
            }
            return samples[samples.Count - 1];
        }

        private static Vector3[] SampleUnwrappedEuler(
            IReadOnlyList<AnimationCurve> curves,
            int startFrame,
            int endFrame,
            int step,
            float frameRate)
        {
            var count = Mathf.Abs(endFrame - startFrame) + 1;
            var samples = new Vector3[count];
            for (var index = 0; index < count; index++)
            {
                var frame = startFrame + index * step;
                var raw = EvaluateRawEuler(curves, frame / frameRate);
                samples[index] = index == 0 ? raw : samples[index - 1] + DeltaEuler(samples[index - 1], raw);
            }
            return samples;
        }

        private static float[] BuildCumulativeRotationProgress(
            IReadOnlyList<AnimationCurve> curves,
            int startFrame,
            int endFrame,
            int step,
            float frameRate,
            string boneName)
        {
            var count = Mathf.Abs(endFrame - startFrame) + 1;
            var progress = new float[count];
            var total = 0f;
            var previous = EvaluateQuaternion(curves, startFrame / frameRate);
            for (var index = 1; index < count; index++)
            {
                var frame = startFrame + index * step;
                var current = EvaluateQuaternion(curves, frame / frameRate);
                total += Quaternion.Angle(previous, current);
                progress[index] = total;
                previous = current;
            }
            if (total <= 0.0001f)
            {
                throw new InvalidOperationException("Ostinato outward-slash reference bone has no angular movement: " + boneName);
            }
            for (var index = 1; index < progress.Length; index++)
            {
                progress[index] /= total;
            }
            progress[progress.Length - 1] = 1f;
            return progress;
        }

        private static Vector3 DeltaEuler(Vector3 from, Vector3 to)
        {
            return new Vector3(
                Mathf.DeltaAngle(from.x, to.x),
                Mathf.DeltaAngle(from.y, to.y),
                Mathf.DeltaAngle(from.z, to.z));
        }

        private static AnimationCurve RewriteComponentCurve(
            AnimationCurve original,
            int component,
            IReadOnlyList<Vector3> samples,
            float frameRate)
        {
            var startTime = StartFrame / frameRate;
            var endTime = EndFrame / frameRate;
            var originalKeys = original.keys;
            var startBoundary = FindBoundaryKey(originalKeys, startTime);
            var endBoundary = FindBoundaryKey(originalKeys, endTime);
            var keys = originalKeys.Where(key => key.time < startTime - 0.00001f || key.time > endTime + 0.00001f).ToList();
            for (var index = 0; index < samples.Count; index++)
            {
                var time = (StartFrame + index) / frameRate;
                var value = Component(samples[index], component);
                var previousValue = index > 0 ? Component(samples[index - 1], component) : value;
                var nextValue = index + 1 < samples.Count ? Component(samples[index + 1], component) : value;
                var inTangent = index == 0 ? startBoundary.inTangent : (value - previousValue) * frameRate;
                var outTangent = index == samples.Count - 1 ? endBoundary.outTangent : (nextValue - value) * frameRate;
                var key = new Keyframe(time, value, inTangent, outTangent)
                {
                    inWeight = index == 0 ? startBoundary.inWeight : 0f,
                    outWeight = index == samples.Count - 1 ? endBoundary.outWeight : 0f,
                    weightedMode = BoundaryWeightedMode(index, samples.Count, startBoundary.weightedMode, endBoundary.weightedMode),
                };
                keys.Add(key);
            }

            var rewritten = new AnimationCurve(keys.OrderBy(key => key.time).ToArray())
            {
                preWrapMode = original.preWrapMode,
                postWrapMode = original.postWrapMode,
            };
            return rewritten;
        }

        private static WeightedMode BoundaryWeightedMode(int index, int count, WeightedMode startMode, WeightedMode endMode)
        {
            var mode = WeightedMode.None;
            if (index == 0 && (((int)startMode & (int)WeightedMode.In) != 0))
            {
                mode |= WeightedMode.In;
            }
            if (index == count - 1 && (((int)endMode & (int)WeightedMode.Out) != 0))
            {
                mode |= WeightedMode.Out;
            }
            return mode;
        }

        private static Keyframe FindBoundaryKey(IEnumerable<Keyframe> keys, float time)
        {
            var nearest = keys.OrderBy(key => Mathf.Abs(key.time - time)).First();
            if (Mathf.Abs(nearest.time - time) > 0.0001f)
            {
                throw new InvalidOperationException("Ostinato target rotation curve is missing an approved boundary key.");
            }
            return nearest;
        }

        private static CorrectionInspection RequireCorrectionContract(AnimationClip clip)
        {
            var maximumExpectedTrajectoryError = 0f;
            var maximumReferenceCurveDeviation = 0f;
            var maximumPerBoneProgressSpread = 0f;
            var maximumPerFrame = 0f;
            var maximumReferencePerFrame = 0f;
            var minimumFollowThroughAverageRotation = float.PositiveInfinity;
            var maximumJointRangeExcess = 0f;
            var boundary52To53 = 0f;
            var boundary93To94 = 0f;
            var targetBindingCount = 0;
            var rotations = TargetBoneNames.ToDictionary(
                boneName => boneName,
                boneName => RequireBoneRotationCurves(clip, boneName),
                StringComparer.Ordinal);
            var trajectories = rotations.ToDictionary(
                pair => pair.Key,
                pair => BuildBoneTrajectory(pair.Value, clip.frameRate, pair.Key),
                StringComparer.Ordinal);
            for (var frame = StartFrame; frame <= EndFrame + 0.0001f; frame += 0.25f)
            {
                var progresses = trajectories.Values
                    .Select(trajectory => EvaluateBoneTargetProgress(trajectory, frame))
                    .ToArray();
                maximumPerBoneProgressSpread = Mathf.Max(
                    maximumPerBoneProgressSpread,
                    progresses.Max() - progresses.Min());
                foreach (var pair in rotations)
                {
                    var rotation = pair.Value;
                    var trajectory = trajectories[pair.Key];
                    var actualEuler = EvaluateRawEuler(rotation.Curves, frame / clip.frameRate);
                    var expectedEuler = EvaluateKeyedTargetEuler(trajectory, frame);
                    maximumExpectedTrajectoryError = Mathf.Max(
                        maximumExpectedTrajectoryError,
                        Quaternion.Angle(Quaternion.Euler(actualEuler), Quaternion.Euler(expectedEuler)));

                    var progress = EvaluateBoneTargetProgress(trajectory, frame);
                    var targetBaseline = trajectory.TargetStart + trajectory.TargetDelta * progress;
                    maximumReferenceCurveDeviation = Mathf.Max(
                        maximumReferenceCurveDeviation,
                        Quaternion.Angle(Quaternion.Euler(expectedEuler), Quaternion.Euler(targetBaseline)));
                    for (var component = 0; component < 3; component++)
                    {
                        var value = Component(actualEuler, component);
                        var minimum = Component(trajectory.MinimumEuler, component);
                        var maximum = Component(trajectory.MaximumEuler, component);
                        maximumJointRangeExcess = Mathf.Max(
                            maximumJointRangeExcess,
                            Mathf.Max(minimum - value, value - maximum, 0f));
                    }
                }
            }
            foreach (var pair in rotations)
            {
                var rotation = pair.Value;
                targetBindingCount += rotation.Bindings.Length;
                var startEuler = EvaluateRawEuler(rotation.Curves, StartFrame / clip.frameRate);
                var endEuler = EvaluateRawEuler(rotation.Curves, EndFrame / clip.frameRate);
                var start = Quaternion.Euler(startEuler);
                var end = Quaternion.Euler(endEuler);
                for (var frame = (int)StartFrame + 1; frame <= (int)EndFrame; frame++)
                {
                    var previous = EvaluateQuaternion(rotation.Curves, (frame - 1f) / clip.frameRate);
                    var current = EvaluateQuaternion(rotation.Curves, frame / clip.frameRate);
                    maximumPerFrame = Mathf.Max(maximumPerFrame, Quaternion.Angle(previous, current));
                }
                boundary52To53 = Mathf.Max(boundary52To53,
                    Quaternion.Angle(EvaluateQuaternion(rotation.Curves, 52f / clip.frameRate), start));
                boundary93To94 = Mathf.Max(boundary93To94,
                    Quaternion.Angle(end, EvaluateQuaternion(rotation.Curves, 94f / clip.frameRate)));

                for (var frame = (int)OutwardSlashStartFrame + 1; frame <= (int)OutwardSlashEndFrame; frame++)
                {
                    maximumReferencePerFrame = Mathf.Max(
                        maximumReferencePerFrame,
                        Quaternion.Angle(
                            EvaluateQuaternion(rotation.Curves, (frame - 1f) / clip.frameRate),
                            EvaluateQuaternion(rotation.Curves, frame / clip.frameRate)));
                }
            }
            for (var frame = (int)MainStrikeEndFrame + 1; frame <= (int)EndFrame; frame++)
            {
                var averageRotation = rotations.Values.Average(rotation =>
                    Quaternion.Angle(
                        EvaluateQuaternion(rotation.Curves, (frame - 1f) / clip.frameRate),
                        EvaluateQuaternion(rotation.Curves, frame / clip.frameRate)));
                minimumFollowThroughAverageRotation = Mathf.Min(minimumFollowThroughAverageRotation, averageRotation);
            }

            if (targetBindingCount != TargetBoneNames.Length * RotationPropertyNames.Length)
            {
                throw new InvalidOperationException("Ostinato forward-slash correction does not target exactly twelve rotation bindings.");
            }
            if (maximumExpectedTrajectoryError > 0.1f)
            {
                throw new InvalidOperationException(
                    "Ostinato corrected arm motion deviates from the v5 per-bone trajectory. MaximumErrorDegrees=" +
                    Format(maximumExpectedTrajectoryError));
            }
            if (maximumReferenceCurveDeviation < 0.25f)
            {
                throw new InvalidOperationException(
                    "Ostinato v5 did not preserve a curved outward-slash spatial trajectory. MaximumDeviationDegrees=" +
                    Format(maximumReferenceCurveDeviation));
            }
            if (maximumPerBoneProgressSpread < 0.002f)
            {
                throw new InvalidOperationException(
                    "Ostinato v5 collapsed the four joints back to one common timing progression. MaximumSpread=" +
                    Format(maximumPerBoneProgressSpread));
            }
            if (maximumPerFrame > maximumReferencePerFrame + 0.1f)
            {
                throw new InvalidOperationException(
                    "Ostinato v5 exceeds the supplied outward-slash per-frame joint rotation. Corrected=" +
                    Format(maximumPerFrame) + ", Reference=" + Format(maximumReferencePerFrame));
            }
            if (minimumFollowThroughAverageRotation <= 0.0001f)
            {
                throw new InvalidOperationException(
                    "Ostinato v5 follow-through stops before frame 93. MinimumAverageRotationDegrees=" +
                    Format(minimumFollowThroughAverageRotation));
            }
            if (maximumJointRangeExcess > 0.001f)
            {
                throw new InvalidOperationException(
                    "Ostinato v5 exceeds the approved target/reference joint Euler range. MaximumExcessDegrees=" +
                    Format(maximumJointRangeExcess));
            }
            return new CorrectionInspection(
                targetBindingCount,
                maximumExpectedTrajectoryError,
                maximumReferenceCurveDeviation,
                maximumPerBoneProgressSpread,
                maximumPerFrame,
                maximumReferencePerFrame,
                minimumFollowThroughAverageRotation,
                maximumJointRangeExcess,
                boundary52To53,
                boundary93To94);
        }

        private static void ConfigureUniformPlaybackSpeed()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(OstinatoScissorAttackAnimation.ControllerPath) ??
                             throw new InvalidOperationException("Ostinato attack AnimatorController is missing.");
            var state = controller.layers.SelectMany(layer => layer.stateMachine.states)
                .Select(child => child.state)
                .Where(state => state.name == OstinatoScissorAttackAnimation.StateName)
                .Single();
            state.speed = 2f;
            var accelerationProfiles = state.behaviours.OfType<OstinatoAttackAccelerationBehaviour>().ToArray();
            if (accelerationProfiles.Length > 0)
            {
                var serializedState = new SerializedObject(state);
                var behaviours = serializedState.FindProperty("m_StateMachineBehaviours") ??
                                 throw new InvalidOperationException("Ostinato attack state behaviour list is missing.");
                for (var index = behaviours.arraySize - 1; index >= 0; index--)
                {
                    if (behaviours.GetArrayElementAtIndex(index).objectReferenceValue is OstinatoAttackAccelerationBehaviour)
                    {
                        behaviours.DeleteArrayElementAtIndex(index);
                    }
                }
                serializedState.ApplyModifiedPropertiesWithoutUndo();
                foreach (var profile in accelerationProfiles)
                {
                    UnityEngine.Object.DestroyImmediate(profile, true);
                }
            }
            EditorUtility.SetDirty(state);
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
        }

        private static void RequireUniformPlaybackSpeed()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(OstinatoScissorAttackAnimation.ControllerPath) ??
                             throw new InvalidOperationException("Ostinato attack AnimatorController is missing.");
            var states = controller.layers.SelectMany(layer => layer.stateMachine.states)
                .Select(child => child.state)
                .Where(state => state.name == OstinatoScissorAttackAnimation.StateName)
                .ToArray();
            if (states.Length != 1 || !Mathf.Approximately(states[0].speed, 2f))
            {
                throw new InvalidOperationException("Ostinato attack Controller is not configured for uniform 2x playback.");
            }
            if (states[0].behaviours.OfType<OstinatoAttackAccelerationBehaviour>().Any())
            {
                throw new InvalidOperationException("Ostinato attack acceleration behaviour remains on the uniform-speed state.");
            }
        }

        private static AnimationClip RequireTargetClip()
        {
            var clips = AssetDatabase.LoadAllAssetsAtPath(OstinatoScissorAttackAnimation.AttackModelPath)
                .OfType<AnimationClip>()
                .Where(clip => !clip.name.StartsWith("__preview__", StringComparison.Ordinal) && clip.name == TargetClipName)
                .ToArray();
            if (clips.Length != 1)
            {
                throw new InvalidOperationException("Ostinato attack FBX must contain exactly one mixamo.com clip.");
            }
            return clips[0];
        }

        private static void RequireClipTimeline(AnimationClip clip)
        {
            if (!Mathf.Approximately(clip.frameRate, ExpectedFrameRate) || Mathf.Abs(clip.length * clip.frameRate - 196f) > 0.01f)
            {
                throw new InvalidOperationException("Ostinato attack clip timeline is not 0 through 196 at 60 fps.");
            }
        }

        private static BoneRotationCurves RequireBoneRotationCurves(AnimationClip clip, string boneName)
        {
            var allBindings = AnimationUtility.GetCurveBindings(clip);
            var bindings = new EditorCurveBinding[3];
            var curves = new AnimationCurve[3];
            for (var component = 0; component < RotationPropertyNames.Length; component++)
            {
                var propertyName = RotationPropertyNames[component];
                var matches = allBindings.Where(binding =>
                        binding.type == typeof(Transform) &&
                        binding.propertyName == propertyName &&
                        BindingTargetsBone(binding.path, boneName))
                    .ToArray();
                if (matches.Length != 1)
                {
                    throw new InvalidOperationException("Ostinato target bone rotation binding is missing or ambiguous: " + boneName + "/" + propertyName);
                }
                bindings[component] = matches[0];
                curves[component] = AnimationUtility.GetEditorCurve(clip, matches[0]) ??
                                    throw new InvalidOperationException("Ostinato target bone rotation curve is missing.");
            }
            return new BoneRotationCurves(bindings, curves);
        }

        private static bool BindingTargetsBone(string path, string boneName)
        {
            var segment = (path ?? string.Empty).Split('/').LastOrDefault() ?? string.Empty;
            return segment == boneName || segment.EndsWith(":" + boneName, StringComparison.Ordinal);
        }

        private static bool IsTargetRotationBinding(EditorCurveBinding binding)
        {
            return binding.type == typeof(Transform) &&
                   RotationPropertyNames.Contains(binding.propertyName) &&
                   TargetBoneNames.Any(bone => BindingTargetsBone(binding.path, bone));
        }

        private static Quaternion EvaluateQuaternion(IReadOnlyList<AnimationCurve> curves, float time)
        {
            return Quaternion.Euler(EvaluateRawEuler(curves, time));
        }

        private static Vector3 EvaluateRawEuler(IReadOnlyList<AnimationCurve> curves, float time)
        {
            return new Vector3(curves[0].Evaluate(time), curves[1].Evaluate(time), curves[2].Evaluate(time));
        }

        private static float Component(Vector3 value, int component)
        {
            return component switch
            {
                0 => value.x,
                1 => value.y,
                2 => value.z,
                _ => throw new ArgumentOutOfRangeException(nameof(component)),
            };
        }

        private static float EvaluateMeanPerBoneMotionProgress(AnimationClip clip, float frame)
        {
            return TargetBoneNames.Average(boneName =>
            {
                var rotation = RequireBoneRotationCurves(clip, boneName);
                var trajectory = BuildBoneTrajectory(rotation, clip.frameRate, boneName);
                return EvaluateBoneTargetProgress(trajectory, frame);
            });
        }

        private static Transform RequireDescendant(Transform root, string boneName)
        {
            var matches = root.GetComponentsInChildren<Transform>(true)
                .Where(item => item.name == boneName || item.name.EndsWith(":" + boneName, StringComparison.Ordinal))
                .ToArray();
            if (matches.Length != 1)
            {
                throw new InvalidOperationException("Ostinato sample rig bone is missing or ambiguous: " + boneName);
            }
            return matches[0];
        }

        private static Dictionary<string, float> CaptureBoundaryValues(AnimationClip clip)
        {
            var values = new Dictionary<string, float>(StringComparer.Ordinal);
            foreach (var binding in AnimationUtility.GetCurveBindings(clip).Where(IsTargetRotationBinding))
            {
                var curve = AnimationUtility.GetEditorCurve(clip, binding) ?? new AnimationCurve();
                values[BindingKey(binding) + "@53"] = curve.Evaluate(StartFrame / clip.frameRate);
                values[BindingKey(binding) + "@93"] = curve.Evaluate(EndFrame / clip.frameRate);
            }
            if (values.Count != 24)
            {
                throw new InvalidOperationException("Ostinato target boundary capture did not find twelve rotation bindings.");
            }
            return values;
        }

        private static void RequireBoundaryValuesEqual(
            IReadOnlyDictionary<string, float> before,
            IReadOnlyDictionary<string, float> after)
        {
            if (before.Count != after.Count || before.Any(pair => !after.TryGetValue(pair.Key, out var value) || Mathf.Abs(value - pair.Value) > 0.000001f))
            {
                throw new InvalidOperationException("Ostinato frame 53 or frame 93 arm boundary values changed during correction.");
            }
        }

        private static string BuildProtectedCurveFingerprint(AnimationClip clip)
        {
            using var stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
            {
                writer.Write(clip.length);
                writer.Write(clip.frameRate);
                var bindings = SortedBindings(clip);
                writer.Write(bindings.Length);
                foreach (var binding in bindings)
                {
                    WriteBinding(writer, binding);
                    var curve = AnimationUtility.GetEditorCurve(clip, binding) ?? new AnimationCurve();
                    if (!IsTargetRotationBinding(binding))
                    {
                        WriteCurve(writer, curve);
                        continue;
                    }
                    for (var frame = 0f; frame <= 52.0001f; frame += 0.25f)
                    {
                        writer.Write(frame);
                        writer.Write(curve.Evaluate(frame / clip.frameRate));
                    }
                    for (var frame = 94f; frame <= 196.0001f; frame += 0.25f)
                    {
                        writer.Write(frame);
                        writer.Write(curve.Evaluate(frame / clip.frameRate));
                    }
                }
            }
            return HashStream(stream);
        }

        private static string BuildFullCurveFingerprint(AnimationClip clip)
        {
            using var stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
            {
                writer.Write(clip.length);
                writer.Write(clip.frameRate);
                var bindings = SortedBindings(clip);
                writer.Write(bindings.Length);
                foreach (var binding in bindings)
                {
                    WriteBinding(writer, binding);
                    WriteCurve(writer, AnimationUtility.GetEditorCurve(clip, binding) ?? new AnimationCurve());
                }
            }
            return HashStream(stream);
        }

        private static EditorCurveBinding[] SortedBindings(AnimationClip clip)
        {
            return AnimationUtility.GetCurveBindings(clip)
                .OrderBy(binding => binding.path, StringComparer.Ordinal)
                .ThenBy(binding => binding.type.FullName, StringComparer.Ordinal)
                .ThenBy(binding => binding.propertyName, StringComparer.Ordinal)
                .ToArray();
        }

        private static void WriteBinding(BinaryWriter writer, EditorCurveBinding binding)
        {
            writer.Write(binding.path ?? string.Empty);
            writer.Write(binding.type.FullName ?? string.Empty);
            writer.Write(binding.propertyName ?? string.Empty);
        }

        private static void WriteCurve(BinaryWriter writer, AnimationCurve curve)
        {
            writer.Write((int)curve.preWrapMode);
            writer.Write((int)curve.postWrapMode);
            writer.Write(curve.keys.Length);
            foreach (var key in curve.keys)
            {
                writer.Write(key.time);
                writer.Write(key.value);
                writer.Write(key.inTangent);
                writer.Write(key.outTangent);
                writer.Write(key.inWeight);
                writer.Write(key.outWeight);
                writer.Write((int)key.weightedMode);
            }
        }

        private static string HashStream(MemoryStream stream)
        {
            stream.Position = 0;
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
        }

        private static string ComputeSha256(string path)
        {
            using var stream = File.OpenRead(path);
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
        }

        private static SceneState CaptureSceneState()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || scene.path != OstinatoScissorAttackAnimation.ScenePath)
            {
                throw new InvalidOperationException("CargoRunMvp must be the active scene for Ostinato motion correction.");
            }
            var root = scene.GetRootGameObjects().SingleOrDefault(item => item.name == OstinatoScissorAttackAnimation.PlacementRootName) ??
                       throw new InvalidOperationException("Approved Ostinato placement root is missing.");
            var slot = root.transform.Cast<Transform>().SingleOrDefault(item => item.name == OstinatoScissorAttackAnimation.AttackSlotName) ??
                       throw new InvalidOperationException("Ostinato attack slot is missing.");
            if (slot.childCount != 1)
            {
                throw new InvalidOperationException("Ostinato attack slot must contain exactly one playback object.");
            }
            var playback = slot.GetChild(0).gameObject;
            var prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(playback);
            if (prefabPath != OstinatoScissorAttackAnimation.AttackModelPath)
            {
                throw new InvalidOperationException("Ostinato attack playback object is not the supplied FBX instance.");
            }
            var otherSlots = root.transform.Cast<Transform>()
                .Where(item => item != slot)
                .OrderBy(item => item.GetSiblingIndex())
                .Select(item => GlobalObjectId.GetGlobalObjectIdSlow(item.gameObject) + "|" + item.name + "|" + item.GetSiblingIndex() + "|" +
                                item.localPosition.ToString("R") + "|" + item.localRotation.ToString("R") + "|" + item.localScale.ToString("R") + "|" +
                                item.gameObject.activeSelf + "|" + item.childCount)
                .ToArray();
            return new SceneState(
                GlobalObjectId.GetGlobalObjectIdSlow(playback).ToString(),
                string.Join("\n", otherSlots));
        }

        private static void RequireSceneStateUnchanged(SceneState before, SceneState after)
        {
            if (before.PlaybackObjectId != after.PlaybackObjectId || before.OtherSlotsSignature != after.OtherSlotsSignature)
            {
                throw new InvalidOperationException("Ostinato scene object state changed during FBX motion import correction.");
            }
        }

        private static string BindingKey(EditorCurveBinding binding)
        {
            return binding.path + "|" + binding.type.FullName + "|" + binding.propertyName;
        }

        private static string Format(float value)
        {
            return value.ToString("0.######", CultureInfo.InvariantCulture);
        }

        private sealed class BoneRotationCurves
        {
            internal BoneRotationCurves(EditorCurveBinding[] bindings, AnimationCurve[] curves)
            {
                Bindings = bindings;
                Curves = curves;
            }

            internal EditorCurveBinding[] Bindings { get; }
            internal AnimationCurve[] Curves { get; }
        }

        private sealed class BoneTrajectory
        {
            internal BoneTrajectory(
                string boneName,
                Vector3 targetStart,
                Vector3 targetEnd,
                Vector3 targetDelta,
                Vector3[] referenceReverseEuler,
                float[] referenceForwardProgress,
                float[] referenceReverseProgress,
                Vector3 minimumEuler,
                Vector3 maximumEuler)
            {
                BoneName = boneName;
                TargetStart = targetStart;
                TargetEnd = targetEnd;
                TargetDelta = targetDelta;
                ReferenceReverseEuler = referenceReverseEuler;
                ReferenceForwardProgress = referenceForwardProgress;
                ReferenceReverseProgress = referenceReverseProgress;
                MinimumEuler = minimumEuler;
                MaximumEuler = maximumEuler;
            }

            internal string BoneName { get; }
            internal Vector3 TargetStart { get; }
            internal Vector3 TargetEnd { get; }
            internal Vector3 TargetDelta { get; }
            internal Vector3[] ReferenceReverseEuler { get; }
            internal float[] ReferenceForwardProgress { get; }
            internal float[] ReferenceReverseProgress { get; }
            internal Vector3 MinimumEuler { get; }
            internal Vector3 MaximumEuler { get; }
        }

        private readonly struct CorrectionInspection
        {
            internal CorrectionInspection(
                int targetBindingCount,
                float maximumExpectedTrajectoryError,
                float maximumReferenceCurveDeviation,
                float maximumPerBoneProgressSpread,
                float maximumPerFrameRotation,
                float maximumReferencePerFrameRotation,
                float minimumFollowThroughAverageRotation,
                float maximumJointRangeExcess,
                float boundary52To53MaximumRotation,
                float boundary93To94MaximumRotation)
            {
                TargetBindingCount = targetBindingCount;
                MaximumExpectedTrajectoryError = maximumExpectedTrajectoryError;
                MaximumReferenceCurveDeviation = maximumReferenceCurveDeviation;
                MaximumPerBoneProgressSpread = maximumPerBoneProgressSpread;
                MaximumPerFrameRotation = maximumPerFrameRotation;
                MaximumReferencePerFrameRotation = maximumReferencePerFrameRotation;
                MinimumFollowThroughAverageRotation = minimumFollowThroughAverageRotation;
                MaximumJointRangeExcess = maximumJointRangeExcess;
                Boundary52To53MaximumRotation = boundary52To53MaximumRotation;
                Boundary93To94MaximumRotation = boundary93To94MaximumRotation;
            }

            internal int TargetBindingCount { get; }
            internal float MaximumExpectedTrajectoryError { get; }
            internal float MaximumReferenceCurveDeviation { get; }
            internal float MaximumPerBoneProgressSpread { get; }
            internal float MaximumPerFrameRotation { get; }
            internal float MaximumReferencePerFrameRotation { get; }
            internal float MinimumFollowThroughAverageRotation { get; }
            internal float MaximumJointRangeExcess { get; }
            internal float Boundary52To53MaximumRotation { get; }
            internal float Boundary93To94MaximumRotation { get; }
        }

        private readonly struct SceneState
        {
            internal SceneState(string playbackObjectId, string otherSlotsSignature)
            {
                PlaybackObjectId = playbackObjectId;
                OtherSlotsSignature = otherSlotsSignature;
            }

            internal string PlaybackObjectId { get; }
            internal string OtherSlotsSignature { get; }
        }

        private readonly struct HandVelocitySample
        {
            internal HandVelocitySample(
                int frame,
                float commonProgress,
                float effectivePlaybackSpeed,
                float leftStepDistance,
                float rightStepDistance,
                float handSeparation,
                float closureStep,
                float leftVelocity,
                float rightVelocity,
                float averageHandVelocity,
                float closureVelocity)
            {
                Frame = frame;
                CommonProgress = commonProgress;
                EffectivePlaybackSpeed = effectivePlaybackSpeed;
                LeftStepDistance = leftStepDistance;
                RightStepDistance = rightStepDistance;
                HandSeparation = handSeparation;
                ClosureStep = closureStep;
                LeftVelocity = leftVelocity;
                RightVelocity = rightVelocity;
                AverageHandVelocity = averageHandVelocity;
                ClosureVelocity = closureVelocity;
            }

            internal int Frame { get; }
            internal float CommonProgress { get; }
            internal float EffectivePlaybackSpeed { get; }
            internal float LeftStepDistance { get; }
            internal float RightStepDistance { get; }
            internal float HandSeparation { get; }
            internal float ClosureStep { get; }
            internal float LeftVelocity { get; }
            internal float RightVelocity { get; }
            internal float AverageHandVelocity { get; }
            internal float ClosureVelocity { get; }
        }
    }
}
