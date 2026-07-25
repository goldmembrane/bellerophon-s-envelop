using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor
{
    internal static class OstinatoAttackMotionSegmentAnalysis
    {
        private const string ValidationFolder = "docs/validation/ostinato_attack_motion_analysis_2026-07-20";
        private const string CsvPath = ValidationFolder + "/Ostinato_AttackMotionPerFrame.csv";
        private const string SummaryPath = ValidationFolder + "/Ostinato_AttackMotionAnalysis.txt";

        private static readonly string[] TrackedBoneNames =
        {
            "Hips", "Spine", "Head",
            "LeftShoulder", "LeftArm", "LeftForeArm", "LeftHand",
            "RightShoulder", "RightArm", "RightForeArm", "RightHand",
            "LeftUpLeg", "LeftLeg", "LeftFoot", "RightUpLeg", "RightLeg", "RightFoot",
        };

        private static readonly Dictionary<string, int> BoneIndices = TrackedBoneNames
            .Select((name, index) => new { name, index })
            .ToDictionary(entry => entry.name, entry => entry.index);

        private static readonly DetailedSegment[] DetailedSegments =
        {
            new DetailedSegment("InitialCompression", 0, 7),
            new DetailedSegment("RisingSpread", 8, 30),
            new DetailedSegment("ClosingEntry", 31, 52),
            new DetailedSegment("SlowScissorClose", 53, 84),
            new DetailedSegment("PrimaryImpactClose", 85, 93),
            new DetailedSegment("PostImpactTransition", 94, 100),
            new DetailedSegment("RapidHorizontalReopen", 101, 115),
            new DetailedSegment("RecoveryStart", 116, 150),
            new DetailedSegment("ReturnToDefault", 151, 183),
            new DetailedSegment("LoopHold", 184, 196),
        };

        public static void AnalyzeOstinatoAttackMotionSegments()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != OstinatoScissorAttackAnimation.ScenePath)
            {
                throw new InvalidOperationException("CargoRunMvp must be the active scene for Ostinato attack analysis.");
            }
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException("Ostinato attack analysis must run in Edit Mode.");
            }

            var placement = scene.GetRootGameObjects()
                .Single(root => root.name == OstinatoScissorAttackAnimation.PlacementRootName).transform;
            var slot = placement.GetChild(3);
            if (slot.name != OstinatoScissorAttackAnimation.AttackSlotName || slot.childCount != 1)
            {
                throw new InvalidOperationException("Ostinato attack slot is not ready for motion analysis.");
            }
            var model = slot.GetChild(0).gameObject;
            var clip = AssetDatabase.LoadAllAssetsAtPath(OstinatoScissorAttackAnimation.AttackModelPath)
                .OfType<AnimationClip>()
                .Single(candidate => !candidate.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase) &&
                                     candidate.name.IndexOf("mixamo.com", StringComparison.OrdinalIgnoreCase) >= 0);
            var bones = TrackedBoneNames.ToDictionary(name => name, name => FindRequiredBone(model.transform, name));
            var frameRate = clip.frameRate;
            var finalFrame = Mathf.RoundToInt(clip.length * frameRate);
            var modelPosition = model.transform.localPosition;
            var modelRotation = model.transform.localRotation;
            var modelScale = model.transform.localScale;
            var samples = new List<MotionSample>(finalFrame + 1);

            AnimationMode.StartAnimationMode();
            try
            {
                MotionSample previous = null;
                for (var frame = 0; frame <= finalFrame; frame++)
                {
                    AnimationMode.BeginSampling();
                    AnimationMode.SampleAnimationClip(model, clip, Mathf.Min(frame / frameRate, clip.length));
                    AnimationMode.EndSampling();
                    var sample = MotionSample.Capture(frame, frame / frameRate, model.transform, bones, previous);
                    samples.Add(sample);
                    previous = sample;
                }
            }
            finally
            {
                AnimationMode.StopAnimationMode();
            }

            foreach (var sample in samples) sample.SetPoseDifferenceFrom(samples[0]);

            if (Vector3.Distance(model.transform.localPosition, modelPosition) > 0.0001f ||
                Quaternion.Angle(model.transform.localRotation, modelRotation) > 0.001f ||
                Vector3.Distance(model.transform.localScale, modelScale) > 0.0001f)
            {
                throw new InvalidOperationException("Ostinato attack model root changed during analysis sampling.");
            }

            var csv = new StringBuilder();
            csv.AppendLine("Frame,Time,LeftHandX,LeftHandY,LeftHandZ,RightHandX,RightHandY,RightHandZ," +
                           "LeftHandDeltaX,LeftHandDeltaY,LeftHandDeltaZ,RightHandDeltaX,RightHandDeltaY,RightHandDeltaZ," +
                           "HandSeparation,AverageHandHeight,AverageHandDepth,VerticalHandOffset,DepthHandOffset," +
                           "LeftHandTravel,RightHandTravel,HandTravel," +
                           "LeftShoulderRotationDelta,LeftArmRotationDelta,LeftForeArmRotationDelta,LeftHandRotationDelta," +
                           "RightShoulderRotationDelta,RightArmRotationDelta,RightForeArmRotationDelta,RightHandRotationDelta," +
                           "HipsRotationDelta,SpineRotationDelta,HeadRotationDelta," +
                           "LeftUpLegRotationDelta,LeftLegRotationDelta,LeftFootRotationDelta," +
                           "RightUpLegRotationDelta,RightLegRotationDelta,RightFootRotationDelta," +
                           "HipsX,HipsY,HipsZ,HeadX,HeadY,HeadZ,LeftFootX,LeftFootY,LeftFootZ,RightFootX,RightFootY,RightFootZ," +
                           "LeftArmPoseDifference,RightArmPoseDifference,TorsoPoseDifference,LeftLegPoseDifference,RightLegPoseDifference," +
                           "ArmRotationDelta,TorsoRotationDelta,LegRotationDelta,MotionScore");
            foreach (var sample in samples) csv.AppendLine(sample.ToCsv());
            OstinatoScissorAttackAnimation.WriteText(CsvPath, csv.ToString());

            var movingSamples = samples.Skip(1).ToArray();
            var orderedScores = movingSamples.Select(sample => sample.MotionScore).OrderBy(value => value).ToArray();
            var slowThreshold = orderedScores[Mathf.Clamp(Mathf.FloorToInt(orderedScores.Length * 0.25f), 0, orderedScores.Length - 1)];
            var slowRanges = BuildSlowRanges(samples, slowThreshold);
            var peaks = FindSeparatedPeaks(samples, 12, 6);
            var summary = new StringBuilder();
            summary.AppendLine("Scene=" + scene.path);
            summary.AppendLine("Target=" + OstinatoScissorAttackAnimation.PlacementRootName + "/" + OstinatoScissorAttackAnimation.AttackSlotName);
            summary.AppendLine("Clip=" + clip.name);
            summary.AppendLine("FrameRange=0-" + finalFrame.ToString(CultureInfo.InvariantCulture));
            summary.AppendLine("FrameRate=" + frameRate.ToString("0.###", CultureInfo.InvariantCulture));
            summary.AppendLine("Length=" + clip.length.ToString("0.######", CultureInfo.InvariantCulture));
            summary.AppendLine("TrackedBones=" + string.Join("|", TrackedBoneNames));
            summary.AppendLine("SlowThreshold=" + Format(slowThreshold));
            summary.AppendLine("SlowRanges=" + string.Join("|", slowRanges.Select(range => range.Start + "-" + range.End)));
            summary.AppendLine("MotionPeaks=" + string.Join("|", peaks.Select(sample => sample.Frame + ":" + Format(sample.MotionScore))));
            summary.AppendLine("MinimumHandSeparation=" + DescribeExtremum(samples, sample => sample.HandSeparation, false));
            summary.AppendLine("MaximumHandSeparation=" + DescribeExtremum(samples, sample => sample.HandSeparation, true));
            summary.AppendLine("MinimumAverageHandHeight=" + DescribeExtremum(samples, sample => sample.AverageHandHeight, false));
            summary.AppendLine("MaximumAverageHandHeight=" + DescribeExtremum(samples, sample => sample.AverageHandHeight, true));
            summary.AppendLine("MinimumAverageHandDepth=" + DescribeExtremum(samples, sample => sample.AverageHandDepth, false));
            summary.AppendLine("MaximumAverageHandDepth=" + DescribeExtremum(samples, sample => sample.AverageHandDepth, true));
            summary.AppendLine("MaximumVerticalHandOffset=" + DescribeExtremum(samples, sample => sample.VerticalHandOffset, true));
            summary.AppendLine("MaximumDepthHandOffset=" + DescribeExtremum(samples, sample => sample.DepthHandOffset, true));
            AppendDetailedExtrema(summary, samples);
            AppendSegmentMetrics(summary, samples);
            summary.AppendLine("RootTransformRestored=True");
            summary.AppendLine("AnimationModified=False");
            summary.AppendLine("SceneSaved=False");
            summary.AppendLine("PerFrameCsv=" + CsvPath);
            OstinatoScissorAttackAnimation.WriteText(SummaryPath, summary.ToString());
            Debug.Log("OstinatoAttackMotionSegmentsAnalyzed, Frames=0-" + finalFrame +
                      ", Peaks=" + string.Join("|", peaks.Select(sample => sample.Frame)) +
                      ", SlowRanges=" + string.Join("|", slowRanges.Select(range => range.Start + "-" + range.End)));
        }

        private static void AppendDetailedExtrema(StringBuilder summary, IReadOnlyList<MotionSample> samples)
        {
            summary.AppendLine("MaximumLeftHandTravel=" + DescribeExtremum(samples, sample => sample.LeftHandTravel, true));
            summary.AppendLine("MaximumRightHandTravel=" + DescribeExtremum(samples, sample => sample.RightHandTravel, true));
            foreach (var boneName in TrackedBoneNames)
            {
                summary.AppendLine("Maximum" + boneName + "RotationDelta=" +
                                   DescribeExtremum(samples, sample => sample.BoneRotationDelta(boneName), true));
            }
            summary.AppendLine("MaximumLeftArmPoseDifference=" + DescribeExtremum(samples, sample => sample.LeftArmPoseDifference, true));
            summary.AppendLine("MaximumRightArmPoseDifference=" + DescribeExtremum(samples, sample => sample.RightArmPoseDifference, true));
            summary.AppendLine("MaximumTorsoPoseDifference=" + DescribeExtremum(samples, sample => sample.TorsoPoseDifference, true));
            summary.AppendLine("MaximumLeftLegPoseDifference=" + DescribeExtremum(samples, sample => sample.LeftLegPoseDifference, true));
            summary.AppendLine("MaximumRightLegPoseDifference=" + DescribeExtremum(samples, sample => sample.RightLegPoseDifference, true));
        }

        private static void AppendSegmentMetrics(StringBuilder summary, IReadOnlyList<MotionSample> samples)
        {
            foreach (var segment in DetailedSegments)
            {
                var start = samples[segment.Start];
                var end = samples[segment.End];
                var movement = samples.Where(sample => sample.Frame > segment.Start && sample.Frame <= segment.End).ToArray();
                var peak = samples.Where(sample => sample.Frame >= segment.Start && sample.Frame <= segment.End)
                    .OrderByDescending(sample => sample.MotionScore).First();
                summary.AppendLine("Segment[" + segment.Start + "-" + segment.End + "]=" + segment.Name +
                                   ";Peak=" + peak.Frame + ":" + Format(peak.MotionScore) +
                                   ";LeftHandTravel=" + Format(movement.Sum(sample => sample.LeftHandTravel)) +
                                   ";RightHandTravel=" + Format(movement.Sum(sample => sample.RightHandTravel)) +
                                   ";LeftShoulderRotation=" + Format(movement.Sum(sample => sample.BoneRotationDelta("LeftShoulder"))) +
                                   ";LeftArmRotation=" + Format(movement.Sum(sample => sample.BoneRotationDelta("LeftArm"))) +
                                   ";LeftForeArmRotation=" + Format(movement.Sum(sample => sample.BoneRotationDelta("LeftForeArm"))) +
                                   ";LeftHandRotation=" + Format(movement.Sum(sample => sample.BoneRotationDelta("LeftHand"))) +
                                   ";RightShoulderRotation=" + Format(movement.Sum(sample => sample.BoneRotationDelta("RightShoulder"))) +
                                   ";RightArmRotation=" + Format(movement.Sum(sample => sample.BoneRotationDelta("RightArm"))) +
                                   ";RightForeArmRotation=" + Format(movement.Sum(sample => sample.BoneRotationDelta("RightForeArm"))) +
                                   ";RightHandRotation=" + Format(movement.Sum(sample => sample.BoneRotationDelta("RightHand"))) +
                                   ";HipsRotation=" + Format(movement.Sum(sample => sample.BoneRotationDelta("Hips"))) +
                                   ";SpineRotation=" + Format(movement.Sum(sample => sample.BoneRotationDelta("Spine"))) +
                                   ";HeadRotation=" + Format(movement.Sum(sample => sample.BoneRotationDelta("Head"))) +
                                   ";LeftLegRotation=" + Format(movement.Sum(sample => sample.LeftLegRotationDelta)) +
                                   ";RightLegRotation=" + Format(movement.Sum(sample => sample.RightLegRotationDelta)) +
                                   ";LeftHandOffset=" + FormatVector(end.LeftHand - start.LeftHand) +
                                   ";RightHandOffset=" + FormatVector(end.RightHand - start.RightHand) +
                                   ";HipsOffset=" + FormatVector(end.Hips - start.Hips) +
                                   ";HeadOffset=" + FormatVector(end.Head - start.Head) +
                                   ";LeftFootOffset=" + FormatVector(end.LeftFoot - start.LeftFoot) +
                                   ";RightFootOffset=" + FormatVector(end.RightFoot - start.RightFoot));
            }
        }

        private static Transform FindRequiredBone(Transform root, string name)
        {
            return root.GetComponentsInChildren<Transform>(true).SingleOrDefault(target => target.name == name) ??
                   throw new InvalidOperationException("Required Ostinato analysis bone is missing: " + name);
        }

        private static List<FrameRange> BuildSlowRanges(IReadOnlyList<MotionSample> samples, float threshold)
        {
            var ranges = new List<FrameRange>();
            var start = -1;
            for (var frame = 1; frame < samples.Count; frame++)
            {
                if (samples[frame].MotionScore <= threshold)
                {
                    if (start < 0) start = frame;
                    continue;
                }
                if (start >= 0 && frame - start >= 3) ranges.Add(new FrameRange(start, frame - 1));
                start = -1;
            }
            if (start >= 0 && samples.Count - start >= 3) ranges.Add(new FrameRange(start, samples.Count - 1));
            return ranges;
        }

        private static MotionSample[] FindSeparatedPeaks(IReadOnlyList<MotionSample> samples, int count, int minimumDistance)
        {
            var selected = new List<MotionSample>();
            foreach (var candidate in samples.Skip(1).OrderByDescending(sample => sample.MotionScore))
            {
                if (selected.All(sample => Mathf.Abs(sample.Frame - candidate.Frame) >= minimumDistance))
                {
                    selected.Add(candidate);
                    if (selected.Count == count) break;
                }
            }
            return selected.OrderBy(sample => sample.Frame).ToArray();
        }

        private static string DescribeExtremum(IEnumerable<MotionSample> samples, Func<MotionSample, float> selector, bool maximum)
        {
            var sample = maximum ? samples.OrderByDescending(selector).First() : samples.OrderBy(selector).First();
            return sample.Frame.ToString(CultureInfo.InvariantCulture) + ":" + Format(selector(sample));
        }

        private static string Format(float value) => value.ToString("0.######", CultureInfo.InvariantCulture);

        private static string FormatVector(Vector3 value)
        {
            return "(" + Format(value.x) + "|" + Format(value.y) + "|" + Format(value.z) + ")";
        }

        private sealed class MotionSample
        {
            private readonly Quaternion[] boneRotations;
            private readonly float[] boneRotationDeltas;
            private readonly float[] bonePoseDifferences;

            public int Frame { get; private set; }
            public float Time { get; private set; }
            public Vector3 LeftHand { get; private set; }
            public Vector3 RightHand { get; private set; }
            public Vector3 LeftHandDelta { get; private set; }
            public Vector3 RightHandDelta { get; private set; }
            public Vector3 Hips { get; private set; }
            public Vector3 Head { get; private set; }
            public Vector3 LeftFoot { get; private set; }
            public Vector3 RightFoot { get; private set; }
            public float HandSeparation => Vector3.Distance(LeftHand, RightHand);
            public float AverageHandHeight => (LeftHand.y + RightHand.y) * 0.5f;
            public float AverageHandDepth => (LeftHand.z + RightHand.z) * 0.5f;
            public float VerticalHandOffset => Mathf.Abs(LeftHand.y - RightHand.y);
            public float DepthHandOffset => Mathf.Abs(LeftHand.z - RightHand.z);
            public float LeftHandTravel { get; private set; }
            public float RightHandTravel { get; private set; }
            public float HandTravel => LeftHandTravel + RightHandTravel;
            public float LeftArmRotationDelta => BoneRotationDelta("LeftShoulder") + BoneRotationDelta("LeftArm") +
                                                 BoneRotationDelta("LeftForeArm") + BoneRotationDelta("LeftHand");
            public float RightArmRotationDelta => BoneRotationDelta("RightShoulder") + BoneRotationDelta("RightArm") +
                                                  BoneRotationDelta("RightForeArm") + BoneRotationDelta("RightHand");
            public float ArmRotationDelta { get; private set; }
            public float TorsoRotationDelta { get; private set; }
            public float LeftLegRotationDelta => BoneRotationDelta("LeftUpLeg") + BoneRotationDelta("LeftLeg") + BoneRotationDelta("LeftFoot");
            public float RightLegRotationDelta => BoneRotationDelta("RightUpLeg") + BoneRotationDelta("RightLeg") + BoneRotationDelta("RightFoot");
            public float LegRotationDelta => LeftLegRotationDelta + RightLegRotationDelta;
            public float LeftArmPoseDifference => BonePoseDifference("LeftShoulder") + BonePoseDifference("LeftArm") +
                                                   BonePoseDifference("LeftForeArm") + BonePoseDifference("LeftHand");
            public float RightArmPoseDifference => BonePoseDifference("RightShoulder") + BonePoseDifference("RightArm") +
                                                    BonePoseDifference("RightForeArm") + BonePoseDifference("RightHand");
            public float TorsoPoseDifference => BonePoseDifference("Hips") + BonePoseDifference("Spine") + BonePoseDifference("Head");
            public float LeftLegPoseDifference => BonePoseDifference("LeftUpLeg") + BonePoseDifference("LeftLeg") + BonePoseDifference("LeftFoot");
            public float RightLegPoseDifference => BonePoseDifference("RightUpLeg") + BonePoseDifference("RightLeg") + BonePoseDifference("RightFoot");
            public float MotionScore => HandTravel * 100f + ArmRotationDelta + TorsoRotationDelta;

            private MotionSample(Quaternion[] rotations)
            {
                boneRotations = rotations;
                boneRotationDeltas = new float[rotations.Length];
                bonePoseDifferences = new float[rotations.Length];
            }

            public static MotionSample Capture(int frame, float time, Transform model,
                IReadOnlyDictionary<string, Transform> bones, MotionSample previous)
            {
                var rotations = TrackedBoneNames.Select(name => bones[name].localRotation).ToArray();
                var sample = new MotionSample(rotations)
                {
                    Frame = frame,
                    Time = time,
                    LeftHand = model.InverseTransformPoint(bones["LeftHand"].position),
                    RightHand = model.InverseTransformPoint(bones["RightHand"].position),
                    Hips = model.InverseTransformPoint(bones["Hips"].position),
                    Head = model.InverseTransformPoint(bones["Head"].position),
                    LeftFoot = model.InverseTransformPoint(bones["LeftFoot"].position),
                    RightFoot = model.InverseTransformPoint(bones["RightFoot"].position),
                };
                if (previous == null) return sample;
                sample.LeftHandDelta = sample.LeftHand - previous.LeftHand;
                sample.RightHandDelta = sample.RightHand - previous.RightHand;
                sample.LeftHandTravel = sample.LeftHandDelta.magnitude;
                sample.RightHandTravel = sample.RightHandDelta.magnitude;
                for (var index = 0; index < rotations.Length; index++)
                {
                    sample.boneRotationDeltas[index] = Quaternion.Angle(rotations[index], previous.boneRotations[index]);
                }
                sample.ArmRotationDelta = sample.LeftArmRotationDelta + sample.RightArmRotationDelta;
                sample.TorsoRotationDelta = sample.BoneRotationDelta("Hips") + sample.BoneRotationDelta("Spine") +
                                             sample.BoneRotationDelta("Head");
                return sample;
            }

            public void SetPoseDifferenceFrom(MotionSample reference)
            {
                for (var index = 0; index < boneRotations.Length; index++)
                {
                    bonePoseDifferences[index] = Quaternion.Angle(boneRotations[index], reference.boneRotations[index]);
                }
            }

            public float BoneRotationDelta(string boneName) => boneRotationDeltas[BoneIndices[boneName]];

            private float BonePoseDifference(string boneName) => bonePoseDifferences[BoneIndices[boneName]];

            public string ToCsv()
            {
                return string.Join(",", new[]
                {
                    Frame.ToString(CultureInfo.InvariantCulture), Format(Time),
                    Format(LeftHand.x), Format(LeftHand.y), Format(LeftHand.z),
                    Format(RightHand.x), Format(RightHand.y), Format(RightHand.z),
                    Format(LeftHandDelta.x), Format(LeftHandDelta.y), Format(LeftHandDelta.z),
                    Format(RightHandDelta.x), Format(RightHandDelta.y), Format(RightHandDelta.z),
                    Format(HandSeparation), Format(AverageHandHeight), Format(AverageHandDepth),
                    Format(VerticalHandOffset), Format(DepthHandOffset), Format(LeftHandTravel), Format(RightHandTravel), Format(HandTravel),
                    Format(BoneRotationDelta("LeftShoulder")), Format(BoneRotationDelta("LeftArm")),
                    Format(BoneRotationDelta("LeftForeArm")), Format(BoneRotationDelta("LeftHand")),
                    Format(BoneRotationDelta("RightShoulder")), Format(BoneRotationDelta("RightArm")),
                    Format(BoneRotationDelta("RightForeArm")), Format(BoneRotationDelta("RightHand")),
                    Format(BoneRotationDelta("Hips")), Format(BoneRotationDelta("Spine")), Format(BoneRotationDelta("Head")),
                    Format(BoneRotationDelta("LeftUpLeg")), Format(BoneRotationDelta("LeftLeg")), Format(BoneRotationDelta("LeftFoot")),
                    Format(BoneRotationDelta("RightUpLeg")), Format(BoneRotationDelta("RightLeg")), Format(BoneRotationDelta("RightFoot")),
                    Format(Hips.x), Format(Hips.y), Format(Hips.z), Format(Head.x), Format(Head.y), Format(Head.z),
                    Format(LeftFoot.x), Format(LeftFoot.y), Format(LeftFoot.z), Format(RightFoot.x), Format(RightFoot.y), Format(RightFoot.z),
                    Format(LeftArmPoseDifference), Format(RightArmPoseDifference), Format(TorsoPoseDifference),
                    Format(LeftLegPoseDifference), Format(RightLegPoseDifference),
                    Format(ArmRotationDelta), Format(TorsoRotationDelta), Format(LegRotationDelta), Format(MotionScore),
                });
            }
        }

        private readonly struct DetailedSegment
        {
            public DetailedSegment(string name, int start, int end)
            {
                Name = name;
                Start = start;
                End = end;
            }

            public string Name { get; }
            public int Start { get; }
            public int End { get; }
        }

        private readonly struct FrameRange
        {
            public FrameRange(int start, int end)
            {
                Start = start;
                End = end;
            }

            public int Start { get; }
            public int End { get; }
        }
    }
}
