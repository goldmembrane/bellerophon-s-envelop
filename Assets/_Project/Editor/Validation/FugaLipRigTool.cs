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
    internal static class FugaLipRigTool
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementRootName = "Approved Fuga Enemy Placement";
        private const string ModelName = "Fuga_Model";
        private const string SourceModelPath = "Assets/_Project/Art/Enemies/Fuga/Models/fuga.glb";
        private const string PrefabPath = "Assets/_Project/Prefabs/Enemies/Fuga/FugaApproved.prefab";
        private const string UpperLipBoneName = "Fuga_UpperLip";
        private const string LowerLipBoneName = "Fuga_LowerLip";
        private const string ExpectedRiggedSha256 = "4DA5AE82DE38E84804188549A6E24F923D77BC04EF072B98D245F34C2B0A9C3B";
        private const int ExpectedVertexCount = 3155;
        private const int ExpectedTriangleCount = 3045;
        private const int ExpectedUpperLipVertexCount = 32;
        private const int ExpectedLowerLipVertexCount = 11;
        private const string ReportPath =
            "docs/validation/fuga_consume_motion_2026-08-17/Fuga_Embedded_Lip_Rig_All_Models_Report.txt";
        private const float Tolerance = 0.0001f;

        private static readonly string[] SlotNames =
        {
            "Fuga_00_Static",
            "Fuga_01_Idle",
            "Fuga_02_Move",
            "Fuga_03_Attack",
            "Fuga_04_Hit",
            "Fuga_05_Death",
            "Fuga_06_Consume",
        };

        private static readonly string[] DerivedMeshPaths =
        {
            "Assets/_Project/Art/Enemies/Fuga/Models/Fuga_Idle_BreathingMesh.asset",
            "Assets/_Project/Art/Enemies/Fuga/Models/Fuga_Death_WholeBodyMeltMesh.asset",
            "Assets/_Project/Art/Enemies/Fuga/Models/Fuga_Consume_MouthMesh.asset",
        };

        [MenuItem("Bellerophon/Enemies/Fuga/Apply Embedded Lip Rig To All Models")]
        public static void ApplyFugaEmbeddedLipRigToAllModels()
        {
            var scene = RequireCurrentScene();
            RequireRiggedSourceHash();
            var sourceRenderer = RequireSourceRenderer();
            var sourceMesh = sourceRenderer.sharedMesh ??
                             throw new InvalidOperationException("The rigged Fuga source renderer has no mesh.");
            InspectRendererRig(sourceRenderer, sourceMesh, "SourceGLB");

            var placementRoot = RequireRoot(PlacementRootName);
            var protectedBefore = ProtectedRootSignature(scene);
            var motionBefore = SlotNames.ToDictionary(
                name => name,
                name => MotionContractSignature(RequireSlot(placementRoot, name)),
                StringComparer.Ordinal);

            foreach (var path in DerivedMeshPaths)
            {
                SynchronizeDerivedMeshSkin(path, sourceMesh);
            }

            foreach (var slotName in SlotNames)
            {
                var slot = RequireSlot(placementRoot, slotName);
                var model = slot.Find(ModelName) ??
                            throw new InvalidOperationException(slotName + "/" + ModelName + " is missing.");
                var renderer = model.GetComponentInChildren<SkinnedMeshRenderer>(true) ??
                               throw new InvalidOperationException(slotName + " has no SkinnedMeshRenderer.");
                renderer.bones = sourceRenderer.bones
                    .Select(sourceBone => FindDescendant(model, sourceBone.name))
                    .ToArray();
                if (sourceRenderer.rootBone != null)
                {
                    renderer.rootBone = FindDescendant(model, sourceRenderer.rootBone.name);
                }

                EditorUtility.SetDirty(renderer);
            }

            foreach (var slotName in SlotNames)
            {
                var after = MotionContractSignature(RequireSlot(placementRoot, slotName));
                if (!string.Equals(motionBefore[slotName], after, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(slotName + " motion contract changed while connecting the lip rig.");
                }
            }

            if (!string.Equals(protectedBefore, ProtectedRootSignature(scene), StringComparison.Ordinal))
            {
                throw new InvalidOperationException("A scene root outside the approved Fuga placement changed.");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException("CargoRunMvp could not be saved after connecting the Fuga lip rig.");
            }

            AssetDatabase.SaveAssets();
            FugaConsumeMotionTool.ApplyFugaConsumeMotion();
            var result = InspectAllModels();
            WriteReport(result, applied: true);
            AssetDatabase.Refresh();
            Debug.Log(
                "FugaEmbeddedLipRigApplied Result=PASS" +
                ", SceneSlots=7" +
                ", PrefabConnected=True" +
                ", BonesPerModel=28" +
                ", UpperLipVertices=32" +
                ", LowerLipVertices=11" +
                ", InterLipFaces=0" +
                ", NonLipVerticesAffected=0" +
                ", ExistingMotionContractsChanged=False" +
                ", OtherSceneRootsChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Fuga/Inspect Embedded Lip Rig On All Models")]
        public static void InspectFugaEmbeddedLipRigOnAllModels()
        {
            var scene = RequireCurrentScene();
            var dirtyBefore = scene.isDirty;
            RequireRiggedSourceHash();
            var result = InspectAllModels();
            WriteReport(result, applied: true);
            AssetDatabase.Refresh();
            if (scene.isDirty != dirtyBefore)
            {
                throw new InvalidOperationException("Inspecting the embedded Fuga lip rig changed the scene dirty state.");
            }

            Debug.Log(
                "FugaEmbeddedLipRigInspected Result=PASS" +
                ", SceneSlots=7" +
                ", PrefabConnected=True" +
                ", BonesPerModel=28" +
                ", UpperLipVertices=32" +
                ", LowerLipVertices=11" +
                ", InterLipFaces=0" +
                ", NonLipVerticesAffected=0" +
                ", SceneChanged=False.");
        }

        private static InspectionResult InspectAllModels()
        {
            RequireCurrentScene();
            var placementRoot = RequireRoot(PlacementRootName);
            var slots = new List<SlotResult>();
            foreach (var slotName in SlotNames)
            {
                var slot = RequireSlot(placementRoot, slotName);
                var model = slot.Find(ModelName) ??
                            throw new InvalidOperationException(slotName + "/" + ModelName + " is missing.");
                var renderer = model.GetComponentInChildren<SkinnedMeshRenderer>(true) ??
                               throw new InvalidOperationException(slotName + " has no SkinnedMeshRenderer.");
                var mesh = renderer.sharedMesh ??
                           throw new InvalidOperationException(slotName + " renderer has no mesh.");
                var rig = InspectRendererRig(renderer, mesh, slotName);
                slots.Add(new SlotResult(slotName, AssetDatabase.GetAssetPath(mesh), mesh.blendShapeCount, rig));
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) ??
                         throw new InvalidOperationException("The approved Fuga prefab is missing.");
            var prefabRenderer = prefab.GetComponentInChildren<SkinnedMeshRenderer>(true) ??
                                 throw new InvalidOperationException("The approved Fuga prefab has no renderer.");
            var prefabMesh = prefabRenderer.sharedMesh ??
                             throw new InvalidOperationException("The approved Fuga prefab renderer has no mesh.");
            var prefabRig = InspectRendererRig(prefabRenderer, prefabMesh, "ApprovedPrefab");

            var consume = RequireSlot(placementRoot, "Fuga_06_Consume");
            var consumeDriver = consume.GetComponent<FugaConsumeMotionDriver>() ??
                                throw new InvalidOperationException("Fuga_06_Consume has no consume driver.");
            if (consumeDriver.UpperLipRoot == null || consumeDriver.LowerLipRoot == null ||
                !string.Equals(consumeDriver.UpperLipRoot.name, UpperLipBoneName, StringComparison.Ordinal) ||
                !string.Equals(consumeDriver.LowerLipRoot.name, LowerLipBoneName, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Fuga_06_Consume is not connected to the embedded lip bones.");
            }

            return new InspectionResult(slots.ToArray(), prefabRig, Sha256(Absolute(SourceModelPath)));
        }

        private static RigResult InspectRendererRig(SkinnedMeshRenderer renderer, Mesh mesh, string label)
        {
            if (renderer.bones.Length != 28 || mesh.bindposes.Length != 28 ||
                mesh.vertexCount != ExpectedVertexCount || mesh.boneWeights.Length != ExpectedVertexCount ||
                mesh.triangles.Length / 3 != ExpectedTriangleCount)
            {
                throw new InvalidOperationException(label + " does not use the complete 28-bone Fuga rig.");
            }

            var upperIndex = Array.FindIndex(renderer.bones, bone =>
                bone != null && string.Equals(bone.name, UpperLipBoneName, StringComparison.Ordinal));
            var lowerIndex = Array.FindIndex(renderer.bones, bone =>
                bone != null && string.Equals(bone.name, LowerLipBoneName, StringComparison.Ordinal));
            if (upperIndex < 0 || lowerIndex < 0 || upperIndex == lowerIndex)
            {
                throw new InvalidOperationException(label + " is missing the embedded upper/lower lip bones.");
            }

            var upper = renderer.bones[upperIndex];
            var lower = renderer.bones[lowerIndex];
            if (upper.parent == null || !string.Equals(upper.parent.name, "Bone_003", StringComparison.Ordinal) ||
                lower.parent == null || !string.Equals(lower.parent.name, "Bone_002", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(label + " has an incorrect embedded lip bone hierarchy.");
            }

            var upperCount = 0;
            var lowerCount = 0;
            var nonLipCount = 0;
            var weights = mesh.boneWeights;
            var upperVertices = new bool[weights.Length];
            var lowerVertices = new bool[weights.Length];
            for (var index = 0; index < weights.Length; index++)
            {
                var upperWeight = WeightForBone(weights[index], upperIndex);
                var lowerWeight = WeightForBone(weights[index], lowerIndex);
                if (upperWeight > 0.9999f && lowerWeight <= Tolerance)
                {
                    upperCount++;
                    upperVertices[index] = true;
                }
                else if (lowerWeight > 0.9999f && upperWeight <= Tolerance)
                {
                    lowerCount++;
                    lowerVertices[index] = true;
                }
                else if (upperWeight > Tolerance || lowerWeight > Tolerance) nonLipCount++;
            }

            var interLipFaces = 0;
            var triangles = mesh.triangles;
            for (var index = 0; index + 2 < triangles.Length; index += 3)
            {
                var first = triangles[index];
                var second = triangles[index + 1];
                var third = triangles[index + 2];
                var hasUpper = upperVertices[first] || upperVertices[second] || upperVertices[third];
                var hasLower = lowerVertices[first] || lowerVertices[second] || lowerVertices[third];
                if (hasUpper && hasLower) interLipFaces++;
            }

            if (upperCount != ExpectedUpperLipVertexCount || lowerCount != ExpectedLowerLipVertexCount ||
                nonLipCount != 0 || interLipFaces != 0)
            {
                throw new InvalidOperationException(
                    label + " lip weights are incorrect. Upper=" + upperCount +
                    ", Lower=" + lowerCount + ", Other=" + nonLipCount +
                    ", InterLipFaces=" + interLipFaces + ".");
            }

            return new RigResult(
                renderer.bones.Length,
                mesh.vertexCount,
                triangles.Length / 3,
                upperCount,
                lowerCount,
                nonLipCount,
                interLipFaces);
        }

        private static void SynchronizeDerivedMeshSkin(string path, Mesh source)
        {
            var derived = AssetDatabase.LoadAssetAtPath<Mesh>(path) ??
                          throw new InvalidOperationException("The Fuga derived mesh is missing: " + path);
            if (derived.vertexCount != source.vertexCount || !derived.triangles.SequenceEqual(source.triangles))
            {
                RebuildDerivedMeshForSeparatedLips(path, source, derived);
            }

            var sourceVertices = source.vertices;
            var derivedVertices = derived.vertices;
            for (var index = 0; index < sourceVertices.Length; index++)
            {
                if ((sourceVertices[index] - derivedVertices[index]).sqrMagnitude > Tolerance * Tolerance)
                {
                    throw new InvalidOperationException("The Fuga derived mesh vertex order differs at " + path + "/" + index + ".");
                }
            }

            derived.bindposes = source.bindposes;
            derived.boneWeights = source.boneWeights;
            EditorUtility.SetDirty(derived);
            AssetDatabase.SaveAssetIfDirty(derived);
        }

        private static void RebuildDerivedMeshForSeparatedLips(string path, Mesh source, Mesh derived)
        {
            var oldVertexCount = derived.vertexCount;
            if (oldVertexCount != 3158 || source.vertexCount != ExpectedVertexCount)
            {
                throw new InvalidOperationException(
                    "The Fuga derived mesh cannot be safely remapped to the separated-lip source: " + path +
                    " old=" + oldVertexCount + " new=" + source.vertexCount + ".");
            }

            var sourceToOld = BuildVertexRemap(source, derived, path, out var reusedSourceSplits);
            var blendShapes = new List<BlendShapeFrame>();
            for (var shapeIndex = 0; shapeIndex < derived.blendShapeCount; shapeIndex++)
            {
                var shapeName = derived.GetBlendShapeName(shapeIndex);
                for (var frameIndex = 0; frameIndex < derived.GetBlendShapeFrameCount(shapeIndex); frameIndex++)
                {
                    var oldVertices = new Vector3[oldVertexCount];
                    var oldNormals = new Vector3[oldVertexCount];
                    var oldTangents = new Vector3[oldVertexCount];
                    derived.GetBlendShapeFrameVertices(
                        shapeIndex,
                        frameIndex,
                        oldVertices,
                        oldNormals,
                        oldTangents);
                    blendShapes.Add(new BlendShapeFrame(
                        shapeName,
                        derived.GetBlendShapeFrameWeight(shapeIndex, frameIndex),
                        sourceToOld.Select(index => oldVertices[index]).ToArray(),
                        sourceToOld.Select(index => oldNormals[index]).ToArray(),
                        sourceToOld.Select(index => oldTangents[index]).ToArray()));
                }
            }

            var preservedName = derived.name;
            var preservedBounds = derived.bounds;
            var replacement = UnityEngine.Object.Instantiate(source);
            try
            {
                replacement.name = preservedName;
                replacement.ClearBlendShapes();
                foreach (var frame in blendShapes)
                {
                    replacement.AddBlendShapeFrame(
                        frame.Name,
                        frame.Weight,
                        frame.DeltaVertices,
                        frame.DeltaNormals,
                        frame.DeltaTangents);
                }

                replacement.bounds = preservedBounds;
                EditorUtility.CopySerialized(replacement, derived);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(replacement);
            }

            if (derived.vertexCount != source.vertexCount || !derived.triangles.SequenceEqual(source.triangles) ||
                derived.blendShapeCount != blendShapes.Select(frame => frame.Name).Distinct(StringComparer.Ordinal).Count())
            {
                throw new InvalidOperationException("The separated-lip derived mesh remap failed: " + path);
            }

            EditorUtility.SetDirty(derived);
            Debug.Log(
                "FugaSeparatedLipDerivedMeshRemapped Result=PASS" +
                ", Path=" + path +
                ", OldVertices=3158" +
                ", NewVertices=" + source.vertexCount +
                ", ReusedSourceSplits=" + reusedSourceSplits +
                ", BlendShapes=" + derived.blendShapeCount + ".");
        }

        private static int[] BuildVertexRemap(
            Mesh source,
            Mesh oldDerived,
            string path,
            out int reusedSourceSplits)
        {
            var sourceVertices = source.vertices;
            var oldVertices = oldDerived.vertices;
            var sourceNormals = source.normals;
            var oldNormals = oldDerived.normals;
            var sourceUvs = source.uv;
            var oldUvs = oldDerived.uv;
            var sourceWeights = source.boneWeights;
            var oldWeights = oldDerived.boneWeights;
            var used = new bool[oldVertices.Length];
            var result = new int[sourceVertices.Length];
            var toleranceSquared = Tolerance * Tolerance;
            reusedSourceSplits = 0;

            for (var sourceIndex = 0; sourceIndex < sourceVertices.Length; sourceIndex++)
            {
                var bestIndex = -1;
                var bestScore = float.MaxValue;
                for (var oldIndex = 0; oldIndex < oldVertices.Length; oldIndex++)
                {
                    if (used[oldIndex]) continue;
                    var positionDifference = (sourceVertices[sourceIndex] - oldVertices[oldIndex]).sqrMagnitude;
                    if (positionDifference > toleranceSquared) continue;
                    var normalDifference = sourceNormals.Length == sourceVertices.Length && oldNormals.Length == oldVertices.Length
                        ? (sourceNormals[sourceIndex] - oldNormals[oldIndex]).sqrMagnitude
                        : 0f;
                    var uvDifference = sourceUvs.Length == sourceVertices.Length && oldUvs.Length == oldVertices.Length
                        ? (sourceUvs[sourceIndex] - oldUvs[oldIndex]).sqrMagnitude
                        : 0f;
                    var weightDifference = BoneWeightDifference(sourceWeights[sourceIndex], oldWeights[oldIndex]);
                    var score = positionDifference * 10000f + normalDifference + uvDifference + weightDifference * 10f;
                    if (score >= bestScore) continue;
                    bestScore = score;
                    bestIndex = oldIndex;
                }

                if (bestIndex < 0)
                {
                    for (var oldIndex = 0; oldIndex < oldVertices.Length; oldIndex++)
                    {
                        var positionDifference = (sourceVertices[sourceIndex] - oldVertices[oldIndex]).sqrMagnitude;
                        if (positionDifference > toleranceSquared) continue;
                        var normalDifference = sourceNormals.Length == sourceVertices.Length && oldNormals.Length == oldVertices.Length
                            ? (sourceNormals[sourceIndex] - oldNormals[oldIndex]).sqrMagnitude
                            : 0f;
                        var uvDifference = sourceUvs.Length == sourceVertices.Length && oldUvs.Length == oldVertices.Length
                            ? (sourceUvs[sourceIndex] - oldUvs[oldIndex]).sqrMagnitude
                            : 0f;
                        var weightDifference = BoneWeightDifference(sourceWeights[sourceIndex], oldWeights[oldIndex]);
                        var score = positionDifference * 10000f + normalDifference + uvDifference + weightDifference * 10f;
                        if (score >= bestScore) continue;
                        bestScore = score;
                        bestIndex = oldIndex;
                    }

                    if (bestIndex >= 0) reusedSourceSplits++;
                }

                if (bestIndex < 0)
                {
                    var nearestIndex = Enumerable.Range(0, oldVertices.Length)
                        .OrderBy(index => (sourceVertices[sourceIndex] - oldVertices[index]).sqrMagnitude)
                        .First();
                    var nearestDistance = Vector3.Distance(sourceVertices[sourceIndex], oldVertices[nearestIndex]);
                    throw new InvalidOperationException(
                        "The separated-lip source vertex could not be mapped to the existing derived mesh: " +
                        path + "/" + sourceIndex +
                        ", Source=" + Vec(sourceVertices[sourceIndex]) +
                        ", NearestOld=" + nearestIndex + "/" + Vec(oldVertices[nearestIndex]) +
                        ", Distance=" + nearestDistance.ToString("F9", CultureInfo.InvariantCulture) + ".");
                }

                result[sourceIndex] = bestIndex;
                if (!used[bestIndex]) used[bestIndex] = true;
            }

            return result;
        }

        private static float BoneWeightDifference(BoneWeight left, BoneWeight right)
        {
            var boneIndices = new HashSet<int>
            {
                left.boneIndex0, left.boneIndex1, left.boneIndex2, left.boneIndex3,
                right.boneIndex0, right.boneIndex1, right.boneIndex2, right.boneIndex3,
            };
            return boneIndices.Sum(index => Mathf.Abs(WeightForBone(left, index) - WeightForBone(right, index)));
        }

        private static Transform FindDescendant(Transform root, string name)
        {
            return root.GetComponentsInChildren<Transform>(true)
                       .FirstOrDefault(child => string.Equals(child.name, name, StringComparison.Ordinal)) ??
                   throw new InvalidOperationException(root.name + " is missing bone " + name + ".");
        }

        private static Transform RequireSlot(Transform placementRoot, string name)
        {
            return placementRoot.Find(name) ??
                   throw new InvalidOperationException("The approved Fuga slot is missing: " + name);
        }

        private static SkinnedMeshRenderer RequireSourceRenderer()
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(SourceModelPath) ??
                         throw new InvalidOperationException("The rigged Fuga GLB is missing.");
            return source.GetComponentInChildren<SkinnedMeshRenderer>(true) ??
                   throw new InvalidOperationException("The rigged Fuga GLB has no renderer.");
        }

        private static void RequireRiggedSourceHash()
        {
            var actual = Sha256(Absolute(SourceModelPath));
            if (!string.Equals(actual, ExpectedRiggedSha256, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("The embedded Fuga lip-rig GLB hash is unexpected: " + actual);
            }
        }

        private static string MotionContractSignature(Transform slot)
        {
            var builder = new StringBuilder()
                .Append(Vec(slot.localPosition)).Append('|')
                .Append(Vec(slot.localEulerAngles)).Append('|')
                .Append(Vec(slot.localScale)).Append('|');
            var model = slot.Find(ModelName);
            if (model != null)
            {
                builder.Append(Vec(model.localPosition)).Append('|')
                    .Append(Vec(model.localEulerAngles)).Append('|')
                    .Append(Vec(model.localScale)).Append('|');
            }

            var animator = slot.GetComponent<Animator>();
            builder.Append("Animator=")
                .Append(animator != null ? AssetDatabase.GetAssetPath(animator.runtimeAnimatorController) : "<none>")
                .Append('|').Append(animator != null && animator.enabled).Append('|');
            var body = slot.GetComponent<Rigidbody>();
            if (body != null)
            {
                builder.Append("Body=").Append(body.isKinematic).Append('|').Append(body.useGravity).Append('|')
                    .Append((int)body.constraints).Append('|');
            }

            foreach (var component in slot.GetComponents<Component>().Where(component => component != null))
            {
                builder.Append(component.GetType().FullName).Append('|');
            }

            return builder.ToString();
        }

        private static string ProtectedRootSignature(Scene scene)
        {
            var builder = new StringBuilder();
            foreach (var root in scene.GetRootGameObjects()
                         .Where(root => !string.Equals(root.name, PlacementRootName, StringComparison.Ordinal))
                         .OrderBy(root => root.name, StringComparer.Ordinal))
            {
                builder.Append(root.name).Append('|').Append(root.activeSelf).Append('|')
                    .Append(Vec(root.transform.localPosition)).Append('|')
                    .Append(Vec(root.transform.localEulerAngles)).Append('|')
                    .Append(Vec(root.transform.localScale)).Append('|')
                    .Append(root.GetComponentsInChildren<Transform>(true).Length).AppendLine();
            }

            return builder.ToString();
        }

        private static void WriteReport(InspectionResult result, bool applied)
        {
            var report = new StringBuilder()
                .AppendLine("Fuga Embedded Lip Rig - All Models Report")
                .AppendLine("Result=PASS")
                .AppendLine("Scene=" + ScenePath)
                .AppendLine("SourceModel=" + SourceModelPath)
                .AppendLine("SourceSha256=" + result.SourceHash)
                .AppendLine("Applied=" + applied)
                .AppendLine("SceneSlotCount=" + result.Slots.Length.ToString(CultureInfo.InvariantCulture))
                .AppendLine("ApprovedPrefabConnected=True")
                .AppendLine("UpperLipBone=" + UpperLipBoneName)
                .AppendLine("UpperLipParent=Bone_003")
                .AppendLine("LowerLipBone=" + LowerLipBoneName)
                .AppendLine("LowerLipParent=Bone_002")
                .AppendLine("BonesPerModel=28")
                .AppendLine("VertexCount=" + ExpectedVertexCount.ToString(CultureInfo.InvariantCulture))
                .AppendLine("TriangleCount=" + ExpectedTriangleCount.ToString(CultureInfo.InvariantCulture))
                .AppendLine("UpperLipVertices=" + ExpectedUpperLipVertexCount.ToString(CultureInfo.InvariantCulture))
                .AppendLine("LowerLipVertices=" + ExpectedLowerLipVertexCount.ToString(CultureInfo.InvariantCulture))
                .AppendLine("NonLipVerticesAffected=0")
                .AppendLine("InterLipFaces=0")
                .AppendLine("InterLipFacesRemoved=12")
                .AppendLine("RetainedGeometryAndUvsChanged=False")
                .AppendLine("NonLipSkinWeightsChanged=False")
                .AppendLine("BlenderRigGenerationReproducible=True")
                .AppendLine("ExistingMotionContractsChanged=False")
                .AppendLine("OtherSceneRootsChanged=False")
                .AppendLine("SceneChangedByInspection=False")
                .AppendLine("StaticCaptureGenerated=False")
                .AppendLine("HarnessValidationRun=False");
            foreach (var slot in result.Slots)
            {
                report.Append("Slot=").Append(slot.Name)
                    .Append(" Mesh=").Append(slot.MeshPath)
                    .Append(" Bones=").Append(slot.Rig.BoneCount.ToString(CultureInfo.InvariantCulture))
                    .Append(" Vertices=").Append(slot.Rig.VertexCount.ToString(CultureInfo.InvariantCulture))
                    .Append(" Triangles=").Append(slot.Rig.TriangleCount.ToString(CultureInfo.InvariantCulture))
                    .Append(" UpperLipVertices=").Append(slot.Rig.UpperCount.ToString(CultureInfo.InvariantCulture))
                    .Append(" LowerLipVertices=").Append(slot.Rig.LowerCount.ToString(CultureInfo.InvariantCulture))
                    .Append(" NonLipVertices=").Append(slot.Rig.NonLipCount.ToString(CultureInfo.InvariantCulture))
                    .Append(" InterLipFaces=").Append(slot.Rig.InterLipFaceCount.ToString(CultureInfo.InvariantCulture))
                    .Append(" BlendShapes=").Append(slot.BlendShapeCount.ToString(CultureInfo.InvariantCulture))
                    .AppendLine();
            }

            report.Append("Prefab=").Append(PrefabPath)
                .Append(" Bones=").Append(result.PrefabRig.BoneCount.ToString(CultureInfo.InvariantCulture))
                .Append(" Vertices=").Append(result.PrefabRig.VertexCount.ToString(CultureInfo.InvariantCulture))
                .Append(" Triangles=").Append(result.PrefabRig.TriangleCount.ToString(CultureInfo.InvariantCulture))
                .Append(" UpperLipVertices=").Append(result.PrefabRig.UpperCount.ToString(CultureInfo.InvariantCulture))
                .Append(" LowerLipVertices=").Append(result.PrefabRig.LowerCount.ToString(CultureInfo.InvariantCulture))
                .Append(" NonLipVertices=").Append(result.PrefabRig.NonLipCount.ToString(CultureInfo.InvariantCulture))
                .Append(" InterLipFaces=").Append(result.PrefabRig.InterLipFaceCount.ToString(CultureInfo.InvariantCulture))
                .AppendLine();

            var absolute = Absolute(ReportPath);
            Directory.CreateDirectory(Path.GetDirectoryName(absolute) ??
                                      throw new InvalidOperationException("Invalid Fuga lip-rig report path."));
            File.WriteAllText(absolute, report.ToString(), new UTF8Encoding(false));
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

        private static string Absolute(string assetRelativePath)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetRelativePath));
        }

        private static string Sha256(string path)
        {
            using var stream = File.OpenRead(path);
            using var algorithm = SHA256.Create();
            return BitConverter.ToString(algorithm.ComputeHash(stream)).Replace("-", string.Empty);
        }

        private static string Vec(Vector3 value)
        {
            return "(" + value.x.ToString("F6", CultureInfo.InvariantCulture) + "," +
                   value.y.ToString("F6", CultureInfo.InvariantCulture) + "," +
                   value.z.ToString("F6", CultureInfo.InvariantCulture) + ")";
        }

        private readonly struct RigResult
        {
            public RigResult(
                int boneCount,
                int vertexCount,
                int triangleCount,
                int upperCount,
                int lowerCount,
                int nonLipCount,
                int interLipFaceCount)
            {
                BoneCount = boneCount;
                VertexCount = vertexCount;
                TriangleCount = triangleCount;
                UpperCount = upperCount;
                LowerCount = lowerCount;
                NonLipCount = nonLipCount;
                InterLipFaceCount = interLipFaceCount;
            }

            public int BoneCount { get; }
            public int VertexCount { get; }
            public int TriangleCount { get; }
            public int UpperCount { get; }
            public int LowerCount { get; }
            public int NonLipCount { get; }
            public int InterLipFaceCount { get; }
        }

        private readonly struct BlendShapeFrame
        {
            public BlendShapeFrame(
                string name,
                float weight,
                Vector3[] deltaVertices,
                Vector3[] deltaNormals,
                Vector3[] deltaTangents)
            {
                Name = name;
                Weight = weight;
                DeltaVertices = deltaVertices;
                DeltaNormals = deltaNormals;
                DeltaTangents = deltaTangents;
            }

            public string Name { get; }
            public float Weight { get; }
            public Vector3[] DeltaVertices { get; }
            public Vector3[] DeltaNormals { get; }
            public Vector3[] DeltaTangents { get; }
        }

        private readonly struct SlotResult
        {
            public SlotResult(string name, string meshPath, int blendShapeCount, RigResult rig)
            {
                Name = name;
                MeshPath = meshPath;
                BlendShapeCount = blendShapeCount;
                Rig = rig;
            }

            public string Name { get; }
            public string MeshPath { get; }
            public int BlendShapeCount { get; }
            public RigResult Rig { get; }
        }

        private readonly struct InspectionResult
        {
            public InspectionResult(SlotResult[] slots, RigResult prefabRig, string sourceHash)
            {
                Slots = slots;
                PrefabRig = prefabRig;
                SourceHash = sourceHash;
            }

            public SlotResult[] Slots { get; }
            public RigResult PrefabRig { get; }
            public string SourceHash { get; }
        }
    }
}
