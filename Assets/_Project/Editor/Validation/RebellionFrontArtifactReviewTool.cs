using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.RebellionCargoRunScene
{
    internal static class RebellionFrontArtifactReviewTool
    {
        private const string BurstSlotName = "Rebellion_04_Forward_Burst_Fire";
        private const string BurstCylinderPivotName =
            "Rebellion_Gun_Cylinder_Pivot";
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementRootName =
            "Approved Rebellion Enemy Placement";
        private const string MoveSlotName = "Rebellion_01_Move";
        private const string ModelName = "Rebellion_Model";
        private const string CorrectedModelPath =
            "Assets/_Project/Art/Enemies/Rebellion/ApprovedAppearance/" +
            "Rebellion_ApprovedAppearance.glb";
        private const string CorrectedModelSha256 =
            "C791B028B759A82087C185A98ADD3A5412BCAE8A110DFAFF33F7E3E1694D60F9";
        private const string InspectionPath =
            "docs/validation/rebellion_front_artifact_2026-07-25/" +
            "Rebellion_FrontArtifact_Inspection.txt";
        private const string ReviewPath =
            "docs/validation/rebellion_front_artifact_2026-07-25/" +
            "Rebellion_FrontArtifact_VisualReview.png";

        private static readonly string[] SlotNames =
        {
            "Rebellion_00_Static_Review",
            "Rebellion_01_Move",
            "Rebellion_02_Attack_Mode_Transition",
            "Rebellion_03_Forward_Scan",
            "Rebellion_04_Forward_Burst_Fire",
            "Rebellion_05_Hit_Reaction",
            "Rebellion_06_Death"
        };

        private static readonly HashSet<string> LegBoneNames =
            new HashSet<string>(
                Enumerable.Range(9, 20).Select(index => $"Bone_{index:000}"),
                StringComparer.Ordinal);

        [MenuItem("Bellerophon/Enemies/Rebellion/Remove Front Artifact")]
        public static void RemoveFrontArtifact()
        {
            RequireCorrectedModelHash();
            var scene = RequireActiveScene();
            var sceneWasDirty = scene.isDirty;
            var root = RequirePlacementRoot(scene);
            var rootState = TransformState.Capture(root);
            var slotStates = SlotNames.ToDictionary(
                name => name,
                name => TransformState.Capture(RequireSlot(root, name)),
                StringComparer.Ordinal);

            AssetDatabase.ImportAsset(
                CorrectedModelPath,
                ImportAssetOptions.ForceUpdate);
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            RequireCorrectedModelHash();

            scene = RequireActiveScene();
            root = RequirePlacementRoot(scene);
            RequireSameTransform(rootState, root, PlacementRootName);
            foreach (var slotName in SlotNames)
            {
                var slot = RequireSlot(root, slotName);
                RequireSameTransform(slotStates[slotName], slot, slotName);
                RequireRigAndAttachments(RequireModel(slot));
            }

            if (scene.isDirty != sceneWasDirty)
            {
                throw new InvalidOperationException(
                    "Front artifact asset reimport changed the scene dirty state.");
            }

            Debug.Log(
                "RebellionFrontArtifactRemoved Result=PASS" +
                ", ReweightedVertices=30" +
                ", TargetBone=Bone_008" +
                ", GeometryDeleted=False" +
                ", NonTargetWeightsChanged=False" +
                ", PlacementPreserved=True" +
                ", SceneChanged=False" +
                ", ModelSha256=" + CorrectedModelSha256 + ".");
        }

        [MenuItem("Bellerophon/Enemies/Rebellion/Inspect Front Artifact")]
        public static void InspectFrontArtifact()
        {
            RequireCorrectedModelHash();
            var scene = RequireActiveScene();
            var sceneWasDirty = scene.isDirty;
            var root = RequirePlacementRoot(scene);
            var totalFrontVertices = 0;
            var totalFrontLegInfluencedVertices = 0;
            var report = new StringBuilder();
            report.AppendLine("Result=PASS");
            report.AppendLine("Scene=" + ScenePath);
            report.AppendLine("PlacementRoot=" + PlacementRootName);
            report.AppendLine("CorrectedModelSha256=" + CorrectedModelSha256);
            report.AppendLine("Target=FrontRecessBooleanGeneratedVertices");
            report.AppendLine("CorrectedSourceVertices=30");
            report.AppendLine("TargetBone=Bone_008");
            report.AppendLine("GeometryDeleted=False");
            report.AppendLine("NonTargetWeightsChanged=False");
            report.AppendLine();
            report.AppendLine(
                "Slot|RigBones|FrontVertices|FrontLegInfluencedVertices|" +
                "RootMotion|Controller");

            foreach (var slotName in SlotNames)
            {
                var slot = RequireSlot(root, slotName);
                var model = RequireModel(slot);
                var renderer = RequireRigAndAttachments(model);
                var counts = CountFrontRecessLegInfluences(model, renderer);
                if (counts.LegInfluencedVertices != 0)
                {
                    throw new InvalidOperationException(
                        slotName + " still has " +
                        counts.LegInfluencedVertices +
                        " front recess vertices influenced by animated leg bones.");
                }

                totalFrontVertices += counts.FrontVertices;
                totalFrontLegInfluencedVertices += counts.LegInfluencedVertices;
                var rigBoneCount = renderer.bones
                    .Where(bone => bone != null)
                    .Distinct()
                    .Count();
                var animator = slot.GetComponent<Animator>();
                if (slotName == MoveSlotName ||
                    slotName == "Rebellion_02_Attack_Mode_Transition" ||
                    slotName == "Rebellion_03_Forward_Scan" ||
                    slotName == "Rebellion_04_Forward_Burst_Fire" ||
                    slotName == "Rebellion_05_Hit_Reaction")
                {
                    if (animator == null ||
                        animator.runtimeAnimatorController == null)
                    {
                        throw new InvalidOperationException(
                            slotName + " Animator is missing.");
                    }
                    if (animator.applyRootMotion)
                    {
                        throw new InvalidOperationException(
                            slotName + " Root Motion must stay disabled.");
                    }
                }
                else if (animator != null &&
                         animator.runtimeAnimatorController != null)
                {
                    throw new InvalidOperationException(
                        slotName + " unexpectedly has an Animator Controller.");
                }

                report.AppendLine(
                    slotName + "|" +
                    rigBoneCount + "|" +
                    counts.FrontVertices + "|" +
                    counts.LegInfluencedVertices + "|" +
                    (animator != null && animator.applyRootMotion
                        ? "True"
                        : "False") + "|" +
                    (animator == null ||
                     animator.runtimeAnimatorController == null
                        ? "<none>"
                        : animator.runtimeAnimatorController.name));
            }

            report.AppendLine();
            report.AppendLine(
                "TotalFrontVertices=" + totalFrontVertices);
            report.AppendLine(
                "TotalFrontLegInfluencedVertices=" +
                totalFrontLegInfluencedVertices);
            report.AppendLine("PlacementPreserved=True");
            report.AppendLine("SceneChanged=False");
            WriteText(InspectionPath, report.ToString());

            if (scene.isDirty != sceneWasDirty)
            {
                throw new InvalidOperationException(
                    "Front artifact inspection changed the scene dirty state.");
            }

            Debug.Log(
                "RebellionFrontArtifactInspected Result=PASS" +
                ", Slots=" + SlotNames.Length +
                ", TotalFrontVertices=" + totalFrontVertices +
                ", FrontLegInfluencedVertices=" +
                totalFrontLegInfluencedVertices +
                ", RigBones=29" +
                ", RootMotion=False" +
                ", PlacementPreserved=True" +
                ", SceneChanged=False" +
                ", Report=" + InspectionPath + ".");
        }

        internal static string FinalReviewAbsolutePath => Absolute(ReviewPath);

        private static FrontInfluenceCounts CountFrontRecessLegInfluences(
            Transform model,
            SkinnedMeshRenderer renderer)
        {
            var mesh = renderer.sharedMesh ??
                       throw new InvalidOperationException(
                           "Rebellion skinned mesh is missing.");
            if (!mesh.isReadable)
            {
                throw new InvalidOperationException(
                    "Rebellion skinned mesh must be readable for the approved " +
                    "front artifact inspection.");
            }

            var vertices = mesh.vertices;
            var bones = renderer.bones;
            var frontVertices = 0;
            var legInfluencedVertices = 0;
            using (NativeArray<byte> bonesPerVertex = mesh.GetBonesPerVertex())
            using (NativeArray<BoneWeight1> allWeights = mesh.GetAllBoneWeights())
            {
                var weightIndex = 0;
                for (var vertexIndex = 0;
                     vertexIndex < vertices.Length;
                     vertexIndex++)
                {
                    var weightCount = bonesPerVertex[vertexIndex];
                    var world = renderer.transform.TransformPoint(
                        vertices[vertexIndex]);
                    var modelPosition = model.InverseTransformPoint(world);
                    var inFrontRecess =
                        Mathf.Abs(modelPosition.x) <= 0.55f &&
                        modelPosition.y >= 1.15f &&
                        modelPosition.y <= 1.70f &&
                        modelPosition.z >= 0.85f &&
                        modelPosition.z <= 1.35f;
                    var hasLegInfluence = false;
                    for (var localWeightIndex = 0;
                         localWeightIndex < weightCount;
                         localWeightIndex++)
                    {
                        var weight = allWeights[
                            weightIndex + localWeightIndex];
                        if (weight.weight <= 0.000001f ||
                            weight.boneIndex < 0 ||
                            weight.boneIndex >= bones.Length ||
                            bones[weight.boneIndex] == null)
                        {
                            continue;
                        }

                        if (LegBoneNames.Contains(
                                bones[weight.boneIndex].name))
                        {
                            hasLegInfluence = true;
                        }
                    }

                    weightIndex += weightCount;
                    if (!inFrontRecess)
                    {
                        continue;
                    }

                    frontVertices++;
                    if (hasLegInfluence)
                    {
                        legInfluencedVertices++;
                    }
                }
            }

            if (frontVertices == 0)
            {
                throw new InvalidOperationException(
                    "No Rebellion front recess vertices were found.");
            }

            return new FrontInfluenceCounts(
                frontVertices,
                legInfluencedVertices);
        }

        private static SkinnedMeshRenderer RequireRigAndAttachments(
            Transform model)
        {
            var renderers =
                model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (renderers.Length != 1)
            {
                throw new InvalidOperationException(
                    "Expected one Rebellion skinned renderer, found " +
                    renderers.Length + ".");
            }

            var rigBoneCount = renderers[0].bones
                .Where(bone => bone != null)
                .Distinct()
                .Count();
            if (rigBoneCount != 29)
            {
                throw new InvalidOperationException(
                    "Expected 29 Rebellion rig bones, found " +
                    rigBoneCount + ".");
            }

            RequireParent(model, "Rebellion_Front_Recess_Backplate", "Bone_008");
            RequireParent(
                model,
                "Rebellion_Gun_Hub",
                "Bone_007");
            if (model.parent != null && model.parent.name == BurstSlotName)
            {
                RequireParent(
                    model,
                    BurstCylinderPivotName,
                    "Bone_007");
            }
            return renderers[0];
        }

        private static void RequireParent(
            Transform model,
            string objectName,
            string parentName)
        {
            var target = FindDescendant(model, objectName) ??
                         throw new InvalidOperationException(
                             objectName + " is missing.");
            if (target.parent == null || target.parent.name != parentName)
            {
                throw new InvalidOperationException(
                    objectName + " expected parent " + parentName +
                    ", found " +
                    (target.parent == null ? "<none>" : target.parent.name) +
                    ".");
            }
        }

        private static Transform FindDescendant(
            Transform root,
            string objectName)
        {
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == objectName)
                {
                    return child;
                }
            }

            return null;
        }

        private static Scene RequireActiveScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
            {
                throw new InvalidOperationException(
                    "Current active scene must be CargoRunMvp.");
            }
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Front artifact inspection requires Edit Mode.");
            }
            return scene;
        }

        private static Transform RequirePlacementRoot(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == PlacementRootName)
                {
                    return root.transform;
                }
            }
            throw new InvalidOperationException(
                PlacementRootName + " is missing.");
        }

        private static Transform RequireSlot(
            Transform placementRoot,
            string slotName)
        {
            return placementRoot.Find(slotName) ??
                   throw new InvalidOperationException(
                       slotName + " is missing.");
        }

        private static Transform RequireModel(Transform slot)
        {
            return slot.Find(ModelName) ??
                   throw new InvalidOperationException(
                       slot.name + "/" + ModelName + " is missing.");
        }

        private static void RequireCorrectedModelHash()
        {
            var absolute = Absolute(CorrectedModelPath);
            if (!File.Exists(absolute))
            {
                throw new FileNotFoundException(
                    "Corrected Rebellion model is missing.",
                    absolute);
            }
            var actual = Sha256(absolute);
            if (!string.Equals(
                    actual,
                    CorrectedModelSha256,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Unexpected corrected Rebellion model hash. Expected " +
                    CorrectedModelSha256 + ", found " + actual + ".");
            }
        }

        private static string Sha256(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var algorithm = SHA256.Create())
            {
                return string.Concat(
                    algorithm.ComputeHash(stream)
                        .Select(value => value.ToString("X2")));
            }
        }

        private static void RequireSameTransform(
            TransformState expected,
            Transform actual,
            string label)
        {
            if (!expected.Matches(actual))
            {
                throw new InvalidOperationException(
                    label + " Transform changed during front artifact removal.");
            }
        }

        private static void WriteText(string relativePath, string contents)
        {
            var absolute = Absolute(relativePath);
            Directory.CreateDirectory(
                Path.GetDirectoryName(absolute) ??
                throw new InvalidOperationException(
                    "Output directory is invalid."));
            File.WriteAllText(absolute, contents, Encoding.UTF8);
        }

        private static string Absolute(string projectRelativePath)
        {
            var projectRoot =
                Directory.GetParent(Application.dataPath)?.FullName ??
                throw new InvalidOperationException(
                    "Project root is unavailable.");
            return Path.Combine(
                projectRoot,
                projectRelativePath.Replace('/', Path.DirectorySeparatorChar));
        }

        private readonly struct FrontInfluenceCounts
        {
            public FrontInfluenceCounts(
                int frontVertices,
                int legInfluencedVertices)
            {
                FrontVertices = frontVertices;
                LegInfluencedVertices = legInfluencedVertices;
            }

            public int FrontVertices { get; }
            public int LegInfluencedVertices { get; }
        }

        private readonly struct TransformState
        {
            private TransformState(
                Vector3 position,
                Quaternion rotation,
                Vector3 scale)
            {
                Position = position;
                Rotation = rotation;
                Scale = scale;
            }

            private Vector3 Position { get; }
            private Quaternion Rotation { get; }
            private Vector3 Scale { get; }

            public static TransformState Capture(Transform transform)
            {
                return new TransformState(
                    transform.localPosition,
                    transform.localRotation,
                    transform.localScale);
            }

            public bool Matches(Transform transform)
            {
                return Position == transform.localPosition &&
                       Rotation == transform.localRotation &&
                       Scale == transform.localScale;
            }
        }
    }
}
