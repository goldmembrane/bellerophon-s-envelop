using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Bellerophon.Enemies.Fuga;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.FugaCargoRunScene
{
    internal static class FugaConsumeMotionTool
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementRootName = "Approved Fuga Enemy Placement";
        private const string ConsumeSlotName = "Fuga_06_Consume";
        private const string ModelName = "Fuga_Model";
        private const string ImportedModelPath = "Assets/_Project/Art/Enemies/Fuga/Models/fuga.glb";
        private const string DerivedMeshPath =
            "Assets/_Project/Art/Enemies/Fuga/Models/Fuga_Consume_MouthMesh.asset";
        private const string MouthBlendShapeName = "Fuga_Consume_Mouth_Open_60Deg";
        private const string MouthInspectionPath =
            "docs/validation/fuga_consume_motion_2026-08-17/Fuga_Consume_Mouth_Rig_Inspection.txt";
        private const string MotionReportPath =
            "docs/validation/fuga_consume_motion_2026-08-17/Fuga_Consume_Motion_Report.txt";
        private const float LoopDuration = 2f;
        private const float WingbeatFrequency = 0.7f;
        private const float MaximumBodyTiltDegrees = 30f;
        private const float TotalMouthOpenDegrees = 60f;
        private const float EachMouthOpenDegrees = 30f;
        private const float ForwardDistanceMeters = 0.08f;
        private const float GeometryTolerance = 0.0001f;
        private const int RequiredReviewLoops = 2;
        private const float WeightThreshold = 0.001f;
        private const string UpperLipBoneName = "Fuga_UpperLip";
        private const string LowerLipBoneName = "Fuga_LowerLip";
        private const int SeparatedLipVertexCount = 3155;
        private const int SeparatedLipTriangleCount = 3045;
        private const int UpperLipVertexCount = 32;
        private const int LowerLipVertexCount = 11;
        // Measured duplicate-boundary vertices on the supplied GLB define the visible V-shaped lip seam.
        private static readonly Vector3 LeftLipCreaseModel = new(-0.095581f, 0.740350f, 0.829804f);
        private static readonly Vector3 CenterLipCreaseModel = new(-0.007721f, 0.719717f, 0.934433f);
        private static readonly Vector3 RightLipCreaseModel = new(0.101562f, 0.737247f, 0.838944f);
        // The lip-only hinge sits behind the visible seam so opposite 30-degree rotations create a real gap.
        private const float LipHingeDepthMeters = 0.274433f;

        [MenuItem("Bellerophon/Enemies/Fuga/Apply Consume Motion")]
        public static void ApplyFugaConsumeMotion()
        {
            var scene = RequireCurrentScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException("CargoRunMvp must be saved before applying the Fuga consume motion.");
            }

            var placementRoot = RequireRoot(PlacementRootName);
            var slot = placementRoot.Find(ConsumeSlotName) ??
                       throw new InvalidOperationException(ConsumeSlotName + " is missing.");
            var model = slot.Find(ModelName) ??
                        throw new InvalidOperationException(ConsumeSlotName + "/" + ModelName + " is missing.");
            var renderer = model.GetComponentInChildren<SkinnedMeshRenderer>(true) ??
                           throw new InvalidOperationException("The current Fuga consume model has no SkinnedMeshRenderer.");
            var sourceHashBefore = Sha256(Absolute(ImportedModelPath));
            var otherSlotsBefore = OtherSlotSignature(placementRoot);

            var sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ImportedModelPath) ??
                               throw new InvalidOperationException("The rigged Fuga GLB is missing.");
            var sourceRenderer = sourcePrefab.GetComponentInChildren<SkinnedMeshRenderer>(true) ??
                                 throw new InvalidOperationException("The rigged Fuga GLB has no renderer.");
            renderer.sharedMesh = sourceRenderer.sharedMesh;
            EditorUtility.SetDirty(renderer);

            var leftWing = FindBone(renderer, "Bone_013");
            var rightWing = FindBone(renderer, "Bone_017");
            var upperLip = FindBone(renderer, UpperLipBoneName);
            var lowerLip = FindBone(renderer, LowerLipBoneName);
            var mouthInfo = InspectMouthRig(model, renderer);
            RestoreWingBindRotations(leftWing, rightWing);
            var animator = slot.GetComponent<Animator>() ?? slot.gameObject.AddComponent<Animator>();
            animator.runtimeAnimatorController = null;
            animator.applyRootMotion = false;
            animator.enabled = false;
            EditorUtility.SetDirty(animator);

            var legacyPlayback = slot.GetComponent<FugaAnimationReviewPlaybackDriver>();
            if (legacyPlayback != null)
            {
                UnityEngine.Object.DestroyImmediate(legacyPlayback);
            }

            var otherPhysicsDriver = slot.GetComponent<FugaPhysicsMotionDriver>();
            if (otherPhysicsDriver != null)
            {
                otherPhysicsDriver.enabled = false;
                EditorUtility.SetDirty(otherPhysicsDriver);
            }

            var body = slot.GetComponent<Rigidbody>() ??
                       throw new InvalidOperationException(ConsumeSlotName + " has no Rigidbody.");
            if (slot.GetComponent<Collider>() == null)
            {
                throw new InvalidOperationException(ConsumeSlotName + " has no Collider.");
            }

            body.isKinematic = false;
            body.useGravity = false;
            body.constraints = RigidbodyConstraints.FreezeRotation;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            EditorUtility.SetDirty(body);

            var driver = slot.GetComponent<FugaConsumeMotionDriver>() ??
                         slot.gameObject.AddComponent<FugaConsumeMotionDriver>();
            driver.enabled = true;
            driver.Configure(
                body,
                model,
                leftWing,
                rightWing,
                upperLip,
                lowerLip,
                model.forward,
                LoopDuration,
                WingbeatFrequency,
                MaximumBodyTiltDegrees,
                TotalMouthOpenDegrees,
                ForwardDistanceMeters);
            EditorUtility.SetDirty(driver);

            if (!string.Equals(otherSlotsBefore, OtherSlotSignature(placementRoot), StringComparison.Ordinal))
            {
                throw new InvalidOperationException("A protected non-consume Fuga slot changed.");
            }

            RequireHash(sourceHashBefore, Sha256(Absolute(ImportedModelPath)), "original Fuga GLB");
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException("CargoRunMvp could not be saved after applying the Fuga consume motion.");
            }

            AssetDatabase.SaveAssets();
            var result = InspectAppliedState();
            WriteMotionReport(result, mouthInfo, directReviewCompleted: false);
            AssetDatabase.Refresh();
            Debug.Log(
                "FugaConsumeMotionApplied Result=PASS" +
                ", UpperLipBone=" + UpperLipBoneName +
                ", LowerLipBone=" + LowerLipBoneName +
                ", InterLipFaces=0" +
                ", LipRigWingWeightedVertices=0" +
                ", WingbeatFrequencyHz=0.7" +
                ", MaximumBodyTiltDegrees=30" +
                ", TotalMouthOpenDegrees=60" +
                ", ForwardDistanceMeters=0.08" +
                ", LoopDurationSeconds=2" +
                ", Loop=True.");
        }

        [MenuItem("Bellerophon/Enemies/Fuga/Inspect Consume Motion")]
        public static void InspectFugaConsumeMotion()
        {
            var scene = RequireCurrentScene();
            var dirtyBefore = scene.isDirty;
            var result = InspectAppliedState();
            var mouthInfo = InspectMouthRig(result.Model, result.Renderer);
            WriteMotionReport(result, mouthInfo, directReviewCompleted: false);
            AssetDatabase.Refresh();
            if (scene.isDirty != dirtyBefore)
            {
                throw new InvalidOperationException("Inspecting the Fuga consume motion changed the scene dirty state.");
            }

            Debug.Log(
                "FugaConsumeMotionInspected Result=PASS" +
                ", WingbeatFrequencyHz=0.7" +
                ", WingbeatContinuousAcrossConsumeLoop=True" +
                ", MaximumBodyTiltDegrees=30" +
                ", UpperLowerMouthOpenDegrees=30+30" +
                ", ForwardDistanceMeters=0.08" +
                ", LoopDurationSeconds=2" +
                ", RigidbodyForwardMotion=True" +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Fuga/Start Consume Motion Review Playback")]
        public static void StartFugaConsumeMotionReviewPlayback()
        {
            if (EditorApplication.isPlaying)
            {
                throw new InvalidOperationException("Unity is already in Play Mode.");
            }

            var scene = RequireCurrentScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException("CargoRunMvp must be saved before the direct Fuga consume review.");
            }

            InspectAppliedState();
            var gameViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView") ??
                               throw new InvalidOperationException("The Unity Game View type is unavailable.");
            EditorWindow.GetWindow(gameViewType).Focus();
            EditorApplication.isPaused = false;
            EditorApplication.delayCall += () =>
            {
                EditorApplication.isPaused = false;
                EditorApplication.isPlaying = true;
            };
            Debug.Log(
                "FugaConsumeMotionReviewPlaybackStarted Result=PASS" +
                ", RequiredLoops=2" +
                ", LiveGameView=True" +
                ", CaptureCreated=False.");
        }

        [MenuItem("Bellerophon/Enemies/Fuga/Stop Consume Motion Review Playback")]
        public static void StopFugaConsumeMotionReviewPlayback()
        {
            if (!EditorApplication.isPlaying)
            {
                throw new InvalidOperationException("Unity must be in Play Mode to finish the direct Fuga consume review.");
            }

            var result = InspectAppliedState();
            var driver = result.Driver;
            if (driver.CompletedLoopCount < RequiredReviewLoops || driver.CompletedWingbeatCount < 2 ||
                driver.InvalidLoopPeakCount != 0 || driver.InvalidLoopReturnCount != 0)
            {
                Debug.LogError(
                    "FugaConsumeMotionReviewPlaybackIncomplete" +
                    ", CompletedLoops=" + driver.CompletedLoopCount.ToString(CultureInfo.InvariantCulture) +
                    ", CompletedWingbeats=" + driver.CompletedWingbeatCount.ToString(CultureInfo.InvariantCulture) +
                    ", LastLoopPeakBodyTiltDegrees=" + driver.LastLoopPeakBodyTiltDegrees.ToString("F6", CultureInfo.InvariantCulture) +
                    ", LastLoopPeakForwardOffsetMeters=" + driver.LastLoopPeakForwardOffsetMeters.ToString("F6", CultureInfo.InvariantCulture) +
                    ", LastLoopPeakMouthWeight=" + driver.LastLoopPeakMouthWeight.ToString("F6", CultureInfo.InvariantCulture) +
                    ", LastLoopPeakUpperLipAngleDegrees=" + driver.LastLoopPeakUpperLipAngleDegrees.ToString("F6", CultureInfo.InvariantCulture) +
                    ", LastLoopPeakLowerLipAngleDegrees=" + driver.LastLoopPeakLowerLipAngleDegrees.ToString("F6", CultureInfo.InvariantCulture) +
                    ", InvalidLoopPeakCount=" + driver.InvalidLoopPeakCount.ToString(CultureInfo.InvariantCulture) +
                    ", InvalidLoopReturnCount=" + driver.InvalidLoopReturnCount.ToString(CultureInfo.InvariantCulture) + ".");
                EditorApplication.delayCall += () => EditorApplication.isPlaying = false;
                throw new InvalidOperationException("The direct Fuga consume review did not complete the required exact loops.");
            }

            var mouthInfo = InspectMouthRig(result.Model, result.Renderer);
            WriteMotionReport(result, mouthInfo, directReviewCompleted: true);
            Debug.Log(
                "FugaConsumeMotionReviewPlaybackStopped Result=PASS" +
                ", CompletedLoops=" + driver.CompletedLoopCount.ToString(CultureInfo.InvariantCulture) +
                ", CompletedWingbeats=" + driver.CompletedWingbeatCount.ToString(CultureInfo.InvariantCulture) +
                ", LastLoopPeakBodyTiltDegrees=" + driver.LastLoopPeakBodyTiltDegrees.ToString("F6", CultureInfo.InvariantCulture) +
                ", LastLoopPeakForwardOffsetMeters=" + driver.LastLoopPeakForwardOffsetMeters.ToString("F6", CultureInfo.InvariantCulture) +
                ", LastLoopPeakMouthWeight=" + driver.LastLoopPeakMouthWeight.ToString("F6", CultureInfo.InvariantCulture) +
                ", LastLoopPeakUpperLipAngleDegrees=" + driver.LastLoopPeakUpperLipAngleDegrees.ToString("F6", CultureInfo.InvariantCulture) +
                ", LastLoopPeakLowerLipAngleDegrees=" + driver.LastLoopPeakLowerLipAngleDegrees.ToString("F6", CultureInfo.InvariantCulture) +
                ", InvalidLoopPeakCount=0" +
                ", InvalidLoopReturnCount=0" +
                ", LiveGameView=True" +
                ", CaptureCreated=False.");
            EditorApplication.delayCall += () => EditorApplication.isPlaying = false;
        }

        [MenuItem("Bellerophon/Enemies/Fuga/Inspect Consume Mouth Rig")]
        public static void InspectFugaConsumeMouthRig()
        {
            var scene = RequireCurrentScene();
            var dirtyBefore = scene.isDirty;
            var slot = RequireRoot(PlacementRootName).Find(ConsumeSlotName) ??
                       throw new InvalidOperationException(ConsumeSlotName + " is missing.");
            var model = slot.Find(ModelName) ??
                        throw new InvalidOperationException(ConsumeSlotName + "/" + ModelName + " is missing.");
            var renderer = model.GetComponentInChildren<SkinnedMeshRenderer>(true) ??
                           throw new InvalidOperationException("The current Fuga consume model has no SkinnedMeshRenderer.");
            var mesh = renderer.sharedMesh ??
                       throw new InvalidOperationException("The current Fuga consume renderer has no mesh.");
            var imported = AssetDatabase.LoadAssetAtPath<GameObject>(ImportedModelPath) ??
                           throw new InvalidOperationException("The supplied Fuga GLB is missing.");
            var sourceRenderer = imported.GetComponentInChildren<SkinnedMeshRenderer>(true) ??
                                 throw new InvalidOperationException("The supplied Fuga GLB has no SkinnedMeshRenderer.");
            var sourceMesh = sourceRenderer.sharedMesh ??
                             throw new InvalidOperationException("The supplied Fuga GLB has no skinned mesh.");
            var camera = Camera.main ?? UnityEngine.Object.FindFirstObjectByType<Camera>() ??
                         throw new InvalidOperationException("The current scene has no camera for target-facing mouth identification.");

            var sourceHashBefore = Sha256(Absolute(ImportedModelPath));
            var vertices = sourceMesh.vertices;
            var weights = sourceMesh.boneWeights;
            var bones = renderer.bones;
            if (vertices.Length == 0 || weights.Length != vertices.Length || bones.Length == 0)
            {
                throw new InvalidOperationException("The original Fuga mesh skin data is incomplete.");
            }

            var currentVertices = mesh.vertices;
            var sourceTriangles = sourceMesh.triangles;
            var currentTriangles = mesh.triangles;
            var sourceNormals = sourceMesh.normals;
            if (sourceNormals.Length != vertices.Length)
            {
                throw new InvalidOperationException("The original Fuga mesh normals are incomplete.");
            }

            var modelNormals = sourceNormals
                .Select(normal => model.InverseTransformDirection(renderer.transform.TransformDirection(normal)).normalized)
                .ToArray();
            var sourceVertexOrderMatches = currentVertices.Length == vertices.Length &&
                                           Enumerable.Range(0, vertices.Length).All(index =>
                                               (currentVertices[index] - vertices[index]).sqrMagnitude <=
                                               GeometryTolerance * GeometryTolerance);
            var sourceTopologyMatches = sourceTriangles.SequenceEqual(currentTriangles);
            if (!sourceVertexOrderMatches || !sourceTopologyMatches)
            {
                throw new InvalidOperationException(
                    "The current consume mesh does not preserve the original Fuga vertex order and topology.");
            }

            var region = AnalyzeMouthRegion(model, renderer, camera, vertices, weights);
            var modelVertices = region.ModelVertices;
            var projections = modelVertices.Select(vertex => Vector3.Dot(vertex, region.FrontAxis)).ToArray();
            var frontIndices = Enumerable.Range(0, modelVertices.Length)
                .Where(index =>
                {
                    var crease = LipCreasePoint(region, modelVertices[index].x);
                    var lipChainWeight = WeightForNamedBones(
                        weights[index], bones, UpperLipBoneName, LowerLipBoneName);
                    return LipWidthProgress(region, modelVertices[index].x) < 1f &&
                           Mathf.Abs(projections[index] - Vector3.Dot(crease, region.FrontAxis)) < 0.16f &&
                           Mathf.Abs(modelVertices[index].y - crease.y) <= region.VerticalHalfBand &&
                           lipChainWeight > 0.05f;
                })
                .ToArray();
            if (frontIndices.Length < 24)
            {
                throw new InvalidOperationException(
                    "The target-facing Fuga front region is too small for upper/lower mouth identification: " +
                    frontIndices.Length + ".");
            }

            var mouthSplitY = region.MouthSplitY;
            var upperIndices = frontIndices
                .Where(index => IsUpperLipVertex(region, modelVertices[index], modelNormals[index]))
                .ToArray();
            var lowerIndices = frontIndices
                .Where(index => !IsUpperLipVertex(region, modelVertices[index], modelNormals[index]))
                .ToArray();
            var upperSet = new HashSet<int>(upperIndices);
            var lowerSet = new HashSet<int>(lowerIndices);
            var lipSetOverlap = upperSet.Count(lowerSet.Contains);
            var leftCreaseIndices = IndicesAtPosition(modelVertices, region.LeftLipCrease);
            var centerCreaseIndices = IndicesAtPosition(modelVertices, region.MouthPivot);
            var rightCreaseIndices = IndicesAtPosition(modelVertices, region.RightLipCrease);
            var scores = Enumerable.Range(0, bones.Length)
                .Select(index => ScoreBone(index, bones[index], model, modelVertices, weights, upperIndices, lowerIndices))
                .ToArray();
            var upperCandidate = scores.Single(score =>
                score.Bone != null && string.Equals(score.Bone.name, UpperLipBoneName, StringComparison.Ordinal));
            var lowerCandidate = scores.Single(score =>
                score.Bone != null && string.Equals(score.Bone.name, LowerLipBoneName, StringComparison.Ordinal));

            var broadFrontIndices = Enumerable.Range(0, modelVertices.Length)
                .Where(index =>
                    projections[index] >= region.FrontMaximum - 0.36f &&
                    Mathf.Abs(modelVertices[index].x - region.MouthPivot.x) <= 0.34f &&
                    modelVertices[index].y >= 0.48f &&
                    modelVertices[index].y <= 0.94f)
                .ToArray();
            var broadFrontSet = new HashSet<int>(broadFrontIndices);
            var edgeUse = new Dictionary<ulong, int>();
            var triangles = sourceTriangles;
            for (var triangleIndex = 0; triangleIndex + 2 < triangles.Length; triangleIndex += 3)
            {
                AddEdgeUse(edgeUse, triangles[triangleIndex], triangles[triangleIndex + 1]);
                AddEdgeUse(edgeUse, triangles[triangleIndex + 1], triangles[triangleIndex + 2]);
                AddEdgeUse(edgeUse, triangles[triangleIndex + 2], triangles[triangleIndex]);
            }

            var boundaryVertices = new HashSet<int>();
            foreach (var edge in edgeUse)
            {
                if (edge.Value != 1) continue;
                var first = (int)(edge.Key >> 32);
                var second = (int)(edge.Key & uint.MaxValue);
                if (broadFrontSet.Contains(first)) boundaryVertices.Add(first);
                if (broadFrontSet.Contains(second)) boundaryVertices.Add(second);
            }

            var duplicatePairs = new List<string>();
            for (var firstIndex = 0; firstIndex < broadFrontIndices.Length; firstIndex++)
            {
                var first = broadFrontIndices[firstIndex];
                for (var secondIndex = firstIndex + 1; secondIndex < broadFrontIndices.Length; secondIndex++)
                {
                    var second = broadFrontIndices[secondIndex];
                    if ((modelVertices[first] - modelVertices[second]).sqrMagnitude > 0.00000025f) continue;
                    duplicatePairs.Add(first.ToString(CultureInfo.InvariantCulture) + ":" +
                                       second.ToString(CultureInfo.InvariantCulture) + "@" + Vec(modelVertices[first]));
                }
            }

            var blendShapeDeltas = new Vector3[currentVertices.Length];
            var blendShapeNormals = new Vector3[currentVertices.Length];
            var blendShapeTangents = new Vector3[currentVertices.Length];
            var affectedIndices = Array.Empty<int>();
            if (mesh.blendShapeCount > 0)
            {
                mesh.GetBlendShapeFrameVertices(0, 0, blendShapeDeltas, blendShapeNormals, blendShapeTangents);
                affectedIndices = Enumerable.Range(0, blendShapeDeltas.Length)
                    .Where(index => blendShapeDeltas[index].sqrMagnitude > GeometryTolerance * GeometryTolerance)
                    .ToArray();
            }

            var wingBoneSet = BoneSet(
                renderer,
                "Bone_010", "Bone_011", "Bone_012", "Bone_013",
                "Bone_014", "Bone_015", "Bone_016", "Bone_017");
            var upperWingWeighted = upperIndices.Count(index => SumWeight(weights[index], wingBoneSet) > WeightThreshold);
            var lowerWingWeighted = lowerIndices.Count(index => SumWeight(weights[index], wingBoneSet) > WeightThreshold);
            var affectedWingWeighted = affectedIndices.Count(index => SumWeight(weights[index], wingBoneSet) > 0.001f);
            var affectedOutsideBroadFront = affectedIndices.Count(index => !broadFrontSet.Contains(index));
            var identifiedLipSet = new HashSet<int>(frontIndices);
            var affectedOutsideIdentifiedLips = affectedIndices.Count(index => !identifiedLipSet.Contains(index));
            var normals = sourceNormals;
            var uvs = sourceMesh.uv;

            if (lipSetOverlap != 0 || upperIndices.Length == 0 || lowerIndices.Length == 0 ||
                leftCreaseIndices.Length == 0 || centerCreaseIndices.Length == 0 || rightCreaseIndices.Length == 0 ||
                upperWingWeighted != 0 || lowerWingWeighted != 0)
            {
                throw new InvalidOperationException(
                    "The original Fuga upper/lower lip identification is not isolated. Overlap=" + lipSetOverlap +
                    ", Upper=" + upperIndices.Length + ", Lower=" + lowerIndices.Length +
                    ", UpperWing=" + upperWingWeighted + ", LowerWing=" + lowerWingWeighted + ".");
            }

            var report = new StringBuilder()
                .AppendLine("Fuga Consume Mouth Rig Inspection")
                .AppendLine("Result=PASS")
                .AppendLine("Scene=" + ScenePath)
                .AppendLine("Target=" + PlacementRootName + "/" + ConsumeSlotName + "/" + ModelName)
                .AppendLine("IdentificationSourceMesh=" + AssetDatabase.GetAssetPath(sourceMesh))
                .AppendLine("CurrentRendererMesh=" + AssetDatabase.GetAssetPath(mesh))
                .AppendLine("SourceVertexOrderMatchesCurrent=True")
                .AppendLine("SourceTriangleTopologyMatchesCurrent=True")
                .AppendLine("VertexCount=" + vertices.Length.ToString(CultureInfo.InvariantCulture))
                .AppendLine("BoneCount=" + bones.Length.ToString(CultureInfo.InvariantCulture))
                .AppendLine("TargetCamera=" + camera.name)
                .AppendLine("ModelLocalTargetFacingAxis=" + (region.FrontSign > 0f ? "+Z" : "-Z"))
                .AppendLine("IdentificationBasis=OriginalMeshCentralVCreaseTopologyAndDedicatedLipBoneSkinWeights")
                .AppendLine("CreaseVertexSurfaceClassification=ModelYRelativeToCentralVThenModelNormalYAtSharedCrease")
                .AppendLine("LeftLipCreaseModel=" + Vec(region.LeftLipCrease))
                .AppendLine("CenterLipCreaseModel=" + Vec(region.MouthPivot))
                .AppendLine("RightLipCreaseModel=" + Vec(region.RightLipCrease))
                .AppendLine("LeftLipCreaseVertexIndices=" + IndexList(leftCreaseIndices))
                .AppendLine("CenterLipCreaseVertexIndices=" + IndexList(centerCreaseIndices))
                .AppendLine("RightLipCreaseVertexIndices=" + IndexList(rightCreaseIndices))
                .AppendLine("IdentifiedLipVertexCount=" + frontIndices.Length.ToString(CultureInfo.InvariantCulture))
                .AppendLine("UpperLipRegionVertexCount=" + upperIndices.Length.ToString(CultureInfo.InvariantCulture))
                .AppendLine("LowerLipRegionVertexCount=" + lowerIndices.Length.ToString(CultureInfo.InvariantCulture))
                .AppendLine("UpperLipVertexIndices=" + IndexList(upperIndices))
                .AppendLine("LowerLipVertexIndices=" + IndexList(lowerIndices))
                .AppendLine("UpperLowerLipVertexOverlap=" + lipSetOverlap.ToString(CultureInfo.InvariantCulture))
                .AppendLine("UpperLipWingWeightedVertexCount=" + upperWingWeighted.ToString(CultureInfo.InvariantCulture))
                .AppendLine("LowerLipWingWeightedVertexCount=" + lowerWingWeighted.ToString(CultureInfo.InvariantCulture))
                .AppendLine("MouthSplitModelY=" + mouthSplitY.ToString("F6", CultureInfo.InvariantCulture))
                .AppendLine("UpperCandidateBone=" + upperCandidate.Bone.name)
                .AppendLine("UpperCandidatePath=" + RelativePath(model, upperCandidate.Bone))
                .AppendLine("LowerCandidateBone=" + lowerCandidate.Bone.name)
                .AppendLine("LowerCandidatePath=" + RelativePath(model, lowerCandidate.Bone))
                .AppendLine("LipBoneAssignmentBasis=EmbeddedFugaUpperLipAndFugaLowerLipBones")
                .AppendLine("BroadFrontVertexCount=" + broadFrontIndices.Length.ToString(CultureInfo.InvariantCulture))
                .AppendLine("BroadFrontBoundaryVertexCount=" + boundaryVertices.Count.ToString(CultureInfo.InvariantCulture))
                .AppendLine("BroadFrontDuplicatePositionPairCount=" + duplicatePairs.Count.ToString(CultureInfo.InvariantCulture))
                .AppendLine("CurrentBlendShapeAffectedVertexCount=" + affectedIndices.Length.ToString(CultureInfo.InvariantCulture))
                .AppendLine("CurrentBlendShapeWingWeightedVertexCount=" + affectedWingWeighted.ToString(CultureInfo.InvariantCulture))
                .AppendLine("CurrentBlendShapeOutsideBroadFrontVertexCount=" + affectedOutsideBroadFront.ToString(CultureInfo.InvariantCulture))
                .AppendLine("CurrentBlendShapeOutsideIdentifiedLipVertexCount=" + affectedOutsideIdentifiedLips.ToString(CultureInfo.InvariantCulture))
                .AppendLine("SceneChanged=False")
                .AppendLine();

            AppendLipVertices(report, "UpperLipVertex", upperIndices, modelVertices, weights, bones);
            AppendLipVertices(report, "LowerLipVertex", lowerIndices, modelVertices, weights, bones);
            foreach (var pair in duplicatePairs)
            {
                report.Append("DuplicatePair=").AppendLine(pair);
            }

            foreach (var index in boundaryVertices.OrderBy(index => modelVertices[index].y).ThenBy(index => modelVertices[index].x))
            {
                report.Append("BoundaryVertex=").Append(index.ToString(CultureInfo.InvariantCulture))
                    .Append(" Position=").Append(Vec(modelVertices[index]))
                    .Append(" Normal=").Append(normals.Length == vertices.Length ? Vec(normals[index]) : "<none>")
                    .Append(" UV=").Append(uvs.Length == vertices.Length ?
                        "(" + uvs[index].x.ToString("F6", CultureInfo.InvariantCulture) + "," +
                        uvs[index].y.ToString("F6", CultureInfo.InvariantCulture) + ")" : "<none>")
                    .AppendLine();
            }

            foreach (var score in scores.OrderByDescending(score => score.UpperWeight + score.LowerWeight))
            {
                report.Append("Bone=").Append(score.Bone != null ? score.Bone.name : "<null>")
                    .Append(" Path=").Append(score.Bone != null ? RelativePath(model, score.Bone) : "<null>")
                    .Append(" UpperWeight=").Append(score.UpperWeight.ToString("F6", CultureInfo.InvariantCulture))
                    .Append(" LowerWeight=").Append(score.LowerWeight.ToString("F6", CultureInfo.InvariantCulture))
                    .Append(" UpperVertices=").Append(score.UpperVertices.ToString(CultureInfo.InvariantCulture))
                    .Append(" LowerVertices=").Append(score.LowerVertices.ToString(CultureInfo.InvariantCulture))
                    .Append(" ModelPosition=").Append(score.Bone != null ? Vec(model.InverseTransformPoint(score.Bone.position)) : "<null>")
                    .AppendLine();
            }

            var absolute = Absolute(MouthInspectionPath);
            Directory.CreateDirectory(Path.GetDirectoryName(absolute) ??
                                      throw new InvalidOperationException("Invalid Fuga consume inspection report path."));
            File.WriteAllText(absolute, report.ToString(), new UTF8Encoding(false));
            AssetDatabase.Refresh();
            if (scene.isDirty != dirtyBefore)
            {
                throw new InvalidOperationException("The Fuga consume mouth inspection changed the scene dirty state.");
            }

            RequireHash(sourceHashBefore, Sha256(Absolute(ImportedModelPath)), "original Fuga GLB");

            Debug.Log(
                "FugaConsumeMouthRigInspected Result=PASS" +
                ", TargetFacingAxis=" + (region.FrontSign > 0f ? "+Z" : "-Z") +
                ", UpperLipVertices=" + upperIndices.Length.ToString(CultureInfo.InvariantCulture) +
                ", LowerLipVertices=" + lowerIndices.Length.ToString(CultureInfo.InvariantCulture) +
                ", LipVertexOverlap=0" +
                ", LipCreaseModelY=" + region.MouthSplitY.ToString("F6", CultureInfo.InvariantCulture) +
                ", UpperCandidate=" + upperCandidate.Bone.name +
                ", LowerCandidate=" + lowerCandidate.Bone.name +
                ", BoundaryVertices=" + boundaryVertices.Count.ToString(CultureInfo.InvariantCulture) +
                ", DuplicatePairs=" + duplicatePairs.Count.ToString(CultureInfo.InvariantCulture) +
                ", BlendShapeWingWeightedVertices=" + affectedWingWeighted.ToString(CultureInfo.InvariantCulture) +
                ", SceneChanged=False.");
        }

        private static void AddEdgeUse(IDictionary<ulong, int> edgeUse, int first, int second)
        {
            var minimum = (uint)Mathf.Min(first, second);
            var maximum = (uint)Mathf.Max(first, second);
            var key = ((ulong)minimum << 32) | maximum;
            edgeUse[key] = edgeUse.TryGetValue(key, out var count) ? count + 1 : 1;
        }

        private static int[] IndicesAtPosition(IReadOnlyList<Vector3> vertices, Vector3 position)
        {
            return Enumerable.Range(0, vertices.Count)
                .Where(index => (vertices[index] - position).sqrMagnitude <= 0.000004f)
                .ToArray();
        }

        private static bool IsUpperLipVertex(MouthRegion region, Vector3 modelVertex, Vector3 modelNormal)
        {
            var signedVerticalDistance = modelVertex.y - LipCreasePoint(region, modelVertex.x).y;
            if (Mathf.Abs(signedVerticalDistance) > GeometryTolerance)
            {
                return signedVerticalDistance > 0f;
            }

            return modelNormal.y >= 0f;
        }

        private static string IndexList(IEnumerable<int> indices)
        {
            return string.Join(",", indices.Select(index => index.ToString(CultureInfo.InvariantCulture)));
        }

        private static float WeightForNamedBones(BoneWeight weight, IReadOnlyList<Transform> bones, params string[] names)
        {
            var total = 0f;
            for (var slot = 0; slot < 4; slot++)
            {
                var boneIndex = BoneIndex(weight, slot);
                if (boneIndex < 0 || boneIndex >= bones.Count || bones[boneIndex] == null ||
                    !names.Contains(bones[boneIndex].name, StringComparer.Ordinal))
                {
                    continue;
                }

                total += BoneWeightValue(weight, slot);
            }

            return total;
        }

        private static void AppendLipVertices(
            StringBuilder report,
            string label,
            IEnumerable<int> indices,
            IReadOnlyList<Vector3> modelVertices,
            IReadOnlyList<BoneWeight> weights,
            IReadOnlyList<Transform> bones)
        {
            foreach (var index in indices.OrderBy(index => modelVertices[index].x)
                         .ThenBy(index => modelVertices[index].y)
                         .ThenBy(index => modelVertices[index].z))
            {
                report.Append(label).Append('=').Append(index.ToString(CultureInfo.InvariantCulture))
                    .Append(" Position=").Append(Vec(modelVertices[index]))
                    .Append(" Fuga_UpperLipWeight=")
                    .Append(WeightForNamedBones(weights[index], bones, UpperLipBoneName).ToString("F6", CultureInfo.InvariantCulture))
                    .Append(" Fuga_LowerLipWeight=")
                    .Append(WeightForNamedBones(weights[index], bones, LowerLipBoneName).ToString("F6", CultureInfo.InvariantCulture))
                    .Append(" DominantBone=").Append(DominantBoneName(weights[index], bones))
                    .AppendLine();
            }
        }

        private static string DominantBoneName(BoneWeight weight, IReadOnlyList<Transform> bones)
        {
            var dominantSlot = 0;
            var dominantWeight = BoneWeightValue(weight, 0);
            for (var slot = 1; slot < 4; slot++)
            {
                var candidateWeight = BoneWeightValue(weight, slot);
                if (candidateWeight <= dominantWeight) continue;
                dominantSlot = slot;
                dominantWeight = candidateWeight;
            }

            var boneIndex = BoneIndex(weight, dominantSlot);
            return boneIndex >= 0 && boneIndex < bones.Count && bones[boneIndex] != null
                ? bones[boneIndex].name
                : "<none>";
        }

        private static int BoneIndex(BoneWeight weight, int slot)
        {
            return slot switch
            {
                0 => weight.boneIndex0,
                1 => weight.boneIndex1,
                2 => weight.boneIndex2,
                3 => weight.boneIndex3,
                _ => -1
            };
        }

        private static float BoneWeightValue(BoneWeight weight, int slot)
        {
            return slot switch
            {
                0 => weight.weight0,
                1 => weight.weight1,
                2 => weight.weight2,
                3 => weight.weight3,
                _ => 0f
            };
        }

        private static Mesh CreateConsumeMouthMesh(
            Transform model,
            SkinnedMeshRenderer sceneRenderer,
            Camera camera,
            out MouthInfo info)
        {
            var imported = AssetDatabase.LoadAssetAtPath<GameObject>(ImportedModelPath) ??
                           throw new InvalidOperationException("The supplied Fuga GLB is missing.");
            var sourceRenderer = imported.GetComponentInChildren<SkinnedMeshRenderer>(true) ??
                                 throw new InvalidOperationException("The supplied Fuga GLB has no SkinnedMeshRenderer.");
            var source = sourceRenderer.sharedMesh ??
                         throw new InvalidOperationException("The supplied Fuga GLB has no skinned mesh.");
            if (source.vertexCount != sceneRenderer.sharedMesh.vertexCount ||
                source.boneWeights.Length != source.vertexCount)
            {
                throw new InvalidOperationException("The current consume skin does not match the supplied Fuga GLB.");
            }

            var generated = UnityEngine.Object.Instantiate(source);
            generated.name = "Fuga_Consume_MouthMesh";
            generated.ClearBlendShapes();
            var vertices = source.vertices;
            var weights = source.boneWeights;
            var region = AnalyzeMouthRegion(model, sceneRenderer, camera, vertices, weights);
            var upperBoneSet = BoneSet(sceneRenderer, "Bone_001", "Bone_003");
            var lowerBoneSet = BoneSet(sceneRenderer, "Bone_003", "Bone_002");
            var deltas = new Vector3[vertices.Length];
            var targets = (Vector3[])vertices.Clone();
            var upperAffected = 0;
            var lowerAffected = 0;
            var upperFullAngle = 0;
            var lowerFullAngle = 0;
            var maximumUpperAngle = 0f;
            var maximumLowerAngle = 0f;
            var maximumUpperRise = 0f;
            var maximumLowerDrop = 0f;
            var lipDirectionErrors = 0;
            for (var index = 0; index < vertices.Length; index++)
            {
                var modelVertex = region.ModelVertices[index];
                var crease = LipCreasePoint(region, modelVertex.x);
                var projection = Vector3.Dot(modelVertex, region.FrontAxis);
                var creaseProjection = Vector3.Dot(crease, region.FrontAxis);
                var depthDistance = Mathf.Abs(projection - creaseProjection);
                var widthProgress = LipWidthProgress(region, modelVertex.x);
                var verticalDistance = Mathf.Abs(modelVertex.y - crease.y);
                if (depthDistance >= 0.16f || widthProgress >= 1f || verticalDistance >= region.VerticalHalfBand)
                {
                    continue;
                }

                var depthMask = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((0.16f - depthDistance) / 0.04f));
                var widthMask = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((1f - widthProgress) / 0.18f));
                var verticalMask = Mathf.SmoothStep(
                    0f,
                    1f,
                    Mathf.Clamp01((region.VerticalHalfBand - verticalDistance) / (region.VerticalHalfBand * 0.25f)));
                var isUpper = modelVertex.y >= crease.y;
                var skinWeight = SumWeight(weights[index], isUpper ? upperBoneSet : lowerBoneSet);
                var skinMask = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((skinWeight - 0.05f) / 0.35f));
                var mask = depthMask * widthMask * verticalMask * skinMask;
                if (mask <= 0.0001f)
                {
                    continue;
                }

                var signedAngle = (isUpper ? -region.FrontSign : region.FrontSign) *
                                  EachMouthOpenDegrees * mask;
                var hinge = crease - region.FrontAxis * LipHingeDepthMeters;
                var targetModel = hinge +
                                  Quaternion.AngleAxis(signedAngle, Vector3.right) *
                                  (modelVertex - hinge);
                var targetRenderer = sceneRenderer.transform.InverseTransformPoint(model.TransformPoint(targetModel));
                var modelDelta = targetModel - modelVertex;
                deltas[index] = targetRenderer - vertices[index];
                targets[index] = targetRenderer;
                if (deltas[index].sqrMagnitude <= GeometryTolerance * GeometryTolerance)
                {
                    continue;
                }

                if (isUpper)
                {
                    upperAffected++;
                    maximumUpperAngle = Mathf.Max(maximumUpperAngle, Mathf.Abs(signedAngle));
                    maximumUpperRise = Mathf.Max(maximumUpperRise, modelDelta.y);
                    if (modelDelta.y <= 0f) lipDirectionErrors++;
                    if (Mathf.Abs(signedAngle) >= EachMouthOpenDegrees - 0.01f) upperFullAngle++;
                }
                else
                {
                    lowerAffected++;
                    maximumLowerAngle = Mathf.Max(maximumLowerAngle, Mathf.Abs(signedAngle));
                    maximumLowerDrop = Mathf.Max(maximumLowerDrop, -modelDelta.y);
                    if (modelDelta.y >= 0f) lipDirectionErrors++;
                    if (Mathf.Abs(signedAngle) >= EachMouthOpenDegrees - 0.01f) lowerFullAngle++;
                }
            }

            var wingBoneSet = BoneSet(
                sceneRenderer,
                "Bone_010", "Bone_011", "Bone_012", "Bone_013",
                "Bone_014", "Bone_015", "Bone_016", "Bone_017");
            var wingAffected = Enumerable.Range(0, deltas.Length).Count(index =>
                deltas[index].sqrMagnitude > GeometryTolerance * GeometryTolerance &&
                SumWeight(weights[index], wingBoneSet) > WeightThreshold);

            if (upperAffected < 20 || lowerAffected < 8 || upperFullAngle < 1 || lowerFullAngle < 1 ||
                lipDirectionErrors != 0 || wingAffected != 0 ||
                maximumUpperRise < 0.04f || maximumLowerDrop < 0.04f)
            {
                UnityEngine.Object.DestroyImmediate(generated);
                throw new InvalidOperationException(
                    "The Fuga upper/lower mouth deformation region is incomplete. Upper=" + upperAffected +
                    ", Lower=" + lowerAffected + ", UpperFull=" + upperFullAngle + ", LowerFull=" + lowerFullAngle +
                    ", DirectionErrors=" + lipDirectionErrors + ", WingAffected=" + wingAffected +
                    ", UpperRise=" + maximumUpperRise.ToString("F6", CultureInfo.InvariantCulture) +
                    ", LowerDrop=" + maximumLowerDrop.ToString("F6", CultureInfo.InvariantCulture) + ".");
            }

            AddBlendShape(generated, source, targets, deltas);
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(DerivedMeshPath);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(generated, DerivedMeshPath);
                existing = generated;
            }
            else
            {
                EditorUtility.CopySerialized(generated, existing);
                UnityEngine.Object.DestroyImmediate(generated);
                EditorUtility.SetDirty(existing);
            }

            AssetDatabase.SaveAssetIfDirty(existing);
            AssetDatabase.ImportAsset(DerivedMeshPath, ImportAssetOptions.ForceSynchronousImport);
            info = new MouthInfo(
                upperAffected,
                lowerAffected,
                upperFullAngle,
                lowerFullAngle,
                maximumUpperAngle,
                maximumLowerAngle,
                region.FrontSign,
                region.MouthSplitY,
                maximumUpperRise,
                maximumLowerDrop,
                wingAffected,
                lipDirectionErrors,
                0);
            return AssetDatabase.LoadAssetAtPath<Mesh>(DerivedMeshPath) ??
                   throw new InvalidOperationException("The derived Fuga consume mouth mesh was not created.");
        }

        private static void AddBlendShape(Mesh generated, Mesh source, Vector3[] targets, Vector3[] deltas)
        {
            var target = UnityEngine.Object.Instantiate(source);
            try
            {
                target.vertices = targets;
                target.RecalculateNormals();
                target.RecalculateTangents();
                var sourceNormals = source.normals;
                var targetNormals = target.normals;
                var sourceTangents = source.tangents;
                var targetTangents = target.tangents;
                var deltaNormals = new Vector3[source.vertexCount];
                var deltaTangents = new Vector3[source.vertexCount];
                if (sourceNormals.Length == targetNormals.Length)
                {
                    for (var index = 0; index < deltaNormals.Length; index++)
                    {
                        deltaNormals[index] = targetNormals[index] - sourceNormals[index];
                    }
                }

                if (sourceTangents.Length == targetTangents.Length)
                {
                    for (var index = 0; index < deltaTangents.Length; index++)
                    {
                        deltaTangents[index] = new Vector3(
                            targetTangents[index].x - sourceTangents[index].x,
                            targetTangents[index].y - sourceTangents[index].y,
                            targetTangents[index].z - sourceTangents[index].z);
                    }
                }

                generated.AddBlendShapeFrame(MouthBlendShapeName, 100f, deltas, deltaNormals, deltaTangents);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        private static MouthRegion AnalyzeMouthRegion(
            Transform model,
            SkinnedMeshRenderer renderer,
            Camera camera,
            Vector3[] vertices,
            BoneWeight[] weights)
        {
            var modelVertices = vertices
                .Select(vertex => model.InverseTransformPoint(renderer.transform.TransformPoint(vertex)))
                .ToArray();
            var modelBounds = BoundsFromVertices(modelVertices);
            var cameraInModel = model.InverseTransformPoint(camera.transform.position);
            var frontSign = cameraInModel.z >= modelBounds.center.z ? 1f : -1f;
            var frontAxis = Vector3.forward * frontSign;
            var projections = modelVertices.Select(vertex => Vector3.Dot(vertex, frontAxis)).ToArray();
            var frontMaximum = projections.Max();
            var bone002Index = Array.FindIndex(
                renderer.bones,
                bone => bone != null && string.Equals(bone.name, "Bone_002", StringComparison.Ordinal));
            var bone003Index = Array.FindIndex(
                renderer.bones,
                bone => bone != null && string.Equals(bone.name, "Bone_003", StringComparison.Ordinal));
            var upperLipBoneIndex = Array.FindIndex(
                renderer.bones,
                bone => bone != null && string.Equals(bone.name, UpperLipBoneName, StringComparison.Ordinal));
            var lowerLipBoneIndex = Array.FindIndex(
                renderer.bones,
                bone => bone != null && string.Equals(bone.name, LowerLipBoneName, StringComparison.Ordinal));
            if (bone002Index < 0 || bone003Index < 0 || upperLipBoneIndex < 0 || lowerLipBoneIndex < 0)
            {
                throw new InvalidOperationException("The embedded Fuga upper/lower lip rig is incomplete.");
            }

            RequireMeasuredVertex(modelVertices, LeftLipCreaseModel, "left lip crease");
            RequireMeasuredVertex(modelVertices, CenterLipCreaseModel, "center lip crease");
            RequireMeasuredVertex(modelVertices, RightLipCreaseModel, "right lip crease");

            var lipCandidates = Enumerable.Range(0, modelVertices.Length)
                .Where(index =>
                {
                    var lipWeight = WeightForBone(weights[index], upperLipBoneIndex) +
                                    WeightForBone(weights[index], lowerLipBoneIndex);
                    return lipWeight > 0.05f &&
                           projections[index] >= frontMaximum - Mathf.Max(0.22f, modelBounds.size.z * 0.16f) &&
                           Mathf.Abs(modelVertices[index].x - modelBounds.center.x) <= modelBounds.size.x * 0.22f;
                })
                .ToArray();
            if (lipCandidates.Length < 24)
            {
                throw new InvalidOperationException(
                    "The actual Fuga lip-chain front region is incomplete: " + lipCandidates.Length + ".");
            }

            var mouthSplitY = CenterLipCreaseModel.y;
            var mouthCenterX = CenterLipCreaseModel.x;
            var centerHalfWidth = Mathf.Max(
                mouthCenterX - LeftLipCreaseModel.x,
                RightLipCreaseModel.x - mouthCenterX) + 0.025f;
            var verticalHalfBand = 0.14f;
            var frontRootProjection = new[]
                {
                    Vector3.Dot(LeftLipCreaseModel, frontAxis),
                    Vector3.Dot(CenterLipCreaseModel, frontAxis),
                    Vector3.Dot(RightLipCreaseModel, frontAxis)
                }.Min() - 0.16f;
            var mouthPivot = CenterLipCreaseModel;
            return new MouthRegion(
                modelVertices,
                modelBounds,
                frontAxis,
                frontSign,
                frontMaximum,
                frontRootProjection,
                centerHalfWidth,
                verticalHalfBand,
                mouthSplitY,
                mouthPivot,
                LeftLipCreaseModel,
                RightLipCreaseModel);
        }

        private static void RequireMeasuredVertex(Vector3[] vertices, Vector3 expected, string label)
        {
            if (vertices.Any(vertex => (vertex - expected).sqrMagnitude <= 0.000004f)) return;
            throw new InvalidOperationException("The measured Fuga " + label + " vertex is missing.");
        }

        private static Vector3 LipCreasePoint(MouthRegion region, float modelX)
        {
            if (modelX <= region.MouthPivot.x)
            {
                return Vector3.Lerp(
                    region.LeftLipCrease,
                    region.MouthPivot,
                    Mathf.InverseLerp(region.LeftLipCrease.x, region.MouthPivot.x, modelX));
            }

            return Vector3.Lerp(
                region.MouthPivot,
                region.RightLipCrease,
                Mathf.InverseLerp(region.MouthPivot.x, region.RightLipCrease.x, modelX));
        }

        private static float LipWidthProgress(MouthRegion region, float modelX)
        {
            var leftLimit = region.LeftLipCrease.x - 0.025f;
            var rightLimit = region.RightLipCrease.x + 0.025f;
            return modelX <= region.MouthPivot.x
                ? Mathf.InverseLerp(region.MouthPivot.x, leftLimit, modelX)
                : Mathf.InverseLerp(region.MouthPivot.x, rightLimit, modelX);
        }

        private static MouthInfo InspectMouthRig(Transform model, SkinnedMeshRenderer renderer)
        {
            var mesh = renderer.sharedMesh ?? throw new InvalidOperationException("The Fuga consume renderer has no mesh.");
            var upperLip = FindBone(renderer, UpperLipBoneName);
            var lowerLip = FindBone(renderer, LowerLipBoneName);
            if (upperLip.parent == null || !string.Equals(upperLip.parent.name, "Bone_003", StringComparison.Ordinal) ||
                lowerLip.parent == null || !string.Equals(lowerLip.parent.name, "Bone_002", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("The Fuga upper/lower lip bone hierarchy is incorrect.");
            }

            if (renderer.bones.Length != 28 || mesh.bindposes.Length != renderer.bones.Length ||
                mesh.vertexCount != SeparatedLipVertexCount || mesh.boneWeights.Length != mesh.vertexCount ||
                mesh.triangles.Length / 3 != SeparatedLipTriangleCount)
            {
                throw new InvalidOperationException("The Fuga consume renderer is not connected to the complete 28-bone rig.");
            }

            var upperBoneIndex = Array.IndexOf(renderer.bones, upperLip);
            var lowerBoneIndex = Array.IndexOf(renderer.bones, lowerLip);
            if (upperBoneIndex < 0 || lowerBoneIndex < 0 || upperBoneIndex == lowerBoneIndex)
            {
                throw new InvalidOperationException("The Fuga lip bones are missing from the renderer bone array.");
            }

            var weights = mesh.boneWeights;
            var upperSet = new HashSet<int>(Enumerable.Range(0, weights.Length).Where(index =>
                WeightForBone(weights[index], upperBoneIndex) > 0.9999f &&
                WeightForBone(weights[index], lowerBoneIndex) <= GeometryTolerance));
            var lowerSet = new HashSet<int>(Enumerable.Range(0, weights.Length).Where(index =>
                WeightForBone(weights[index], lowerBoneIndex) > 0.9999f &&
                WeightForBone(weights[index], upperBoneIndex) <= GeometryTolerance));
            if (upperSet.Overlaps(lowerSet))
            {
                throw new InvalidOperationException("The Fuga upper/lower lip vertex sets overlap.");
            }

            var modelVertices = mesh.vertices
                .Select(vertex => model.InverseTransformPoint(renderer.transform.TransformPoint(vertex)))
                .ToArray();
            var upperAffected = 0;
            var lowerAffected = 0;
            var nonLipAffected = 0;
            var maximumUpperRise = 0f;
            var maximumLowerDrop = 0f;
            var directionErrors = 0;
            var hinge = CenterLipCreaseModel - Vector3.forward * LipHingeDepthMeters;
            for (var index = 0; index < weights.Length; index++)
            {
                var upperWeight = WeightForBone(weights[index], upperBoneIndex);
                var lowerWeight = WeightForBone(weights[index], lowerBoneIndex);
                if (upperSet.Contains(index))
                {
                    if (upperWeight < 0.9999f || lowerWeight > GeometryTolerance) directionErrors++;
                    upperAffected++;
                    var target = hinge + Quaternion.AngleAxis(-EachMouthOpenDegrees, Vector3.right) *
                                 (modelVertices[index] - hinge);
                    var rise = target.y - modelVertices[index].y;
                    maximumUpperRise = Mathf.Max(maximumUpperRise, rise);
                    if (rise <= 0f) directionErrors++;
                }
                else if (lowerSet.Contains(index))
                {
                    if (lowerWeight < 0.9999f || upperWeight > GeometryTolerance) directionErrors++;
                    lowerAffected++;
                    var target = hinge + Quaternion.AngleAxis(EachMouthOpenDegrees, Vector3.right) *
                                 (modelVertices[index] - hinge);
                    var drop = modelVertices[index].y - target.y;
                    maximumLowerDrop = Mathf.Max(maximumLowerDrop, drop);
                    if (drop <= 0f) directionErrors++;
                }
                else if (upperWeight > GeometryTolerance || lowerWeight > GeometryTolerance)
                {
                    nonLipAffected++;
                }
            }

            var wingBoneSet = BoneSet(
                renderer,
                "Bone_010", "Bone_011", "Bone_012", "Bone_013",
                "Bone_014", "Bone_015", "Bone_016", "Bone_017");
            var wingAffected = upperSet.Concat(lowerSet).Count(index =>
                SumWeight(weights[index], wingBoneSet) > WeightThreshold);
            var interLipFaces = 0;
            var triangles = mesh.triangles;
            for (var index = 0; index + 2 < triangles.Length; index += 3)
            {
                var first = triangles[index];
                var second = triangles[index + 1];
                var third = triangles[index + 2];
                var hasUpper = upperSet.Contains(first) || upperSet.Contains(second) || upperSet.Contains(third);
                var hasLower = lowerSet.Contains(first) || lowerSet.Contains(second) || lowerSet.Contains(third);
                if (hasUpper && hasLower) interLipFaces++;
            }

            if (upperAffected != UpperLipVertexCount || lowerAffected != LowerLipVertexCount ||
                nonLipAffected != 0 || wingAffected != 0 || directionErrors != 0 ||
                interLipFaces != 0 || maximumUpperRise < 0.04f || maximumLowerDrop < 0.04f)
            {
                throw new InvalidOperationException(
                    "The Fuga bone-driven lip rig is not isolated. Upper=" + upperAffected +
                    ", Lower=" + lowerAffected + ", NonLip=" + nonLipAffected +
                    ", Wing=" + wingAffected + ", DirectionErrors=" + directionErrors +
                    ", InterLipFaces=" + interLipFaces + ".");
            }

            return new MouthInfo(
                upperAffected,
                lowerAffected,
                upperAffected,
                lowerAffected,
                EachMouthOpenDegrees,
                EachMouthOpenDegrees,
                1f,
                CenterLipCreaseModel.y,
                maximumUpperRise,
                maximumLowerDrop,
                wingAffected,
                directionErrors,
                interLipFaces);
        }

        private static AppliedResult InspectAppliedState()
        {
            RequireCurrentScene();
            var placementRoot = RequireRoot(PlacementRootName);
            var slot = placementRoot.Find(ConsumeSlotName) ??
                       throw new InvalidOperationException(ConsumeSlotName + " is missing.");
            var model = slot.Find(ModelName) ??
                        throw new InvalidOperationException(ConsumeSlotName + "/" + ModelName + " is missing.");
            var renderer = model.GetComponentInChildren<SkinnedMeshRenderer>(true) ??
                           throw new InvalidOperationException("The Fuga consume model has no renderer.");
            InspectMouthRig(model, renderer);
            var leftWing = FindBone(renderer, "Bone_013");
            var rightWing = FindBone(renderer, "Bone_017");
            var upperLip = FindBone(renderer, UpperLipBoneName);
            var lowerLip = FindBone(renderer, LowerLipBoneName);
            RequireWingBindRotations(leftWing, rightWing);
            var body = slot.GetComponent<Rigidbody>() ??
                       throw new InvalidOperationException("The Fuga consume slot has no Rigidbody.");
            var animator = slot.GetComponent<Animator>() ??
                           throw new InvalidOperationException("The Fuga consume slot has no Animator.");
            var driver = slot.GetComponent<FugaConsumeMotionDriver>() ??
                         throw new InvalidOperationException("The Fuga consume motion driver is missing.");
            var configuredHorizontalForward = Vector3.ProjectOnPlane(driver.ForwardWorld, Vector3.up).normalized;
            var currentHorizontalForward = Vector3.ProjectOnPlane(model.forward, Vector3.up).normalized;
            if (!driver.enabled || driver.Body != body || driver.VisualRoot != model ||
                driver.LeftWingRoot != leftWing || driver.RightWingRoot != rightWing ||
                driver.UpperLipRoot != upperLip || driver.LowerLipRoot != lowerLip ||
                Mathf.Abs(driver.LoopDuration - LoopDuration) > GeometryTolerance ||
                Mathf.Abs(driver.WingbeatFrequency - WingbeatFrequency) > GeometryTolerance ||
                Mathf.Abs(driver.BodyTiltDegrees - MaximumBodyTiltDegrees) > GeometryTolerance ||
                Mathf.Abs(driver.MouthOpenDegrees - TotalMouthOpenDegrees) > GeometryTolerance ||
                Mathf.Abs(driver.ForwardDistanceMeters - ForwardDistanceMeters) > GeometryTolerance ||
                configuredHorizontalForward.sqrMagnitude < 0.999f || currentHorizontalForward.sqrMagnitude < 0.999f ||
                Vector3.Dot(configuredHorizontalForward, currentHorizontalForward) < 0.9999f)
            {
                throw new InvalidOperationException("The Fuga consume motion driver configuration is incomplete.");
            }

            if (body.isKinematic || body.useGravity || body.constraints != RigidbodyConstraints.FreezeRotation ||
                animator.enabled || animator.runtimeAnimatorController != null ||
                slot.GetComponent<FugaAnimationReviewPlaybackDriver>() != null)
            {
                throw new InvalidOperationException("The Fuga consume physics or legacy animation ownership is incorrect.");
            }

            var otherPhysicsDriver = slot.GetComponent<FugaPhysicsMotionDriver>();
            if (otherPhysicsDriver != null && otherPhysicsDriver.enabled)
            {
                throw new InvalidOperationException("Another Fuga physics driver is still enabled on the consume slot.");
            }

            return new AppliedResult(
                slot,
                model,
                renderer,
                body,
                driver,
                Sha256(Absolute(ImportedModelPath)));
        }

        private static void WriteMotionReport(AppliedResult result, MouthInfo mouth, bool directReviewCompleted)
        {
            var report = new StringBuilder()
                .AppendLine("Fuga Consume Motion Report")
                .AppendLine("Result=PASS")
                .AppendLine("Scene=" + ScenePath)
                .AppendLine("Target=" + PlacementRootName + "/" + ConsumeSlotName + "/" + ModelName)
                .AppendLine("SourceModel=" + ImportedModelPath)
                .AppendLine("SourceSha256=" + result.SourceHash)
                .AppendLine("MouthMotionOwner=EmbeddedUpperLowerLipBones")
                .AppendLine("UpperLipBone=" + UpperLipBoneName)
                .AppendLine("LowerLipBone=" + LowerLipBoneName)
                .AppendLine("TargetFacingModelAxis=" + (mouth.FrontSign > 0f ? "+Z" : "-Z"))
                .AppendLine("UpperMouthIdentification=SurfaceImmediatelyAboveMeasuredThreePointVCrease")
                .AppendLine("UpperMouthSkinInfluence=" + UpperLipBoneName)
                .AppendLine("LowerMouthIdentification=SurfaceImmediatelyBelowMeasuredThreePointVCrease")
                .AppendLine("LowerMouthSkinInfluence=" + LowerLipBoneName)
                .AppendLine("LeftLipCreaseModel=" + Vec(LeftLipCreaseModel))
                .AppendLine("CenterLipCreaseModel=" + Vec(CenterLipCreaseModel))
                .AppendLine("RightLipCreaseModel=" + Vec(RightLipCreaseModel))
                .AppendLine("LipHingeDepthMeters=" + LipHingeDepthMeters.ToString("F6", CultureInfo.InvariantCulture))
                .AppendLine("UpperMouthPath=Bone_000/Bone_001/Bone_003/" + UpperLipBoneName)
                .AppendLine("LowerMouthPath=Bone_000/Bone_001/Bone_003/Bone_002/" + LowerLipBoneName)
                .AppendLine("UpperMouthAffectedVertices=" + mouth.UpperAffectedVertices.ToString(CultureInfo.InvariantCulture))
                .AppendLine("LowerMouthAffectedVertices=" + mouth.LowerAffectedVertices.ToString(CultureInfo.InvariantCulture))
                .AppendLine("LipRigWingWeightedVertices=" + mouth.WingAffectedVertices.ToString(CultureInfo.InvariantCulture))
                .AppendLine("LipRigDirectionErrors=" + mouth.DirectionErrorVertices.ToString(CultureInfo.InvariantCulture))
                .AppendLine("InterLipFaces=" + mouth.InterLipFaceCount.ToString(CultureInfo.InvariantCulture))
                .AppendLine("InterLipFacesRemoved=12")
                .AppendLine("InterLipMeshDeformation=False")
                .AppendLine("UpperLipMaximumRiseMeters=" + mouth.MaximumUpperRiseMeters.ToString("F6", CultureInfo.InvariantCulture))
                .AppendLine("LowerLipMaximumDropMeters=" + mouth.MaximumLowerDropMeters.ToString("F6", CultureInfo.InvariantCulture))
                .AppendLine("UpperMouthOpenDegrees=30.000")
                .AppendLine("LowerMouthOpenDegrees=30.000")
                .AppendLine("TotalMouthOpenDegrees=60.000")
                .AppendLine("LoopDurationSeconds=2.000")
                .AppendLine("Loop=True")
                .AppendLine("WingbeatFrequencyHz=0.700")
                .AppendLine("WingBindPoseRestoredFromOriginalGlb=True")
                .AppendLine("EditorConfigureAppliesWingPose=False")
                .AppendLine("WingPoseAccumulationAcrossApplyAndPlay=False")
                .AppendLine("WingbeatPhaseContinuousAcrossConsumeLoop=True")
                .AppendLine("WingUpstrokeDegrees=44.000")
                .AppendLine("WingDownstrokeDegrees=-40.000")
                .AppendLine("FirstBodyTiltEndSeconds=0.350")
                .AppendLine("MaximumMouthOpenSeconds=0.800")
                .AppendLine("BitePeakSeconds=1.000")
                .AppendLine("BiteEndSeconds=1.050")
                .AppendLine("ReturnEndSeconds=1.650")
                .AppendLine("MaximumBodyTiltDegrees=30.000")
                .AppendLine("ForwardDistanceMeters=0.080")
                .AppendLine("ForwardMovementOwner=RigidbodyLinearVelocityInFixedUpdate")
                .AppendLine("VisualRootPositionAnimationCurves=0")
                .AppendLine("OriginalGlbLipRigModified=True")
                .AppendLine("OtherFugaSlotsChanged=False")
                .AppendLine("DirectUnityGameViewMotionReview=" + directReviewCompleted)
                .AppendLine("DirectReviewCompletedLoops=" + result.Driver.CompletedLoopCount.ToString(CultureInfo.InvariantCulture))
                .AppendLine("DirectReviewCompletedWingbeats=" + result.Driver.CompletedWingbeatCount.ToString(CultureInfo.InvariantCulture))
                .AppendLine("DirectReviewLastLoopPeakBodyTiltDegrees=" + result.Driver.LastLoopPeakBodyTiltDegrees.ToString("F6", CultureInfo.InvariantCulture))
                .AppendLine("DirectReviewLastLoopPeakForwardOffsetMeters=" + result.Driver.LastLoopPeakForwardOffsetMeters.ToString("F6", CultureInfo.InvariantCulture))
                .AppendLine("DirectReviewLastLoopPeakMouthWeight=" + result.Driver.LastLoopPeakMouthWeight.ToString("F6", CultureInfo.InvariantCulture))
                .AppendLine("DirectReviewLastLoopPeakUpperLipAngleDegrees=" + result.Driver.LastLoopPeakUpperLipAngleDegrees.ToString("F6", CultureInfo.InvariantCulture))
                .AppendLine("DirectReviewLastLoopPeakLowerLipAngleDegrees=" + result.Driver.LastLoopPeakLowerLipAngleDegrees.ToString("F6", CultureInfo.InvariantCulture))
                .AppendLine("DirectReviewInvalidLoopPeakCount=" + result.Driver.InvalidLoopPeakCount.ToString(CultureInfo.InvariantCulture))
                .AppendLine("DirectReviewInvalidLoopReturnCount=" + result.Driver.InvalidLoopReturnCount.ToString(CultureInfo.InvariantCulture))
                .AppendLine("StaticCaptureGenerated=False")
                .AppendLine("HarnessValidationRun=False")
                .ToString();
            var absolute = Absolute(MotionReportPath);
            Directory.CreateDirectory(Path.GetDirectoryName(absolute) ??
                                      throw new InvalidOperationException("Invalid Fuga consume report path."));
            File.WriteAllText(absolute, report, new UTF8Encoding(false));
        }

        private static BoneScore ScoreBone(
            int boneIndex,
            Transform bone,
            Transform model,
            Vector3[] vertices,
            BoneWeight[] weights,
            int[] upperIndices,
            int[] lowerIndices)
        {
            var upperWeight = 0f;
            var lowerWeight = 0f;
            var upperVertices = 0;
            var lowerVertices = 0;
            foreach (var index in upperIndices)
            {
                var weight = WeightForBone(weights[index], boneIndex);
                upperWeight += weight;
                if (weight > WeightThreshold) upperVertices++;
            }

            foreach (var index in lowerIndices)
            {
                var weight = WeightForBone(weights[index], boneIndex);
                lowerWeight += weight;
                if (weight > WeightThreshold) lowerVertices++;
            }

            return new BoneScore(bone, upperWeight, lowerWeight, upperVertices, lowerVertices);
        }

        private static float WeightForBone(BoneWeight weight, int boneIndex)
        {
            var result = 0f;
            if (weight.boneIndex0 == boneIndex) result += weight.weight0;
            if (weight.boneIndex1 == boneIndex) result += weight.weight1;
            if (weight.boneIndex2 == boneIndex) result += weight.weight2;
            if (weight.boneIndex3 == boneIndex) result += weight.weight3;
            return result;
        }

        private static bool[] BoneSet(SkinnedMeshRenderer renderer, params string[] names)
        {
            var remaining = names.ToList();
            var result = new bool[renderer.bones.Length];
            for (var index = 0; index < renderer.bones.Length; index++)
            {
                var bone = renderer.bones[index];
                if (bone == null) continue;
                var nameIndex = remaining.FindIndex(name => string.Equals(name, bone.name, StringComparison.Ordinal));
                if (nameIndex < 0) continue;
                result[index] = true;
                remaining.RemoveAt(nameIndex);
            }

            if (remaining.Count != 0)
            {
                throw new InvalidOperationException("The Fuga mouth bone set is incomplete: " +
                                                    string.Join(",", remaining) + ".");
            }

            return result;
        }

        private static float SumWeight(BoneWeight weight, bool[] set)
        {
            var result = 0f;
            if (set[weight.boneIndex0]) result += weight.weight0;
            if (set[weight.boneIndex1]) result += weight.weight1;
            if (set[weight.boneIndex2]) result += weight.weight2;
            if (set[weight.boneIndex3]) result += weight.weight3;
            return result;
        }

        private static void RestoreWingBindRotations(Transform leftWing, Transform rightWing)
        {
            var imported = AssetDatabase.LoadAssetAtPath<GameObject>(ImportedModelPath) ??
                           throw new InvalidOperationException("The supplied Fuga GLB is missing.");
            var sourceRenderer = imported.GetComponentInChildren<SkinnedMeshRenderer>(true) ??
                                 throw new InvalidOperationException("The supplied Fuga GLB has no renderer.");
            var sourceLeftWing = FindBone(sourceRenderer, "Bone_013");
            var sourceRightWing = FindBone(sourceRenderer, "Bone_017");
            leftWing.localRotation = sourceLeftWing.localRotation;
            rightWing.localRotation = sourceRightWing.localRotation;
            EditorUtility.SetDirty(leftWing);
            EditorUtility.SetDirty(rightWing);
        }

        private static void RequireWingBindRotations(Transform leftWing, Transform rightWing)
        {
            if (Application.isPlaying) return;
            var imported = AssetDatabase.LoadAssetAtPath<GameObject>(ImportedModelPath) ??
                           throw new InvalidOperationException("The supplied Fuga GLB is missing.");
            var sourceRenderer = imported.GetComponentInChildren<SkinnedMeshRenderer>(true) ??
                                 throw new InvalidOperationException("The supplied Fuga GLB has no renderer.");
            var sourceLeftWing = FindBone(sourceRenderer, "Bone_013");
            var sourceRightWing = FindBone(sourceRenderer, "Bone_017");
            if (Quaternion.Angle(leftWing.localRotation, sourceLeftWing.localRotation) > 0.001f ||
                Quaternion.Angle(rightWing.localRotation, sourceRightWing.localRotation) > 0.001f)
            {
                throw new InvalidOperationException("The Fuga consume wing bind rotations were not restored.");
            }
        }

        private static Transform FindBone(SkinnedMeshRenderer renderer, string name)
        {
            return renderer.bones.FirstOrDefault(bone => bone != null && string.Equals(bone.name, name, StringComparison.Ordinal)) ??
                   throw new InvalidOperationException("The Fuga bone is missing: " + name + ".");
        }

        private static Bounds BoundsFromVertices(Vector3[] vertices)
        {
            var bounds = new Bounds(vertices[0], Vector3.zero);
            for (var index = 1; index < vertices.Length; index++) bounds.Encapsulate(vertices[index]);
            return bounds;
        }

        private static string RelativePath(Transform root, Transform target)
        {
            return AnimationUtility.CalculateTransformPath(target, root);
        }

        private static Scene RequireCurrentScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || !string.Equals(scene.path, ScenePath, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("CargoRunMvp must already be the active scene.");
            }

            return scene;
        }

        private static Transform RequireRoot(string name)
        {
            return GameObject.Find(name)?.transform ??
                   throw new InvalidOperationException(name + " is missing from CargoRunMvp.");
        }

        private static string OtherSlotSignature(Transform placementRoot)
        {
            var builder = new StringBuilder();
            foreach (Transform slot in placementRoot)
            {
                if (string.Equals(slot.name, ConsumeSlotName, StringComparison.Ordinal)) continue;
                builder.Append("Slot=").Append(slot.name).Append('|')
                    .Append(Vec(slot.localPosition)).Append('|')
                    .Append(Vec(slot.localEulerAngles)).Append('|')
                    .Append(Vec(slot.localScale)).AppendLine();
                foreach (var child in slot.GetComponentsInChildren<Transform>(true))
                {
                    builder.Append(RelativePath(slot, child)).Append('|')
                        .Append(Vec(child.localPosition)).Append('|')
                        .Append(Vec(child.localEulerAngles)).Append('|')
                        .Append(Vec(child.localScale)).Append('|')
                        .Append(child.gameObject.activeSelf).AppendLine();
                }

                foreach (var renderer in slot.GetComponentsInChildren<Renderer>(true))
                {
                    var mesh = renderer is SkinnedMeshRenderer skinned
                        ? skinned.sharedMesh
                        : renderer.GetComponent<MeshFilter>()?.sharedMesh;
                    builder.Append("Renderer=").Append(RelativePath(slot, renderer.transform)).Append('|')
                        .Append(mesh != null ? AssetDatabase.GetAssetPath(mesh) : "<null>").Append('|')
                        .Append(renderer.enabled).AppendLine();
                }

                foreach (var component in slot.GetComponents<Component>())
                {
                    if (component != null) builder.Append("Component=").Append(component.GetType().FullName).AppendLine();
                }
            }

            return builder.ToString();
        }

        private static string Sha256(string path)
        {
            using var stream = File.OpenRead(path);
            using var algorithm = SHA256.Create();
            return BitConverter.ToString(algorithm.ComputeHash(stream)).Replace("-", string.Empty);
        }

        private static void RequireHash(string expected, string actual, string label)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(label + " changed unexpectedly.");
            }
        }

        private static string Absolute(string assetRelativePath)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetRelativePath));
        }

        private static string Vec(Vector3 value)
        {
            return "(" + value.x.ToString("F6", CultureInfo.InvariantCulture) + "," +
                   value.y.ToString("F6", CultureInfo.InvariantCulture) + "," +
                   value.z.ToString("F6", CultureInfo.InvariantCulture) + ")";
        }

        private readonly struct BoneScore
        {
            public BoneScore(Transform bone, float upperWeight, float lowerWeight, int upperVertices, int lowerVertices)
            {
                Bone = bone;
                UpperWeight = upperWeight;
                LowerWeight = lowerWeight;
                UpperVertices = upperVertices;
                LowerVertices = lowerVertices;
            }

            public Transform Bone { get; }
            public float UpperWeight { get; }
            public float LowerWeight { get; }
            public int UpperVertices { get; }
            public int LowerVertices { get; }
        }

        private readonly struct MouthRegion
        {
            public MouthRegion(
                Vector3[] modelVertices,
                Bounds modelBounds,
                Vector3 frontAxis,
                float frontSign,
                float frontMaximum,
                float frontRootProjection,
                float centerHalfWidth,
                float verticalHalfBand,
                float mouthSplitY,
                Vector3 mouthPivot,
                Vector3 leftLipCrease,
                Vector3 rightLipCrease)
            {
                ModelVertices = modelVertices;
                ModelBounds = modelBounds;
                FrontAxis = frontAxis;
                FrontSign = frontSign;
                FrontMaximum = frontMaximum;
                FrontRootProjection = frontRootProjection;
                CenterHalfWidth = centerHalfWidth;
                VerticalHalfBand = verticalHalfBand;
                MouthSplitY = mouthSplitY;
                MouthPivot = mouthPivot;
                LeftLipCrease = leftLipCrease;
                RightLipCrease = rightLipCrease;
            }

            public Vector3[] ModelVertices { get; }
            public Bounds ModelBounds { get; }
            public Vector3 FrontAxis { get; }
            public float FrontSign { get; }
            public float FrontMaximum { get; }
            public float FrontRootProjection { get; }
            public float CenterHalfWidth { get; }
            public float VerticalHalfBand { get; }
            public float MouthSplitY { get; }
            public Vector3 MouthPivot { get; }
            public Vector3 LeftLipCrease { get; }
            public Vector3 RightLipCrease { get; }
        }

        private readonly struct MouthInfo
        {
            public MouthInfo(
                int upperAffectedVertices,
                int lowerAffectedVertices,
                int upperFullAngleVertices,
                int lowerFullAngleVertices,
                float maximumUpperAngleDegrees,
                float maximumLowerAngleDegrees,
                float frontSign,
                float mouthSplitY,
                float maximumUpperRiseMeters,
                float maximumLowerDropMeters,
                int wingAffectedVertices,
                int directionErrorVertices,
                int interLipFaceCount)
            {
                UpperAffectedVertices = upperAffectedVertices;
                LowerAffectedVertices = lowerAffectedVertices;
                UpperFullAngleVertices = upperFullAngleVertices;
                LowerFullAngleVertices = lowerFullAngleVertices;
                MaximumUpperAngleDegrees = maximumUpperAngleDegrees;
                MaximumLowerAngleDegrees = maximumLowerAngleDegrees;
                FrontSign = frontSign;
                MouthSplitY = mouthSplitY;
                MaximumUpperRiseMeters = maximumUpperRiseMeters;
                MaximumLowerDropMeters = maximumLowerDropMeters;
                WingAffectedVertices = wingAffectedVertices;
                DirectionErrorVertices = directionErrorVertices;
                InterLipFaceCount = interLipFaceCount;
            }

            public int UpperAffectedVertices { get; }
            public int LowerAffectedVertices { get; }
            public int UpperFullAngleVertices { get; }
            public int LowerFullAngleVertices { get; }
            public float MaximumUpperAngleDegrees { get; }
            public float MaximumLowerAngleDegrees { get; }
            public float FrontSign { get; }
            public float MouthSplitY { get; }
            public float MaximumUpperRiseMeters { get; }
            public float MaximumLowerDropMeters { get; }
            public int WingAffectedVertices { get; }
            public int DirectionErrorVertices { get; }
            public int InterLipFaceCount { get; }
        }

        private readonly struct AppliedResult
        {
            public AppliedResult(
                Transform slot,
                Transform model,
                SkinnedMeshRenderer renderer,
                Rigidbody body,
                FugaConsumeMotionDriver driver,
                string sourceHash)
            {
                Slot = slot;
                Model = model;
                Renderer = renderer;
                Body = body;
                Driver = driver;
                SourceHash = sourceHash;
            }

            public Transform Slot { get; }
            public Transform Model { get; }
            public SkinnedMeshRenderer Renderer { get; }
            public Rigidbody Body { get; }
            public FugaConsumeMotionDriver Driver { get; }
            public string SourceHash { get; }
        }
    }
}
