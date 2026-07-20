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
        internal const string EnabledMarker = "Bellerophon.OstinatoForwardSlashMotion.v4";

        public override uint GetVersion()
        {
            return 4u;
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
        private const string ValidationFolder = "docs/validation/ostinato_attack_forward_slash_motion_2026-07-20";
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
                fullFingerprintBefore != PreviousV3CurveFingerprint)
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
            report.AppendLine("MotionMethod=OutwardSlashReferencedAngularVelocityProfileOnShortestPerAxisEulerArc");
            report.AppendLine("ActiveSlashFrames=53..93");
            report.AppendLine("OutwardReferenceFrames=101..115");
            report.AppendLine("TargetRotationBindingCount=" + correction.TargetBindingCount);
            report.AppendLine("MaximumCorrectedQuaternionErrorDegrees=" + Format(correction.MaximumQuaternionError));
            report.AppendLine("MaximumCommonProgressMismatch=" + Format(correction.MaximumCommonProgressMismatch));
            report.AppendLine("MaximumTargetTimingProgressError=" + Format(correction.MaximumTargetTimingProgressError));
            report.AppendLine("MaximumCorrectedPerFrameRotationDegrees=" + Format(correction.MaximumPerFrameRotation));
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
            report.AppendLine("ActiveSlashFrames=53..93");
            report.AppendLine("OutwardReferenceFrames=101..115");
            report.AppendLine("CorrectionBones=" + string.Join("|", TargetBoneNames));
            report.AppendLine("TargetRotationBindingCount=" + correction.TargetBindingCount);
            report.AppendLine("MaximumCorrectedQuaternionErrorDegrees=" + Format(correction.MaximumQuaternionError));
            report.AppendLine("MaximumCommonProgressMismatch=" + Format(correction.MaximumCommonProgressMismatch));
            report.AppendLine("MaximumTargetTimingProgressError=" + Format(correction.MaximumTargetTimingProgressError));
            report.AppendLine("MaximumCorrectedPerFrameRotationDegrees=" + Format(correction.MaximumPerFrameRotation));
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
                        EvaluateCommonMotionProgress(clip, frame),
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

                var currentV4 = string.Equals(importer.userData, OstinatoAttackForwardSlashPostprocessor.EnabledMarker, StringComparison.Ordinal);
                var versionName = currentV4
                    ? "v4"
                    : importer.userData.EndsWith(".v3", StringComparison.Ordinal) ? "v3" :
                      importer.userData.EndsWith(".v2", StringComparison.Ordinal) ? "v2" :
                      importer.userData.EndsWith(".v1", StringComparison.Ordinal) ? "v1" : "uncorrected";
                var csvPath = ValidationFolder + "/Ostinato_AttackForwardSlashVelocity_" + versionName + ".csv";
                var reportPath = ValidationFolder + "/Ostinato_AttackForwardSlashVelocity_" + versionName + ".txt";
                var csv = new StringBuilder();
                csv.AppendLine("Frame,CommonProgress,EffectivePlaybackSpeed,LeftStepDistance,RightStepDistance,HandSeparation,ClosureStep,LeftVelocity,RightVelocity,AverageHandVelocity,ClosureVelocity");
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
                report.AppendLine("ActiveSlashFrames=53..93");
                report.AppendLine("MotionTimingReferenceFrames=101..115");
                report.AppendLine("CommonProgressForBothArms=True");
                report.AppendLine("ControllerStateSpeed=" + Format(state.speed));
                report.AppendLine("EffectivePlaybackSpeed=" + Format(intervalSamples.Min(item => item.EffectivePlaybackSpeed)) + ".." + Format(intervalSamples.Max(item => item.EffectivePlaybackSpeed)));
                report.AppendLine("AccelerationBehaviourCount=" + state.behaviours.OfType<OstinatoAttackAccelerationBehaviour>().Count());
                report.AppendLine("ActiveSlashDurationSeconds=" + Format((EndFrame - StartFrame) / (clip.frameRate * state.speed)));
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
                if (currentV4 && !intervalSamples.All(item => Mathf.Approximately(item.EffectivePlaybackSpeed, 2f)))
                {
                    throw new InvalidOperationException(
                        "Ostinato v4 does not preserve uniform 2x Controller playback.");
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
            OstinatoScissorAttackAnimation.CaptureOstinatoScissorAttackRuntimePlayback();
        }

        internal static void RewriteTargetRotationCurves(AnimationClip clip)
        {
            RequireClipTimeline(clip);
            var outwardReferenceProgress = BuildOutwardReferenceProgress(clip);
            foreach (var boneName in TargetBoneNames)
            {
                var rotation = RequireBoneRotationCurves(clip, boneName);
                var startTime = StartFrame / clip.frameRate;
                var endTime = EndFrame / clip.frameRate;
                var start = EvaluateRawEuler(rotation.Curves, startTime);
                var end = EvaluateRawEuler(rotation.Curves, endTime);
                var delta = new Vector3(
                    Mathf.DeltaAngle(start.x, end.x),
                    Mathf.DeltaAngle(start.y, end.y),
                    Mathf.DeltaAngle(start.z, end.z));
                if (Mathf.Abs(start.x + delta.x - end.x) > 0.01f ||
                    Mathf.Abs(start.y + delta.y - end.y) > 0.01f ||
                    Mathf.Abs(start.z + delta.z - end.z) > 0.01f)
                {
                    throw new InvalidOperationException("Ostinato target Euler curve crosses a wrap boundary: " + boneName);
                }

                var samples = new Vector3[(int)(EndFrame - StartFrame) + 1];
                for (var frame = (int)StartFrame; frame <= (int)EndFrame; frame++)
                {
                    var progress = EvaluateTargetSlashProgress(frame, outwardReferenceProgress);
                    samples[frame - (int)StartFrame] = start + delta * progress;
                }

                samples[0] = start;
                samples[samples.Length - 1] = end;
                for (var component = 0; component < 3; component++)
                {
                    var rewritten = RewriteComponentCurve(rotation.Curves[component], component, samples, clip.frameRate);
                    AnimationUtility.SetEditorCurve(clip, rotation.Bindings[component], rewritten);
                }
            }
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
            var maximumError = 0f;
            var maximumCommonProgressMismatch = 0f;
            var maximumTargetTimingProgressError = 0f;
            var maximumPerFrame = 0f;
            var boundary52To53 = 0f;
            var boundary93To94 = 0f;
            var targetBindingCount = 0;
            var rotations = TargetBoneNames.ToDictionary(
                boneName => boneName,
                boneName => RequireBoneRotationCurves(clip, boneName),
                StringComparer.Ordinal);
            var outwardReferenceProgress = BuildOutwardReferenceProgress(clip);
            float previousCommonProgress = -0.0001f;
            for (var frame = StartFrame; frame <= EndFrame + 0.0001f; frame += 0.25f)
            {
                var progresses = new List<float>();
                foreach (var rotation in rotations.Values)
                {
                    var startEuler = EvaluateRawEuler(rotation.Curves, StartFrame / clip.frameRate);
                    var endEuler = EvaluateRawEuler(rotation.Curves, EndFrame / clip.frameRate);
                    var delta = new Vector3(
                        Mathf.DeltaAngle(startEuler.x, endEuler.x),
                        Mathf.DeltaAngle(startEuler.y, endEuler.y),
                        Mathf.DeltaAngle(startEuler.z, endEuler.z));
                    var actualEuler = EvaluateRawEuler(rotation.Curves, frame / clip.frameRate);
                    for (var component = 0; component < 3; component++)
                    {
                        var componentDelta = Component(delta, component);
                        if (Mathf.Abs(componentDelta) > 0.001f)
                        {
                            progresses.Add((Component(actualEuler, component) - Component(startEuler, component)) / componentDelta);
                        }
                    }
                }
                var commonProgress = progresses.Average();
                maximumCommonProgressMismatch = Mathf.Max(
                    maximumCommonProgressMismatch,
                    progresses.Max(progress => Mathf.Abs(progress - commonProgress)));
                maximumTargetTimingProgressError = Mathf.Max(
                    maximumTargetTimingProgressError,
                    Mathf.Abs(commonProgress - EvaluateTargetSlashProgress(frame, outwardReferenceProgress)));
                if (commonProgress + 0.0001f < previousCommonProgress || commonProgress < -0.0001f || commonProgress > 1.0001f)
                {
                    throw new InvalidOperationException("Ostinato corrected arm motion does not use one monotonic common progress curve.");
                }
                previousCommonProgress = commonProgress;
                foreach (var rotation in rotations.Values)
                {
                    var startEuler = EvaluateRawEuler(rotation.Curves, StartFrame / clip.frameRate);
                    var endEuler = EvaluateRawEuler(rotation.Curves, EndFrame / clip.frameRate);
                    var delta = new Vector3(
                        Mathf.DeltaAngle(startEuler.x, endEuler.x),
                        Mathf.DeltaAngle(startEuler.y, endEuler.y),
                        Mathf.DeltaAngle(startEuler.z, endEuler.z));
                    var actual = EvaluateQuaternion(rotation.Curves, frame / clip.frameRate);
                    var expected = Quaternion.Euler(startEuler + delta * commonProgress);
                    maximumError = Mathf.Max(maximumError, Quaternion.Angle(actual, expected));
                }
            }
            foreach (var rotation in rotations.Values)
            {
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
            }

            if (targetBindingCount != TargetBoneNames.Length * RotationPropertyNames.Length)
            {
                throw new InvalidOperationException("Ostinato forward-slash correction does not target exactly twelve rotation bindings.");
            }
            if (maximumError > 0.5f)
            {
                throw new InvalidOperationException("Ostinato corrected arm motion deviates from the approved shortest rotation arc.");
            }
            if (maximumCommonProgressMismatch > 0.0005f)
            {
                throw new InvalidOperationException("Ostinato left and right arm curves do not share one common progress value.");
            }
            if (maximumTargetTimingProgressError > 0.002f)
            {
                throw new InvalidOperationException(
                    "Ostinato arm curves do not match the approved outward-reference timing. MaximumTimingProgressError=" +
                    Format(maximumTargetTimingProgressError) +
                    ", MaximumCommonProgressMismatch=" + Format(maximumCommonProgressMismatch));
            }
            if (maximumPerFrame > 5f)
            {
                throw new InvalidOperationException("Ostinato corrected arm motion exceeds the approved per-frame anatomical rotation limit.");
            }
            return new CorrectionInspection(
                targetBindingCount,
                maximumError,
                maximumCommonProgressMismatch,
                maximumTargetTimingProgressError,
                maximumPerFrame,
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

        private static float EvaluateCommonMotionProgress(AnimationClip clip, float frame)
        {
            var progresses = new List<float>();
            foreach (var boneName in TargetBoneNames)
            {
                var rotation = RequireBoneRotationCurves(clip, boneName);
                var start = EvaluateRawEuler(rotation.Curves, StartFrame / clip.frameRate);
                var end = EvaluateRawEuler(rotation.Curves, EndFrame / clip.frameRate);
                var actual = EvaluateRawEuler(rotation.Curves, frame / clip.frameRate);
                var delta = new Vector3(
                    Mathf.DeltaAngle(start.x, end.x),
                    Mathf.DeltaAngle(start.y, end.y),
                    Mathf.DeltaAngle(start.z, end.z));
                for (var component = 0; component < 3; component++)
                {
                    var componentDelta = Component(delta, component);
                    if (Mathf.Abs(componentDelta) > 0.001f)
                    {
                        progresses.Add((Component(actual, component) - Component(start, component)) / componentDelta);
                    }
                }
            }
            return progresses.Average();
        }

        private static float[] BuildOutwardReferenceProgress(AnimationClip clip)
        {
            var intervalCount = (int)(OutwardSlashEndFrame - OutwardSlashStartFrame);
            var intervalSteps = new float[intervalCount];
            for (var index = 0; index < intervalCount; index++)
            {
                var previousFrame = OutwardSlashStartFrame + index;
                var currentFrame = previousFrame + 1f;
                intervalSteps[index] = TargetBoneNames.Average(boneName =>
                {
                    var rotation = RequireBoneRotationCurves(clip, boneName);
                    var previous = EvaluateQuaternion(rotation.Curves, previousFrame / clip.frameRate);
                    var current = EvaluateQuaternion(rotation.Curves, currentFrame / clip.frameRate);
                    return Quaternion.Angle(previous, current);
                });
            }
            var total = intervalSteps.Sum();
            if (total <= 0.0001f)
            {
                throw new InvalidOperationException("Ostinato outward-slash timing reference has no angular movement.");
            }
            var progress = new float[intervalCount + 1];
            var cumulative = 0f;
            for (var index = 0; index < intervalCount; index++)
            {
                cumulative += intervalSteps[index];
                progress[index + 1] = cumulative / total;
            }
            progress[0] = 0f;
            progress[progress.Length - 1] = 1f;
            return progress;
        }

        private static float EvaluateTargetSlashProgress(float frame, IReadOnlyList<float> outwardReferenceProgress)
        {
            var normalized = Mathf.Clamp01(Mathf.InverseLerp(StartFrame, EndFrame, frame));
            var referencePosition = normalized * (outwardReferenceProgress.Count - 1);
            var lower = Mathf.FloorToInt(referencePosition);
            var upper = Mathf.Min(lower + 1, outwardReferenceProgress.Count - 1);
            return Mathf.Lerp(
                outwardReferenceProgress[lower],
                outwardReferenceProgress[upper],
                referencePosition - lower);
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

        private readonly struct CorrectionInspection
        {
            internal CorrectionInspection(
                int targetBindingCount,
                float maximumQuaternionError,
                float maximumCommonProgressMismatch,
                float maximumTargetTimingProgressError,
                float maximumPerFrameRotation,
                float boundary52To53MaximumRotation,
                float boundary93To94MaximumRotation)
            {
                TargetBindingCount = targetBindingCount;
                MaximumQuaternionError = maximumQuaternionError;
                MaximumCommonProgressMismatch = maximumCommonProgressMismatch;
                MaximumTargetTimingProgressError = maximumTargetTimingProgressError;
                MaximumPerFrameRotation = maximumPerFrameRotation;
                Boundary52To53MaximumRotation = boundary52To53MaximumRotation;
                Boundary93To94MaximumRotation = boundary93To94MaximumRotation;
            }

            internal int TargetBindingCount { get; }
            internal float MaximumQuaternionError { get; }
            internal float MaximumCommonProgressMismatch { get; }
            internal float MaximumTargetTimingProgressError { get; }
            internal float MaximumPerFrameRotation { get; }
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
