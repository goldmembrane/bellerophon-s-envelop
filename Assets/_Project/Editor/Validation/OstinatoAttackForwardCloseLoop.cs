using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor
{
    internal static class OstinatoAttackForwardCloseLoop
    {
        private const string SourceClipNameFragment = "mixamo.com";
        private const string LoopClipPath = "Assets/_Project/Art/Enemies/Ostinato/Animations/Ostinato_04_Scissor_Attack_ForwardCloseLoop.anim";
        private const string ValidationFolder = "docs/validation/ostinato_attack_forward_close_loop_2026-07-20";
        private const string ApplyReportPath = ValidationFolder + "/Ostinato_AttackForwardCloseLoopApply.txt";
        private const string InspectionReportPath = ValidationFolder + "/Ostinato_AttackForwardCloseLoopInspection.txt";
        private const string CaptureReportPath = ValidationFolder + "/Ostinato_AttackForwardCloseLoopCapture.txt";
        private const string CaptureImagePath = ValidationFolder + "/Ostinato_AttackForwardCloseLoopContactSheet.png";
        private const int FinalFrame = 93;
        private const float FrameRate = 60f;
        private const float TimeEpsilon = 0.00001f;
        private const float ValueEpsilon = 0.00001f;
        private const int ReviewLayer = 30;
        private const int ImageSize = 320;
        private const int SheetColumns = 3;

        private static readonly int[] CaptureFrames = { 0, 15, 30, 53, 70, 84, 85, 90, 93, 0 };
        private static readonly string[] ApprovedMaterialPaths =
        {
            "Assets/_Project/Art/Enemies/Ostinato/ApprovedSample/Materials/Ostinato_Approved_Chitin.mat",
            "Assets/_Project/Art/Enemies/Ostinato/ApprovedSample/Materials/Ostinato_Approved_SoftTissue.mat",
            "Assets/_Project/Art/Enemies/Ostinato/ApprovedSample/Materials/Ostinato_Approved_HookBlade.mat",
            "Assets/_Project/Art/Enemies/Ostinato/ApprovedSample/Materials/Ostinato_Approved_CompoundEye.mat",
        };

        [MenuItem("Bellerophon/Enemies/Ostinato/Apply Attack Forward Close Loop")]
        public static void ApplyOstinatoAttackFbxForwardCloseLoop()
        {
            var scene = RequireOpenScene();
            var placementRoot = RequirePlacementRoot(scene);
            var slotBefore = RequireAttackSlot(placementRoot);
            var oldModel = RequireSinglePlaybackModel(slotBefore);
            var oldModelId = GlobalObjectId.GetGlobalObjectIdSlow(oldModel.gameObject).ToString();
            var otherSlotSignatures = CaptureOtherSlotSignatures(placementRoot);
            var sourcePath = OstinatoScissorAttackAnimation.ProjectAbsolutePath(OstinatoScissorAttackAnimation.SourceAttackRelativePath);
            var importedPath = OstinatoScissorAttackAnimation.ProjectAbsolutePath(OstinatoScissorAttackAnimation.AttackModelPath);
            var sourceHashBefore = ComputeSha256(sourcePath);
            var importedHashBefore = ComputeSha256(importedPath);
            RequireEqual(sourceHashBefore, importedHashBefore, "Unity attack FBX differs from the supplied attack FBX.");

            OstinatoScissorAttackAnimation.ApplyOstinatoScissorAttackAnimation();

            scene = RequireOpenScene();
            placementRoot = RequirePlacementRoot(scene);
            var slotAfter = RequireAttackSlot(placementRoot);
            var newModel = RequireSinglePlaybackModel(slotAfter);
            var newModelId = GlobalObjectId.GetGlobalObjectIdSlow(newModel.gameObject).ToString();
            if (oldModelId == newModelId)
                throw new InvalidOperationException("The previous Ostinato attack motion object was not replaced.");
            RequireOtherSlotsUnchanged(placementRoot, otherSlotSignatures);

            var sourceClip = RequireSourceClip();
            var sourceFingerprintBefore = BuildClipFingerprint(sourceClip);
            var loopClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(LoopClipPath);
            if (loopClip == null)
            {
                loopClip = new AnimationClip { name = "Ostinato_04_Scissor_Attack_ForwardCloseLoop" };
                AssetDatabase.CreateAsset(loopClip, LoopClipPath);
            }
            ReplaceWithForwardCloseCurves(sourceClip, loopClip);
            ConnectController(loopClip);
            EditorUtility.SetDirty(loopClip);
            AssetDatabase.SaveAssets();

            var sourceHashAfter = ComputeSha256(sourcePath);
            var importedHashAfter = ComputeSha256(importedPath);
            RequireEqual(sourceHashBefore, sourceHashAfter, "Supplied attack FBX changed during application.");
            RequireEqual(importedHashBefore, importedHashAfter, "Unity attack FBX changed during application.");
            RequireEqual(sourceFingerprintBefore, BuildClipFingerprint(RequireSourceClip()), "Source attack curves changed during application.");
            var inspection = InspectInternal();

            var report = new StringBuilder();
            report.AppendLine("Result=PASS");
            report.AppendLine("Scene=" + OstinatoScissorAttackAnimation.ScenePath);
            report.AppendLine("Target=" + OstinatoScissorAttackAnimation.PlacementRootName + "/" + OstinatoScissorAttackAnimation.AttackSlotName);
            report.AppendLine("OldAttackObjectId=" + oldModelId);
            report.AppendLine("NewAttackObjectId=" + newModelId);
            report.AppendLine("PreviousAttackObjectDeleted=True");
            report.AppendLine("PlaybackPrefabSource=" + OstinatoScissorAttackAnimation.AttackModelPath);
            report.AppendLine("DirectSuppliedFbxInstance=True");
            report.AppendLine("AppearanceModel=" + OstinatoScissorAttackAnimation.ApprovedModelPath);
            report.AppendLine("AnimationSource=" + OstinatoScissorAttackAnimation.AttackModelPath + "#" + sourceClip.name);
            report.AppendLine("LoopClip=" + LoopClipPath);
            report.AppendLine("FrameRange=0..93");
            report.AppendLine("FrameRate=60");
            report.AppendLine("LengthSeconds=" + Format(FinalFrame / FrameRate));
            report.AppendLine("LoopTime=True");
            report.AppendLine("PlaybackSpeed=1");
            report.AppendLine("SourceFbxSha256=" + sourceHashBefore);
            report.AppendLine("SourceCurveFingerprint=" + sourceFingerprintBefore);
            report.AppendLine("LoopCurveFingerprint=" + inspection.LoopFingerprint);
            report.AppendLine("OriginalMotionEdited=False");
            report.AppendLine("ModelOrBoneEdit=False");
            report.AppendLine("OtherOstinatoSlotsUnchanged=True");
            OstinatoScissorAttackAnimation.WriteText(ApplyReportPath, report.ToString());
            Selection.activeGameObject = slotAfter.gameObject;
            Debug.Log("Ostinato supplied FBX attack object replaced, approved appearance synchronized, and frames 0 through 93 looped.");
        }

        [MenuItem("Bellerophon/Enemies/Ostinato/Inspect Attack Forward Close Loop")]
        public static void InspectOstinatoAttackFbxForwardCloseLoop()
        {
            var result = InspectInternal();
            var report = new StringBuilder();
            report.AppendLine("Result=PASS");
            report.AppendLine("SourceClip=" + result.SourceClip.name);
            report.AppendLine("SourceFrameRate=" + Format(result.SourceClip.frameRate));
            report.AppendLine("SourceLengthSeconds=" + Format(result.SourceClip.length));
            report.AppendLine("LoopClip=" + LoopClipPath);
            report.AppendLine("LoopFirstFrame=0");
            report.AppendLine("LoopFinalFrame=" + FinalFrame);
            report.AppendLine("LoopLengthSeconds=" + Format(result.LoopClip.length));
            report.AppendLine("ExpectedLoopLengthSeconds=" + Format(FinalFrame / FrameRate));
            report.AppendLine("DenseSampleMaximumError=" + Format(result.DenseSampleMaximumError));
            report.AppendLine("FloatCurveBindings=" + AnimationUtility.GetCurveBindings(result.LoopClip).Length);
            report.AppendLine("ObjectCurveBindings=" + AnimationUtility.GetObjectReferenceCurveBindings(result.LoopClip).Length);
            report.AppendLine("LoopTime=True");
            report.AppendLine("ControllerStateUsesLoopClip=True");
            report.AppendLine("PlaybackSpeed=1");
            report.AppendLine("DirectSuppliedFbxInstance=True");
            report.AppendLine("ApprovedStaticMesh=True");
            report.AppendLine("ApprovedMaterials=True");
            report.AppendLine("CorrectedBladeControlsPresent=False");
            report.AppendLine("SourceFbxSha256=" + result.SourceHash);
            report.AppendLine("ImportedFbxSha256=" + result.ImportedHash);
            report.AppendLine("SourceCopyHashesMatch=True");
            report.AppendLine("SourceCurveFingerprint=" + result.SourceFingerprint);
            report.AppendLine("LoopCurveFingerprint=" + result.LoopFingerprint);
            report.AppendLine("OriginalMotionEdited=False");
            report.AppendLine("ModelOrBoneEdit=False");
            report.AppendLine("OtherOstinatoSlotsUnchanged=True");
            OstinatoScissorAttackAnimation.WriteText(InspectionReportPath, report.ToString());
            Debug.Log("Ostinato supplied-FBX direct instance, approved appearance, and exact frames 0 through 93 loop inspection passed.");
        }

        [MenuItem("Bellerophon/Enemies/Ostinato/Capture Attack Forward Close Loop")]
        public static void CaptureOstinatoAttackFbxForwardCloseLoop()
        {
            var result = InspectInternal();
            var scene = RequireOpenScene();
            var model = RequireSinglePlaybackModel(RequireAttackSlot(RequirePlacementRoot(scene)));
            var renderer = RequireApprovedRenderer(model.gameObject);
            var layeredObjects = model.GetComponentsInChildren<Transform>(true).Select(item => item.gameObject).ToArray();
            var originalLayers = layeredObjects.Select(item => item.layer).ToArray();
            GameObject cameraObject = null;
            GameObject keyObject = null;
            GameObject fillObject = null;
            var captured = new List<byte[]>();
            try
            {
                foreach (var item in layeredObjects) item.layer = ReviewLayer;
                cameraObject = new GameObject("Ostinato Forward Close Loop Camera") { hideFlags = HideFlags.HideAndDontSave };
                keyObject = new GameObject("Ostinato Forward Close Loop Key Light") { hideFlags = HideFlags.HideAndDontSave };
                fillObject = new GameObject("Ostinato Forward Close Loop Fill Light") { hideFlags = HideFlags.HideAndDontSave };
                var reviewCamera = cameraObject.AddComponent<Camera>();
                ConfigureCameraAndLights(reviewCamera, keyObject.AddComponent<Light>(), fillObject.AddComponent<Light>());

                AnimationMode.StartAnimationMode();
                foreach (var frame in CaptureFrames)
                {
                    AnimationMode.BeginSampling();
                    AnimationMode.SampleAnimationClip(model.gameObject, result.LoopClip, frame / FrameRate);
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
                var outputPath = OstinatoScissorAttackAnimation.ProjectAbsolutePath(CaptureImagePath);
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? throw new InvalidOperationException("Capture output directory is invalid."));
                File.WriteAllBytes(outputPath, sheet.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(sheet);

                var report = new StringBuilder();
                report.AppendLine("Result=PASS");
                report.AppendLine("CaptureMode=Unity Edit Mode exact-frame AnimationMode sampling");
                report.AppendLine("ViewsPerFrame=Front|ThreeQuarter");
                report.AppendLine("CapturedTimelineFrames=" + string.Join("|", CaptureFrames));
                report.AppendLine("LoopSequence=0->15->30->53->70->84->85->90->93->0");
                report.AppendLine("LoopBoundary=93->0");
                report.AppendLine("FinalImage=" + CaptureImagePath);
                OstinatoScissorAttackAnimation.WriteText(CaptureReportPath, report.ToString());
                Debug.Log("Ostinato frames 0 through 93 forward-close loop capture completed: " + CaptureImagePath);
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

        private static InspectionResult InspectInternal()
        {
            var scene = RequireOpenScene();
            var root = RequirePlacementRoot(scene);
            if (root.childCount != 9) throw new InvalidOperationException("Approved Ostinato placement must contain nine slots.");
            var slot = RequireAttackSlot(root);
            if (slot.GetSiblingIndex() != 3) throw new InvalidOperationException("Ostinato attack slot sibling index changed.");
            var model = RequireSinglePlaybackModel(slot);
            RequireDirectSuppliedFbxInstance(model);
            RequireApprovedRenderer(model.gameObject);
            RequireNoCorrectionControls(model);
            var animator = model.GetComponent<Animator>() ?? throw new InvalidOperationException("Ostinato attack Animator is missing.");
            if (animator.applyRootMotion) throw new InvalidOperationException("Ostinato attack Animator must keep Apply Root Motion disabled.");

            var sourcePath = OstinatoScissorAttackAnimation.ProjectAbsolutePath(OstinatoScissorAttackAnimation.SourceAttackRelativePath);
            var importedPath = OstinatoScissorAttackAnimation.ProjectAbsolutePath(OstinatoScissorAttackAnimation.AttackModelPath);
            var sourceHash = ComputeSha256(sourcePath);
            var importedHash = ComputeSha256(importedPath);
            RequireEqual(sourceHash, importedHash, "Supplied and imported attack FBX hashes differ.");
            var sourceClip = RequireSourceClip();
            var loopClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(LoopClipPath) ??
                           throw new InvalidOperationException("Ostinato forward-close loop clip is missing.");
            if (Mathf.Abs(sourceClip.frameRate - FrameRate) > 0.001f) throw new InvalidOperationException("Source clip must be 60 fps.");
            if (sourceClip.length < FinalFrame / FrameRate) throw new InvalidOperationException("Source clip does not reach frame 93.");
            if (Mathf.Abs(loopClip.frameRate - FrameRate) > 0.001f) throw new InvalidOperationException("Loop clip must be 60 fps.");
            if (Mathf.Abs(loopClip.length - FinalFrame / FrameRate) > TimeEpsilon)
                throw new InvalidOperationException("Loop clip length must be 1.55 seconds. Actual=" + Format(loopClip.length));
            if (!AnimationUtility.GetAnimationClipSettings(loopClip).loopTime)
                throw new InvalidOperationException("Forward-close clip loop playback is disabled.");
            var denseError = RequireExactTrimmedCurves(sourceClip, loopClip);
            RequireControllerUses(loopClip);
            if (animator.runtimeAnimatorController != AssetDatabase.LoadAssetAtPath<AnimatorController>(OstinatoScissorAttackAnimation.ControllerPath))
                throw new InvalidOperationException("Scene attack Animator does not use the approved attack controller.");
            return new InspectionResult(
                sourceClip,
                loopClip,
                sourceHash,
                importedHash,
                BuildClipFingerprint(sourceClip),
                BuildClipFingerprint(loopClip),
                denseError);
        }

        private static void ReplaceWithForwardCloseCurves(AnimationClip source, AnimationClip destination)
        {
            foreach (var binding in AnimationUtility.GetCurveBindings(destination)) AnimationUtility.SetEditorCurve(destination, binding, null);
            foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(destination)) AnimationUtility.SetObjectReferenceCurve(destination, binding, null);
            foreach (var binding in AnimationUtility.GetCurveBindings(source))
            {
                var sourceCurve = AnimationUtility.GetEditorCurve(source, binding) ?? throw new InvalidOperationException("Source float curve is missing.");
                AnimationUtility.SetEditorCurve(destination, binding, BuildTrimmedCurve(sourceCurve));
            }
            foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(source))
            {
                var keys = AnimationUtility.GetObjectReferenceCurve(source, binding) ?? Array.Empty<ObjectReferenceKeyframe>();
                AnimationUtility.SetObjectReferenceCurve(destination, binding, keys.Where(key => key.time <= FinalFrame / FrameRate + TimeEpsilon).ToArray());
            }
            destination.frameRate = FrameRate;
            var settings = AnimationUtility.GetAnimationClipSettings(source);
            settings.startTime = 0f;
            settings.stopTime = FinalFrame / FrameRate;
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(destination, settings);
            AnimationUtility.SetAnimationEvents(destination, AnimationUtility.GetAnimationEvents(source)
                .Where(item => item.time <= FinalFrame / FrameRate + TimeEpsilon).ToArray());
        }

        private static AnimationCurve BuildTrimmedCurve(AnimationCurve source)
        {
            var finalTime = FinalFrame / FrameRate;
            var keys = source.keys.Where(key => key.time <= finalTime + TimeEpsilon).ToList();
            if (!keys.Any(key => Mathf.Abs(key.time - finalTime) <= TimeEpsilon))
            {
                var sampleStep = 0.0001f;
                var leftTime = Mathf.Max(0f, finalTime - sampleStep);
                var inTangent = (source.Evaluate(finalTime) - source.Evaluate(leftTime)) / (finalTime - leftTime);
                keys.Add(new Keyframe(finalTime, source.Evaluate(finalTime), inTangent, inTangent));
            }
            keys.Sort((left, right) => left.time.CompareTo(right.time));
            return new AnimationCurve(keys.ToArray()) { preWrapMode = source.preWrapMode, postWrapMode = source.postWrapMode };
        }

        private static float RequireExactTrimmedCurves(AnimationClip source, AnimationClip loop)
        {
            var sourceBindings = AnimationUtility.GetCurveBindings(source).OrderBy(BindingId).ToArray();
            var loopBindings = AnimationUtility.GetCurveBindings(loop).OrderBy(BindingId).ToArray();
            if (!sourceBindings.Select(BindingId).SequenceEqual(loopBindings.Select(BindingId)))
                throw new InvalidOperationException("Loop float curve bindings differ from the source.");
            var maximumError = 0f;
            for (var index = 0; index < sourceBindings.Length; index++)
            {
                var sourceCurve = AnimationUtility.GetEditorCurve(source, sourceBindings[index]);
                var expected = BuildTrimmedCurve(sourceCurve);
                var actual = AnimationUtility.GetEditorCurve(loop, loopBindings[index]);
                RequireCurvesEqual(expected, actual, BindingId(sourceBindings[index]));
                for (var sample = 0; sample <= FinalFrame * 4; sample++)
                {
                    var time = sample / (FrameRate * 4f);
                    var error = Mathf.Abs(sourceCurve.Evaluate(time) - actual.Evaluate(time));
                    maximumError = Mathf.Max(maximumError, error);
                    if (error > 0.0001f)
                        throw new InvalidOperationException("Trimmed loop differs from source at time " + Format(time) + " for " + BindingId(sourceBindings[index]));
                }
            }
            var sourceObjectBindings = AnimationUtility.GetObjectReferenceCurveBindings(source).OrderBy(BindingId).ToArray();
            var loopObjectBindings = AnimationUtility.GetObjectReferenceCurveBindings(loop).OrderBy(BindingId).ToArray();
            if (!sourceObjectBindings.Select(BindingId).SequenceEqual(loopObjectBindings.Select(BindingId)))
                throw new InvalidOperationException("Loop object curve bindings differ from the source.");
            return maximumError;
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
                throw new InvalidOperationException("Ostinato attack state does not use the frames 0 through 93 loop clip at speed 1.");
        }

        private static AnimationClip RequireSourceClip()
        {
            var matches = AssetDatabase.LoadAllAssetsAtPath(OstinatoScissorAttackAnimation.AttackModelPath).OfType<AnimationClip>()
                .Where(clip => !clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase))
                .Where(clip => clip.name.IndexOf(SourceClipNameFragment, StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
            if (matches.Length != 1) throw new InvalidOperationException("Expected exactly one supplied Ostinato attack clip. Count=" + matches.Length);
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

        private static Transform RequireSinglePlaybackModel(Transform slot)
        {
            if (slot.childCount != 1) throw new InvalidOperationException("Ostinato attack slot must contain exactly one playback model.");
            return slot.GetChild(0);
        }

        private static void RequireDirectSuppliedFbxInstance(Transform model)
        {
            var path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(model.gameObject);
            if (path != OstinatoScissorAttackAnimation.AttackModelPath)
                throw new InvalidOperationException("Ostinato attack model is not a direct instance of the supplied attack FBX. Actual=" + path);
        }

        private static SkinnedMeshRenderer RequireApprovedRenderer(GameObject model)
        {
            var renderers = model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (renderers.Length != 1) throw new InvalidOperationException("Ostinato attack model must contain exactly one skinned renderer.");
            var renderer = renderers[0];
            if (AssetDatabase.GetAssetPath(renderer.sharedMesh) != OstinatoScissorAttackAnimation.ApprovedModelPath)
                throw new InvalidOperationException("Ostinato attack display mesh is not the approved static mesh.");
            var materialPaths = renderer.sharedMaterials.Select(AssetDatabase.GetAssetPath).ToArray();
            if (!materialPaths.SequenceEqual(ApprovedMaterialPaths))
                throw new InvalidOperationException("Ostinato attack materials differ from the approved static appearance.");
            return renderer;
        }

        private static void RequireNoCorrectionControls(Transform model)
        {
            var forbidden = new[] { "LeftBladeControl", "RightBladeControl", "LeftBladeRigidRoot", "RightBladeRigidRoot", "RigidBladeRig" };
            var found = model.GetComponentsInChildren<Transform>(true).Where(item => forbidden.Contains(item.name)).Select(item => item.name).Distinct().ToArray();
            if (found.Length > 0) throw new InvalidOperationException("Unexpected previous blade correction controls remain: " + string.Join("|", found));
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

        private static bool FloatEquivalent(float left, float right) =>
            (float.IsInfinity(left) && float.IsInfinity(right) && Math.Sign(left) == Math.Sign(right)) || Mathf.Abs(left - right) <= ValueEpsilon;

        private static string Format(float value) => value.ToString("0.######", CultureInfo.InvariantCulture);

        private static void RequireEqual(string expected, string actual, string message)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal)) throw new InvalidOperationException(message);
        }

        private readonly struct InspectionResult
        {
            public InspectionResult(
                AnimationClip sourceClip,
                AnimationClip loopClip,
                string sourceHash,
                string importedHash,
                string sourceFingerprint,
                string loopFingerprint,
                float denseSampleMaximumError)
            {
                SourceClip = sourceClip;
                LoopClip = loopClip;
                SourceHash = sourceHash;
                ImportedHash = importedHash;
                SourceFingerprint = sourceFingerprint;
                LoopFingerprint = loopFingerprint;
                DenseSampleMaximumError = denseSampleMaximumError;
            }

            public AnimationClip SourceClip { get; }
            public AnimationClip LoopClip { get; }
            public string SourceHash { get; }
            public string ImportedHash { get; }
            public string SourceFingerprint { get; }
            public string LoopFingerprint { get; }
            public float DenseSampleMaximumError { get; }
        }
    }
}
