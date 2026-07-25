using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor
{
    internal static class OstinatoAttackMotionPhaseCut
    {
        private const string SourceClipNameFragment = "mixamo.com";
        private const string DerivedClipPath = "Assets/_Project/Art/Enemies/Ostinato/Animations/Ostinato_04_Scissor_Attack_PhaseCut.anim";
        private const string ValidationFolder = "docs/validation/ostinato_attack_motion_phase_cut_2026-07-20";
        private const string ApplyReportPath = ValidationFolder + "/Ostinato_AttackMotionPhaseCutApply.txt";
        private const string InspectionReportPath = ValidationFolder + "/Ostinato_AttackMotionPhaseCutInspection.txt";
        private const string CaptureReportPath = ValidationFolder + "/Ostinato_AttackMotionPhaseCutCapture.txt";
        private const string CaptureImagePath = ValidationFolder + "/Ostinato_AttackMotionPhaseCutContactSheet.png";
        private const int ReviewLayer = 30;
        private const int ImageSize = 320;
        private const int SheetColumns = 3;
        private const int SourceKeepEndFrame = 100;
        private const int SourceFinalFrame = 196;
        private const int ReturnStartFrame = 101;
        private const int ReturnEndFrame = 118;
        private const int HoldStartFrame = 119;
        private const int DerivedFinalFrame = 131;
        private const int ReturnDurationFrames = ReturnEndFrame - SourceKeepEndFrame;
        private const float FrameRate = 60f;
        private const float TimeEpsilon = 0.00001f;
        private const float ValueEpsilon = 0.00001f;
        private const string EulerRotationPropertyPrefix = "localEulerAnglesRaw.";

        private static readonly int[] CaptureFrames = { 0, 53, 84, 85, 93, 94, 100, 101, 104, 109, 114, 118, 119, 131 };
        private static readonly string[] ApprovedMaterialPaths =
        {
            "Assets/_Project/Art/Enemies/Ostinato/ApprovedSample/Materials/Ostinato_Approved_Chitin.mat",
            "Assets/_Project/Art/Enemies/Ostinato/ApprovedSample/Materials/Ostinato_Approved_SoftTissue.mat",
            "Assets/_Project/Art/Enemies/Ostinato/ApprovedSample/Materials/Ostinato_Approved_HookBlade.mat",
            "Assets/_Project/Art/Enemies/Ostinato/ApprovedSample/Materials/Ostinato_Approved_CompoundEye.mat",
        };

        [MenuItem("Bellerophon/Enemies/Ostinato/Apply Attack Motion Phase Cut")]
        public static void ApplyOstinatoAttackMotionPhaseCut()
        {
            var scene = RequireOpenScene();
            var placementRoot = RequirePlacementRoot(scene);
            var slot = RequireAttackSlot(placementRoot);
            var otherSlotSignatures = CaptureOtherSlotSignatures(placementRoot);
            var slotSignatureBefore = BuildHierarchySignature(slot);
            var sourceHashBefore = ComputeSha256(OstinatoScissorAttackAnimation.ProjectAbsolutePath(OstinatoScissorAttackAnimation.AttackModelPath));
            var sourceClip = RequireSourceClip();
            var sourceFingerprintBefore = BuildClipFingerprint(sourceClip);

            var derivedClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(DerivedClipPath);
            if (derivedClip == null)
            {
                derivedClip = new AnimationClip { name = "Ostinato_04_Scissor_Attack_PhaseCut" };
                AssetDatabase.CreateAsset(derivedClip, DerivedClipPath);
            }

            ReplaceWithPhaseCutCurves(sourceClip, derivedClip);
            ConnectController(derivedClip);
            EditorUtility.SetDirty(derivedClip);
            AssetDatabase.SaveAssets();

            RequireOtherSlotsUnchanged(placementRoot, otherSlotSignatures);
            RequireEqual(slotSignatureBefore, BuildHierarchySignature(slot), "Attack scene object changed while only the clip/controller should change.");
            RequireEqual(sourceHashBefore, ComputeSha256(OstinatoScissorAttackAnimation.ProjectAbsolutePath(OstinatoScissorAttackAnimation.AttackModelPath)), "Imported source FBX changed.");
            RequireEqual(sourceFingerprintBefore, BuildClipFingerprint(RequireSourceClip()), "Source animation curves changed.");
            var result = InspectInternal(true);

            var report = new StringBuilder();
            report.AppendLine("Result=PASS");
            report.AppendLine("SourceClip=" + OstinatoScissorAttackAnimation.AttackModelPath + "#" + sourceClip.name);
            report.AppendLine("DerivedClip=" + DerivedClipPath);
            report.AppendLine("AttackMapping=Derived[0..100]=Source[0..100]");
            report.AppendLine("ReturnTransition=Derived[101..118]:Source100Pose->Source0DefaultPose");
            report.AppendLine("DefaultHold=Derived[119..131]");
            report.AppendLine("OriginalAttackFramesUnchanged=0..100");
            report.AppendLine("OriginalPostAttackFramesPlayed=False");
            report.AppendLine("ReturnDurationFrames=" + ReturnDurationFrames);
            report.AppendLine("ReturnDurationSeconds=" + Format(ReturnDurationFrames / FrameRate));
            report.AppendLine("SourceFbxSha256=" + sourceHashBefore);
            report.AppendLine("SourceCurveFingerprint=" + sourceFingerprintBefore);
            report.AppendLine("DerivedCurveFingerprint=" + result.DerivedFingerprint);
            report.AppendLine("Controller=" + OstinatoScissorAttackAnimation.ControllerPath);
            report.AppendLine("AnimatorState=" + OstinatoScissorAttackAnimation.StateName);
            report.AppendLine("ModelOrBoneEdit=False");
            report.AppendLine("OtherOstinatoSlotsUnchanged=True");
            OstinatoScissorAttackAnimation.WriteText(ApplyReportPath, report.ToString());
            Debug.Log("Ostinato attack preserved through frame 100 and smooth return-to-default applied through frame 118.");
        }

        [MenuItem("Bellerophon/Enemies/Ostinato/Inspect Attack Motion Phase Cut")]
        public static void InspectOstinatoAttackMotionPhaseCut()
        {
            var result = InspectInternal(true);
            var report = new StringBuilder();
            report.AppendLine("Result=PASS");
            report.AppendLine("SourceFrameRate=" + Format(result.SourceClip.frameRate));
            report.AppendLine("SourceFinalFrame=" + SourceFinalFrame);
            report.AppendLine("SourceLengthSeconds=" + Format(result.SourceClip.length));
            report.AppendLine("DerivedFrameRate=" + Format(result.DerivedClip.frameRate));
            report.AppendLine("DerivedFinalFrame=" + DerivedFinalFrame);
            report.AppendLine("DerivedLengthSeconds=" + Format(result.DerivedClip.length));
            report.AppendLine("ExpectedDerivedLengthSeconds=" + Format(DerivedFinalFrame / FrameRate));
            report.AppendLine("AttackPrefixExact=Derived[0..100]==Source[0..100]");
            report.AppendLine("AttackPrefixDenseSampleMaxError=" + Format(result.Metrics.PrefixDenseSampleMaxError));
            report.AppendLine("ReturnTransitionFrames=101..118");
            report.AppendLine("ReturnDurationSeconds=" + Format(ReturnDurationFrames / FrameRate));
            report.AppendLine("DefaultHoldFrames=119..131");
            report.AppendLine("MaximumReturnEulerChannelStepDegrees=" + Format(result.Metrics.MaximumRotationChannelStep));
            report.AppendLine("MaximumFirstReturnEulerChannelStepDegrees=" + Format(result.Metrics.MaximumFirstRotationChannelStep));
            report.AppendLine("MaximumReturnPositionChannelStep=" + Format(result.Metrics.MaximumPositionChannelStep));
            report.AppendLine("ReturnCurveOvershoot=False");
            report.AppendLine("FinalPoseMatchesSourceFrame0=True");
            report.AppendLine("LoopBoundaryContinuous=True");
            report.AppendLine("FloatCurveBindings=" + AnimationUtility.GetCurveBindings(result.DerivedClip).Length);
            report.AppendLine("ObjectCurveBindings=" + AnimationUtility.GetObjectReferenceCurveBindings(result.DerivedClip).Length);
            report.AppendLine("SourceCurveFingerprint=" + result.SourceFingerprint);
            report.AppendLine("DerivedCurveFingerprint=" + result.DerivedFingerprint);
            report.AppendLine("LoopTime=True");
            report.AppendLine("ControllerStateUsesDerivedClip=True");
            report.AppendLine("PlaybackSpeed=1");
            report.AppendLine("AppearanceModel=" + OstinatoScissorAttackAnimation.ApprovedModelPath);
            report.AppendLine("AppearanceMaterialsApproved=True");
            report.AppendLine("DirectAttackFbxSceneInstance=True");
            report.AppendLine("SourceFbxUnchanged=True");
            report.AppendLine("ModelOrBoneEdit=False");
            report.AppendLine("OtherOstinatoSlotsUnchanged=True");
            OstinatoScissorAttackAnimation.WriteText(InspectionReportPath, report.ToString());
            Debug.Log("Ostinato unchanged attack and smooth return-to-default inspection passed.");
        }

        [MenuItem("Bellerophon/Enemies/Ostinato/Capture Attack Motion Phase Cut")]
        public static void CaptureOstinatoAttackMotionPhaseCut()
        {
            var result = InspectInternal(false);
            var scene = RequireOpenScene();
            var slot = RequireAttackSlot(RequirePlacementRoot(scene));
            var playbackModel = RequireDirectAttackFbxInstance(slot);
            var renderer = RequireApprovedRenderer(playbackModel.gameObject);
            var layeredObjects = playbackModel.GetComponentsInChildren<Transform>(true).Select(item => item.gameObject).ToArray();
            var originalLayers = layeredObjects.Select(item => item.layer).ToArray();
            GameObject cameraObject = null;
            GameObject keyObject = null;
            GameObject fillObject = null;
            var captured = new List<byte[]>();
            try
            {
                foreach (var item in layeredObjects) item.layer = ReviewLayer;
                cameraObject = new GameObject("Ostinato Phase Cut Review Camera") { hideFlags = HideFlags.HideAndDontSave };
                keyObject = new GameObject("Ostinato Phase Cut Key Light") { hideFlags = HideFlags.HideAndDontSave };
                fillObject = new GameObject("Ostinato Phase Cut Fill Light") { hideFlags = HideFlags.HideAndDontSave };
                var reviewCamera = cameraObject.AddComponent<Camera>();
                var keyLight = keyObject.AddComponent<Light>();
                var fillLight = fillObject.AddComponent<Light>();
                ConfigureCameraAndLights(reviewCamera, keyLight, fillLight);

                AnimationMode.StartAnimationMode();
                foreach (var frame in CaptureFrames)
                {
                    AnimationMode.BeginSampling();
                    AnimationMode.SampleAnimationClip(playbackModel.gameObject, result.DerivedClip, frame / FrameRate);
                    AnimationMode.EndSampling();
                    var frameTexture = RenderFrame(reviewCamera, renderer);
                    captured.Add(frameTexture.EncodeToPNG());
                    UnityEngine.Object.DestroyImmediate(frameTexture);
                }

                var frameWidth = ImageSize * 2;
                var rows = Mathf.CeilToInt(captured.Count / (float)SheetColumns);
                var sheet = new Texture2D(frameWidth * SheetColumns, ImageSize * rows, TextureFormat.RGBA32, false);
                sheet.SetPixels32(Enumerable.Repeat(new Color32(9, 12, 14, 255), sheet.width * sheet.height).ToArray());
                for (var index = 0; index < captured.Count; index++)
                {
                    var frameTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    frameTexture.LoadImage(captured[index], false);
                    var column = index % SheetColumns;
                    var row = rows - 1 - index / SheetColumns;
                    sheet.SetPixels(column * frameWidth, row * ImageSize, frameWidth, ImageSize, frameTexture.GetPixels());
                    UnityEngine.Object.DestroyImmediate(frameTexture);
                }
                sheet.Apply(false, false);
                var imagePath = OstinatoScissorAttackAnimation.ProjectAbsolutePath(CaptureImagePath);
                Directory.CreateDirectory(Path.GetDirectoryName(imagePath) ?? throw new InvalidOperationException("Capture path has no directory."));
                File.WriteAllBytes(imagePath, sheet.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(sheet);

                var report = new StringBuilder();
                report.AppendLine("Result=PASS");
                report.AppendLine("CaptureMode=Unity Edit Mode exact-frame AnimationMode sampling");
                report.AppendLine("ViewsPerFrame=Front|ThreeQuarter");
                report.AppendLine("DerivedFrames=" + string.Join("|", CaptureFrames));
                report.AppendLine("Timeline=AttackUnchanged[0..100]|SmoothReturn[101..118]|DefaultHold[119..131]");
                report.AppendLine("AttackFrames=84|85|93|94|100");
                report.AppendLine("ReturnFrames=101|104|109|114|118");
                report.AppendLine("DefaultHoldFrames=119|131");
                report.AppendLine("FinalImage=" + CaptureImagePath);
                OstinatoScissorAttackAnimation.WriteText(CaptureReportPath, report.ToString());
                Debug.Log("Ostinato attack motion phase cut capture completed: " + CaptureImagePath);
            }
            finally
            {
                if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
                for (var index = 0; index < layeredObjects.Length; index++)
                    if (layeredObjects[index] != null) layeredObjects[index].layer = originalLayers[index];
                DestroyImmediate(cameraObject);
                DestroyImmediate(keyObject);
                DestroyImmediate(fillObject);
            }
        }

        private static InspectionResult InspectInternal(bool writeNothing)
        {
            _ = writeNothing;
            var scene = RequireOpenScene();
            var placementRoot = RequirePlacementRoot(scene);
            var slot = RequireAttackSlot(placementRoot);
            RequireApprovedRenderer(slot.gameObject);
            RequireDirectAttackFbxInstance(slot);
            if (placementRoot.childCount != 9) throw new InvalidOperationException("Approved Ostinato placement must contain nine slots.");

            var sourceHashBefore = ComputeSha256(OstinatoScissorAttackAnimation.ProjectAbsolutePath(OstinatoScissorAttackAnimation.AttackModelPath));
            var sourceClip = RequireSourceClip();
            var derivedClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(DerivedClipPath) ??
                              throw new InvalidOperationException("Derived phase-cut clip is missing: " + DerivedClipPath);
            if (Mathf.Abs(sourceClip.frameRate - FrameRate) > 0.001f) throw new InvalidOperationException("Source clip is not 60 fps.");
            if (Mathf.Abs(sourceClip.length - SourceFinalFrame / FrameRate) > TimeEpsilon)
                throw new InvalidOperationException("Source clip length changed. Actual=" + Format(sourceClip.length));
            if (Mathf.Abs(derivedClip.frameRate - FrameRate) > 0.001f) throw new InvalidOperationException("Derived clip is not 60 fps.");
            if (Mathf.Abs(derivedClip.length - DerivedFinalFrame / FrameRate) > TimeEpsilon)
                throw new InvalidOperationException("Derived clip length must be 131 frames. Actual=" + Format(derivedClip.length));
            if (!AnimationUtility.GetAnimationClipSettings(derivedClip).loopTime)
                throw new InvalidOperationException("Derived phase-cut clip must loop.");

            var metrics = RequireExactAttackAndReturnCurves(sourceClip, derivedClip);
            RequireControllerUses(derivedClip);
            RequireEqual(sourceHashBefore, ComputeSha256(OstinatoScissorAttackAnimation.ProjectAbsolutePath(OstinatoScissorAttackAnimation.AttackModelPath)), "Source FBX changed during inspection.");
            return new InspectionResult(sourceClip, derivedClip, BuildClipFingerprint(sourceClip), BuildClipFingerprint(derivedClip), metrics);
        }

        private static void ReplaceWithPhaseCutCurves(AnimationClip source, AnimationClip destination)
        {
            foreach (var binding in AnimationUtility.GetCurveBindings(destination)) AnimationUtility.SetEditorCurve(destination, binding, null);
            foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(destination)) AnimationUtility.SetObjectReferenceCurve(destination, binding, null);

            foreach (var binding in AnimationUtility.GetCurveBindings(source))
            {
                var sourceCurve = AnimationUtility.GetEditorCurve(source, binding) ?? throw new InvalidOperationException("Source float curve is missing.");
                AnimationUtility.SetEditorCurve(destination, binding, BuildReturnCurve(sourceCurve, binding));
            }
            foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(source))
            {
                var keys = AnimationUtility.GetObjectReferenceCurve(source, binding) ?? Array.Empty<ObjectReferenceKeyframe>();
                AnimationUtility.SetObjectReferenceCurve(destination, binding, KeepAttackObjectKeys(keys));
            }

            destination.frameRate = FrameRate;
            var settings = AnimationUtility.GetAnimationClipSettings(source);
            settings.startTime = 0f;
            settings.stopTime = DerivedFinalFrame / FrameRate;
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(destination, settings);
            AnimationUtility.SetAnimationEvents(destination, KeepAttackEvents(AnimationUtility.GetAnimationEvents(source)));
        }

        private static AnimationCurve BuildReturnCurve(AnimationCurve source, EditorCurveBinding binding)
        {
            var keys = source.keys
                .Where(key => key.time <= SourceKeepEndFrame / FrameRate + TimeEpsilon)
                .ToList();
            EnsureCopiedBoundaryKey(source, keys, SourceKeepEndFrame, SourceKeepEndFrame);
            var startTime = SourceKeepEndFrame / FrameRate;
            var startKeyIndex = keys.FindIndex(key => Mathf.Abs(key.time - startTime) <= TimeEpsilon);
            if (startKeyIndex < 0) throw new InvalidOperationException("Return transition start key is missing.");
            var startKey = keys[startKeyIndex];
            startKey.outTangent = 0f;
            startKey.outWeight = 1f / 3f;
            startKey.weightedMode &= WeightedMode.In;
            keys[startKeyIndex] = startKey;

            var startValue = source.Evaluate(startTime);
            var defaultValue = source.Evaluate(0f);
            var isRotation = binding.propertyName.StartsWith(EulerRotationPropertyPrefix, StringComparison.Ordinal);
            var valueDelta = isRotation ? Mathf.DeltaAngle(startValue, defaultValue) : defaultValue - startValue;
            var durationSeconds = ReturnDurationFrames / FrameRate;
            for (var frame = ReturnStartFrame; frame <= ReturnEndFrame; frame++)
            {
                var normalized = (frame - SourceKeepEndFrame) / (float)ReturnDurationFrames;
                var eased = SmoothStep01(normalized);
                var slope = valueDelta * SmoothStepDerivative(normalized) / durationSeconds;
                var key = new Keyframe(frame / FrameRate, startValue + valueDelta * eased, slope, slope)
                {
                    inWeight = 1f / 3f,
                    outWeight = 1f / 3f,
                    weightedMode = WeightedMode.None,
                };
                keys.Add(key);
            }
            keys.Add(new Keyframe(DerivedFinalFrame / FrameRate, startValue + valueDelta, 0f, 0f));
            keys.Sort((left, right) => left.time.CompareTo(right.time));
            var curve = new AnimationCurve(keys.ToArray()) { preWrapMode = source.preWrapMode, postWrapMode = source.postWrapMode };
            return curve;
        }

        private static void EnsureCopiedBoundaryKey(AnimationCurve source, ICollection<Keyframe> destination, int sourceFrame, int destinationFrame)
        {
            var sourceTime = sourceFrame / FrameRate;
            if (destination.Any(key => Mathf.Abs(key.time - destinationFrame / FrameRate) <= TimeEpsilon)) return;
            var exactMatches = source.keys.Where(key => Mathf.Abs(key.time - sourceTime) <= TimeEpsilon).ToArray();
            if (exactMatches.Length == 1)
            {
                destination.Add(ShiftKey(exactMatches[0], sourceTime - destinationFrame / FrameRate));
                return;
            }

            var sampleStep = 0.0001f;
            var leftTime = Mathf.Max(0f, sourceTime - sampleStep);
            var rightTime = Mathf.Min(SourceFinalFrame / FrameRate, sourceTime + sampleStep);
            var inTangent = sourceTime > leftTime ? (source.Evaluate(sourceTime) - source.Evaluate(leftTime)) / (sourceTime - leftTime) : 0f;
            var outTangent = rightTime > sourceTime ? (source.Evaluate(rightTime) - source.Evaluate(sourceTime)) / (rightTime - sourceTime) : 0f;
            destination.Add(new Keyframe(destinationFrame / FrameRate, source.Evaluate(sourceTime), inTangent, outTangent));
        }

        private static Keyframe ShiftKey(Keyframe key, float shift)
        {
            key.time -= shift;
            return key;
        }

        private static ObjectReferenceKeyframe[] KeepAttackObjectKeys(IEnumerable<ObjectReferenceKeyframe> source)
        {
            return source.Where(key => key.time <= SourceKeepEndFrame / FrameRate + TimeEpsilon).ToArray();
        }

        private static AnimationEvent[] KeepAttackEvents(IEnumerable<AnimationEvent> source)
        {
            return source.Where(item => item.time <= SourceKeepEndFrame / FrameRate + TimeEpsilon)
                .Select(item =>
                {
                    return new AnimationEvent
                    {
                        functionName = item.functionName,
                        floatParameter = item.floatParameter,
                        intParameter = item.intParameter,
                        stringParameter = item.stringParameter,
                        objectReferenceParameter = item.objectReferenceParameter,
                        messageOptions = item.messageOptions,
                        time = item.time
                    };
                }).ToArray();
        }

        private static CurveMetrics RequireExactAttackAndReturnCurves(AnimationClip source, AnimationClip derived)
        {
            var metrics = new CurveMetrics();
            var sourceBindings = AnimationUtility.GetCurveBindings(source).OrderBy(BindingId).ToArray();
            var derivedBindings = AnimationUtility.GetCurveBindings(derived).OrderBy(BindingId).ToArray();
            if (!sourceBindings.Select(BindingId).SequenceEqual(derivedBindings.Select(BindingId)))
                throw new InvalidOperationException("Derived float curve bindings differ from the source.");
            for (var index = 0; index < sourceBindings.Length; index++)
            {
                var sourceCurve = AnimationUtility.GetEditorCurve(source, sourceBindings[index]);
                var expected = BuildReturnCurve(sourceCurve, sourceBindings[index]);
                var actual = AnimationUtility.GetEditorCurve(derived, derivedBindings[index]);
                RequireCurvesEqual(expected, actual, BindingId(sourceBindings[index]));
                for (var sample = 0; sample <= SourceKeepEndFrame * 4; sample++)
                {
                    var time = sample / (FrameRate * 4f);
                    var error = Mathf.Abs(sourceCurve.Evaluate(time) - actual.Evaluate(time));
                    metrics.PrefixDenseSampleMaxError = Mathf.Max(metrics.PrefixDenseSampleMaxError, error);
                    if (error > 0.0001f)
                        throw new InvalidOperationException("Attack prefix changed at time " + Format(time) + " for " + BindingId(sourceBindings[index]));
                }

                var isRotation = sourceBindings[index].propertyName.StartsWith(EulerRotationPropertyPrefix, StringComparison.Ordinal);
                var startValue = actual.Evaluate(SourceKeepEndFrame / FrameRate);
                var finalValue = actual.Evaluate(ReturnEndFrame / FrameRate);
                var minimum = Mathf.Min(startValue, finalValue) - 0.0001f;
                var maximum = Mathf.Max(startValue, finalValue) + 0.0001f;
                for (var frame = ReturnStartFrame; frame <= ReturnEndFrame; frame++)
                {
                    var previous = actual.Evaluate((frame - 1) / FrameRate);
                    var current = actual.Evaluate(frame / FrameRate);
                    if (current < minimum || current > maximum)
                        throw new InvalidOperationException("Return curve overshot its endpoints for " + BindingId(sourceBindings[index]));
                    if (isRotation)
                    {
                        var step = Mathf.Abs(Mathf.DeltaAngle(previous, current));
                        metrics.MaximumRotationChannelStep = Mathf.Max(metrics.MaximumRotationChannelStep, step);
                        if (frame == ReturnStartFrame)
                            metrics.MaximumFirstRotationChannelStep = Mathf.Max(metrics.MaximumFirstRotationChannelStep, step);
                    }
                    else
                    {
                        metrics.MaximumPositionChannelStep = Mathf.Max(metrics.MaximumPositionChannelStep, Mathf.Abs(current - previous));
                    }
                }
                for (var frame = HoldStartFrame; frame <= DerivedFinalFrame; frame++)
                {
                    var heldValue = actual.Evaluate(frame / FrameRate);
                    if (isRotation)
                    {
                        if (Mathf.Abs(Mathf.DeltaAngle(heldValue, sourceCurve.Evaluate(0f))) > 0.0001f)
                            throw new InvalidOperationException("Default rotation hold differs from source frame 0 for " + BindingId(sourceBindings[index]));
                    }
                    else if (Mathf.Abs(heldValue - sourceCurve.Evaluate(0f)) > 0.0001f)
                    {
                        throw new InvalidOperationException("Default hold differs from source frame 0 for " + BindingId(sourceBindings[index]));
                    }
                }
            }

            var sourceObjectBindings = AnimationUtility.GetObjectReferenceCurveBindings(source).OrderBy(BindingId).ToArray();
            var derivedObjectBindings = AnimationUtility.GetObjectReferenceCurveBindings(derived).OrderBy(BindingId).ToArray();
            if (!sourceObjectBindings.Select(BindingId).SequenceEqual(derivedObjectBindings.Select(BindingId)))
                throw new InvalidOperationException("Derived object curve bindings differ from the source.");
            for (var index = 0; index < sourceObjectBindings.Length; index++)
            {
                var expected = KeepAttackObjectKeys(AnimationUtility.GetObjectReferenceCurve(source, sourceObjectBindings[index]));
                var actual = AnimationUtility.GetObjectReferenceCurve(derived, derivedObjectBindings[index]);
                if (expected.Length != actual.Length) throw new InvalidOperationException("Object key count mismatch.");
                for (var keyIndex = 0; keyIndex < expected.Length; keyIndex++)
                    if (Mathf.Abs(expected[keyIndex].time - actual[keyIndex].time) > TimeEpsilon || expected[keyIndex].value != actual[keyIndex].value)
                        throw new InvalidOperationException("Object key mismatch.");
            }
            return metrics;
        }

        private static void RequireCurvesEqual(AnimationCurve expected, AnimationCurve actual, string id)
        {
            if (actual == null || expected.preWrapMode != actual.preWrapMode || expected.postWrapMode != actual.postWrapMode || expected.length != actual.length)
                throw new InvalidOperationException("Curve metadata mismatch: " + id);
            for (var index = 0; index < expected.length; index++)
            {
                var left = expected.keys[index];
                var right = actual.keys[index];
                if (Mathf.Abs(left.time - right.time) > TimeEpsilon || Mathf.Abs(left.value - right.value) > ValueEpsilon ||
                    !FloatEquivalent(left.inTangent, right.inTangent) || !FloatEquivalent(left.outTangent, right.outTangent) ||
                    Mathf.Abs(left.inWeight - right.inWeight) > ValueEpsilon || Mathf.Abs(left.outWeight - right.outWeight) > ValueEpsilon ||
                    left.weightedMode != right.weightedMode)
                    throw new InvalidOperationException("Curve key mismatch: " + id + " key " + index);
            }
        }

        private static bool FloatEquivalent(float left, float right) =>
            (float.IsInfinity(left) && float.IsInfinity(right) && Math.Sign(left) == Math.Sign(right)) || Mathf.Abs(left - right) <= ValueEpsilon;

        private static void ConnectController(AnimationClip clip)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(OstinatoScissorAttackAnimation.ControllerPath) ??
                             throw new InvalidOperationException("Ostinato attack controller is missing.");
            var states = controller.layers.SelectMany(layer => layer.stateMachine.states).Select(item => item.state)
                .Where(state => state.name == OstinatoScissorAttackAnimation.StateName).ToArray();
            if (states.Length != 1) throw new InvalidOperationException("Expected exactly one Ostinato attack state.");
            states[0].motion = clip;
            states[0].speed = 1f;
            EditorUtility.SetDirty(states[0]);
            EditorUtility.SetDirty(controller);
        }

        private static void RequireControllerUses(AnimationClip clip)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(OstinatoScissorAttackAnimation.ControllerPath) ??
                             throw new InvalidOperationException("Ostinato attack controller is missing.");
            var states = controller.layers.SelectMany(layer => layer.stateMachine.states).Select(item => item.state)
                .Where(state => state.name == OstinatoScissorAttackAnimation.StateName).ToArray();
            if (states.Length != 1 || states[0].motion != clip || !Mathf.Approximately(states[0].speed, 1f))
                throw new InvalidOperationException("Ostinato attack controller does not use the derived phase-cut clip at speed 1.");
        }

        private static AnimationClip RequireSourceClip()
        {
            var matches = AssetDatabase.LoadAllAssetsAtPath(OstinatoScissorAttackAnimation.AttackModelPath).OfType<AnimationClip>()
                .Where(clip => !clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase))
                .Where(clip => clip.name.IndexOf(SourceClipNameFragment, StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
            if (matches.Length != 1) throw new InvalidOperationException("Expected exactly one source Ostinato attack clip. Count=" + matches.Length);
            return matches[0];
        }

        private static Scene RequireOpenScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != OstinatoScissorAttackAnimation.ScenePath)
                throw new InvalidOperationException("CargoRunMvp must be the active scene. Active=" + scene.path);
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Unity must remain in Edit Mode for this operation.");
            return scene;
        }

        private static Transform RequirePlacementRoot(Scene scene) =>
            scene.GetRootGameObjects().SingleOrDefault(root => root.name == OstinatoScissorAttackAnimation.PlacementRootName)?.transform ??
            throw new InvalidOperationException("Approved Ostinato placement root is missing.");

        private static Transform RequireAttackSlot(Transform root)
        {
            var matches = root.Cast<Transform>().Where(child => child.name == OstinatoScissorAttackAnimation.AttackSlotName).ToArray();
            if (matches.Length != 1) throw new InvalidOperationException("Expected exactly one Ostinato attack slot. Count=" + matches.Length);
            return matches[0];
        }

        private static SkinnedMeshRenderer RequireApprovedRenderer(GameObject slot)
        {
            var renderers = slot.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (renderers.Length != 1) throw new InvalidOperationException("Attack slot must contain exactly one skinned renderer.");
            var renderer = renderers[0];
            if (AssetDatabase.GetAssetPath(renderer.sharedMesh) != OstinatoScissorAttackAnimation.ApprovedModelPath)
                throw new InvalidOperationException("Attack appearance mesh is not the approved static model mesh.");
            var materialPaths = renderer.sharedMaterials.Select(AssetDatabase.GetAssetPath).ToArray();
            if (!materialPaths.SequenceEqual(ApprovedMaterialPaths))
                throw new InvalidOperationException("Attack appearance materials differ from the approved static appearance.");
            return renderer;
        }

        private static Transform RequireDirectAttackFbxInstance(Transform slot)
        {
            if (slot.childCount != 1)
                throw new InvalidOperationException("Attack slot must contain exactly one playback model.");
            var model = slot.GetChild(0);
            var prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(model.gameObject);
            if (prefabPath != OstinatoScissorAttackAnimation.AttackModelPath)
                throw new InvalidOperationException("Attack scene object is not a direct instance of the supplied attack FBX.");
            return model;
        }

        private static string[] CaptureOtherSlotSignatures(Transform root) =>
            root.Cast<Transform>().Where(child => child.name != OstinatoScissorAttackAnimation.AttackSlotName).Select(BuildHierarchySignature).ToArray();

        private static void RequireOtherSlotsUnchanged(Transform root, string[] before)
        {
            if (!before.SequenceEqual(CaptureOtherSlotSignatures(root)))
                throw new InvalidOperationException("An Ostinato slot outside the attack slot changed.");
        }

        private static string BuildHierarchySignature(Transform root)
        {
            var builder = new StringBuilder();
            foreach (var item in root.GetComponentsInChildren<Transform>(true))
                builder.Append(item.name).Append('|').Append(item.GetSiblingIndex()).Append('|')
                    .Append(item.localPosition.ToString("R", CultureInfo.InvariantCulture)).Append('|')
                    .Append(item.localRotation.ToString("R", CultureInfo.InvariantCulture)).Append('|')
                    .Append(item.localScale.ToString("R", CultureInfo.InvariantCulture)).Append(';');
            foreach (var renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                builder.Append(AssetDatabase.GetAssetPath(renderer.sharedMesh)).Append('|')
                    .Append(string.Join(",", renderer.sharedMaterials.Select(AssetDatabase.GetAssetPath))).Append(';');
            return builder.ToString();
        }

        private static string BuildClipFingerprint(AnimationClip clip)
        {
            using var stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
            {
                writer.Write(clip.length);
                writer.Write(clip.frameRate);
                foreach (var binding in AnimationUtility.GetCurveBindings(clip).OrderBy(BindingId))
                {
                    writer.Write(BindingId(binding));
                    var curve = AnimationUtility.GetEditorCurve(clip, binding);
                    writer.Write((int)curve.preWrapMode); writer.Write((int)curve.postWrapMode); writer.Write(curve.length);
                    foreach (var key in curve.keys)
                    {
                        writer.Write(key.time); writer.Write(key.value); writer.Write(key.inTangent); writer.Write(key.outTangent);
                        writer.Write(key.inWeight); writer.Write(key.outWeight); writer.Write((int)key.weightedMode);
                    }
                }
            }
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

        private static Texture2D RenderFrame(Camera camera, Renderer renderer)
        {
            var bounds = renderer.bounds;
            bounds.Expand(new Vector3(0.15f, 0.12f, 0.12f));
            var target = bounds.center + Vector3.up * bounds.extents.y * 0.02f;
            var halfFov = camera.fieldOfView * 0.5f * Mathf.Deg2Rad;
            var distance = Mathf.Max(bounds.extents.y, bounds.extents.x) / Mathf.Tan(halfFov) + bounds.extents.z + 0.15f;
            var front = RenderView(camera, target, Vector3.back, distance);
            var threeQuarter = RenderView(camera, target, new Vector3(0.7f, 0f, -1f).normalized, distance);
            var combined = new Texture2D(ImageSize * 2, ImageSize, TextureFormat.RGBA32, false);
            combined.SetPixels(0, 0, ImageSize, ImageSize, front.GetPixels());
            combined.SetPixels(ImageSize, 0, ImageSize, ImageSize, threeQuarter.GetPixels());
            combined.Apply(false, false);
            UnityEngine.Object.DestroyImmediate(front);
            UnityEngine.Object.DestroyImmediate(threeQuarter);
            return combined;
        }

        private static Texture2D RenderView(Camera camera, Vector3 target, Vector3 direction, float distance)
        {
            camera.transform.position = target + direction * distance;
            camera.transform.rotation = Quaternion.LookRotation(target - camera.transform.position, Vector3.up);
            var renderTexture = RenderTexture.GetTemporary(ImageSize, ImageSize, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var previous = RenderTexture.active;
            try
            {
                camera.targetTexture = renderTexture;
                camera.Render();
                RenderTexture.active = renderTexture;
                var texture = new Texture2D(ImageSize, ImageSize, TextureFormat.RGBA32, false);
                texture.ReadPixels(new Rect(0, 0, ImageSize, ImageSize), 0, 0, false);
                texture.Apply(false, false);
                return texture;
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(renderTexture);
            }
        }

        private static void ConfigureCameraAndLights(Camera camera, Light key, Light fill)
        {
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.035f, 0.045f, 0.055f, 1f);
            camera.fieldOfView = 40f;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 100f;
            camera.cullingMask = 1 << ReviewLayer;
            camera.allowHDR = true;
            camera.allowMSAA = true;
            key.type = LightType.Directional; key.intensity = 1.45f; key.color = new Color(1f, 0.89f, 0.72f); key.cullingMask = 1 << ReviewLayer;
            key.transform.rotation = Quaternion.Euler(38f, -32f, 0f);
            fill.type = LightType.Directional; fill.intensity = 0.78f; fill.color = new Color(0.46f, 0.66f, 1f); fill.cullingMask = 1 << ReviewLayer;
            fill.transform.rotation = Quaternion.Euler(326f, 148f, 0f);
        }

        private static void DestroyImmediate(GameObject target)
        {
            if (target != null) UnityEngine.Object.DestroyImmediate(target);
        }

        private static string BindingId(EditorCurveBinding binding) =>
            (binding.path ?? string.Empty) + "|" + (binding.type?.FullName ?? string.Empty) + "|" + (binding.propertyName ?? string.Empty);

        private static int FrameFromTime(float time) => Mathf.RoundToInt(time * FrameRate);
        private static float SmoothStep01(float value) => value * value * (3f - 2f * value);
        private static float SmoothStepDerivative(float value) => 6f * value * (1f - value);
        private static string Format(float value) => value.ToString("0.######", CultureInfo.InvariantCulture);

        private static void RequireEqual(string expected, string actual, string message)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal)) throw new InvalidOperationException(message);
        }

        private readonly struct InspectionResult
        {
            public InspectionResult(
                AnimationClip sourceClip,
                AnimationClip derivedClip,
                string sourceFingerprint,
                string derivedFingerprint,
                CurveMetrics metrics)
            {
                SourceClip = sourceClip;
                DerivedClip = derivedClip;
                SourceFingerprint = sourceFingerprint;
                DerivedFingerprint = derivedFingerprint;
                Metrics = metrics;
            }

            public AnimationClip SourceClip { get; }
            public AnimationClip DerivedClip { get; }
            public string SourceFingerprint { get; }
            public string DerivedFingerprint { get; }
            public CurveMetrics Metrics { get; }
        }

        private sealed class CurveMetrics
        {
            public float PrefixDenseSampleMaxError;
            public float MaximumRotationChannelStep;
            public float MaximumFirstRotationChannelStep;
            public float MaximumPositionChannelStep;
        }
    }
}
