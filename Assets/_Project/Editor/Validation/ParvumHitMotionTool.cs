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
    internal static class ParvumHitMotionTool
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string ParvumRootName = "Approved Parvum Enemy Placement";
        private const string HitSlotName = "Parvum_04_Hit";
        private const string ModelName = "Parvum_Model";
        private const string SourceModelPath = "Assets/_Project/Art/Enemies/Parvum/Models/parvum.glb";
        private const string GeneratedMeshPath =
            "Assets/_Project/Art/Enemies/Parvum/Models/parvum_hit_left_crush_mesh.asset";
        private const string ClipPath =
            "Assets/_Project/Art/Enemies/Parvum/Animations/Parvum_Hit_LeftCrush_NewModel.anim";
        private const string ControllerPath =
            "Assets/_Project/Art/Enemies/Parvum/Animations/Controllers/Parvum_Hit_LeftCrush_NewModel_Controller.controller";
        private const string OldHitClipPath =
            "Assets/_Project/Art/Enemies/Parvum/Animations/Parvum_Hit.anim";
        private const string OldHitControllerPath =
            "Assets/_Project/Art/Enemies/Parvum/Animations/Controllers/Parvum_Hit_Controller.controller";
        private const string BodyCrushBlendShapeName = "Hit_FrontViewLeft_Body_Crush_New";
        private const string HeadShakeBlendShapeName = "Hit_ObjectLeft_Head_Shake_New";
        private const string LowerJawRecoilBlendShapeName = "Hit_ObjectLeft_LowerJaw_Recoil_New";
        private const string OutputFolder = "docs/validation/parvum_hit_motion_2026-08-15";
        private const string ReportPath = OutputFolder + "/Parvum_Hit_Motion_Report.txt";
        private const string CapturePath = OutputFolder + "/Parvum_Hit_Motion_Final_Comparison.png";
        private const string ExpectedSourceSha256 =
            "E27840896F1DFA15BEE6F45F2BA943D28375A485E141907283CF79446B5640AB";

        private const float CycleSeconds = 3f;
        private const float ImpactOnsetTime = 0.08f;
        private const float ImpactPeakTime = 0.18f;
        private const float BodyRecoilTime = 0.30f;
        private const float BodyAftershockTime = 0.40f;
        private const float BodyDampingTime = 0.52f;
        private const float HeadSettleTime = 0.42f;
        private const float ImpactRecoveryTime = 0.68f;
        private const float MaximumCrushDistance = 0.38f;
        private const float CrushVerticalDrop = 0.12f;
        private const float CrushDepthRatio = 0.10f;
        private const float HeadPitchDegrees = 12f;
        private const float HeadYawDegrees = 48f;
        private const float HeadRollDegrees = 20f;
        private const float HeadImpactCompression = 0.055f;
        private const float MinimumExpandedHeadTravel = 0.70f;
        private const float LowerJawPeakTime = 0.26f;
        private const float LowerJawReboundTime = 0.38f;
        private const float LowerJawAftershockTime = 0.50f;
        private const float LowerJawRecoveryTime = 0.72f;
        private const float MinimumLowerJawTravel = 0.12f;
        private const float GeometryTolerance = 0.0001f;
        private const float GroundTolerance = 0.003f;
        private const int ReviewLayer = 31;
        private const int PanelWidth = 420;
        private const int CaptureHeight = 560;

        private static readonly float[] CaptureTimes =
            { 0f, ImpactPeakTime, LowerJawPeakTime, BodyRecoilTime, BodyAftershockTime, LowerJawRecoveryTime };

        private static readonly string[] LowerJawSurfaceBoneNames =
            { "Bone_011", "Bone_012", "Bone_013", "Bone_014", "Bone_015", "Bone_016", "Bone_017", "Bone_018" };

        [MenuItem("Bellerophon/Enemies/Parvum/Apply Hit Left Crush And Head Shake")]
        public static void ApplyParvumHitMotion()
        {
            var scene = RequireCurrentScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp has unsaved editor changes; the new Parvum hit motion was not applied.");
            }

            RequireSourceHash();
            var parvumRoot = RequireRoot(ParvumRootName).transform;
            var hitSlot = RequireDirectChild(parvumRoot, HitSlotName);
            var model = RequireDirectChild(hitSlot, ModelName);
            var renderer = RequireSingleBodyRenderer(model);
            var sourceRenderer = RequireSourceRenderer();
            RequireCompatibleSource(renderer, sourceRenderer);

            var protectedBefore = ProtectedRootSignatures(scene);
            var otherSlotsBefore = OtherParvumSlotSignatures(parvumRoot);
            var hitTransformBefore = TransformSignature(hitSlot);
            var modelTransformBefore = TransformSignature(model);
            var physicsBefore = PhysicsSignature(hitSlot);

            var generatedMesh = EnsureGeneratedMesh(sourceRenderer.sharedMesh, renderer);
            var clip = EnsureClip(hitSlot, renderer);
            var controller = EnsureController(clip);
            renderer.sharedMesh = generatedMesh;
            renderer.localBounds = generatedMesh.bounds;
            renderer.SetBlendShapeWeight(generatedMesh.GetBlendShapeIndex(BodyCrushBlendShapeName), 0f);
            renderer.SetBlendShapeWeight(generatedMesh.GetBlendShapeIndex(HeadShakeBlendShapeName), 0f);
            renderer.SetBlendShapeWeight(generatedMesh.GetBlendShapeIndex(LowerJawRecoilBlendShapeName), 0f);

            var animator = hitSlot.GetComponent<Animator>();
            if (animator == null)
            {
                animator = hitSlot.gameObject.AddComponent<Animator>();
            }

            var otherConfiguredAnimators = hitSlot.GetComponentsInChildren<Animator>(true)
                .Where(candidate => candidate != animator && candidate.runtimeAnimatorController != null)
                .ToArray();
            if (otherConfiguredAnimators.Length > 0)
            {
                throw new InvalidOperationException(
                    "Parvum hit contains an unexpected additional configured Animator: " +
                    otherConfiguredAnimators[0].name + ".");
            }

            animator.runtimeAnimatorController = controller;
            animator.avatar = null;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.enabled = true;

            var result = InspectState(parvumRoot, hitSlot, model, renderer, animator, clip, controller);
            if (!string.Equals(hitTransformBefore, TransformSignature(hitSlot), StringComparison.Ordinal) ||
                !string.Equals(modelTransformBefore, TransformSignature(model), StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Parvum hit root or model Transform changed during hit setup.");
            }

            if (!string.Equals(physicsBefore, PhysicsSignature(hitSlot), StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Parvum hit physics configuration changed during hit setup.");
            }

            if (!otherSlotsBefore.SequenceEqual(OtherParvumSlotSignatures(parvumRoot), StringComparer.Ordinal))
            {
                throw new InvalidOperationException("A non-hit Parvum slot changed during hit setup.");
            }

            if (!protectedBefore.SequenceEqual(ProtectedRootSignatures(scene), StringComparer.Ordinal))
            {
                throw new InvalidOperationException("A scene root outside Parvum changed during hit setup.");
            }

            EditorUtility.SetDirty(renderer);
            EditorUtility.SetDirty(animator);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException("CargoRunMvp could not be saved after applying Parvum hit motion.");
            }

            AssetDatabase.SaveAssets();
            WriteReport(result, false);
            Debug.Log(
                "ParvumHitMotionApplied Result=PASS" +
                ", Target=" + ParvumRootName + "/" + HitSlotName + "/" + ModelName +
                ", CycleSeconds=" + Num(result.CycleSeconds) +
                ", FrontViewLeftCrushDistance=" + Num(result.MaximumInwardCrush) +
                ", ObjectLeftHeadTravel=" + Num(result.MaximumHeadLeftTravel) +
                ", LowerJawRecoilTravel=" + Num(result.MaximumLowerJawTravel) +
                ", HeadShakeCount=1" +
                ", GroundDelta=" + Num(result.WorldGroundDelta) +
                ", OldHitAssetsAssigned=False" +
                ", PhysicsPreserved=True" +
                ", OtherParvumSlotsChanged=False" +
                ", OtherSceneRootsChanged=False" +
                ", SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Parvum/Inspect Hit Left Crush And Head Shake")]
        public static void InspectParvumHitMotion()
        {
            var scene = RequireCurrentScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException("CargoRunMvp must be saved before inspecting Parvum hit motion.");
            }

            RequireSourceHash();
            var parvumRoot = RequireRoot(ParvumRootName).transform;
            var hitSlot = RequireDirectChild(parvumRoot, HitSlotName);
            var model = RequireDirectChild(hitSlot, ModelName);
            var renderer = RequireSingleBodyRenderer(model);
            var animator = hitSlot.GetComponent<Animator>() ??
                           throw new InvalidOperationException("Parvum hit Animator is missing.");
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath) ??
                       throw new InvalidOperationException("The new Parvum hit clip is missing.");
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) ??
                             throw new InvalidOperationException("The new Parvum hit controller is missing.");
            var result = InspectState(parvumRoot, hitSlot, model, renderer, animator, clip, controller);
            WriteReport(result, File.Exists(Absolute(CapturePath)));
            Debug.Log(
                "ParvumHitMotionInspected Result=PASS" +
                ", CycleSeconds=" + Num(result.CycleSeconds) +
                ", CrushAffectedVertices=" + result.CrushAffectedVertexCount.ToString(CultureInfo.InvariantCulture) +
                ", HeadAffectedVertices=" + result.HeadAffectedVertexCount.ToString(CultureInfo.InvariantCulture) +
                ", LowerJawAffectedVertices=" + result.LowerJawAffectedVertexCount.ToString(CultureInfo.InvariantCulture) +
                ", FrontViewLeftCrushDistance=" + Num(result.MaximumInwardCrush) +
                ", OppositeSideTravel=" + Num(result.OppositeSideMaximumTravel) +
                ", ObjectLeftHeadTravel=" + Num(result.MaximumHeadLeftTravel) +
                ", LowerJawRecoilTravel=" + Num(result.MaximumLowerJawTravel) +
                ", HeadShakeCount=1" +
                ", GroundDelta=" + Num(result.WorldGroundDelta) +
                ", RootTransformCurves=False" +
                ", PhysicsPreserved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Parvum/Capture Hit Left Crush And Head Shake Comparison")]
        public static void CaptureParvumHitMotionComparison()
        {
            var scene = RequireCurrentScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException("CargoRunMvp must be saved before the final Parvum hit capture.");
            }

            var parvumRoot = RequireRoot(ParvumRootName).transform;
            var hitSlot = RequireDirectChild(parvumRoot, HitSlotName);
            var model = RequireDirectChild(hitSlot, ModelName);
            var renderer = RequireSingleBodyRenderer(model);
            var animator = hitSlot.GetComponent<Animator>() ??
                           throw new InvalidOperationException("Parvum hit Animator is missing.");
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath) ??
                       throw new InvalidOperationException("The new Parvum hit clip is missing.");
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) ??
                             throw new InvalidOperationException("The new Parvum hit controller is missing.");
            var result = InspectState(parvumRoot, hitSlot, model, renderer, animator, clip, controller);

            Directory.CreateDirectory(Absolute(OutputFolder));
            CaptureComparison(hitSlot, renderer, animator, clip, Absolute(CapturePath));
            if (scene.isDirty)
            {
                throw new InvalidOperationException("Final Parvum hit capture unexpectedly dirtied CargoRunMvp.");
            }

            WriteReport(result, true);
            AssetDatabase.Refresh();
            Debug.Log(
                "ParvumHitMotionCaptured Result=PASS" +
                ", Image=" + CapturePath +
                ", Times=0,0.18,0.26,0.30,0.40,0.72" +
                ", Phases=Rest,HeadBodyImpactPeak,LowerJawPeak,StrongBodyOutwardRebound,BodyRecompression,Recovered" +
                ", SceneChanged=False.");
        }

        private static Mesh EnsureGeneratedMesh(Mesh sourceMesh, SkinnedMeshRenderer targetRenderer)
        {
            if (sourceMesh == null)
            {
                throw new InvalidOperationException("The supplied Parvum GLB has no source mesh.");
            }

            var generated = UnityEngine.Object.Instantiate(sourceMesh);
            generated.name = "parvum_hit_left_crush_mesh";
            generated.ClearBlendShapes();
            var deformation = BuildDeformation(sourceMesh, targetRenderer);
            AddBlendShape(generated, BodyCrushBlendShapeName, deformation.CrushDeltas);
            AddBlendShape(generated, HeadShakeBlendShapeName, deformation.HeadDeltas);
            AddBlendShape(generated, LowerJawRecoilBlendShapeName, deformation.LowerJawDeltas);

            var combinedBounds = sourceMesh.bounds;
            combinedBounds.Encapsulate(BoundsFromVertices(
                sourceMesh.vertices.Select((vertex, index) => vertex + deformation.CrushDeltas[index]).ToArray()));
            combinedBounds.Encapsulate(BoundsFromVertices(
                sourceMesh.vertices.Select((vertex, index) => vertex + deformation.HeadDeltas[index]).ToArray()));
            combinedBounds.Encapsulate(BoundsFromVertices(
                sourceMesh.vertices.Select((vertex, index) => vertex + deformation.LowerJawDeltas[index]).ToArray()));
            generated.bounds = combinedBounds;

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

        private static DeformationData BuildDeformation(Mesh sourceMesh, SkinnedMeshRenderer targetRenderer)
        {
            var vertices = sourceMesh.vertices;
            var bounds = BoundsFromVertices(vertices);
            var crushDeltas = new Vector3[vertices.Length];
            var headDeltas = new Vector3[vertices.Length];
            var lowerJawDeltas = new Vector3[vertices.Length];
            var lowerJawWeights = BuildLowerJawWeights(sourceMesh, targetRenderer);
            var headPivot = new Vector3(0f, 0.76f, 0.58f);
            var headRotation = Quaternion.Euler(HeadPitchDegrees, -HeadYawDegrees, HeadRollDegrees);
            var headImpactTranslation = new Vector3(-0.045f, -0.02f, -0.025f);
            var lowerJawPivot = new Vector3(0f, 0.64f, 0.58f);
            var lowerJawRotation = Quaternion.Euler(10f, -18f, 13f);
            var lowerJawTranslation = new Vector3(-0.07f, -0.035f, -0.045f);
            for (var index = 0; index < vertices.Length; index++)
            {
                var vertex = vertices[index];
                // The model faces local +Z. From a frontal camera, screen-left is local +X.
                var frontViewLeftWeight = Mathf.SmoothStep(
                    0f,
                    1f,
                    Mathf.InverseLerp(0.02f, bounds.max.x * 0.92f, vertex.x));
                var groundedHeight = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(bounds.min.y, 0.32f, vertex.y));
                var headExclusion =
                    BandWeight(vertex.x, -0.78f, -0.62f, 0.62f, 0.78f) *
                    Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.48f, 0.82f, vertex.y)) *
                    Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.42f, 0.82f, vertex.z));
                var crushWeight = frontViewLeftWeight * (1f - headExclusion * 0.78f);
                var depthToCenter = bounds.center.z - vertex.z;
                crushDeltas[index] = new Vector3(
                    -MaximumCrushDistance * crushWeight,
                    -CrushVerticalDrop * groundedHeight * crushWeight,
                    depthToCenter * CrushDepthRatio * crushWeight);

                var headWeight =
                    BandWeight(vertex.x, -0.82f, -0.68f, 0.68f, 0.82f) *
                    Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.42f, 0.72f, vertex.y)) *
                    Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.42f, 0.76f, vertex.z));
                var impactedHeadSide = Mathf.SmoothStep(
                    0f,
                    1f,
                    Mathf.InverseLerp(0f, bounds.max.x * 0.72f, vertex.x));
                var rotated = headPivot + headRotation * (vertex - headPivot) + headImpactTranslation;
                var impactCompression = new Vector3(
                    -HeadImpactCompression * impactedHeadSide,
                    -HeadImpactCompression * 0.22f * impactedHeadSide,
                    -HeadImpactCompression * 0.45f * impactedHeadSide);
                headDeltas[index] = (rotated - vertex + impactCompression) * headWeight;

                var jawRotated = lowerJawPivot + lowerJawRotation * (vertex - lowerJawPivot) + lowerJawTranslation;
                lowerJawDeltas[index] = (jawRotated - vertex) * lowerJawWeights[index];
            }

            return new DeformationData(crushDeltas, headDeltas, lowerJawDeltas);
        }

        private static float[] BuildLowerJawWeights(Mesh sourceMesh, SkinnedMeshRenderer targetRenderer)
        {
            var bones = targetRenderer.bones;
            var lowerJawNames = new HashSet<string>(LowerJawSurfaceBoneNames, StringComparer.Ordinal);
            var lowerJawBoneIndices = new HashSet<int>(Enumerable.Range(0, bones.Length).Where(index =>
                bones[index] != null && lowerJawNames.Contains(bones[index].name)));
            if (lowerJawBoneIndices.Count != LowerJawSurfaceBoneNames.Length)
            {
                throw new InvalidOperationException("Parvum lower-jaw surface rigs Bone_011 through Bone_018 are incomplete.");
            }

            var rigWeights = new float[sourceMesh.vertexCount];
            var bonesPerVertex = sourceMesh.GetBonesPerVertex();
            var allWeights = sourceMesh.GetAllBoneWeights();
            try
            {
                var weightIndex = 0;
                for (var vertexIndex = 0; vertexIndex < sourceMesh.vertexCount; vertexIndex++)
                {
                    var influenceCount = bonesPerVertex[vertexIndex];
                    for (var influenceIndex = 0; influenceIndex < influenceCount; influenceIndex++)
                    {
                        var influence = allWeights[weightIndex++];
                        if (lowerJawBoneIndices.Contains(influence.boneIndex))
                        {
                            rigWeights[vertexIndex] += influence.weight;
                        }
                    }
                }
            }
            finally
            {
                bonesPerVertex.Dispose();
                allWeights.Dispose();
            }

            var vertices = sourceMesh.vertices;
            var weights = new float[sourceMesh.vertexCount];
            for (var index = 0; index < sourceMesh.vertexCount; index++)
            {
                var vertex = vertices[index];
                if (rigWeights[index] <= 0.02f || vertex.y >= 0.82f)
                {
                    continue;
                }

                weights[index] = Mathf.Clamp01(rigWeights[index]) *
                                 BandWeight(vertex.x, -0.62f, -0.54f, 0.54f, 0.62f) *
                                 BandWeight(vertex.y, 0.28f, 0.36f, 0.90f, 0.98f) *
                                 BandWeight(vertex.z, 0.44f, 0.54f, 1.36f, 1.44f);
            }

            return weights;
        }

        private static float BandWeight(float value, float outerMin, float innerMin, float innerMax, float outerMax)
        {
            var enter = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(outerMin, innerMin, value));
            var exit = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(innerMax, outerMax, value));
            return Mathf.Clamp01(Mathf.Min(enter, exit));
        }

        private static void AddBlendShape(Mesh mesh, string name, Vector3[] deltas)
        {
            var vertices = mesh.vertices;
            var targets = new Vector3[vertices.Length];
            for (var index = 0; index < vertices.Length; index++)
            {
                targets[index] = vertices[index] + deltas[index];
            }

            var deltaNormals = new Vector3[vertices.Length];
            var deltaTangents = new Vector3[vertices.Length];
            var sourceNormals = mesh.normals;
            var sourceTangents = mesh.tangents;
            var targetMesh = UnityEngine.Object.Instantiate(mesh);
            try
            {
                targetMesh.vertices = targets;
                targetMesh.RecalculateNormals();
                targetMesh.RecalculateTangents();
                var targetNormals = targetMesh.normals;
                var targetTangents = targetMesh.tangents;
                for (var index = 0; index < vertices.Length; index++)
                {
                    if (sourceNormals.Length == vertices.Length && targetNormals.Length == vertices.Length)
                    {
                        deltaNormals[index] = targetNormals[index] - sourceNormals[index];
                    }

                    if (sourceTangents.Length == vertices.Length && targetTangents.Length == vertices.Length)
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

            mesh.AddBlendShapeFrame(name, 100f, deltas, deltaNormals, deltaTangents);
        }

        private static AnimationClip EnsureClip(Transform hitSlot, SkinnedMeshRenderer renderer)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);
            if (clip == null)
            {
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, ClipPath);
            }

            clip.ClearCurves();
            clip.name = "Parvum_Hit_LeftCrush_NewModel";
            clip.frameRate = 60f;
            clip.wrapMode = WrapMode.Loop;
            var rendererPath = AnimationUtility.CalculateTransformPath(renderer.transform, hitSlot);
            SetBlendShapeCurve(
                clip,
                rendererPath,
                BodyCrushBlendShapeName,
                new Keyframe(0f, 0f),
                new Keyframe(ImpactOnsetTime, 8f),
                new Keyframe(ImpactPeakTime, 100f),
                new Keyframe(BodyRecoilTime, -28f),
                new Keyframe(BodyAftershockTime, 50f),
                new Keyframe(BodyDampingTime, -15f),
                new Keyframe(ImpactRecoveryTime, 0f),
                new Keyframe(CycleSeconds, 0f));
            SetBlendShapeCurve(
                clip,
                rendererPath,
                HeadShakeBlendShapeName,
                new Keyframe(0f, 0f),
                new Keyframe(ImpactOnsetTime, 8f),
                new Keyframe(ImpactPeakTime, 100f),
                new Keyframe(BodyRecoilTime, 58f),
                new Keyframe(HeadSettleTime, 24f),
                new Keyframe(ImpactRecoveryTime, 0f),
                new Keyframe(CycleSeconds, 0f));
            SetBlendShapeCurve(
                clip,
                rendererPath,
                LowerJawRecoilBlendShapeName,
                new Keyframe(0f, 0f),
                new Keyframe(ImpactOnsetTime, 4f),
                new Keyframe(ImpactPeakTime, 38f),
                new Keyframe(LowerJawPeakTime, 100f),
                new Keyframe(LowerJawReboundTime, -42f),
                new Keyframe(LowerJawAftershockTime, 24f),
                new Keyframe(LowerJawRecoveryTime, 0f),
                new Keyframe(CycleSeconds, 0f));

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.loopBlend = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
            return clip;
        }

        private static void SetBlendShapeCurve(
            AnimationClip clip,
            string rendererPath,
            string blendShape,
            params Keyframe[] keys)
        {
            var curve = new AnimationCurve(keys);
            for (var index = 0; index < curve.length; index++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, index, AnimationUtility.TangentMode.ClampedAuto);
                AnimationUtility.SetKeyRightTangentMode(curve, index, AnimationUtility.TangentMode.ClampedAuto);
            }

            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(
                    rendererPath,
                    typeof(SkinnedMeshRenderer),
                    "blendShape." + blendShape),
                curve);
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
                .FirstOrDefault(candidate => candidate.name == "Parvum_Hit_LeftCrush_NewModel") ??
                        stateMachine.AddState("Parvum_Hit_LeftCrush_NewModel");
            foreach (var child in stateMachine.states.Where(child => child.state != state).ToArray())
            {
                stateMachine.RemoveState(child.state);
            }

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
            Transform hitSlot,
            Transform model,
            SkinnedMeshRenderer renderer,
            Animator animator,
            AnimationClip clip,
            AnimatorController controller)
        {
            if (EditorUtility.scriptCompilationFailed)
            {
                throw new InvalidOperationException("Unity reports script compilation errors.");
            }

            if (renderer.sharedMesh == null ||
                !string.Equals(AssetDatabase.GetAssetPath(renderer.sharedMesh), GeneratedMeshPath, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Parvum hit renderer is not using the newly generated hit mesh.");
            }

            var sourceMesh = RequireSourceRenderer().sharedMesh;
            var mesh = renderer.sharedMesh;
            if (sourceMesh.vertexCount != mesh.vertexCount || sourceMesh.subMeshCount != mesh.subMeshCount)
            {
                throw new InvalidOperationException("Generated Parvum hit mesh changed source topology.");
            }

            if (mesh.blendShapeCount != 3 ||
                mesh.GetBlendShapeIndex(BodyCrushBlendShapeName) != 0 ||
                mesh.GetBlendShapeIndex(HeadShakeBlendShapeName) != 1 ||
                mesh.GetBlendShapeIndex(LowerJawRecoilBlendShapeName) != 2)
            {
                throw new InvalidOperationException("Generated Parvum hit mesh must contain only the three new hit BlendShapes.");
            }

            var expected = BuildDeformation(sourceMesh, renderer);
            var actualCrush = ReadBlendShapeDeltas(mesh, 0);
            var actualHead = ReadBlendShapeDeltas(mesh, 1);
            var actualLowerJaw = ReadBlendShapeDeltas(mesh, 2);
            var crushAffected = 0;
            var headAffected = 0;
            var lowerJawAffected = 0;
            var maximumInwardCrush = 0f;
            var oppositeSideTravel = 0f;
            var maximumHeadLeftTravel = 0f;
            var maximumLowerJawTravel = 0f;
            for (var index = 0; index < sourceMesh.vertexCount; index++)
            {
                if ((actualCrush[index] - expected.CrushDeltas[index]).sqrMagnitude >
                    GeometryTolerance * GeometryTolerance ||
                    (actualHead[index] - expected.HeadDeltas[index]).sqrMagnitude >
                    GeometryTolerance * GeometryTolerance ||
                    (actualLowerJaw[index] - expected.LowerJawDeltas[index]).sqrMagnitude >
                    GeometryTolerance * GeometryTolerance)
                {
                    throw new InvalidOperationException(
                        "Generated Parvum hit deformation does not match the new formula at vertex " +
                        index.ToString(CultureInfo.InvariantCulture) + ".");
                }

                if (actualCrush[index].sqrMagnitude > GeometryTolerance * GeometryTolerance)
                {
                    crushAffected++;
                }

                if (actualHead[index].sqrMagnitude > GeometryTolerance * GeometryTolerance)
                {
                    headAffected++;
                }

                if (actualLowerJaw[index].sqrMagnitude > GeometryTolerance * GeometryTolerance)
                {
                    lowerJawAffected++;
                }

                maximumInwardCrush = Mathf.Max(maximumInwardCrush, -actualCrush[index].x);
                maximumHeadLeftTravel = Mathf.Max(maximumHeadLeftTravel, -actualHead[index].x);
                maximumLowerJawTravel = Mathf.Max(maximumLowerJawTravel, actualLowerJaw[index].magnitude);
                if (sourceMesh.vertices[index].x < -0.02f)
                {
                    oppositeSideTravel = Mathf.Max(oppositeSideTravel, actualCrush[index].magnitude);
                }
            }

            if (crushAffected < sourceMesh.vertexCount * 0.12f || maximumInwardCrush < 0.30f)
            {
                throw new InvalidOperationException(
                    "Parvum front-view-left body crush is not broad or deep enough. Affected=" +
                    crushAffected.ToString(CultureInfo.InvariantCulture) +
                    ", Inward=" + Num(maximumInwardCrush) + ".");
            }

            if (oppositeSideTravel > 0.01f)
            {
                throw new InvalidOperationException(
                    "Parvum body crush leaked onto the opposite side. Travel=" + Num(oppositeSideTravel) + ".");
            }

            if (headAffected < sourceMesh.vertexCount * 0.10f ||
                maximumHeadLeftTravel < MinimumExpandedHeadTravel)
            {
                throw new InvalidOperationException(
                    "Parvum object-left head rotation radius is not visibly expanded. Affected=" +
                    headAffected.ToString(CultureInfo.InvariantCulture) +
                    ", LeftTravel=" + Num(maximumHeadLeftTravel) + ".");
            }

            if (lowerJawAffected < 300 || maximumLowerJawTravel < MinimumLowerJawTravel)
            {
                throw new InvalidOperationException(
                    "Parvum lower-jaw impact recoil is not broad or strong enough. Affected=" +
                    lowerJawAffected.ToString(CultureInfo.InvariantCulture) +
                    ", Travel=" + Num(maximumLowerJawTravel) + ".");
            }

            if (animator.runtimeAnimatorController != controller || animator.applyRootMotion ||
                string.Equals(AssetDatabase.GetAssetPath(controller), OldHitControllerPath, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Parvum hit Animator is not exclusively using the new controller.");
            }

            if (controller.animationClips.Length != 1 || controller.animationClips[0] != clip ||
                string.Equals(AssetDatabase.GetAssetPath(controller.animationClips[0]), OldHitClipPath, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("The new hit controller must contain only the new hit clip.");
            }

            var rendererPath = AnimationUtility.CalculateTransformPath(renderer.transform, hitSlot);
            var bindings = AnimationUtility.GetCurveBindings(clip);
            if (bindings.Length != 3 || bindings.Any(binding =>
                    !string.Equals(binding.path, rendererPath, StringComparison.Ordinal) ||
                    binding.type != typeof(SkinnedMeshRenderer)) ||
                bindings.Any(binding => binding.type == typeof(Transform)))
            {
                throw new InvalidOperationException("The new Parvum hit clip must contain only three BlendShape curves.");
            }

            var crushCurve = RequireCurve(clip, rendererPath, BodyCrushBlendShapeName);
            var headCurve = RequireCurve(clip, rendererPath, HeadShakeBlendShapeName);
            var lowerJawCurve = RequireCurve(clip, rendererPath, LowerJawRecoilBlendShapeName);
            RequireCurveValue(crushCurve, 0f, 0f, "rest body crush");
            RequireCurveValue(crushCurve, ImpactOnsetTime, 8f, "simultaneous body onset");
            RequireCurveValue(crushCurve, ImpactPeakTime, 100f, "peak body crush");
            RequireCurveValue(crushCurve, BodyRecoilTime, -28f, "strong outward body impact recoil");
            RequireCurveValue(crushCurve, BodyAftershockTime, 50f, "strong body recompression");
            RequireCurveValue(crushCurve, BodyDampingTime, -15f, "damped outward body settle");
            RequireCurveValue(crushCurve, ImpactRecoveryTime, 0f, "released body crush");
            RequireCurveValue(crushCurve, CycleSeconds, 0f, "recovered body crush");
            RequireCurveValue(headCurve, 0f, 0f, "rest head shake");
            RequireCurveValue(headCurve, ImpactOnsetTime, 8f, "simultaneous head onset");
            RequireCurveValue(headCurve, ImpactPeakTime, 100f, "single object-left head impact snap");
            RequireCurveValue(headCurve, BodyRecoilTime, 58f, "head impact recoil");
            RequireCurveValue(headCurve, HeadSettleTime, 24f, "head one-way settle");
            RequireCurveValue(headCurve, ImpactRecoveryTime, 0f, "recovered head shake");
            RequireCurveValue(headCurve, CycleSeconds, 0f, "loop-end head shake");
            RequireCurveValue(lowerJawCurve, 0f, 0f, "rest lower jaw");
            RequireCurveValue(lowerJawCurve, ImpactPeakTime, 38f, "lower-jaw impact lag");
            RequireCurveValue(lowerJawCurve, LowerJawPeakTime, 100f, "lower-jaw recoil peak");
            RequireCurveValue(lowerJawCurve, LowerJawReboundTime, -42f, "lower-jaw opposite rebound");
            RequireCurveValue(lowerJawCurve, LowerJawAftershockTime, 24f, "lower-jaw damped aftershock");
            RequireCurveValue(lowerJawCurve, LowerJawRecoveryTime, 0f, "recovered lower jaw");
            RequireCurveValue(lowerJawCurve, CycleSeconds, 0f, "loop-end lower jaw");
            if (crushCurve.Evaluate(ImpactOnsetTime) <= 0f || headCurve.Evaluate(ImpactOnsetTime) <= 0f ||
                Mathf.Abs(crushCurve.Evaluate(ImpactPeakTime) - headCurve.Evaluate(ImpactPeakTime)) > 0.05f)
            {
                throw new InvalidOperationException(
                    "Parvum body crush and object-left head impact must start and peak together.");
            }

            for (var sampleTime = ImpactPeakTime + 0.02f;
                 sampleTime <= ImpactRecoveryTime;
                 sampleTime += 0.02f)
            {
                if (headCurve.Evaluate(sampleTime) > headCurve.Evaluate(sampleTime - 0.02f) + 0.05f)
                {
                    throw new InvalidOperationException(
                        "Parvum head impact must settle from one snap without a second directional shake.");
                }
            }
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            if (Mathf.Abs(clip.length - CycleSeconds) > GeometryTolerance || !settings.loopTime)
            {
                throw new InvalidOperationException("The new Parvum hit clip must be an exact three-second cycle.");
            }

            var worldGroundDelta = MeasureWorldGroundDelta(hitSlot, renderer, animator, clip);
            if (worldGroundDelta > GroundTolerance)
            {
                throw new InvalidOperationException(
                    "The new Parvum hit motion changes ground contact. Delta=" + Num(worldGroundDelta) + ".");
            }

            RequireReviewPhysics(hitSlot);
            RequireOnlyHitConfigured(parvumRoot, hitSlot, animator);
            return new InspectionResult(
                sourceMesh.vertexCount,
                crushAffected,
                headAffected,
                lowerJawAffected,
                CycleSeconds,
                maximumInwardCrush,
                oppositeSideTravel,
                maximumHeadLeftTravel,
                maximumLowerJawTravel,
                worldGroundDelta,
                rendererPath,
                Sha256(Absolute(SourceModelPath)));
        }

        private static Vector3[] ReadBlendShapeDeltas(Mesh mesh, int shapeIndex)
        {
            var deltas = new Vector3[mesh.vertexCount];
            var normals = new Vector3[mesh.vertexCount];
            var tangents = new Vector3[mesh.vertexCount];
            mesh.GetBlendShapeFrameVertices(shapeIndex, 0, deltas, normals, tangents);
            return deltas;
        }

        private static AnimationCurve RequireCurve(AnimationClip clip, string rendererPath, string blendShape)
        {
            var binding = EditorCurveBinding.FloatCurve(
                rendererPath,
                typeof(SkinnedMeshRenderer),
                "blendShape." + blendShape);
            return AnimationUtility.GetEditorCurve(clip, binding) ??
                   throw new InvalidOperationException("Missing Parvum hit curve: " + blendShape + ".");
        }

        private static void RequireCurveValue(
            AnimationCurve curve,
            float time,
            float expected,
            string label)
        {
            if (Mathf.Abs(curve.Evaluate(time) - expected) > 0.05f)
            {
                throw new InvalidOperationException("Parvum hit curve value is invalid for " + label + ".");
            }
        }

        private static float MeasureWorldGroundDelta(
            Transform hitSlot,
            SkinnedMeshRenderer renderer,
            Animator animator,
            AnimationClip clip)
        {
            var transforms = hitSlot.GetComponentsInChildren<Transform>(true);
            var positions = transforms.Select(item => item.localPosition).ToArray();
            var rotations = transforms.Select(item => item.localRotation).ToArray();
            var scales = transforms.Select(item => item.localScale).ToArray();
            var weights = Enumerable.Range(0, renderer.sharedMesh.blendShapeCount)
                .Select(renderer.GetBlendShapeWeight).ToArray();
            var animatorEnabled = animator.enabled;
            try
            {
                animator.enabled = false;
                clip.SampleAnimation(hitSlot.gameObject, 0f);
                var rest = BakedWorldBounds(renderer).min.y;
                var maximumDelta = 0f;
                foreach (var time in CaptureTimes)
                {
                    clip.SampleAnimation(hitSlot.gameObject, time);
                    maximumDelta = Mathf.Max(maximumDelta, Mathf.Abs(BakedWorldBounds(renderer).min.y - rest));
                }

                return maximumDelta;
            }
            finally
            {
                for (var index = 0; index < transforms.Length; index++)
                {
                    transforms[index].localPosition = positions[index];
                    transforms[index].localRotation = rotations[index];
                    transforms[index].localScale = scales[index];
                }

                for (var index = 0; index < weights.Length; index++)
                {
                    renderer.SetBlendShapeWeight(index, weights[index]);
                }

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
                if (vertices.Length == 0)
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

        private static void CaptureComparison(
            Transform hitSlot,
            SkinnedMeshRenderer renderer,
            Animator animator,
            AnimationClip clip,
            string destination)
        {
            var transforms = hitSlot.GetComponentsInChildren<Transform>(true);
            var layers = transforms.Select(item => item.gameObject.layer).ToArray();
            var positions = transforms.Select(item => item.localPosition).ToArray();
            var rotations = transforms.Select(item => item.localRotation).ToArray();
            var scales = transforms.Select(item => item.localScale).ToArray();
            var weights = Enumerable.Range(0, renderer.sharedMesh.blendShapeCount)
                .Select(renderer.GetBlendShapeWeight).ToArray();
            var animatorEnabled = animator.enabled;
            var updateWhenOffscreen = renderer.updateWhenOffscreen;
            var forceRecalculation = renderer.forceMatrixRecalculationPerRender;
            var localBounds = renderer.localBounds;
            var previousActive = RenderTexture.active;
            var target = new RenderTexture(PanelWidth, CaptureHeight, 24, RenderTextureFormat.ARGB32);
            var panelImage = new Texture2D(PanelWidth, CaptureHeight, TextureFormat.RGB24, false);
            var composite = new Texture2D(
                PanelWidth * CaptureTimes.Length,
                CaptureHeight * 2,
                TextureFormat.RGB24,
                false);
            var cameraObject = new GameObject("ParvumHitReviewCamera", typeof(Camera))
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            var lightObject = new GameObject("ParvumHitReviewLight", typeof(Light))
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            try
            {
                animator.enabled = false;
                renderer.updateWhenOffscreen = true;
                renderer.forceMatrixRecalculationPerRender = true;
                renderer.localBounds = new Bounds(renderer.sharedMesh.bounds.center, Vector3.one * 20f);
                for (var index = 0; index < transforms.Length; index++)
                {
                    transforms[index].gameObject.layer = ReviewLayer;
                }

                Bounds reviewBounds = default;
                var hasBounds = false;
                foreach (var time in CaptureTimes)
                {
                    clip.SampleAnimation(hitSlot.gameObject, time);
                    var sampled = BakedWorldBounds(renderer);
                    if (!hasBounds)
                    {
                        reviewBounds = sampled;
                        hasBounds = true;
                    }
                    else
                    {
                        reviewBounds.Encapsulate(sampled);
                    }
                }

                var camera = cameraObject.GetComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.012f, 0.016f, 0.02f, 1f);
                camera.cullingMask = 1 << ReviewLayer;
                camera.allowHDR = false;
                camera.allowMSAA = false;
                camera.fieldOfView = 32f;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 1000f;
                camera.targetTexture = target;
                camera.aspect = PanelWidth / (float)CaptureHeight;

                var worldForward = renderer.transform.TransformDirection(Vector3.forward).normalized;
                var worldRight = renderer.transform.TransformDirection(Vector3.right).normalized;
                var frontDirection = (-worldForward + Vector3.down * 0.025f).normalized;
                var threeQuarterDirection = (-worldForward + worldRight * 0.72f + Vector3.down * 0.025f).normalized;
                var frontPosition = CameraPosition(reviewBounds, frontDirection, camera);
                var threeQuarterPosition = CameraPosition(reviewBounds, threeQuarterDirection, camera);

                var light = lightObject.GetComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.35f;
                light.color = new Color(0.9f, 0.95f, 1f);
                light.cullingMask = 1 << ReviewLayer;
                light.shadows = LightShadows.None;

                for (var panel = 0; panel < CaptureTimes.Length; panel++)
                {
                    clip.SampleAnimation(hitSlot.gameObject, CaptureTimes[panel]);
                    RenderPanel(camera, light, frontPosition, frontDirection, target, panelImage);
                    composite.SetPixels32(
                        panel * PanelWidth,
                        CaptureHeight,
                        PanelWidth,
                        CaptureHeight,
                        panelImage.GetPixels32());

                    RenderPanel(camera, light, threeQuarterPosition, threeQuarterDirection, target, panelImage);
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
                    transforms[index].gameObject.layer = layers[index];
                    transforms[index].localPosition = positions[index];
                    transforms[index].localRotation = rotations[index];
                    transforms[index].localScale = scales[index];
                }

                for (var index = 0; index < weights.Length; index++)
                {
                    renderer.SetBlendShapeWeight(index, weights[index]);
                }

                renderer.updateWhenOffscreen = updateWhenOffscreen;
                renderer.forceMatrixRecalculationPerRender = forceRecalculation;
                renderer.localBounds = localBounds;
                animator.enabled = animatorEnabled;
                RenderTexture.active = previousActive;
                cameraObject.GetComponent<Camera>().targetTexture = null;
                UnityEngine.Object.DestroyImmediate(panelImage);
                UnityEngine.Object.DestroyImmediate(composite);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(lightObject);
            }
        }

        private static Vector3 CameraPosition(Bounds bounds, Vector3 direction, Camera camera)
        {
            var verticalRadians = camera.fieldOfView * 0.5f * Mathf.Deg2Rad;
            var horizontalRadians = Mathf.Atan(Mathf.Tan(verticalRadians) * camera.aspect);
            var distance = Mathf.Max(
                bounds.extents.y / Mathf.Max(0.01f, Mathf.Tan(verticalRadians)),
                bounds.extents.x / Mathf.Max(0.01f, Mathf.Tan(horizontalRadians))) * 1.35f;
            return bounds.center - direction * distance;
        }

        private static void RenderPanel(
            Camera camera,
            Light light,
            Vector3 position,
            Vector3 direction,
            RenderTexture target,
            Texture2D panelImage)
        {
            var rotation = Quaternion.LookRotation(direction, Vector3.up);
            camera.transform.SetPositionAndRotation(position, rotation);
            light.transform.rotation = Quaternion.LookRotation(
                direction + new Vector3(-0.4f, -0.5f, 0.2f),
                Vector3.up);
            RenderTexture.active = target;
            camera.Render();
            panelImage.ReadPixels(new Rect(0, 0, PanelWidth, CaptureHeight), 0, 0);
            panelImage.Apply();
        }

        private static void RequireReviewPhysics(Transform hitSlot)
        {
            var body = hitSlot.GetComponent<Rigidbody>() ??
                       throw new InvalidOperationException("Parvum hit Rigidbody is missing.");
            var collider = hitSlot.GetComponent<Collider>() ??
                           throw new InvalidOperationException("Parvum hit Collider is missing.");
            var driver = hitSlot.GetComponent<ParvumPhysicsMotionDriver>() ??
                         throw new InvalidOperationException("Parvum hit physics motion driver is missing.");
            if (!body.isKinematic || !collider.enabled || !driver.LockRootMotionForReview ||
                driver.MotionPathTarget == null)
            {
                throw new InvalidOperationException("Parvum hit review physics binding is invalid.");
            }
        }

        private static void RequireOnlyHitConfigured(
            Transform parvumRoot,
            Transform hitSlot,
            Animator hitAnimator)
        {
            for (var index = 0; index < parvumRoot.childCount; index++)
            {
                var slot = parvumRoot.GetChild(index);
                if (slot == hitSlot)
                {
                    if (slot.GetComponentsInChildren<Animator>(true)
                            .Count(candidate => candidate.runtimeAnimatorController != null) != 1)
                    {
                        throw new InvalidOperationException("Parvum hit must have exactly one configured Animator.");
                    }

                    continue;
                }

                if (slot.GetComponentsInChildren<Animator>(true)
                    .Any(candidate => candidate.runtimeAnimatorController == hitAnimator.runtimeAnimatorController))
                {
                    throw new InvalidOperationException(slot.name + " unexpectedly uses the new Parvum hit controller.");
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
                    "Current Parvum hit model must contain exactly one active SkinnedMeshRenderer. Count=" +
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
                throw new InvalidOperationException("Current Parvum hit renderer does not match the supplied GLB mesh.");
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
                .Where(slot => !string.Equals(slot.name, HitSlotName, StringComparison.Ordinal))
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
                foreach (var bodyRenderer in item.GetComponents<SkinnedMeshRenderer>())
                {
                    builder.Append("Mesh=").Append(AssetDatabase.GetAssetPath(bodyRenderer.sharedMesh)).AppendLine();
                }

                foreach (var childAnimator in item.GetComponents<Animator>())
                {
                    builder.Append("Controller=")
                        .Append(AssetDatabase.GetAssetPath(childAnimator.runtimeAnimatorController)).AppendLine();
                }
            }

            return builder.ToString();
        }

        private static string PhysicsSignature(Transform hitSlot)
        {
            var body = hitSlot.GetComponent<Rigidbody>() ??
                       throw new InvalidOperationException("Parvum hit Rigidbody is missing.");
            var collider = hitSlot.GetComponent<Collider>() ??
                           throw new InvalidOperationException("Parvum hit Collider is missing.");
            var driver = hitSlot.GetComponent<ParvumPhysicsMotionDriver>() ??
                         throw new InvalidOperationException("Parvum hit physics motion driver is missing.");
            return EditorJsonUtility.ToJson(body) + "|" +
                   EditorJsonUtility.ToJson(collider) + "|" +
                   EditorJsonUtility.ToJson(driver) + "|Target=" +
                   (driver.MotionPathTarget != null
                       ? AnimationUtility.CalculateTransformPath(driver.MotionPathTarget, hitSlot)
                       : "<missing>");
        }

        private static string TransformSignature(Transform item)
        {
            return Vec(item.localPosition) + "|" + Vec(item.localEulerAngles) + "|" + Vec(item.localScale);
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
                .AppendLine("Parvum New Left-Crush Hit Motion Report")
                .AppendLine("Result=PASS")
                .AppendLine("Target=" + ParvumRootName + "/" + HitSlotName + "/" + ModelName)
                .AppendLine("SourceModel=" + SourceModelPath)
                .AppendLine("SourceSha256=" + result.SourceSha256)
                .AppendLine("GeneratedMesh=" + GeneratedMeshPath)
                .AppendLine("AnimationClip=" + ClipPath)
                .AppendLine("AnimatorController=" + ControllerPath)
                .AppendLine("ExistingParvumAnimationAssetsUsed=False")
                .AppendLine("OldHitClipAssigned=False")
                .AppendLine("OldHitControllerAssigned=False")
                .AppendLine("RendererPath=" + result.RendererPath)
                .AppendLine("BodyCrushBlendShape=" + BodyCrushBlendShapeName)
                .AppendLine("HeadShakeBlendShape=" + HeadShakeBlendShapeName)
                .AppendLine("LowerJawRecoilBlendShape=" + LowerJawRecoilBlendShapeName)
                .AppendLine("VertexCount=" + result.VertexCount.ToString(CultureInfo.InvariantCulture))
                .AppendLine("CrushAffectedVertexCount=" + result.CrushAffectedVertexCount.ToString(CultureInfo.InvariantCulture))
                .AppendLine("HeadAffectedVertexCount=" + result.HeadAffectedVertexCount.ToString(CultureInfo.InvariantCulture))
                .AppendLine("LowerJawAffectedVertexCount=" + result.LowerJawAffectedVertexCount.ToString(CultureInfo.InvariantCulture))
                .AppendLine("CycleSeconds=" + Num(result.CycleSeconds))
                .AppendLine("Loop=True")
                .AppendLine("BodyCrushSide=FrontViewLeft")
                .AppendLine("BodyCrushLocalMapping=LocalPositiveXTowardCenter")
                .AppendLine("MaximumInwardCrush=" + Num(result.MaximumInwardCrush))
                .AppendLine("OppositeSideMaximumTravel=" + Num(result.OppositeSideMaximumTravel))
                .AppendLine("HeadShakeSide=ParvumObjectLeft")
                .AppendLine("HeadShakeLocalDirection=LocalNegativeX")
                .AppendLine("HeadShakeCount=1")
                .AppendLine("MaximumHeadLeftTravel=" + Num(result.MaximumHeadLeftTravel))
                .AppendLine("MaximumLowerJawRecoilTravel=" + Num(result.MaximumLowerJawTravel))
                .AppendLine("LowerJawSurfaceRigs=" + string.Join(",", LowerJawSurfaceBoneNames))
                .AppendLine("BodyReaction=StrongCrushOutwardReboundRecompressionAndDampedSettle")
                .AppendLine("HeadReaction=LargeSingleSideImpactSnapWithPitchYawRollAndLocalCompression")
                .AppendLine("LowerJawReaction=DelayedPeakOppositeReboundAndDampedAftershock")
                .AppendLine("HeadImpactEulerDegrees=(12,-48,20)")
                .AppendLine("HeadRotationRadius=StrongSideImpactSilhouette")
                .AppendLine("BodyHeadTiming=SimultaneousStartAndImpactPeak")
                .AppendLine("BodyWeightPhases=0,8,100,-28,50,-15,0,0")
                .AppendLine("LowerJawWeightPhases=0,4,38,100,-42,24,0,0")
                .AppendLine("PhaseTimes=0,0.08,0.18,0.26,0.30,0.38,0.40,0.50,0.52,0.68,0.72,3")
                .AppendLine("WorldGroundDelta=" + Num(result.WorldGroundDelta))
                .AppendLine("RootTransformCurves=False")
                .AppendLine("ModelTransformCurves=False")
                .AppendLine("RigidbodyColliderDriverPreserved=True")
                .AppendLine("OtherParvumSlotsChanged=False")
                .AppendLine("OtherSceneRootsChanged=False")
                .AppendLine("CaptureCreated=" + (captureCreated ? "True" : "False"))
                .AppendLine("CapturePath=" + CapturePath)
                .AppendLine("HarnessValidationRun=False")
                .ToString();
            var destination = Absolute(ReportPath);
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                                      throw new InvalidOperationException("Invalid Parvum hit report path."));
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

        private readonly struct DeformationData
        {
            public DeformationData(Vector3[] crushDeltas, Vector3[] headDeltas, Vector3[] lowerJawDeltas)
            {
                CrushDeltas = crushDeltas;
                HeadDeltas = headDeltas;
                LowerJawDeltas = lowerJawDeltas;
            }

            public Vector3[] CrushDeltas { get; }
            public Vector3[] HeadDeltas { get; }
            public Vector3[] LowerJawDeltas { get; }
        }

        private readonly struct InspectionResult
        {
            public InspectionResult(
                int vertexCount,
                int crushAffectedVertexCount,
                int headAffectedVertexCount,
                int lowerJawAffectedVertexCount,
                float cycleSeconds,
                float maximumInwardCrush,
                float oppositeSideMaximumTravel,
                float maximumHeadLeftTravel,
                float maximumLowerJawTravel,
                float worldGroundDelta,
                string rendererPath,
                string sourceSha256)
            {
                VertexCount = vertexCount;
                CrushAffectedVertexCount = crushAffectedVertexCount;
                HeadAffectedVertexCount = headAffectedVertexCount;
                LowerJawAffectedVertexCount = lowerJawAffectedVertexCount;
                CycleSeconds = cycleSeconds;
                MaximumInwardCrush = maximumInwardCrush;
                OppositeSideMaximumTravel = oppositeSideMaximumTravel;
                MaximumHeadLeftTravel = maximumHeadLeftTravel;
                MaximumLowerJawTravel = maximumLowerJawTravel;
                WorldGroundDelta = worldGroundDelta;
                RendererPath = rendererPath;
                SourceSha256 = sourceSha256;
            }

            public int VertexCount { get; }
            public int CrushAffectedVertexCount { get; }
            public int HeadAffectedVertexCount { get; }
            public int LowerJawAffectedVertexCount { get; }
            public float CycleSeconds { get; }
            public float MaximumInwardCrush { get; }
            public float OppositeSideMaximumTravel { get; }
            public float MaximumHeadLeftTravel { get; }
            public float MaximumLowerJawTravel { get; }
            public float WorldGroundDelta { get; }
            public string RendererPath { get; }
            public string SourceSha256 { get; }
        }
    }
}
