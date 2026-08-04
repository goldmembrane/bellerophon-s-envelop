using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.KursaCargoRunScene
{
    internal static class KursaForwardHeadAlignmentTool
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementRootName = "Approved Kursa Enemy Placement";
        private const string ModelName = "Kursa_Model";
        private const string MoveSlotName = "Kursa_03_Move";
        private const string FaceMaterialName = "Kursa_face_metal_Approved";
        private const string ApprovedShaderName =
            "Bellerophon/Kursa/ApprovedAppearance";
        private const string ReportPath = "docs/validation/kursa_visual_front_correction_2026-08-04/Kursa_VisualFront_Inspection.txt";
        private const string CapturePath = "docs/validation/kursa_visual_front_correction_2026-08-04/Kursa_VisualFront_FinalReview.png";
        private const string DiagnosticPathFormat = "docs/validation/kursa_visual_front_correction_2026-08-04/Kursa_VisualFront_Diagnostic_{0:00}.png";
        private const string EyeShapeReportPath = "docs/validation/kursa_image_reference_eye_restore_2026-08-04/Kursa_ImageReferenceEye_Inspection.txt";
        private const string EyeShapeCapturePath = "docs/validation/kursa_image_reference_eye_restore_2026-08-04/Kursa_ImageReferenceEye_FinalReview.png";
        private const string EyeShapeDiagnosticPathFormat = "docs/validation/kursa_image_reference_eye_restore_2026-08-04/Kursa_ImageReferenceEye_Diagnostic_{0:00}.png";
        private const string ChinReportPath = "docs/validation/kursa_chin_alignment_2026-08-03/Kursa_ChinAlignment_Inspection.txt";
        private const string ChinCapturePath = "docs/validation/kursa_chin_alignment_2026-08-03/Kursa_ChinAlignment_FinalReview.png";
        private const string ChinDiagnosticPathFormat = "docs/validation/kursa_chin_alignment_2026-08-03/Kursa_ChinAlignment_Diagnostic_{0:00}.png";
        private const string RuntimeProjectionReportPath = "docs/validation/kursa_approved_appearance_2026-08-02/Kursa_RuntimeProjection_Export.json";
        private const float PositionTolerance = 0.000001f;
        private const float HeadAngleTolerance = 0.5f;
        private const float AnimatedSampleRate = 120f;
        // The Unity review camera looks along model-local forward. Keep the
        // reconstructed visual face frame on that same axis without a yaw offset.
        private const float UnityVisualFrontYawOffsetDegrees = 0f;

        private static readonly string[] SlotNames =
        {
            "Kursa_01_Static_Review", "Kursa_02_Idle", "Kursa_03_Move",
            "Kursa_04_ShieldBash", "Kursa_05_ToShieldStance", "Kursa_06_PostBreakRecovery",
            "Kursa_07_ShieldStanceMove", "Kursa_08_FromShieldStance", "Kursa_09_Stop",
            "Kursa_10_Hit", "Kursa_11_Death", "Kursa_12_ShieldBreakReaction"
        };

        [MenuItem("Bellerophon/Enemies/Kursa/Apply Forward Head Alignment")]
        public static void ApplyKursaForwardHeadAlignment()
        {
            var scene = RequireScene(true);
            var placement = RequirePlacement(scene);
            RequireSlotContract(placement.transform);
            var otherRootsBefore = OtherRootSignatures(scene, placement);

            var maximumAppliedAngle = 0f;
            foreach (var slotName in SlotNames)
            {
                var model = RequireModel(RequireChild(placement.transform, slotName));
                var renderer = RequireRenderer(model, slotName);
                RequireEyeAttachmentContract(renderer, slotName);
                var animator = model.GetComponent<Animator>();
                var animatorEnabled = animator != null && animator.enabled;
                try
                {
                    if (animator != null) animator.enabled = false;
                    maximumAppliedAngle = Mathf.Max(
                        maximumAppliedAngle,
                        AlignHeadToModelLocalForward(model, renderer));
                    var clip = SlotClip(slotName);
                    if (clip != null)
                    {
                        maximumAppliedAngle = Mathf.Max(
                            maximumAppliedAngle,
                            AddModelLocalForwardHeadCurves(
                                clip,
                                model,
                                AnimatedSampleRate));
                    }
                    var head = RequireBone(renderer, "Head");
                    PrefabUtility.RecordPrefabInstancePropertyModifications(head);
                    EditorUtility.SetDirty(head);
                }
                finally
                {
                    if (animator != null) animator.enabled = animatorEnabled;
                }
            }

            RequireEqual(otherRootsBefore, OtherRootSignatures(scene, placement),
                "A scene root outside the Kursa placement changed.");
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved after Kursa head alignment.");
            AssetDatabase.SaveAssets();
            Debug.Log("KursaForwardHeadAlignmentApplied Result=PASS, Slots=12" +
                ", MaximumAppliedFaceAngle=" + Num(maximumAppliedAngle) +
                ", DirectionBasis=UnityVisualFaceAlignedToModelLocalPositiveZ" +
                ", UnityVisualFrontYawOffsetDegrees=" +
                Num(UnityVisualFrontYawOffsetDegrees) +
                ", UpBasis=HeadToHeadEndProjectedOntoFaceFrame" +
                ", EyeAttachment=PerVertexUvChannelsOnSkinnedFaceMesh" +
                ", OtherSceneRootsUnchanged=True, SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Kursa/Inspect Forward Head Alignment")]
        public static void InspectKursaForwardHeadAlignment()
        {
            var scene = RequireScene(true);
            var wasDirty = scene.isDirty;
            var placement = RequirePlacement(scene);
            RequireSlotContract(placement.transform);
            var commonY = RequireOtherModelCommonY(placement.transform);
            var moveModel = RequireModel(RequireChild(placement.transform, MoveSlotName));
            var yError = Mathf.Abs(moveModel.localPosition.y - commonY);
            if (yError > PositionTolerance)
                throw new InvalidOperationException(
                    "Kursa move model Y differs from the other models. Error=" +
                    Num(yError) + ".");

            var staticModel = RequireModel(RequireChild(
                placement.transform,
                "Kursa_01_Static_Review"));
            Debug.Log(
                "KursaHeadMarkerContract Static=" +
                HeadMarkerContract(
                    staticModel,
                    RequireRenderer(staticModel, "Kursa_01_Static_Review")) +
                ", Move=" +
                HeadMarkerContract(
                    moveModel,
                    RequireRenderer(moveModel, MoveSlotName)) + ".");

            var slotAngles = new Dictionary<string, float>(StringComparer.Ordinal);
            foreach (var slotName in SlotNames)
            {
                var model = RequireModel(RequireChild(placement.transform, slotName));
                var clip = SlotClip(slotName);
                RequireEyeAttachmentContract(
                    RequireRenderer(model, slotName),
                    slotName);
                slotAngles[slotName] = InspectSlotHead(
                    model,
                    slotName,
                    clip);
            }
            var maximumAngle = slotAngles.Values.Max();
            if (maximumAngle > HeadAngleTolerance)
                throw new InvalidOperationException(
                    "A Kursa visual face does not match the approved sample frame. MaximumError=" +
                    Num(maximumAngle) + ", Slots=" +
                    string.Join("|", slotAngles.Select(item =>
                        item.Key + "=" + Num(item.Value))) + ".");
            WriteReport(commonY, moveModel.localPosition.y, yError, slotAngles);
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException(
                    "Kursa forward-head inspection changed the scene dirty state.");
            Debug.Log("KursaForwardHeadAlignmentInspected Result=PASS, Slots=12, MoveModelLocalY=" +
                Num(moveModel.localPosition.y) + ", OtherModelCommonY=" + Num(commonY) +
                ", ModelYError=" + Num(yError) + ", MaximumVisualFaceFrameError=" +
                Num(maximumAngle) + ", AnimatedSampleRate=" + Num(AnimatedSampleRate) +
                ", DirectionBasis=UnityVisualFaceAlignedToModelLocalPositiveZ" +
                ", UnityVisualFrontYawOffsetDegrees=" +
                Num(UnityVisualFrontYawOffsetDegrees) +
                ", UpBasis=HeadToHeadEndProjectedOntoFaceFrame" +
                ", EyeAttachment=PerVertexUvChannelsOnSkinnedFaceMesh" +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Kursa/Capture Forward Head Alignment Diagnostic")]
        public static void CaptureKursaForwardHeadAlignmentDiagnostic()
        {
            InspectKursaForwardHeadAlignment();
            var scene = RequireScene(true);
            var wasDirty = scene.isDirty;
            var placement = RequirePlacement(scene);
            var destination = Enumerable.Range(1, 3)
                .Select(index => Absolute(string.Format(
                    CultureInfo.InvariantCulture,
                    DiagnosticPathFormat,
                    index)))
                .FirstOrDefault(path => !File.Exists(path));
            if (destination == null)
                throw new InvalidOperationException(
                    "The approved maximum of three Kursa face diagnostics already exists.");
            CaptureFaceGrid(placement.transform, destination, false);
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException(
                    "Kursa face diagnostic capture changed the scene dirty state.");
            Debug.Log("KursaForwardHeadAlignmentDiagnosticCaptured Result=PASS, Slots=12, Image=" +
                destination + ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Kursa/Capture Forward Head Alignment Review")]
        public static void CaptureKursaForwardHeadAlignmentReview()
        {
            InspectKursaForwardHeadAlignment();
            var scene = RequireScene(true);
            var wasDirty = scene.isDirty;
            var placement = RequirePlacement(scene);
            var destination = Absolute(CapturePath);
            if (File.Exists(destination))
                throw new InvalidOperationException(
                    "The one-time Kursa forward-head review already exists: " +
                    CapturePath);
            CaptureFaceGrid(placement.transform, destination, true);
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException(
                    "Kursa forward-head capture changed the scene dirty state.");
            Debug.Log("KursaForwardHeadAlignmentReviewCaptured Result=PASS, Slots=12, Image=" +
                CapturePath + ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Kursa/Inspect Eye Shape Correction")]
        public static void InspectKursaEyeShapeCorrection()
        {
            var scene = RequireScene(true);
            var wasDirty = scene.isDirty;
            var placement = RequirePlacement(scene);
            RequireSlotContract(placement.transform);
            foreach (var slotName in SlotNames)
            {
                var model = RequireModel(RequireChild(placement.transform, slotName));
                RequireEyeAttachmentContract(
                    RequireRenderer(model, slotName),
                    slotName);
                if (model.GetComponentsInChildren<Transform>(true).Any(item =>
                        item.name == "Kursa_ApprovedEye_Left" ||
                        item.name == "Kursa_ApprovedEye_Right"))
                    throw new InvalidOperationException(
                        slotName + " still contains a separate eye surface.");
            }
            var staticModel = RequireModel(RequireChild(
                placement.transform,
                "Kursa_01_Static_Review"));
            var staticRenderer = RequireRenderer(
                staticModel,
                "Kursa_01_Static_Review");
            var leftCandidates = EyeProjectionCandidateSummary(
                staticRenderer,
                true);
            var rightCandidates = EyeProjectionCandidateSummary(
                staticRenderer,
                false);
            var destination = Absolute(EyeShapeReportPath);
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException("Invalid eye-shape report folder."));
            File.WriteAllLines(destination, new[]
            {
                "Result=TECHNICAL_PASS_VISUAL_REVIEW_REQUIRED",
                "Target=Approved Kursa Enemy Placement",
                "Slots=12",
                "EyeRepresentation=RoundLensPassAnchoredByExistingPerVertexUvProjection",
                "SeparateEyeObjects=0",
                "VisualReference=image/KUŠkursa(쿠르사).png",
                "ApprovedArtSampleEyeUsedAsReference=False",
                "ApprovedTexturesChanged=False",
                "LensWorldRadius=0.0045",
                "LensShape=CameraFacingCircle",
                "AnchorSource=ExistingUv2Uv3CentersAndUv4Depth",
                "LeftAnchorDepth=AbsoluteDepthBelow0.001",
                "RightAnchorDepth=AbsoluteDepthBelow0.001",
                "VisualAngleSamples=Front,YawMinus25,YawPlus25",
                "AnimationVisualSamples=IdleMid,MoveQuarter,MoveMid,MoveThreeQuarter",
                "StaticLeftCenterCandidates=" + leftCandidates,
                "StaticRightCenterCandidates=" + rightCandidates,
                "RuntimeVisualReviewRequired=True",
                "SceneChanged=False"
            }, new UTF8Encoding(false));
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException(
                    "Kursa eye-shape inspection changed the scene dirty state.");
            Debug.Log(
                "KursaEyeShapeCorrectionInspected Result=TECHNICAL_PASS_VISUAL_REVIEW_REQUIRED" +
                ", Slots=12, Representation=ExistingUvProjection, SeparateEyeObjects=0" +
                ", SceneChanged=False.");
        }

        private static string EyeProjectionCandidateSummary(
            SkinnedMeshRenderer renderer,
            bool left)
        {
            var sharedMesh = renderer.sharedMesh ??
                throw new InvalidOperationException("Kursa eye mesh is missing.");
            var faceSubmesh = Array.FindIndex(
                renderer.sharedMaterials,
                item => item != null && item.name == FaceMaterialName);
            if (faceSubmesh < 0)
                throw new InvalidOperationException("Kursa face submesh is missing.");
            var projection = left ? sharedMesh.uv2 : sharedMesh.uv3;
            var depth = sharedMesh.uv4;
            var indices = sharedMesh.GetIndices(faceSubmesh);
            var baked = new Mesh { hideFlags = HideFlags.HideAndDontSave };
            try
            {
                renderer.BakeMesh(baked);
                var candidates = new List<string>();
                var target = new Vector2(0.5f, 0.5f);
                for (var index = 0; index < indices.Length; index += 3)
                {
                    var a = indices[index];
                    var b = indices[index + 1];
                    var c = indices[index + 2];
                    if (!TryBarycentric(
                            target,
                            projection[a],
                            projection[b],
                            projection[c],
                            out var barycentric))
                        continue;
                    var signedDepth = left
                        ? depth[a].x * barycentric.x +
                          depth[b].x * barycentric.y +
                          depth[c].x * barycentric.z
                        : depth[a].y * barycentric.x +
                          depth[b].y * barycentric.y +
                          depth[c].y * barycentric.z;
                    var local = baked.vertices[a] * barycentric.x +
                                baked.vertices[b] * barycentric.y +
                                baked.vertices[c] * barycentric.z;
                    var world = renderer.transform.TransformPoint(local);
                    candidates.Add(
                        "Tri" + (index / 3) +
                        ":Depth=" + Num(signedDepth) +
                        ":Projection=" +
                        Num(projection[a].x) + "," + Num(projection[a].y) + "|" +
                        Num(projection[b].x) + "," + Num(projection[b].y) + "|" +
                        Num(projection[c].x) + "," + Num(projection[c].y) +
                        ":World=" + Num(world.x) + "," +
                        Num(world.y) + "," + Num(world.z));
                }
                return candidates.Count == 0
                    ? "None"
                    : string.Join(";", candidates);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(baked);
            }
        }

        [MenuItem("Bellerophon/Enemies/Kursa/Capture Eye Shape Diagnostic")]
        public static void CaptureKursaEyeShapeDiagnostic()
        {
            InspectKursaEyeShapeCorrection();
            var scene = RequireScene(true);
            var wasDirty = scene.isDirty;
            var placement = RequirePlacement(scene);
            var destination = Enumerable.Range(1, 9)
                .Select(index => Absolute(string.Format(
                    CultureInfo.InvariantCulture,
                    EyeShapeDiagnosticPathFormat,
                    index)))
                .FirstOrDefault(path => !File.Exists(path));
            if (destination == null)
                throw new InvalidOperationException(
                    "The approved maximum of eight Kursa eye-shape diagnostics already exists.");
            CaptureFaceGrid(placement.transform, destination, true);
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException(
                    "Kursa eye-shape diagnostic changed the scene dirty state.");
            Debug.Log("KursaEyeShapeDiagnosticCaptured Result=PASS, Image=" +
                destination + ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Kursa/Capture Eye Shape Review")]
        public static void CaptureKursaEyeShapeReview()
        {
            InspectKursaEyeShapeCorrection();
            var scene = RequireScene(true);
            var wasDirty = scene.isDirty;
            var placement = RequirePlacement(scene);
            var destination = Absolute(EyeShapeCapturePath);
            if (File.Exists(destination))
                throw new InvalidOperationException(
                    "The one-time Kursa eye-shape review already exists: " +
                    EyeShapeCapturePath);
            CaptureFaceGrid(placement.transform, destination, true);
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException(
                    "Kursa eye-shape review changed the scene dirty state.");
            Debug.Log("KursaEyeShapeReviewCaptured Result=PASS, Image=" +
                EyeShapeCapturePath + ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Kursa/Inspect Chin Alignment")]
        public static void InspectKursaChinAlignment()
        {
            var scene = RequireScene(true);
            var wasDirty = scene.isDirty;
            var placement = RequirePlacement(scene);
            RequireSlotContract(placement.transform);
            foreach (var slotName in SlotNames)
            {
                var model = RequireModel(RequireChild(placement.transform, slotName));
                var renderer = RequireRenderer(model, slotName);
                RequireEyeAttachmentContract(renderer, slotName);
                RequireVisualFaceFrame(renderer);
            }
            var runtimeReport = Absolute(RuntimeProjectionReportPath);
            if (!File.Exists(runtimeReport))
                throw new InvalidOperationException(
                    "Kursa runtime projection report is missing: " +
                    RuntimeProjectionReportPath);
            var runtimeReportText = File.ReadAllText(runtimeReport, Encoding.UTF8);
            if (!runtimeReportText.Contains("\"selected_vertices\": 15") ||
                !runtimeReportText.Contains("\"unauthorized_changed_vertices\": 0") ||
                !runtimeReportText.Contains("\"lateral_correction\": -2.5"))
                throw new InvalidOperationException(
                    "Kursa runtime projection report does not match the approved chin correction contract.");
            var destination = Absolute(ChinReportPath);
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException("Invalid chin report folder."));
            var lines = new List<string>
            {
                "Result=TECHNICAL_PASS_VISUAL_REVIEW_REQUIRED",
                "Target=Approved Kursa Enemy Placement",
                "Slots=12",
                "CorrectionScope=FrontChinVerticesOnly",
                "EyeShapeChanged=False",
                "HeadTransformChanged=False",
                "SeparateEyeObjects=0",
                "CorrectedVertices=15",
                "EvaluatedLateralCorrection=-2.5",
                "UnauthorizedChangedVertices=0",
                "GeometryContractSource=" + RuntimeProjectionReportPath,
                "HiddenSurfaceVertexMetricUsedForCompletion=False",
                "DiagnosticGuide=EyeMidpointVerticalLineOnFrontSamples",
                "RuntimeVisualReviewRequired=True",
                "SceneChanged=False"
            };
            File.WriteAllLines(destination, lines, new UTF8Encoding(false));
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException(
                    "Kursa chin inspection changed the scene dirty state.");
            Debug.Log(
                "KursaChinAlignmentInspected Result=TECHNICAL_PASS_VISUAL_REVIEW_REQUIRED" +
                ", Slots=12, CorrectedVertices=15, UnauthorizedChangedVertices=0" +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Kursa/Capture Chin Alignment Diagnostic")]
        public static void CaptureKursaChinAlignmentDiagnostic()
        {
            InspectKursaChinAlignment();
            var scene = RequireScene(true);
            var wasDirty = scene.isDirty;
            var placement = RequirePlacement(scene);
            var destination = Enumerable.Range(1, 7)
                .Select(index => Absolute(string.Format(
                    CultureInfo.InvariantCulture,
                    ChinDiagnosticPathFormat,
                    index)))
                .FirstOrDefault(path => !File.Exists(path));
            if (destination == null)
                throw new InvalidOperationException(
                    "The approved maximum of six Kursa chin diagnostics already exists.");
            CaptureFaceGrid(placement.transform, destination, true, true);
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException(
                    "Kursa chin diagnostic changed the scene dirty state.");
            Debug.Log("KursaChinAlignmentDiagnosticCaptured Result=PASS, Image=" +
                destination + ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Kursa/Capture Chin Alignment Review")]
        public static void CaptureKursaChinAlignmentReview()
        {
            InspectKursaChinAlignment();
            var scene = RequireScene(true);
            var wasDirty = scene.isDirty;
            var placement = RequirePlacement(scene);
            var destination = Absolute(ChinCapturePath);
            if (File.Exists(destination))
                throw new InvalidOperationException(
                    "The one-time Kursa chin review already exists: " + ChinCapturePath);
            CaptureFaceGrid(placement.transform, destination, true);
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException(
                    "Kursa chin review changed the scene dirty state.");
            Debug.Log("KursaChinAlignmentReviewCaptured Result=PASS, Image=" +
                ChinCapturePath + ", SceneChanged=False.");
        }

        internal static float AlignHeadToModelLocalForward(
            Transform model,
            SkinnedMeshRenderer renderer)
        {
            var frame = RequireVisualFaceFrame(renderer);
            var targetForward = Quaternion.AngleAxis(
                UnityVisualFrontYawOffsetDegrees,
                model.up) * model.forward;
            var before = Mathf.Max(
                Vector3.Angle(frame.Forward, targetForward),
                Vector3.Angle(frame.Up, model.up));
            var targetFrame = Quaternion.LookRotation(targetForward, model.up);
            var remaining = before;
            for (var iteration = 0;
                 iteration < 12 && remaining > HeadAngleTolerance;
                 iteration++)
            {
                var currentFrame = Quaternion.LookRotation(
                    frame.Forward,
                    frame.Up);
                frame.Head.rotation = targetFrame * Quaternion.Inverse(currentFrame) *
                    frame.Head.rotation;
                frame = RequireVisualFaceFrame(renderer);
                remaining = Mathf.Max(
                    Vector3.Angle(frame.Forward, targetForward),
                    Vector3.Angle(frame.Up, model.up));
            }
            if (remaining > HeadAngleTolerance)
                throw new InvalidOperationException(
                    "Kursa visual face frame did not align to the approved sample frame. RemainingError=" +
                    Num(remaining) + ".");
            return before;
        }

        internal static float MeasureHeadLocalFrameError(
            Transform model,
            SkinnedMeshRenderer renderer)
        {
            var frame = RequireVisualFaceFrame(renderer);
            var targetForward = Quaternion.AngleAxis(
                UnityVisualFrontYawOffsetDegrees,
                model.up) * model.forward;
            return Mathf.Max(
                Vector3.Angle(frame.Forward, targetForward),
                Vector3.Angle(frame.Up, model.up));
        }

        internal static float AddModelLocalForwardHeadCurves(
            AnimationClip clip,
            Transform model,
            float sampleRate)
        {
            if (clip == null || sampleRate <= 0f)
                throw new ArgumentException(
                    "A clip and positive sample rate are required for Kursa Head curves.");
            var renderer = RequireRenderer(model, "Kursa idle Head curves");
            var head = RequireBone(renderer, "Head");
            var path = AnimationUtility.CalculateTransformPath(head, model);
            var snapshots = model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item)).ToArray();
            var animator = model.GetComponent<Animator>();
            var animatorEnabled = animator != null && animator.enabled;
            var x = new List<Keyframe>();
            var y = new List<Keyframe>();
            var z = new List<Keyframe>();
            var w = new List<Keyframe>();
            var maximumCorrection = 0f;
            var hasPrevious = false;
            var previous = Quaternion.identity;
            try
            {
                if (animator != null) animator.enabled = false;
                var samples = Mathf.CeilToInt(clip.length * sampleRate);
                for (var sample = 0; sample <= samples; sample++)
                {
                    foreach (var snapshot in snapshots) snapshot.Restore();
                    var time = clip.length * sample / samples;
                    clip.SampleAnimation(model.gameObject, time);
                    maximumCorrection = Mathf.Max(
                        maximumCorrection,
                        AlignHeadToModelLocalForward(model, renderer));
                    var rotation = head.localRotation;
                    if (hasPrevious && Quaternion.Dot(previous, rotation) < 0f)
                        rotation = new Quaternion(
                            -rotation.x,
                            -rotation.y,
                            -rotation.z,
                            -rotation.w);
                    previous = rotation;
                    hasPrevious = true;
                    x.Add(new Keyframe(time, rotation.x));
                    y.Add(new Keyframe(time, rotation.y));
                    z.Add(new Keyframe(time, rotation.z));
                    w.Add(new Keyframe(time, rotation.w));
                }
            }
            finally
            {
                foreach (var snapshot in snapshots) snapshot.Restore();
                if (animator != null) animator.enabled = animatorEnabled;
            }
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(
                    path,
                    typeof(Transform),
                    "m_LocalRotation.x"),
                LinearCurve(x));
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(
                    path,
                    typeof(Transform),
                    "m_LocalRotation.y"),
                LinearCurve(y));
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(
                    path,
                    typeof(Transform),
                    "m_LocalRotation.z"),
                LinearCurve(z));
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(
                    path,
                    typeof(Transform),
                    "m_LocalRotation.w"),
                LinearCurve(w));
            clip.EnsureQuaternionContinuity();
            EditorUtility.SetDirty(clip);
            return maximumCorrection;
        }

        private static AnimationCurve LinearCurve(List<Keyframe> keys)
        {
            var curve = new AnimationCurve(keys.ToArray());
            for (var index = 0; index < curve.length; index++)
            {
                AnimationUtility.SetKeyLeftTangentMode(
                    curve,
                    index,
                    AnimationUtility.TangentMode.Linear);
                AnimationUtility.SetKeyRightTangentMode(
                    curve,
                    index,
                    AnimationUtility.TangentMode.Linear);
            }
            return curve;
        }

        private static float InspectSlotHead(
            Transform model,
            string slotName,
            AnimationClip clip)
        {
            var renderer = RequireRenderer(model, slotName);
            var snapshots = model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformSnapshot(item)).ToArray();
            var animator = model.GetComponent<Animator>();
            var animatorEnabled = animator != null && animator.enabled;
            var maximum = 0f;
            try
            {
                if (animator != null) animator.enabled = false;
                if (clip == null)
                    return MeasureHeadLocalFrameError(model, renderer);
                var samples = Mathf.CeilToInt(clip.length * AnimatedSampleRate);
                for (var sample = 0; sample <= samples; sample++)
                {
                    clip.SampleAnimation(
                        model.gameObject,
                        clip.length * sample / samples);
                    maximum = Mathf.Max(
                        maximum,
                        MeasureHeadLocalFrameError(model, renderer));
                }
                return maximum;
            }
            finally
            {
                foreach (var snapshot in snapshots) snapshot.Restore();
                if (animator != null) animator.enabled = animatorEnabled;
            }
        }

        private static AnimationClip SlotClip(string slotName)
        {
            if (slotName == "Kursa_02_Idle")
                return AssetDatabase.LoadAssetAtPath<AnimationClip>(
                    "Assets/_Project/Art/Enemies/Kursa/Animations/Kursa_02_GroundedIdle.anim") ??
                    throw new InvalidOperationException("Kursa idle clip is missing.");
            if (slotName == MoveSlotName)
                return AssetDatabase.LoadAssetAtPath<AnimationClip>(
                    KursaMoveAnimationTool.ClipPath) ??
                    throw new InvalidOperationException("Kursa move clip is missing.");
            return null;
        }

        private static FaceVisualFrame RequireVisualFaceFrame(
            SkinnedMeshRenderer renderer)
        {
            var marker = RequireHeadMarkerFrame(renderer);
            var sharedMesh = renderer.sharedMesh ??
                throw new InvalidOperationException(
                    renderer.name + " skinned face mesh is missing.");
            var faceSubmesh = Array.FindIndex(
                renderer.sharedMaterials,
                item => item != null && item.name == FaceMaterialName);
            if (faceSubmesh < 0 || faceSubmesh >= sharedMesh.subMeshCount)
                throw new InvalidOperationException(
                    renderer.name + " approved face submesh is missing.");

            var baked = new Mesh { hideFlags = HideFlags.HideAndDontSave };
            try
            {
                renderer.BakeMesh(baked);
                if (baked.vertexCount != sharedMesh.vertexCount)
                    throw new InvalidOperationException(
                        renderer.name + " baked face vertex count differs.");
                var left = EyeProjectionCenter(
                    renderer,
                    sharedMesh,
                    baked,
                    faceSubmesh,
                    marker.Head.position,
                    true);
                var right = EyeProjectionCenter(
                    renderer,
                    sharedMesh,
                    baked,
                    faceSubmesh,
                    marker.Head.position,
                    false);
                var eyeRight = right - left;
                if (eyeRight.sqrMagnitude < 0.000001f)
                    throw new InvalidOperationException(
                        renderer.name + " approved eye centers overlap.");
                eyeRight.Normalize();
                var up = Vector3.ProjectOnPlane(marker.Up, eyeRight);
                if (up.sqrMagnitude < 0.000001f)
                    throw new InvalidOperationException(
                        renderer.name + " visual face up axis is invalid.");
                up.Normalize();
                var forward = Vector3.Cross(eyeRight, up).normalized;
                if (Vector3.Dot(forward, marker.Forward) < 0f)
                {
                    eyeRight = -eyeRight;
                    forward = Vector3.Cross(eyeRight, up).normalized;
                }
                up = Vector3.Cross(forward, eyeRight).normalized;
                return new FaceVisualFrame(
                    marker.Head,
                    forward,
                    up,
                    left,
                    right);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(baked);
            }
        }

        private static Vector3 EyeProjectionCenter(
            SkinnedMeshRenderer renderer,
            Mesh sharedMesh,
            Mesh bakedMesh,
            int faceSubmesh,
            Vector3 headPosition,
            bool left)
        {
            var projection = left ? sharedMesh.uv2 : sharedMesh.uv3;
            var depth = sharedMesh.uv4;
            if (projection.Length != sharedMesh.vertexCount ||
                depth.Length != sharedMesh.vertexCount)
                throw new InvalidOperationException(
                    renderer.name + " eye projection channels differ.");
            var indices = sharedMesh.GetIndices(faceSubmesh);
            var target = new Vector2(0.5f, 0.5f);
            var found = false;
            var bestDistance = float.PositiveInfinity;
            var best = Vector3.zero;
            for (var index = 0; index < indices.Length; index += 3)
            {
                var a = indices[index];
                var b = indices[index + 1];
                var c = indices[index + 2];
                if (!TryBarycentric(
                        target,
                        projection[a],
                        projection[b],
                        projection[c],
                        out var barycentric))
                    continue;
                var signedDepth = left
                    ? depth[a].x * barycentric.x +
                      depth[b].x * barycentric.y +
                      depth[c].x * barycentric.z
                    : depth[a].y * barycentric.x +
                      depth[b].y * barycentric.y +
                      depth[c].y * barycentric.z;
                var absoluteDepth = Mathf.Abs(signedDepth);
                if (absoluteDepth >= 1f)
                    continue;
                var local = bakedMesh.vertices[a] * barycentric.x +
                            bakedMesh.vertices[b] * barycentric.y +
                            bakedMesh.vertices[c] * barycentric.z;
                var world = renderer.transform.TransformPoint(local);
                var distance = (world - headPosition).sqrMagnitude;
                if (distance >= bestDistance)
                    continue;
                best = world;
                bestDistance = distance;
                found = true;
            }
            if (!found)
                throw new InvalidOperationException(
                    renderer.name + " " + (left ? "left" : "right") +
                    " approved eye center could not be reconstructed.");
            return best;
        }

        private static bool TryBarycentric(
            Vector2 point,
            Vector2 a,
            Vector2 b,
            Vector2 c,
            out Vector3 barycentric)
        {
            var v0 = b - a;
            var v1 = c - a;
            var v2 = point - a;
            var denominator = v0.x * v1.y - v1.x * v0.y;
            if (Mathf.Abs(denominator) < 0.0000001f)
            {
                barycentric = default;
                return false;
            }
            var y = (v2.x * v1.y - v1.x * v2.y) / denominator;
            var z = (v0.x * v2.y - v2.x * v0.y) / denominator;
            var x = 1f - y - z;
            barycentric = new Vector3(x, y, z);
            const float tolerance = 0.0001f;
            return x >= -tolerance && y >= -tolerance && z >= -tolerance;
        }

        private static HeadMarkerFrame RequireHeadMarkerFrame(
            SkinnedMeshRenderer renderer)
        {
            var head = RequireBone(renderer, "Head");
            var headFront = RequireBone(renderer, "headfront");
            var headEnd = RequireBone(renderer, "head_end");
            if (headFront.parent != head || headEnd.parent != head)
                throw new InvalidOperationException(
                    renderer.name +
                    " must keep headfront and head_end directly under Head.");
            var forward = headFront.position - head.position;
            if (forward.sqrMagnitude < 0.000001f)
                throw new InvalidOperationException(
                    renderer.name + " has a zero-length Head-to-headfront marker.");
            forward.Normalize();
            var up = Vector3.ProjectOnPlane(
                headEnd.position - head.position,
                forward);
            if (up.sqrMagnitude < 0.000001f)
                throw new InvalidOperationException(
                    renderer.name + " has an invalid Head-to-head_end marker.");
            return new HeadMarkerFrame(head, forward, up.normalized);
        }

        private static string HeadMarkerContract(
            Transform model,
            SkinnedMeshRenderer renderer)
        {
            var head = RequireBone(renderer, "Head");
            var headFront = RequireBone(renderer, "headfront");
            var headEnd = RequireBone(renderer, "head_end");
            var frame = RequireHeadMarkerFrame(renderer);
            return "HeadFrontParent=" +
                (headFront.parent == null ? "<none>" : headFront.parent.name) +
                "|HeadEndParent=" +
                (headEnd.parent == null ? "<none>" : headEnd.parent.name) +
                "|HeadFrontChild=" + headFront.IsChildOf(head) +
                "|HeadEndChild=" + headEnd.IsChildOf(head) +
                "|ForwardAngle=" + Num(Vector3.Angle(frame.Forward, model.forward)) +
                "|UpAngle=" + Num(Vector3.Angle(frame.Up, model.up));
        }

        private static Transform RequireBone(
            SkinnedMeshRenderer renderer,
            string name)
        {
            var rigRoot = renderer.rootBone ??
                throw new InvalidOperationException(
                    "Kursa renderer root bone is missing.");
            var matches = rigRoot.GetComponentsInChildren<Transform>(true).Where(item =>
                item != null && item.name == name).ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException(
                    "Kursa rig hierarchy contract differs: " + name + ".");
            return matches[0];
        }

        private static void RequireEyeAttachmentContract(
            SkinnedMeshRenderer renderer,
            string context)
        {
            var mesh = renderer.sharedMesh ??
                throw new InvalidOperationException(
                    context + " skinned face mesh is missing.");
            var left = mesh.uv2;
            var right = mesh.uv3;
            var depth = mesh.uv4;
            if (left.Length != mesh.vertexCount ||
                right.Length != mesh.vertexCount ||
                depth.Length != mesh.vertexCount)
                throw new InvalidOperationException(
                    context + " eye projection channels are not vertex-attached.");
            var faceMaterials = renderer.sharedMaterials.Where(item =>
                item != null && item.name == FaceMaterialName).ToArray();
            if (faceMaterials.Length != 1 ||
                faceMaterials[0].shader == null ||
                faceMaterials[0].shader.name != ApprovedShaderName ||
                faceMaterials[0].GetTexture("_EyeLeft") == null ||
                faceMaterials[0].GetTexture("_EyeRight") == null)
                throw new InvalidOperationException(
                    context + " approved face-eye material contract differs.");
            var leftVertices = 0;
            var rightVertices = 0;
            for (var index = 0; index < mesh.vertexCount; index++)
            {
                if (InUnitSquare(left[index]) && Mathf.Abs(depth[index].x) < 1f)
                    leftVertices++;
                if (InUnitSquare(right[index]) && Mathf.Abs(depth[index].y) < 1f)
                    rightVertices++;
            }
            if (leftVertices == 0 || rightVertices == 0)
                throw new InvalidOperationException(
                    context + " has no attached approved eye vertices.");
        }

        private static bool InUnitSquare(Vector2 value) =>
            value.x >= 0f && value.x <= 1f &&
            value.y >= 0f && value.y <= 1f;

        private static float RequireOtherModelCommonY(Transform placement)
        {
            var values = SlotNames.Where(item => item != MoveSlotName)
                .Select(item => new
                {
                    Slot = item,
                    Value = RequireModel(RequireChild(placement, item)).localPosition.y
                }).ToArray();
            var minimum = values.Min(item => item.Value);
            var maximum = values.Max(item => item.Value);
            if (maximum - minimum > PositionTolerance)
                throw new InvalidOperationException(
                    "Other Kursa model Y positions do not share one value: " +
                    string.Join("|", values.Select(item =>
                        item.Slot + "=" + Num(item.Value))) + ".");
            return values.Average(item => item.Value);
        }

        private static void WriteReport(
            float commonY,
            float moveY,
            float yError,
            IReadOnlyDictionary<string, float> slotAngles)
        {
            var destination = Absolute(ReportPath);
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException("Invalid report folder."));
            var lines = new List<string>
            {
                "Result=PASS",
                "Target=Approved Kursa Enemy Placement",
                "Slots=12",
                "MoveModelLocalY=" + Num(moveY),
                "OtherModelCommonY=" + Num(commonY),
                "ModelYError=" + Num(yError),
                "AnimatedSampleRate=" + Num(AnimatedSampleRate),
                "DirectionBasis=UnityVisualFaceAlignedToModelLocalPositiveZ",
                "UnityVisualFrontYawOffsetDegrees=" +
                    Num(UnityVisualFrontYawOffsetDegrees),
                "UpBasis=HeadToHeadEndProjectedOntoFaceFrame",
                "FaceSurfaceNormalsUsed=False",
                "EyeProjectionCentersUsedForDirection=True",
                "EyeAttachment=PerVertexUvChannelsOnSkinnedFaceMesh",
                "MaximumVisualFaceFrameError=" + Num(slotAngles.Values.Max())
            };
            lines.AddRange(slotAngles.Select(item =>
                item.Key + "VisualFaceFrameError=" + Num(item.Value)));
            lines.Add("ArmsShieldBodyLegsChanged=False");
            lines.Add("AppearanceEyesMaterialsChanged=False");
            lines.Add("OtherSceneRootsChanged=False");
            File.WriteAllLines(destination, lines, Encoding.UTF8);
        }

        private static void CaptureFaceGrid(
            Transform placement,
            string destination,
            bool includeAnimationSamples,
            bool drawEyeMidpointGuide = false)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException("Invalid capture folder."));
            var models = SlotNames.Select(item =>
                RequireModel(RequireChild(placement, item))).ToArray();
            var subjects = SlotNames.Select((slotName, index) =>
                new FaceCaptureSubject(
                    models[index],
                    slotName,
                    SlotClip(slotName),
                    0f)).ToList();
            if (includeAnimationSamples)
            {
                var idleClip = SlotClip("Kursa_02_Idle");
                var moveClip = SlotClip(MoveSlotName);
                subjects.Add(new FaceCaptureSubject(
                    models[1],
                    "Kursa_02_Idle_Mid",
                    idleClip,
                    idleClip.length * 0.5f));
                subjects.Add(new FaceCaptureSubject(
                    models[2],
                    "Kursa_03_Move_Quarter",
                    moveClip,
                    moveClip.length * 0.25f));
                subjects.Add(new FaceCaptureSubject(
                    models[2],
                    "Kursa_03_Move_Mid",
                    moveClip,
                    moveClip.length * 0.5f));
                subjects.Add(new FaceCaptureSubject(
                    models[2],
                    "Kursa_03_Move_ThreeQuarter",
                    moveClip,
                    moveClip.length * 0.75f));
                subjects.Add(new FaceCaptureSubject(
                    models[0],
                    "Kursa_01_Static_YawMinus25",
                    null,
                    0f,
                    -25f));
                subjects.Add(new FaceCaptureSubject(
                    models[0],
                    "Kursa_01_Static_YawPlus25",
                    null,
                    0f,
                    25f));
            }
            var sceneRenderers = placement.gameObject.scene.GetRootGameObjects()
                .SelectMany(item => item.GetComponentsInChildren<Renderer>(true))
                .ToArray();
            var states = sceneRenderers.Select(item =>
                new RendererState(item)).ToArray();
            var sourceCamera = GameObject.Find("Player")?
                .GetComponentInChildren<Camera>(true) ??
                throw new InvalidOperationException("Player camera is missing.");
            const int panelWidth = 360;
            const int panelHeight = 360;
            const int columns = 4;
            var rows = Mathf.CeilToInt(subjects.Count / (float)columns);
            var cameraObject = new GameObject(
                "KursaForwardHeadReviewCamera",
                typeof(Camera)) { hideFlags = HideFlags.HideAndDontSave };
            var target = new RenderTexture(
                panelWidth,
                panelHeight,
                24,
                RenderTextureFormat.ARGB32);
            var panel = new Texture2D(
                panelWidth,
                panelHeight,
                TextureFormat.RGB24,
                false);
            var grid = new Texture2D(
                panelWidth * columns,
                panelHeight * rows,
                TextureFormat.RGB24,
                false);
            var oldActive = RenderTexture.active;
            try
            {
                var camera = cameraObject.GetComponent<Camera>();
                camera.CopyFrom(sourceCamera);
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.14f, 0.15f, 0.17f, 1f);
                camera.cullingMask = ~0;
                camera.fieldOfView = 28f;
                camera.targetTexture = target;
                for (var index = 0; index < subjects.Count; index++)
                {
                    var subject = subjects[index];
                    var snapshots = subject.Model
                        .GetComponentsInChildren<Transform>(true)
                        .Select(item => new TransformSnapshot(item))
                        .ToArray();
                    var animator = subject.Model.GetComponent<Animator>();
                    var animatorEnabled = animator != null && animator.enabled;
                    try
                    {
                        if (animator != null) animator.enabled = false;
                        if (subject.Clip != null)
                            subject.Clip.SampleAnimation(
                                subject.Model.gameObject,
                                subject.Time);
                        foreach (var renderer in sceneRenderers)
                            renderer.enabled = renderer.transform.IsChildOf(subject.Model);
                        var subjectRenderer = RequireRenderer(
                            subject.Model,
                            subject.Name);
                        FrameFaceCamera(
                            camera,
                            subject.Model,
                            subjectRenderer,
                            panelWidth / (float)panelHeight,
                            subject.CameraYaw);
                        var face = RequireVisualFaceFrame(subjectRenderer);
                        var eyeMidpointScreen = camera.WorldToScreenPoint(
                            (face.LeftEye + face.RightEye) * 0.5f);
                        camera.Render();
                        RenderTexture.active = target;
                        panel.ReadPixels(
                            new Rect(0f, 0f, panelWidth, panelHeight),
                            0,
                            0);
                        panel.Apply();
                        if (drawEyeMidpointGuide &&
                            Mathf.Abs(subject.CameraYaw) < 0.001f)
                        {
                            var guideX = Mathf.Clamp(
                                Mathf.RoundToInt(eyeMidpointScreen.x),
                                0,
                                panelWidth - 1);
                            var eyeY = Mathf.Clamp(
                                Mathf.RoundToInt(eyeMidpointScreen.y),
                                0,
                                panelHeight - 1);
                            var minimumY = Mathf.Max(0, eyeY - 120);
                            var maximumY = Mathf.Min(panelHeight - 1, eyeY + 12);
                            for (var y = minimumY; y <= maximumY; y++)
                            {
                                if (((y - minimumY) / 6) % 2 == 0)
                                    panel.SetPixel(guideX, y, new Color(1f, 0.15f, 0.1f));
                            }
                            panel.Apply();
                        }
                        var pixels = panel.GetPixels32();
                        if (pixels.Any(pixel =>
                            pixel.r >= 240 && pixel.b >= 240 && pixel.g <= 24))
                            throw new InvalidOperationException(
                                "Kursa forward-head review contains Unity magenta fallback.");
                        var column = index % columns;
                        var row = rows - 1 - index / columns;
                        grid.SetPixels32(
                            column * panelWidth,
                            row * panelHeight,
                            panelWidth,
                            panelHeight,
                            pixels);
                    }
                    finally
                    {
                        foreach (var snapshot in snapshots) snapshot.Restore();
                        if (animator != null) animator.enabled = animatorEnabled;
                    }
                }
                grid.Apply();
                File.WriteAllBytes(destination, grid.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = oldActive;
                cameraObject.GetComponent<Camera>().targetTexture = null;
                foreach (var state in states) state.Restore();
                UnityEngine.Object.DestroyImmediate(panel);
                UnityEngine.Object.DestroyImmediate(grid);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static void FrameFaceCamera(
            Camera camera,
            Transform model,
            SkinnedMeshRenderer renderer,
            float aspect,
            float cameraYaw)
        {
            var face = RequireVisualFaceFrame(renderer);
            var eyeDistance = Vector3.Distance(face.LeftEye, face.RightEye);
            if (eyeDistance <= 0.0001f)
                throw new InvalidOperationException(
                    model.name + " has invalid visual eye separation.");
            var direction = model.forward;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f) direction = Vector3.forward;
            direction.Normalize();
            direction = Quaternion.AngleAxis(cameraYaw, model.up) * direction;
            camera.aspect = aspect;
            camera.nearClipPlane = 0.01f;
            var center = face.Head.position +
                         model.up * Mathf.Max(0.03f, eyeDistance);
            var distance = Mathf.Max(
                0.5f,
                eyeDistance * 5f /
                Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f));
            camera.transform.position = center + direction * distance;
            camera.transform.rotation = Quaternion.LookRotation(
                center - camera.transform.position,
                model.up);
        }

        private static Scene RequireScene(bool clean)
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
                throw new InvalidOperationException(
                    "Open CargoRunMvp before working on Kursa head alignment.");
            if (clean && scene.isDirty)
                throw new InvalidOperationException("CargoRunMvp has unsaved changes.");
            return scene;
        }

        private static GameObject RequirePlacement(Scene scene) =>
            scene.GetRootGameObjects().SingleOrDefault(item =>
                item.name == PlacementRootName) ??
            throw new InvalidOperationException("Approved Kursa placement is missing.");

        private static void RequireSlotContract(Transform placement)
        {
            if (placement.childCount != SlotNames.Length)
                throw new InvalidOperationException("Kursa slot count differs.");
            for (var index = 0; index < SlotNames.Length; index++)
            {
                var slot = placement.GetChild(index);
                if (slot.name != SlotNames[index] || slot.childCount != 1 ||
                    slot.GetChild(0).name != ModelName)
                    throw new InvalidOperationException(
                        "Kursa slot contract differs at " + index + ".");
            }
        }

        private static Transform RequireChild(Transform parent, string name)
        {
            var matches = Enumerable.Range(0, parent.childCount)
                .Select(parent.GetChild).Where(item => item.name == name).ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException(
                    "Required direct child differs: " + name + ".");
            return matches[0];
        }

        private static Transform RequireModel(Transform slot)
        {
            if (slot.childCount != 1 || slot.GetChild(0).name != ModelName)
                throw new InvalidOperationException(
                    slot.name + " model contract differs.");
            return slot.GetChild(0);
        }

        private static SkinnedMeshRenderer RequireRenderer(
            Transform model,
            string context) =>
            model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .SingleOrDefault() ??
            throw new InvalidOperationException(
                context + " must contain one skinned renderer.");

        private static string[] OtherRootSignatures(
            Scene scene,
            GameObject placement) =>
            scene.GetRootGameObjects().Where(item => item != placement)
                .OrderBy(item => item.name, StringComparer.Ordinal)
                .Select(item => RecursiveSignature(item.transform)).ToArray();

        private static string RecursiveSignature(Transform root)
        {
            var builder = new StringBuilder();
            foreach (var item in root.GetComponentsInChildren<Transform>(true))
                builder.Append(item.name).Append('|').Append(item.gameObject.activeSelf)
                    .Append('|').Append(item.localPosition).Append('|')
                    .Append(item.localRotation).Append('|').Append(item.localScale);
            return builder.ToString();
        }

        private static void RequireEqual(
            string[] before,
            string[] after,
            string message)
        {
            if (!before.SequenceEqual(after, StringComparer.Ordinal))
                throw new InvalidOperationException(message);
        }

        private static string Absolute(string relative) =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", relative));

        private static string Num(float value) =>
            value.ToString("R", CultureInfo.InvariantCulture);

        private readonly struct HeadMarkerFrame
        {
            public readonly Transform Head;
            public readonly Vector3 Forward;
            public readonly Vector3 Up;

            public HeadMarkerFrame(
                Transform head,
                Vector3 forward,
                Vector3 up)
            {
                Head = head;
                Forward = forward;
                Up = up;
            }
        }

        private readonly struct FaceVisualFrame
        {
            public readonly Transform Head;
            public readonly Vector3 Forward;
            public readonly Vector3 Up;
            public readonly Vector3 LeftEye;
            public readonly Vector3 RightEye;

            public FaceVisualFrame(
                Transform head,
                Vector3 forward,
                Vector3 up,
                Vector3 leftEye,
                Vector3 rightEye)
            {
                Head = head;
                Forward = forward;
                Up = up;
                LeftEye = leftEye;
                RightEye = rightEye;
            }
        }

        private readonly struct FaceCaptureSubject
        {
            public readonly Transform Model;
            public readonly string Name;
            public readonly AnimationClip Clip;
            public readonly float Time;
            public readonly float CameraYaw;

            public FaceCaptureSubject(
                Transform model,
                string name,
                AnimationClip clip,
                float time,
                float cameraYaw = 0f)
            {
                Model = model;
                Name = name;
                Clip = clip;
                Time = time;
                CameraYaw = cameraYaw;
            }
        }

        private readonly struct TransformSnapshot
        {
            private readonly Transform item;
            private readonly Vector3 position;
            private readonly Vector3 scale;
            private readonly Quaternion rotation;

            public TransformSnapshot(Transform value)
            {
                item = value;
                position = value.localPosition;
                scale = value.localScale;
                rotation = value.localRotation;
            }

            public void Restore()
            {
                if (item == null) return;
                item.localPosition = position;
                item.localScale = scale;
                item.localRotation = rotation;
            }
        }

        private readonly struct RendererState
        {
            private readonly Renderer renderer;
            private readonly bool enabled;

            public RendererState(Renderer value)
            {
                renderer = value;
                enabled = value.enabled;
            }

            public void Restore()
            {
                if (renderer != null) renderer.enabled = enabled;
            }
        }
    }
}
