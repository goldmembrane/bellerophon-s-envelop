using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Bellerophon.Enemies.Parvum;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.ParvumCargoRunScene
{
    internal static class ParvumMoveMotionTool
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string ParvumRootName = "Approved Parvum Enemy Placement";
        private const string MoveSlotName = "Parvum_02_Move";
        private const string ModelName = "Parvum_Model";
        private const string MotionTargetName = "MotionPath_Target_Rigidbody_Goal";
        private const string SourceModelPath = "Assets/_Project/Art/Enemies/Parvum/Models/parvum.glb";
        private const string ReferenceClipPath = "Assets/_Project/Art/Enemies/Parvum/Animations/Parvum_Move.anim";
        private const string ReferenceMeshPath =
            "Assets/_Project/Art/Enemies/Parvum/Models/parvum_runtime_blendshape_mesh.asset";
        private const string GeneratedMeshPath =
            "Assets/_Project/Art/Enemies/Parvum/Models/parvum_move_forward_slime_mesh.asset";
        private const string ClipPath =
            "Assets/_Project/Art/Enemies/Parvum/Animations/Parvum_Move_NewModel.anim";
        private const string ControllerPath =
            "Assets/_Project/Art/Enemies/Parvum/Animations/Controllers/Parvum_Move_NewModel_Controller.controller";
        private const string ReferenceBlendShapeName = "Move_Squash_Forward_Slosh";
        private const string BlendShapeName = "Move_Forward_Slime_Push_NewModel";
        private const string MouthRootSurfaceBlendShapeName = "Move_Upper_Lower_Mouth_Roots_Close";
        private const string LegacyMouthBlendShapeName = "Move_Mouth_Close_70pct";
        private const string UpperMouthRootBoneName = "Bone_002";
        private const string LowerMouthRootBoneName = "Bone_018";
        private const string LowerJawRootBoneName = "Bone_016";
        private const string LowerJawAnchorBoneName = "Bone_011";
        private const string UpperJawLeftBoneName = "Bone_009";
        private const string UpperJawRightBoneName = "Bone_010";
        private const string InnerMouthRootBoneName = "Bone_008";
        private const string ReferenceMotionTargetPath =
            "Parvum_Physics_Motion_Helper_Targets/MotionPath_Target_Rigidbody_Goal";
        private const string OutputFolder = "docs/validation/parvum_move_motion_2026-08-14";
        private const string ReportPath = OutputFolder + "/Parvum_Move_Motion_Report.txt";
        private const string CapturePath = OutputFolder + "/Parvum_Move_Motion_Final_Comparison.png";
        private const string RigIdentificationCapturePath =
            OutputFolder + "/Parvum_Move_Mouth_Root_Rig_Identification.png";
        private const string ExpectedSourceSha256 =
            "E27840896F1DFA15BEE6F45F2BA943D28375A485E141907283CF79446B5640AB";

        // These ratios come from the previous Move_Squash_Forward_Slosh motion and are remapped to the new GLB axes.
        private const float LateralSquashRatio = 0.035f;
        private const float ForwardReachExtentRatio = 0.12f;
        private const float VerticalCompressionRatio = 0.08f;
        private const float ApprovedCycleSeconds = 3f;
        private const float ApprovedMaximumForwardPulse = 0.6f;
        private const float ApprovedMaximumLateralRadius = 0.15f;
        private const float MouthClosureRatio = 0.7f;
        private const float MaximumJawSearchDegrees = 80f;
        private const float JawSearchStepDegrees = 0.25f;
        private const float GeometryTolerance = 0.0001f;
        private const float GroundTolerance = 0.002f;
        private const int ReviewLayer = 31;
        private const int PanelWidth = 480;
        private const int CaptureHeight = 720;
        private const int RigIdentificationPanelSize = 480;

        private static readonly float[] CaptureTimes = { 0f, 0.66f, 1.5f, 2.34f, 3f };
        private static readonly string[] ToothBranchRootBoneNames =
            { "Bone_009", "Bone_010", "Bone_020", "Bone_022", "Bone_024", "Bone_026" };

        [MenuItem("Bellerophon/Enemies/Parvum/Apply New-Model Forward Slime Move")]
        public static void ApplyParvumMoveMotion()
        {
            var scene = RequireCurrentScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp has unsaved editor changes; Parvum move motion was not applied.");
            }

            RequireSourceHash();
            var reference = ReadReferenceMotion();
            var parvumRoot = RequireRoot(ParvumRootName).transform;
            var moveSlot = RequireDirectChild(parvumRoot, MoveSlotName);
            var model = RequireDirectChild(moveSlot, ModelName);
            var renderer = RequireSingleBodyRenderer(model);
            var sourceRenderer = RequireSourceRenderer();
            RequireCompatibleSource(renderer, sourceRenderer);
            var motionTarget = FindChildRecursive(moveSlot, MotionTargetName) ??
                               throw new InvalidOperationException("Parvum move Motion Path target is missing.");
            var driver = RequireReviewPhysics(moveSlot, motionTarget);

            var protectedBefore = ProtectedRootSignatures(scene);
            var otherSlotsBefore = OtherParvumSlotSignatures(parvumRoot);
            var moveTransformBefore = TransformSignature(moveSlot);
            var modelTransformBefore = TransformSignature(model);

            var generatedMesh = EnsureGeneratedMesh(sourceRenderer);
            renderer.sharedMesh = generatedMesh;
            renderer.localBounds = generatedMesh.bounds;
            renderer.SetBlendShapeWeight(generatedMesh.GetBlendShapeIndex(BlendShapeName), reference.ShapeWeights[0]);

            var animator = moveSlot.GetComponent<Animator>();
            if (animator == null)
            {
                animator = moveSlot.gameObject.AddComponent<Animator>();
            }

            var mouthRigPose = SolveMouthRigPose(model, renderer, animator, reference);
            AddMouthRootSurfaceBlendShape(renderer);
            renderer.localBounds = renderer.sharedMesh.bounds;
            renderer.SetBlendShapeWeight(
                renderer.sharedMesh.GetBlendShapeIndex(MouthRootSurfaceBlendShapeName),
                0f);
            var clip = EnsureClip(moveSlot, renderer, motionTarget, reference, mouthRigPose);
            var controller = EnsureController(clip);

            var otherConfiguredAnimators = moveSlot.GetComponentsInChildren<Animator>(true)
                .Where(candidate => candidate != animator && candidate.runtimeAnimatorController != null)
                .ToArray();
            if (otherConfiguredAnimators.Length > 0)
            {
                throw new InvalidOperationException(
                    "Parvum move contains an unexpected additional configured Animator: " +
                    otherConfiguredAnimators[0].name + ".");
            }

            animator.runtimeAnimatorController = controller;
            animator.avatar = null;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.enabled = true;

            var result = InspectState(
                parvumRoot,
                moveSlot,
                model,
                renderer,
                animator,
                clip,
                controller,
                motionTarget,
                driver,
                reference,
                mouthRigPose);
            if (!string.Equals(moveTransformBefore, TransformSignature(moveSlot), StringComparison.Ordinal) ||
                !string.Equals(modelTransformBefore, TransformSignature(model), StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Parvum move root or model Transform changed during move setup.");
            }

            if (!otherSlotsBefore.SequenceEqual(OtherParvumSlotSignatures(parvumRoot), StringComparer.Ordinal))
            {
                throw new InvalidOperationException("A non-move Parvum slot changed during move setup.");
            }

            if (!protectedBefore.SequenceEqual(ProtectedRootSignatures(scene), StringComparer.Ordinal))
            {
                throw new InvalidOperationException("A scene root outside Parvum changed during move setup.");
            }

            EditorUtility.SetDirty(renderer);
            EditorUtility.SetDirty(animator);
            EditorUtility.SetDirty(driver);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException("CargoRunMvp could not be saved after applying Parvum move motion.");
            }

            AssetDatabase.SaveAssets();
            WriteReport(result, captureCreated: false);
            Debug.Log(
                "ParvumMoveMotionApplied Result=PASS" +
                ", Target=" + ParvumRootName + "/" + MoveSlotName + "/" + ModelName +
                ", Vertices=" + result.VertexCount.ToString(CultureInfo.InvariantCulture) +
                ", AffectedVertices=" + result.AffectedVertexCount.ToString(CultureInfo.InvariantCulture) +
                ", MouthAffectedVertices=" + result.MouthAffectedVertexCount.ToString(CultureInfo.InvariantCulture) +
                ", UpperLowerMouthRootAffectedVertices=" + result.BodySideMouthRootAffectedVertexCount.ToString(CultureInfo.InvariantCulture) +
                ", InnerMouthAffectedVertices=" + result.InnerMouthAffectedVertexCount.ToString(CultureInfo.InvariantCulture) +
                ", MouthClosurePercent=" + Num(result.MouthClosurePercent) +
                ", UpperLowerMouthRootTravel=" + Num(result.BodySideMouthRootMaximumTravel) +
                ", InnerMouthTravel=" + Num(result.InnerMouthTravelDistance) +
                ", CycleSeconds=" + Num(result.CycleSeconds) +
                ", LocalForward=+Z" +
                ", MaxShapeForward=" + Num(result.MaximumShapeForward) +
                ", MotionTargetPulse=" + Num(result.MaximumReferenceTargetPulse) +
                ", LateralRadius=" + Num(ApprovedMaximumLateralRadius) +
                ", GroundDelta=" + Num(result.WorldGroundDelta) +
                ", RigidbodyTargetFollow=True" +
                ", ReviewRootLocked=True" +
                ", OtherParvumSlotsChanged=False" +
                ", OtherSceneRootsChanged=False" +
                ", SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Parvum/Inspect New-Model Forward Slime Move")]
        public static void InspectParvumMoveMotion()
        {
            var scene = RequireCurrentScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException("CargoRunMvp must be saved before inspecting Parvum move motion.");
            }

            RequireSourceHash();
            var reference = ReadReferenceMotion();
            LogBoneInfluenceSummary(RequireSourceRenderer());
            var parvumRoot = RequireRoot(ParvumRootName).transform;
            var moveSlot = RequireDirectChild(parvumRoot, MoveSlotName);
            var model = RequireDirectChild(moveSlot, ModelName);
            var renderer = RequireSingleBodyRenderer(model);
            var animator = moveSlot.GetComponent<Animator>() ??
                           throw new InvalidOperationException("Parvum move Animator is missing.");
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath) ??
                       throw new InvalidOperationException("New-model Parvum move clip is missing.");
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) ??
                             throw new InvalidOperationException("New-model Parvum move controller is missing.");
            var motionTarget = FindChildRecursive(moveSlot, MotionTargetName) ??
                               throw new InvalidOperationException("Parvum move Motion Path target is missing.");
            var driver = RequireReviewPhysics(moveSlot, motionTarget);
            var mouthRigPose = ReadMouthRigPose(moveSlot, model, renderer, animator, clip, reference);
            var result = InspectState(
                parvumRoot,
                moveSlot,
                model,
                renderer,
                animator,
                clip,
                controller,
                motionTarget,
                driver,
                reference,
                mouthRigPose);
            WriteReport(result, File.Exists(Absolute(CapturePath)));

            Debug.Log(
                "ParvumMoveMotionInspected Result=PASS" +
                ", Vertices=" + result.VertexCount.ToString(CultureInfo.InvariantCulture) +
                ", AffectedVertices=" + result.AffectedVertexCount.ToString(CultureInfo.InvariantCulture) +
                ", MouthAffectedVertices=" + result.MouthAffectedVertexCount.ToString(CultureInfo.InvariantCulture) +
                ", UpperLowerMouthRootAffectedVertices=" + result.BodySideMouthRootAffectedVertexCount.ToString(CultureInfo.InvariantCulture) +
                ", InnerMouthAffectedVertices=" + result.InnerMouthAffectedVertexCount.ToString(CultureInfo.InvariantCulture) +
                ", MouthClosurePercent=" + Num(result.MouthClosurePercent) +
                ", UpperLowerMouthRootTravel=" + Num(result.BodySideMouthRootMaximumTravel) +
                ", InnerMouthTravel=" + Num(result.InnerMouthTravelDistance) +
                ", CycleSeconds=" + Num(result.CycleSeconds) +
                ", ShapePushes=2" +
                ", LocalForward=+Z" +
                ", MotionTargetPulse=" + Num(result.MaximumReferenceTargetPulse) +
                ", LateralRadius=" + Num(ApprovedMaximumLateralRadius) +
                ", GroundDelta=" + Num(result.WorldGroundDelta) +
                ", RootTransformCurves=False" +
                ", MotionPathTargetCurves=True" +
                ", ReviewRootLocked=True.");
        }

        [MenuItem("Bellerophon/Enemies/Parvum/Capture Mouth-Root Rig Identification")]
        public static void CaptureParvumMoveMouthRootRigIdentification()
        {
            var scene = RequireCurrentScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be saved before capturing Parvum mouth-root rig identification.");
            }

            var parvumRoot = RequireRoot(ParvumRootName).transform;
            var moveSlot = RequireDirectChild(parvumRoot, MoveSlotName);
            var model = RequireDirectChild(moveSlot, ModelName);
            var renderer = RequireSingleBodyRenderer(model);
            var animator = moveSlot.GetComponent<Animator>() ??
                           throw new InvalidOperationException("Parvum move Animator is missing.");
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath) ??
                       throw new InvalidOperationException("New-model Parvum move clip is missing.");
            var candidates = ToothBranchRootBoneNames
                .Select(name => FindChildRecursive(model, name) ??
                                throw new InvalidOperationException("Parvum mouth-root rig candidate is missing: " + name + "."))
                .ToArray();
            CaptureMouthRootRigIdentification(
                moveSlot,
                model,
                renderer,
                animator,
                clip,
                candidates,
                Absolute(RigIdentificationCapturePath));
            if (scene.isDirty)
            {
                throw new InvalidOperationException("Parvum mouth-root rig identification changed the scene.");
            }

            Debug.Log(
                "ParvumMoveMouthRootRigIdentificationCaptured Result=PASS" +
                ", Image=" + RigIdentificationCapturePath +
                ", Panels=ClosedBaseline," + string.Join(",", ToothBranchRootBoneNames) +
                ", CandidateOffset=ModelUp0.25m" +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Parvum/Capture New-Model Forward Slime Move Comparison")]
        public static void CaptureParvumMoveMotionComparison()
        {
            var scene = RequireCurrentScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException("CargoRunMvp must be saved before capturing Parvum move motion.");
            }

            var reference = ReadReferenceMotion();
            var parvumRoot = RequireRoot(ParvumRootName).transform;
            var moveSlot = RequireDirectChild(parvumRoot, MoveSlotName);
            var model = RequireDirectChild(moveSlot, ModelName);
            var renderer = RequireSingleBodyRenderer(model);
            var animator = moveSlot.GetComponent<Animator>() ??
                           throw new InvalidOperationException("Parvum move Animator is missing.");
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath) ??
                       throw new InvalidOperationException("New-model Parvum move clip is missing.");
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) ??
                             throw new InvalidOperationException("New-model Parvum move controller is missing.");
            var motionTarget = FindChildRecursive(moveSlot, MotionTargetName) ??
                               throw new InvalidOperationException("Parvum move Motion Path target is missing.");
            var driver = RequireReviewPhysics(moveSlot, motionTarget);
            var mouthRigPose = ReadMouthRigPose(moveSlot, model, renderer, animator, clip, reference);
            var result = InspectState(
                parvumRoot,
                moveSlot,
                model,
                renderer,
                animator,
                clip,
                controller,
                motionTarget,
                driver,
                reference,
                mouthRigPose);
            CaptureComparison(
                moveSlot,
                renderer,
                animator,
                clip,
                Absolute(CapturePath));
            if (scene.isDirty)
            {
                throw new InvalidOperationException("Parvum move comparison capture changed the scene.");
            }

            WriteReport(result, captureCreated: true);
            Debug.Log(
                "ParvumMoveMotionCaptured Result=PASS" +
                ", Image=" + CapturePath +
                ", Times=0,0.66,1.5,2.34,3" +
                ", MoveWeights=" + string.Join(",", CaptureTimes.Select(time => Num(reference.ShapeCurve.Evaluate(time)))) +
                ", MouthWeights=" + string.Join(",", CaptureTimes.Select(time => Num(reference.MouthCurve.Evaluate(time)))) +
                ", View=BodySideMouthRootCloseup" +
                ", SceneChanged=False.");
        }

        private static Mesh EnsureGeneratedMesh(SkinnedMeshRenderer sourceRenderer)
        {
            var sourceMesh = sourceRenderer.sharedMesh;
            if (sourceMesh == null)
            {
                throw new InvalidOperationException("Supplied Parvum source mesh is missing.");
            }

            var generated = UnityEngine.Object.Instantiate(sourceMesh);
            generated.name = "parvum_move_forward_slime_mesh";
            generated.ClearBlendShapes();
            AddForwardSlimeBlendShape(generated);

            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(GeneratedMeshPath);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(generated, GeneratedMeshPath);
                existing = generated;
            }
            else
            {
                EditorUtility.CopySerialized(generated, existing);
                UnityEngine.Object.DestroyImmediate(generated);
                EditorUtility.SetDirty(existing);
            }

            AssetDatabase.SaveAssets();
            return existing;
        }

        private static void AddForwardSlimeBlendShape(Mesh mesh)
        {
            var vertices = mesh.vertices;
            if (vertices == null || vertices.Length == 0)
            {
                throw new InvalidOperationException("Parvum source mesh has no readable vertices.");
            }

            var sourceBounds = BoundsFromVertices(vertices);
            var targetVertices = new Vector3[vertices.Length];
            var deltaVertices = new Vector3[vertices.Length];
            for (var index = 0; index < vertices.Length; index++)
            {
                var target = ForwardSlimeTarget(vertices[index], sourceBounds);
                targetVertices[index] = target;
                deltaVertices[index] = target - vertices[index];
            }

            BuildNormalAndTangentDeltas(
                mesh,
                targetVertices,
                out var deltaNormals,
                out var deltaTangents);
            mesh.AddBlendShapeFrame(BlendShapeName, 100f, deltaVertices, deltaNormals, deltaTangents);
            sourceBounds.Encapsulate(BoundsFromVertices(targetVertices));
            mesh.bounds = sourceBounds;
        }

        private static Vector3 ForwardSlimeTarget(Vector3 vertex, Bounds bounds)
        {
            var extentZ = Mathf.Max(bounds.extents.z, 0.001f);
            var normalizedZ = Mathf.Clamp((vertex.z - bounds.center.z) / extentZ, -1f, 1f);
            var front = Mathf.InverseLerp(-0.2f, 1f, normalizedZ);
            return new Vector3(
                bounds.center.x + (vertex.x - bounds.center.x) * (1f - LateralSquashRatio),
                bounds.min.y + (vertex.y - bounds.min.y) * (1f - VerticalCompressionRatio),
                vertex.z + front * extentZ * ForwardReachExtentRatio);
        }

        private static void AddMouthRootSurfaceBlendShape(SkinnedMeshRenderer renderer)
        {
            var mesh = renderer.sharedMesh ??
                       throw new InvalidOperationException("Parvum generated mesh is missing for mouth-root setup.");
            if (mesh.GetBlendShapeIndex(MouthRootSurfaceBlendShapeName) >= 0)
            {
                throw new InvalidOperationException("Parvum mouth-root BlendShape already exists before setup.");
            }

            var deltaVertices = BuildMouthRootSurfaceDeltaVertices(renderer, mesh, out var analysis);
            var vertices = mesh.vertices;
            var targetVertices = new Vector3[vertices.Length];
            for (var index = 0; index < vertices.Length; index++)
            {
                targetVertices[index] = vertices[index] + deltaVertices[index];
            }

            BuildNormalAndTangentDeltas(
                mesh,
                targetVertices,
                out var deltaNormals,
                out var deltaTangents);
            mesh.AddBlendShapeFrame(
                MouthRootSurfaceBlendShapeName,
                100f,
                deltaVertices,
                deltaNormals,
                deltaTangents);
            var combinedBounds = mesh.bounds;
            combinedBounds.Encapsulate(BoundsFromVertices(targetVertices));
            mesh.bounds = combinedBounds;
            EditorUtility.SetDirty(mesh);
            AssetDatabase.SaveAssets();
            Debug.Log(
                "ParvumMouthRootSurfaceBuilt UpperVertices=" +
                analysis.UpperAffectedVertexCount.ToString(CultureInfo.InvariantCulture) +
                ", LowerVertices=" + analysis.LowerAffectedVertexCount.ToString(CultureInfo.InvariantCulture) +
                ", OpenGap=" + Num(analysis.OpenGap) +
                ", ClosedGap=" + Num(analysis.ClosedGap) +
                ", ClosurePercent=" + Num(analysis.ClosurePercent) +
                ", MaximumTravel=" + Num(analysis.MaximumTravel) + ".");
        }

        private static Vector3[] BuildMouthRootSurfaceDeltaVertices(
            SkinnedMeshRenderer renderer,
            Mesh mesh,
            out MouthRootSurfaceAnalysis analysis)
        {
            var bones = renderer.bones;
            var upperRootIndex = Array.FindIndex(bones, bone =>
                bone != null && string.Equals(bone.name, UpperMouthRootBoneName, StringComparison.Ordinal));
            var lowerRootIndices = new HashSet<int>(Enumerable.Range(0, bones.Length).Where(index =>
                bones[index] != null &&
                (string.Equals(bones[index].name, LowerMouthRootBoneName, StringComparison.Ordinal) ||
                 string.Equals(bones[index].name, "Bone_017", StringComparison.Ordinal))));
            var lowerJaw = bones.FirstOrDefault(bone =>
                bone != null && string.Equals(bone.name, LowerJawRootBoneName, StringComparison.Ordinal)) ??
                           throw new InvalidOperationException("Parvum lower-jaw rig root is missing for mouth-root setup.");
            if (upperRootIndex < 0 || lowerRootIndices.Count != 2)
            {
                throw new InvalidOperationException("Parvum upper/lower mouth-root skin rigs are incomplete.");
            }

            var lowerJawTransforms = new HashSet<Transform>(lowerJaw.GetComponentsInChildren<Transform>(true));
            var lowerJawIndices = new HashSet<int>(Enumerable.Range(0, bones.Length)
                .Where(index => bones[index] != null && lowerJawTransforms.Contains(bones[index])));
            var toothTransforms = new HashSet<Transform>();
            foreach (var rootName in ToothBranchRootBoneNames)
            {
                var toothRoot = bones.FirstOrDefault(bone =>
                    bone != null && string.Equals(bone.name, rootName, StringComparison.Ordinal)) ??
                                throw new InvalidOperationException(
                                    "Parvum tooth branch is missing during mouth-root setup: " + rootName + ".");
                foreach (var toothTransform in toothRoot.GetComponentsInChildren<Transform>(true))
                {
                    toothTransforms.Add(toothTransform);
                }
            }
            var toothIndices = new HashSet<int>(Enumerable.Range(0, bones.Length)
                .Where(index => bones[index] != null && toothTransforms.Contains(bones[index])));

            var vertices = mesh.vertices;
            var upperWeights = new float[vertices.Length];
            var lowerWeights = new float[vertices.Length];
            var upperRootWeights = new float[vertices.Length];
            var lowerRootWeights = new float[vertices.Length];
            var lowerJawWeights = new float[vertices.Length];
            var toothWeights = new float[vertices.Length];
            var bonesPerVertex = mesh.GetBonesPerVertex();
            var allWeights = mesh.GetAllBoneWeights();
            try
            {
                var weightIndex = 0;
                for (var vertexIndex = 0; vertexIndex < vertices.Length; vertexIndex++)
                {
                    var influenceCount = bonesPerVertex[vertexIndex];
                    for (var influenceIndex = 0; influenceIndex < influenceCount; influenceIndex++)
                    {
                        var influence = allWeights[weightIndex++];
                        if (influence.boneIndex == upperRootIndex)
                        {
                            upperRootWeights[vertexIndex] += influence.weight;
                        }
                        if (lowerRootIndices.Contains(influence.boneIndex))
                        {
                            lowerRootWeights[vertexIndex] += influence.weight;
                        }
                        if (lowerJawIndices.Contains(influence.boneIndex))
                        {
                            lowerJawWeights[vertexIndex] += influence.weight;
                        }
                        if (toothIndices.Contains(influence.boneIndex))
                        {
                            toothWeights[vertexIndex] += influence.weight;
                        }
                    }
                }
            }
            finally
            {
                bonesPerVertex.Dispose();
                allWeights.Dispose();
            }

            // These fixed bands isolate the body-side lip roots on the supplied GLB while excluding teeth and the front jaw.
            for (var index = 0; index < vertices.Length; index++)
            {
                var vertex = vertices[index];
                var exclusion = toothWeights[index] > 0.01f || lowerJawWeights[index] > 0.01f ? 0f : 1f;
                upperWeights[index] = upperRootWeights[index] * exclusion *
                                      BandWeight(vertex.x, -0.42f, -0.34f, 0.34f, 0.42f) *
                                      BandWeight(vertex.y, 0.78f, 0.84f, 1.10f, 1.17f) *
                                      BandWeight(vertex.z, 0.68f, 0.76f, 1.12f, 1.22f);
                lowerWeights[index] = Mathf.Clamp01(lowerRootWeights[index]) * exclusion *
                                      BandWeight(vertex.x, -0.45f, -0.36f, 0.36f, 0.45f) *
                                      BandWeight(vertex.y, 0.52f, 0.60f, 0.88f, 0.94f) *
                                      BandWeight(vertex.z, 0.56f, 0.65f, 1.08f, 1.18f);
            }

            var upperOpenCenter = WeightedCenter(vertices, upperWeights);
            var lowerOpenCenter = WeightedCenter(vertices, lowerWeights);
            var openGap = upperOpenCenter.y - lowerOpenCenter.y;
            if (openGap <= GeometryTolerance)
            {
                throw new InvalidOperationException("Parvum upper/lower mouth-root surface gap is invalid.");
            }

            var closureTravel = openGap * MouthClosureRatio /
                                MouthRootGapResponse(upperWeights, lowerWeights);
            var deltaVertices = new Vector3[vertices.Length];
            var targetVertices = new Vector3[vertices.Length];
            var maximumTravel = 0f;
            for (var index = 0; index < vertices.Length; index++)
            {
                var delta = Vector3.down * closureTravel * upperWeights[index] +
                            Vector3.up * closureTravel * lowerWeights[index];
                deltaVertices[index] = delta;
                targetVertices[index] = vertices[index] + delta;
                maximumTravel = Mathf.Max(maximumTravel, delta.magnitude);
            }

            var upperClosedCenter = WeightedCenter(targetVertices, upperWeights);
            var lowerClosedCenter = WeightedCenter(targetVertices, lowerWeights);
            var closedGap = upperClosedCenter.y - lowerClosedCenter.y;
            analysis = new MouthRootSurfaceAnalysis(
                upperWeights.Count(weight => weight > GeometryTolerance),
                lowerWeights.Count(weight => weight > GeometryTolerance),
                openGap,
                closedGap,
                (1f - closedGap / openGap) * 100f,
                maximumTravel);
            return deltaVertices;
        }

        private static float BandWeight(float value, float minimum, float fadeInEnd, float fadeOutStart, float maximum)
        {
            if (value <= minimum || value >= maximum)
            {
                return 0f;
            }

            var fadeIn = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(minimum, fadeInEnd, value));
            var fadeOut = Mathf.SmoothStep(1f, 0f, Mathf.InverseLerp(fadeOutStart, maximum, value));
            return Mathf.Min(fadeIn, fadeOut);
        }

        private static Vector3 WeightedCenter(IReadOnlyList<Vector3> vertices, IReadOnlyList<float> weights)
        {
            var sum = Vector3.zero;
            var weightSum = 0f;
            for (var index = 0; index < vertices.Count; index++)
            {
                if (weights[index] <= GeometryTolerance)
                {
                    continue;
                }
                sum += vertices[index] * weights[index];
                weightSum += weights[index];
            }

            if (weightSum <= GeometryTolerance)
            {
                throw new InvalidOperationException("Parvum mouth-root surface group is empty.");
            }
            return sum / weightSum;
        }

        private static float MouthRootGapResponse(
            IReadOnlyList<float> upperWeights,
            IReadOnlyList<float> lowerWeights)
        {
            var upperWeightSum = 0f;
            var lowerWeightSum = 0f;
            var upperDisplacementSum = 0f;
            var lowerDisplacementSum = 0f;
            for (var index = 0; index < upperWeights.Count; index++)
            {
                var unitDisplacement = -upperWeights[index] + lowerWeights[index];
                upperWeightSum += upperWeights[index];
                lowerWeightSum += lowerWeights[index];
                upperDisplacementSum += upperWeights[index] * unitDisplacement;
                lowerDisplacementSum += lowerWeights[index] * unitDisplacement;
            }
            if (upperWeightSum <= GeometryTolerance || lowerWeightSum <= GeometryTolerance)
            {
                throw new InvalidOperationException("Parvum mouth-root response group is empty.");
            }

            var response = lowerDisplacementSum / lowerWeightSum -
                           upperDisplacementSum / upperWeightSum;
            if (response <= GeometryTolerance)
            {
                throw new InvalidOperationException("Parvum mouth-root closure response is invalid.");
            }
            return response;
        }

        private static void BuildNormalAndTangentDeltas(
            Mesh source,
            Vector3[] targetVertices,
            out Vector3[] deltaNormals,
            out Vector3[] deltaTangents)
        {
            var sourceNormals = source.normals;
            var sourceTangents = source.tangents;
            deltaNormals = new Vector3[targetVertices.Length];
            deltaTangents = new Vector3[targetVertices.Length];
            var targetMesh = UnityEngine.Object.Instantiate(source);
            try
            {
                targetMesh.vertices = targetVertices;
                targetMesh.RecalculateNormals();
                targetMesh.RecalculateTangents();
                var targetNormals = targetMesh.normals;
                var targetTangents = targetMesh.tangents;
                for (var index = 0; index < targetVertices.Length; index++)
                {
                    if (sourceNormals.Length == targetVertices.Length && targetNormals.Length == targetVertices.Length)
                    {
                        deltaNormals[index] = targetNormals[index] - sourceNormals[index];
                    }

                    if (sourceTangents.Length == targetVertices.Length && targetTangents.Length == targetVertices.Length)
                    {
                        deltaTangents[index] =
                            new Vector3(targetTangents[index].x, targetTangents[index].y, targetTangents[index].z) -
                            new Vector3(sourceTangents[index].x, sourceTangents[index].y, sourceTangents[index].z);
                    }
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(targetMesh);
            }
        }

        private static AnimationClip EnsureClip(
            Transform moveSlot,
            SkinnedMeshRenderer renderer,
            Transform motionTarget,
            ReferenceMotion reference,
            MouthRigPose mouthRigPose)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);
            if (clip == null)
            {
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, ClipPath);
            }

            clip.ClearCurves();
            clip.name = "Parvum_Move_NewModel";
            clip.frameRate = reference.FrameRate;
            clip.wrapMode = WrapMode.Loop;
            var rendererPath = AnimationUtility.CalculateTransformPath(renderer.transform, moveSlot);
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(
                    rendererPath,
                    typeof(SkinnedMeshRenderer),
                    "blendShape." + BlendShapeName),
                CloneCurve(reference.ShapeCurve));
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(
                    rendererPath,
                    typeof(SkinnedMeshRenderer),
                    "blendShape." + MouthRootSurfaceBlendShapeName),
                CloneCurve(reference.MouthCurve));
            SetQuaternionCurves(
                clip,
                mouthRigPose.BonePath,
                reference.MouthCurve,
                mouthRigPose.OpenLocalRotation,
                mouthRigPose.ClosedLocalRotation);
            SetVector3Curves(
                clip,
                mouthRigPose.InnerBonePath,
                reference.MouthCurve,
                mouthRigPose.InnerOpenLocalPosition,
                mouthRigPose.InnerClosedLocalPosition);
            SetQuaternionCurves(
                clip,
                mouthRigPose.InnerBonePath,
                reference.MouthCurve,
                mouthRigPose.InnerOpenLocalRotation,
                mouthRigPose.InnerClosedLocalRotation);
            var targetPath = AnimationUtility.CalculateTransformPath(motionTarget, moveSlot);
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(targetPath, typeof(Transform), "m_LocalPosition.x"),
                CloneCurve(reference.TargetXCurve));
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(targetPath, typeof(Transform), "m_LocalPosition.y"),
                CloneCurve(reference.TargetYCurve));
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(targetPath, typeof(Transform), "m_LocalPosition.z"),
                CloneCurve(reference.TargetZCurve));

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.loopBlend = true;
            settings.loopBlendPositionY = true;
            settings.loopBlendPositionXZ = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
            return clip;
        }

        private static void SetQuaternionCurves(
            AnimationClip clip,
            string bonePath,
            AnimationCurve mouthWeightCurve,
            Quaternion openLocalRotation,
            Quaternion closedLocalRotation)
        {
            var components = new[] { "x", "y", "z", "w" };
            for (var componentIndex = 0; componentIndex < components.Length; componentIndex++)
            {
                var keys = mouthWeightCurve.keys.Select(key =>
                {
                    var normalizedWeight = Mathf.Clamp01(key.value / 100f);
                    var rotation = Quaternion.Slerp(openLocalRotation, closedLocalRotation, normalizedWeight);
                    var value = QuaternionComponent(rotation, componentIndex);
                    return new Keyframe(key.time, value, 0f, 0f);
                }).ToArray();
                var curve = new AnimationCurve(keys);
                for (var index = 0; index < curve.length; index++)
                {
                    AnimationUtility.SetKeyLeftTangentMode(curve, index, AnimationUtility.TangentMode.ClampedAuto);
                    AnimationUtility.SetKeyRightTangentMode(curve, index, AnimationUtility.TangentMode.ClampedAuto);
                }

                AnimationUtility.SetEditorCurve(
                    clip,
                    EditorCurveBinding.FloatCurve(
                        bonePath,
                        typeof(Transform),
                        "m_LocalRotation." + components[componentIndex]),
                    curve);
            }

            clip.EnsureQuaternionContinuity();
        }

        private static void SetVector3Curves(
            AnimationClip clip,
            string bonePath,
            AnimationCurve mouthWeightCurve,
            Vector3 openLocalPosition,
            Vector3 closedLocalPosition)
        {
            var components = new[] { "x", "y", "z" };
            for (var componentIndex = 0; componentIndex < components.Length; componentIndex++)
            {
                var keys = mouthWeightCurve.keys.Select(key =>
                {
                    var normalizedWeight = Mathf.Clamp01(key.value / 100f);
                    var position = Vector3.Lerp(openLocalPosition, closedLocalPosition, normalizedWeight);
                    return new Keyframe(key.time, Vector3Component(position, componentIndex), 0f, 0f);
                }).ToArray();
                var curve = new AnimationCurve(keys);
                for (var index = 0; index < curve.length; index++)
                {
                    AnimationUtility.SetKeyLeftTangentMode(curve, index, AnimationUtility.TangentMode.ClampedAuto);
                    AnimationUtility.SetKeyRightTangentMode(curve, index, AnimationUtility.TangentMode.ClampedAuto);
                }

                AnimationUtility.SetEditorCurve(
                    clip,
                    EditorCurveBinding.FloatCurve(
                        bonePath,
                        typeof(Transform),
                        "m_LocalPosition." + components[componentIndex]),
                    curve);
            }
        }

        private static float Vector3Component(Vector3 value, int componentIndex)
        {
            return componentIndex switch
            {
                0 => value.x,
                1 => value.y,
                2 => value.z,
                _ => throw new ArgumentOutOfRangeException(nameof(componentIndex))
            };
        }

        private static float QuaternionComponent(Quaternion rotation, int componentIndex)
        {
            return componentIndex switch
            {
                0 => rotation.x,
                1 => rotation.y,
                2 => rotation.z,
                3 => rotation.w,
                _ => throw new ArgumentOutOfRangeException(nameof(componentIndex))
            };
        }

        private static AnimatorController EnsureController(AnimationClip clip)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            }

            var stateMachine = controller.layers[0].stateMachine;
            var state = stateMachine.states
                .Select(child => child.state)
                .FirstOrDefault(candidate => candidate.name == "Parvum_Move_NewModel") ??
                        stateMachine.AddState("Parvum_Move_NewModel");
            state.motion = clip;
            state.speed = 1f;
            state.writeDefaultValues = false;
            stateMachine.defaultState = state;
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static InspectionResult InspectState(
            Transform parvumRoot,
            Transform moveSlot,
            Transform model,
            SkinnedMeshRenderer renderer,
            Animator animator,
            AnimationClip clip,
            AnimatorController controller,
            Transform motionTarget,
            ParvumPhysicsMotionDriver driver,
            ReferenceMotion reference,
            MouthRigPose mouthRigPose)
        {
            if (EditorUtility.scriptCompilationFailed)
            {
                throw new InvalidOperationException("Unity reports script compilation errors.");
            }

            if (renderer.sharedMesh == null ||
                !string.Equals(AssetDatabase.GetAssetPath(renderer.sharedMesh), GeneratedMeshPath, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Parvum move renderer is not using the new-model generated mesh.");
            }

            if (string.Equals(AssetDatabase.GetAssetPath(renderer.sharedMesh), ReferenceMeshPath, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("The previous Parvum move mesh was assigned instead of a new mesh.");
            }

            var sourceRenderer = RequireSourceRenderer();
            var sourceMesh = sourceRenderer.sharedMesh;
            var generatedMesh = renderer.sharedMesh;
            if (sourceMesh.vertexCount != generatedMesh.vertexCount)
            {
                throw new InvalidOperationException("Generated Parvum move mesh changed the source vertex count.");
            }

            var moveBlendShapeIndex = generatedMesh.GetBlendShapeIndex(BlendShapeName);
            var mouthRootBlendShapeIndex = generatedMesh.GetBlendShapeIndex(MouthRootSurfaceBlendShapeName);
            if (generatedMesh.blendShapeCount != 2 || moveBlendShapeIndex != 0 ||
                mouthRootBlendShapeIndex != 1 ||
                generatedMesh.GetBlendShapeIndex(LegacyMouthBlendShapeName) >= 0)
            {
                throw new InvalidOperationException(
                    "Generated Parvum move mesh must contain the body move and upper/lower mouth-root surface BlendShapes only.");
            }

            var vertexCount = sourceMesh.vertexCount;
            var deltaVertices = new Vector3[vertexCount];
            var deltaNormals = new Vector3[vertexCount];
            var deltaTangents = new Vector3[vertexCount];
            generatedMesh.GetBlendShapeFrameVertices(moveBlendShapeIndex, 0, deltaVertices, deltaNormals, deltaTangents);
            var sourceVertices = sourceMesh.vertices;
            var sourceBounds = BoundsFromVertices(sourceVertices);
            var targetVertices = new Vector3[vertexCount];
            var affectedVertexCount = 0;
            var backwardVertexCount = 0;
            var maximumShapeForward = 0f;
            for (var index = 0; index < vertexCount; index++)
            {
                var expectedTarget = ForwardSlimeTarget(sourceVertices[index], sourceBounds);
                var actualTarget = sourceVertices[index] + deltaVertices[index];
                if ((actualTarget - expectedTarget).sqrMagnitude > GeometryTolerance * GeometryTolerance)
                {
                    throw new InvalidOperationException(
                        "Generated Parvum move delta does not match the remapped reference formula at vertex " +
                        index.ToString(CultureInfo.InvariantCulture) + ".");
                }

                targetVertices[index] = actualTarget;
                if (deltaVertices[index].sqrMagnitude > 0.0000000001f)
                {
                    affectedVertexCount++;
                }

                if (deltaVertices[index].z < -GeometryTolerance)
                {
                    backwardVertexCount++;
                }

                maximumShapeForward = Mathf.Max(maximumShapeForward, deltaVertices[index].z);
            }

            if (affectedVertexCount < Mathf.FloorToInt(vertexCount * 0.95f) || backwardVertexCount != 0)
            {
                throw new InvalidOperationException(
                    "Parvum move BlendShape must move the connected slime body without backward vertex travel.");
            }

            var actualMouthRootDeltaVertices = new Vector3[vertexCount];
            var mouthRootDeltaNormals = new Vector3[vertexCount];
            var mouthRootDeltaTangents = new Vector3[vertexCount];
            generatedMesh.GetBlendShapeFrameVertices(
                mouthRootBlendShapeIndex,
                0,
                actualMouthRootDeltaVertices,
                mouthRootDeltaNormals,
                mouthRootDeltaTangents);
            var expectedMouthRootDeltaVertices = BuildMouthRootSurfaceDeltaVertices(
                renderer,
                generatedMesh,
                out var mouthRootAnalysis);
            for (var index = 0; index < vertexCount; index++)
            {
                if ((actualMouthRootDeltaVertices[index] - expectedMouthRootDeltaVertices[index]).sqrMagnitude >
                    GeometryTolerance * GeometryTolerance)
                {
                    throw new InvalidOperationException(
                        "Generated Parvum mouth-root surface delta differs from its rig-weight target at vertex " +
                        index.ToString(CultureInfo.InvariantCulture) + ".");
                }
            }

            if (mouthRootAnalysis.UpperAffectedVertexCount == 0 ||
                mouthRootAnalysis.LowerAffectedVertexCount == 0 ||
                mouthRootAnalysis.MaximumTravel <= 0.01f ||
                Mathf.Abs(mouthRootAnalysis.ClosurePercent - MouthClosureRatio * 100f) > 0.1f)
            {
                throw new InvalidOperationException(
                    "Parvum upper/lower mouth-root surfaces are not producing the approved closure. " +
                    "UpperVertices=" + mouthRootAnalysis.UpperAffectedVertexCount.ToString(CultureInfo.InvariantCulture) +
                    ", LowerVertices=" + mouthRootAnalysis.LowerAffectedVertexCount.ToString(CultureInfo.InvariantCulture) +
                    ", ClosurePercent=" + Num(mouthRootAnalysis.ClosurePercent) + ".");
            }

            var targetBounds = BoundsFromVertices(targetVertices);
            var lateralSquashPercent = (1f - targetBounds.size.x / sourceBounds.size.x) * 100f;
            var verticalCompressionPercent = (1f - targetBounds.size.y / sourceBounds.size.y) * 100f;
            if (Mathf.Abs(lateralSquashPercent - LateralSquashRatio * 100f) > 0.01f ||
                Mathf.Abs(verticalCompressionPercent - VerticalCompressionRatio * 100f) > 0.01f ||
                Mathf.Abs(targetBounds.min.y - sourceBounds.min.y) > GeometryTolerance ||
                Mathf.Abs(maximumShapeForward - sourceBounds.extents.z * ForwardReachExtentRatio) > GeometryTolerance)
            {
                throw new InvalidOperationException("Parvum move bounds do not match the adapted reference deformation.");
            }

            if (mouthRigPose.AffectedVertexCount == 0 ||
                mouthRigPose.AffectedVertexCount >= vertexCount ||
                mouthRigPose.ClosedAperture <= GeometryTolerance ||
                Mathf.Abs(mouthRigPose.ClosurePercent - MouthClosureRatio * 100f) > 0.1f)
            {
                throw new InvalidOperationException(
                    "Parvum lower-jaw rig does not produce the approved 70-percent visible aperture reduction. " +
                    "Open=" + Num(mouthRigPose.OpenAperture) +
                    ", Closed=" + Num(mouthRigPose.ClosedAperture) +
                    ", ClosurePercent=" + Num(mouthRigPose.ClosurePercent) + ".");
            }

            if (mouthRigPose.InnerAffectedVertexCount == 0 ||
                mouthRigPose.InnerTravelDistance <= 0.01f)
            {
                throw new InvalidOperationException(
                    "Parvum inner-mouth rig is not following the lower-jaw closure.");
            }

            if (animator.runtimeAnimatorController != controller || animator.applyRootMotion)
            {
                throw new InvalidOperationException("Parvum move Animator configuration is invalid.");
            }

            InspectClipBindings(moveSlot, renderer, motionTarget, clip, reference, mouthRigPose);
            if (driver.MotionPathTarget != motionTarget || !driver.LockRootMotionForReview)
            {
                throw new InvalidOperationException("Parvum move physics driver is not following the approved target in review-lock mode.");
            }

            var worldGroundDelta = MeasureWorldGroundDelta(renderer, animator, mouthRigPose);
            if (worldGroundDelta > GroundTolerance)
            {
                throw new InvalidOperationException(
                    "Parvum move deformation lifts or penetrates the visible ground. Delta=" + Num(worldGroundDelta) + ".");
            }

            RequireLocalPositiveZForward(model);
            RequireOnlyMoveConfigured(parvumRoot, moveSlot, animator);
            return new InspectionResult(
                vertexCount,
                affectedVertexCount,
                mouthRigPose.AffectedVertexCount,
                mouthRootAnalysis.UpperAffectedVertexCount + mouthRootAnalysis.LowerAffectedVertexCount,
                mouthRigPose.InnerAffectedVertexCount,
                reference.CycleSeconds,
                maximumShapeForward,
                reference.MaximumTargetPulse,
                lateralSquashPercent,
                verticalCompressionPercent,
                mouthRigPose.OpenAperture,
                mouthRigPose.ClosedAperture,
                mouthRigPose.ClosurePercent,
                mouthRigPose.JawRotationDegrees,
                mouthRootAnalysis.MaximumTravel,
                mouthRigPose.InnerTravelDistance,
                worldGroundDelta,
                AnimationUtility.CalculateTransformPath(renderer.transform, moveSlot),
                AnimationUtility.CalculateTransformPath(motionTarget, moveSlot),
                Sha256(Absolute(SourceModelPath)));
        }

        private static void InspectClipBindings(
            Transform moveSlot,
            SkinnedMeshRenderer renderer,
            Transform motionTarget,
            AnimationClip clip,
            ReferenceMotion reference,
            MouthRigPose mouthRigPose)
        {
            var bindings = AnimationUtility.GetCurveBindings(clip);
            var rendererPath = AnimationUtility.CalculateTransformPath(renderer.transform, moveSlot);
            var targetPath = AnimationUtility.CalculateTransformPath(motionTarget, moveSlot);
            const int expectedBindingCount = 16;
            if (bindings.Length != expectedBindingCount ||
                bindings.Count(binding =>
                    binding.type == typeof(SkinnedMeshRenderer) &&
                    string.Equals(binding.path, rendererPath, StringComparison.Ordinal) &&
                    string.Equals(binding.propertyName, "blendShape." + BlendShapeName, StringComparison.Ordinal)) != 1 ||
                bindings.Count(binding =>
                    binding.type == typeof(SkinnedMeshRenderer) &&
                    string.Equals(binding.path, rendererPath, StringComparison.Ordinal) &&
                    string.Equals(binding.propertyName, "blendShape." + MouthRootSurfaceBlendShapeName, StringComparison.Ordinal)) != 1 ||
                bindings.Count(binding =>
                    binding.type == typeof(Transform) &&
                    string.Equals(binding.path, mouthRigPose.BonePath, StringComparison.Ordinal) &&
                    binding.propertyName.StartsWith("m_LocalRotation.", StringComparison.Ordinal)) != 4 ||
                bindings.Count(binding =>
                    binding.type == typeof(Transform) &&
                    string.Equals(binding.path, mouthRigPose.InnerBonePath, StringComparison.Ordinal) &&
                    binding.propertyName.StartsWith("m_LocalRotation.", StringComparison.Ordinal)) != 4 ||
                bindings.Count(binding =>
                    binding.type == typeof(Transform) &&
                    string.Equals(binding.path, mouthRigPose.InnerBonePath, StringComparison.Ordinal) &&
                    binding.propertyName.StartsWith("m_LocalPosition.", StringComparison.Ordinal)) != 3 ||
                bindings.Count(binding =>
                    binding.type == typeof(Transform) &&
                    string.Equals(binding.path, targetPath, StringComparison.Ordinal) &&
                    binding.propertyName.StartsWith("m_LocalPosition.", StringComparison.Ordinal)) != 3)
            {
                throw new InvalidOperationException(
                    "Parvum move clip must contain body/mouth-root BlendShapes, lower-jaw/inner-mouth rig curves, and Motion Path target curves only.");
            }

            RequireCurveMatches(
                clip,
                bindings.Single(binding =>
                    binding.type == typeof(SkinnedMeshRenderer) &&
                    string.Equals(binding.propertyName, "blendShape." + BlendShapeName, StringComparison.Ordinal)),
                reference.ShapeCurve);
            RequireCurveMatches(
                clip,
                bindings.Single(binding =>
                    binding.type == typeof(SkinnedMeshRenderer) &&
                    string.Equals(binding.propertyName, "blendShape." + MouthRootSurfaceBlendShapeName, StringComparison.Ordinal)),
                reference.MouthCurve);
            var quaternionComponents = new[] { "x", "y", "z", "w" };
            for (var componentIndex = 0; componentIndex < quaternionComponents.Length; componentIndex++)
            {
                var component = quaternionComponents[componentIndex];
                var expected = new AnimationCurve(reference.MouthCurve.keys.Select(key =>
                {
                    var rotation = Quaternion.Slerp(
                        mouthRigPose.OpenLocalRotation,
                        mouthRigPose.ClosedLocalRotation,
                        Mathf.Clamp01(key.value / 100f));
                    return new Keyframe(key.time, QuaternionComponent(rotation, componentIndex));
                }).ToArray());
                RequireCurveMatches(
                    clip,
                    bindings.Single(binding =>
                        binding.type == typeof(Transform) &&
                        string.Equals(binding.path, mouthRigPose.BonePath, StringComparison.Ordinal) &&
                        string.Equals(binding.propertyName, "m_LocalRotation." + component, StringComparison.Ordinal)),
                    expected);
            }
            for (var componentIndex = 0; componentIndex < quaternionComponents.Length; componentIndex++)
            {
                var component = quaternionComponents[componentIndex];
                var expected = new AnimationCurve(reference.MouthCurve.keys.Select(key =>
                {
                    var rotation = Quaternion.Slerp(
                        mouthRigPose.InnerOpenLocalRotation,
                        mouthRigPose.InnerClosedLocalRotation,
                        Mathf.Clamp01(key.value / 100f));
                    return new Keyframe(key.time, QuaternionComponent(rotation, componentIndex));
                }).ToArray());
                RequireCurveMatches(
                    clip,
                    bindings.Single(binding =>
                        binding.type == typeof(Transform) &&
                        string.Equals(binding.path, mouthRigPose.InnerBonePath, StringComparison.Ordinal) &&
                        string.Equals(binding.propertyName, "m_LocalRotation." + component, StringComparison.Ordinal)),
                    expected);
            }
            var positionComponents = new[] { "x", "y", "z" };
            for (var componentIndex = 0; componentIndex < positionComponents.Length; componentIndex++)
            {
                var component = positionComponents[componentIndex];
                var expected = new AnimationCurve(reference.MouthCurve.keys.Select(key =>
                {
                    var position = Vector3.Lerp(
                        mouthRigPose.InnerOpenLocalPosition,
                        mouthRigPose.InnerClosedLocalPosition,
                        Mathf.Clamp01(key.value / 100f));
                    return new Keyframe(key.time, Vector3Component(position, componentIndex));
                }).ToArray());
                RequireCurveMatches(
                    clip,
                    bindings.Single(binding =>
                        binding.type == typeof(Transform) &&
                        string.Equals(binding.path, mouthRigPose.InnerBonePath, StringComparison.Ordinal) &&
                        string.Equals(binding.propertyName, "m_LocalPosition." + component, StringComparison.Ordinal)),
                    expected);
            }
            RequireCurveMatches(
                clip,
                bindings.Single(binding =>
                    binding.type == typeof(Transform) &&
                    string.Equals(binding.path, targetPath, StringComparison.Ordinal) &&
                    string.Equals(binding.propertyName, "m_LocalPosition.x", StringComparison.Ordinal)),
                reference.TargetXCurve);
            RequireCurveMatches(
                clip,
                bindings.Single(binding =>
                    binding.type == typeof(Transform) &&
                    string.Equals(binding.path, targetPath, StringComparison.Ordinal) &&
                    string.Equals(binding.propertyName, "m_LocalPosition.y", StringComparison.Ordinal)),
                reference.TargetYCurve);
            RequireCurveMatches(
                clip,
                bindings.Single(binding =>
                    binding.type == typeof(Transform) &&
                    string.Equals(binding.path, targetPath, StringComparison.Ordinal) &&
                    string.Equals(binding.propertyName, "m_LocalPosition.z", StringComparison.Ordinal)),
                reference.TargetZCurve);

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            if (!settings.loopTime || Mathf.Abs(clip.length - reference.CycleSeconds) > GeometryTolerance)
            {
                throw new InvalidOperationException("Parvum move clip does not match the approved three-second loop.");
            }
        }

        private static void RequireCurveMatches(AnimationClip clip, EditorCurveBinding binding, AnimationCurve expected)
        {
            var actual = AnimationUtility.GetEditorCurve(clip, binding) ??
                         throw new InvalidOperationException("Parvum move curve is missing: " + binding.propertyName + ".");
            if (actual.length != expected.length)
            {
                throw new InvalidOperationException("Parvum move curve key count differs from its reference.");
            }

            for (var index = 0; index < actual.length; index++)
            {
                if (Mathf.Abs(actual.keys[index].time - expected.keys[index].time) > GeometryTolerance ||
                    Mathf.Abs(actual.keys[index].value - expected.keys[index].value) > GeometryTolerance)
                {
                    throw new InvalidOperationException("Parvum move curve differs from its reference at key " + index + ".");
                }
            }
        }

        private static ReferenceMotion ReadReferenceMotion()
        {
            var referenceClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ReferenceClipPath) ??
                                throw new InvalidOperationException("Previous Parvum move reference clip is missing.");
            var referenceMesh = AssetDatabase.LoadAssetAtPath<Mesh>(ReferenceMeshPath) ??
                                throw new InvalidOperationException("Previous Parvum move reference mesh is missing.");
            if (referenceMesh.GetBlendShapeIndex(ReferenceBlendShapeName) < 0)
            {
                throw new InvalidOperationException("Previous Parvum move reference BlendShape is missing.");
            }

            var bindings = AnimationUtility.GetCurveBindings(referenceClip);
            var shapeBinding = bindings.SingleOrDefault(binding =>
                binding.type == typeof(SkinnedMeshRenderer) &&
                string.Equals(
                    binding.propertyName,
                    "blendShape." + ReferenceBlendShapeName,
                    StringComparison.Ordinal));
            if (string.IsNullOrEmpty(shapeBinding.propertyName))
            {
                throw new InvalidOperationException("Previous Parvum move reference curve is missing.");
            }

            var targetBindings = bindings
                .Where(binding =>
                    binding.type == typeof(Transform) &&
                    string.Equals(binding.path, ReferenceMotionTargetPath, StringComparison.Ordinal) &&
                    binding.propertyName.StartsWith("m_LocalPosition.", StringComparison.Ordinal))
                .ToArray();
            if (targetBindings.Length != 3)
            {
                throw new InvalidOperationException("Previous Parvum move reference Motion Path curves are incomplete.");
            }

            var shapeCurve = AnimationUtility.GetEditorCurve(referenceClip, shapeBinding) ??
                             throw new InvalidOperationException("Previous Parvum move reference shape curve is unreadable.");
            var targetX = AnimationUtility.GetEditorCurve(
                referenceClip,
                targetBindings.Single(binding => binding.propertyName.EndsWith(".x", StringComparison.Ordinal))) ??
                          throw new InvalidOperationException("Previous Parvum move X target curve is unreadable.");
            var targetY = AnimationUtility.GetEditorCurve(
                referenceClip,
                targetBindings.Single(binding => binding.propertyName.EndsWith(".y", StringComparison.Ordinal))) ??
                          throw new InvalidOperationException("Previous Parvum move Y target curve is unreadable.");
            var targetZ = AnimationUtility.GetEditorCurve(
                referenceClip,
                targetBindings.Single(binding => binding.propertyName.EndsWith(".z", StringComparison.Ordinal))) ??
                          throw new InvalidOperationException("Previous Parvum move Z target curve is unreadable.");
            var settings = AnimationUtility.GetAnimationClipSettings(referenceClip);
            if (!settings.loopTime || shapeCurve.length != 5 ||
                Mathf.Abs(referenceClip.length - 1f) > GeometryTolerance ||
                CountLocalMaxima(shapeCurve) != 2)
            {
                throw new InvalidOperationException("Previous Parvum move reference is not the expected one-second two-push loop.");
            }

            var sourceCycleSeconds = referenceClip.length;
            var timeScale = ApprovedCycleSeconds / sourceCycleSeconds;
            var sourceMaximumLateralRadius = targetX.keys.Max(key => Mathf.Abs(key.value));
            var sourceInitialTargetZ = targetZ.Evaluate(0f);
            var sourceMaximumTargetPulse = targetZ.keys.Max(key => key.value) - sourceInitialTargetZ;
            if (sourceMaximumLateralRadius <= GeometryTolerance || sourceMaximumTargetPulse <= GeometryTolerance)
            {
                throw new InvalidOperationException("Previous Parvum move reference range is invalid.");
            }

            var lateralScale = ApprovedMaximumLateralRadius / sourceMaximumLateralRadius;
            var forwardScale = ApprovedMaximumForwardPulse / sourceMaximumTargetPulse;
            var adjustedShape = RemapCurve(shapeCurve, timeScale, 1f, 0f);
            var adjustedTargetX = RemapCurve(targetX, timeScale, lateralScale, 0f);
            var adjustedTargetY = RemapCurve(targetY, timeScale, 1f, 0f);
            var adjustedTargetZ = RemapCurve(
                targetZ,
                timeScale,
                forwardScale,
                sourceInitialTargetZ * (1f - forwardScale));
            var adjustedMaximumLateralRadius = adjustedTargetX.keys.Max(key => Mathf.Abs(key.value));
            var adjustedMaximumTargetPulse =
                adjustedTargetZ.keys.Max(key => key.value) - adjustedTargetZ.Evaluate(0f);
            var adjustedShapeEndTime = adjustedShape.length > 0 ? adjustedShape.keys[^1].time : 0f;
            if (Mathf.Abs(adjustedShapeEndTime - ApprovedCycleSeconds) > GeometryTolerance ||
                Mathf.Abs(adjustedMaximumLateralRadius - ApprovedMaximumLateralRadius) > GeometryTolerance ||
                Mathf.Abs(adjustedMaximumTargetPulse - ApprovedMaximumForwardPulse) > GeometryTolerance)
            {
                throw new InvalidOperationException("Adjusted Parvum move does not match the approved timing and radius values.");
            }

            return new ReferenceMotion(
                adjustedShape,
                CreateMouthCurve(),
                adjustedTargetX,
                adjustedTargetY,
                adjustedTargetZ,
                ApprovedCycleSeconds,
                referenceClip.frameRate,
                adjustedMaximumTargetPulse);
        }

        private static AnimationCurve CreateMouthCurve()
        {
            var curve = new AnimationCurve(
                new Keyframe(0f, 0f, 0f, 0f),
                new Keyframe(0.66f, 100f, 0f, 0f),
                new Keyframe(1.5f, 0f, 0f, 0f),
                new Keyframe(2.34f, 100f, 0f, 0f),
                new Keyframe(3f, 0f, 0f, 0f));
            for (var index = 0; index < curve.length; index++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, index, AnimationUtility.TangentMode.ClampedAuto);
                AnimationUtility.SetKeyRightTangentMode(curve, index, AnimationUtility.TangentMode.ClampedAuto);
            }

            return curve;
        }

        private static int CountLocalMaxima(AnimationCurve curve)
        {
            var count = 0;
            for (var index = 1; index < curve.length - 1; index++)
            {
                if (curve.keys[index].value > curve.keys[index - 1].value &&
                    curve.keys[index].value > curve.keys[index + 1].value)
                {
                    count++;
                }
            }

            return count;
        }

        private static AnimationCurve CloneCurve(AnimationCurve source)
        {
            return new AnimationCurve(source.keys)
            {
                preWrapMode = source.preWrapMode,
                postWrapMode = source.postWrapMode
            };
        }

        private static AnimationCurve RemapCurve(
            AnimationCurve source,
            float timeScale,
            float valueScale,
            float valueOffset)
        {
            var keys = source.keys;
            for (var index = 0; index < keys.Length; index++)
            {
                var key = keys[index];
                key.time *= timeScale;
                key.value = key.value * valueScale + valueOffset;
                key.inTangent = key.inTangent * valueScale / timeScale;
                key.outTangent = key.outTangent * valueScale / timeScale;
                keys[index] = key;
            }

            return new AnimationCurve(keys)
            {
                preWrapMode = source.preWrapMode,
                postWrapMode = source.postWrapMode
            };
        }

        private static ParvumPhysicsMotionDriver RequireReviewPhysics(Transform moveSlot, Transform motionTarget)
        {
            var body = moveSlot.GetComponent<Rigidbody>() ??
                       throw new InvalidOperationException("Parvum move root Rigidbody is missing.");
            if (!body.isKinematic)
            {
                throw new InvalidOperationException("Parvum move review Rigidbody must remain kinematic.");
            }

            if (moveSlot.GetComponent<Collider>() == null)
            {
                throw new InvalidOperationException("Parvum move root Collider is missing.");
            }

            var driver = moveSlot.GetComponent<ParvumPhysicsMotionDriver>() ??
                         throw new InvalidOperationException("Parvum move physics driver is missing.");
            if (driver.MotionPathTarget != motionTarget || !driver.LockRootMotionForReview)
            {
                throw new InvalidOperationException(
                    "Parvum move physics driver must keep its current Motion Path target and review root lock.");
            }

            return driver;
        }

        private static MouthRigPose SolveMouthRigPose(
            Transform model,
            SkinnedMeshRenderer renderer,
            Animator animator,
            ReferenceMotion reference)
        {
            var lowerJaw = FindChildRecursive(model, LowerJawRootBoneName) ??
                           throw new InvalidOperationException("Parvum lower-jaw rig root is missing: " + LowerJawRootBoneName + ".");
            var innerMouth = FindChildRecursive(model, InnerMouthRootBoneName) ??
                             throw new InvalidOperationException("Parvum inner-mouth rig root is missing: " + InnerMouthRootBoneName + ".");
            var groups = BuildMouthSkinGroups(renderer, lowerJaw, innerMouth);
            var moveBlendShapeIndex = renderer.sharedMesh.GetBlendShapeIndex(BlendShapeName);
            var originalMoveWeight = renderer.GetBlendShapeWeight(moveBlendShapeIndex);
            var originalLocalRotation = lowerJaw.localRotation;
            var originalWorldRotation = lowerJaw.rotation;
            var originalInnerLocalPosition = innerMouth.localPosition;
            var originalInnerLocalRotation = innerMouth.localRotation;
            var originalInnerWorldPosition = innerMouth.position;
            var originalInnerWorldRotation = innerMouth.rotation;
            var jawPivot = lowerJaw.position;
            var animatorEnabled = animator.enabled;
            try
            {
                animator.enabled = false;
                renderer.SetBlendShapeWeight(moveBlendShapeIndex, reference.ShapeCurve.Evaluate(0.66f));
                lowerJaw.localRotation = originalLocalRotation;
                var openAperture = MeasureMouthAperture(model, renderer, groups);
                if (openAperture <= GeometryTolerance)
                {
                    throw new InvalidOperationException("Parvum visible mouth aperture is invalid before jaw-rig solving.");
                }

                var targetAperture = openAperture * (1f - MouthClosureRatio);
                var hingeAxis = model.TransformDirection(Vector3.right).normalized;
                var bestScore = float.PositiveInfinity;
                var bestAngle = 0f;
                var bestAperture = openAperture;
                var bestLocalRotation = originalLocalRotation;
                for (var angle = -MaximumJawSearchDegrees; angle <= MaximumJawSearchDegrees; angle += JawSearchStepDegrees)
                {
                    lowerJaw.rotation = Quaternion.AngleAxis(angle, hingeAxis) * originalWorldRotation;
                    var aperture = MeasureMouthAperture(model, renderer, groups);
                    if (aperture <= GeometryTolerance)
                    {
                        continue;
                    }

                    var score = Mathf.Abs(aperture - targetAperture);
                    if (score >= bestScore)
                    {
                        continue;
                    }

                    bestScore = score;
                    bestAngle = angle;
                    bestAperture = aperture;
                    bestLocalRotation = lowerJaw.localRotation;
                }

                var refineStart = bestAngle - JawSearchStepDegrees;
                var refineEnd = bestAngle + JawSearchStepDegrees;
                for (var angle = refineStart; angle <= refineEnd; angle += 0.01f)
                {
                    lowerJaw.rotation = Quaternion.AngleAxis(angle, hingeAxis) * originalWorldRotation;
                    var aperture = MeasureMouthAperture(model, renderer, groups);
                    if (aperture <= GeometryTolerance)
                    {
                        continue;
                    }

                    var score = Mathf.Abs(aperture - targetAperture);
                    if (score >= bestScore)
                    {
                        continue;
                    }

                    bestScore = score;
                    bestAngle = angle;
                    bestAperture = aperture;
                    bestLocalRotation = lowerJaw.localRotation;
                }

                var closurePercent = (1f - bestAperture / openAperture) * 100f;
                if (Mathf.Abs(closurePercent - MouthClosureRatio * 100f) > 0.1f)
                {
                    throw new InvalidOperationException(
                        "Parvum lower-jaw rig could not reach a 70-percent visible closure. Best=" +
                        Num(closurePercent) + " percent at " + Num(bestAngle) + " degrees.");
                }

                var jawDelta = Quaternion.AngleAxis(bestAngle, hingeAxis);
                var innerClosedWorldPosition = jawPivot + jawDelta * (originalInnerWorldPosition - jawPivot);
                var innerClosedWorldRotation = jawDelta * originalInnerWorldRotation;
                var innerParent = innerMouth.parent ??
                                  throw new InvalidOperationException("Parvum inner-mouth rig root has no parent.");
                var innerClosedLocalPosition = innerParent.InverseTransformPoint(innerClosedWorldPosition);
                var innerClosedLocalRotation = Quaternion.Inverse(innerParent.rotation) * innerClosedWorldRotation;
                lowerJaw.localRotation = originalLocalRotation;
                innerMouth.localPosition = originalInnerLocalPosition;
                innerMouth.localRotation = originalInnerLocalRotation;

                return new MouthRigPose(
                    AnimationUtility.CalculateTransformPath(lowerJaw, model.parent == null ? model : model.parent),
                    originalLocalRotation,
                    bestLocalRotation,
                    AnimationUtility.CalculateTransformPath(innerMouth, model.parent == null ? model : model.parent),
                    originalInnerLocalPosition,
                    innerClosedLocalPosition,
                    originalInnerLocalRotation,
                    innerClosedLocalRotation,
                    bestAngle,
                    openAperture,
                    bestAperture,
                    closurePercent,
                    groups.AffectedVertexCount,
                    groups.InnerAffectedVertexCount,
                    Vector3.Distance(originalInnerWorldPosition, innerClosedWorldPosition));
            }
            finally
            {
                lowerJaw.localRotation = originalLocalRotation;
                innerMouth.localPosition = originalInnerLocalPosition;
                innerMouth.localRotation = originalInnerLocalRotation;
                renderer.SetBlendShapeWeight(moveBlendShapeIndex, originalMoveWeight);
                animator.enabled = animatorEnabled;
            }
        }

        private static void ApplyBodySideMouthRootDelta(
            IReadOnlyList<Transform> roots,
            IReadOnlyList<Vector3> openWorldPositions,
            IReadOnlyList<Quaternion> openWorldRotations,
            Vector3 jawPivot,
            Quaternion jawDelta)
        {
            for (var index = 0; index < roots.Count; index++)
            {
                var root = roots[index];
                var parent = root.parent ??
                             throw new InvalidOperationException(
                                 "Parvum body-side mouth-root rig has no parent: " + root.name + ".");
                root.localPosition = parent.InverseTransformPoint(
                    jawPivot + jawDelta * (openWorldPositions[index] - jawPivot));
                root.localRotation = Quaternion.Inverse(parent.rotation) *
                                     (jawDelta * openWorldRotations[index]);
            }
        }

        private static MouthRootRigAnalysis BuildBodySideMouthRootRigPoses(
            Transform animationRoot,
            SkinnedMeshRenderer renderer,
            Transform lowerJaw,
            Quaternion jawDelta)
        {
            var roots = RequireBodySideMouthRoots(renderer);
            var jawPivot = lowerJaw.position;
            var poses = new List<MouthRootRigPose>(roots.Length);
            foreach (var root in roots)
            {
                var parent = root.parent ??
                             throw new InvalidOperationException(
                                 "Parvum body-side mouth-root rig has no parent: " + root.name + ".");
                var closedWorldPosition = jawPivot + jawDelta * (root.position - jawPivot);
                var closedWorldRotation = jawDelta * root.rotation;
                poses.Add(new MouthRootRigPose(
                    AnimationUtility.CalculateTransformPath(root, animationRoot),
                    root.localPosition,
                    parent.InverseTransformPoint(closedWorldPosition),
                    root.localRotation,
                    Quaternion.Inverse(parent.rotation) * closedWorldRotation));
            }

            return AnalyzeBodySideMouthRootRig(renderer, poses);
        }

        private static Transform[] RequireBodySideMouthRoots(SkinnedMeshRenderer renderer)
        {
            var roots = ToothBranchRootBoneNames.Select(name =>
            {
                var root = renderer.bones.FirstOrDefault(bone =>
                    bone != null && string.Equals(bone.name, name, StringComparison.Ordinal)) ??
                           throw new InvalidOperationException(
                               "Parvum body-side mouth-root rig is missing: " + name + ".");
                if (root.parent == null || !string.Equals(root.parent.name, "Bone_002", StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "Parvum body-side mouth-root rig parent changed: " + name + ".");
                }

                return root;
            }).ToArray();
            if (roots.Distinct().Count() != ToothBranchRootBoneNames.Length)
            {
                throw new InvalidOperationException("Parvum body-side mouth-root rigs are not unique.");
            }

            return roots;
        }

        private static MouthRootRigAnalysis AnalyzeBodySideMouthRootRig(
            SkinnedMeshRenderer renderer,
            IReadOnlyList<MouthRootRigPose> poses)
        {
            var roots = RequireBodySideMouthRoots(renderer);
            if (poses.Count != roots.Length)
            {
                throw new InvalidOperationException("Parvum body-side mouth-root pose count is invalid.");
            }

            var influencedTransforms = new HashSet<Transform>(
                roots.SelectMany(root => root.GetComponentsInChildren<Transform>(true)));
            var influencedBoneIndices = new HashSet<int>(Enumerable.Range(0, renderer.bones.Length)
                .Where(index => renderer.bones[index] != null && influencedTransforms.Contains(renderer.bones[index])));
            var affected = new bool[renderer.sharedMesh.vertexCount];
            var bonesPerVertex = renderer.sharedMesh.GetBonesPerVertex();
            var allWeights = renderer.sharedMesh.GetAllBoneWeights();
            try
            {
                var weightIndex = 0;
                for (var vertexIndex = 0; vertexIndex < affected.Length; vertexIndex++)
                {
                    var influenceCount = bonesPerVertex[vertexIndex];
                    for (var influenceIndex = 0; influenceIndex < influenceCount; influenceIndex++)
                    {
                        var influence = allWeights[weightIndex++];
                        if (influence.weight >= 0.05f && influencedBoneIndices.Contains(influence.boneIndex))
                        {
                            affected[vertexIndex] = true;
                        }
                    }
                }
            }
            finally
            {
                bonesPerVertex.Dispose();
                allWeights.Dispose();
            }

            var originalPositions = roots.Select(root => root.localPosition).ToArray();
            var originalRotations = roots.Select(root => root.localRotation).ToArray();
            var openMesh = new Mesh();
            var closedMesh = new Mesh();
            try
            {
                for (var index = 0; index < roots.Length; index++)
                {
                    roots[index].localPosition = poses[index].OpenLocalPosition;
                    roots[index].localRotation = poses[index].OpenLocalRotation;
                }
                renderer.BakeMesh(openMesh, false);
                for (var index = 0; index < roots.Length; index++)
                {
                    roots[index].localPosition = poses[index].ClosedLocalPosition;
                    roots[index].localRotation = poses[index].ClosedLocalRotation;
                }
                renderer.BakeMesh(closedMesh, false);

                var openVertices = openMesh.vertices;
                var closedVertices = closedMesh.vertices;
                var maximumTravel = 0f;
                for (var index = 0; index < affected.Length; index++)
                {
                    if (affected[index])
                    {
                        maximumTravel = Mathf.Max(
                            maximumTravel,
                            Vector3.Distance(openVertices[index], closedVertices[index]));
                    }
                }

                return new MouthRootRigAnalysis(poses.ToArray(), affected.Count(value => value), maximumTravel);
            }
            finally
            {
                for (var index = 0; index < roots.Length; index++)
                {
                    roots[index].localPosition = originalPositions[index];
                    roots[index].localRotation = originalRotations[index];
                }
                UnityEngine.Object.DestroyImmediate(openMesh);
                UnityEngine.Object.DestroyImmediate(closedMesh);
            }
        }

        private static MouthRigPose ReadMouthRigPose(
            Transform moveSlot,
            Transform model,
            SkinnedMeshRenderer renderer,
            Animator animator,
            AnimationClip clip,
            ReferenceMotion reference)
        {
            var lowerJaw = FindChildRecursive(model, LowerJawRootBoneName) ??
                           throw new InvalidOperationException("Parvum lower-jaw rig root is missing: " + LowerJawRootBoneName + ".");
            var innerMouth = FindChildRecursive(model, InnerMouthRootBoneName) ??
                             throw new InvalidOperationException("Parvum inner-mouth rig root is missing: " + InnerMouthRootBoneName + ".");
            var bonePath = AnimationUtility.CalculateTransformPath(lowerJaw, moveSlot);
            var innerBonePath = AnimationUtility.CalculateTransformPath(innerMouth, moveSlot);
            var bindings = AnimationUtility.GetCurveBindings(clip);
            var components = new[] { "x", "y", "z", "w" };
            var curves = components.Select(component =>
            {
                var binding = bindings.SingleOrDefault(candidate =>
                    candidate.type == typeof(Transform) &&
                    string.Equals(candidate.path, bonePath, StringComparison.Ordinal) &&
                    string.Equals(candidate.propertyName, "m_LocalRotation." + component, StringComparison.Ordinal));
                if (string.IsNullOrEmpty(binding.propertyName))
                {
                    throw new InvalidOperationException("Parvum lower-jaw animation curve is missing: " + component + ".");
                }

                return AnimationUtility.GetEditorCurve(clip, binding) ??
                       throw new InvalidOperationException("Parvum lower-jaw animation curve is unreadable: " + component + ".");
            }).ToArray();
            var openRotation = NormalizeQuaternion(new Quaternion(
                curves[0].Evaluate(0f),
                curves[1].Evaluate(0f),
                curves[2].Evaluate(0f),
                curves[3].Evaluate(0f)));
            var closedRotation = NormalizeQuaternion(new Quaternion(
                curves[0].Evaluate(0.66f),
                curves[1].Evaluate(0.66f),
                curves[2].Evaluate(0.66f),
                curves[3].Evaluate(0.66f)));
            var innerRotationCurves = components.Select(component =>
                RequireTransformCurve(clip, bindings, innerBonePath, "m_LocalRotation." + component)).ToArray();
            var positionComponents = new[] { "x", "y", "z" };
            var innerPositionCurves = positionComponents.Select(component =>
                RequireTransformCurve(clip, bindings, innerBonePath, "m_LocalPosition." + component)).ToArray();
            var innerOpenLocalPosition = new Vector3(
                innerPositionCurves[0].Evaluate(0f),
                innerPositionCurves[1].Evaluate(0f),
                innerPositionCurves[2].Evaluate(0f));
            var innerClosedLocalPosition = new Vector3(
                innerPositionCurves[0].Evaluate(0.66f),
                innerPositionCurves[1].Evaluate(0.66f),
                innerPositionCurves[2].Evaluate(0.66f));
            var innerOpenLocalRotation = NormalizeQuaternion(new Quaternion(
                innerRotationCurves[0].Evaluate(0f),
                innerRotationCurves[1].Evaluate(0f),
                innerRotationCurves[2].Evaluate(0f),
                innerRotationCurves[3].Evaluate(0f)));
            var innerClosedLocalRotation = NormalizeQuaternion(new Quaternion(
                innerRotationCurves[0].Evaluate(0.66f),
                innerRotationCurves[1].Evaluate(0.66f),
                innerRotationCurves[2].Evaluate(0.66f),
                innerRotationCurves[3].Evaluate(0.66f)));
            var groups = BuildMouthSkinGroups(renderer, lowerJaw, innerMouth);
            var moveBlendShapeIndex = renderer.sharedMesh.GetBlendShapeIndex(BlendShapeName);
            var mouthRootBlendShapeIndex = renderer.sharedMesh.GetBlendShapeIndex(MouthRootSurfaceBlendShapeName);
            var originalMoveWeight = renderer.GetBlendShapeWeight(moveBlendShapeIndex);
            var originalMouthRootWeight = renderer.GetBlendShapeWeight(mouthRootBlendShapeIndex);
            var originalLocalRotation = lowerJaw.localRotation;
            var originalInnerLocalPosition = innerMouth.localPosition;
            var originalInnerLocalRotation = innerMouth.localRotation;
            var animatorEnabled = animator.enabled;
            try
            {
                animator.enabled = false;
                renderer.SetBlendShapeWeight(moveBlendShapeIndex, reference.ShapeCurve.Evaluate(0.66f));
                renderer.SetBlendShapeWeight(mouthRootBlendShapeIndex, 0f);
                lowerJaw.localRotation = openRotation;
                innerMouth.localPosition = innerOpenLocalPosition;
                innerMouth.localRotation = innerOpenLocalRotation;
                var openAperture = MeasureMouthAperture(model, renderer, groups);
                var innerOpenCenter = MeasureWeightedGroupCenter(model, renderer, groups.InnerWeights);
                lowerJaw.localRotation = closedRotation;
                innerMouth.localPosition = innerClosedLocalPosition;
                innerMouth.localRotation = innerClosedLocalRotation;
                renderer.SetBlendShapeWeight(mouthRootBlendShapeIndex, 100f);
                var closedAperture = MeasureMouthAperture(model, renderer, groups);
                var innerClosedCenter = MeasureWeightedGroupCenter(model, renderer, groups.InnerWeights);
                var closurePercent = (1f - closedAperture / openAperture) * 100f;
                return new MouthRigPose(
                    bonePath,
                    openRotation,
                    closedRotation,
                    innerBonePath,
                    innerOpenLocalPosition,
                    innerClosedLocalPosition,
                    innerOpenLocalRotation,
                    innerClosedLocalRotation,
                    Quaternion.Angle(openRotation, closedRotation),
                    openAperture,
                    closedAperture,
                    closurePercent,
                    groups.AffectedVertexCount,
                    groups.InnerAffectedVertexCount,
                    Vector3.Distance(innerOpenCenter, innerClosedCenter));
            }
            finally
            {
                lowerJaw.localRotation = originalLocalRotation;
                innerMouth.localPosition = originalInnerLocalPosition;
                innerMouth.localRotation = originalInnerLocalRotation;
                renderer.SetBlendShapeWeight(moveBlendShapeIndex, originalMoveWeight);
                renderer.SetBlendShapeWeight(mouthRootBlendShapeIndex, originalMouthRootWeight);
                animator.enabled = animatorEnabled;
            }
        }

        private static AnimationCurve RequireTransformCurve(
            AnimationClip clip,
            IReadOnlyList<EditorCurveBinding> bindings,
            string path,
            string propertyName)
        {
            var binding = bindings.SingleOrDefault(candidate =>
                candidate.type == typeof(Transform) &&
                string.Equals(candidate.path, path, StringComparison.Ordinal) &&
                string.Equals(candidate.propertyName, propertyName, StringComparison.Ordinal));
            if (string.IsNullOrEmpty(binding.propertyName))
            {
                throw new InvalidOperationException("Parvum mouth-rig animation curve is missing: " + propertyName + ".");
            }

            return AnimationUtility.GetEditorCurve(clip, binding) ??
                   throw new InvalidOperationException("Parvum mouth-rig animation curve is unreadable: " + propertyName + ".");
        }

        private static Quaternion NormalizeQuaternion(Quaternion rotation)
        {
            var magnitude = Mathf.Sqrt(
                rotation.x * rotation.x + rotation.y * rotation.y +
                rotation.z * rotation.z + rotation.w * rotation.w);
            if (magnitude <= GeometryTolerance)
            {
                throw new InvalidOperationException("Parvum lower-jaw animation contains an invalid quaternion.");
            }

            return new Quaternion(
                rotation.x / magnitude,
                rotation.y / magnitude,
                rotation.z / magnitude,
                rotation.w / magnitude);
        }

        private static MouthSkinGroups BuildMouthSkinGroups(
            SkinnedMeshRenderer renderer,
            Transform lowerJawRoot,
            Transform innerMouthRoot)
        {
            var mesh = renderer.sharedMesh ??
                       throw new InvalidOperationException("Parvum mesh is missing for mouth-rig analysis.");
            var bones = renderer.bones;
            var upperIndices = new HashSet<int>();
            var lowerAnchorIndex = -1;
            var lowerJawIndices = new HashSet<int>();
            var innerMouthIndices = new HashSet<int>();
            var lowerJawTransforms = new HashSet<Transform>(lowerJawRoot.GetComponentsInChildren<Transform>(true));
            var innerMouthTransforms = new HashSet<Transform>(innerMouthRoot.GetComponentsInChildren<Transform>(true));
            for (var index = 0; index < bones.Length; index++)
            {
                var bone = bones[index];
                if (bone == null)
                {
                    continue;
                }

                if (string.Equals(bone.name, UpperJawLeftBoneName, StringComparison.Ordinal) ||
                    string.Equals(bone.name, UpperJawRightBoneName, StringComparison.Ordinal))
                {
                    upperIndices.Add(index);
                }

                if (string.Equals(bone.name, LowerJawAnchorBoneName, StringComparison.Ordinal))
                {
                    lowerAnchorIndex = index;
                }

                if (lowerJawTransforms.Contains(bone))
                {
                    lowerJawIndices.Add(index);
                }

                if (innerMouthTransforms.Contains(bone))
                {
                    innerMouthIndices.Add(index);
                }
            }

            if (upperIndices.Count != 2 || lowerAnchorIndex < 0 ||
                lowerJawIndices.Count == 0 || innerMouthIndices.Count < 2)
            {
                throw new InvalidOperationException("Parvum upper/lower mouth rig bones are incomplete.");
            }

            var upperWeights = new float[mesh.vertexCount];
            var lowerWeights = new float[mesh.vertexCount];
            var innerWeights = new float[mesh.vertexCount];
            var affected = new bool[mesh.vertexCount];
            var innerAffected = new bool[mesh.vertexCount];
            var bonesPerVertex = mesh.GetBonesPerVertex();
            var allWeights = mesh.GetAllBoneWeights();
            try
            {
                var weightIndex = 0;
                for (var vertexIndex = 0; vertexIndex < mesh.vertexCount; vertexIndex++)
                {
                    var influenceCount = bonesPerVertex[vertexIndex];
                    for (var influenceIndex = 0; influenceIndex < influenceCount; influenceIndex++)
                    {
                        var influence = allWeights[weightIndex++];
                        if (upperIndices.Contains(influence.boneIndex))
                        {
                            upperWeights[vertexIndex] += influence.weight;
                        }

                        if (influence.boneIndex == lowerAnchorIndex)
                        {
                            lowerWeights[vertexIndex] += influence.weight;
                        }

                        if (influence.weight >= 0.05f && lowerJawIndices.Contains(influence.boneIndex))
                        {
                            affected[vertexIndex] = true;
                        }

                        if (innerMouthIndices.Contains(influence.boneIndex))
                        {
                            innerWeights[vertexIndex] += influence.weight;
                            if (influence.weight >= 0.05f)
                            {
                                innerAffected[vertexIndex] = true;
                            }
                        }
                    }
                }
            }
            finally
            {
                bonesPerVertex.Dispose();
                allWeights.Dispose();
            }

            return new MouthSkinGroups(
                upperWeights,
                lowerWeights,
                innerWeights,
                affected.Count(value => value),
                innerAffected.Count(value => value));
        }

        private static float MeasureMouthAperture(
            Transform model,
            SkinnedMeshRenderer renderer,
            MouthSkinGroups groups)
        {
            var baked = new Mesh();
            try
            {
                renderer.BakeMesh(baked, false);
                var vertices = baked.vertices;
                var upper = WeightedCenterInModelSpace(model, renderer, vertices, groups.UpperWeights);
                var lower = WeightedCenterInModelSpace(model, renderer, vertices, groups.LowerWeights);
                return upper.y - lower.y;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(baked);
            }
        }

        private static Vector3 MeasureWeightedGroupCenter(
            Transform model,
            SkinnedMeshRenderer renderer,
            IReadOnlyList<float> weights)
        {
            var baked = new Mesh();
            try
            {
                renderer.BakeMesh(baked, false);
                return WeightedCenterInModelSpace(model, renderer, baked.vertices, weights);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(baked);
            }
        }

        private static Vector3 WeightedCenterInModelSpace(
            Transform model,
            Renderer renderer,
            IReadOnlyList<Vector3> vertices,
            IReadOnlyList<float> weights)
        {
            var weightedSum = Vector3.zero;
            var weightSum = 0f;
            for (var index = 0; index < vertices.Count; index++)
            {
                var weight = weights[index];
                if (weight <= 0f)
                {
                    continue;
                }

                var modelPoint = model.InverseTransformPoint(renderer.transform.TransformPoint(vertices[index]));
                weightedSum += modelPoint * weight;
                weightSum += weight;
            }

            if (weightSum <= GeometryTolerance)
            {
                throw new InvalidOperationException("Parvum mouth rig has no weighted aperture vertices.");
            }

            return weightedSum / weightSum;
        }

        private static void RequireLocalPositiveZForward(Transform model)
        {
            var muzzleEnd = FindChildRecursive(model, "Bone_008") ??
                            throw new InvalidOperationException("Parvum muzzle-direction reference Bone_008 is missing.");
            var local = model.InverseTransformPoint(muzzleEnd.position);
            if (local.z <= 0.5f || local.z <= Mathf.Abs(local.x))
            {
                throw new InvalidOperationException(
                    "Parvum muzzle direction is not local +Z. Bone_008=" + Vec(local) + ".");
            }
        }

        private static void LogBoneInfluenceSummary(SkinnedMeshRenderer sourceRenderer)
        {
            var mesh = sourceRenderer.sharedMesh ??
                       throw new InvalidOperationException("Parvum source mesh is missing for bone influence analysis.");
            var stats = BuildBoneInfluenceStats(sourceRenderer, mesh.vertices);
            var builder = new StringBuilder("ParvumBoneInfluenceSummary Threshold=0.05");
            foreach (var stat in stats.Where(item => item.Count > 0).OrderByDescending(item => item.WeightSum))
            {
                builder.AppendLine().Append("Bone=").Append(stat.Name)
                    .Append(",Index=").Append(stat.Index.ToString(CultureInfo.InvariantCulture))
                    .Append(",Parent=").Append(
                        sourceRenderer.bones[stat.Index] != null && sourceRenderer.bones[stat.Index].parent != null
                            ? sourceRenderer.bones[stat.Index].parent.name
                            : "None")
                    .Append(",Count=").Append(stat.Count.ToString(CultureInfo.InvariantCulture))
                    .Append(",Weight=").Append(Num(stat.WeightSum))
                    .Append(",Center=").Append(Vec(stat.WeightedCenter))
                    .Append(",Min=").Append(Vec(stat.Minimum))
                    .Append(",Max=").Append(Vec(stat.Maximum));
            }

            Debug.Log(builder.ToString());
        }

        private static BoneInfluenceStats[] BuildBoneInfluenceStats(
            SkinnedMeshRenderer sourceRenderer,
            IReadOnlyList<Vector3> vertices)
        {
            var mesh = sourceRenderer.sharedMesh ??
                       throw new InvalidOperationException("Parvum source mesh is missing for bone influence analysis.");
            var bones = sourceRenderer.bones;
            var bonesPerVertex = mesh.GetBonesPerVertex();
            var allWeights = mesh.GetAllBoneWeights();
            try
            {
                var stats = Enumerable.Range(0, bones.Length)
                    .Select(index => new BoneInfluenceStats(index, bones[index] == null ? "Missing" : bones[index].name))
                    .ToArray();
                var weightIndex = 0;
                for (var vertexIndex = 0; vertexIndex < vertices.Count; vertexIndex++)
                {
                    var influenceCount = bonesPerVertex[vertexIndex];
                    for (var influenceIndex = 0; influenceIndex < influenceCount; influenceIndex++)
                    {
                        var influence = allWeights[weightIndex++];
                        if (influence.weight < 0.05f || influence.boneIndex < 0 || influence.boneIndex >= stats.Length)
                        {
                            continue;
                        }

                        stats[influence.boneIndex].Add(vertices[vertexIndex], influence.weight);
                    }
                }

                return stats;
            }
            finally
            {
                bonesPerVertex.Dispose();
                allWeights.Dispose();
            }
        }

        private static float MeasureWorldGroundDelta(
            SkinnedMeshRenderer renderer,
            Animator animator,
            MouthRigPose mouthRigPose)
        {
            var moveBlendShapeIndex = renderer.sharedMesh.GetBlendShapeIndex(BlendShapeName);
            var mouthRootBlendShapeIndex = renderer.sharedMesh.GetBlendShapeIndex(MouthRootSurfaceBlendShapeName);
            var originalMoveWeight = renderer.GetBlendShapeWeight(moveBlendShapeIndex);
            var originalMouthRootWeight = renderer.GetBlendShapeWeight(mouthRootBlendShapeIndex);
            var lowerJaw = renderer.bones.FirstOrDefault(bone =>
                bone != null && string.Equals(bone.name, LowerJawRootBoneName, StringComparison.Ordinal)) ??
                           throw new InvalidOperationException("Parvum lower-jaw rig root is missing during ground measurement.");
            var innerMouth = renderer.bones.FirstOrDefault(bone =>
                bone != null && string.Equals(bone.name, InnerMouthRootBoneName, StringComparison.Ordinal)) ??
                             throw new InvalidOperationException("Parvum inner-mouth rig root is missing during ground measurement.");
            var originalJawRotation = lowerJaw.localRotation;
            var originalInnerPosition = innerMouth.localPosition;
            var originalInnerRotation = innerMouth.localRotation;
            var animatorEnabled = animator.enabled;
            try
            {
                animator.enabled = false;
                renderer.SetBlendShapeWeight(moveBlendShapeIndex, 0f);
                renderer.SetBlendShapeWeight(mouthRootBlendShapeIndex, 0f);
                lowerJaw.localRotation = mouthRigPose.OpenLocalRotation;
                innerMouth.localPosition = mouthRigPose.InnerOpenLocalPosition;
                innerMouth.localRotation = mouthRigPose.InnerOpenLocalRotation;
                var baseBounds = BakedWorldBounds(renderer);
                renderer.SetBlendShapeWeight(moveBlendShapeIndex, 100f);
                var movedBounds = BakedWorldBounds(renderer);
                renderer.SetBlendShapeWeight(moveBlendShapeIndex, 0f);
                lowerJaw.localRotation = mouthRigPose.ClosedLocalRotation;
                innerMouth.localPosition = mouthRigPose.InnerClosedLocalPosition;
                innerMouth.localRotation = mouthRigPose.InnerClosedLocalRotation;
                renderer.SetBlendShapeWeight(mouthRootBlendShapeIndex, 100f);
                var mouthClosedBounds = BakedWorldBounds(renderer);
                return Mathf.Max(
                    Mathf.Abs(movedBounds.min.y - baseBounds.min.y),
                    Mathf.Abs(mouthClosedBounds.min.y - baseBounds.min.y));
            }
            finally
            {
                renderer.SetBlendShapeWeight(moveBlendShapeIndex, originalMoveWeight);
                renderer.SetBlendShapeWeight(mouthRootBlendShapeIndex, originalMouthRootWeight);
                lowerJaw.localRotation = originalJawRotation;
                innerMouth.localPosition = originalInnerPosition;
                innerMouth.localRotation = originalInnerRotation;
                animator.enabled = animatorEnabled;
            }
        }

        private static Bounds BakedWorldBounds(SkinnedMeshRenderer renderer)
        {
            var baked = new Mesh();
            try
            {
                renderer.BakeMesh(baked, false);
                var vertices = baked.vertices;
                if (vertices == null || vertices.Length == 0)
                {
                    throw new InvalidOperationException("Parvum renderer produced no baked vertices.");
                }

                var matrix = renderer.transform.localToWorldMatrix;
                var bounds = new Bounds(matrix.MultiplyPoint3x4(vertices[0]), Vector3.zero);
                for (var index = 1; index < vertices.Length; index++)
                {
                    bounds.Encapsulate(matrix.MultiplyPoint3x4(vertices[index]));
                }

                return bounds;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(baked);
            }
        }

        private static void CaptureMouthRootRigIdentification(
            Transform moveSlot,
            Transform model,
            SkinnedMeshRenderer renderer,
            Animator animator,
            AnimationClip clip,
            IReadOnlyList<Transform> candidates,
            string destination)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                                      throw new InvalidOperationException("Invalid Parvum mouth-root diagnostic path."));
            var sourceCamera = Camera.main ?? UnityEngine.Object.FindFirstObjectByType<Camera>() ??
                               throw new InvalidOperationException("The scene has no camera for Parvum mouth-root framing.");
            var transforms = moveSlot.GetComponentsInChildren<Transform>(true);
            var originalLayers = transforms.Select(item => item.gameObject.layer).ToArray();
            var originalPositions = transforms.Select(item => item.localPosition).ToArray();
            var originalRotations = transforms.Select(item => item.localRotation).ToArray();
            var originalScales = transforms.Select(item => item.localScale).ToArray();
            var originalBlendShapeWeights = Enumerable.Range(0, renderer.sharedMesh.blendShapeCount)
                .Select(renderer.GetBlendShapeWeight)
                .ToArray();
            var animatorEnabled = animator.enabled;
            var forceMatrixRecalculation = renderer.forceMatrixRecalculationPerRender;
            var previousActive = RenderTexture.active;
            var panelTarget = new RenderTexture(
                RigIdentificationPanelSize,
                RigIdentificationPanelSize,
                24,
                RenderTextureFormat.ARGB32);
            var panelImage = new Texture2D(
                RigIdentificationPanelSize,
                RigIdentificationPanelSize,
                TextureFormat.RGB24,
                false);
            var panelCount = candidates.Count + 1;
            var composite = new Texture2D(
                RigIdentificationPanelSize * panelCount,
                RigIdentificationPanelSize,
                TextureFormat.RGB24,
                false);
            var cameraObject = new GameObject("ParvumMouthRootRigIdentificationCamera", typeof(Camera))
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            var lightObject = new GameObject("ParvumMouthRootRigIdentificationLight", typeof(Light))
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            try
            {
                animator.enabled = false;
                renderer.forceMatrixRecalculationPerRender = true;
                for (var index = 0; index < transforms.Length; index++)
                {
                    transforms[index].gameObject.layer = ReviewLayer;
                }

                clip.SampleAnimation(moveSlot.gameObject, 0.66f);
                var candidateCenter = candidates.Aggregate(Vector3.zero, (sum, candidate) => sum + candidate.position) /
                                      candidates.Count;
                var worldForward = renderer.transform.TransformDirection(Vector3.forward).normalized;
                var worldRight = renderer.transform.TransformDirection(Vector3.right).normalized;
                var worldUp = model.TransformDirection(Vector3.up).normalized;
                var viewDirection = (-worldRight - worldForward * 0.12f + Vector3.down * 0.02f).normalized;
                var distance = Mathf.Max(0.5f, renderer.bounds.extents.magnitude * 0.9f);

                var reviewCamera = cameraObject.GetComponent<Camera>();
                reviewCamera.CopyFrom(sourceCamera);
                reviewCamera.clearFlags = CameraClearFlags.SolidColor;
                reviewCamera.backgroundColor = new Color(0.012f, 0.016f, 0.02f, 1f);
                reviewCamera.cullingMask = 1 << ReviewLayer;
                reviewCamera.allowHDR = false;
                reviewCamera.fieldOfView = 28f;
                reviewCamera.nearClipPlane = 0.01f;
                reviewCamera.targetTexture = panelTarget;
                reviewCamera.aspect = 1f;
                reviewCamera.transform.SetPositionAndRotation(
                    candidateCenter - viewDirection * distance,
                    Quaternion.LookRotation(viewDirection, worldUp));

                var reviewLight = lightObject.GetComponent<Light>();
                reviewLight.type = LightType.Directional;
                reviewLight.intensity = 1.5f;
                reviewLight.color = new Color(0.9f, 0.95f, 1f);
                reviewLight.cullingMask = 1 << ReviewLayer;
                reviewLight.shadows = LightShadows.None;
                reviewLight.transform.rotation = Quaternion.LookRotation(
                    viewDirection + new Vector3(-0.3f, -0.35f, 0.15f),
                    worldUp);

                for (var panel = 0; panel < panelCount; panel++)
                {
                    clip.SampleAnimation(moveSlot.gameObject, 0.66f);
                    if (panel > 0)
                    {
                        candidates[panel - 1].position += worldUp * 0.25f;
                    }

                    RenderTexture.active = panelTarget;
                    reviewCamera.Render();
                    panelImage.ReadPixels(
                        new Rect(0, 0, RigIdentificationPanelSize, RigIdentificationPanelSize),
                        0,
                        0);
                    panelImage.Apply();
                    composite.SetPixels32(
                        panel * RigIdentificationPanelSize,
                        0,
                        RigIdentificationPanelSize,
                        RigIdentificationPanelSize,
                        panelImage.GetPixels32());
                }

                composite.Apply();
                File.WriteAllBytes(destination, composite.EncodeToPNG());
            }
            finally
            {
                for (var index = 0; index < transforms.Length; index++)
                {
                    transforms[index].gameObject.layer = originalLayers[index];
                    transforms[index].localPosition = originalPositions[index];
                    transforms[index].localRotation = originalRotations[index];
                    transforms[index].localScale = originalScales[index];
                }

                for (var index = 0; index < originalBlendShapeWeights.Length; index++)
                {
                    renderer.SetBlendShapeWeight(index, originalBlendShapeWeights[index]);
                }

                renderer.forceMatrixRecalculationPerRender = forceMatrixRecalculation;
                animator.enabled = animatorEnabled;
                RenderTexture.active = previousActive;
                cameraObject.GetComponent<Camera>().targetTexture = null;
                UnityEngine.Object.DestroyImmediate(panelImage);
                UnityEngine.Object.DestroyImmediate(composite);
                panelTarget.Release();
                UnityEngine.Object.DestroyImmediate(panelTarget);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(lightObject);
            }
        }

        private static void CaptureComparison(
            Transform moveSlot,
            SkinnedMeshRenderer renderer,
            Animator animator,
            AnimationClip clip,
            string destination)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                                      throw new InvalidOperationException("Invalid Parvum move capture path."));
            var sourceCamera = Camera.main ?? UnityEngine.Object.FindFirstObjectByType<Camera>() ??
                               throw new InvalidOperationException("The scene has no camera for Parvum move review framing.");
            var transforms = moveSlot.GetComponentsInChildren<Transform>(true);
            var originalLayers = transforms.Select(item => item.gameObject.layer).ToArray();
            var originalPositions = transforms.Select(item => item.localPosition).ToArray();
            var originalRotations = transforms.Select(item => item.localRotation).ToArray();
            var originalScales = transforms.Select(item => item.localScale).ToArray();
            var originalBlendShapeWeights = Enumerable.Range(0, renderer.sharedMesh.blendShapeCount)
                .Select(renderer.GetBlendShapeWeight)
                .ToArray();
            var animatorEnabled = animator.enabled;
            var forceMatrixRecalculation = renderer.forceMatrixRecalculationPerRender;
            var previousActive = RenderTexture.active;
            var panelTarget = new RenderTexture(PanelWidth, CaptureHeight, 24, RenderTextureFormat.ARGB32);
            var panelImage = new Texture2D(PanelWidth, CaptureHeight, TextureFormat.RGB24, false);
            var composite = new Texture2D(
                PanelWidth * CaptureTimes.Length,
                CaptureHeight,
                TextureFormat.RGB24,
                false);
            var cameraObject = new GameObject("ParvumMoveReviewCamera", typeof(Camera))
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            var lightObject = new GameObject("ParvumMoveReviewLight", typeof(Light))
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            try
            {
                animator.enabled = false;
                renderer.forceMatrixRecalculationPerRender = true;
                for (var index = 0; index < transforms.Length; index++)
                {
                    transforms[index].gameObject.layer = ReviewLayer;
                }

                clip.SampleAnimation(moveSlot.gameObject, 0.66f);
                var upperMouthRoot = renderer.bones.First(bone =>
                    bone != null && string.Equals(bone.name, UpperMouthRootBoneName, StringComparison.Ordinal));
                var lowerMouthRoot = renderer.bones.First(bone =>
                    bone != null && string.Equals(bone.name, LowerMouthRootBoneName, StringComparison.Ordinal));
                var mouthRootCenter = (upperMouthRoot.position + lowerMouthRoot.position) * 0.5f;
                clip.SampleAnimation(moveSlot.gameObject, 0f);

                var reviewCamera = cameraObject.GetComponent<Camera>();
                reviewCamera.CopyFrom(sourceCamera);
                reviewCamera.clearFlags = CameraClearFlags.SolidColor;
                reviewCamera.backgroundColor = new Color(0.012f, 0.016f, 0.02f, 1f);
                reviewCamera.cullingMask = 1 << ReviewLayer;
                reviewCamera.allowHDR = false;
                reviewCamera.targetTexture = panelTarget;
                reviewCamera.aspect = PanelWidth / (float)CaptureHeight;
                reviewCamera.fieldOfView = 30f;
                var worldForward = renderer.transform.TransformDirection(Vector3.forward).normalized;
                var worldRight = renderer.transform.TransformDirection(Vector3.right).normalized;
                var viewDirection = (-worldRight - worldForward * 0.08f).normalized;
                var verticalRadians = Mathf.Max(1f, reviewCamera.fieldOfView * 0.5f) * Mathf.Deg2Rad;
                var horizontalRadians = Mathf.Atan(Mathf.Tan(verticalRadians) * reviewCamera.aspect);
                var distance = Mathf.Max(
                    0.46f / Mathf.Max(0.01f, Mathf.Tan(verticalRadians)),
                    0.72f / Mathf.Max(0.01f, Mathf.Tan(horizontalRadians))) * 1.12f;
                var focus = mouthRootCenter + worldForward * 0.28f + Vector3.up * 0.03f;
                reviewCamera.transform.SetPositionAndRotation(
                    focus - viewDirection * distance,
                    Quaternion.LookRotation(viewDirection, Vector3.up));

                var reviewLight = lightObject.GetComponent<Light>();
                reviewLight.type = LightType.Directional;
                reviewLight.intensity = 1.35f;
                reviewLight.color = new Color(0.88f, 0.94f, 1f);
                reviewLight.cullingMask = 1 << ReviewLayer;
                reviewLight.shadows = LightShadows.None;
                reviewLight.transform.rotation = Quaternion.LookRotation(
                    viewDirection + new Vector3(-0.45f, -0.55f, 0.2f),
                    Vector3.up);

                for (var panel = 0; panel < CaptureTimes.Length; panel++)
                {
                    clip.SampleAnimation(moveSlot.gameObject, CaptureTimes[panel]);
                    RenderTexture.active = panelTarget;
                    reviewCamera.Render();
                    panelImage.ReadPixels(new Rect(0, 0, PanelWidth, CaptureHeight), 0, 0);
                    panelImage.Apply();
                    composite.SetPixels32(
                        panel * PanelWidth,
                        0,
                        PanelWidth,
                        CaptureHeight,
                        panelImage.GetPixels32());
                }

                composite.Apply();
                File.WriteAllBytes(destination, composite.EncodeToPNG());
            }
            finally
            {
                for (var index = 0; index < transforms.Length; index++)
                {
                    transforms[index].gameObject.layer = originalLayers[index];
                    transforms[index].localPosition = originalPositions[index];
                    transforms[index].localRotation = originalRotations[index];
                    transforms[index].localScale = originalScales[index];
                }

                for (var index = 0; index < originalBlendShapeWeights.Length; index++)
                {
                    renderer.SetBlendShapeWeight(index, originalBlendShapeWeights[index]);
                }
                renderer.forceMatrixRecalculationPerRender = forceMatrixRecalculation;
                animator.enabled = animatorEnabled;
                RenderTexture.active = previousActive;
                cameraObject.GetComponent<Camera>().targetTexture = null;
                UnityEngine.Object.DestroyImmediate(panelImage);
                UnityEngine.Object.DestroyImmediate(composite);
                panelTarget.Release();
                UnityEngine.Object.DestroyImmediate(panelTarget);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(lightObject);
            }
        }

        private static void RequireOnlyMoveConfigured(
            Transform parvumRoot,
            Transform moveSlot,
            Animator moveAnimator)
        {
            for (var index = 0; index < parvumRoot.childCount; index++)
            {
                var slot = parvumRoot.GetChild(index);
                if (slot == moveSlot)
                {
                    if (slot.GetComponentsInChildren<Animator>(true)
                        .Count(candidate => candidate.runtimeAnimatorController != null) != 1)
                    {
                        throw new InvalidOperationException("Parvum move must have exactly one configured Animator.");
                    }

                    continue;
                }

                if (slot.GetComponentsInChildren<Animator>(true)
                    .Any(candidate => candidate.runtimeAnimatorController == moveAnimator.runtimeAnimatorController))
                {
                    throw new InvalidOperationException(slot.name + " unexpectedly uses the new Parvum move controller.");
                }
            }
        }

        private static SkinnedMeshRenderer RequireSourceRenderer()
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(SourceModelPath) ??
                         throw new InvalidOperationException("The supplied Parvum GLB asset is missing.");
            var renderers = source.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Where(candidate => candidate.sharedMesh != null)
                .ToArray();
            if (renderers.Length != 1)
            {
                throw new InvalidOperationException(
                    "The supplied Parvum GLB must contain exactly one SkinnedMeshRenderer. Count=" +
                    renderers.Length.ToString(CultureInfo.InvariantCulture) + ".");
            }

            return renderers[0];
        }

        private static SkinnedMeshRenderer RequireSingleBodyRenderer(Transform model)
        {
            var renderers = model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Where(candidate => candidate.sharedMesh != null && candidate.gameObject.activeInHierarchy)
                .ToArray();
            if (renderers.Length != 1)
            {
                throw new InvalidOperationException(
                    "Current Parvum model must contain exactly one active SkinnedMeshRenderer. Count=" +
                    renderers.Length.ToString(CultureInfo.InvariantCulture) + ".");
            }

            return renderers[0];
        }

        private static void RequireCompatibleSource(SkinnedMeshRenderer current, SkinnedMeshRenderer source)
        {
            if (current.sharedMesh == null || source.sharedMesh == null ||
                current.sharedMesh.vertexCount != source.sharedMesh.vertexCount ||
                current.sharedMesh.subMeshCount != source.sharedMesh.subMeshCount)
            {
                throw new InvalidOperationException("Current Parvum move renderer does not match the supplied GLB mesh.");
            }
        }

        private static Bounds BoundsFromVertices(IReadOnlyList<Vector3> vertices)
        {
            if (vertices.Count == 0)
            {
                throw new InvalidOperationException("Cannot calculate bounds from an empty vertex collection.");
            }

            var bounds = new Bounds(vertices[0], Vector3.zero);
            for (var index = 1; index < vertices.Count; index++)
            {
                bounds.Encapsulate(vertices[index]);
            }

            return bounds;
        }

        private static string[] OtherParvumSlotSignatures(Transform root)
        {
            return root.Cast<Transform>()
                .Where(slot => !string.Equals(slot.name, MoveSlotName, StringComparison.Ordinal))
                .Select(SlotSignature)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
        }

        private static string[] ProtectedRootSignatures(Scene scene)
        {
            return scene.GetRootGameObjects()
                .Where(root => !string.Equals(root.name, ParvumRootName, StringComparison.Ordinal))
                .Select(root => SlotSignature(root.transform))
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
        }

        private static string SlotSignature(Transform root)
        {
            var builder = new StringBuilder();
            foreach (var item in root.GetComponentsInChildren<Transform>(true))
            {
                builder.Append(AnimationUtility.CalculateTransformPath(item, root)).Append('|')
                    .Append(TransformSignature(item)).Append('|')
                    .Append(item.gameObject.activeSelf ? '1' : '0').AppendLine();
                foreach (var renderer in item.GetComponents<SkinnedMeshRenderer>())
                {
                    builder.Append("Mesh=").Append(AssetDatabase.GetAssetPath(renderer.sharedMesh)).AppendLine();
                }

                foreach (var animator in item.GetComponents<Animator>())
                {
                    builder.Append("Controller=")
                        .Append(AssetDatabase.GetAssetPath(animator.runtimeAnimatorController)).AppendLine();
                }
            }

            return builder.ToString();
        }

        private static string TransformSignature(Transform item)
        {
            return Vec(item.localPosition) + "|" + Vec(item.localEulerAngles) + "|" + Vec(item.localScale);
        }

        private static Transform FindChildRecursive(Transform root, string childName)
        {
            foreach (var candidate in root.GetComponentsInChildren<Transform>(true))
            {
                if (string.Equals(candidate.name, childName, StringComparison.Ordinal))
                {
                    return candidate;
                }
            }

            return null;
        }

        private static Transform RequireDirectChild(Transform parent, string childName)
        {
            var child = parent.Find(childName) ??
                        throw new InvalidOperationException("Missing direct child " + childName + " under " + parent.name + ".");
            if (child.parent != parent)
            {
                throw new InvalidOperationException(childName + " is not a direct child of " + parent.name + ".");
            }

            return child;
        }

        private static GameObject RequireRoot(string rootName)
        {
            var root = GameObject.Find(rootName) ??
                       throw new InvalidOperationException("Missing scene root: " + rootName + ".");
            if (root.transform.parent != null)
            {
                throw new InvalidOperationException(rootName + " is not a scene root.");
            }

            return root;
        }

        private static Scene RequireCurrentScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !string.Equals(scene.path, ScenePath, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("CargoRunMvp must already be the active scene. Active=" + scene.path + ".");
            }

            return scene;
        }

        private static void RequireSourceHash()
        {
            var actual = Sha256(Absolute(SourceModelPath));
            if (!string.Equals(actual, ExpectedSourceSha256, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Supplied Parvum GLB hash changed. Expected=" + ExpectedSourceSha256 + ", Actual=" + actual + ".");
            }
        }

        private static void WriteReport(InspectionResult result, bool captureCreated)
        {
            var report = new StringBuilder()
                .AppendLine("Parvum New-Model Forward Slime Move Report")
                .AppendLine("Result=PASS")
                .AppendLine("Target=" + ParvumRootName + "/" + MoveSlotName + "/" + ModelName)
                .AppendLine("SourceModel=" + SourceModelPath)
                .AppendLine("SourceSha256=" + result.SourceSha256)
                .AppendLine("ReferenceClip=" + ReferenceClipPath)
                .AppendLine("ReferenceMesh=" + ReferenceMeshPath)
                .AppendLine("ReferenceAssetsAssigned=False")
                .AppendLine("GeneratedMesh=" + GeneratedMeshPath)
                .AppendLine("AnimationClip=" + ClipPath)
                .AppendLine("AnimatorController=" + ControllerPath)
                .AppendLine("RendererPath=" + result.RendererPath)
                .AppendLine("MotionTargetPath=" + result.MotionTargetPath)
                .AppendLine("BodyBlendShape=" + BlendShapeName)
                .AppendLine("MouthRootSurfaceBlendShape=" + MouthRootSurfaceBlendShapeName)
                .AppendLine("FrontMouthBlendShape=None")
                .AppendLine("MouthRigRoot=" + LowerJawRootBoneName)
                .AppendLine("UpperMouthRootSkinRig=" + UpperMouthRootBoneName)
                .AppendLine("LowerMouthRootSkinRigs=" + LowerMouthRootBoneName + ",Bone_017")
                .AppendLine("ExcludedToothBranchRigs=" + string.Join(",", ToothBranchRootBoneNames))
                .AppendLine("MouthRigLowerAnchor=" + LowerJawAnchorBoneName)
                .AppendLine("MouthRigUpperAnchors=" + UpperJawLeftBoneName + "," + UpperJawRightBoneName)
                .AppendLine("InnerMouthRigRoot=" + InnerMouthRootBoneName)
                .AppendLine("VertexCount=" + result.VertexCount.ToString(CultureInfo.InvariantCulture))
                .AppendLine("AffectedVertexCount=" + result.AffectedVertexCount.ToString(CultureInfo.InvariantCulture))
                .AppendLine("MouthAffectedVertexCount=" + result.MouthAffectedVertexCount.ToString(CultureInfo.InvariantCulture))
                .AppendLine("UpperLowerMouthRootAffectedVertexCount=" + result.BodySideMouthRootAffectedVertexCount.ToString(CultureInfo.InvariantCulture))
                .AppendLine("InnerMouthAffectedVertexCount=" + result.InnerMouthAffectedVertexCount.ToString(CultureInfo.InvariantCulture))
                .AppendLine("CycleSeconds=" + Num(result.CycleSeconds))
                .AppendLine("Loop=True")
                .AppendLine("SourceReferenceCycleSeconds=1")
                .AppendLine("AppliedShapeTimes=0,0.75,1.5,2.25,3")
                .AppendLine("ReferenceShapeWeights=10.5,70,24.5,63,10.5")
                .AppendLine("AppliedTargetTimes=0,0.66,1.5,2.34,3")
                .AppendLine("AppliedTargetYValues=0,-0.03,0,-0.025,0")
                .AppendLine("MouthClosureTimes=0,0.66,1.5,2.34,3")
                .AppendLine("MouthClosureWeights=0,100,0,100,0")
                .AppendLine("MouthOpenAperture=" + Num(result.MouthOpenAperture))
                .AppendLine("MouthClosedAperture=" + Num(result.MouthClosedAperture))
                .AppendLine("MouthClosurePercent=" + Num(result.MouthClosurePercent))
                .AppendLine("MouthRigRotationDegrees=" + Num(result.JawRotationDegrees))
                .AppendLine("MouthRigHierarchyDriven=True")
                .AppendLine("UpperLowerMouthRootSurfaceCoupled=True")
                .AppendLine("UpperLowerMouthRootMaximumTravel=" + Num(result.BodySideMouthRootMaximumTravel))
                .AppendLine("InnerMouthRigCoupled=True")
                .AppendLine("InnerMouthTravelDistance=" + Num(result.InnerMouthTravelDistance))
                .AppendLine("PushCount=2")
                .AppendLine("MuzzleForwardAxis=Local+Z")
                .AppendLine("LateralSquashPercentAt100=" + Num(result.LateralSquashPercent))
                .AppendLine("VerticalCompressionPercentAt100=" + Num(result.VerticalCompressionPercent))
                .AppendLine("MaximumShapeForwardAt100=" + Num(result.MaximumShapeForward))
                .AppendLine("MaximumMotionTargetPulse=" + Num(result.MaximumReferenceTargetPulse))
                .AppendLine("MaximumLateralRadius=" + Num(ApprovedMaximumLateralRadius))
                .AppendLine("WorldGroundDelta=" + Num(result.WorldGroundDelta))
                .AppendLine("RootTransformCurves=False")
                .AppendLine("TransformScaleAnimation=False")
                .AppendLine("MotionPathTargetCurves=True")
                .AppendLine("RigidbodyTargetFollow=True")
                .AppendLine("ReviewRootLocked=True")
                .AppendLine("OriginalGlbModified=False")
                .AppendLine("OtherParvumSlotsChanged=False")
                .AppendLine("OtherSceneRootsChanged=False")
                .AppendLine("CaptureView=BodySideMouthRootCloseup")
                .AppendLine("CapturePanelsLeftToRight=0s,0.66s,1.5s,2.34s,3s")
                .AppendLine("CaptureCreated=" + (captureCreated ? "True" : "False"))
                .ToString();
            var destination = Absolute(ReportPath);
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                                      throw new InvalidOperationException("Invalid Parvum move report path."));
            File.WriteAllText(destination, report, new UTF8Encoding(false));
        }

        private static string Sha256(string path)
        {
            using var stream = File.OpenRead(path);
            using var hash = SHA256.Create();
            return BitConverter.ToString(hash.ComputeHash(stream)).Replace("-", string.Empty);
        }

        private static string Absolute(string relativePath)
        {
            return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), relativePath));
        }

        private static string Num(float value)
        {
            return value.ToString("0.######", CultureInfo.InvariantCulture);
        }

        private static string Vec(Vector3 value)
        {
            return "(" + Num(value.x) + "," + Num(value.y) + "," + Num(value.z) + ")";
        }

        private readonly struct ReferenceMotion
        {
            public ReferenceMotion(
                AnimationCurve shapeCurve,
                AnimationCurve mouthCurve,
                AnimationCurve targetXCurve,
                AnimationCurve targetYCurve,
                AnimationCurve targetZCurve,
                float cycleSeconds,
                float frameRate,
                float maximumTargetPulse)
            {
                ShapeCurve = shapeCurve;
                MouthCurve = mouthCurve;
                TargetXCurve = targetXCurve;
                TargetYCurve = targetYCurve;
                TargetZCurve = targetZCurve;
                CycleSeconds = cycleSeconds;
                FrameRate = frameRate;
                MaximumTargetPulse = maximumTargetPulse;
                ShapeWeights = shapeCurve.keys.Select(key => key.value).ToArray();
            }

            public AnimationCurve ShapeCurve { get; }
            public AnimationCurve MouthCurve { get; }
            public AnimationCurve TargetXCurve { get; }
            public AnimationCurve TargetYCurve { get; }
            public AnimationCurve TargetZCurve { get; }
            public float CycleSeconds { get; }
            public float FrameRate { get; }
            public float MaximumTargetPulse { get; }
            public float[] ShapeWeights { get; }
        }

        private readonly struct MouthRigPose
        {
            public MouthRigPose(
                string bonePath,
                Quaternion openLocalRotation,
                Quaternion closedLocalRotation,
                string innerBonePath,
                Vector3 innerOpenLocalPosition,
                Vector3 innerClosedLocalPosition,
                Quaternion innerOpenLocalRotation,
                Quaternion innerClosedLocalRotation,
                float jawRotationDegrees,
                float openAperture,
                float closedAperture,
                float closurePercent,
                int affectedVertexCount,
                int innerAffectedVertexCount,
                float innerTravelDistance)
            {
                BonePath = bonePath;
                OpenLocalRotation = openLocalRotation;
                ClosedLocalRotation = closedLocalRotation;
                InnerBonePath = innerBonePath;
                InnerOpenLocalPosition = innerOpenLocalPosition;
                InnerClosedLocalPosition = innerClosedLocalPosition;
                InnerOpenLocalRotation = innerOpenLocalRotation;
                InnerClosedLocalRotation = innerClosedLocalRotation;
                JawRotationDegrees = jawRotationDegrees;
                OpenAperture = openAperture;
                ClosedAperture = closedAperture;
                ClosurePercent = closurePercent;
                AffectedVertexCount = affectedVertexCount;
                InnerAffectedVertexCount = innerAffectedVertexCount;
                InnerTravelDistance = innerTravelDistance;
            }

            public string BonePath { get; }
            public Quaternion OpenLocalRotation { get; }
            public Quaternion ClosedLocalRotation { get; }
            public string InnerBonePath { get; }
            public Vector3 InnerOpenLocalPosition { get; }
            public Vector3 InnerClosedLocalPosition { get; }
            public Quaternion InnerOpenLocalRotation { get; }
            public Quaternion InnerClosedLocalRotation { get; }
            public float JawRotationDegrees { get; }
            public float OpenAperture { get; }
            public float ClosedAperture { get; }
            public float ClosurePercent { get; }
            public int AffectedVertexCount { get; }
            public int InnerAffectedVertexCount { get; }
            public float InnerTravelDistance { get; }
        }

        private readonly struct MouthRootSurfaceAnalysis
        {
            public MouthRootSurfaceAnalysis(
                int upperAffectedVertexCount,
                int lowerAffectedVertexCount,
                float openGap,
                float closedGap,
                float closurePercent,
                float maximumTravel)
            {
                UpperAffectedVertexCount = upperAffectedVertexCount;
                LowerAffectedVertexCount = lowerAffectedVertexCount;
                OpenGap = openGap;
                ClosedGap = closedGap;
                ClosurePercent = closurePercent;
                MaximumTravel = maximumTravel;
            }

            public int UpperAffectedVertexCount { get; }
            public int LowerAffectedVertexCount { get; }
            public float OpenGap { get; }
            public float ClosedGap { get; }
            public float ClosurePercent { get; }
            public float MaximumTravel { get; }
        }

        private readonly struct MouthRootRigPose
        {
            public MouthRootRigPose(
                string bonePath,
                Vector3 openLocalPosition,
                Vector3 closedLocalPosition,
                Quaternion openLocalRotation,
                Quaternion closedLocalRotation)
            {
                BonePath = bonePath;
                OpenLocalPosition = openLocalPosition;
                ClosedLocalPosition = closedLocalPosition;
                OpenLocalRotation = openLocalRotation;
                ClosedLocalRotation = closedLocalRotation;
            }

            public string BonePath { get; }
            public Vector3 OpenLocalPosition { get; }
            public Vector3 ClosedLocalPosition { get; }
            public Quaternion OpenLocalRotation { get; }
            public Quaternion ClosedLocalRotation { get; }
        }

        private readonly struct MouthRootRigAnalysis
        {
            public MouthRootRigAnalysis(
                IReadOnlyList<MouthRootRigPose> poses,
                int affectedVertexCount,
                float maximumTravel)
            {
                Poses = poses;
                AffectedVertexCount = affectedVertexCount;
                MaximumTravel = maximumTravel;
            }

            public IReadOnlyList<MouthRootRigPose> Poses { get; }
            public int AffectedVertexCount { get; }
            public float MaximumTravel { get; }
        }

        private sealed class MouthSkinGroups
        {
            public MouthSkinGroups(
                float[] upperWeights,
                float[] lowerWeights,
                float[] innerWeights,
                int affectedVertexCount,
                int innerAffectedVertexCount)
            {
                UpperWeights = upperWeights;
                LowerWeights = lowerWeights;
                InnerWeights = innerWeights;
                AffectedVertexCount = affectedVertexCount;
                InnerAffectedVertexCount = innerAffectedVertexCount;
            }

            public float[] UpperWeights { get; }
            public float[] LowerWeights { get; }
            public float[] InnerWeights { get; }
            public int AffectedVertexCount { get; }
            public int InnerAffectedVertexCount { get; }
        }

        private sealed class BoneInfluenceStats
        {
            public BoneInfluenceStats(int index, string name)
            {
                Index = index;
                Name = name;
                Minimum = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
                Maximum = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
            }

            public int Index { get; }
            public string Name { get; }
            public int Count { get; private set; }
            public float WeightSum { get; private set; }
            public Vector3 WeightedPositionSum { get; private set; }
            public Vector3 Minimum { get; private set; }
            public Vector3 Maximum { get; private set; }
            public Vector3 WeightedCenter => WeightSum <= 0f ? Vector3.zero : WeightedPositionSum / WeightSum;

            public void Add(Vector3 vertex, float weight)
            {
                Count++;
                WeightSum += weight;
                WeightedPositionSum += vertex * weight;
                Minimum = Vector3.Min(Minimum, vertex);
                Maximum = Vector3.Max(Maximum, vertex);
            }
        }

        private readonly struct InspectionResult
        {
            public InspectionResult(
                int vertexCount,
                int affectedVertexCount,
                int mouthAffectedVertexCount,
                int bodySideMouthRootAffectedVertexCount,
                int innerMouthAffectedVertexCount,
                float cycleSeconds,
                float maximumShapeForward,
                float maximumReferenceTargetPulse,
                float lateralSquashPercent,
                float verticalCompressionPercent,
                float mouthOpenAperture,
                float mouthClosedAperture,
                float mouthClosurePercent,
                float jawRotationDegrees,
                float bodySideMouthRootMaximumTravel,
                float innerMouthTravelDistance,
                float worldGroundDelta,
                string rendererPath,
                string motionTargetPath,
                string sourceSha256)
            {
                VertexCount = vertexCount;
                AffectedVertexCount = affectedVertexCount;
                MouthAffectedVertexCount = mouthAffectedVertexCount;
                BodySideMouthRootAffectedVertexCount = bodySideMouthRootAffectedVertexCount;
                InnerMouthAffectedVertexCount = innerMouthAffectedVertexCount;
                CycleSeconds = cycleSeconds;
                MaximumShapeForward = maximumShapeForward;
                MaximumReferenceTargetPulse = maximumReferenceTargetPulse;
                LateralSquashPercent = lateralSquashPercent;
                VerticalCompressionPercent = verticalCompressionPercent;
                MouthOpenAperture = mouthOpenAperture;
                MouthClosedAperture = mouthClosedAperture;
                MouthClosurePercent = mouthClosurePercent;
                JawRotationDegrees = jawRotationDegrees;
                BodySideMouthRootMaximumTravel = bodySideMouthRootMaximumTravel;
                InnerMouthTravelDistance = innerMouthTravelDistance;
                WorldGroundDelta = worldGroundDelta;
                RendererPath = rendererPath;
                MotionTargetPath = motionTargetPath;
                SourceSha256 = sourceSha256;
            }

            public int VertexCount { get; }
            public int AffectedVertexCount { get; }
            public int MouthAffectedVertexCount { get; }
            public int BodySideMouthRootAffectedVertexCount { get; }
            public int InnerMouthAffectedVertexCount { get; }
            public float CycleSeconds { get; }
            public float MaximumShapeForward { get; }
            public float MaximumReferenceTargetPulse { get; }
            public float LateralSquashPercent { get; }
            public float VerticalCompressionPercent { get; }
            public float MouthOpenAperture { get; }
            public float MouthClosedAperture { get; }
            public float MouthClosurePercent { get; }
            public float JawRotationDegrees { get; }
            public float BodySideMouthRootMaximumTravel { get; }
            public float InnerMouthTravelDistance { get; }
            public float WorldGroundDelta { get; }
            public string RendererPath { get; }
            public string MotionTargetPath { get; }
            public string SourceSha256 { get; }
        }
    }
}
