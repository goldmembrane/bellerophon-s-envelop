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
    internal static class OstinatoAttackWristOrientation
    {
        private const string CorrectedClipPath = "Assets/_Project/Art/Enemies/Ostinato/Animations/Ostinato_04_Scissor_Attack_HorizontalInward.anim";
        private const string DerivedMeshPath = "Assets/_Project/Art/Enemies/Ostinato/Animations/Ostinato_04_Scissor_Attack_BladeWristRig.asset";
        private const string ValidationFolder = "docs/validation/ostinato_attack_wrist_orientation_2026-07-20";
        private const string ApplyReportPath = ValidationFolder + "/Ostinato_AttackWristOrientationApply.txt";
        private const string InspectionReportPath = ValidationFolder + "/Ostinato_AttackWristOrientationInspection.txt";
        private const string CaptureFolderPath = ValidationFolder + "/exact_frames";
        private const string CaptureSheetPath = ValidationFolder + "/Ostinato_AttackWristOrientationComparison.png";
        private const string CaptureReportPath = ValidationFolder + "/Ostinato_AttackWristOrientationCapture.txt";
        private const string AttackTakeNameFragment = "mixamo.com";
        private const string BindingRootName = "Armature";
        private const int ReferenceFrame = 83;
        private const int HoldFirstFrame = 78;
        private const int HoldLastFrame = 93;
        private const int BlendOutLastFrame = 99;
        private const int LastFrame = 159;
        private const float FrameRate = 60f;
        private const int ReviewLayer = 30;
        private const int PanelSize = 320;
        // Keeps the wrist seam fixed while the connected blade body reaches the post-impact angle.
        private const float BladeConnectionBlendDistance = 0.012f;
        private const float BladeWristAnchorDepth = 0.006f;

        private static readonly string[] BladeControlNames = { "LeftBladeControl", "RightBladeControl" };
        private static readonly string[] QuaternionProperties =
        {
            "m_LocalRotation.x", "m_LocalRotation.y", "m_LocalRotation.z", "m_LocalRotation.w",
        };
        private const string LeftBladeShape = "LeftBladePostImpactHorizontal";
        private const string RightBladeShape = "RightBladePostImpactHorizontal";
        private static readonly int[] CaptureFrames = { 50, 61, 67, 77, 78, 83, 93, 94, 99 };
        private static readonly string[] ApprovedMaterialPaths =
        {
            "Assets/_Project/Art/Enemies/Ostinato/ApprovedSample/Materials/Ostinato_Approved_Chitin.mat",
            "Assets/_Project/Art/Enemies/Ostinato/ApprovedSample/Materials/Ostinato_Approved_SoftTissue.mat",
            "Assets/_Project/Art/Enemies/Ostinato/ApprovedSample/Materials/Ostinato_Approved_HookBlade.mat",
            "Assets/_Project/Art/Enemies/Ostinato/ApprovedSample/Materials/Ostinato_Approved_CompoundEye.mat",
        };

        public static void ApplyOstinatoAttackHorizontalInwardWrist()
        {
            var scene = RequireOpenScene();
            var sourceClip = RequireSourceClip();
            var sourceFingerprint = BuildCurveFingerprint(sourceClip, _ => true);
            var sourceNonHandFingerprint = BuildCurveFingerprint(sourceClip, binding => !IsBladeCorrectionBinding(binding));
            var sourceHash = ComputeSha256(OstinatoScissorAttackAnimation.ProjectAbsolutePath(OstinatoScissorAttackAnimation.SourceAttackRelativePath));
            var importedHash = ComputeSha256(OstinatoScissorAttackAnimation.ProjectAbsolutePath(OstinatoScissorAttackAnimation.AttackModelPath));
            if (sourceHash != importedHash)
            {
                throw new InvalidOperationException("Ostinato source and imported FBX hashes differ before wrist-only correction.");
            }

            var meshContract = CreateDerivedBladeMesh(sourceClip, out var targetContract);
            var correctedClip = CreateCorrectedClip(sourceClip, targetContract);
            var correctedNonHandFingerprint = BuildCurveFingerprint(correctedClip, binding => !IsBladeCorrectionBinding(binding));
            if (sourceNonHandFingerprint != correctedNonHandFingerprint)
            {
                throw new InvalidOperationException("A non-hand animation curve changed while creating the corrected clip.");
            }

            var controller = ConfigureController(correctedClip);
            var sceneAnimator = RequireSceneAnimator(scene);
            var sceneRenderer = RequireRenderer(sceneAnimator.gameObject);
            RemoveBladeControlsAndRestoreBones(sceneAnimator.gameObject, sceneRenderer);
            if (sceneRenderer.sharedMesh != meshContract.DerivedMesh)
            {
                sceneRenderer.sharedMesh = meshContract.DerivedMesh;
            }
            if (sceneAnimator.runtimeAnimatorController != controller)
            {
                sceneAnimator.runtimeAnimatorController = controller;
            }
            EditorUtility.SetDirty(sceneRenderer);
            EditorUtility.SetDirty(sceneAnimator);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException("Failed to persist the Ostinato blade-control scene changes.");
            }
            AssetDatabase.SaveAssets();

            var metrics = InspectContract(sourceClip, correctedClip, targetContract);
            var report = new StringBuilder();
            report.AppendLine("SourceClip=" + AssetDatabase.GetAssetPath(sourceClip));
            report.AppendLine("CorrectedClip=" + CorrectedClipPath);
            report.AppendLine("DerivedMesh=" + DerivedMeshPath);
            report.AppendLine("Controller=" + OstinatoScissorAttackAnimation.ControllerPath);
            report.AppendLine("AnimatorState=" + OstinatoScissorAttackAnimation.StateName);
            report.AppendLine("ReferenceFrame=" + ReferenceFrame);
            report.AppendLine("PreImpactUnchangedFrames=0-77");
            report.AppendLine("PostImpactHorizontalFrames=" + HoldFirstFrame + "-" + HoldLastFrame);
            report.AppendLine("BlendOutFrames=" + (HoldLastFrame + 1) + "-" + BlendOutLastFrame);
            report.AppendLine("ChangedBindings=blendShape." + LeftBladeShape + "_078..093|blendShape." + RightBladeShape + "_078..093");
            report.AppendLine("SourceCurveFingerprint=" + sourceFingerprint);
            report.AppendLine("CorrectedCurveFingerprint=" + BuildCurveFingerprint(correctedClip, _ => true));
            report.AppendLine("SourceNonHandCurveFingerprint=" + sourceNonHandFingerprint);
            report.AppendLine("CorrectedNonHandCurveFingerprint=" + correctedNonHandFingerprint);
            report.AppendLine("NonHandCurvesUnchanged=True");
            report.AppendLine("BodyPositionAndRotationCurvesUnchanged=True");
            report.AppendLine("BladeVerticesReweighted=0");
            report.AppendLine("BladeBoundaryVerticesSplit=0");
            report.AppendLine("ConnectedBladeBodyBoundaryVertices=" + meshContract.BoundaryVertexCount);
            report.AppendLine("NonBladeWeightsUnchanged=" + meshContract.NonBladeWeightsUnchanged);
            report.AppendLine("MeshGeometryUnchanged=" + meshContract.GeometryUnchanged);
            report.AppendLine("LeftReferenceBladeHorizontalAngle=" + Format(targetContract.LeftHorizontalAngle));
            report.AppendLine("RightReferenceBladeHorizontalAngle=" + Format(targetContract.RightHorizontalAngle));
            AppendMetrics(report, metrics);
            report.AppendLine("SourceSha256=" + sourceHash);
            report.AppendLine("ImportedSha256=" + importedHash);
            report.AppendLine("SourceFbxModified=False");
            OstinatoScissorAttackAnimation.WriteText(ApplyReportPath, report.ToString());
            Debug.Log("OstinatoAttackHorizontalInwardWristApplied, PreImpactFrames=0-77, HoldFrames=78-93, NonHandCurvesUnchanged=True, " +
                      "MaxHorizontalAngle=" + Format(metrics.MaxHorizontalAngle) +
                      ", MaxWorldRotationDeviation=" + Format(metrics.MaxWorldRotationDeviation));
        }

        public static void InspectOstinatoAttackHorizontalInwardWrist()
        {
            var scene = RequireOpenScene();
            var sourceClip = RequireSourceClip();
            var correctedClip = RequireAsset<AnimationClip>(CorrectedClipPath);
            var derivedMesh = RequireAsset<Mesh>(DerivedMeshPath);
            var meshContract = InspectDerivedBladeMesh(derivedMesh);
            var sourceNonHandFingerprint = BuildCurveFingerprint(sourceClip, binding => !IsBladeCorrectionBinding(binding));
            var correctedNonHandFingerprint = BuildCurveFingerprint(correctedClip, binding => !IsBladeCorrectionBinding(binding));
            if (sourceNonHandFingerprint != correctedNonHandFingerprint)
            {
                throw new InvalidOperationException("Corrected Ostinato clip changed a non-hand animation curve.");
            }
            var targetContract = BuildTargetContract(sourceClip);
            var metrics = InspectContract(sourceClip, correctedClip, targetContract);
            var controller = RequireAsset<AnimatorController>(OstinatoScissorAttackAnimation.ControllerPath);
            var states = controller.layers.SelectMany(layer => layer.stateMachine.states)
                .Select(entry => entry.state)
                .Where(state => state.name == OstinatoScissorAttackAnimation.StateName)
                .ToArray();
            if (states.Length != 1 || states[0].motion != correctedClip || !Mathf.Approximately(states[0].speed, 1f))
            {
                throw new InvalidOperationException("Ostinato attack controller does not use the corrected clip at speed 1.");
            }
            var animator = RequireSceneAnimator(scene);
            var sceneRenderer = RequireRenderer(animator.gameObject);
            RemoveBladeControlsAndRestoreBones(animator.gameObject, sceneRenderer);
            if (animator.runtimeAnimatorController != controller || animator.applyRootMotion || sceneRenderer.sharedMesh != derivedMesh)
            {
                throw new InvalidOperationException("Ostinato scene Animator contract changed.");
            }

            var report = new StringBuilder();
            report.AppendLine("Scene=" + scene.path);
            report.AppendLine("CorrectedClip=" + CorrectedClipPath);
            report.AppendLine("DerivedMesh=" + DerivedMeshPath);
            report.AppendLine("ClipLength=" + Format(correctedClip.length));
            report.AppendLine("ClipFrameRate=" + Format(correctedClip.frameRate));
            report.AppendLine("PreImpactUnchangedFrames=0-77");
            report.AppendLine("PostImpactHorizontalFrames=78-93");
            report.AppendLine("TargetWorldOrientationReferenceFrame=83");
            report.AppendLine("CuttingEdgesFaceBodyCenterFromReferencePose=True");
            report.AppendLine("NonHandCurvesUnchanged=" + (sourceNonHandFingerprint == correctedNonHandFingerprint));
            report.AppendLine("BodyPositionAndRotationCurvesUnchanged=True");
            report.AppendLine("ExistingHandAndForeArmCurvesChanged=False");
            report.AppendLine("BladeControlsRemoved=True");
            report.AppendLine("BladeVerticesReweighted=0");
            report.AppendLine("BladeBoundaryVerticesSplit=0");
            report.AppendLine("ConnectedBladeBodyBoundaryVertices=" + meshContract.BoundaryVertexCount);
            report.AppendLine("NonBladeWeightsUnchanged=" + meshContract.NonBladeWeightsUnchanged);
            report.AppendLine("MeshGeometryUnchanged=" + meshContract.GeometryUnchanged);
            report.AppendLine("AnimatorStateSpeed=" + Format(states[0].speed));
            report.AppendLine("AnimatorApplyRootMotion=" + animator.applyRootMotion);
            AppendMetrics(report, metrics);
            report.AppendLine("SourceFbxModified=False");
            report.AppendLine("OtherSlotsTargeted=False");
            OstinatoScissorAttackAnimation.WriteText(InspectionReportPath, report.ToString());
            Debug.Log("OstinatoAttackHorizontalInwardWristInspected, PreImpactFrames=0-77, HoldFrames=78-93, NonHandCurvesUnchanged=True, " +
                      "PostImpactOnly=True, RootMotionUnchanged=True");
        }

        public static void CaptureOstinatoAttackHorizontalInwardWrist()
        {
            RequireOpenScene();
            var sourceClip = RequireSourceClip();
            var correctedClip = RequireAsset<AnimationClip>(CorrectedClipPath);
            var model = CreatePlaybackModel();
            var cameraObject = new GameObject("Ostinato_WristReviewCamera", typeof(Camera));
            var keyObject = new GameObject("Ostinato_WristKeyLight", typeof(Light));
            var fillObject = new GameObject("Ostinato_WristFillLight", typeof(Light));
            try
            {
                SetLayerRecursively(model, ReviewLayer);
                var camera = cameraObject.GetComponent<Camera>();
                ConfigureCameraAndLights(camera, keyObject.GetComponent<Light>(), fillObject.GetComponent<Light>(), keyObject.transform, fillObject.transform);
                var framesPath = OstinatoScissorAttackAnimation.ProjectAbsolutePath(CaptureFolderPath);
                Directory.CreateDirectory(framesPath);
                var captured = new List<byte[]>();
                foreach (var frame in CaptureFrames)
                {
                    var framingBounds = GetSampleBounds(model, sourceClip, frame);
                    framingBounds.Encapsulate(GetSampleBounds(model, correctedClip, frame));
                    var panels = new[]
                    {
                        RenderSample(model, sourceClip, frame, camera, Vector3.back, framingBounds),
                        RenderSample(model, correctedClip, frame, camera, Vector3.back, framingBounds),
                        RenderSample(model, sourceClip, frame, camera, new Vector3(0.7f, 0f, -1f).normalized, framingBounds),
                        RenderSample(model, correctedClip, frame, camera, new Vector3(0.7f, 0f, -1f).normalized, framingBounds),
                    };
                    var combined = CombineHorizontal(panels);
                    foreach (var panel in panels) UnityEngine.Object.DestroyImmediate(panel);
                    var bytes = combined.EncodeToPNG();
                    UnityEngine.Object.DestroyImmediate(combined);
                    File.WriteAllBytes(Path.Combine(framesPath, "frame_" + frame.ToString("D3", CultureInfo.InvariantCulture) + ".png"), bytes);
                    captured.Add(bytes);
                }
                WriteSheet(captured);
                var report = new StringBuilder();
                report.AppendLine("Panels=SourceFront|CorrectedFront|SourceThreeQuarter|CorrectedThreeQuarter");
                report.AppendLine("ExactUnityFrames=" + string.Join("|", CaptureFrames));
                report.AppendLine("ReferenceFrame=83");
                report.AppendLine("PreImpactUnchangedFrames=0-77");
                report.AppendLine("PostImpactHorizontalFrames=78-93");
                report.AppendLine("RenderPath=BakedMeshPerSample");
                report.AppendLine("FrameDirectory=" + CaptureFolderPath);
                report.AppendLine("ComparisonSheet=" + CaptureSheetPath);
                report.AppendLine("SourceFbxModified=False");
                OstinatoScissorAttackAnimation.WriteText(CaptureReportPath, report.ToString());
                Debug.Log("OstinatoAttackHorizontalInwardWristCaptured, ExactFrames=" + string.Join("|", CaptureFrames));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(model);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(keyObject);
                UnityEngine.Object.DestroyImmediate(fillObject);
            }
        }

        private static AnimationClip CreateCorrectedClip(AnimationClip sourceClip, TargetContract targetContract)
        {
            var correctedClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(CorrectedClipPath);
            if (correctedClip == null)
            {
                correctedClip = new AnimationClip();
                AssetDatabase.CreateAsset(correctedClip, CorrectedClipPath);
            }
            EditorUtility.CopySerialized(sourceClip, correctedClip);
            correctedClip.name = "Ostinato_04_Scissor_Attack_HorizontalInward";

            var model = CreatePlaybackModel(false);
            try
            {
                var renderer = RequireRenderer(model);
                var rendererPath = AnimationUtility.CalculateTransformPath(renderer.transform, model.transform);
                for (var frame = HoldFirstFrame; frame <= HoldLastFrame; frame++)
                {
                    SetBlendShapeCurve(correctedClip, rendererPath, BladeShapeName(LeftBladeShape, frame), frame);
                    SetBlendShapeCurve(correctedClip, rendererPath, BladeShapeName(RightBladeShape, frame), frame);
                }
                EditorUtility.SetDirty(correctedClip);
                AssetDatabase.SaveAssets();
                return correctedClip;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(model);
            }
        }

        private static MeshContract CreateDerivedBladeMesh(AnimationClip sourceClip, out TargetContract targetContract)
        {
            var sourceMesh = RequireRenderer(RequireAsset<GameObject>(OstinatoScissorAttackAnimation.ApprovedModelPath)).sharedMesh;
            targetContract = BuildTargetContract(sourceClip);
            var derivedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(DerivedMeshPath);
            if (derivedMesh == null)
            {
                derivedMesh = new Mesh();
                derivedMesh.name = "Ostinato_04_Scissor_Attack_BladeWristRig";
                AssetDatabase.CreateAsset(derivedMesh, DerivedMeshPath);
            }
            WriteBoundarySeparatedMesh(sourceMesh, derivedMesh, Array.Empty<int>(), sourceMesh.boneWeights,
                sourceMesh.GetIndices(2), sourceMesh.bindposes);
            derivedMesh.name = "Ostinato_04_Scissor_Attack_BladeWristRig";
            var zeroNormals = new Vector3[sourceMesh.vertexCount];
            var zeroTangents = new Vector3[sourceMesh.vertexCount];
            for (var frame = HoldFirstFrame; frame <= HoldLastFrame; frame++)
            {
                BuildConnectedBladeDeltas(sourceMesh, sourceClip, frame, out var leftDeltas, out var rightDeltas);
                derivedMesh.AddBlendShapeFrame(BladeShapeName(LeftBladeShape, frame), 100f,
                    leftDeltas, zeroNormals, zeroTangents);
                derivedMesh.AddBlendShapeFrame(BladeShapeName(RightBladeShape, frame), 100f,
                    rightDeltas, zeroNormals, zeroTangents);
            }
            EditorUtility.SetDirty(derivedMesh);
            AssetDatabase.SaveAssets();
            return InspectDerivedBladeMesh(derivedMesh);
        }

        private static MeshContract InspectDerivedBladeMesh(Mesh derivedMesh)
        {
            var asset = RequireAsset<GameObject>(OstinatoScissorAttackAnimation.ApprovedModelPath);
            var sourceRenderer = RequireRenderer(asset);
            var sourceMesh = sourceRenderer.sharedMesh;
            var sourceWeights = sourceMesh.boneWeights;
            var derivedWeights = derivedMesh.boneWeights;
            var sourceBladeVertices = sourceMesh.GetIndices(2).Distinct().ToHashSet();
            var sourceBodyVertices = Enumerable.Range(0, sourceMesh.subMeshCount)
                .Where(subMesh => subMesh != 2)
                .SelectMany(subMesh => sourceMesh.GetIndices(subMesh))
                .ToHashSet();
            var sharedBoundaryCount = sourceBladeVertices.Count(sourceBodyVertices.Contains);
            var weightsUnchanged = derivedWeights.Length == sourceWeights.Length &&
                                   Enumerable.Range(0, sourceWeights.Length)
                                       .All(index => BoneWeightEquals(sourceWeights[index], derivedWeights[index]));
            var geometryUnchanged = BuildGeometryFingerprint(sourceMesh) == BuildGeometryFingerprint(derivedMesh);
            if (!weightsUnchanged || !geometryUnchanged || derivedMesh.vertexCount != sourceMesh.vertexCount)
                throw new InvalidOperationException("Connected post-impact mesh changed source topology, weights, or base geometry.");
            var sharedVertices = sourceBladeVertices.Where(sourceBodyVertices.Contains).ToArray();
            var correctionShapeIndices = Enumerable.Range(HoldFirstFrame, HoldLastFrame - HoldFirstFrame + 1)
                .SelectMany(frame => new[]
                {
                    derivedMesh.GetBlendShapeIndex(BladeShapeName(LeftBladeShape, frame)),
                    derivedMesh.GetBlendShapeIndex(BladeShapeName(RightBladeShape, frame)),
                }).ToArray();
            if (correctionShapeIndices.Any(index => index < 0))
                throw new InvalidOperationException("One or more frame-specific connected blade BlendShapes are missing.");
            foreach (var shapeIndex in correctionShapeIndices)
            {
                var deltas = new Vector3[derivedMesh.vertexCount];
                var normalDeltas = new Vector3[derivedMesh.vertexCount];
                var tangentDeltas = new Vector3[derivedMesh.vertexCount];
                derivedMesh.GetBlendShapeFrameVertices(shapeIndex, 0, deltas, normalDeltas, tangentDeltas);
                if (!sharedVertices.Any(index => deltas[index].sqrMagnitude <= 0.0000000001f))
                    throw new InvalidOperationException("A connected blade BlendShape has no fixed blade/wrist anchor vertex.");
            }
            return new MeshContract(derivedMesh, sourceBladeVertices.Count, sharedBoundaryCount,
                sharedBoundaryCount, weightsUnchanged, geometryUnchanged);
        }

        private static float SumSideWeight(BoneWeight weight, IReadOnlyList<string> boneNames, string sidePrefix)
        {
            var result = 0f;
            if (weight.weight0 > 0f && boneNames[weight.boneIndex0].StartsWith(sidePrefix, StringComparison.Ordinal)) result += weight.weight0;
            if (weight.weight1 > 0f && boneNames[weight.boneIndex1].StartsWith(sidePrefix, StringComparison.Ordinal)) result += weight.weight1;
            if (weight.weight2 > 0f && boneNames[weight.boneIndex2].StartsWith(sidePrefix, StringComparison.Ordinal)) result += weight.weight2;
            if (weight.weight3 > 0f && boneNames[weight.boneIndex3].StartsWith(sidePrefix, StringComparison.Ordinal)) result += weight.weight3;
            return result;
        }

        private static void BuildConnectedBladeDeltas(Mesh sourceMesh, AnimationClip sourceClip, int frame,
            out Vector3[] leftDeltas, out Vector3[] rightDeltas)
        {
            var model = CreatePlaybackModel(false);
            try
            {
                var renderer = RequireRenderer(model);
                renderer.sharedMesh = sourceMesh;
                Sample(model, sourceClip, frame);
                var baked = new Mesh();
                try
                {
                    renderer.BakeMesh(baked);
                    var sourceVertices = sourceMesh.vertices;
                    var bakedVertices = baked.vertices;
                    var bladeVertices = sourceMesh.GetIndices(2).Distinct().ToArray();
                    var bodyVertices = Enumerable.Range(0, sourceMesh.subMeshCount)
                        .Where(subMesh => subMesh != 2)
                        .SelectMany(subMesh => sourceMesh.GetIndices(subMesh))
                        .ToHashSet();
                    var boundaryVertices = bladeVertices.Where(bodyVertices.Contains).ToHashSet();
                    var leftHand = FindDescendant(model.transform, "LeftHand");
                    var rightHand = FindDescendant(model.transform, "RightHand");
                    var boneNames = renderer.bones.Select(bone => bone.name).ToArray();
                    var sourceWeights = sourceMesh.boneWeights;
                    var leftVertices = bladeVertices.Where(index =>
                        SumSideWeight(sourceWeights[index], boneNames, "Left") >
                        SumSideWeight(sourceWeights[index], boneNames, "Right")).ToArray();
                    var rightVertices = bladeVertices.Where(index =>
                        SumSideWeight(sourceWeights[index], boneNames, "Right") >
                        SumSideWeight(sourceWeights[index], boneNames, "Left")).ToArray();
                    var leftSharedBoundary = leftVertices.Where(boundaryVertices.Contains).ToArray();
                    var rightSharedBoundary = rightVertices.Where(boundaryVertices.Contains).ToArray();
                    if (leftSharedBoundary.Length == 0 || rightSharedBoundary.Length == 0)
                        throw new InvalidOperationException("A connected blade side has no fixed wrist boundary.");
                    var leftMinimumDistance = leftSharedBoundary.Min(index =>
                        Vector3.Distance(renderer.transform.TransformPoint(bakedVertices[index]), leftHand.position));
                    var rightMinimumDistance = rightSharedBoundary.Min(index =>
                        Vector3.Distance(renderer.transform.TransformPoint(bakedVertices[index]), rightHand.position));
                    var leftAnchors = leftVertices.Where(index =>
                        Vector3.Distance(renderer.transform.TransformPoint(bakedVertices[index]), leftHand.position) <=
                        leftMinimumDistance + BladeWristAnchorDepth).ToArray();
                    var rightAnchors = rightVertices.Where(index =>
                        Vector3.Distance(renderer.transform.TransformPoint(bakedVertices[index]), rightHand.position) <=
                        rightMinimumDistance + BladeWristAnchorDepth).ToArray();
                    var leftAxis = PrincipalAxis(leftVertices.Select(index =>
                        renderer.transform.TransformPoint(bakedVertices[index])).ToArray());
                    var rightAxis = PrincipalAxis(rightVertices.Select(index =>
                        renderer.transform.TransformPoint(bakedVertices[index])).ToArray());
                    var leftHorizontal = Vector3.ProjectOnPlane(leftAxis, Vector3.up).normalized;
                    var rightHorizontal = Vector3.ProjectOnPlane(rightAxis, Vector3.up).normalized;
                    var leftCorrection = Quaternion.FromToRotation(leftAxis, leftHorizontal);
                    var rightCorrection = Quaternion.FromToRotation(rightAxis, rightHorizontal);

                    leftDeltas = new Vector3[sourceMesh.vertexCount];
                    rightDeltas = new Vector3[sourceMesh.vertexCount];
                    BuildSideDeltas(renderer, sourceMesh, sourceVertices, bakedVertices, leftVertices, leftAnchors,
                        leftHand.position, leftCorrection, leftDeltas);
                    BuildSideDeltas(renderer, sourceMesh, sourceVertices, bakedVertices, rightVertices, rightAnchors,
                        rightHand.position, rightCorrection, rightDeltas);
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(baked);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(model);
            }
        }

        private static void BuildSideDeltas(SkinnedMeshRenderer renderer, Mesh sourceMesh,
            IReadOnlyList<Vector3> sourceVertices, IReadOnlyList<Vector3> bakedVertices,
            IReadOnlyList<int> sideVertices, IReadOnlyList<int> boundaryVertices, Vector3 wristWorldPosition,
            Quaternion worldCorrection, Vector3[] deltas)
        {
            var influenceByVertex = new Dictionary<int, float>(sideVertices.Count);
            foreach (var vertexIndex in sideVertices)
            {
                if (boundaryVertices.Contains(vertexIndex))
                {
                    influenceByVertex[vertexIndex] = 0f;
                    continue;
                }
                var distance = boundaryVertices.Min(boundaryIndex =>
                    Vector3.Distance(sourceVertices[vertexIndex], sourceVertices[boundaryIndex]));
                influenceByVertex[vertexIndex] = Mathf.SmoothStep(0f, 1f,
                    Mathf.Clamp01(distance / BladeConnectionBlendDistance));
            }
            var skinByVertex = new Dictionary<int, Matrix4x4>(sideVertices.Count);
            var inverseByVertex = new Dictionary<int, Matrix4x4>(sideVertices.Count);
            foreach (var vertexIndex in sideVertices)
            {
                var skinMatrix = BuildSkinMatrix(renderer, sourceMesh, sourceMesh.boneWeights[vertexIndex]);
                var reconstructionError = Vector3.Distance(
                    skinMatrix.MultiplyPoint3x4(sourceVertices[vertexIndex]), bakedVertices[vertexIndex]);
                if (reconstructionError > 0.0001f)
                    throw new InvalidOperationException("Ostinato skin matrix reconstruction differs from BakeMesh. Error=" +
                                                        Format(reconstructionError) + ", Vertex=" + vertexIndex);
                skinByVertex[vertexIndex] = skinMatrix;
                inverseByVertex[vertexIndex] = BuildDampedSkinInverse(skinMatrix);
            }

            worldCorrection.ToAngleAxis(out var correctionAngle, out var correctionAxis);
            var optimizedCorrection = worldCorrection;
            var bestHorizontalAngle = float.MaxValue;
            for (var step = 0; step <= 180; step++)
            {
                var candidate = Quaternion.AngleAxis(correctionAngle * (step / 60f), correctionAxis);
                var candidatePoints = sideVertices.Select(vertexIndex =>
                {
                    var sourceWorld = renderer.transform.TransformPoint(bakedVertices[vertexIndex]);
                    var rotatedWorld = wristWorldPosition + candidate * (sourceWorld - wristWorldPosition);
                    var targetLocal = renderer.transform.InverseTransformPoint(
                        Vector3.Lerp(sourceWorld, rotatedWorld, influenceByVertex[vertexIndex]));
                    var restDelta = inverseByVertex[vertexIndex].MultiplyVector(targetLocal - bakedVertices[vertexIndex]);
                    var actualLocal = bakedVertices[vertexIndex] + skinByVertex[vertexIndex].MultiplyVector(restDelta);
                    return renderer.transform.TransformPoint(actualLocal);
                }).ToArray();
                var horizontalAngle = AngleFromHorizontal(PrincipalAxis(candidatePoints));
                if (horizontalAngle >= bestHorizontalAngle) continue;
                bestHorizontalAngle = horizontalAngle;
                optimizedCorrection = candidate;
            }

            foreach (var vertexIndex in sideVertices)
            {
                var influence = influenceByVertex[vertexIndex];
                if (influence <= 0f) continue;
                var sourceWorld = renderer.transform.TransformPoint(bakedVertices[vertexIndex]);
                var rotatedWorld = wristWorldPosition + optimizedCorrection * (sourceWorld - wristWorldPosition);
                var targetLocal = renderer.transform.InverseTransformPoint(Vector3.Lerp(sourceWorld, rotatedWorld, influence));
                deltas[vertexIndex] = inverseByVertex[vertexIndex].MultiplyVector(targetLocal - bakedVertices[vertexIndex]);
            }
        }

        private static Matrix4x4 BuildDampedSkinInverse(Matrix4x4 skinMatrix)
        {
            var transpose = Matrix4x4.identity;
            var normal = Matrix4x4.identity;
            for (var row = 0; row < 3; row++)
                for (var column = 0; column < 3; column++)
                    transpose[row, column] = skinMatrix[column, row];
            for (var row = 0; row < 3; row++)
            {
                for (var column = 0; column < 3; column++)
                {
                    var value = 0f;
                    for (var axis = 0; axis < 3; axis++)
                        value += transpose[row, axis] * skinMatrix[axis, column];
                    normal[row, column] = value + (row == column ? 0.0001f : 0f);
                }
            }
            return normal.inverse * transpose;
        }

        private static Matrix4x4 BuildSkinMatrix(SkinnedMeshRenderer renderer, Mesh mesh, BoneWeight weight)
        {
            var result = new Matrix4x4();
            AddBoneMatrix(ref result, renderer, mesh, weight.boneIndex0, weight.weight0);
            AddBoneMatrix(ref result, renderer, mesh, weight.boneIndex1, weight.weight1);
            AddBoneMatrix(ref result, renderer, mesh, weight.boneIndex2, weight.weight2);
            AddBoneMatrix(ref result, renderer, mesh, weight.boneIndex3, weight.weight3);
            return result;
        }

        private static void AddBoneMatrix(ref Matrix4x4 result, SkinnedMeshRenderer renderer, Mesh mesh,
            int boneIndex, float weight)
        {
            if (weight <= 0f) return;
            var value = renderer.transform.worldToLocalMatrix * renderer.bones[boneIndex].localToWorldMatrix *
                        mesh.bindposes[boneIndex];
            for (var row = 0; row < 4; row++)
                for (var column = 0; column < 4; column++)
                    result[row, column] += value[row, column] * weight;
        }

        private static bool BoneWeightEquals(BoneWeight left, BoneWeight right)
        {
            return left.boneIndex0 == right.boneIndex0 && left.boneIndex1 == right.boneIndex1 &&
                   left.boneIndex2 == right.boneIndex2 && left.boneIndex3 == right.boneIndex3 &&
                   left.weight0.Equals(right.weight0) && left.weight1.Equals(right.weight1) &&
                   left.weight2.Equals(right.weight2) && left.weight3.Equals(right.weight3);
        }

        private static T[] DuplicateVertexData<T>(T[] source, IReadOnlyList<int> duplicateSourceIndices,
            int expectedVertexCount)
        {
            if (source.Length == 0) return source;
            if (source.Length != expectedVertexCount)
                throw new InvalidOperationException("A source mesh vertex channel has an unexpected length.");
            var result = new T[source.Length + duplicateSourceIndices.Count];
            Array.Copy(source, result, source.Length);
            for (var offset = 0; offset < duplicateSourceIndices.Count; offset++)
                result[source.Length + offset] = source[duplicateSourceIndices[offset]];
            return result;
        }

        private static void WriteBoundarySeparatedMesh(Mesh source, Mesh destination,
            IReadOnlyList<int> duplicateSourceIndices, BoneWeight[] correctedWeights, int[] derivedBladeIndices,
            Matrix4x4[] bindposes)
        {
            var sourceVertexCount = source.vertexCount;
            var destinationVertexCount = sourceVertexCount + duplicateSourceIndices.Count;
            destination.Clear();
            destination.name = "Ostinato_04_Scissor_Attack_BladeWristRig";
            destination.indexFormat = destinationVertexCount > ushort.MaxValue
                ? UnityEngine.Rendering.IndexFormat.UInt32
                : source.indexFormat;
            destination.vertices = DuplicateVertexData(source.vertices, duplicateSourceIndices, sourceVertexCount);

            var normals = DuplicateVertexData(source.normals, duplicateSourceIndices, sourceVertexCount);
            if (normals.Length > 0) destination.normals = normals;
            var tangents = DuplicateVertexData(source.tangents, duplicateSourceIndices, sourceVertexCount);
            if (tangents.Length > 0) destination.tangents = tangents;
            var colors = DuplicateVertexData(source.colors32, duplicateSourceIndices, sourceVertexCount);
            if (colors.Length > 0) destination.colors32 = colors;
            for (var channel = 0; channel < 8; channel++)
            {
                var sourceUv = new List<Vector4>();
                source.GetUVs(channel, sourceUv);
                if (sourceUv.Count == 0) continue;
                var destinationUv = DuplicateVertexData(sourceUv.ToArray(), duplicateSourceIndices, sourceVertexCount);
                destination.SetUVs(channel, destinationUv);
            }

            destination.boneWeights = correctedWeights;
            destination.bindposes = bindposes;
            destination.subMeshCount = source.subMeshCount;
            for (var subMesh = 0; subMesh < source.subMeshCount; subMesh++)
            {
                var indices = subMesh == 2 ? derivedBladeIndices : source.GetIndices(subMesh);
                destination.SetIndices(indices, source.GetTopology(subMesh), subMesh, false);
            }

            for (var shapeIndex = 0; shapeIndex < source.blendShapeCount; shapeIndex++)
            {
                var shapeName = source.GetBlendShapeName(shapeIndex);
                for (var frameIndex = 0; frameIndex < source.GetBlendShapeFrameCount(shapeIndex); frameIndex++)
                {
                    var deltaVertices = new Vector3[sourceVertexCount];
                    var deltaNormals = new Vector3[sourceVertexCount];
                    var deltaTangents = new Vector3[sourceVertexCount];
                    source.GetBlendShapeFrameVertices(shapeIndex, frameIndex, deltaVertices, deltaNormals, deltaTangents);
                    destination.AddBlendShapeFrame(shapeName, source.GetBlendShapeFrameWeight(shapeIndex, frameIndex),
                        DuplicateVertexData(deltaVertices, duplicateSourceIndices, sourceVertexCount),
                        DuplicateVertexData(deltaNormals, duplicateSourceIndices, sourceVertexCount),
                        DuplicateVertexData(deltaTangents, duplicateSourceIndices, sourceVertexCount));
                }
            }
            destination.bounds = source.bounds;
        }

        private static bool VisualGeometryMatches(Mesh source, Mesh derived)
        {
            if (source.subMeshCount != derived.subMeshCount) return false;
            var sourceVertices = source.vertices;
            var derivedVertices = derived.vertices;
            var sourceNormals = source.normals;
            var derivedNormals = derived.normals;
            var sourceTangents = source.tangents;
            var derivedTangents = derived.tangents;
            var sourceColors = source.colors32;
            var derivedColors = derived.colors32;
            var sourceUv = new List<Vector4>[8];
            var derivedUv = new List<Vector4>[8];
            for (var channel = 0; channel < 8; channel++)
            {
                sourceUv[channel] = new List<Vector4>();
                derivedUv[channel] = new List<Vector4>();
                source.GetUVs(channel, sourceUv[channel]);
                derived.GetUVs(channel, derivedUv[channel]);
                if (sourceUv[channel].Count != 0 && derivedUv[channel].Count == 0) return false;
            }

            for (var subMesh = 0; subMesh < source.subMeshCount; subMesh++)
            {
                if (source.GetTopology(subMesh) != derived.GetTopology(subMesh)) return false;
                var sourceIndices = source.GetIndices(subMesh);
                var derivedIndices = derived.GetIndices(subMesh);
                if (sourceIndices.Length != derivedIndices.Length) return false;
                for (var offset = 0; offset < sourceIndices.Length; offset++)
                {
                    var sourceIndex = sourceIndices[offset];
                    var derivedIndex = derivedIndices[offset];
                    if (sourceVertices[sourceIndex] != derivedVertices[derivedIndex]) return false;
                    if (sourceNormals.Length > 0 && sourceNormals[sourceIndex] != derivedNormals[derivedIndex]) return false;
                    if (sourceTangents.Length > 0 && sourceTangents[sourceIndex] != derivedTangents[derivedIndex]) return false;
                    if (sourceColors.Length > 0 && !sourceColors[sourceIndex].Equals(derivedColors[derivedIndex])) return false;
                    for (var channel = 0; channel < 8; channel++)
                        if (sourceUv[channel].Count > 0 && sourceUv[channel][sourceIndex] != derivedUv[channel][derivedIndex])
                            return false;
                }
            }
            return true;
        }

        private static string BuildGeometryFingerprint(Mesh mesh)
        {
            using var stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
            {
                writer.Write(mesh.vertexCount); writer.Write(mesh.subMeshCount);
                foreach (var value in mesh.vertices) { writer.Write(value.x); writer.Write(value.y); writer.Write(value.z); }
                foreach (var value in mesh.normals) { writer.Write(value.x); writer.Write(value.y); writer.Write(value.z); }
                foreach (var value in mesh.uv) { writer.Write(value.x); writer.Write(value.y); }
                for (var subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
                {
                    writer.Write((int)mesh.GetTopology(subMesh));
                    foreach (var index in mesh.GetIndices(subMesh)) writer.Write(index);
                }
            }
            stream.Position = 0; using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
        }

        private static TargetContract BuildTargetContract(AnimationClip sourceClip)
        {
            var model = CreatePlaybackModel(false);
            try
            {
                Sample(model, sourceClip, ReferenceFrame);
                var renderer = RequireRenderer(model);
                var leftHand = FindDescendant(model.transform, "LeftHand");
                var rightHand = FindDescendant(model.transform, "RightHand");
                var axes = MeasureBladeAxes(renderer, leftHand, rightHand);
                var leftHorizontal = Vector3.ProjectOnPlane(axes.LeftAxis, Vector3.up).normalized;
                var rightHorizontal = Vector3.ProjectOnPlane(axes.RightAxis, Vector3.up).normalized;
                if (leftHorizontal.sqrMagnitude < 0.9f || rightHorizontal.sqrMagnitude < 0.9f)
                {
                    throw new InvalidOperationException("A reference blade axis cannot be projected onto the ground plane.");
                }
                var leftCorrection = Quaternion.FromToRotation(axes.LeftAxis, leftHorizontal);
                var rightCorrection = Quaternion.FromToRotation(axes.RightAxis, rightHorizontal);
                return new TargetContract(
                    leftCorrection,
                    rightCorrection,
                    AngleFromHorizontal(leftHorizontal),
                    AngleFromHorizontal(rightHorizontal));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(model);
            }
        }

        private static InspectionMetrics InspectContract(AnimationClip sourceClip, AnimationClip correctedClip, TargetContract targetContract)
        {
            if (Mathf.Abs(sourceClip.length - correctedClip.length) > 0.0001f ||
                Mathf.Abs(sourceClip.frameRate - correctedClip.frameRate) > 0.001f)
            {
                throw new InvalidOperationException("Corrected clip timing differs from the source clip.");
            }
            var sourceModel = CreatePlaybackModel(false);
            var correctedModel = CreatePlaybackModel();
            try
            {
                var sourceLeftHand = FindDescendant(sourceModel.transform, "LeftHand");
                var sourceRightHand = FindDescendant(sourceModel.transform, "RightHand");
                var correctedLeftHand = FindDescendant(correctedModel.transform, "LeftHand");
                var correctedRightHand = FindDescendant(correctedModel.transform, "RightHand");
                var sourceRenderer = RequireRenderer(sourceModel);
                var correctedRenderer = RequireRenderer(correctedModel);
                var leftShapeIndices = Enumerable.Range(HoldFirstFrame, HoldLastFrame - HoldFirstFrame + 1)
                    .Select(frame => correctedRenderer.sharedMesh.GetBlendShapeIndex(BladeShapeName(LeftBladeShape, frame)))
                    .ToArray();
                var rightShapeIndices = Enumerable.Range(HoldFirstFrame, HoldLastFrame - HoldFirstFrame + 1)
                    .Select(frame => correctedRenderer.sharedMesh.GetBlendShapeIndex(BladeShapeName(RightBladeShape, frame)))
                    .ToArray();
                if (leftShapeIndices.Any(index => index < 0) || rightShapeIndices.Any(index => index < 0))
                    throw new InvalidOperationException("Frame-specific connected blade BlendShapes are missing during inspection.");
                var maxPreImpactVertexDeviation = 0f;
                var maxHorizontalAngle = 0f;
                var maxShapeWeight = 0f;
                var maxShapeWeightStep = 0f;
                var maxPostImpactVertexDeviation = 0f;
                var maxPostImpactBoundsRatio = 0f;
                float? previousLeftWeight = null;
                float? previousRightWeight = null;
                var maxHandDeviation = 0f;
                var sourceBaked = new Mesh();
                var correctedBaked = new Mesh();
                for (var frame = 0; frame <= LastFrame; frame++)
                {
                    Sample(sourceModel, sourceClip, frame);
                    Sample(correctedModel, correctedClip, frame);
                    var leftWeight = leftShapeIndices.Sum(index => correctedRenderer.GetBlendShapeWeight(index));
                    var rightWeight = rightShapeIndices.Sum(index => correctedRenderer.GetBlendShapeWeight(index));
                    maxShapeWeight = Mathf.Max(maxShapeWeight, leftWeight, rightWeight);
                    maxHandDeviation = Mathf.Max(maxHandDeviation,
                        Quaternion.Angle(sourceLeftHand.localRotation, correctedLeftHand.localRotation),
                        Quaternion.Angle(sourceRightHand.localRotation, correctedRightHand.localRotation));
                    if (previousLeftWeight.HasValue)
                        maxShapeWeightStep = Mathf.Max(maxShapeWeightStep,
                            Mathf.Abs(leftWeight - previousLeftWeight.Value), Mathf.Abs(rightWeight - previousRightWeight.Value));
                    previousLeftWeight = leftWeight;
                    previousRightWeight = rightWeight;
                    if (frame <= 77)
                    {
                        sourceRenderer.BakeMesh(sourceBaked);
                        correctedRenderer.BakeMesh(correctedBaked);
                        if (sourceBaked.vertexCount != correctedBaked.vertexCount)
                            throw new InvalidOperationException("Pre-impact source/corrected baked mesh vertex counts differ.");
                        var sourceVertices = sourceBaked.vertices;
                        var correctedVertices = correctedBaked.vertices;
                        for (var vertex = 0; vertex < sourceVertices.Length; vertex++)
                            maxPreImpactVertexDeviation = Mathf.Max(maxPreImpactVertexDeviation,
                                Vector3.Distance(sourceVertices[vertex], correctedVertices[vertex]));
                    }
                    if (frame >= HoldFirstFrame && frame <= HoldLastFrame)
                    {
                        sourceRenderer.BakeMesh(sourceBaked);
                        correctedRenderer.BakeMesh(correctedBaked);
                        var sourceVertices = sourceBaked.vertices;
                        var correctedVertices = correctedBaked.vertices;
                        for (var vertex = 0; vertex < sourceVertices.Length; vertex++)
                            maxPostImpactVertexDeviation = Mathf.Max(maxPostImpactVertexDeviation,
                                Vector3.Distance(sourceVertices[vertex], correctedVertices[vertex]));
                        maxPostImpactBoundsRatio = Mathf.Max(maxPostImpactBoundsRatio,
                            correctedBaked.bounds.size.magnitude / sourceBaked.bounds.size.magnitude);
                        var axes = MeasureBladeAxes(correctedRenderer, correctedLeftHand, correctedRightHand);
                        maxHorizontalAngle = Mathf.Max(maxHorizontalAngle,
                            AngleFromHorizontal(axes.LeftAxis), AngleFromHorizontal(axes.RightAxis));
                    }
                }
                UnityEngine.Object.DestroyImmediate(sourceBaked);
                UnityEngine.Object.DestroyImmediate(correctedBaked);
                if (maxPreImpactVertexDeviation > 0.00001f)
                    throw new InvalidOperationException("A pre-impact corrected vertex differs from the source. MaxDeviation=" +
                                                        Format(maxPreImpactVertexDeviation));
                if (maxHorizontalAngle > 2f)
                    throw new InvalidOperationException("A post-impact blade is not horizontal. MaxAngle=" +
                                                        Format(maxHorizontalAngle));
                if (maxHandDeviation > 0.0001f)
                    throw new InvalidOperationException("An existing hand rotation changed. MaxDeviation=" +
                                                        Format(maxHandDeviation));
                if (maxPostImpactVertexDeviation > 2f || maxPostImpactBoundsRatio > 2.5f)
                    throw new InvalidOperationException("Post-impact connected blade deformation exceeds the visual bounds contract. " +
                                                        "MaxVertexDeviation=" + Format(maxPostImpactVertexDeviation) +
                                                        ", MaxBoundsRatio=" + Format(maxPostImpactBoundsRatio));
                return new InspectionMetrics(maxPreImpactVertexDeviation, maxHorizontalAngle,
                    maxShapeWeight, maxShapeWeightStep, maxHandDeviation,
                    maxPostImpactVertexDeviation, maxPostImpactBoundsRatio);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sourceModel);
                UnityEngine.Object.DestroyImmediate(correctedModel);
            }
        }

        private static AnimatorController ConfigureController(AnimationClip correctedClip)
        {
            var controller = RequireAsset<AnimatorController>(OstinatoScissorAttackAnimation.ControllerPath);
            var stateMachine = controller.layers[0].stateMachine;
            foreach (var state in stateMachine.states.Select(entry => entry.state).ToArray()) stateMachine.RemoveState(state);
            var attackState = stateMachine.AddState(OstinatoScissorAttackAnimation.StateName);
            attackState.motion = correctedClip;
            attackState.speed = 1f;
            attackState.writeDefaultValues = true;
            stateMachine.defaultState = attackState;
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static float CorrectionWeight(int frame)
        {
            if (frame < HoldFirstFrame || frame > BlendOutLastFrame) return 0f;
            if (frame <= HoldLastFrame) return 1f;
            return Mathf.SmoothStep(1f, 0f, (frame - HoldLastFrame) / 6f);
        }

        private static string BladeShapeName(string baseName, int frame) => baseName + "_" + frame.ToString("000");

        private static void SetBlendShapeCurve(AnimationClip clip, string rendererPath, string shapeName, int targetFrame)
        {
            var keys = targetFrame == HoldLastFrame
                ? new[]
                {
                    new Keyframe(0f, 0f),
                    new Keyframe((targetFrame - 1) / FrameRate, 0f),
                    new Keyframe(targetFrame / FrameRate, 100f),
                    new Keyframe(BlendOutLastFrame / FrameRate, 0f),
                    new Keyframe(LastFrame / FrameRate, 0f),
                }
                : new[]
                {
                    new Keyframe(0f, 0f),
                    new Keyframe((targetFrame - 1) / FrameRate, 0f),
                    new Keyframe(targetFrame / FrameRate, 100f),
                    new Keyframe((targetFrame + 1) / FrameRate, 0f),
                    new Keyframe(LastFrame / FrameRate, 0f),
                };
            var curve = new AnimationCurve(keys) { preWrapMode = WrapMode.ClampForever, postWrapMode = WrapMode.ClampForever };
            for (var key = 0; key < curve.length; key++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, key, AnimationUtility.TangentMode.Linear);
                AnimationUtility.SetKeyRightTangentMode(curve, key, AnimationUtility.TangentMode.Linear);
            }
            AnimationUtility.SetEditorCurve(clip,
                EditorCurveBinding.FloatCurve(rendererPath, typeof(SkinnedMeshRenderer), "blendShape." + shapeName), curve);
        }

        private static void SetQuaternionCurves(AnimationClip clip, string path, IReadOnlyList<Quaternion> rotations)
        {
            var values = new Func<Quaternion, float>[] { q => q.x, q => q.y, q => q.z, q => q.w };
            for (var component = 0; component < QuaternionProperties.Length; component++)
            {
                var keys = new Keyframe[rotations.Count];
                for (var frame = 0; frame < rotations.Count; frame++)
                    keys[frame] = new Keyframe(frame / FrameRate, values[component](rotations[frame]));
                var curve = new AnimationCurve(keys) { preWrapMode = WrapMode.ClampForever, postWrapMode = WrapMode.ClampForever };
                for (var key = 0; key < curve.length; key++)
                {
                    AnimationUtility.SetKeyLeftTangentMode(curve, key, AnimationUtility.TangentMode.Linear);
                    AnimationUtility.SetKeyRightTangentMode(curve, key, AnimationUtility.TangentMode.Linear);
                }
                AnimationUtility.SetEditorCurve(clip,
                    EditorCurveBinding.FloatCurve(path, typeof(Transform), QuaternionProperties[component]), curve);
            }
        }

        private static void EnsureQuaternionSigns(Quaternion[] rotations)
        {
            for (var index = 1; index < rotations.Length; index++)
            {
                if (Quaternion.Dot(rotations[index - 1], rotations[index]) >= 0f) continue;
                var current = rotations[index];
                rotations[index] = new Quaternion(-current.x, -current.y, -current.z, -current.w);
            }
        }

        private static string RequireHandPath(AnimationClip clip, string handName)
        {
            var allBindings = AnimationUtility.GetCurveBindings(clip);
            var bindings = allBindings
                .Where(binding => binding.path.EndsWith("/" + handName, StringComparison.Ordinal) &&
                                  QuaternionProperties.Contains(binding.propertyName))
                .ToArray();
            if (bindings.Length != 4 || bindings.Select(binding => binding.path).Distinct().Count() != 1)
            {
                throw new InvalidOperationException("Expected four quaternion curves for " + handName + ". Found=" +
                    string.Join("|", bindings.Select(binding => binding.path + ":" + binding.propertyName)) +
                    ", AllHandBindings=" + string.Join("|", allBindings
                        .Where(binding => binding.path.IndexOf(handName, StringComparison.OrdinalIgnoreCase) >= 0)
                        .Select(binding => binding.path + ":" + binding.propertyName)));
            }
            return bindings[0].path;
        }

        private static bool IsBladeCorrectionBinding(EditorCurveBinding binding)
        {
            return binding.type == typeof(SkinnedMeshRenderer) &&
                   (binding.propertyName.StartsWith("blendShape." + LeftBladeShape + "_", StringComparison.Ordinal) ||
                    binding.propertyName.StartsWith("blendShape." + RightBladeShape + "_", StringComparison.Ordinal));
        }

        private static string BuildCurveFingerprint(AnimationClip clip, Func<EditorCurveBinding, bool> include)
        {
            using var stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
            {
                writer.Write(clip.length);
                writer.Write(clip.frameRate);
                var bindings = AnimationUtility.GetCurveBindings(clip).Where(include)
                    .OrderBy(binding => binding.path, StringComparer.Ordinal)
                    .ThenBy(binding => binding.type.FullName, StringComparer.Ordinal)
                    .ThenBy(binding => binding.propertyName, StringComparer.Ordinal).ToArray();
                writer.Write(bindings.Length);
                foreach (var binding in bindings)
                {
                    writer.Write(binding.path ?? string.Empty);
                    writer.Write(binding.type.FullName ?? string.Empty);
                    writer.Write(binding.propertyName ?? string.Empty);
                    var curve = AnimationUtility.GetEditorCurve(clip, binding) ?? new AnimationCurve();
                    writer.Write((int)curve.preWrapMode); writer.Write((int)curve.postWrapMode); writer.Write(curve.keys.Length);
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

        private static BladeAxes MeasureBladeAxes(SkinnedMeshRenderer renderer, Transform leftHand, Transform rightHand)
        {
            var mesh = new Mesh();
            try
            {
                renderer.BakeMesh(mesh);
                if (mesh.subMeshCount <= 2) throw new InvalidOperationException("Approved Ostinato mesh has no HookBlade submesh.");
                var indices = mesh.GetIndices(2).Distinct().ToArray();
                var vertices = mesh.vertices;
                var weights = renderer.sharedMesh.boneWeights;
                var boneNames = renderer.bones.Select(bone => bone.name).ToArray();
                var left = new List<Vector3>();
                var right = new List<Vector3>();
                foreach (var index in indices)
                {
                    var world = renderer.transform.TransformPoint(vertices[index]);
                    var leftWeight = SumSideWeight(weights[index], boneNames, "Left");
                    var rightWeight = SumSideWeight(weights[index], boneNames, "Right");
                    if (leftWeight > rightWeight) left.Add(world);
                    else if (rightWeight > leftWeight) right.Add(world);
                }
                return new BladeAxes(PrincipalAxis(left), PrincipalAxis(right));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(mesh);
            }
        }

        private static Vector3 PrincipalAxis(IReadOnlyList<Vector3> points)
        {
            if (points.Count < 3) throw new InvalidOperationException("A blade cluster has too few vertices.");
            var center = points.Aggregate(Vector3.zero, (sum, point) => sum + point) / points.Count;
            var xx = 0f; var xy = 0f; var xz = 0f; var yy = 0f; var yz = 0f; var zz = 0f;
            foreach (var point in points)
            {
                var value = point - center;
                xx += value.x * value.x; xy += value.x * value.y; xz += value.x * value.z;
                yy += value.y * value.y; yz += value.y * value.z; zz += value.z * value.z;
            }
            var axis = Vector3.right;
            for (var iteration = 0; iteration < 16; iteration++)
                axis = new Vector3(xx * axis.x + xy * axis.y + xz * axis.z,
                    xy * axis.x + yy * axis.y + yz * axis.z,
                    xz * axis.x + yz * axis.y + zz * axis.z).normalized;
            return axis;
        }

        private static float AngleFromHorizontal(Vector3 axis)
        {
            return Mathf.Abs(Mathf.Asin(Mathf.Clamp(axis.normalized.y, -1f, 1f)) * Mathf.Rad2Deg);
        }

        private static GameObject CreatePlaybackModel(bool useDerivedMesh = true)
        {
            var asset = RequireAsset<GameObject>(OstinatoScissorAttackAnimation.ApprovedModelPath);
            var model = PrefabUtility.InstantiatePrefab(asset) as GameObject ??
                        throw new InvalidOperationException("Approved Ostinato model could not be instantiated.");
            model.name = "Ostinato_WristOrientation_Temporary";
            model.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            model.transform.localScale = Vector3.one;
            var hips = model.GetComponentsInChildren<Transform>(true).Single(target => target.name == "Hips");
            hips.parent.name = BindingRootName;
            var renderer = RequireRenderer(model);
            var derivedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(DerivedMeshPath);
            if (useDerivedMesh && derivedMesh != null) renderer.sharedMesh = derivedMesh;
            renderer.sharedMaterials = ApprovedMaterialPaths.Select(RequireAsset<Material>).ToArray();
            renderer.updateWhenOffscreen = true;
            return model;
        }

        private static BladeControls EnsureBladeControls(GameObject model, SkinnedMeshRenderer renderer)
        {
            var leftHand = FindDescendant(model.transform, "LeftHand");
            var rightHand = FindDescendant(model.transform, "RightHand");
            var left = leftHand.Cast<Transform>().SingleOrDefault(child => child.name == "LeftBladeControl");
            var right = rightHand.Cast<Transform>().SingleOrDefault(child => child.name == "RightBladeControl");
            if (left == null)
            {
                left = new GameObject("LeftBladeControl").transform;
                left.SetParent(leftHand, false);
            }
            if (right == null)
            {
                right = new GameObject("RightBladeControl").transform;
                right.SetParent(rightHand, false);
            }
            left.localPosition = Vector3.zero; left.localRotation = Quaternion.identity; left.localScale = Vector3.one;
            right.localPosition = Vector3.zero; right.localRotation = Quaternion.identity; right.localScale = Vector3.one;
            var bones = renderer.bones.Where(bone => bone != null && bone != left && bone != right).ToList();
            bones.Add(left); bones.Add(right); renderer.bones = bones.ToArray();
            return new BladeControls(left, right);
        }

        private static void RemoveBladeControlsAndRestoreBones(GameObject model, SkinnedMeshRenderer renderer)
        {
            var approvedRenderer = RequireRenderer(RequireAsset<GameObject>(OstinatoScissorAttackAnimation.ApprovedModelPath));
            renderer.bones = approvedRenderer.bones.Select(bone => FindDescendant(model.transform, bone.name)).ToArray();
            renderer.rootBone = approvedRenderer.rootBone == null
                ? null
                : FindDescendant(model.transform, approvedRenderer.rootBone.name);
            foreach (var control in model.GetComponentsInChildren<Transform>(true)
                         .Where(target => BladeControlNames.Contains(target.name)).ToArray())
                UnityEngine.Object.DestroyImmediate(control.gameObject);
        }

        private static void Sample(GameObject model, AnimationClip clip, int frame)
        {
            var approvedAsset = RequireAsset<GameObject>(OstinatoScissorAttackAnimation.ApprovedModelPath);
            FindDescendant(model.transform, "LeftHand").localRotation = FindDescendant(approvedAsset.transform, "LeftHand").localRotation;
            FindDescendant(model.transform, "RightHand").localRotation = FindDescendant(approvedAsset.transform, "RightHand").localRotation;
            var renderer = RequireRenderer(model);
            for (var shape = 0; shape < renderer.sharedMesh.blendShapeCount; shape++)
                renderer.SetBlendShapeWeight(shape, 0f);
            clip.SampleAnimation(model, frame / FrameRate);
        }

        private static Bounds GetSampleBounds(GameObject model, AnimationClip clip, int frame)
        {
            Sample(model, clip, frame);
            return RequireRenderer(model).bounds;
        }

        private static Texture2D RenderSample(GameObject model, AnimationClip clip, int frame, Camera camera, Vector3 direction, Bounds framingBounds)
        {
            Sample(model, clip, frame);
            var skinnedRenderer = RequireRenderer(model);
            var bakedMesh = new Mesh { name = "Ostinato_WristReviewBakedMesh" };
            skinnedRenderer.BakeMesh(bakedMesh);
            var bakedObject = new GameObject("Ostinato_WristReviewBakedModel", typeof(MeshFilter), typeof(MeshRenderer));
            bakedObject.layer = ReviewLayer;
            bakedObject.transform.SetParent(skinnedRenderer.transform.parent, false);
            bakedObject.transform.localPosition = skinnedRenderer.transform.localPosition;
            bakedObject.transform.localRotation = skinnedRenderer.transform.localRotation;
            bakedObject.transform.localScale = skinnedRenderer.transform.localScale;
            bakedObject.GetComponent<MeshFilter>().sharedMesh = bakedMesh;
            bakedObject.GetComponent<MeshRenderer>().sharedMaterials = skinnedRenderer.sharedMaterials;
            var bounds = framingBounds;
            bounds.Expand(new Vector3(0.15f, 0.12f, 0.12f));
            var target = bounds.center + Vector3.up * bounds.extents.y * 0.02f;
            var distance = Mathf.Max(bounds.extents.y, bounds.extents.x) /
                           Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad) + bounds.extents.z + 0.15f;
            camera.transform.position = target + direction * distance;
            camera.transform.rotation = Quaternion.LookRotation(target - camera.transform.position, Vector3.up);
            var renderTexture = RenderTexture.GetTemporary(PanelSize, PanelSize, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var previous = RenderTexture.active;
            try
            {
                skinnedRenderer.enabled = false;
                camera.targetTexture = renderTexture; camera.Render(); RenderTexture.active = renderTexture;
                var texture = new Texture2D(PanelSize, PanelSize, TextureFormat.RGBA32, false);
                texture.ReadPixels(new Rect(0, 0, PanelSize, PanelSize), 0, 0, false); texture.Apply(false, false);
                return texture;
            }
            finally
            {
                skinnedRenderer.enabled = true;
                camera.targetTexture = null; RenderTexture.active = previous; RenderTexture.ReleaseTemporary(renderTexture);
                UnityEngine.Object.DestroyImmediate(bakedObject);
                UnityEngine.Object.DestroyImmediate(bakedMesh);
            }
        }

        private static Texture2D CombineHorizontal(IReadOnlyList<Texture2D> panels)
        {
            var combined = new Texture2D(PanelSize * panels.Count, PanelSize, TextureFormat.RGBA32, false);
            for (var index = 0; index < panels.Count; index++)
                combined.SetPixels(index * PanelSize, 0, PanelSize, PanelSize, panels[index].GetPixels());
            combined.Apply(false, false);
            return combined;
        }

        private static void WriteSheet(IReadOnlyList<byte[]> frames)
        {
            const int columns = 2;
            var frameWidth = PanelSize * 4;
            var rows = Mathf.CeilToInt(frames.Count / (float)columns);
            var sheet = new Texture2D(frameWidth * columns, PanelSize * rows, TextureFormat.RGBA32, false);
            sheet.SetPixels32(Enumerable.Repeat(new Color32(9, 12, 14, 255), sheet.width * sheet.height).ToArray());
            for (var index = 0; index < frames.Count; index++)
            {
                var frame = new Texture2D(2, 2, TextureFormat.RGBA32, false); frame.LoadImage(frames[index], false);
                var column = index % columns; var rowFromTop = index / columns; var row = rows - 1 - rowFromTop;
                sheet.SetPixels(column * frameWidth, row * PanelSize, frameWidth, PanelSize, frame.GetPixels());
                UnityEngine.Object.DestroyImmediate(frame);
            }
            sheet.Apply(false, false);
            var path = OstinatoScissorAttackAnimation.ProjectAbsolutePath(CaptureSheetPath);
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? throw new InvalidOperationException("Capture directory is invalid."));
            File.WriteAllBytes(path, sheet.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(sheet);
        }

        private static void ConfigureCameraAndLights(Camera camera, Light key, Light fill, Transform keyTransform, Transform fillTransform)
        {
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.035f, 0.045f, 0.055f, 1f);
            camera.fieldOfView = 40f; camera.nearClipPlane = 0.05f; camera.farClipPlane = 100f;
            camera.cullingMask = 1 << ReviewLayer; camera.allowHDR = true; camera.allowMSAA = true;
            key.type = LightType.Directional; key.intensity = 1.45f; key.color = new Color(1f, 0.89f, 0.72f); key.cullingMask = 1 << ReviewLayer;
            keyTransform.rotation = Quaternion.Euler(38f, -32f, 0f);
            fill.type = LightType.Directional; fill.intensity = 0.78f; fill.color = new Color(0.46f, 0.66f, 1f); fill.cullingMask = 1 << ReviewLayer;
            fillTransform.rotation = Quaternion.Euler(326f, 148f, 0f);
        }

        private static void SetLayerRecursively(GameObject root, int layer)
        {
            foreach (var target in root.GetComponentsInChildren<Transform>(true)) target.gameObject.layer = layer;
        }

        private static AnimationClip RequireSourceClip()
        {
            var clips = AssetDatabase.LoadAllAssetsAtPath(OstinatoScissorAttackAnimation.AttackModelPath)
                .OfType<AnimationClip>().Where(clip => !clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase) &&
                    clip.name.IndexOf(AttackTakeNameFragment, StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
            if (clips.Length != 1) throw new InvalidOperationException("Expected one source Ostinato attack clip. Count=" + clips.Length);
            return clips[0];
        }

        private static Scene RequireOpenScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != OstinatoScissorAttackAnimation.ScenePath || EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("CargoRunMvp must be active in Edit Mode.");
            return scene;
        }

        private static Animator RequireSceneAnimator(Scene scene)
        {
            var root = scene.GetRootGameObjects().Single(target => target.name == OstinatoScissorAttackAnimation.PlacementRootName).transform;
            var slot = root.GetChild(3);
            if (slot.name != OstinatoScissorAttackAnimation.AttackSlotName || slot.childCount != 1)
                throw new InvalidOperationException("Ostinato attack slot is not ready.");
            return slot.GetChild(0).GetComponent<Animator>() ?? throw new InvalidOperationException("Ostinato attack Animator is missing.");
        }

        private static SkinnedMeshRenderer RequireRenderer(GameObject model)
        {
            var renderers = model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (renderers.Length != 1 || renderers[0].sharedMesh == null)
                throw new InvalidOperationException("Ostinato playback model must contain one skinned renderer.");
            return renderers[0];
        }

        private static Transform FindDescendant(Transform root, string name)
        {
            return root.GetComponentsInChildren<Transform>(true).SingleOrDefault(target => target.name == name) ??
                   throw new InvalidOperationException("Required Ostinato bone is missing: " + name);
        }

        private static T RequireAsset<T>(string path) where T : UnityEngine.Object
        {
            return AssetDatabase.LoadAssetAtPath<T>(path) ?? throw new InvalidOperationException("Required asset is missing: " + path);
        }

        private static string ComputeSha256(string path)
        {
            using var stream = File.OpenRead(path); using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
        }

        private static void AppendMetrics(StringBuilder report, InspectionMetrics metrics)
        {
            report.AppendLine("MaxPreImpactVertexDeviation=" + Format(metrics.MaxWorldRotationDeviation));
            report.AppendLine("MaxHoldBladeHorizontalAngleDegrees=" + Format(metrics.MaxHorizontalAngle));
            report.AppendLine("MaxPostImpactBlendShapeWeight=" + Format(metrics.MaxSourceCorrection));
            report.AppendLine("MaxBlendShapeWeightStep=" + Format(metrics.MaxCorrectedStep));
            report.AppendLine("MaxExistingHandRotationDeviationDegrees=" + Format(metrics.MaxHandDeviation));
            report.AppendLine("MaxPostImpactVertexDeviation=" + Format(metrics.MaxPostImpactVertexDeviation));
            report.AppendLine("MaxPostImpactBoundsRatio=" + Format(metrics.MaxPostImpactBoundsRatio));
            report.AppendLine("ExistingHandAndForeArmAnimationUnchanged=True");
            report.AppendLine("PreImpactFrames0To77Unchanged=True");
            report.AppendLine("BladeLongAxesHorizontalAcrossFrames78To93=True");
            report.AppendLine("BladeWristBoundaryConnected=True");
        }

        private static string Format(float value) => value.ToString("0.######", CultureInfo.InvariantCulture);

        private readonly struct BladeAxes
        {
            public BladeAxes(Vector3 leftAxis, Vector3 rightAxis) { LeftAxis = leftAxis; RightAxis = rightAxis; }
            public Vector3 LeftAxis { get; }
            public Vector3 RightAxis { get; }
        }

        private readonly struct TargetContract
        {
            public TargetContract(Quaternion left, Quaternion right, float leftAngle, float rightAngle)
            {
                LeftTargetWorldRotation = left; RightTargetWorldRotation = right;
                LeftHorizontalAngle = leftAngle; RightHorizontalAngle = rightAngle;
            }
            public Quaternion LeftTargetWorldRotation { get; }
            public Quaternion RightTargetWorldRotation { get; }
            public float LeftHorizontalAngle { get; }
            public float RightHorizontalAngle { get; }
        }

        private readonly struct InspectionMetrics
        {
            public InspectionMetrics(float worldDeviation, float horizontalAngle, float sourceCorrection, float correctedStep,
                float handDeviation, float postImpactVertexDeviation, float postImpactBoundsRatio)
            {
                MaxWorldRotationDeviation = worldDeviation; MaxHorizontalAngle = horizontalAngle;
                MaxSourceCorrection = sourceCorrection; MaxCorrectedStep = correctedStep; MaxHandDeviation = handDeviation;
                MaxPostImpactVertexDeviation = postImpactVertexDeviation; MaxPostImpactBoundsRatio = postImpactBoundsRatio;
            }
            public float MaxWorldRotationDeviation { get; }
            public float MaxHorizontalAngle { get; }
            public float MaxSourceCorrection { get; }
            public float MaxCorrectedStep { get; }
            public float MaxHandDeviation { get; }
            public float MaxPostImpactVertexDeviation { get; }
            public float MaxPostImpactBoundsRatio { get; }
        }

        private readonly struct BladeControls
        {
            public BladeControls(Transform left, Transform right) { Left = left; Right = right; }
            public Transform Left { get; }
            public Transform Right { get; }
        }

        private readonly struct MeshContract
        {
            public MeshContract(Mesh derivedMesh, int bladeVertexCount, int boundaryVertexCount,
                int bladeBodySharedVertexCount, bool nonBladeWeightsUnchanged, bool geometryUnchanged)
            {
                DerivedMesh = derivedMesh; BladeVertexCount = bladeVertexCount;
                BoundaryVertexCount = boundaryVertexCount; BladeBodySharedVertexCount = bladeBodySharedVertexCount;
                NonBladeWeightsUnchanged = nonBladeWeightsUnchanged; GeometryUnchanged = geometryUnchanged;
            }
            public Mesh DerivedMesh { get; }
            public int BladeVertexCount { get; }
            public int BoundaryVertexCount { get; }
            public int BladeBodySharedVertexCount { get; }
            public bool NonBladeWeightsUnchanged { get; }
            public bool GeometryUnchanged { get; }
        }
    }
}
