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
    internal static class ParvumAttackMotionTool
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string ParvumRootName = "Approved Parvum Enemy Placement";
        private const string AttackSlotName = "Parvum_03_Attack";
        private const string ModelName = "Parvum_Model";
        private const string MotionTargetName = "MotionPath_Target_Rigidbody_Goal";
        private const string SourceModelPath = "Assets/_Project/Art/Enemies/Parvum/Models/parvum.glb";
        private const string OldAttackClipPath = "Assets/_Project/Art/Enemies/Parvum/Animations/Parvum_Attack.anim";
        private const string OldAttackControllerPath = "Assets/_Project/Art/Enemies/Parvum/Animations/Controllers/Parvum_Attack_Controller.controller";
        private const string GeneratedMeshPath = "Assets/_Project/Art/Enemies/Parvum/Models/parvum_attack_bite_mesh.asset";
        private const string ClipPath = "Assets/_Project/Art/Enemies/Parvum/Animations/Parvum_Attack_NewModel.anim";
        private const string ControllerPath = "Assets/_Project/Art/Enemies/Parvum/Animations/Controllers/Parvum_Attack_NewModel_Controller.controller";
        private const string OpenRootBlendShapeName = "Attack_Upper_Lower_Mouth_Roots_Open";
        private const string BiteRootBlendShapeName = "Attack_Upper_Lower_Mouth_Roots_Bite";
        private const string BodyImpactBlendShapeName = "Attack_Full_Body_Impact_Expansion";
        private const string UpperMouthRootBoneName = "Bone_002";
        private const string LowerMouthRootBoneName = "Bone_018";
        private const string LowerMouthRootSecondBoneName = "Bone_017";
        private const string LowerJawRootBoneName = "Bone_016";
        private const string InnerMouthRootBoneName = "Bone_008";
        private const string OutputFolder = "docs/validation/parvum_attack_motion_2026-08-15";
        private const string ReportPath = OutputFolder + "/Parvum_Attack_Motion_Report.txt";
        private const string CapturePath = OutputFolder + "/Parvum_Attack_Motion_Final_Comparison.png";
        private const string OuterLipIdentificationReportPath = OutputFolder + "/Parvum_Attack_Outer_Lip_Identification.txt";
        private const string ExpectedSourceSha256 = "E27840896F1DFA15BEE6F45F2BA943D28375A485E141907283CF79446B5640AB";

        // User-approved three-second attack presentation cycle.
        private const float CycleSeconds = 3f;
        private const float WideOpenTime = 0.84f;
        private const float ForwardLeanTime = 1.80f;
        // Holds the full mouth opening until this time so the close and body lunge land as one forceful bite.
        private const float BiteSnapStartTime = 1.96f;
        private const float BiteImpactTime = 2.28f;
        // Briefly holds most of the impact pose before the longer recovery begins.
        private const float ImpactFollowThroughTime = 2.64f;
        private const float RecoveryTime = 3f;
        private const float MouthRootOpenRatio = 0.65f;
        private const float MouthRootBiteRatio = 0.50f;
        // Advances both lip surfaces by a geometry-scaled amount during impact so the bite closes toward the target.
        private const float LipPuckerForwardGapRatio = 0.50f;
        // Outer-rim boosts keep the body-side mouth silhouette readable from the in-game side camera.
        private const float OuterLipOpenBoost = 0.45f;
        private const float OuterUpperBiteBoost = 0.65f;
        private const float OuterLowerBiteBoost = 1.10f;
        private const float OuterLipPuckerBoost = 0.60f;
        // Image #2 is the foremost biting assembly, not the averaged outer-mouth group.
        // Its own measured aperture drives a near-contact snap at impact.
        private const float FrontMouthMinimumZ = 1.12f;
        private const float FrontMouthOpenRatio = 0.42f;
        // The broad exterior muzzle must remain visibly separated instead of using the tooth-center contact gap.
        private const float FrontMouthBiteApertureRatio = 0.085f;
        private const float FrontUpperClosureShare = 0.48f;
        private const float FrontRigidMotionBoost = 1.08f;
        // The forceful bite is driven by a grounded full-body bulge instead of moving the model Transform.
        private const float BodyImpactForwardExpansionMaximum = 0.68f;
        private const float BodyImpactForwardExpansionRatio = 0.25f;
        private const float BodyImpactSideExpansionRatio = 0.19f;
        private const float BodyImpactVerticalExpansionRatio = 0.14f;
        private const float GeometryTolerance = 0.0001f;
        private const int ReviewLayer = 31;
        private const int PanelWidth = 420;
        private const int CaptureHeight = 640;

        private static readonly float[] CaptureTimes =
            { 0f, WideOpenTime, ForwardLeanTime, BiteImpactTime, RecoveryTime };

        private static readonly string[] ToothBranchRootBoneNames =
            { "Bone_009", "Bone_010", "Bone_020", "Bone_022", "Bone_024", "Bone_026" };

        private static readonly string[] UpperMouthSurfaceBoneNames =
            { "Bone_002", "Bone_003", "Bone_004", "Bone_005", "Bone_006" };

        private static readonly string[] LowerMouthSurfaceBoneNames =
            { "Bone_011", "Bone_012", "Bone_013", "Bone_014", "Bone_015", "Bone_016", "Bone_017", "Bone_018" };

        [MenuItem("Bellerophon/Enemies/Parvum/Apply New-Model Bite Attack")]
        public static void ApplyParvumAttackMotion()
        {
            var scene = RequireCurrentScene();
            RequireSourceHash();
            var parvumRoot = RequireRoot(ParvumRootName).transform;
            var attackSlot = RequireDirectChild(parvumRoot, AttackSlotName);
            var model = RequireDirectChild(attackSlot, ModelName);
            var renderer = RequireSingleBodyRenderer(model);
            var sourceRenderer = RequireSourceRenderer();
            RequireCompatibleSource(renderer, sourceRenderer);
            if (scene.isDirty &&
                !string.Equals(AssetDatabase.GetAssetPath(renderer.sharedMesh), GeneratedMeshPath, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "CargoRunMvp has unsaved editor changes outside the failed Parvum attack mesh assignment; " +
                    "the attack was not applied.");
            }
            RequireLocalPositiveZForward(model);
            var motionTarget = FindChildRecursive(attackSlot, MotionTargetName) ??
                               throw new InvalidOperationException("Parvum attack Motion Path target is missing.");
            var physicsBefore = RequireReviewPhysics(attackSlot, motionTarget);
            var protectedBefore = ProtectedRootSignatures(scene);
            var otherSlotsBefore = OtherParvumSlotSignatures(parvumRoot);
            var slotTransformBefore = TransformSignature(attackSlot);
            var modelTransformBefore = TransformSignature(model);

            var generatedMesh = EnsureGeneratedMesh(renderer, sourceRenderer);
            renderer.sharedMesh = generatedMesh;
            renderer.localBounds = generatedMesh.bounds;
            for (var index = 0; index < generatedMesh.blendShapeCount; index++)
            {
                renderer.SetBlendShapeWeight(index, 0f);
            }

            var animator = attackSlot.GetComponent<Animator>();
            if (animator == null)
            {
                animator = attackSlot.gameObject.AddComponent<Animator>();
            }

            RequireLipBlendShapeMotion(model, renderer, animator);
            var clip = EnsureClip(attackSlot, renderer);
            var controller = EnsureController(clip);
            animator.runtimeAnimatorController = controller;
            animator.avatar = null;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.enabled = true;

            var result = InspectState(parvumRoot, attackSlot, model, renderer, animator, clip, controller, motionTarget);
            if (!string.Equals(slotTransformBefore, TransformSignature(attackSlot), StringComparison.Ordinal) ||
                !string.Equals(modelTransformBefore, TransformSignature(model), StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Parvum attack slot or model Transform changed during setup.");
            }

            if (!string.Equals(physicsBefore, PhysicsSignature(attackSlot, motionTarget), StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Parvum attack Rigidbody, Collider, driver, or Motion Path target changed.");
            }

            if (!otherSlotsBefore.SequenceEqual(OtherParvumSlotSignatures(parvumRoot), StringComparer.Ordinal))
            {
                throw new InvalidOperationException("A non-attack Parvum slot changed during attack setup.");
            }

            if (!protectedBefore.SequenceEqual(ProtectedRootSignatures(scene), StringComparer.Ordinal))
            {
                throw new InvalidOperationException("A scene root outside Parvum changed during attack setup.");
            }

            EditorUtility.SetDirty(renderer);
            EditorUtility.SetDirty(animator);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException("CargoRunMvp could not be saved after applying the Parvum attack.");
            }

            AssetDatabase.SaveAssets();
            WriteReport(result, false);
            Debug.Log(
                "ParvumAttackMotionApplied Result=PASS" +
                ", Target=" + ParvumRootName + "/" + AttackSlotName + "/" + ModelName +
                ", CycleSeconds=" + Num(result.CycleSeconds) +
                ", WideOpenPercent=" + Num(result.WideOpenPercent) +
                ", BiteClosurePercent=" + Num(result.BiteClosurePercent) +
                ", ForwardAdvance=" + Num(result.ForwardLeanDistance) +
                ", ImpactLunge=" + Num(result.ImpactLungeDistance) +
                ", ModelVerticalTravel=" + Num(result.ModelVerticalTravel) +
                ", ModelRotationTravel=" + Num(result.ModelRotationTravel) +
                ", ModelForwardPositionTravel=" + Num(result.ModelForwardPositionTravel) +
                ", UpperLipOpenLift=" + Num(result.UpperLipOpenLift) +
                ", LowerLipOpenDrop=" + Num(result.LowerLipOpenDrop) +
                ", UpperLipBiteForward=" + Num(result.UpperLipBiteForward) +
                ", LowerLipBiteForward=" + Num(result.LowerLipBiteForward) +
                ", OuterUpperLipBiteDown=" + Num(result.OuterUpperLipBiteDown) +
                ", OuterLowerLipBiteUp=" + Num(result.OuterLowerLipBiteUp) +
                ", BodyImpactVertices=" + result.BodyImpactVertexCount.ToString(CultureInfo.InvariantCulture) +
                ", BodyImpactForwardExpansion=" + Num(result.BodyImpactForwardExpansion) +
                ", BodyImpactSideExpansion=" + Num(result.BodyImpactSideExpansion) +
                ", UpperRootVertices=" + result.UpperRootVertexCount.ToString(CultureInfo.InvariantCulture) +
                ", LowerRootVertices=" + result.LowerRootVertexCount.ToString(CultureInfo.InvariantCulture) +
                ", OuterUpperRootVertices=" + result.OuterUpperRootVertexCount.ToString(CultureInfo.InvariantCulture) +
                ", OuterLowerRootVertices=" + result.OuterLowerRootVertexCount.ToString(CultureInfo.InvariantCulture) +
                ", InnerMouthVertices=" + result.InnerMouthVertexCount.ToString(CultureInfo.InvariantCulture) +
                ", RootTransformCurves=False" +
                ", OldAttackAssetsAssigned=False" +
                ", PhysicsPreserved=True" +
                ", OtherParvumSlotsChanged=False" +
                ", OtherSceneRootsChanged=False" +
                ", SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Parvum/Inspect New-Model Bite Attack")]
        public static void InspectParvumAttackMotion()
        {
            var scene = RequireCurrentScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException("CargoRunMvp must be saved before inspecting the Parvum attack.");
            }

            RequireSourceHash();
            var parvumRoot = RequireRoot(ParvumRootName).transform;
            var attackSlot = RequireDirectChild(parvumRoot, AttackSlotName);
            var model = RequireDirectChild(attackSlot, ModelName);
            var renderer = RequireSingleBodyRenderer(model);
            var animator = attackSlot.GetComponent<Animator>() ??
                           throw new InvalidOperationException("Parvum attack Animator is missing.");
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath) ??
                       throw new InvalidOperationException("New-model Parvum attack clip is missing.");
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) ??
                             throw new InvalidOperationException("New-model Parvum attack controller is missing.");
            var motionTarget = FindChildRecursive(attackSlot, MotionTargetName) ??
                               throw new InvalidOperationException("Parvum attack Motion Path target is missing.");
            RequireReviewPhysics(attackSlot, motionTarget);
            var result = InspectState(parvumRoot, attackSlot, model, renderer, animator, clip, controller, motionTarget);
            WriteReport(result, false);
            Debug.Log(
                "ParvumAttackMotionInspected Result=PASS" +
                ", CycleSeconds=" + Num(result.CycleSeconds) +
                ", WideOpenPercent=" + Num(result.WideOpenPercent) +
                ", BiteClosurePercent=" + Num(result.BiteClosurePercent) +
                ", ForwardAdvance=" + Num(result.ForwardLeanDistance) +
                ", ImpactLunge=" + Num(result.ImpactLungeDistance) +
                ", GroundDelta=" + Num(result.WorldGroundDelta) +
                ", ModelVerticalTravel=" + Num(result.ModelVerticalTravel) +
                ", ModelRotationTravel=" + Num(result.ModelRotationTravel) +
                ", ModelForwardPositionTravel=" + Num(result.ModelForwardPositionTravel) +
                ", UpperLipOpenLift=" + Num(result.UpperLipOpenLift) +
                ", LowerLipOpenDrop=" + Num(result.LowerLipOpenDrop) +
                ", UpperLipBiteForward=" + Num(result.UpperLipBiteForward) +
                ", LowerLipBiteForward=" + Num(result.LowerLipBiteForward) +
                ", OuterUpperLipBiteDown=" + Num(result.OuterUpperLipBiteDown) +
                ", OuterLowerLipBiteUp=" + Num(result.OuterLowerLipBiteUp) +
                ", BodyImpactVertices=" + result.BodyImpactVertexCount.ToString(CultureInfo.InvariantCulture) +
                ", BodyImpactForwardExpansion=" + Num(result.BodyImpactForwardExpansion) +
                ", BodyImpactSideExpansion=" + Num(result.BodyImpactSideExpansion) +
                ", MouthAndBodyImpactBlendShapes=True" +
                ", PhysicsPreserved=True" +
                ", RootTransformCurves=False.");
        }

        [MenuItem("Bellerophon/Enemies/Parvum/Inspect Attack Outer-Lip Region")]
        public static void InspectParvumAttackOuterLipRegion()
        {
            var scene = RequireCurrentScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException("CargoRunMvp must be saved before identifying the Parvum outer lips.");
            }

            RequireSourceHash();
            var parvumRoot = RequireRoot(ParvumRootName).transform;
            var attackSlot = RequireDirectChild(parvumRoot, AttackSlotName);
            var model = RequireDirectChild(attackSlot, ModelName);
            var renderer = RequireSingleBodyRenderer(model);
            var mesh = renderer.sharedMesh ?? throw new InvalidOperationException("Parvum attack mesh is missing.");
            var influence = BuildMouthRigInfluenceData(renderer, mesh);
            var vertices = mesh.vertices;
            var innerUpperWeights = new float[vertices.Length];
            var innerLowerWeights = new float[vertices.Length];
            for (var index = 0; index < vertices.Length; index++)
            {
                var vertex = vertices[index];
                innerUpperWeights[index] = influence.UpperRootWeights[index] * influence.ExclusionWeights[index] *
                                           BandWeight(vertex.x, -0.42f, -0.34f, 0.34f, 0.42f) *
                                           BandWeight(vertex.y, 0.78f, 0.84f, 1.10f, 1.17f) *
                                           BandWeight(vertex.z, 0.68f, 0.76f, 1.12f, 1.22f);
                innerLowerWeights[index] = Mathf.Clamp01(influence.LowerRootWeights[index]) * influence.ExclusionWeights[index] *
                                           BandWeight(vertex.x, -0.45f, -0.36f, 0.36f, 0.45f) *
                                           BandWeight(vertex.y, 0.52f, 0.60f, 0.88f, 0.94f) *
                                           BandWeight(vertex.z, 0.56f, 0.65f, 1.08f, 1.18f);
            }

            Directory.CreateDirectory(Absolute(OutputFolder));
            var report = new StringBuilder()
                .AppendLine("Parvum Attack Outer-Lip Identification")
                .AppendLine("Result=PASS")
                .AppendLine("Target=" + ParvumRootName + "/" + AttackSlotName + "/" + ModelName)
                .AppendLine("LocalForward=+Z")
                .AppendLine("UpperSurfaceRigs=" + string.Join(",", UpperMouthSurfaceBoneNames))
                .AppendLine("LowerSurfaceRigs=" + string.Join(",", LowerMouthSurfaceBoneNames))
                .AppendLine("RigidUpperToothBranchRigs=" + string.Join(",", ToothBranchRootBoneNames));
            AppendVertexGroup(report, "UpperRigEligible", vertices, influence.UpperRootWeights, influence.ExclusionWeights);
            AppendVertexGroup(report, "LowerRigEligible", vertices, influence.LowerRootWeights, influence.ExclusionWeights);
            AppendVertexGroup(report, "CurrentInnerUpper", vertices, innerUpperWeights, null);
            AppendVertexGroup(report, "CurrentInnerLower", vertices, innerLowerWeights, null);
            AppendSubMeshReport(report, renderer, mesh, vertices);
            AppendConnectedComponentReport(report, renderer, mesh, vertices);
            AppendBoneHierarchyReport(report, renderer);
            AppendBoneBranchInfluenceReport(report, renderer, mesh, vertices, ToothBranchRootBoneNames);
            AppendBoneBranchInfluenceReport(report, renderer, mesh, vertices, LowerMouthSurfaceBoneNames);
            AppendAxisHistogram(report, "UpperEligibleZ", vertices, influence.UpperRootWeights, influence.ExclusionWeights, 2, 0.2f, 1.4f, 0.1f);
            AppendAxisHistogram(report, "UpperEligibleY", vertices, influence.UpperRootWeights, influence.ExclusionWeights, 1, 0.4f, 1.5f, 0.1f);
            AppendAxisHistogram(report, "LowerEligibleZ", vertices, influence.LowerRootWeights, influence.ExclusionWeights, 2, 0.2f, 1.4f, 0.1f);
            AppendAxisHistogram(report, "LowerEligibleY", vertices, influence.LowerRootWeights, influence.ExclusionWeights, 1, 0.2f, 1.2f, 0.1f);
            File.WriteAllText(Absolute(OuterLipIdentificationReportPath), report.ToString(), new UTF8Encoding(false));
            Debug.Log(
                "ParvumAttackOuterLipRegionInspected Result=PASS" +
                ", Report=" + OuterLipIdentificationReportPath +
                ", CurrentInnerUpperVertices=" + innerUpperWeights.Count(weight => weight > GeometryTolerance) +
                ", CurrentInnerLowerVertices=" + innerLowerWeights.Count(weight => weight > GeometryTolerance) +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Parvum/Capture New-Model Bite Attack Comparison")]
        public static void CaptureParvumAttackMotionComparison()
        {
            var scene = RequireCurrentScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException("CargoRunMvp must be saved before capturing the Parvum attack.");
            }

            var parvumRoot = RequireRoot(ParvumRootName).transform;
            var attackSlot = RequireDirectChild(parvumRoot, AttackSlotName);
            var model = RequireDirectChild(attackSlot, ModelName);
            var renderer = RequireSingleBodyRenderer(model);
            var animator = attackSlot.GetComponent<Animator>() ??
                           throw new InvalidOperationException("Parvum attack Animator is missing.");
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath) ??
                       throw new InvalidOperationException("New-model Parvum attack clip is missing.");
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) ??
                             throw new InvalidOperationException("New-model Parvum attack controller is missing.");
            var motionTarget = FindChildRecursive(attackSlot, MotionTargetName) ??
                               throw new InvalidOperationException("Parvum attack Motion Path target is missing.");
            var result = InspectState(parvumRoot, attackSlot, model, renderer, animator, clip, controller, motionTarget);
            CaptureComparison(attackSlot, renderer, animator, clip, Absolute(CapturePath));
            if (scene.isDirty)
            {
                throw new InvalidOperationException("Parvum attack comparison capture changed the scene.");
            }

            WriteReport(result, true);
            Debug.Log(
                "ParvumAttackMotionCaptured Result=PASS" +
                ", Image=" + CapturePath +
                ", Times=0,0.84,1.80,2.28,3.00" +
                ", Phases=Rest,WideOpen,ForwardAdvance,ClosedBiteImpact,Recovered" +
                ", SceneChanged=False.");
        }

        private static Mesh EnsureGeneratedMesh(
            SkinnedMeshRenderer renderer,
            SkinnedMeshRenderer sourceRenderer)
        {
            var sourceMesh = sourceRenderer.sharedMesh ??
                             throw new InvalidOperationException("Supplied Parvum GLB mesh is missing.");
            var generated = UnityEngine.Object.Instantiate(sourceMesh);
            generated.name = "parvum_attack_bite_mesh";
            generated.ClearBlendShapes();
            var vertices = generated.vertices;
            var rootDeltas = BuildMouthRootDeltas(renderer, generated, out var rootAnalysis);
            var bodyImpactDeltas = BuildBodyImpactDeltas(renderer, generated, rootDeltas, out var bodyImpactAnalysis);
            var openTargets = new Vector3[vertices.Length];
            var biteTargets = new Vector3[vertices.Length];
            var bodyImpactTargets = new Vector3[vertices.Length];
            for (var index = 0; index < vertices.Length; index++)
            {
                openTargets[index] = vertices[index] + rootDeltas.OpenDeltas[index];
                biteTargets[index] = vertices[index] + rootDeltas.BiteDeltas[index];
                bodyImpactTargets[index] = vertices[index] + bodyImpactDeltas[index];
            }

            AddBlendShape(generated, OpenRootBlendShapeName, rootDeltas.OpenDeltas, openTargets);
            AddBlendShape(generated, BiteRootBlendShapeName, rootDeltas.BiteDeltas, biteTargets);
            AddBlendShape(generated, BodyImpactBlendShapeName, bodyImpactDeltas, bodyImpactTargets);
            var combinedBounds = generated.bounds;
            combinedBounds.Encapsulate(BoundsFromVertices(openTargets));
            combinedBounds.Encapsulate(BoundsFromVertices(biteTargets));
            combinedBounds.Encapsulate(BoundsFromVertices(bodyImpactTargets));
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
            Debug.Log(
                "ParvumAttackMeshBuilt UpperRootVertices=" + rootAnalysis.UpperVertexCount.ToString(CultureInfo.InvariantCulture) +
                ", LowerRootVertices=" + rootAnalysis.LowerVertexCount.ToString(CultureInfo.InvariantCulture) +
                ", OuterUpperRootVertices=" + rootAnalysis.OuterUpperVertexCount.ToString(CultureInfo.InvariantCulture) +
                ", OuterLowerRootVertices=" + rootAnalysis.OuterLowerVertexCount.ToString(CultureInfo.InvariantCulture) +
                ", RootOpenPercent=" + Num(rootAnalysis.OpenPercent) +
                ", RootBiteClosurePercent=" + Num(rootAnalysis.BiteClosurePercent) +
                ", LipPuckerForwardDistance=" + Num(rootAnalysis.PuckerForwardDistance) +
                ", BodyImpactVertices=" + bodyImpactAnalysis.AffectedVertexCount.ToString(CultureInfo.InvariantCulture) +
                ", BodyImpactForwardExpansion=" + Num(bodyImpactAnalysis.MaximumForwardDelta) +
                ", BodyImpactSideExpansion=" + Num(bodyImpactAnalysis.MaximumSideDelta) + ".");
            return existing;
        }

        private static void AddBlendShape(Mesh mesh, string name, Vector3[] deltas, Vector3[] targets)
        {
            BuildNormalAndTangentDeltas(mesh, targets, out var normalDeltas, out var tangentDeltas);
            mesh.AddBlendShapeFrame(name, 100f, deltas, normalDeltas, tangentDeltas);
        }

        private static Vector3[] BuildBodyImpactDeltas(
            SkinnedMeshRenderer renderer,
            Mesh mesh,
            MouthRootDeltas mouthDeltas,
            out BodyImpactAnalysis analysis)
        {
            var vertices = mesh.vertices;
            // Use immutable base-vertex bounds so rebuilding and inspecting the expanded mesh select the same region.
            var bounds = BoundsFromVertices(vertices);
            var influence = BuildMouthRigInfluenceData(renderer, mesh);
            var groundStart = bounds.min.y + bounds.size.y * 0.01f;
            var groundFull = bounds.min.y + bounds.size.y * 0.12f;
            var deltas = new Vector3[vertices.Length];
            var affectedVertexCount = 0;
            var maximumForwardDelta = 0f;
            var maximumSideDelta = 0f;
            var minimumAffectedHeight = float.PositiveInfinity;
            for (var index = 0; index < vertices.Length; index++)
            {
                if (influence.ExclusionWeights[index] <= GeometryTolerance ||
                    mouthDeltas.OpenDeltas[index].sqrMagnitude > GeometryTolerance * GeometryTolerance ||
                    mouthDeltas.BiteDeltas[index].sqrMagnitude > GeometryTolerance * GeometryTolerance)
                {
                    continue;
                }

                var vertex = vertices[index];
                var groundWeight = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(groundStart, groundFull, vertex.y));
                var weight = groundWeight;
                if (weight <= GeometryTolerance)
                {
                    continue;
                }

                var sideDelta = (vertex.x - bounds.center.x) * BodyImpactSideExpansionRatio * weight;
                var verticalDelta = Mathf.Max(0f, vertex.y - bounds.min.y) *
                                    BodyImpactVerticalExpansionRatio * weight;
                var forwardDelta = Mathf.Min(
                    BodyImpactForwardExpansionMaximum,
                    Mathf.Max(0f, vertex.z - bounds.min.z) * BodyImpactForwardExpansionRatio) * weight;
                deltas[index] = new Vector3(sideDelta, verticalDelta, forwardDelta);
                affectedVertexCount++;
                maximumForwardDelta = Mathf.Max(maximumForwardDelta, deltas[index].z);
                maximumSideDelta = Mathf.Max(maximumSideDelta, Mathf.Abs(deltas[index].x));
                minimumAffectedHeight = Mathf.Min(minimumAffectedHeight, vertex.y);
            }

            if (affectedVertexCount == 0)
            {
                throw new InvalidOperationException("Parvum front-body impact expansion region is empty.");
            }
            analysis = new BodyImpactAnalysis(
                affectedVertexCount,
                maximumForwardDelta,
                maximumSideDelta,
                minimumAffectedHeight - bounds.min.y);
            return deltas;
        }

        private static MouthRootDeltas BuildMouthRootDeltas(
            SkinnedMeshRenderer renderer,
            Mesh mesh,
            out MouthRootAnalysis analysis)
        {
            var bones = renderer.bones;
            var upperSurfaceNames = new HashSet<string>(UpperMouthSurfaceBoneNames, StringComparer.Ordinal);
            var lowerSurfaceNames = new HashSet<string>(LowerMouthSurfaceBoneNames, StringComparer.Ordinal);
            var upperRootIndices = new HashSet<int>(Enumerable.Range(0, bones.Length).Where(index =>
                bones[index] != null && upperSurfaceNames.Contains(bones[index].name)));
            var lowerRootIndices = new HashSet<int>(Enumerable.Range(0, bones.Length).Where(index =>
                bones[index] != null && lowerSurfaceNames.Contains(bones[index].name)));
            if (upperRootIndices.Count != UpperMouthSurfaceBoneNames.Length ||
                lowerRootIndices.Count != LowerMouthSurfaceBoneNames.Length)
            {
                throw new InvalidOperationException("Parvum visible upper/lower mouth surface rigs are incomplete.");
            }

            var toothTransforms = new HashSet<Transform>();
            foreach (var rootName in ToothBranchRootBoneNames)
            {
                var toothRoot = bones.FirstOrDefault(bone =>
                    bone != null && string.Equals(bone.name, rootName, StringComparison.Ordinal)) ??
                                throw new InvalidOperationException("Parvum tooth branch is missing: " + rootName + ".");
                foreach (var item in toothRoot.GetComponentsInChildren<Transform>(true))
                {
                    toothTransforms.Add(item);
                }
            }

            var toothIndices = new HashSet<int>(Enumerable.Range(0, bones.Length)
                .Where(index => bones[index] != null && toothTransforms.Contains(bones[index])));
            var vertices = mesh.vertices;
            var upperRootWeights = new float[vertices.Length];
            var lowerRootWeights = new float[vertices.Length];
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
                        if (upperRootIndices.Contains(influence.boneIndex))
                        {
                            upperRootWeights[vertexIndex] += influence.weight;
                        }
                        if (lowerRootIndices.Contains(influence.boneIndex))
                        {
                            lowerRootWeights[vertexIndex] += influence.weight;
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

            var innerUpperWeights = new float[vertices.Length];
            var innerLowerWeights = new float[vertices.Length];
            var outerUpperWeights = new float[vertices.Length];
            var outerLowerWeights = new float[vertices.Length];
            var upperFrontRigidWeights = new float[vertices.Length];
            var lowerFrontRigidWeights = new float[vertices.Length];
            var upperSnoutWeights = new float[vertices.Length];
            var lowerSnoutWeights = new float[vertices.Length];
            var frontUpperWeights = new float[vertices.Length];
            var frontLowerWeights = new float[vertices.Length];
            var upperWeights = new float[vertices.Length];
            var lowerWeights = new float[vertices.Length];
            for (var index = 0; index < vertices.Length; index++)
            {
                var vertex = vertices[index];
                var exclusion = 1f - Mathf.SmoothStep(0.15f, 0.65f, toothWeights[index]);
                innerUpperWeights[index] = upperRootWeights[index] * exclusion *
                                           BandWeight(vertex.x, -0.42f, -0.34f, 0.34f, 0.42f) *
                                           BandWeight(vertex.y, 0.78f, 0.84f, 1.10f, 1.17f) *
                                           BandWeight(vertex.z, 0.68f, 0.76f, 1.12f, 1.22f);
                innerLowerWeights[index] = Mathf.Clamp01(lowerRootWeights[index]) * exclusion *
                                           BandWeight(vertex.x, -0.45f, -0.36f, 0.36f, 0.45f) *
                                           BandWeight(vertex.y, 0.52f, 0.60f, 0.88f, 0.94f) *
                                           BandWeight(vertex.z, 0.56f, 0.65f, 1.08f, 1.18f);

                // The user's Image #2 includes the broad exterior muzzle, not only the central teeth.
                // Include the full front-facing Bone_002..006 surface, then taper it separately from the rigid teeth.
                var upperFrontSurface = upperRootWeights[index] > 0.02f && exclusion > 0.05f && vertex.y >= 0.76f
                    ? BandWeight(vertex.x, -0.60f, -0.52f, 0.52f, 0.60f) *
                      BandWeight(vertex.y, 0.66f, 0.74f, 1.24f, 1.32f) *
                      BandWeight(vertex.z, 0.52f, 0.64f, 1.34f, 1.42f)
                    : 0f;
                var rigidUpperTeeth = toothWeights[index] > 0.01f ? 1f : 0f;
                upperSnoutWeights[index] = upperFrontSurface;
                var upperFrontRigid = rigidUpperTeeth;
                // Include the lower exterior jaw through Bone_011..018; only its central tooth-bearing tip stays rigid.
                var lowerFrontSurface = lowerRootWeights[index] > 0.02f && exclusion > 0.05f && vertex.y < 0.76f
                    ? BandWeight(vertex.x, -0.60f, -0.52f, 0.52f, 0.60f) *
                      BandWeight(vertex.y, 0.30f, 0.38f, 0.86f, 0.94f) *
                      BandWeight(vertex.z, 0.48f, 0.60f, 1.34f, 1.42f)
                    : 0f;
                lowerSnoutWeights[index] = lowerFrontSurface;
                var lowerFrontRigid = lowerFrontSurface *
                                      BandWeight(vertex.x, -0.38f, -0.30f, 0.30f, 0.38f) *
                                      BandWeight(vertex.y, 0.46f, 0.52f, 0.72f, 0.78f) *
                                      BandWeight(vertex.z, 1.12f, 1.20f, 1.34f, 1.42f);
                upperFrontRigidWeights[index] = upperFrontRigid;
                lowerFrontRigidWeights[index] = lowerFrontRigid;
                if (vertex.z >= FrontMouthMinimumZ)
                {
                    frontUpperWeights[index] = upperFrontRigid >= 0.999f && vertex.y >= 0.76f ? 1f : 0f;
                    frontLowerWeights[index] = lowerFrontRigid >= 0.999f && vertex.y < 0.76f ? 1f : 0f;
                }

                // The visible upper lip spans the Bone_006-to-Bone_002 muzzle surface chain.
                var upperFlesh = Mathf.Clamp01(upperRootWeights[index]) * exclusion *
                                 BandWeight(vertex.x, -0.55f, -0.47f, 0.47f, 0.55f) *
                                 BandWeight(vertex.y, 0.70f, 0.76f, 1.22f, 1.30f) *
                                 BandWeight(vertex.z, 0.52f, 0.64f, 1.26f, 1.34f);
                var fullUpperMouth = Mathf.Max(upperFlesh, Mathf.Max(upperFrontSurface, upperFrontRigid));
                outerUpperWeights[index] = Mathf.Max(0f, fullUpperMouth - innerUpperWeights[index]);

                // The visible lower lip reaches from Bone_018 at the root through Bone_011 at the front.
                var lowerFlesh = Mathf.Clamp01(lowerRootWeights[index]) * exclusion *
                                 BandWeight(vertex.x, -0.55f, -0.47f, 0.47f, 0.55f) *
                                 BandWeight(vertex.y, 0.38f, 0.46f, 0.94f, 1.00f) *
                                 BandWeight(vertex.z, 0.48f, 0.60f, 1.26f, 1.34f);
                var fullLowerMouth = Mathf.Max(lowerFlesh, Mathf.Max(lowerFrontSurface, lowerFrontRigid));
                outerLowerWeights[index] = Mathf.Max(0f, fullLowerMouth - innerLowerWeights[index]);
                upperWeights[index] = Mathf.Max(innerUpperWeights[index], fullUpperMouth);
                lowerWeights[index] = Mathf.Max(innerLowerWeights[index], fullLowerMouth);
            }

            var upperCenter = WeightedCenter(vertices, upperWeights);
            var lowerCenter = WeightedCenter(vertices, lowerWeights);
            var gap = upperCenter.y - lowerCenter.y;
            if (gap <= GeometryTolerance)
            {
                throw new InvalidOperationException("Parvum upper/lower mouth-root gap is invalid.");
            }

            var response = MouthRootGapResponse(upperWeights, lowerWeights);
            var openTravel = gap * MouthRootOpenRatio / response;
            var biteTravel = gap * MouthRootBiteRatio / response;
            var puckerTravel = gap * LipPuckerForwardGapRatio / (response * 0.5f);
            if (frontUpperWeights.All(weight => weight <= GeometryTolerance) ||
                frontLowerWeights.All(weight => weight <= GeometryTolerance))
            {
                throw new InvalidOperationException("Parvum Image #2 foremost upper/lower mouth groups are empty.");
            }
            var frontGap = WeightedCenter(vertices, frontUpperWeights).y -
                           WeightedCenter(vertices, frontLowerWeights).y;
            if (frontGap <= GeometryTolerance)
            {
                throw new InvalidOperationException("Parvum Image #2 foremost mouth aperture is invalid.");
            }
            var frontOpenTravel = frontGap * FrontMouthOpenRatio;
            var frontTargetAperture = Mathf.Max(GeometryTolerance * 10f, frontGap * FrontMouthBiteApertureRatio);
            var frontClosureTravel = frontGap - frontTargetAperture;
            var frontUpperBiteTravel = frontClosureTravel * FrontUpperClosureShare;
            var frontLowerBiteTravel = frontClosureTravel * (1f - FrontUpperClosureShare);
            var frontPuckerTravel = frontGap * LipPuckerForwardGapRatio;
            var openDeltas = new Vector3[vertices.Length];
            var biteDeltas = new Vector3[vertices.Length];
            var rigidUpperFrontVertices = new bool[vertices.Length];
            var rigidLowerFrontVertices = new bool[vertices.Length];
            var openTargets = new Vector3[vertices.Length];
            var biteTargets = new Vector3[vertices.Length];
            for (var index = 0; index < vertices.Length; index++)
            {
                var outerOpenUpper = outerUpperWeights[index] * OuterLipOpenBoost;
                var outerOpenLower = outerLowerWeights[index] * OuterLipOpenBoost;
                var outerBiteUpper = outerUpperWeights[index] * OuterUpperBiteBoost;
                var outerBiteLower = outerLowerWeights[index] * OuterLowerBiteBoost;
                var outerPucker = Mathf.Max(outerUpperWeights[index], outerLowerWeights[index]) * OuterLipPuckerBoost;
                openDeltas[index] = Vector3.up * openTravel * (upperWeights[index] + outerOpenUpper) +
                                    Vector3.down * openTravel * (lowerWeights[index] + outerOpenLower);
                biteDeltas[index] = Vector3.down * biteTravel * (upperWeights[index] + outerBiteUpper) +
                                    Vector3.up * biteTravel * (lowerWeights[index] + outerBiteLower) +
                                    Vector3.forward * puckerTravel *
                                    (Mathf.Max(upperWeights[index], lowerWeights[index]) + outerPucker);
                var snoutFrontness = Mathf.Clamp01(Mathf.InverseLerp(0.52f, 1.34f, vertices[index].z));
                var snoutTravelScale = Mathf.Lerp(0.51f, 1.18f, snoutFrontness);
                if (upperSnoutWeights[index] > GeometryTolerance)
                {
                    var upperSnoutOpen = Vector3.up * frontOpenTravel * snoutTravelScale;
                    var upperSnoutBite = Vector3.down * frontUpperBiteTravel * snoutTravelScale +
                                         Vector3.forward * frontPuckerTravel * snoutTravelScale;
                    openDeltas[index] = Vector3.Lerp(openDeltas[index], upperSnoutOpen, upperSnoutWeights[index]);
                    biteDeltas[index] = Vector3.Lerp(biteDeltas[index], upperSnoutBite, upperSnoutWeights[index]);
                }
                if (lowerSnoutWeights[index] > GeometryTolerance)
                {
                    var lowerSnoutOpen = Vector3.down * frontOpenTravel * snoutTravelScale;
                    var lowerSnoutBite = Vector3.up * frontLowerBiteTravel * snoutTravelScale +
                                         Vector3.forward * frontPuckerTravel * snoutTravelScale;
                    openDeltas[index] = Vector3.Lerp(openDeltas[index], lowerSnoutOpen, lowerSnoutWeights[index]);
                    biteDeltas[index] = Vector3.Lerp(biteDeltas[index], lowerSnoutBite, lowerSnoutWeights[index]);
                }
                rigidUpperFrontVertices[index] = upperFrontRigidWeights[index] >= 0.999f;
                rigidLowerFrontVertices[index] = lowerFrontRigidWeights[index] >= 0.999f;
                if (rigidUpperFrontVertices[index] && rigidLowerFrontVertices[index])
                {
                    rigidUpperFrontVertices[index] = vertices[index].y >= 0.76f;
                    rigidLowerFrontVertices[index] = !rigidUpperFrontVertices[index];
                }
                if (rigidUpperFrontVertices[index])
                {
                    openDeltas[index] = Vector3.up * frontOpenTravel * FrontRigidMotionBoost;
                    biteDeltas[index] = Vector3.down * frontUpperBiteTravel * FrontRigidMotionBoost +
                                        Vector3.forward * frontPuckerTravel * FrontRigidMotionBoost;
                }
                else if (rigidLowerFrontVertices[index])
                {
                    openDeltas[index] = Vector3.down * frontOpenTravel * FrontRigidMotionBoost;
                    biteDeltas[index] = Vector3.up * frontLowerBiteTravel * FrontRigidMotionBoost +
                                        Vector3.forward * frontPuckerTravel * FrontRigidMotionBoost;
                }
                openTargets[index] = vertices[index] + openDeltas[index];
                biteTargets[index] = vertices[index] + biteDeltas[index];
            }

            var openGap = WeightedCenter(openTargets, upperWeights).y - WeightedCenter(openTargets, lowerWeights).y;
            var biteGap = WeightedCenter(biteTargets, upperWeights).y - WeightedCenter(biteTargets, lowerWeights).y;
            var restForward = (WeightedCenter(vertices, upperWeights).z + WeightedCenter(vertices, lowerWeights).z) * 0.5f;
            var biteForward = (WeightedCenter(biteTargets, upperWeights).z + WeightedCenter(biteTargets, lowerWeights).z) * 0.5f;
            analysis = new MouthRootAnalysis(
                upperWeights.Count(weight => weight > GeometryTolerance),
                lowerWeights.Count(weight => weight > GeometryTolerance),
                outerUpperWeights.Count(weight => weight > GeometryTolerance),
                outerLowerWeights.Count(weight => weight > GeometryTolerance),
                (openGap / gap - 1f) * 100f,
                (1f - biteGap / gap) * 100f,
                biteForward - restForward);
            return new MouthRootDeltas(
                openDeltas,
                biteDeltas,
                rigidUpperFrontVertices,
                rigidLowerFrontVertices);
        }

        private static MouthRigInfluenceData BuildMouthRigInfluenceData(
            SkinnedMeshRenderer renderer,
            Mesh mesh)
        {
            var bones = renderer.bones;
            var upperSurfaceNames = new HashSet<string>(UpperMouthSurfaceBoneNames, StringComparer.Ordinal);
            var lowerSurfaceNames = new HashSet<string>(LowerMouthSurfaceBoneNames, StringComparer.Ordinal);
            var upperRootIndices = new HashSet<int>(Enumerable.Range(0, bones.Length).Where(index =>
                bones[index] != null && upperSurfaceNames.Contains(bones[index].name)));
            var lowerRootIndices = new HashSet<int>(Enumerable.Range(0, bones.Length).Where(index =>
                bones[index] != null && lowerSurfaceNames.Contains(bones[index].name)));
            if (upperRootIndices.Count != UpperMouthSurfaceBoneNames.Length ||
                lowerRootIndices.Count != LowerMouthSurfaceBoneNames.Length)
            {
                throw new InvalidOperationException("Parvum visible upper/lower mouth surface rigs are incomplete.");
            }

            var toothTransforms = new HashSet<Transform>();
            foreach (var rootName in ToothBranchRootBoneNames)
            {
                var toothRoot = bones.FirstOrDefault(bone =>
                    bone != null && string.Equals(bone.name, rootName, StringComparison.Ordinal)) ??
                                throw new InvalidOperationException("Parvum tooth branch is missing: " + rootName + ".");
                foreach (var item in toothRoot.GetComponentsInChildren<Transform>(true))
                {
                    toothTransforms.Add(item);
                }
            }

            var toothIndices = new HashSet<int>(Enumerable.Range(0, bones.Length)
                .Where(index => bones[index] != null && toothTransforms.Contains(bones[index])));
            var upperRootWeights = new float[mesh.vertexCount];
            var lowerRootWeights = new float[mesh.vertexCount];
            var toothWeights = new float[mesh.vertexCount];
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
                        if (upperRootIndices.Contains(influence.boneIndex))
                        {
                            upperRootWeights[vertexIndex] += influence.weight;
                        }
                        if (lowerRootIndices.Contains(influence.boneIndex))
                        {
                            lowerRootWeights[vertexIndex] += influence.weight;
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

            var exclusionWeights = new float[mesh.vertexCount];
            for (var index = 0; index < mesh.vertexCount; index++)
            {
                exclusionWeights[index] = 1f - Mathf.SmoothStep(0.15f, 0.65f, toothWeights[index]);
            }
            return new MouthRigInfluenceData(upperRootWeights, lowerRootWeights, exclusionWeights);
        }

        private static void AppendVertexGroup(
            StringBuilder report,
            string label,
            IReadOnlyList<Vector3> vertices,
            IReadOnlyList<float> primaryWeights,
            IReadOnlyList<float> exclusionWeights)
        {
            var selected = new List<Vector3>();
            for (var index = 0; index < vertices.Count; index++)
            {
                var exclusion = exclusionWeights == null ? 1f : exclusionWeights[index];
                if (primaryWeights[index] * exclusion > GeometryTolerance)
                {
                    selected.Add(vertices[index]);
                }
            }
            report.AppendLine(label + "VertexCount=" + selected.Count.ToString(CultureInfo.InvariantCulture));
            if (selected.Count == 0)
            {
                return;
            }
            var bounds = BoundsFromVertices(selected);
            report.AppendLine(label + "BoundsMin=" + Vec(bounds.min));
            report.AppendLine(label + "BoundsMax=" + Vec(bounds.max));
            report.AppendLine(label + "BoundsCenter=" + Vec(bounds.center));
            report.AppendLine(label + "BoundsSize=" + Vec(bounds.size));
        }

        private static void AppendAxisHistogram(
            StringBuilder report,
            string label,
            IReadOnlyList<Vector3> vertices,
            IReadOnlyList<float> primaryWeights,
            IReadOnlyList<float> exclusionWeights,
            int axis,
            float minimum,
            float maximum,
            float step)
        {
            for (var lower = minimum; lower < maximum - GeometryTolerance; lower += step)
            {
                var upper = Mathf.Min(lower + step, maximum);
                var count = 0;
                for (var index = 0; index < vertices.Count; index++)
                {
                    var exclusion = exclusionWeights == null ? 1f : exclusionWeights[index];
                    var value = Vector3Component(vertices[index], axis);
                    if (primaryWeights[index] * exclusion > GeometryTolerance && value >= lower && value < upper)
                    {
                        count++;
                    }
                }
                report.AppendLine(
                    label + "[" + Num(lower) + "," + Num(upper) + ")=" +
                    count.ToString(CultureInfo.InvariantCulture));
            }
        }

        private static void AppendSubMeshReport(
            StringBuilder report,
            SkinnedMeshRenderer renderer,
            Mesh mesh,
            IReadOnlyList<Vector3> vertices)
        {
            var materials = renderer.sharedMaterials;
            report.AppendLine("SubMeshCount=" + mesh.subMeshCount.ToString(CultureInfo.InvariantCulture));
            for (var subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
            {
                var indices = mesh.GetIndices(subMesh);
                var uniqueIndices = new HashSet<int>(indices);
                var selected = uniqueIndices.Select(index => vertices[index]).ToArray();
                var materialName = subMesh < materials.Length && materials[subMesh] != null
                    ? materials[subMesh].name
                    : "<missing>";
                report.AppendLine(
                    "SubMesh[" + subMesh.ToString(CultureInfo.InvariantCulture) + "]Material=" + materialName);
                report.AppendLine(
                    "SubMesh[" + subMesh.ToString(CultureInfo.InvariantCulture) + "]VertexCount=" +
                    selected.Length.ToString(CultureInfo.InvariantCulture));
                report.AppendLine(
                    "SubMesh[" + subMesh.ToString(CultureInfo.InvariantCulture) + "]IndexCount=" +
                    indices.Length.ToString(CultureInfo.InvariantCulture));
                if (selected.Length == 0)
                {
                    continue;
                }

                var bounds = BoundsFromVertices(selected);
                report.AppendLine(
                    "SubMesh[" + subMesh.ToString(CultureInfo.InvariantCulture) + "]BoundsMin=" + Vec(bounds.min));
                report.AppendLine(
                    "SubMesh[" + subMesh.ToString(CultureInfo.InvariantCulture) + "]BoundsMax=" + Vec(bounds.max));
            }
        }

        private static void AppendConnectedComponentReport(
            StringBuilder report,
            SkinnedMeshRenderer renderer,
            Mesh mesh,
            IReadOnlyList<Vector3> vertices)
        {
            var parent = Enumerable.Range(0, vertices.Count).ToArray();
            int Find(int value)
            {
                while (parent[value] != value)
                {
                    parent[value] = parent[parent[value]];
                    value = parent[value];
                }
                return value;
            }
            void Union(int first, int second)
            {
                var firstRoot = Find(first);
                var secondRoot = Find(second);
                if (firstRoot != secondRoot)
                {
                    parent[secondRoot] = firstRoot;
                }
            }

            for (var subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
            {
                var indices = mesh.GetIndices(subMesh);
                for (var index = 0; index + 2 < indices.Length; index += 3)
                {
                    Union(indices[index], indices[index + 1]);
                    Union(indices[index + 1], indices[index + 2]);
                }
            }

            var components = Enumerable.Range(0, vertices.Count)
                .GroupBy(Find)
                .Select(group => group.ToArray())
                .OrderByDescending(group => group.Length)
                .ToArray();
            var boneWeights = mesh.boneWeights;
            var bones = renderer.bones;
            report.AppendLine("ConnectedComponentCount=" + components.Length.ToString(CultureInfo.InvariantCulture));
            for (var componentIndex = 0; componentIndex < components.Length; componentIndex++)
            {
                var component = components[componentIndex];
                var bounds = BoundsFromVertices(component.Select(index => vertices[index]).ToArray());
                var weightTotals = new Dictionary<int, float>();
                foreach (var vertexIndex in component)
                {
                    var weight = boneWeights[vertexIndex];
                    AddWeight(weightTotals, weight.boneIndex0, weight.weight0);
                    AddWeight(weightTotals, weight.boneIndex1, weight.weight1);
                    AddWeight(weightTotals, weight.boneIndex2, weight.weight2);
                    AddWeight(weightTotals, weight.boneIndex3, weight.weight3);
                }
                var dominantBones = string.Join(",", weightTotals
                    .Where(pair => pair.Value > GeometryTolerance && pair.Key >= 0 && pair.Key < bones.Length)
                    .OrderByDescending(pair => pair.Value)
                    .Take(6)
                    .Select(pair => (bones[pair.Key] != null ? bones[pair.Key].name : "<missing>") + ":" + Num(pair.Value)));
                report.AppendLine(
                    "Component[" + componentIndex.ToString(CultureInfo.InvariantCulture) + "]VertexCount=" +
                    component.Length.ToString(CultureInfo.InvariantCulture));
                report.AppendLine(
                    "Component[" + componentIndex.ToString(CultureInfo.InvariantCulture) + "]BoundsMin=" + Vec(bounds.min));
                report.AppendLine(
                    "Component[" + componentIndex.ToString(CultureInfo.InvariantCulture) + "]BoundsMax=" + Vec(bounds.max));
                report.AppendLine(
                    "Component[" + componentIndex.ToString(CultureInfo.InvariantCulture) + "]DominantBones=" + dominantBones);
            }
        }

        private static void AddWeight(IDictionary<int, float> totals, int boneIndex, float weight)
        {
            if (weight <= GeometryTolerance)
            {
                return;
            }
            totals[boneIndex] = totals.TryGetValue(boneIndex, out var current) ? current + weight : weight;
        }

        private static void AppendBoneHierarchyReport(StringBuilder report, SkinnedMeshRenderer renderer)
        {
            report.AppendLine("BoneCount=" + renderer.bones.Length.ToString(CultureInfo.InvariantCulture));
            for (var index = 0; index < renderer.bones.Length; index++)
            {
                var bone = renderer.bones[index];
                report.AppendLine(
                    "Bone[" + index.ToString(CultureInfo.InvariantCulture) + "]=" +
                    (bone != null ? bone.name : "<missing>") +
                    ",Parent=" + (bone != null && bone.parent != null ? bone.parent.name : "<none>"));
            }
        }

        private static void AppendBoneBranchInfluenceReport(
            StringBuilder report,
            SkinnedMeshRenderer renderer,
            Mesh mesh,
            IReadOnlyList<Vector3> vertices,
            IEnumerable<string> branchRootNames)
        {
            var bones = renderer.bones;
            var bonesPerVertex = mesh.GetBonesPerVertex();
            var allWeights = mesh.GetAllBoneWeights();
            try
            {
                foreach (var branchRootName in branchRootNames)
                {
                    var branchRoot = bones.FirstOrDefault(bone =>
                        bone != null && string.Equals(bone.name, branchRootName, StringComparison.Ordinal)) ??
                                     throw new InvalidOperationException("Parvum mouth branch is missing: " + branchRootName + ".");
                    var branchTransforms = new HashSet<Transform>(branchRoot.GetComponentsInChildren<Transform>(true));
                    var branchIndices = new HashSet<int>(Enumerable.Range(0, bones.Length)
                        .Where(index => bones[index] != null && branchTransforms.Contains(bones[index])));
                    var branchWeights = new float[mesh.vertexCount];
                    var weightIndex = 0;
                    for (var vertexIndex = 0; vertexIndex < mesh.vertexCount; vertexIndex++)
                    {
                        var influenceCount = bonesPerVertex[vertexIndex];
                        for (var influenceIndex = 0; influenceIndex < influenceCount; influenceIndex++)
                        {
                            var influence = allWeights[weightIndex++];
                            if (branchIndices.Contains(influence.boneIndex))
                            {
                                branchWeights[vertexIndex] += influence.weight;
                            }
                        }
                    }

                    AppendVertexGroup(report, "Branch_" + branchRootName, vertices, branchWeights, null);
                    report.AppendLine(
                        "Branch_" + branchRootName + "WeightedCenter=" + Vec(WeightedCenter(vertices, branchWeights)));
                }
            }
            finally
            {
                bonesPerVertex.Dispose();
                allWeights.Dispose();
            }
        }

        private static void RequireLipBlendShapeMotion(
            Transform model,
            SkinnedMeshRenderer renderer,
            Animator animator)
        {
            var lowerJaw = FindChildRecursive(model, LowerJawRootBoneName) ??
                           throw new InvalidOperationException("Parvum lower-jaw rig root is missing.");
            var innerMouth = FindChildRecursive(model, InnerMouthRootBoneName) ??
                              throw new InvalidOperationException("Parvum inner-mouth rig root is missing.");
            var groups = BuildMouthSkinGroups(renderer, lowerJaw, innerMouth);
            var animatorEnabled = animator.enabled;
            var originalWeights = Enumerable.Range(0, renderer.sharedMesh.blendShapeCount)
                .Select(renderer.GetBlendShapeWeight).ToArray();
            try
            {
                animator.enabled = false;
                for (var index = 0; index < renderer.sharedMesh.blendShapeCount; index++)
                {
                    renderer.SetBlendShapeWeight(index, 0f);
                }

                var openRootIndex = renderer.sharedMesh.GetBlendShapeIndex(OpenRootBlendShapeName);
                var biteRootIndex = renderer.sharedMesh.GetBlendShapeIndex(BiteRootBlendShapeName);
                if (openRootIndex < 0 || biteRootIndex < 0)
                {
                    throw new InvalidOperationException("Parvum lip-only attack BlendShapes are missing.");
                }

                var restAperture = MeasureMouthAperture(model, renderer, groups);
                renderer.SetBlendShapeWeight(openRootIndex, 100f);
                var openAperture = MeasureMouthAperture(model, renderer, groups);
                renderer.SetBlendShapeWeight(openRootIndex, 0f);
                renderer.SetBlendShapeWeight(biteRootIndex, 100f);
                var closedAperture = MeasureMouthAperture(model, renderer, groups);
                var wideOpenPercent = (openAperture / restAperture - 1f) * 100f;
                var closurePercent = (1f - closedAperture / openAperture) * 100f;
                if (wideOpenPercent < 10f || closurePercent < 60f)
                {
                    throw new InvalidOperationException(
                        "Parvum mouth rig cannot produce a clearly wide-open and closing bite. WideOpen=" +
                        Num(wideOpenPercent) + ", Closure=" + Num(closurePercent) + ".");
                }

            }
            finally
            {
                for (var index = 0; index < originalWeights.Length; index++)
                {
                    renderer.SetBlendShapeWeight(index, originalWeights[index]);
                }
                animator.enabled = animatorEnabled;
            }
        }

        private static AnimationClip EnsureClip(
            Transform attackSlot,
            SkinnedMeshRenderer renderer)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);
            if (clip == null)
            {
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, ClipPath);
            }

            clip.ClearCurves();
            clip.name = "Parvum_Attack_NewModel";
            clip.frameRate = 60f;
            clip.wrapMode = WrapMode.Loop;
            var rendererPath = AnimationUtility.CalculateTransformPath(renderer.transform, attackSlot);
            SetBlendShapeCurve(clip, rendererPath, OpenRootBlendShapeName,
                new Keyframe(0f, 0f),
                new Keyframe(WideOpenTime, 100f),
                new Keyframe(BiteSnapStartTime, 100f),
                new Keyframe(BiteImpactTime, 0f),
                new Keyframe(ImpactFollowThroughTime, 0f),
                new Keyframe(RecoveryTime, 0f));
            SetBlendShapeCurve(clip, rendererPath, BiteRootBlendShapeName,
                new Keyframe(0f, 0f),
                new Keyframe(BiteSnapStartTime, 0f),
                new Keyframe(BiteImpactTime, 100f),
                new Keyframe(ImpactFollowThroughTime, 100f),
                new Keyframe(RecoveryTime, 0f));
            SetBlendShapeCurve(clip, rendererPath, BodyImpactBlendShapeName,
                new Keyframe(0f, 0f),
                new Keyframe(WideOpenTime, 0f),
                new Keyframe(ForwardLeanTime, 0f),
                new Keyframe(BiteSnapStartTime, 20f),
                new Keyframe(BiteImpactTime, 100f),
                new Keyframe(ImpactFollowThroughTime, 82f),
                new Keyframe(RecoveryTime, 0f));

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

        private static void SetBlendShapeCurve(
            AnimationClip clip,
            string rendererPath,
            string blendShape,
            params Keyframe[] keys)
        {
            var curve = SmoothCurve(keys);
            AnimationUtility.SetEditorCurve(
                clip,
                EditorCurveBinding.FloatCurve(
                    rendererPath,
                    typeof(SkinnedMeshRenderer),
                    "blendShape." + blendShape),
                curve);
        }

        private static void SetQuaternionCurves(
            AnimationClip clip,
            string path,
            IReadOnlyList<float> times,
            IReadOnlyList<Quaternion> rotations)
        {
            var components = new[] { "x", "y", "z", "w" };
            for (var component = 0; component < components.Length; component++)
            {
                var keys = new Keyframe[times.Count];
                for (var index = 0; index < times.Count; index++)
                {
                    keys[index] = new Keyframe(times[index], QuaternionComponent(rotations[index], component));
                }
                AnimationUtility.SetEditorCurve(
                    clip,
                    EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalRotation." + components[component]),
                    SmoothCurve(keys));
            }
            clip.EnsureQuaternionContinuity();
        }

        private static void SetVector3Curves(
            AnimationClip clip,
            string path,
            IReadOnlyList<float> times,
            IReadOnlyList<Vector3> positions)
        {
            var components = new[] { "x", "y", "z" };
            for (var component = 0; component < components.Length; component++)
            {
                var keys = new Keyframe[times.Count];
                for (var index = 0; index < times.Count; index++)
                {
                    keys[index] = new Keyframe(times[index], Vector3Component(positions[index], component));
                }
                AnimationUtility.SetEditorCurve(
                    clip,
                    EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalPosition." + components[component]),
                    SmoothCurve(keys));
            }
        }

        private static AnimationCurve SmoothCurve(params Keyframe[] keys)
        {
            var curve = new AnimationCurve(keys);
            for (var index = 0; index < curve.length; index++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, index, AnimationUtility.TangentMode.ClampedAuto);
                AnimationUtility.SetKeyRightTangentMode(curve, index, AnimationUtility.TangentMode.ClampedAuto);
            }
            return curve;
        }

        private static AnimatorController EnsureController(AnimationClip clip)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            }

            var stateMachine = controller.layers[0].stateMachine;
            var state = stateMachine.states.Select(child => child.state)
                            .FirstOrDefault(candidate => candidate.name == "Parvum_Attack_NewModel") ??
                        stateMachine.AddState("Parvum_Attack_NewModel");
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
            Transform attackSlot,
            Transform model,
            SkinnedMeshRenderer renderer,
            Animator animator,
            AnimationClip clip,
            AnimatorController controller,
            Transform motionTarget)
        {
            if (EditorUtility.scriptCompilationFailed)
            {
                throw new InvalidOperationException("Unity reports script compilation errors.");
            }
            if (!string.Equals(AssetDatabase.GetAssetPath(renderer.sharedMesh), GeneratedMeshPath, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Parvum attack renderer is not using the new attack mesh.");
            }
            if (!string.Equals(AssetDatabase.GetAssetPath(animator.runtimeAnimatorController), ControllerPath, StringComparison.Ordinal) ||
                animator.runtimeAnimatorController != controller || animator.applyRootMotion)
            {
                throw new InvalidOperationException("Parvum attack Animator is not exclusively using the new controller.");
            }
            if (string.Equals(AssetDatabase.GetAssetPath(animator.runtimeAnimatorController), OldAttackControllerPath, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("The old Parvum attack controller is still assigned.");
            }
            if (controller.animationClips.Length != 1 || controller.animationClips[0] != clip ||
                string.Equals(AssetDatabase.GetAssetPath(controller.animationClips[0]), OldAttackClipPath, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("The new controller must contain only the new-model attack clip.");
            }
            if (Mathf.Abs(clip.length - CycleSeconds) > 0.0001f || !clip.isLooping)
            {
                throw new InvalidOperationException("Parvum attack clip must be an exact 3-second loop.");
            }

            var mesh = renderer.sharedMesh;
            if (mesh.blendShapeCount != 3 ||
                mesh.GetBlendShapeIndex(OpenRootBlendShapeName) != 0 ||
                mesh.GetBlendShapeIndex(BiteRootBlendShapeName) != 1 ||
                mesh.GetBlendShapeIndex(BodyImpactBlendShapeName) != 2)
            {
                throw new InvalidOperationException(
                    "Parvum attack mesh must contain the two mouth-root BlendShapes and the front-body impact BlendShape.");
            }

            var rendererPath = AnimationUtility.CalculateTransformPath(renderer.transform, attackSlot);
            var openCurve = RequireCurve(clip, rendererPath, typeof(SkinnedMeshRenderer), "blendShape." + OpenRootBlendShapeName);
            var biteCurve = RequireCurve(clip, rendererPath, typeof(SkinnedMeshRenderer), "blendShape." + BiteRootBlendShapeName);
            var bodyImpactCurve = RequireCurve(
                clip,
                rendererPath,
                typeof(SkinnedMeshRenderer),
                "blendShape." + BodyImpactBlendShapeName);
            RequireCurveValue(openCurve, WideOpenTime, 100f, "wide-open mouth roots");
            RequireCurveValue(openCurve, BiteSnapStartTime, 100f, "held-open mouth roots before impact");
            RequireCurveValue(openCurve, BiteImpactTime, 0f, "impact open roots");
            RequireCurveValue(biteCurve, BiteSnapStartTime, 0f, "pre-impact bite mouth roots");
            RequireCurveValue(biteCurve, BiteImpactTime, 100f, "bite mouth roots");
            RequireCurveValue(biteCurve, ImpactFollowThroughTime, 100f, "bite follow-through mouth roots");
            RequireCurveValue(biteCurve, RecoveryTime, 0f, "recovered mouth roots");
            RequireCurveValue(bodyImpactCurve, WideOpenTime, 0f, "wide-open body impact");
            RequireCurveValue(bodyImpactCurve, BiteImpactTime, 100f, "impact body expansion");
            RequireCurveValue(bodyImpactCurve, RecoveryTime, 0f, "recovered body expansion");
            var upperMouthRoot = FindChildRecursive(model, UpperMouthRootBoneName) ??
                                 throw new InvalidOperationException("Parvum upper-mouth rig root is missing during inspection.");
            var lowerJaw = FindChildRecursive(model, LowerJawRootBoneName) ??
                           throw new InvalidOperationException("Parvum lower-jaw rig root is missing during inspection.");
            var innerMouth = FindChildRecursive(model, InnerMouthRootBoneName) ??
                             throw new InvalidOperationException("Parvum inner-mouth rig root is missing during inspection.");
            var fixedMouthPaths = new HashSet<string>(StringComparer.Ordinal)
            {
                AnimationUtility.CalculateTransformPath(upperMouthRoot, attackSlot),
                AnimationUtility.CalculateTransformPath(lowerJaw, attackSlot),
                AnimationUtility.CalculateTransformPath(innerMouth, attackSlot)
            };
            if (AnimationUtility.GetCurveBindings(clip).Any(binding =>
                    binding.type == typeof(Transform) && fixedMouthPaths.Contains(binding.path)))
            {
                throw new InvalidOperationException("Parvum lip-only attack must not rotate or translate the mouth rig bones.");
            }
            if (AnimationUtility.GetCurveBindings(clip).Any(binding =>
                    binding.type == typeof(Transform)))
            {
                throw new InvalidOperationException(
                    "Parvum attack must use only mouth and broad front-body BlendShapes, without Transform curves.");
            }

            RequireReviewPhysics(attackSlot, motionTarget);
            RequireOnlyAttackConfigured(parvumRoot, attackSlot, animator);
            var sourceRenderer = RequireSourceRenderer();
            if (sourceRenderer.sharedMesh.vertexCount != mesh.vertexCount ||
                sourceRenderer.sharedMesh.subMeshCount != mesh.subMeshCount)
            {
                throw new InvalidOperationException("Generated Parvum attack mesh changed source topology.");
            }

            var rootDeltas = BuildMouthRootDeltas(renderer, mesh, out var rootAnalysis);
            if (rootAnalysis.UpperVertexCount == 0 || rootAnalysis.LowerVertexCount == 0 ||
                rootAnalysis.OuterUpperVertexCount == 0 || rootAnalysis.OuterLowerVertexCount == 0 ||
                rootAnalysis.OpenPercent < MouthRootOpenRatio * 100f ||
                rootAnalysis.BiteClosurePercent < MouthRootBiteRatio * 100f ||
                rootAnalysis.PuckerForwardDistance <= 0.03f)
            {
                throw new InvalidOperationException("Parvum upper/lower mouth-root deformation is incomplete.");
            }
            if (rootDeltas.OpenDeltas.All(delta => delta.sqrMagnitude <= GeometryTolerance * GeometryTolerance) ||
                rootDeltas.BiteDeltas.All(delta => delta.sqrMagnitude <= GeometryTolerance * GeometryTolerance))
            {
                throw new InvalidOperationException("Parvum mouth-root attack deltas are empty.");
            }
            RequireRigidFrontMouthGroup(
                rootDeltas,
                rootDeltas.RigidUpperFrontVertices,
                "upper front mouth and teeth");
            RequireRigidFrontMouthGroup(
                rootDeltas,
                rootDeltas.RigidLowerFrontVertices,
                "lower front mouth and teeth");
            var bodyImpactDeltas = BuildBodyImpactDeltas(renderer, mesh, rootDeltas, out var bodyImpactAnalysis);
            if (bodyImpactAnalysis.AffectedVertexCount < mesh.vertexCount * 0.32f ||
                bodyImpactAnalysis.MaximumForwardDelta < 0.1f ||
                bodyImpactAnalysis.MaximumSideDelta <= GeometryTolerance ||
                bodyImpactAnalysis.MinimumAffectedHeightAboveGround <= GeometryTolerance ||
                bodyImpactDeltas.All(delta => delta.sqrMagnitude <= GeometryTolerance * GeometryTolerance))
            {
                throw new InvalidOperationException(
                    "Parvum grounded full-body impact expansion is incomplete. Vertices=" +
                    bodyImpactAnalysis.AffectedVertexCount.ToString(CultureInfo.InvariantCulture) +
                    ", Forward=" + Num(bodyImpactAnalysis.MaximumForwardDelta) +
                    ", Side=" + Num(bodyImpactAnalysis.MaximumSideDelta) +
                    ", GroundClearance=" + Num(bodyImpactAnalysis.MinimumAffectedHeightAboveGround) + ".");
            }

            var groups = BuildMouthSkinGroups(renderer, lowerJaw, innerMouth);
            var sample = MeasureSamples(attackSlot, model, renderer, animator, clip, groups);
            if (sample.WideOpenPercent < 55f ||
                sample.BiteAperture <= GeometryTolerance ||
                sample.BiteClosurePercent < 97f ||
                sample.BiteClosurePercent > 99f)
            {
                throw new InvalidOperationException(
                    "Parvum attack sampling did not produce a wide-open mouth followed by a non-crossing closed bite. " +
                    "Aperture=" + Num(sample.BiteAperture) +
                    ", Closure=" + Num(sample.BiteClosurePercent) +
                    ", UpperLift=" + Num(sample.UpperLipOpenLift) +
                    ", LowerDrop=" + Num(sample.LowerLipOpenDrop) +
                    ", UpperPucker=" + Num(sample.UpperLipBiteForward) +
                    ", LowerPucker=" + Num(sample.LowerLipBiteForward) + ".");
            }
            if (sample.UpperLipOpenLift <= 0.04f || sample.LowerLipOpenDrop <= 0.08f ||
                sample.UpperLipBiteForward <= 0.08f || sample.LowerLipBiteForward <= 0.13f)
            {
                throw new InvalidOperationException(
                    "Parvum upper/lower lips do not open vertically and pucker forward as required. UpperLift=" +
                    Num(sample.UpperLipOpenLift) + ", LowerDrop=" + Num(sample.LowerLipOpenDrop) +
                    ", UpperPucker=" + Num(sample.UpperLipBiteForward) +
                    ", LowerPucker=" + Num(sample.LowerLipBiteForward) + ".");
            }
            if (sample.OuterUpperLipBiteDown <= 0.09f || sample.OuterLowerLipBiteUp <= 0.03f)
            {
                throw new InvalidOperationException(
                    "Parvum outer mouth does not close at impact. OuterUpperDown=" +
                    Num(sample.OuterUpperLipBiteDown) + ", OuterLowerUp=" + Num(sample.OuterLowerLipBiteUp) + ".");
            }
            if (sample.FrontOpenAperture < sample.FrontRestAperture * 1.7f ||
                sample.FrontBiteAperture <= GeometryTolerance ||
                sample.FrontBiteClosurePercent < 98.5f ||
                sample.FrontBiteClosurePercent > 99.7f ||
                sample.FrontUpperBiteDown <= 0.08f ||
                sample.FrontLowerBiteUp <= 0.08f)
            {
                throw new InvalidOperationException(
                    "Parvum Image #2 foremost upper/lower mouth does not visibly snap shut. RestAperture=" +
                    Num(sample.FrontRestAperture) + ", OpenAperture=" + Num(sample.FrontOpenAperture) +
                    ", BiteAperture=" + Num(sample.FrontBiteAperture) +
                    ", Closure=" + Num(sample.FrontBiteClosurePercent) +
                    ", UpperDown=" + Num(sample.FrontUpperBiteDown) +
                    ", LowerUp=" + Num(sample.FrontLowerBiteUp) + ".");
            }
            if (sample.ForwardLeanDistance <= 0.30f ||
                sample.ImpactLungeDistance <= 0.25f ||
                sample.ModelVerticalTravel > GeometryTolerance ||
                sample.ModelLateralTravel > GeometryTolerance ||
                sample.ModelRotationTravel > GeometryTolerance ||
                sample.ModelForwardPositionTravel > GeometryTolerance)
            {
                throw new InvalidOperationException(
                    "Parvum broad front-body mesh impact or fixed-model Transform requirement is invalid. Advance=" +
                    Num(sample.ForwardLeanDistance) +
                    ", ImpactLunge=" + Num(sample.ImpactLungeDistance) +
                    ", ModelVerticalTravel=" + Num(sample.ModelVerticalTravel) +
                    ", ModelLateralTravel=" + Num(sample.ModelLateralTravel) +
                    ", ModelRotationTravel=" + Num(sample.ModelRotationTravel) +
                    ", ModelForwardPositionTravel=" + Num(sample.ModelForwardPositionTravel) + ".");
            }
            if (groups.InnerAffectedVertexCount == 0)
            {
                throw new InvalidOperationException("Parvum inner-mouth rig has no influenced vertices.");
            }

            return new InspectionResult(
                mesh.vertexCount,
                clip.length,
                sample.RestAperture,
                sample.OpenAperture,
                sample.BiteAperture,
                sample.WideOpenPercent,
                sample.BiteClosurePercent,
                sample.ForwardLeanDistance,
                sample.ImpactLungeDistance,
                sample.WorldGroundDelta,
                sample.ModelVerticalTravel,
                sample.ModelLateralTravel,
                sample.ModelRotationTravel,
                sample.ModelForwardPositionTravel,
                sample.UpperLipOpenLift,
                sample.LowerLipOpenDrop,
                sample.UpperLipBiteForward,
                sample.LowerLipBiteForward,
                sample.OuterUpperLipBiteDown,
                sample.OuterLowerLipBiteUp,
                sample.FrontRestAperture,
                sample.FrontOpenAperture,
                sample.FrontBiteAperture,
                sample.FrontBiteClosurePercent,
                sample.FrontUpperBiteDown,
                sample.FrontLowerBiteUp,
                bodyImpactAnalysis.AffectedVertexCount,
                bodyImpactAnalysis.MaximumForwardDelta,
                bodyImpactAnalysis.MaximumSideDelta,
                rootAnalysis.UpperVertexCount,
                rootAnalysis.LowerVertexCount,
                rootAnalysis.OuterUpperVertexCount,
                rootAnalysis.OuterLowerVertexCount,
                groups.InnerAffectedVertexCount,
                AnimationUtility.CalculateTransformPath(motionTarget, attackSlot),
                Sha256(Absolute(SourceModelPath)));
        }

        private static SampleMeasurements MeasureSamples(
            Transform attackSlot,
            Transform model,
            SkinnedMeshRenderer renderer,
            Animator animator,
            AnimationClip clip,
            MouthSkinGroups groups)
        {
            var transforms = attackSlot.GetComponentsInChildren<Transform>(true);
            var positions = transforms.Select(item => item.localPosition).ToArray();
            var rotations = transforms.Select(item => item.localRotation).ToArray();
            var scales = transforms.Select(item => item.localScale).ToArray();
            var weights = Enumerable.Range(0, renderer.sharedMesh.blendShapeCount)
                .Select(renderer.GetBlendShapeWeight).ToArray();
            var animatorEnabled = animator.enabled;
            try
            {
                animator.enabled = false;
                var modelIndex = Array.IndexOf(transforms, model);
                if (modelIndex < 0)
                {
                    throw new InvalidOperationException("Parvum model is missing from the attack transform snapshot.");
                }
                clip.SampleAnimation(attackSlot.gameObject, 0f);
                var restCenters = MeasureLipCenters(model, renderer, groups);
                var restOuterCenters = MeasureLipCenters(
                    model,
                    renderer,
                    groups.OuterUpperWeights,
                    groups.OuterLowerWeights);
                var restFrontCenters = MeasureLipCenters(
                    model,
                    renderer,
                    groups.FrontUpperWeights,
                    groups.FrontLowerWeights);
                var restAperture = restCenters.Aperture;
                var restBounds = BakedWorldBounds(renderer);
                var restForward = BakedMaximumAlongModelForward(model, renderer);
                var restModelY = model.localPosition.y;
                var restModelX = model.localPosition.x;
                var restModelZ = model.localPosition.z;
                var restModelRotation = model.localRotation;
                clip.SampleAnimation(attackSlot.gameObject, WideOpenTime);
                var openCenters = MeasureLipCenters(model, renderer, groups);
                var openFrontCenters = MeasureLipCenters(
                    model,
                    renderer,
                    groups.FrontUpperWeights,
                    groups.FrontLowerWeights);
                var openAperture = openCenters.Aperture;
                var openBounds = BakedWorldBounds(renderer);
                var openModelY = model.localPosition.y;
                var openModelX = model.localPosition.x;
                var openModelZ = model.localPosition.z;
                var openModelRotation = model.localRotation;
                clip.SampleAnimation(attackSlot.gameObject, ForwardLeanTime);
                var anticipationForward = BakedMaximumAlongModelForward(model, renderer);
                var leanBounds = BakedWorldBounds(renderer);
                var leanModelY = model.localPosition.y;
                var leanModelX = model.localPosition.x;
                var leanModelZ = model.localPosition.z;
                var leanModelRotation = model.localRotation;
                clip.SampleAnimation(attackSlot.gameObject, BiteImpactTime);
                var biteCenters = MeasureLipCenters(model, renderer, groups);
                var biteOuterCenters = MeasureLipCenters(
                    model,
                    renderer,
                    groups.OuterUpperWeights,
                    groups.OuterLowerWeights);
                var biteFrontCenters = MeasureLipCenters(
                    model,
                    renderer,
                    groups.FrontUpperWeights,
                    groups.FrontLowerWeights);
                var biteAperture = biteCenters.Aperture;
                var impactForward = BakedMaximumAlongModelForward(model, renderer);
                var biteBounds = BakedWorldBounds(renderer);
                var biteModelY = model.localPosition.y;
                var biteModelX = model.localPosition.x;
                var biteModelZ = model.localPosition.z;
                var biteModelRotation = model.localRotation;
                clip.SampleAnimation(attackSlot.gameObject, RecoveryTime);
                var recoveryAperture = MeasureMouthAperture(model, renderer, groups);
                var recoveryBounds = BakedWorldBounds(renderer);
                var recoveryModelY = model.localPosition.y;
                var recoveryModelX = model.localPosition.x;
                var recoveryModelZ = model.localPosition.z;
                var recoveryModelRotation = model.localRotation;
                if (Mathf.Abs(recoveryAperture - restAperture) > 0.001f)
                {
                    throw new InvalidOperationException("Parvum attack recovery does not return to the rest mouth aperture.");
                }

                var groundDelta = new[] { openBounds.min.y, leanBounds.min.y, biteBounds.min.y, recoveryBounds.min.y }
                    .Max(value => Mathf.Abs(value - restBounds.min.y));
                var modelVerticalTravel = new[] { openModelY, leanModelY, biteModelY, recoveryModelY }
                    .Max(value => Mathf.Abs(value - restModelY));
                var modelLateralTravel = new[] { openModelX, leanModelX, biteModelX, recoveryModelX }
                    .Max(value => Mathf.Abs(value - restModelX));
                var modelForwardPositionTravel = new[] { openModelZ, leanModelZ, biteModelZ, recoveryModelZ }
                    .Max(value => Mathf.Abs(value - restModelZ));
                var modelRotationTravel = new[]
                    {
                        openModelRotation,
                        leanModelRotation,
                        biteModelRotation,
                        recoveryModelRotation
                    }
                    .Select(value => Quaternion.Angle(restModelRotation, value))
                    .Max();
                return new SampleMeasurements(
                    restAperture,
                    openAperture,
                    biteAperture,
                    (openAperture / restAperture - 1f) * 100f,
                    (1f - biteAperture / openAperture) * 100f,
                    impactForward - restForward,
                    impactForward - anticipationForward,
                    groundDelta,
                    modelVerticalTravel,
                    modelLateralTravel,
                    modelRotationTravel,
                    modelForwardPositionTravel,
                    openCenters.Upper.y - restCenters.Upper.y,
                    restCenters.Lower.y - openCenters.Lower.y,
                    biteCenters.Upper.z - restCenters.Upper.z,
                    biteCenters.Lower.z - restCenters.Lower.z,
                    restOuterCenters.Upper.y - biteOuterCenters.Upper.y,
                    biteOuterCenters.Lower.y - restOuterCenters.Lower.y,
                    restFrontCenters.Aperture,
                    openFrontCenters.Aperture,
                    biteFrontCenters.Aperture,
                    (1f - biteFrontCenters.Aperture / openFrontCenters.Aperture) * 100f,
                    restFrontCenters.Upper.y - biteFrontCenters.Upper.y,
                    biteFrontCenters.Lower.y - restFrontCenters.Lower.y);
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

        private static MouthSkinGroups BuildMouthSkinGroups(
            SkinnedMeshRenderer renderer,
            Transform lowerJawRoot,
            Transform innerMouthRoot)
        {
            var mesh = renderer.sharedMesh ?? throw new InvalidOperationException("Parvum attack mesh is missing.");
            var bones = renderer.bones;
            var upperSurfaceNames = new HashSet<string>(UpperMouthSurfaceBoneNames, StringComparer.Ordinal);
            var lowerSurfaceNames = new HashSet<string>(LowerMouthSurfaceBoneNames, StringComparer.Ordinal);
            var upperRootIndices = new HashSet<int>(Enumerable.Range(0, bones.Length).Where(index =>
                bones[index] != null && upperSurfaceNames.Contains(bones[index].name)));
            var lowerRootIndices = new HashSet<int>(Enumerable.Range(0, bones.Length).Where(index =>
                bones[index] != null && lowerSurfaceNames.Contains(bones[index].name)));
            var toothTransforms = new HashSet<Transform>();
            foreach (var rootName in ToothBranchRootBoneNames)
            {
                var toothRoot = bones.FirstOrDefault(bone =>
                    bone != null && string.Equals(bone.name, rootName, StringComparison.Ordinal)) ??
                                throw new InvalidOperationException("Parvum tooth branch is missing: " + rootName + ".");
                foreach (var item in toothRoot.GetComponentsInChildren<Transform>(true))
                {
                    toothTransforms.Add(item);
                }
            }
            var toothIndices = new HashSet<int>(Enumerable.Range(0, bones.Length)
                .Where(index => bones[index] != null && toothTransforms.Contains(bones[index])));
            var innerIndices = new HashSet<int>();
            var innerTransforms = new HashSet<Transform>(innerMouthRoot.GetComponentsInChildren<Transform>(true));
            for (var index = 0; index < bones.Length; index++)
            {
                var bone = bones[index];
                if (bone == null)
                {
                    continue;
                }
                if (innerTransforms.Contains(bone))
                {
                    innerIndices.Add(index);
                }
            }
            if (upperRootIndices.Count != UpperMouthSurfaceBoneNames.Length ||
                lowerRootIndices.Count != LowerMouthSurfaceBoneNames.Length || innerIndices.Count < 2)
            {
                throw new InvalidOperationException("Parvum upper/lower lip and inner-mouth rig bones are incomplete.");
            }

            var upperRootWeights = new float[mesh.vertexCount];
            var lowerRootWeights = new float[mesh.vertexCount];
            var toothWeights = new float[mesh.vertexCount];
            var innerWeights = new float[mesh.vertexCount];
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
                        if (upperRootIndices.Contains(influence.boneIndex))
                        {
                            upperRootWeights[vertexIndex] += influence.weight;
                        }
                        if (lowerRootIndices.Contains(influence.boneIndex))
                        {
                            lowerRootWeights[vertexIndex] += influence.weight;
                        }
                        if (toothIndices.Contains(influence.boneIndex))
                        {
                            toothWeights[vertexIndex] += influence.weight;
                        }
                        if (innerIndices.Contains(influence.boneIndex))
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

            var vertices = mesh.vertices;
            var innerUpperWeights = new float[mesh.vertexCount];
            var innerLowerWeights = new float[mesh.vertexCount];
            var outerUpperWeights = new float[mesh.vertexCount];
            var outerLowerWeights = new float[mesh.vertexCount];
            var frontUpperWeights = new float[mesh.vertexCount];
            var frontLowerWeights = new float[mesh.vertexCount];
            var upperWeights = new float[mesh.vertexCount];
            var lowerWeights = new float[mesh.vertexCount];
            for (var index = 0; index < mesh.vertexCount; index++)
            {
                var vertex = vertices[index];
                var exclusion = 1f - Mathf.SmoothStep(0.15f, 0.65f, toothWeights[index]);
                innerUpperWeights[index] = upperRootWeights[index] * exclusion *
                                           BandWeight(vertex.x, -0.42f, -0.34f, 0.34f, 0.42f) *
                                           BandWeight(vertex.y, 0.78f, 0.84f, 1.10f, 1.17f) *
                                           BandWeight(vertex.z, 0.68f, 0.76f, 1.12f, 1.22f);
                innerLowerWeights[index] = Mathf.Clamp01(lowerRootWeights[index]) * exclusion *
                                           BandWeight(vertex.x, -0.45f, -0.36f, 0.36f, 0.45f) *
                                           BandWeight(vertex.y, 0.52f, 0.60f, 0.88f, 0.94f) *
                                           BandWeight(vertex.z, 0.56f, 0.65f, 1.08f, 1.18f);

                var upperFrontSurface = upperRootWeights[index] > 0.02f && exclusion > 0.05f && vertex.y >= 0.76f
                    ? BandWeight(vertex.x, -0.60f, -0.52f, 0.52f, 0.60f) *
                      BandWeight(vertex.y, 0.66f, 0.74f, 1.24f, 1.32f) *
                      BandWeight(vertex.z, 0.52f, 0.64f, 1.34f, 1.42f)
                    : 0f;
                var rigidUpperTeeth = toothWeights[index] > 0.01f ? 1f : 0f;
                var upperFrontRigid = rigidUpperTeeth;
                var lowerFrontSurface = lowerRootWeights[index] > 0.02f && exclusion > 0.05f && vertex.y < 0.76f
                    ? BandWeight(vertex.x, -0.60f, -0.52f, 0.52f, 0.60f) *
                      BandWeight(vertex.y, 0.30f, 0.38f, 0.86f, 0.94f) *
                      BandWeight(vertex.z, 0.48f, 0.60f, 1.34f, 1.42f)
                    : 0f;
                var lowerFrontRigid = lowerFrontSurface *
                                      BandWeight(vertex.x, -0.38f, -0.30f, 0.30f, 0.38f) *
                                      BandWeight(vertex.y, 0.46f, 0.52f, 0.72f, 0.78f) *
                                      BandWeight(vertex.z, 1.12f, 1.20f, 1.34f, 1.42f);
                var upperFlesh = Mathf.Clamp01(upperRootWeights[index]) * exclusion *
                                 BandWeight(vertex.x, -0.55f, -0.47f, 0.47f, 0.55f) *
                                 BandWeight(vertex.y, 0.70f, 0.76f, 1.22f, 1.30f) *
                                 BandWeight(vertex.z, 0.52f, 0.64f, 1.26f, 1.34f);
                var lowerFlesh = Mathf.Clamp01(lowerRootWeights[index]) * exclusion *
                                 BandWeight(vertex.x, -0.55f, -0.47f, 0.47f, 0.55f) *
                                 BandWeight(vertex.y, 0.38f, 0.46f, 0.94f, 1.00f) *
                                 BandWeight(vertex.z, 0.48f, 0.60f, 1.26f, 1.34f);
                var fullUpperMouth = Mathf.Max(upperFlesh, Mathf.Max(upperFrontSurface, upperFrontRigid));
                var fullLowerMouth = Mathf.Max(lowerFlesh, Mathf.Max(lowerFrontSurface, lowerFrontRigid));
                if (vertex.z >= FrontMouthMinimumZ)
                {
                    frontUpperWeights[index] = upperFrontRigid >= 0.999f && vertex.y >= 0.76f ? 1f : 0f;
                    frontLowerWeights[index] = lowerFrontRigid >= 0.999f && vertex.y < 0.76f ? 1f : 0f;
                }
                outerUpperWeights[index] = Mathf.Max(0f, fullUpperMouth - innerUpperWeights[index]);
                outerLowerWeights[index] = Mathf.Max(0f, fullLowerMouth - innerLowerWeights[index]);
                upperWeights[index] = Mathf.Max(innerUpperWeights[index], fullUpperMouth);
                lowerWeights[index] = Mathf.Max(innerLowerWeights[index], fullLowerMouth);
            }
            if (upperWeights.All(weight => weight <= GeometryTolerance) ||
                lowerWeights.All(weight => weight <= GeometryTolerance) ||
                frontUpperWeights.All(weight => weight <= GeometryTolerance) ||
                frontLowerWeights.All(weight => weight <= GeometryTolerance))
            {
                throw new InvalidOperationException("Parvum upper/lower or Image #2 foremost mouth skin groups are empty.");
            }

            return new MouthSkinGroups(
                upperWeights,
                lowerWeights,
                innerUpperWeights,
                innerLowerWeights,
                outerUpperWeights,
                outerLowerWeights,
                frontUpperWeights,
                frontLowerWeights,
                innerWeights,
                innerAffected.Count(value => value));
        }

        private static float MeasureMouthAperture(
            Transform model,
            SkinnedMeshRenderer renderer,
            MouthSkinGroups groups)
        {
            return MeasureLipCenters(model, renderer, groups).Aperture;
        }

        private static LipCenters MeasureLipCenters(
            Transform model,
            SkinnedMeshRenderer renderer,
            MouthSkinGroups groups)
        {
            return MeasureLipCenters(model, renderer, groups.UpperWeights, groups.LowerWeights);
        }

        private static LipCenters MeasureLipCenters(
            Transform model,
            SkinnedMeshRenderer renderer,
            IReadOnlyList<float> upperWeights,
            IReadOnlyList<float> lowerWeights)
        {
            var baked = new Mesh();
            try
            {
                renderer.BakeMesh(baked, false);
                var upper = WeightedCenterInModelSpace(model, renderer, baked.vertices, upperWeights);
                var lower = WeightedCenterInModelSpace(model, renderer, baked.vertices, lowerWeights);
                return new LipCenters(upper, lower);
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
            var sum = Vector3.zero;
            var weightSum = 0f;
            for (var index = 0; index < vertices.Count; index++)
            {
                if (weights[index] <= 0f)
                {
                    continue;
                }
                sum += model.InverseTransformPoint(renderer.transform.TransformPoint(vertices[index])) * weights[index];
                weightSum += weights[index];
            }
            if (weightSum <= GeometryTolerance)
            {
                throw new InvalidOperationException("Parvum mouth aperture rig group is empty.");
            }
            return sum / weightSum;
        }

        private static void CaptureComparison(
            Transform attackSlot,
            SkinnedMeshRenderer renderer,
            Animator animator,
            AnimationClip clip,
            string destination)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ??
                                      throw new InvalidOperationException("Invalid Parvum attack capture path."));
            _ = Camera.main ?? UnityEngine.Object.FindFirstObjectByType<Camera>() ??
                throw new InvalidOperationException("The scene has no camera for Parvum attack framing.");
            var transforms = attackSlot.GetComponentsInChildren<Transform>(true);
            var layers = transforms.Select(item => item.gameObject.layer).ToArray();
            var positions = transforms.Select(item => item.localPosition).ToArray();
            var rotations = transforms.Select(item => item.localRotation).ToArray();
            var scales = transforms.Select(item => item.localScale).ToArray();
            var weights = Enumerable.Range(0, renderer.sharedMesh.blendShapeCount)
                .Select(renderer.GetBlendShapeWeight).ToArray();
            var animatorEnabled = animator.enabled;
            var forceRecalculation = renderer.forceMatrixRecalculationPerRender;
            var updateWhenOffscreen = renderer.updateWhenOffscreen;
            var rendererLocalBounds = renderer.localBounds;
            var previousActive = RenderTexture.active;
            var target = new RenderTexture(PanelWidth, CaptureHeight, 24, RenderTextureFormat.ARGB32);
            var panelImage = new Texture2D(PanelWidth, CaptureHeight, TextureFormat.RGB24, false);
            var composite = new Texture2D(
                PanelWidth * CaptureTimes.Length,
                CaptureHeight * 2,
                TextureFormat.RGB24,
                false);
            var cameraObject = new GameObject("ParvumAttackReviewCamera", typeof(Camera))
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            var lightObject = new GameObject("ParvumAttackReviewLight", typeof(Light))
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            try
            {
                animator.enabled = false;
                renderer.forceMatrixRecalculationPerRender = true;
                renderer.updateWhenOffscreen = true;
                renderer.localBounds = new Bounds(renderer.sharedMesh.bounds.center, Vector3.one * 20f);
                for (var index = 0; index < transforms.Length; index++)
                {
                    transforms[index].gameObject.layer = ReviewLayer;
                }

                Bounds framingBounds = default;
                var hasBounds = false;
                foreach (var time in CaptureTimes)
                {
                    clip.SampleAnimation(attackSlot.gameObject, time);
                    var sampled = BakedWorldBounds(renderer);
                    if (!hasBounds)
                    {
                        framingBounds = sampled;
                        hasBounds = true;
                    }
                    else
                    {
                        framingBounds.Encapsulate(sampled);
                    }
                }

                var reviewCamera = cameraObject.GetComponent<Camera>();
                reviewCamera.clearFlags = CameraClearFlags.SolidColor;
                reviewCamera.backgroundColor = new Color(0.012f, 0.016f, 0.02f, 1f);
                reviewCamera.cullingMask = 1 << ReviewLayer;
                reviewCamera.allowHDR = false;
                reviewCamera.allowMSAA = false;
                reviewCamera.usePhysicalProperties = false;
                reviewCamera.rect = new Rect(0f, 0f, 1f, 1f);
                reviewCamera.nearClipPlane = 0.01f;
                reviewCamera.farClipPlane = 1000f;
                reviewCamera.targetTexture = target;
                reviewCamera.aspect = PanelWidth / (float)CaptureHeight;
                reviewCamera.fieldOfView = 32f;
                var worldForward = renderer.transform.TransformDirection(Vector3.forward).normalized;
                var worldRight = renderer.transform.TransformDirection(Vector3.right).normalized;
                var viewDirection = (-worldForward + worldRight * 0.82f + Vector3.down * 0.04f).normalized;
                var verticalRadians = reviewCamera.fieldOfView * 0.5f * Mathf.Deg2Rad;
                var horizontalRadians = Mathf.Atan(Mathf.Tan(verticalRadians) * reviewCamera.aspect);
                var distance = Mathf.Max(
                    framingBounds.extents.y / Mathf.Max(0.01f, Mathf.Tan(verticalRadians)),
                    framingBounds.extents.x / Mathf.Max(0.01f, Mathf.Tan(horizontalRadians))) * 1.32f;
                Debug.Log(
                    "ParvumAttackCaptureFraming BoundsCenter=" + Vec(framingBounds.center) +
                    ", BoundsSize=" + Vec(framingBounds.size) +
                    ", CameraDistance=" + Num(distance));
                var reviewRotation = Quaternion.LookRotation(viewDirection, Vector3.up);
                var fullBodyCameraPosition = framingBounds.center - viewDirection * distance;
                var mouthBounds = TransformLocalBoundsToWorld(
                    renderer.transform,
                    new Bounds(new Vector3(0f, 0.86f, 1.08f), new Vector3(1.04f, 1.02f, 0.82f)));
                var mouthDistance = Mathf.Max(
                    mouthBounds.extents.y / Mathf.Max(0.01f, Mathf.Tan(verticalRadians)),
                    mouthBounds.extents.x / Mathf.Max(0.01f, Mathf.Tan(horizontalRadians))) * 1.12f;
                var mouthCameraPosition = mouthBounds.center - viewDirection * mouthDistance;
                reviewCamera.transform.SetPositionAndRotation(fullBodyCameraPosition, reviewRotation);

                var light = lightObject.GetComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.35f;
                light.color = new Color(0.9f, 0.95f, 1f);
                light.cullingMask = 1 << ReviewLayer;
                light.shadows = LightShadows.None;
                light.transform.rotation = Quaternion.LookRotation(viewDirection + new Vector3(-0.4f, -0.5f, 0.2f), Vector3.up);

                for (var panel = 0; panel < CaptureTimes.Length; panel++)
                {
                    clip.SampleAnimation(attackSlot.gameObject, CaptureTimes[panel]);
                    RenderTexture.active = target;
                    reviewCamera.transform.SetPositionAndRotation(fullBodyCameraPosition, reviewRotation);
                    reviewCamera.Render();
                    panelImage.ReadPixels(new Rect(0, 0, PanelWidth, CaptureHeight), 0, 0);
                    panelImage.Apply();
                    composite.SetPixels32(
                        panel * PanelWidth,
                        CaptureHeight,
                        PanelWidth,
                        CaptureHeight,
                        panelImage.GetPixels32());

                    reviewCamera.transform.SetPositionAndRotation(mouthCameraPosition, reviewRotation);
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
                    transforms[index].gameObject.layer = layers[index];
                    transforms[index].localPosition = positions[index];
                    transforms[index].localRotation = rotations[index];
                    transforms[index].localScale = scales[index];
                }
                for (var index = 0; index < weights.Length; index++)
                {
                    renderer.SetBlendShapeWeight(index, weights[index]);
                }
                renderer.forceMatrixRecalculationPerRender = forceRecalculation;
                renderer.updateWhenOffscreen = updateWhenOffscreen;
                renderer.localBounds = rendererLocalBounds;
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

        private static Bounds TransformLocalBoundsToWorld(Transform transform, Bounds localBounds)
        {
            var worldBounds = new Bounds(transform.TransformPoint(localBounds.center), Vector3.zero);
            for (var x = -1; x <= 1; x += 2)
            {
                for (var y = -1; y <= 1; y += 2)
                {
                    for (var z = -1; z <= 1; z += 2)
                    {
                        worldBounds.Encapsulate(transform.TransformPoint(
                            localBounds.center + Vector3.Scale(localBounds.extents, new Vector3(x, y, z))));
                    }
                }
            }
            return worldBounds;
        }

        private static string RequireReviewPhysics(Transform attackSlot, Transform motionTarget)
        {
            var body = attackSlot.GetComponent<Rigidbody>() ??
                       throw new InvalidOperationException("Parvum attack root Rigidbody is missing.");
            if (!body.isKinematic)
            {
                throw new InvalidOperationException("Parvum attack review Rigidbody must remain kinematic.");
            }
            if (attackSlot.GetComponent<Collider>() == null)
            {
                throw new InvalidOperationException("Parvum attack root Collider is missing.");
            }
            var driver = attackSlot.GetComponent<ParvumPhysicsMotionDriver>() ??
                         throw new InvalidOperationException("Parvum attack physics driver is missing.");
            if (driver.MotionPathTarget != motionTarget || !driver.LockRootMotionForReview)
            {
                throw new InvalidOperationException("Parvum attack physics target or review lock changed.");
            }
            return PhysicsSignature(attackSlot, motionTarget);
        }

        private static string PhysicsSignature(Transform attackSlot, Transform motionTarget)
        {
            var body = attackSlot.GetComponent<Rigidbody>() ??
                       throw new InvalidOperationException("Parvum attack Rigidbody is missing during signature capture.");
            var collider = attackSlot.GetComponent<Collider>() ??
                           throw new InvalidOperationException("Parvum attack Collider is missing during signature capture.");
            var driver = attackSlot.GetComponent<ParvumPhysicsMotionDriver>() ??
                         throw new InvalidOperationException("Parvum attack driver is missing during signature capture.");
            return string.Join("|",
                body.isKinematic,
                body.useGravity,
                Num(body.mass),
                Num(body.linearDamping),
                Num(body.angularDamping),
                body.constraints,
                collider.GetType().FullName,
                collider.enabled,
                driver.MotionPathTarget == motionTarget,
                driver.LockRootMotionForReview,
                TransformSignature(motionTarget));
        }

        private static void RequireOnlyAttackConfigured(
            Transform parvumRoot,
            Transform attackSlot,
            Animator attackAnimator)
        {
            foreach (Transform slot in parvumRoot)
            {
                if (slot == attackSlot)
                {
                    if (slot.GetComponentsInChildren<Animator>(true)
                        .Count(candidate => candidate.runtimeAnimatorController != null) != 1)
                    {
                        throw new InvalidOperationException("Parvum attack must have exactly one configured Animator.");
                    }
                    continue;
                }
                if (slot.GetComponentsInChildren<Animator>(true)
                    .Any(candidate => candidate.runtimeAnimatorController == attackAnimator.runtimeAnimatorController))
                {
                    throw new InvalidOperationException(slot.name + " unexpectedly uses the new Parvum attack controller.");
                }
            }
        }

        private static AnimationCurve RequireCurve(
            AnimationClip clip,
            string path,
            Type type,
            string property)
        {
            var binding = AnimationUtility.GetCurveBindings(clip).SingleOrDefault(candidate =>
                candidate.path == path && candidate.type == type && candidate.propertyName == property);
            if (string.IsNullOrEmpty(binding.propertyName))
            {
                throw new InvalidOperationException("Parvum attack curve is missing: " + property + ".");
            }
            return AnimationUtility.GetEditorCurve(clip, binding) ??
                   throw new InvalidOperationException("Parvum attack curve is unreadable: " + property + ".");
        }

        private static void RequireCurveValue(AnimationCurve curve, float time, float expected, string label)
        {
            var actual = curve.Evaluate(time);
            if (Mathf.Abs(actual - expected) > 0.01f)
            {
                throw new InvalidOperationException(
                    "Parvum attack " + label + " curve value is invalid. Expected=" +
                    Num(expected) + ", Actual=" + Num(actual) + ".");
            }
        }

        private static void RequireRigidFrontMouthGroup(
            MouthRootDeltas deltas,
            IReadOnlyList<bool> selectedVertices,
            string label)
        {
            var selected = Enumerable.Range(0, selectedVertices.Count)
                .Where(index => selectedVertices[index])
                .ToArray();
            if (selected.Length < 25)
            {
                throw new InvalidOperationException(
                    "Parvum " + label + " rigid group is incomplete. Vertices=" +
                    selected.Length.ToString(CultureInfo.InvariantCulture) + ".");
            }

            var referenceOpen = deltas.OpenDeltas[selected[0]];
            var referenceBite = deltas.BiteDeltas[selected[0]];
            var maximumOpenDeviation = selected.Max(index =>
                Vector3.Distance(referenceOpen, deltas.OpenDeltas[index]));
            var maximumBiteDeviation = selected.Max(index =>
                Vector3.Distance(referenceBite, deltas.BiteDeltas[index]));
            if (maximumOpenDeviation > GeometryTolerance || maximumBiteDeviation > GeometryTolerance)
            {
                throw new InvalidOperationException(
                    "Parvum " + label + " must move as one rigid mouth assembly. OpenDeviation=" +
                    Num(maximumOpenDeviation) + ", BiteDeviation=" + Num(maximumBiteDeviation) + ".");
            }
        }

        private static void BuildNormalAndTangentDeltas(
            Mesh source,
            Vector3[] targetVertices,
            out Vector3[] deltaNormals,
            out Vector3[] deltaTangents)
        {
            var target = UnityEngine.Object.Instantiate(source);
            try
            {
                target.ClearBlendShapes();
                target.vertices = targetVertices;
                target.RecalculateNormals();
                target.RecalculateTangents();
                var sourceNormals = source.normals;
                var targetNormals = target.normals;
                var sourceTangents = source.tangents;
                var targetTangents = target.tangents;
                deltaNormals = new Vector3[targetVertices.Length];
                deltaTangents = new Vector3[targetVertices.Length];
                for (var index = 0; index < targetVertices.Length; index++)
                {
                    if (index < sourceNormals.Length && index < targetNormals.Length)
                    {
                        deltaNormals[index] = targetNormals[index] - sourceNormals[index];
                    }
                    if (index < sourceTangents.Length && index < targetTangents.Length)
                    {
                        deltaTangents[index] =
                            new Vector3(targetTangents[index].x, targetTangents[index].y, targetTangents[index].z) -
                            new Vector3(sourceTangents[index].x, sourceTangents[index].y, sourceTangents[index].z);
                    }
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        private static Bounds BakedWorldBounds(SkinnedMeshRenderer renderer)
        {
            var baked = new Mesh();
            try
            {
                renderer.BakeMesh(baked, false);
                var vertices = baked.vertices;
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

        private static float BakedMaximumAlongModelForward(Transform model, SkinnedMeshRenderer renderer)
        {
            var baked = new Mesh();
            try
            {
                renderer.BakeMesh(baked, false);
                var forward = model.forward.normalized;
                var matrix = renderer.transform.localToWorldMatrix;
                var maximum = float.NegativeInfinity;
                foreach (var vertex in baked.vertices)
                {
                    maximum = Mathf.Max(maximum, Vector3.Dot(matrix.MultiplyPoint3x4(vertex), forward));
                }
                return maximum;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(baked);
            }
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
            var response = lowerDisplacementSum / lowerWeightSum - upperDisplacementSum / upperWeightSum;
            if (response <= GeometryTolerance)
            {
                throw new InvalidOperationException("Parvum mouth-root response is invalid.");
            }
            return response;
        }

        private static SkinnedMeshRenderer RequireSourceRenderer()
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(SourceModelPath) ??
                         throw new InvalidOperationException("The supplied Parvum GLB asset is missing.");
            var renderers = source.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Where(candidate => candidate.sharedMesh != null).ToArray();
            if (renderers.Length != 1)
            {
                throw new InvalidOperationException("The supplied Parvum GLB must contain exactly one body renderer.");
            }
            return renderers[0];
        }

        private static SkinnedMeshRenderer RequireSingleBodyRenderer(Transform model)
        {
            var renderers = model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Where(candidate => candidate.sharedMesh != null && candidate.gameObject.activeInHierarchy).ToArray();
            if (renderers.Length != 1)
            {
                throw new InvalidOperationException("Current Parvum model must contain exactly one active body renderer.");
            }
            return renderers[0];
        }

        private static void RequireCompatibleSource(SkinnedMeshRenderer current, SkinnedMeshRenderer source)
        {
            if (current.sharedMesh == null || source.sharedMesh == null ||
                current.sharedMesh.vertexCount != source.sharedMesh.vertexCount ||
                current.sharedMesh.subMeshCount != source.sharedMesh.subMeshCount)
            {
                throw new InvalidOperationException("Current Parvum attack renderer does not match the supplied GLB mesh.");
            }
        }

        private static void RequireLocalPositiveZForward(Transform model)
        {
            var muzzleEnd = FindChildRecursive(model, InnerMouthRootBoneName) ??
                            throw new InvalidOperationException("Parvum muzzle direction bone is missing.");
            var local = model.InverseTransformPoint(muzzleEnd.position);
            if (local.z <= 0.5f || local.z <= Mathf.Abs(local.x))
            {
                throw new InvalidOperationException("Parvum muzzle direction is not local +Z: " + Vec(local) + ".");
            }
        }

        private static string[] OtherParvumSlotSignatures(Transform root)
        {
            return root.Cast<Transform>()
                .Where(slot => !string.Equals(slot.name, AttackSlotName, StringComparison.Ordinal))
                .Select(SlotSignature).OrderBy(value => value, StringComparer.Ordinal).ToArray();
        }

        private static string[] ProtectedRootSignatures(Scene scene)
        {
            return scene.GetRootGameObjects()
                .Where(root => !string.Equals(root.name, ParvumRootName, StringComparison.Ordinal))
                .Select(root => SlotSignature(root.transform))
                .OrderBy(value => value, StringComparer.Ordinal).ToArray();
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

        private static Transform FindChildRecursive(Transform root, string childName)
        {
            return root.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(candidate => string.Equals(candidate.name, childName, StringComparison.Ordinal));
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

        private static string TransformSignature(Transform item)
        {
            return Vec(item.localPosition) + "|" + Vec(item.localEulerAngles) + "|" + Vec(item.localScale);
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
            Directory.CreateDirectory(Absolute(OutputFolder));
            var report = new StringBuilder()
                .AppendLine("Parvum New-Model Bite Attack Report")
                .AppendLine("Result=PASS")
                .AppendLine("Target=" + ParvumRootName + "/" + AttackSlotName + "/" + ModelName)
                .AppendLine("SourceModel=" + SourceModelPath)
                .AppendLine("SourceSha256=" + result.SourceSha256)
                .AppendLine("GeneratedMesh=" + GeneratedMeshPath)
                .AppendLine("AnimationClip=" + ClipPath)
                .AppendLine("AnimatorController=" + ControllerPath)
                .AppendLine("OldAttackClip=" + OldAttackClipPath)
                .AppendLine("OldAttackController=" + OldAttackControllerPath)
                .AppendLine("OldAttackAssetsAssigned=False")
                .AppendLine("CycleSeconds=" + Num(result.CycleSeconds))
                .AppendLine("TimingBasis=User-approved forceful bite with a held opening and synchronized short impact snap")
                .AppendLine("PhaseTimes=0,0.84,1.80,1.96,2.28,2.64,3.00")
                .AppendLine("Phases=Rest,WideOpen,AdvanceAnticipation,BiteSnapStart,ClosedBiteImpact,ImpactFollowThrough,Recovered")
                .AppendLine("BiteSnapDurationSeconds=" + Num(BiteImpactTime - BiteSnapStartTime))
                .AppendLine("LocalForward=+Z")
                .AppendLine("ForwardAdvanceDriver=Grounded full-body visible-mesh expansion without Transform curves")
                .AppendLine("MouthRootOpenBlendShape=" + OpenRootBlendShapeName)
                .AppendLine("MouthRootBiteBlendShape=" + BiteRootBlendShapeName)
                .AppendLine("BodyImpactBlendShape=" + BodyImpactBlendShapeName)
                .AppendLine("MouthRigRoot=" + LowerJawRootBoneName)
                .AppendLine("UpperVisibleMouthSurfaceRigs=" + string.Join(",", UpperMouthSurfaceBoneNames))
                .AppendLine("LowerVisibleMouthSurfaceRigs=" + string.Join(",", LowerMouthSurfaceBoneNames))
                .AppendLine("InnerMouthRigRoot=" + InnerMouthRootBoneName)
                .AppendLine("RigidUpperToothBranchRigs=" + string.Join(",", ToothBranchRootBoneNames))
                .AppendLine("VertexCount=" + result.VertexCount.ToString(CultureInfo.InvariantCulture))
                .AppendLine("UpperRootAffectedVertices=" + result.UpperRootVertexCount.ToString(CultureInfo.InvariantCulture))
                .AppendLine("LowerRootAffectedVertices=" + result.LowerRootVertexCount.ToString(CultureInfo.InvariantCulture))
                .AppendLine("OuterUpperRootAffectedVertices=" + result.OuterUpperRootVertexCount.ToString(CultureInfo.InvariantCulture))
                .AppendLine("OuterLowerRootAffectedVertices=" + result.OuterLowerRootVertexCount.ToString(CultureInfo.InvariantCulture))
                .AppendLine("InnerMouthAffectedVertices=" + result.InnerMouthVertexCount.ToString(CultureInfo.InvariantCulture))
                .AppendLine("RestAperture=" + Num(result.RestAperture))
                .AppendLine("WideOpenAperture=" + Num(result.OpenAperture))
                .AppendLine("BiteAperture=" + Num(result.BiteAperture))
                .AppendLine("WideOpenPercent=" + Num(result.WideOpenPercent))
                .AppendLine("BiteClosurePercent=" + Num(result.BiteClosurePercent))
                .AppendLine("MouthRootOpenRatio=" + Num(MouthRootOpenRatio))
                .AppendLine("MouthRootBiteRatio=" + Num(MouthRootBiteRatio))
                .AppendLine("LipPuckerForwardGapRatio=" + Num(LipPuckerForwardGapRatio))
                .AppendLine("OuterLipOpenBoost=" + Num(OuterLipOpenBoost))
                .AppendLine("OuterUpperBiteBoost=" + Num(OuterUpperBiteBoost))
                .AppendLine("OuterLowerBiteBoost=" + Num(OuterLowerBiteBoost))
                .AppendLine("OuterLipPuckerBoost=" + Num(OuterLipPuckerBoost))
                .AppendLine("UpperLipOpenLift=" + Num(result.UpperLipOpenLift))
                .AppendLine("LowerLipOpenDrop=" + Num(result.LowerLipOpenDrop))
                .AppendLine("UpperLipBiteForward=" + Num(result.UpperLipBiteForward))
                .AppendLine("LowerLipBiteForward=" + Num(result.LowerLipBiteForward))
                .AppendLine("OuterUpperLipBiteDown=" + Num(result.OuterUpperLipBiteDown))
                .AppendLine("OuterLowerLipBiteUp=" + Num(result.OuterLowerLipBiteUp))
                .AppendLine("Image2FrontSelectionMinimumZ=" + Num(FrontMouthMinimumZ))
                .AppendLine("Image2FrontRestAperture=" + Num(result.FrontRestAperture))
                .AppendLine("Image2FrontOpenAperture=" + Num(result.FrontOpenAperture))
                .AppendLine("Image2FrontBiteAperture=" + Num(result.FrontBiteAperture))
                .AppendLine("Image2FrontBiteClosurePercent=" + Num(result.FrontBiteClosurePercent))
                .AppendLine("Image2FrontUpperBiteDown=" + Num(result.FrontUpperBiteDown))
                .AppendLine("Image2FrontLowerBiteUp=" + Num(result.FrontLowerBiteUp))
                .AppendLine("RigidFrontMouthAssemblies=True")
                .AppendLine("BodyImpactAffectedVertices=" + result.BodyImpactVertexCount.ToString(CultureInfo.InvariantCulture))
                .AppendLine("BodyImpactScope=Full grounded torso excluding separately animated mouth assemblies")
                .AppendLine("BodyImpactForwardExpansionMaximum=" + Num(BodyImpactForwardExpansionMaximum))
                .AppendLine("BodyImpactForwardExpansionRatio=" + Num(BodyImpactForwardExpansionRatio))
                .AppendLine("BodyImpactSideExpansionRatio=" + Num(BodyImpactSideExpansionRatio))
                .AppendLine("BodyImpactVerticalExpansionRatio=" + Num(BodyImpactVerticalExpansionRatio))
                .AppendLine("BodyImpactForwardExpansion=" + Num(result.BodyImpactForwardExpansion))
                .AppendLine("BodyImpactSideExpansion=" + Num(result.BodyImpactSideExpansion))
                .AppendLine("ForwardAdvanceDistance=" + Num(result.ForwardLeanDistance))
                .AppendLine("ImpactLungeDistance=" + Num(result.ImpactLungeDistance))
                .AppendLine("WorldGroundDelta=" + Num(result.WorldGroundDelta))
                .AppendLine("ModelVerticalTravel=" + Num(result.ModelVerticalTravel))
                .AppendLine("ModelLateralTravel=" + Num(result.ModelLateralTravel))
                .AppendLine("ModelRotationTravel=" + Num(result.ModelRotationTravel))
                .AppendLine("ModelForwardPositionTravel=" + Num(result.ModelForwardPositionTravel))
                .AppendLine("MouthRigTransformCurves=False")
                .AppendLine("ModelTransformCurves=False")
                .AppendLine("RootTransformCurves=False")
                .AppendLine("RigidbodyColliderDriverPreserved=True")
                .AppendLine("MotionTargetPath=" + result.MotionTargetPath)
                .AppendLine("DamageAiHealthPlayerHitLogicChanged=False")
                .AppendLine("OtherParvumSlotsChanged=False")
                .AppendLine("OtherSceneRootsChanged=False")
                .AppendLine("CaptureCreated=" + captureCreated)
                .AppendLine("CapturePath=" + CapturePath);
            File.WriteAllText(Absolute(ReportPath), report.ToString(), new UTF8Encoding(false));
        }

        private static string Sha256(string path)
        {
            using var stream = File.OpenRead(path);
            using var algorithm = SHA256.Create();
            return BitConverter.ToString(algorithm.ComputeHash(stream)).Replace("-", string.Empty);
        }

        private static string Absolute(string relativePath)
        {
            return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), relativePath));
        }

        private static float QuaternionComponent(Quaternion value, int index)
        {
            return index switch
            {
                0 => value.x,
                1 => value.y,
                2 => value.z,
                3 => value.w,
                _ => throw new ArgumentOutOfRangeException(nameof(index))
            };
        }

        private static float Vector3Component(Vector3 value, int index)
        {
            return index switch
            {
                0 => value.x,
                1 => value.y,
                2 => value.z,
                _ => throw new ArgumentOutOfRangeException(nameof(index))
            };
        }

        private static string Num(float value)
        {
            return value.ToString("0.######", CultureInfo.InvariantCulture);
        }

        private static string Vec(Vector3 value)
        {
            return "(" + Num(value.x) + "," + Num(value.y) + "," + Num(value.z) + ")";
        }

        private sealed class MouthRootDeltas
        {
            public MouthRootDeltas(
                Vector3[] openDeltas,
                Vector3[] biteDeltas,
                bool[] rigidUpperFrontVertices,
                bool[] rigidLowerFrontVertices)
            {
                OpenDeltas = openDeltas;
                BiteDeltas = biteDeltas;
                RigidUpperFrontVertices = rigidUpperFrontVertices;
                RigidLowerFrontVertices = rigidLowerFrontVertices;
            }

            public Vector3[] OpenDeltas { get; }
            public Vector3[] BiteDeltas { get; }
            public bool[] RigidUpperFrontVertices { get; }
            public bool[] RigidLowerFrontVertices { get; }
        }

        private sealed class MouthRigInfluenceData
        {
            public MouthRigInfluenceData(
                float[] upperRootWeights,
                float[] lowerRootWeights,
                float[] exclusionWeights)
            {
                UpperRootWeights = upperRootWeights;
                LowerRootWeights = lowerRootWeights;
                ExclusionWeights = exclusionWeights;
            }

            public float[] UpperRootWeights { get; }
            public float[] LowerRootWeights { get; }
            public float[] ExclusionWeights { get; }
        }

        private readonly struct MouthRootAnalysis
        {
            public MouthRootAnalysis(
                int upperVertexCount,
                int lowerVertexCount,
                int outerUpperVertexCount,
                int outerLowerVertexCount,
                float openPercent,
                float biteClosurePercent,
                float puckerForwardDistance)
            {
                UpperVertexCount = upperVertexCount;
                LowerVertexCount = lowerVertexCount;
                OuterUpperVertexCount = outerUpperVertexCount;
                OuterLowerVertexCount = outerLowerVertexCount;
                OpenPercent = openPercent;
                BiteClosurePercent = biteClosurePercent;
                PuckerForwardDistance = puckerForwardDistance;
            }

            public int UpperVertexCount { get; }
            public int LowerVertexCount { get; }
            public int OuterUpperVertexCount { get; }
            public int OuterLowerVertexCount { get; }
            public float OpenPercent { get; }
            public float BiteClosurePercent { get; }
            public float PuckerForwardDistance { get; }
        }

        private readonly struct BodyImpactAnalysis
        {
            public BodyImpactAnalysis(
                int affectedVertexCount,
                float maximumForwardDelta,
                float maximumSideDelta,
                float minimumAffectedHeightAboveGround)
            {
                AffectedVertexCount = affectedVertexCount;
                MaximumForwardDelta = maximumForwardDelta;
                MaximumSideDelta = maximumSideDelta;
                MinimumAffectedHeightAboveGround = minimumAffectedHeightAboveGround;
            }

            public int AffectedVertexCount { get; }
            public float MaximumForwardDelta { get; }
            public float MaximumSideDelta { get; }
            public float MinimumAffectedHeightAboveGround { get; }
        }

        private readonly struct LipCenters
        {
            public LipCenters(Vector3 upper, Vector3 lower)
            {
                Upper = upper;
                Lower = lower;
            }

            public Vector3 Upper { get; }
            public Vector3 Lower { get; }
            public float Aperture => Upper.y - Lower.y;
        }

        private sealed class MouthSkinGroups
        {
            public MouthSkinGroups(
                float[] upperWeights,
                float[] lowerWeights,
                float[] innerUpperWeights,
                float[] innerLowerWeights,
                float[] outerUpperWeights,
                float[] outerLowerWeights,
                float[] frontUpperWeights,
                float[] frontLowerWeights,
                float[] innerWeights,
                int innerAffectedVertexCount)
            {
                UpperWeights = upperWeights;
                LowerWeights = lowerWeights;
                InnerUpperWeights = innerUpperWeights;
                InnerLowerWeights = innerLowerWeights;
                OuterUpperWeights = outerUpperWeights;
                OuterLowerWeights = outerLowerWeights;
                FrontUpperWeights = frontUpperWeights;
                FrontLowerWeights = frontLowerWeights;
                InnerWeights = innerWeights;
                InnerAffectedVertexCount = innerAffectedVertexCount;
            }

            public float[] UpperWeights { get; }
            public float[] LowerWeights { get; }
            public float[] InnerUpperWeights { get; }
            public float[] InnerLowerWeights { get; }
            public float[] OuterUpperWeights { get; }
            public float[] OuterLowerWeights { get; }
            public float[] FrontUpperWeights { get; }
            public float[] FrontLowerWeights { get; }
            public float[] InnerWeights { get; }
            public int InnerAffectedVertexCount { get; }
        }

        private readonly struct SampleMeasurements
        {
            public SampleMeasurements(
                float restAperture,
                float openAperture,
                float biteAperture,
                float wideOpenPercent,
                float biteClosurePercent,
                float forwardLeanDistance,
                float impactLungeDistance,
                float worldGroundDelta,
                float modelVerticalTravel,
                float modelLateralTravel,
                float modelRotationTravel,
                float modelForwardPositionTravel,
                float upperLipOpenLift,
                float lowerLipOpenDrop,
                float upperLipBiteForward,
                float lowerLipBiteForward,
                float outerUpperLipBiteDown,
                float outerLowerLipBiteUp,
                float frontRestAperture,
                float frontOpenAperture,
                float frontBiteAperture,
                float frontBiteClosurePercent,
                float frontUpperBiteDown,
                float frontLowerBiteUp)
            {
                RestAperture = restAperture;
                OpenAperture = openAperture;
                BiteAperture = biteAperture;
                WideOpenPercent = wideOpenPercent;
                BiteClosurePercent = biteClosurePercent;
                ForwardLeanDistance = forwardLeanDistance;
                ImpactLungeDistance = impactLungeDistance;
                WorldGroundDelta = worldGroundDelta;
                ModelVerticalTravel = modelVerticalTravel;
                ModelLateralTravel = modelLateralTravel;
                ModelRotationTravel = modelRotationTravel;
                ModelForwardPositionTravel = modelForwardPositionTravel;
                UpperLipOpenLift = upperLipOpenLift;
                LowerLipOpenDrop = lowerLipOpenDrop;
                UpperLipBiteForward = upperLipBiteForward;
                LowerLipBiteForward = lowerLipBiteForward;
                OuterUpperLipBiteDown = outerUpperLipBiteDown;
                OuterLowerLipBiteUp = outerLowerLipBiteUp;
                FrontRestAperture = frontRestAperture;
                FrontOpenAperture = frontOpenAperture;
                FrontBiteAperture = frontBiteAperture;
                FrontBiteClosurePercent = frontBiteClosurePercent;
                FrontUpperBiteDown = frontUpperBiteDown;
                FrontLowerBiteUp = frontLowerBiteUp;
            }

            public float RestAperture { get; }
            public float OpenAperture { get; }
            public float BiteAperture { get; }
            public float WideOpenPercent { get; }
            public float BiteClosurePercent { get; }
            public float ForwardLeanDistance { get; }
            public float ImpactLungeDistance { get; }
            public float WorldGroundDelta { get; }
            public float ModelVerticalTravel { get; }
            public float ModelLateralTravel { get; }
            public float ModelRotationTravel { get; }
            public float ModelForwardPositionTravel { get; }
            public float UpperLipOpenLift { get; }
            public float LowerLipOpenDrop { get; }
            public float UpperLipBiteForward { get; }
            public float LowerLipBiteForward { get; }
            public float OuterUpperLipBiteDown { get; }
            public float OuterLowerLipBiteUp { get; }
            public float FrontRestAperture { get; }
            public float FrontOpenAperture { get; }
            public float FrontBiteAperture { get; }
            public float FrontBiteClosurePercent { get; }
            public float FrontUpperBiteDown { get; }
            public float FrontLowerBiteUp { get; }
        }

        private sealed class InspectionResult
        {
            public InspectionResult(
                int vertexCount,
                float cycleSeconds,
                float restAperture,
                float openAperture,
                float biteAperture,
                float wideOpenPercent,
                float biteClosurePercent,
                float forwardLeanDistance,
                float impactLungeDistance,
                float worldGroundDelta,
                float modelVerticalTravel,
                float modelLateralTravel,
                float modelRotationTravel,
                float modelForwardPositionTravel,
                float upperLipOpenLift,
                float lowerLipOpenDrop,
                float upperLipBiteForward,
                float lowerLipBiteForward,
                float outerUpperLipBiteDown,
                float outerLowerLipBiteUp,
                float frontRestAperture,
                float frontOpenAperture,
                float frontBiteAperture,
                float frontBiteClosurePercent,
                float frontUpperBiteDown,
                float frontLowerBiteUp,
                int bodyImpactVertexCount,
                float bodyImpactForwardExpansion,
                float bodyImpactSideExpansion,
                int upperRootVertexCount,
                int lowerRootVertexCount,
                int outerUpperRootVertexCount,
                int outerLowerRootVertexCount,
                int innerMouthVertexCount,
                string motionTargetPath,
                string sourceSha256)
            {
                VertexCount = vertexCount;
                CycleSeconds = cycleSeconds;
                RestAperture = restAperture;
                OpenAperture = openAperture;
                BiteAperture = biteAperture;
                WideOpenPercent = wideOpenPercent;
                BiteClosurePercent = biteClosurePercent;
                ForwardLeanDistance = forwardLeanDistance;
                ImpactLungeDistance = impactLungeDistance;
                WorldGroundDelta = worldGroundDelta;
                ModelVerticalTravel = modelVerticalTravel;
                ModelLateralTravel = modelLateralTravel;
                ModelRotationTravel = modelRotationTravel;
                ModelForwardPositionTravel = modelForwardPositionTravel;
                UpperLipOpenLift = upperLipOpenLift;
                LowerLipOpenDrop = lowerLipOpenDrop;
                UpperLipBiteForward = upperLipBiteForward;
                LowerLipBiteForward = lowerLipBiteForward;
                OuterUpperLipBiteDown = outerUpperLipBiteDown;
                OuterLowerLipBiteUp = outerLowerLipBiteUp;
                FrontRestAperture = frontRestAperture;
                FrontOpenAperture = frontOpenAperture;
                FrontBiteAperture = frontBiteAperture;
                FrontBiteClosurePercent = frontBiteClosurePercent;
                FrontUpperBiteDown = frontUpperBiteDown;
                FrontLowerBiteUp = frontLowerBiteUp;
                BodyImpactVertexCount = bodyImpactVertexCount;
                BodyImpactForwardExpansion = bodyImpactForwardExpansion;
                BodyImpactSideExpansion = bodyImpactSideExpansion;
                UpperRootVertexCount = upperRootVertexCount;
                LowerRootVertexCount = lowerRootVertexCount;
                OuterUpperRootVertexCount = outerUpperRootVertexCount;
                OuterLowerRootVertexCount = outerLowerRootVertexCount;
                InnerMouthVertexCount = innerMouthVertexCount;
                MotionTargetPath = motionTargetPath;
                SourceSha256 = sourceSha256;
            }

            public int VertexCount { get; }
            public float CycleSeconds { get; }
            public float RestAperture { get; }
            public float OpenAperture { get; }
            public float BiteAperture { get; }
            public float WideOpenPercent { get; }
            public float BiteClosurePercent { get; }
            public float ForwardLeanDistance { get; }
            public float ImpactLungeDistance { get; }
            public float WorldGroundDelta { get; }
            public float ModelVerticalTravel { get; }
            public float ModelLateralTravel { get; }
            public float ModelRotationTravel { get; }
            public float ModelForwardPositionTravel { get; }
            public float UpperLipOpenLift { get; }
            public float LowerLipOpenDrop { get; }
            public float UpperLipBiteForward { get; }
            public float LowerLipBiteForward { get; }
            public float OuterUpperLipBiteDown { get; }
            public float OuterLowerLipBiteUp { get; }
            public float FrontRestAperture { get; }
            public float FrontOpenAperture { get; }
            public float FrontBiteAperture { get; }
            public float FrontBiteClosurePercent { get; }
            public float FrontUpperBiteDown { get; }
            public float FrontLowerBiteUp { get; }
            public int BodyImpactVertexCount { get; }
            public float BodyImpactForwardExpansion { get; }
            public float BodyImpactSideExpansion { get; }
            public int UpperRootVertexCount { get; }
            public int LowerRootVertexCount { get; }
            public int OuterUpperRootVertexCount { get; }
            public int OuterLowerRootVertexCount { get; }
            public int InnerMouthVertexCount { get; }
            public string MotionTargetPath { get; }
            public string SourceSha256 { get; }
        }
    }
}
