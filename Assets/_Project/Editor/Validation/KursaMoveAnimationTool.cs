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

namespace Bellerophon.Editor.KursaCargoRunScene
{
    internal static class KursaMoveAnimationTool
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementRootName = "Approved Kursa Enemy Placement";
        private const string StaticSlotName = "Kursa_01_Static_Review";
        private const string MoveSlotName = "Kursa_03_Move";
        private const string ModelName = "Kursa_Model";
        internal const string MoveModelPath = "Assets/_Project/Art/Enemies/Kursa/Animations/Models/KUŠkursa walking.fbx";
        private const string AppearanceReferenceModelPath = "Assets/_Project/Art/Enemies/Kursa/ApprovedAppearance/Models/Kursa_Appearance_ReferenceSync.fbx";
        private const string EyeProjectionModelPath = "Assets/_Project/Art/Enemies/Kursa/ApprovedAppearance/Models/Kursa_Appearance_RuntimeProjection.fbx";
        private const string AppearanceMeshPath = "Assets/_Project/Art/Enemies/Kursa/Animations/Kursa_03_Move_AppearanceOnly.asset";
        internal const string ClipPath = "Assets/_Project/Art/Enemies/Kursa/Animations/Kursa_03_Move_InPlace.anim";
        internal const string ControllerPath = "Assets/_Project/Art/Enemies/Kursa/Animations/Kursa_03_Move.controller";
        private const string ReportPath = "docs/validation/kursa_move_animation_2026-08-02/Kursa_03_Move_Inspection.txt";
        private const string CapturePath = "docs/validation/kursa_move_animation_2026-08-02/Kursa_03_Move_Review.png";
        private const string ExpectedMoveSha256 = "A1E365B11C6A0C316E7208891E223746C125B49ECB61E9A15629D852C959603C";
        private const float FrameRate = 60f;
        private const float ExpectedLength = 116f / FrameRate;
        private const int ExpectedTriangles = 3913;
        private const int ExpectedBones = 24;
        private const float GroundAgreementTolerance = 0.0001f;

        private static readonly string[] SlotNames =
        {
            "Kursa_01_Static_Review", "Kursa_02_Idle", "Kursa_03_Move",
            "Kursa_04_ShieldBash", "Kursa_05_ToShieldStance", "Kursa_06_ShieldStance",
            "Kursa_07_ShieldStanceMove", "Kursa_08_FromShieldStance", "Kursa_09_Stop",
            "Kursa_10_Hit", "Kursa_11_Death", "Kursa_12_ShieldBreakReaction"
        };

        private static readonly float[] ReviewTimes = { 0f, 0.25f, 0.5f, 0.75f, 1f };

        [MenuItem("Bellerophon/Enemies/Kursa/Apply Move Animation")]
        public static void ApplyKursaMoveAnimation()
        {
            var scene = RequireScene(true);
            RequireHash(MoveModelPath, ExpectedMoveSha256);
            var placement = RequirePlacement(scene);
            RequireSlotContract(placement.transform);
            var staticRenderer = RequireRenderer(
                RequireModel(RequireChild(placement.transform, StaticSlotName)),
                StaticSlotName);
            var moveSlot = RequireChild(placement.transform, MoveSlotName);
            var previous = RequireModel(moveSlot);
            var targetModelY = MeasureOtherKursaModelY(placement.transform);

            var otherSlotsBefore = OtherSlotSignatures(placement.transform);
            var otherRootsBefore = OtherRootSignatures(scene, placement);
            var previousPosition = previous.localPosition;
            var previousRotation = previous.localRotation;
            var previousScale = previous.localScale;

            var takeName = ConfigureImporter();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MoveModelPath) ??
                throw new InvalidOperationException("Kursa walking FBX prefab is missing.");
            var appearanceMesh = CreateAppearanceOnlyMesh(prefab);
            var sourceClip = RequireEmbeddedClip(takeName);
            var clip = CreateInPlaceClip(
                sourceClip,
                prefab.transform,
                appearanceMesh,
                staticRenderer.sharedMaterials);
            var controller = CreateController(clip);
            var replacement = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject ??
                throw new InvalidOperationException("Kursa walking FBX could not be instantiated.");
            replacement.name = "Kursa_Move_Replacement_Pending";
            replacement.transform.SetParent(moveSlot, false);
            replacement.transform.SetLocalPositionAndRotation(previousPosition, previousRotation);
            replacement.transform.localScale = previousScale;
            var alignedPosition = replacement.transform.localPosition;
            alignedPosition.y = targetModelY;
            replacement.transform.localPosition = alignedPosition;

            try
            {
                var renderer = RequireRenderer(replacement.transform, MoveSlotName);
                ApplyAppearanceOnly(renderer, staticRenderer, appearanceMesh);
                renderer.updateWhenOffscreen = true;
                EditorUtility.SetDirty(renderer);

                var animator = replacement.GetComponent<Animator>() ?? replacement.AddComponent<Animator>();
                animator.enabled = true;
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.updateMode = AnimatorUpdateMode.Normal;
                EditorUtility.SetDirty(animator);

                var metrics = InspectModel(
                    replacement.transform,
                    sourceClip,
                    clip,
                    controller,
                    false);
                WriteReport(metrics);
            }
            catch
            {
                UnityEngine.Object.DestroyImmediate(replacement);
                throw;
            }

            UnityEngine.Object.DestroyImmediate(previous.gameObject);
            replacement.name = ModelName;
            RequireEqual(otherSlotsBefore, OtherSlotSignatures(placement.transform), "A Kursa slot outside Kursa_03_Move changed.");
            RequireEqual(otherRootsBefore, OtherRootSignatures(scene, placement), "A scene root outside the Kursa placement changed.");
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("CargoRunMvp could not be saved after replacing Kursa_03_Move.");
            AssetDatabase.SaveAssets();
            var priorCapture = Absolute(CapturePath);
            if (File.Exists(priorCapture)) File.Delete(priorCapture);
            Debug.Log("KursaMoveAnimationApplied Result=PASS, Slot=Kursa_03_Move, SourceHash=" + ExpectedMoveSha256 + ", MixamoTake=" + takeName + ", AppearanceOnly=True, WalkingGeometryWeightsBindPosesPreserved=True, TargetModelLocalY=" + Num(targetModelY) + ", ModelLocalForwardHeadAlignment=True, EyeAttachment=PerVertexUvChannelsOnSkinnedFaceMesh, StaticArmDataUsed=False, RootHorizontalLocked=True, OtherSlotsUnchanged=True, OtherSceneRootsUnchanged=True, SceneSaved=True.");
        }

        private static Mesh CreateAppearanceOnlyMesh(GameObject walkingPrefab)
        {
            var walkingRenderer = RequireRenderer(
                walkingPrefab.transform,
                "walking source FBX");
            var referenceRenderer = RequireAssetRenderer(
                AppearanceReferenceModelPath,
                "approved appearance reference FBX");
            var eyeRenderer = RequireAssetRenderer(
                EyeProjectionModelPath,
                "approved eye projection FBX");
            var walkingMesh = walkingRenderer.sharedMesh ??
                throw new InvalidOperationException("Walking source mesh is missing.");
            var referenceMesh = referenceRenderer.sharedMesh ??
                throw new InvalidOperationException("Appearance reference mesh is missing.");
            var eyeMesh = eyeRenderer.sharedMesh ??
                throw new InvalidOperationException("Eye projection mesh is missing.");

            var result = BuildAppearanceOnlyMesh(
                walkingMesh,
                referenceRenderer,
                eyeRenderer);

            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(AppearanceMeshPath) != null &&
                !AssetDatabase.DeleteAsset(AppearanceMeshPath))
                throw new InvalidOperationException(
                    "Existing Kursa move appearance-only mesh could not be replaced.");
            AssetDatabase.CreateAsset(result, AppearanceMeshPath);
            AssetDatabase.SaveAssets();
            return AssetDatabase.LoadAssetAtPath<Mesh>(AppearanceMeshPath) ??
                throw new InvalidOperationException(
                    "Kursa move appearance-only mesh was not saved.");
        }

        private static Mesh BuildAppearanceOnlyMesh(
            Mesh walking,
            SkinnedMeshRenderer referenceRenderer,
            SkinnedMeshRenderer eyeRenderer)
        {
            var reference = referenceRenderer.sharedMesh ??
                throw new InvalidOperationException(
                    "Approved appearance reference mesh is missing.");
            var eye = eyeRenderer.sharedMesh ??
                throw new InvalidOperationException(
                    "Approved eye projection mesh is missing.");
            if (eye.uv2.Length != eye.vertexCount ||
                eye.uv3.Length != eye.vertexCount ||
                eye.uv4.Length != eye.vertexCount)
                throw new InvalidOperationException(
                    "Approved eye projection channels are incomplete.");
            var referenceSignatures = SkinUvSignatures(referenceRenderer);
            var eyeSignatures = SkinUvSignatures(eyeRenderer);
            var referenceTriangles = MeshTriangles(reference, referenceSignatures)
                .GroupBy(item => item.Signature, StringComparer.Ordinal)
                .ToDictionary(
                    group => group.Key,
                    group => group.ToArray(),
                    StringComparer.Ordinal);
            var walkingGeometrySignatures = VertexSignatures(walking);
            var referenceGeometrySignatures = VertexSignatures(reference);
            var walkingTriangles = MeshTriangles(
                    walking,
                    walkingGeometrySignatures,
                    false)
                .GroupBy(item => item.Signature, StringComparer.Ordinal)
                .ToDictionary(
                    group => group.Key,
                    group => group.ToArray(),
                    StringComparer.Ordinal);
            var geometryDomains = referenceGeometrySignatures.Select(signature =>
                new HashSet<int>(Enumerable.Range(0, walking.vertexCount)
                    .Where(index => walkingGeometrySignatures[index] == signature)))
                .ToArray();
            if (geometryDomains.Any(item => item.Count == 0))
                throw new InvalidOperationException(
                    "An appearance-reference corner has no walking geometry match.");
            var domains = eyeSignatures.Select(signature =>
                new HashSet<int>(Enumerable.Range(0, reference.vertexCount)
                    .Where(index => referenceSignatures[index] == signature)))
                .ToArray();
            if (domains.Any(item => item.Count == 0))
                throw new InvalidOperationException(
                    "An eye-projection corner has no approved appearance match.");

            var walkingVertices = walking.vertices;
            var walkingNormals = walking.normals;
            var walkingTangents = walking.tangents;
            var walkingUv = walking.uv;
            var walkingWeights = walking.boneWeights;
            var walkingFullSignatures = FullVertexSignatures(walking);
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var tangents = new List<Vector4>();
            var uv0 = new List<Vector2>();
            var uv1 = new List<Vector2>();
            var uv2 = new List<Vector2>();
            var uv3 = new List<Vector2>();
            var weights = new List<BoneWeight>();
            var submeshIndices = new List<int[]>();
            var eyeLeft = eye.uv2;
            var eyeRight = eye.uv3;
            var eyeDepth = eye.uv4;
            var eyeTriangles = MeshTriangles(eye, eyeSignatures);

            for (var subMesh = 0; subMesh < eye.subMeshCount; subMesh++)
            {
                var indices = new List<int>();
                foreach (var eyeTriangle in eyeTriangles.Where(item =>
                    item.SubMesh == subMesh))
                {
                    if (!referenceTriangles.TryGetValue(
                        eyeTriangle.Signature,
                        out var possibleTriangles))
                        throw new InvalidOperationException(
                            "An approved eye triangle has no appearance-reference match.");
                    var mappings = possibleTriangles.SelectMany(referenceTriangle =>
                    {
                        var eyeMappings = TriangleMappings(
                            eyeTriangle,
                            referenceTriangle,
                            eyeSignatures,
                            referenceSignatures,
                            domains);
                        var geometrySignature = GeometryTriangleSignature(
                            referenceTriangle,
                            referenceGeometrySignatures);
                        if (!walkingTriangles.TryGetValue(
                            geometrySignature,
                            out var possibleWalkingTriangles))
                            return Enumerable.Empty<int[]>();
                        var referenceToWalkingMappings =
                            possibleWalkingTriangles.SelectMany(walkingTriangle =>
                                TriangleMappings(
                                    referenceTriangle,
                                    walkingTriangle,
                                    referenceGeometrySignatures,
                                    walkingGeometrySignatures,
                                    geometryDomains)).ToArray();
                        return eyeMappings.SelectMany(eyeMapping =>
                            referenceToWalkingMappings.Select(geometryMapping =>
                                eyeTriangle.Indices.Select(eyeIndex =>
                                    geometryMapping[eyeMapping[eyeIndex]])
                                    .ToArray()));
                    })
                        .ToArray();
                    if (mappings.Length == 0)
                        throw new InvalidOperationException(
                            "An approved eye triangle has no exact walking-data match.");
                    var groups = mappings.GroupBy(item => string.Join("|",
                            item.Select(walkingIndex =>
                                walkingFullSignatures[walkingIndex])),
                        StringComparer.Ordinal).ToArray();
                    if (groups.Length != 1)
                        throw new InvalidOperationException(
                            "An approved eye triangle maps to different walking corner data.");
                    var walkingCorners = groups[0].First();
                    for (var corner = 0; corner < 3; corner++)
                    {
                        var walkingIndex = walkingCorners[corner];
                        var eyeIndex = eyeTriangle.Indices[corner];
                        indices.Add(vertices.Count);
                        vertices.Add(walkingVertices[walkingIndex]);
                        normals.Add(walkingNormals[walkingIndex]);
                        tangents.Add(walkingTangents[walkingIndex]);
                        uv0.Add(walkingUv[walkingIndex]);
                        uv1.Add(eyeLeft[eyeIndex]);
                        uv2.Add(eyeRight[eyeIndex]);
                        uv3.Add(eyeDepth[eyeIndex]);
                        weights.Add(walkingWeights[walkingIndex]);
                    }
                }
                submeshIndices.Add(indices.ToArray());
            }

            var result = new Mesh
            {
                name = "Kursa_03_Move_AppearanceOnly",
                indexFormat = UnityEngine.Rendering.IndexFormat.UInt32,
                vertices = vertices.ToArray(),
                normals = normals.ToArray(),
                tangents = tangents.ToArray(),
                uv = uv0.ToArray(),
                uv2 = uv1.ToArray(),
                uv3 = uv2.ToArray(),
                uv4 = uv3.ToArray(),
                boneWeights = weights.ToArray(),
                bindposes = walking.bindposes,
                bounds = walking.bounds,
                subMeshCount = submeshIndices.Count
            };
            for (var subMesh = 0; subMesh < submeshIndices.Count; subMesh++)
                result.SetIndices(
                    submeshIndices[subMesh],
                    MeshTopology.Triangles,
                    subMesh,
                    false);
            result.bounds = walking.bounds;
            RequireTriangleDataParity(walking, result);
            return result;
        }

        private static string GeometryTriangleSignature(
            MeshTriangle triangle,
            string[] vertexSignatures) =>
            string.Join("~", triangle.Indices.Select(item => vertexSignatures[item])
                .OrderBy(item => item, StringComparer.Ordinal));

        private static string[] FullVertexSignatures(Mesh mesh)
        {
            var vertices = mesh.vertices;
            var normals = mesh.normals;
            var tangents = mesh.tangents;
            var texture = mesh.uv;
            var weights = mesh.boneWeights;
            var result = new string[mesh.vertexCount];
            for (var index = 0; index < mesh.vertexCount; index++)
            {
                var vertex = vertices[index];
                var normal = normals[index];
                var tangent = tangents[index];
                var uv = texture[index];
                var weight = weights[index];
                result[index] = FloatKey(vertex.x) + ":" + FloatKey(vertex.y) +
                    ":" + FloatKey(vertex.z) + "|" + FloatKey(normal.x) + ":" +
                    FloatKey(normal.y) + ":" + FloatKey(normal.z) + "|" +
                    FloatKey(tangent.x) + ":" + FloatKey(tangent.y) + ":" +
                    FloatKey(tangent.z) + ":" + FloatKey(tangent.w) + "|" +
                    FloatKey(uv.x) + ":" + FloatKey(uv.y) + "|" +
                    weight.boneIndex0 + ":" + FloatKey(weight.weight0) + ":" +
                    weight.boneIndex1 + ":" + FloatKey(weight.weight1) + ":" +
                    weight.boneIndex2 + ":" + FloatKey(weight.weight2) + ":" +
                    weight.boneIndex3 + ":" + FloatKey(weight.weight3);
            }
            return result;
        }

        private static void ApplyAppearanceOnly(
            SkinnedMeshRenderer moveRenderer,
            SkinnedMeshRenderer staticRenderer,
            Mesh appearanceMesh)
        {
            var materials = staticRenderer.sharedMaterials;
            if (materials.Length != appearanceMesh.subMeshCount ||
                materials.Any(item => item == null))
                throw new InvalidOperationException(
                    "The placed static Kursa material order is incomplete.");
            moveRenderer.sharedMesh = appearanceMesh;
            moveRenderer.sharedMaterials = materials;
        }

        private static string[] VertexSignatures(Mesh mesh)
        {
            var vertices = mesh.vertices;
            var uv = mesh.uv;
            if (uv.Length != mesh.vertexCount)
                throw new InvalidOperationException(
                    "Kursa vertex attributes are incomplete for exact mapping.");
            var result = new string[mesh.vertexCount];
            for (var index = 0; index < mesh.vertexCount; index++)
            {
                var vertex = vertices[index];
                var texture = uv[index];
                result[index] = FloatKey(vertex.x) + ":" + FloatKey(vertex.y) +
                    ":" + FloatKey(vertex.z) + "|" + FloatKey(texture.x) +
                    ":" + FloatKey(texture.y);
            }
            return result;
        }

        private static string FloatKey(float value) =>
            Math.Round(value, 6, MidpointRounding.AwayFromZero)
                .ToString("R", CultureInfo.InvariantCulture);

        private static MeshTriangle[] MeshTriangles(
            Mesh mesh,
            string[] vertexSignatures,
            bool includeSubMesh = true)
        {
            var result = new List<MeshTriangle>();
            for (var subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
            {
                if (mesh.GetTopology(subMesh) != MeshTopology.Triangles)
                    throw new InvalidOperationException(
                        "Kursa appearance contains a non-triangle submesh.");
                var indices = mesh.GetIndices(subMesh);
                for (var offset = 0; offset < indices.Length; offset += 3)
                {
                    var triangle = new[]
                    {
                        indices[offset], indices[offset + 1], indices[offset + 2]
                    };
                    var signature = (includeSubMesh ? subMesh + "|" : string.Empty) +
                        string.Join("~",
                        triangle.Select(item => vertexSignatures[item])
                            .OrderBy(item => item, StringComparer.Ordinal));
                    result.Add(new MeshTriangle(
                        result.Count,
                        subMesh,
                        triangle,
                        signature));
                }
            }
            return result.ToArray();
        }

        private static IEnumerable<Dictionary<int, int>> TriangleMappings(
            MeshTriangle source,
            MeshTriangle destination,
            string[] sourceSignatures,
            string[] destinationSignatures,
            HashSet<int>[] domains)
        {
            var permutations = new[]
            {
                new[] { 0, 1, 2 }, new[] { 0, 2, 1 },
                new[] { 1, 0, 2 }, new[] { 1, 2, 0 },
                new[] { 2, 0, 1 }, new[] { 2, 1, 0 }
            };
            foreach (var permutation in permutations)
            {
                var mapping = new Dictionary<int, int>();
                var valid = true;
                for (var corner = 0; corner < 3; corner++)
                {
                    var sourceIndex = source.Indices[corner];
                    var destinationIndex = destination.Indices[permutation[corner]];
                    if (sourceSignatures[sourceIndex] !=
                            destinationSignatures[destinationIndex] ||
                        !domains[sourceIndex].Contains(destinationIndex) ||
                        (mapping.TryGetValue(sourceIndex, out var existing) &&
                            existing != destinationIndex))
                    {
                        valid = false;
                        break;
                    }
                    mapping[sourceIndex] = destinationIndex;
                }
                if (valid && mapping.Values.Distinct().Count() == mapping.Count)
                    yield return mapping;
            }
        }

        private static string[] SkinUvSignatures(SkinnedMeshRenderer renderer)
        {
            var mesh = renderer.sharedMesh ??
                throw new InvalidOperationException(
                    "Kursa renderer mesh is missing during skin mapping.");
            var uv = mesh.uv;
            var weights = mesh.boneWeights;
            var bones = renderer.bones;
            if (uv.Length != mesh.vertexCount || weights.Length != mesh.vertexCount)
                throw new InvalidOperationException(
                    "Kursa skin or UV0 data is incomplete.");
            var result = new string[mesh.vertexCount];
            for (var index = 0; index < mesh.vertexCount; index++)
            {
                var weight = weights[index];
                var influences = new[]
                {
                    new { Bone = weight.boneIndex0, Weight = weight.weight0 },
                    new { Bone = weight.boneIndex1, Weight = weight.weight1 },
                    new { Bone = weight.boneIndex2, Weight = weight.weight2 },
                    new { Bone = weight.boneIndex3, Weight = weight.weight3 }
                }.Where(item => item.Weight > 0.000001f)
                    .Select(item =>
                    {
                        if (item.Bone < 0 || item.Bone >= bones.Length ||
                            bones[item.Bone] == null)
                            throw new InvalidOperationException(
                                "Kursa skin references an invalid bone index.");
                        return bones[item.Bone].name + ":" + FloatKey(item.Weight);
                    })
                    .OrderBy(item => item, StringComparer.Ordinal);
                result[index] = FloatKey(uv[index].x) + ":" +
                    FloatKey(uv[index].y) + "|" + string.Join(";", influences);
            }
            return result;
        }

        private static void RequireVector3Parity(
            Vector3[] expected,
            Vector3[] actual,
            string label)
        {
            if (expected.Length != actual.Length)
                throw new InvalidOperationException(
                    label + " counts differ: " + expected.Length + " vs " +
                    actual.Length + ".");
            for (var index = 0; index < expected.Length; index++)
            {
                var distance = Vector3.Distance(expected[index], actual[index]);
                if (distance > 0.000001f)
                    throw new InvalidOperationException(
                        label + " differ at index " + index + ": expected=" +
                        expected[index].ToString("R") + ", actual=" +
                        actual[index].ToString("R") + ", distance=" +
                        Num(distance) + ".");
            }
        }

        private static void RequireVector2Parity(
            Vector2[] expected,
            Vector2[] actual,
            string label)
        {
            if (expected.Length != actual.Length ||
                expected.Where((value, index) =>
                    Vector2.Distance(value, actual[index]) > 0.000001f).Any())
                throw new InvalidOperationException(
                    label + " differ.");
        }

        private static void RequireVector4Parity(
            Vector4[] expected,
            Vector4[] actual,
            string label)
        {
            if (expected.Length != actual.Length ||
                expected.Where((value, index) =>
                    Vector4.Distance(value, actual[index]) > 0.000001f).Any())
                throw new InvalidOperationException(label + " differ.");
        }

        private static void RequireMatrixParity(
            Matrix4x4[] expected,
            Matrix4x4[] actual,
            string label)
        {
            if (expected.Length != actual.Length)
                throw new InvalidOperationException(label + " counts differ.");
            for (var index = 0; index < expected.Length; index++)
            {
                for (var row = 0; row < 4; row++)
                {
                    for (var column = 0; column < 4; column++)
                    {
                        if (Mathf.Abs(expected[index][row, column] -
                            actual[index][row, column]) > 0.000001f)
                            throw new InvalidOperationException(
                                label + " differ at index " + index + ".");
                    }
                }
            }
        }

        private static void RequireBoneWeightParity(
            BoneWeight[] expected,
            BoneWeight[] actual)
        {
            if (expected.Length != actual.Length)
                throw new InvalidOperationException(
                    "Walking and appearance-reference bone-weight counts differ.");
            for (var index = 0; index < expected.Length; index++)
            {
                var a = expected[index];
                var b = actual[index];
                if (a.boneIndex0 != b.boneIndex0 || a.boneIndex1 != b.boneIndex1 ||
                    a.boneIndex2 != b.boneIndex2 || a.boneIndex3 != b.boneIndex3 ||
                    Mathf.Abs(a.weight0 - b.weight0) > 0.000001f ||
                    Mathf.Abs(a.weight1 - b.weight1) > 0.000001f ||
                    Mathf.Abs(a.weight2 - b.weight2) > 0.000001f ||
                    Mathf.Abs(a.weight3 - b.weight3) > 0.000001f)
                    throw new InvalidOperationException(
                        "Walking and appearance-reference bone weights differ at vertex " +
                        index + ".");
            }
        }

        private static void RequireSubmeshParity(Mesh expected, Mesh actual)
        {
            if (expected.subMeshCount != actual.subMeshCount)
                throw new InvalidOperationException(
                    "Approved appearance submesh counts differ.");
            for (var index = 0; index < expected.subMeshCount; index++)
            {
                if (expected.GetTopology(index) != actual.GetTopology(index) ||
                    !expected.GetIndices(index).SequenceEqual(actual.GetIndices(index)))
                    throw new InvalidOperationException(
                        "Approved appearance submesh assignment differs at index " +
                        index + ".");
            }
        }

        private static void RequireTriangleDataParity(Mesh expected, Mesh actual)
        {
            var expectedSignatures = FullVertexSignatures(expected);
            var actualSignatures = FullVertexSignatures(actual);
            var expectedTriangles = MeshTriangles(
                    expected,
                    expectedSignatures,
                    false)
                .Select(item => item.Signature)
                .OrderBy(item => item, StringComparer.Ordinal);
            var actualTriangles = MeshTriangles(
                    actual,
                    actualSignatures,
                    false)
                .Select(item => item.Signature)
                .OrderBy(item => item, StringComparer.Ordinal);
            if (!expectedTriangles.SequenceEqual(
                actualTriangles,
                StringComparer.Ordinal))
                throw new InvalidOperationException(
                    "Walking source and appearance-only triangle data differ.");
        }

        [MenuItem("Bellerophon/Enemies/Kursa/Inspect Move Animation")]
        public static void InspectKursaMoveAnimation()
        {
            var scene = RequireScene(true);
            var wasDirty = scene.isDirty;
            RequireHash(MoveModelPath, ExpectedMoveSha256);
            var placement = RequirePlacement(scene);
            RequireSlotContract(placement.transform);
            var moveModel = RequireModel(RequireChild(placement.transform, MoveSlotName));
            var sourceClip = RequireEmbeddedClip("Kursa_03_Move_Mixamo");
            var metrics = InspectModel(moveModel, sourceClip, RequireClip(), RequireController(), true);
            WriteReport(metrics);
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException("Kursa move inspection changed the scene dirty state.");
            Debug.Log("KursaMoveAnimationInspected Result=PASS, Length=" + Num(metrics.Length) + ", RootHorizontalRange=" + Num(metrics.RootHorizontalRange) + ", MaximumSourcePositionError=" + Num(metrics.MaximumSourcePositionError) + ", MaximumSourceRotationError=" + Num(metrics.MaximumSourceRotationError) + ", MoveModelLocalY=" + Num(metrics.MoveModelLocalY) + ", OtherKursaCommonModelY=" + Num(metrics.OtherKursaCommonModelY) + ", ModelLocalForwardHeadAlignment=True, MovingBones=" + metrics.MovingBones + ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Kursa/Capture Move Animation Review")]
        public static void CaptureKursaMoveAnimationReview()
        {
            var scene = RequireScene(true);
            var wasDirty = scene.isDirty;
            var placement = RequirePlacement(scene);
            var moveModel = RequireModel(RequireChild(placement.transform, MoveSlotName));
            var clip = RequireClip();
            InspectModel(moveModel, RequireEmbeddedClip("Kursa_03_Move_Mixamo"), clip, RequireController(), true);
            var destination = Absolute(CapturePath);
            if (File.Exists(destination))
                throw new InvalidOperationException("The one-time Kursa move review already exists: " + CapturePath);
            CaptureStrip(moveModel, clip, destination);
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException("Kursa move capture changed the scene dirty state.");
            Debug.Log("KursaMoveAnimationReviewCaptured Result=PASS, NormalizedTimes=0,0.25,0.5,0.75,1, Image=" + CapturePath + ", SceneChanged=False.");
        }

        private static string ConfigureImporter()
        {
            var importer = AssetImporter.GetAtPath(MoveModelPath) as ModelImporter ??
                throw new InvalidOperationException("Kursa walking FBX importer is missing.");
            importer.importAnimation = true;
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.optimizeGameObjects = false;
            importer.isReadable = true;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;

            var defaults = importer.defaultClipAnimations;
            var matches = defaults.Where(item =>
                    item.name.IndexOf("mixamo", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    item.takeName.IndexOf("mixamo", StringComparison.OrdinalIgnoreCase) >= 0)
                .ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException("The Kursa walking FBX must expose exactly one Mixamo take. Matches=" + matches.Length + ", Defaults=" + string.Join("|", defaults.Select(item => item.name + ":" + item.takeName)) + ".");

            var selected = matches[0];
            selected.name = "Kursa_03_Move_Mixamo";
            selected.loopTime = true;
            selected.loopPose = false;
            selected.wrapMode = WrapMode.Loop;
            selected.lockRootPositionXZ = false;
            selected.keepOriginalPositionXZ = true;
            importer.animationWrapMode = WrapMode.Loop;
            importer.clipAnimations = new[] { selected };
            importer.SaveAndReimport();
            return selected.name;
        }

        private static AnimationClip RequireEmbeddedClip(string name)
        {
            var clips = AssetDatabase.LoadAllAssetsAtPath(MoveModelPath).OfType<AnimationClip>()
                .Where(item => !item.name.StartsWith("__preview__", StringComparison.Ordinal)).ToArray();
            if (clips.Length != 1 || clips[0].name != name)
                throw new InvalidOperationException("The selected Mixamo take is not the sole imported Kursa walking clip. Clips=" + string.Join("|", clips.Select(item => item.name)) + ".");
            return clips[0];
        }

        private static AnimationClip CreateInPlaceClip(
            AnimationClip source,
            Transform prefabRoot,
            Mesh appearanceMesh,
            Material[] appearanceMaterials)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);
            if (clip == null)
            {
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, ClipPath);
            }
            clip.ClearCurves();
            clip.name = "Kursa_03_Move_InPlace";
            clip.frameRate = FrameRate;
            clip.wrapMode = WrapMode.Loop;

            var clone = UnityEngine.Object.Instantiate(prefabRoot.gameObject);
            clone.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                var renderer = RequireRenderer(clone.transform, "temporary walking FBX");
                renderer.sharedMesh = appearanceMesh;
                renderer.sharedMaterials = appearanceMaterials;
                var bones = renderer.rootBone.GetComponentsInChildren<Transform>(true);
                if (bones.Select(item => item.name).Distinct(StringComparer.Ordinal).Count() != bones.Length)
                    throw new InvalidOperationException("Kursa walking skeleton contains duplicate bone names.");
                var paths = bones.ToDictionary(item => item.name, item => AnimationUtility.CalculateTransformPath(item, clone.transform), StringComparer.Ordinal);
                source.SampleAnimation(clone, 0f);
                var rootBase = renderer.rootBone.localPosition;
                var horizontalProperties = HorizontalRootProperties(clone.transform, renderer.rootBone);
                var keySets = bones.ToDictionary(item => item.name, _ => new TransformKeys(), StringComparer.Ordinal);
                var frames = Mathf.RoundToInt(source.length * FrameRate);

                for (var frame = 0; frame <= frames; frame++)
                {
                    var time = Mathf.Min(source.length, frame / FrameRate);
                    source.SampleAnimation(clone, time);
                    KursaForwardHeadAlignmentTool.AlignHeadToModelLocalForward(
                        clone.transform,
                        renderer);
                    foreach (var bone in bones)
                    {
                        var position = bone.localPosition;
                        if (bone == renderer.rootBone)
                        {
                            if (horizontalProperties.Contains("m_LocalPosition.x")) position.x = rootBase.x;
                            if (horizontalProperties.Contains("m_LocalPosition.y")) position.y = rootBase.y;
                            if (horizontalProperties.Contains("m_LocalPosition.z")) position.z = rootBase.z;
                        }
                        keySets[bone.name].Add(time, position, bone.localRotation);
                    }
                }
                foreach (var bone in bones)
                    SetTransformCurves(clip, paths[bone.name], keySets[bone.name]);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(clone);
            }

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.loopBlend = false;
            settings.keepOriginalPositionXZ = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            clip.EnsureQuaternionContinuity();
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssets();
            return clip;
        }

        private static void SetTransformCurves(AnimationClip clip, string path, TransformKeys keys)
        {
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalPosition.x"), LinearCurve(keys.PositionX));
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalPosition.y"), LinearCurve(keys.PositionY));
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalPosition.z"), LinearCurve(keys.PositionZ));
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalRotation.x"), LinearCurve(keys.RotationX));
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalRotation.y"), LinearCurve(keys.RotationY));
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalRotation.z"), LinearCurve(keys.RotationZ));
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalRotation.w"), LinearCurve(keys.RotationW));
        }

        private static AnimationCurve LinearCurve(List<Keyframe> keys)
        {
            var curve = new AnimationCurve(keys.ToArray());
            for (var index = 0; index < curve.length; index++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, index, AnimationUtility.TangentMode.Linear);
                AnimationUtility.SetKeyRightTangentMode(curve, index, AnimationUtility.TangentMode.Linear);
            }
            return curve;
        }

        private static HashSet<string> HorizontalRootProperties(Transform root, Transform rootBone)
        {
            var result = new HashSet<string>(StringComparer.Ordinal);
            var axes = new[] { Vector3.right, Vector3.up, Vector3.forward };
            var suffixes = new[] { "x", "y", "z" };
            for (var index = 0; index < axes.Length; index++)
            {
                var worldDirection = rootBone.parent.TransformDirection(axes[index]);
                var modelDirection = root.InverseTransformDirection(worldDirection).normalized;
                if (Mathf.Abs(modelDirection.x) > 0.5f || Mathf.Abs(modelDirection.z) > 0.5f)
                    result.Add("m_LocalPosition." + suffixes[index]);
            }
            if (result.Count != 2)
                throw new InvalidOperationException("Kursa Hips horizontal-axis mapping differs. Properties=" + string.Join(",", result) + ".");
            return result;
        }

        private static AnimatorController CreateController(AnimationClip clip)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(ControllerPath) != null && !AssetDatabase.DeleteAsset(ControllerPath))
                throw new InvalidOperationException("Existing Kursa move controller could not be replaced.");
            var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            var state = controller.layers[0].stateMachine.AddState("KursaMoveMixamoInPlace");
            state.motion = clip;
            state.speed = 1f;
            state.writeDefaultValues = false;
            controller.layers[0].stateMachine.defaultState = state;
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static AnimationClip RequireClip() => AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath) ?? throw new InvalidOperationException("Kursa move in-place clip is missing.");
        private static AnimatorController RequireController() => AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) ?? throw new InvalidOperationException("Kursa move controller is missing.");

        private static Metrics InspectModel(Transform moveModel, AnimationClip sourceClip, AnimationClip clip, AnimatorController controller, bool requireSceneContract)
        {
            if (Mathf.Abs(clip.frameRate - FrameRate) > 0.001f || Mathf.Abs(clip.length - ExpectedLength) > 0.002f || !AnimationUtility.GetAnimationClipSettings(clip).loopTime)
                throw new InvalidOperationException("Kursa move clip timing or loop settings differ.");
            if (AnimationUtility.GetCurveBindings(clip).Any(item => item.propertyName.IndexOf("scale", StringComparison.OrdinalIgnoreCase) >= 0))
                throw new InvalidOperationException("Kursa move clip contains bone scaling curves.");

            var moveRenderer = RequireRenderer(moveModel, MoveSlotName);
            var animator = moveModel.GetComponentsInChildren<Animator>(true).SingleOrDefault() ?? throw new InvalidOperationException("Kursa_03_Move must contain one Animator.");
            if (!animator.enabled || animator.runtimeAnimatorController != controller || animator.applyRootMotion || animator.cullingMode != AnimatorCullingMode.AlwaysAnimate)
                throw new InvalidOperationException("Kursa_03_Move Animator configuration differs.");
            if (requireSceneContract) RequireAnimationSlotContract(moveModel.parent.parent);

            var sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MoveModelPath) ?? throw new InvalidOperationException("Kursa walking prefab is missing during inspection.");
            var sourceClone = UnityEngine.Object.Instantiate(sourcePrefab);
            sourceClone.hideFlags = HideFlags.HideAndDontSave;
            var moveSnapshots = moveModel.GetComponentsInChildren<Transform>(true).Select(item => new TransformState(item)).ToArray();
            var animatorEnabled = animator.enabled;
            var baked = new Mesh();
            try
            {
                animator.enabled = false;
                var sourceRenderer = RequireRenderer(sourceClone.transform, "inspection source FBX");
                var sourceMesh = sourceRenderer.sharedMesh ?? throw new InvalidOperationException("Kursa source walking mesh is missing.");
                var moveMesh = moveRenderer.sharedMesh ?? throw new InvalidOperationException("Kursa placed walking mesh is missing.");
                var appearanceMesh = AssetDatabase.LoadAssetAtPath<Mesh>(AppearanceMeshPath) ??
                    throw new InvalidOperationException("Kursa move appearance-only mesh is missing.");
                var referenceRenderer = RequireAssetRenderer(
                    AppearanceReferenceModelPath,
                    "approved appearance reference FBX");
                var referenceMesh = referenceRenderer.sharedMesh ??
                    throw new InvalidOperationException("Approved appearance reference mesh is missing.");
                var eyeRenderer = RequireAssetRenderer(
                    EyeProjectionModelPath,
                    "approved eye projection FBX");
                var eyeMesh = eyeRenderer.sharedMesh ??
                    throw new InvalidOperationException("Approved eye projection mesh is missing.");
                var staticRenderer = RequireRenderer(
                    RequireModel(RequireChild(moveModel.parent.parent, StaticSlotName)),
                    StaticSlotName);
                var triangles = Enumerable.Range(0, moveMesh.subMeshCount).Sum(index => (int)moveMesh.GetIndexCount(index) / 3);
                if (moveMesh != appearanceMesh ||
                    AssetDatabase.GetAssetPath(moveMesh) != AppearanceMeshPath ||
                    triangles != ExpectedTriangles ||
                    moveRenderer.bones.Length != ExpectedBones)
                    throw new InvalidOperationException(
                        "Kursa_03_Move appearance-only mesh contract differs.");
                var expectedAppearance = BuildAppearanceOnlyMesh(
                    sourceMesh,
                    referenceRenderer,
                    eyeRenderer);
                try
                {
                    RequireVector3Parity(expectedAppearance.vertices,
                        moveMesh.vertices, "Walking vertex positions");
                    RequireVector3Parity(expectedAppearance.normals,
                        moveMesh.normals, "Walking normals");
                    RequireVector4Parity(expectedAppearance.tangents,
                        moveMesh.tangents, "Walking tangents");
                    RequireVector2Parity(expectedAppearance.uv,
                        moveMesh.uv, "Walking UV0");
                    RequireVector2Parity(expectedAppearance.uv2,
                        moveMesh.uv2, "Approved left-eye UV channel");
                    RequireVector2Parity(expectedAppearance.uv3,
                        moveMesh.uv3, "Approved right-eye UV channel");
                    RequireVector2Parity(expectedAppearance.uv4,
                        moveMesh.uv4, "Approved eye-depth UV channel");
                    RequireBoneWeightParity(expectedAppearance.boneWeights,
                        moveMesh.boneWeights);
                    RequireMatrixParity(expectedAppearance.bindposes,
                        moveMesh.bindposes, "Walking bind poses");
                    RequireSubmeshParity(expectedAppearance, moveMesh);
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(expectedAppearance);
                }
                if (!moveRenderer.sharedMaterials.SequenceEqual(
                    staticRenderer.sharedMaterials))
                    throw new InvalidOperationException(
                        "Kursa_03_Move materials do not exactly match the placed static Kursa.");
                RequireBoneOrder(sourceRenderer, moveRenderer);
                sourceRenderer.sharedMesh = appearanceMesh;
                sourceRenderer.sharedMaterials = staticRenderer.sharedMaterials;
                var sourceBones = sourceRenderer.rootBone.GetComponentsInChildren<Transform>(true).ToDictionary(item => item.name, StringComparer.Ordinal);
                var moveBones = moveRenderer.rootBone.GetComponentsInChildren<Transform>(true).ToDictionary(item => item.name, StringComparer.Ordinal);
                if (!sourceBones.Keys.OrderBy(item => item, StringComparer.Ordinal).SequenceEqual(moveBones.Keys.OrderBy(item => item, StringComparer.Ordinal), StringComparer.Ordinal))
                    throw new InvalidOperationException("Kursa source and move bone sets differ.");
                var horizontalProperties = HorizontalRootProperties(sourceClone.transform, sourceRenderer.rootBone);
                sourceClip.SampleAnimation(sourceClone, 0f);
                KursaForwardHeadAlignmentTool.AlignHeadToModelLocalForward(
                    sourceClone.transform,
                    sourceRenderer);
                clip.SampleAnimation(moveModel.gameObject, 0f);
                var rootBase = moveRenderer.rootBone.localPosition;
                var initialRotations = moveBones.ToDictionary(item => item.Key, item => item.Value.localRotation, StringComparer.Ordinal);
                var rotationRanges = moveBones.Keys.ToDictionary(item => item, _ => 0f, StringComparer.Ordinal);
                var maximumPositionError = 0f;
                var maximumRotationError = 0f;
                var maximumScaleError = 0f;
                var maximumHeadLocalFrameError = 0f;
                var rootHorizontalRange = 0f;
                var rootStart = moveModel.InverseTransformPoint(moveRenderer.rootBone.position);
                var minimumGround = float.PositiveInfinity;
                var maximumGround = float.NegativeInfinity;
                var frames = Mathf.RoundToInt(clip.length * FrameRate);

                for (var frame = 0; frame <= frames; frame++)
                {
                    var time = Mathf.Min(clip.length, frame / FrameRate);
                    sourceClip.SampleAnimation(sourceClone, time);
                    KursaForwardHeadAlignmentTool.AlignHeadToModelLocalForward(
                        sourceClone.transform,
                        sourceRenderer);
                    clip.SampleAnimation(moveModel.gameObject, time);
                    foreach (var pair in moveBones)
                    {
                        var sourceBone = sourceBones[pair.Key];
                        var moveBone = pair.Value;
                        var delta = moveBone.localPosition - sourceBone.localPosition;
                        if (moveBone == moveRenderer.rootBone)
                        {
                            if (horizontalProperties.Contains("m_LocalPosition.x")) delta.x = 0f;
                            if (horizontalProperties.Contains("m_LocalPosition.y")) delta.y = 0f;
                            if (horizontalProperties.Contains("m_LocalPosition.z")) delta.z = 0f;
                            if (horizontalProperties.Contains("m_LocalPosition.x")) maximumPositionError = Mathf.Max(maximumPositionError, Mathf.Abs(moveBone.localPosition.x - rootBase.x));
                            if (horizontalProperties.Contains("m_LocalPosition.y")) maximumPositionError = Mathf.Max(maximumPositionError, Mathf.Abs(moveBone.localPosition.y - rootBase.y));
                            if (horizontalProperties.Contains("m_LocalPosition.z")) maximumPositionError = Mathf.Max(maximumPositionError, Mathf.Abs(moveBone.localPosition.z - rootBase.z));
                        }
                        maximumPositionError = Mathf.Max(maximumPositionError, delta.magnitude);
                        maximumRotationError = Mathf.Max(maximumRotationError, Quaternion.Angle(sourceBone.localRotation, moveBone.localRotation));
                        maximumScaleError = Mathf.Max(maximumScaleError, Vector3.Distance(sourceBone.localScale, moveBone.localScale));
                        rotationRanges[pair.Key] = Mathf.Max(rotationRanges[pair.Key], Quaternion.Angle(initialRotations[pair.Key], moveBone.localRotation));
                    }
                    var root = moveModel.InverseTransformPoint(moveRenderer.rootBone.position);
                    rootHorizontalRange = Mathf.Max(rootHorizontalRange, new Vector2(root.x - rootStart.x, root.z - rootStart.z).magnitude);
                    moveRenderer.BakeMesh(baked);
                    maximumHeadLocalFrameError = Mathf.Max(
                        maximumHeadLocalFrameError,
                        KursaForwardHeadAlignmentTool.MeasureHeadLocalFrameError(
                            moveModel,
                            moveRenderer));
                    var ground = MinimumWorldY(moveRenderer, baked);
                    minimumGround = Mathf.Min(minimumGround, ground);
                    maximumGround = Mathf.Max(maximumGround, ground);
                }

                clip.SampleAnimation(moveModel.gameObject, 0f);
                var loopStart = moveBones.ToDictionary(item => item.Key, item => new LocalPose(item.Value), StringComparer.Ordinal);
                clip.SampleAnimation(moveModel.gameObject, clip.length);
                var loopPositionError = moveBones.Max(item => Vector3.Distance(loopStart[item.Key].Position, item.Value.localPosition));
                var loopRotationError = moveBones.Max(item => Quaternion.Angle(loopStart[item.Key].Rotation, item.Value.localRotation));
                var movingBones = rotationRanges.Count(item => item.Value > 1f);

                if (maximumPositionError > 0.0001f || maximumRotationError > 0.01f || maximumScaleError > 0.0001f)
                    throw new InvalidOperationException("Kursa in-place clip differs from the embedded Mixamo bone motion. PositionError=" + Num(maximumPositionError) + ", RotationError=" + Num(maximumRotationError) + ", ScaleError=" + Num(maximumScaleError) + ".");
                if (rootHorizontalRange > 0.001f)
                    throw new InvalidOperationException("Kursa move clip still contains horizontal root translation: " + Num(rootHorizontalRange) + ".");
                if (movingBones < 12)
                    throw new InvalidOperationException("Kursa Mixamo walking motion is not active across the skeleton. MovingBones=" + movingBones + ".");
                if (loopPositionError > 0.001f || loopRotationError > 0.1f)
                    throw new InvalidOperationException("Kursa move loop boundary differs. PositionError=" + Num(loopPositionError) + ", RotationError=" + Num(loopRotationError) + ".");

                var commonModelY = MeasureOtherKursaModelY(
                    moveModel.parent.parent);
                var modelYError = Mathf.Abs(
                    moveModel.localPosition.y - commonModelY);
                if (modelYError > GroundAgreementTolerance)
                    throw new InvalidOperationException(
                        "Kursa move model Y does not match the other Kursa models. Move=" +
                        Num(moveModel.localPosition.y) + ", Target=" +
                        Num(commonModelY) + ".");
                if (maximumHeadLocalFrameError > 0.05f)
                    throw new InvalidOperationException(
                        "Kursa move face does not match model-local +Z/+Y. MaximumError=" +
                        Num(maximumHeadLocalFrameError) + ".");

                return new Metrics(
                    clip.length,
                    rootHorizontalRange,
                    maximumPositionError,
                    maximumRotationError,
                    maximumScaleError,
                    movingBones,
                    maximumGround - minimumGround,
                    loopPositionError,
                    loopRotationError,
                    moveModel.localPosition.y,
                    commonModelY);
            }
            finally
            {
                foreach (var snapshot in moveSnapshots) snapshot.Restore();
                animator.enabled = animatorEnabled;
                UnityEngine.Object.DestroyImmediate(baked);
                UnityEngine.Object.DestroyImmediate(sourceClone);
            }
        }

        private static void GroundCycle(Transform model, SkinnedMeshRenderer renderer, AnimationClip clip, float targetGround)
        {
            var snapshots = model.GetComponentsInChildren<Transform>(true).Select(item => new TransformState(item)).ToArray();
            var animator = model.GetComponent<Animator>();
            var animatorEnabled = animator != null && animator.enabled;
            var baked = new Mesh();
            var minimum = float.PositiveInfinity;
            try
            {
                if (animator != null) animator.enabled = false;
                var samples = Mathf.CeilToInt(clip.length * FrameRate * 2f);
                for (var index = 0; index <= samples; index++)
                {
                    clip.SampleAnimation(
                        model.gameObject,
                        clip.length * index / samples);
                    renderer.BakeMesh(baked);
                    minimum = Mathf.Min(minimum, MinimumWorldY(renderer, baked));
                }
            }
            finally
            {
                foreach (var snapshot in snapshots) snapshot.Restore();
                if (animator != null) animator.enabled = animatorEnabled;
                UnityEngine.Object.DestroyImmediate(baked);
            }
            var position = model.position;
            position.y += targetGround - minimum;
            model.position = position;
        }

        private static GroundMetrics MeasureOtherKursaGrounds(Transform placement)
        {
            var values = SlotNames.Where(item => item != MoveSlotName)
                .Select(item => new
                {
                    Slot = item,
                    Ground = RequireRenderer(
                        RequireModel(RequireChild(placement, item)),
                        item).bounds.min.y
                }).ToArray();
            var minimum = values.Min(item => item.Ground);
            var maximum = values.Max(item => item.Ground);
            if (maximum - minimum > GroundAgreementTolerance)
                throw new InvalidOperationException(
                    "Other Kursa ground heights do not share one baseline: " +
                    string.Join("|", values.Select(item =>
                        item.Slot + "=" + Num(item.Ground))) + ".");
            return new GroundMetrics(
                values.Average(item => item.Ground),
                minimum,
                maximum);
        }

        private static float MeasureOtherKursaModelY(Transform placement)
        {
            var values = SlotNames.Where(item => item != MoveSlotName)
                .Select(item => new
                {
                    Slot = item,
                    Value = RequireModel(
                        RequireChild(placement, item)).localPosition.y
                }).ToArray();
            var minimum = values.Min(item => item.Value);
            var maximum = values.Max(item => item.Value);
            if (maximum - minimum > GroundAgreementTolerance)
                throw new InvalidOperationException(
                    "Other Kursa model Y positions do not share one value: " +
                    string.Join("|", values.Select(item =>
                        item.Slot + "=" + Num(item.Value))) + ".");
            return values.Average(item => item.Value);
        }

        private static float MeasureCycleMinimumGround(
            Transform model,
            SkinnedMeshRenderer renderer,
            AnimationClip clip,
            float sampleRate)
        {
            var snapshots = model.GetComponentsInChildren<Transform>(true)
                .Select(item => new TransformState(item)).ToArray();
            var animator = model.GetComponent<Animator>();
            var animatorEnabled = animator != null && animator.enabled;
            var baked = new Mesh();
            var minimum = float.PositiveInfinity;
            try
            {
                if (animator != null) animator.enabled = false;
                var samples = Mathf.CeilToInt(clip.length * sampleRate);
                for (var index = 0; index <= samples; index++)
                {
                    clip.SampleAnimation(
                        model.gameObject,
                        clip.length * index / samples);
                    renderer.BakeMesh(baked);
                    minimum = Mathf.Min(
                        minimum,
                        MinimumWorldY(renderer, baked));
                }
            }
            finally
            {
                foreach (var snapshot in snapshots) snapshot.Restore();
                if (animator != null) animator.enabled = animatorEnabled;
                UnityEngine.Object.DestroyImmediate(baked);
            }
            return minimum;
        }

        private static float TriangleDistanceSquared(
            Vector3 a0,
            Vector3 a1,
            Vector3 a2,
            Vector3 b0,
            Vector3 b1,
            Vector3 b2)
        {
            if (SegmentIntersectsTriangle(a0, a1, b0, b1, b2) ||
                SegmentIntersectsTriangle(a1, a2, b0, b1, b2) ||
                SegmentIntersectsTriangle(a2, a0, b0, b1, b2) ||
                SegmentIntersectsTriangle(b0, b1, a0, a1, a2) ||
                SegmentIntersectsTriangle(b1, b2, a0, a1, a2) ||
                SegmentIntersectsTriangle(b2, b0, a0, a1, a2))
                return 0f;
            var result = Mathf.Min(
                PointTriangleDistanceSquared(a0, b0, b1, b2),
                PointTriangleDistanceSquared(a1, b0, b1, b2),
                PointTriangleDistanceSquared(a2, b0, b1, b2),
                PointTriangleDistanceSquared(b0, a0, a1, a2),
                PointTriangleDistanceSquared(b1, a0, a1, a2),
                PointTriangleDistanceSquared(b2, a0, a1, a2));
            var aEdges = new[] { (a0, a1), (a1, a2), (a2, a0) };
            var bEdges = new[] { (b0, b1), (b1, b2), (b2, b0) };
            foreach (var a in aEdges)
            foreach (var b in bEdges)
                result = Mathf.Min(
                    result,
                    SegmentDistanceSquared(a.Item1, a.Item2, b.Item1, b.Item2));
            return result;
        }

        private static bool SegmentIntersectsTriangle(
            Vector3 start,
            Vector3 end,
            Vector3 a,
            Vector3 b,
            Vector3 c)
        {
            var direction = end - start;
            var edge1 = b - a;
            var edge2 = c - a;
            var p = Vector3.Cross(direction, edge2);
            var determinant = Vector3.Dot(edge1, p);
            if (Mathf.Abs(determinant) < 0.00000001f)
                return false;
            var inverse = 1f / determinant;
            var t = start - a;
            var u = Vector3.Dot(t, p) * inverse;
            if (u < 0f || u > 1f)
                return false;
            var q = Vector3.Cross(t, edge1);
            var v = Vector3.Dot(direction, q) * inverse;
            if (v < 0f || u + v > 1f)
                return false;
            var distance = Vector3.Dot(edge2, q) * inverse;
            return distance >= 0f && distance <= 1f;
        }

        private static float PointTriangleDistanceSquared(
            Vector3 point,
            Vector3 a,
            Vector3 b,
            Vector3 c)
        {
            var ab = b - a;
            var ac = c - a;
            var ap = point - a;
            var d1 = Vector3.Dot(ab, ap);
            var d2 = Vector3.Dot(ac, ap);
            if (d1 <= 0f && d2 <= 0f) return ap.sqrMagnitude;
            var bp = point - b;
            var d3 = Vector3.Dot(ab, bp);
            var d4 = Vector3.Dot(ac, bp);
            if (d3 >= 0f && d4 <= d3) return bp.sqrMagnitude;
            var vc = d1 * d4 - d3 * d2;
            if (vc <= 0f && d1 >= 0f && d3 <= 0f)
            {
                var v = d1 / (d1 - d3);
                return (point - (a + v * ab)).sqrMagnitude;
            }
            var cp = point - c;
            var d5 = Vector3.Dot(ab, cp);
            var d6 = Vector3.Dot(ac, cp);
            if (d6 >= 0f && d5 <= d6) return cp.sqrMagnitude;
            var vb = d5 * d2 - d1 * d6;
            if (vb <= 0f && d2 >= 0f && d6 <= 0f)
            {
                var w = d2 / (d2 - d6);
                return (point - (a + w * ac)).sqrMagnitude;
            }
            var va = d3 * d6 - d5 * d4;
            if (va <= 0f && d4 - d3 >= 0f && d5 - d6 >= 0f)
            {
                var w = (d4 - d3) / ((d4 - d3) + (d5 - d6));
                return (point - (b + w * (c - b))).sqrMagnitude;
            }
            var denominator = 1f / (va + vb + vc);
            var faceV = vb * denominator;
            var faceW = vc * denominator;
            return (point - (a + ab * faceV + ac * faceW)).sqrMagnitude;
        }

        private static float SegmentDistanceSquared(
            Vector3 p1,
            Vector3 q1,
            Vector3 p2,
            Vector3 q2)
        {
            var d1 = q1 - p1;
            var d2 = q2 - p2;
            var r = p1 - p2;
            var a = Vector3.Dot(d1, d1);
            var e = Vector3.Dot(d2, d2);
            var f = Vector3.Dot(d2, r);
            float s;
            float t;
            if (a <= 0.00000001f && e <= 0.00000001f)
                return (p1 - p2).sqrMagnitude;
            if (a <= 0.00000001f)
            {
                s = 0f;
                t = Mathf.Clamp01(f / e);
            }
            else
            {
                var c = Vector3.Dot(d1, r);
                if (e <= 0.00000001f)
                {
                    t = 0f;
                    s = Mathf.Clamp01(-c / a);
                }
                else
                {
                    var b = Vector3.Dot(d1, d2);
                    var denominator = a * e - b * b;
                    s = denominator == 0f ? 0f :
                        Mathf.Clamp01((b * f - c * e) / denominator);
                    t = (b * s + f) / e;
                    if (t < 0f)
                    {
                        t = 0f;
                        s = Mathf.Clamp01(-c / a);
                    }
                    else if (t > 1f)
                    {
                        t = 1f;
                        s = Mathf.Clamp01((b - c) / a);
                    }
                }
            }
            var c1 = p1 + d1 * s;
            var c2 = p2 + d2 * t;
            return (c1 - c2).sqrMagnitude;
        }

        private static void RequireBoneOrder(SkinnedMeshRenderer first, SkinnedMeshRenderer second)
        {
            var a = first.bones.Select(item => item == null ? "<null>" : item.name).ToArray();
            var b = second.bones.Select(item => item == null ? "<null>" : item.name).ToArray();
            if (!a.SequenceEqual(b, StringComparer.Ordinal))
                throw new InvalidOperationException("Kursa source, static, and move bone order differs.");
        }

        private static float MinimumWorldY(SkinnedMeshRenderer renderer, Mesh baked)
        {
            var matrix = renderer.localToWorldMatrix;
            return baked.vertices.Min(item => matrix.MultiplyPoint3x4(item).y);
        }

        private static void CaptureStrip(Transform model, AnimationClip clip, string destination)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ?? throw new InvalidOperationException("Invalid capture folder."));
            var snapshots = model.GetComponentsInChildren<Transform>(true).Select(item => new TransformState(item)).ToArray();
            var animator = model.GetComponent<Animator>();
            var animatorEnabled = animator.enabled;
            var otherRenderers = model.gameObject.scene.GetRootGameObjects().SelectMany(item => item.GetComponentsInChildren<Renderer>(true)).Where(item => !item.transform.IsChildOf(model)).Select(item => new RendererState(item)).ToArray();
            var sourceCamera = GameObject.Find("Player")?.GetComponentInChildren<Camera>(true) ?? throw new InvalidOperationException("Player camera is missing.");
            var cameraObject = new GameObject("KursaMoveReviewCamera", typeof(Camera)) { hideFlags = HideFlags.HideAndDontSave };
            const int width = 384;
            const int height = 640;
            var strip = new Texture2D(width * 5, height, TextureFormat.RGB24, false);
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var panel = new Texture2D(width, height, TextureFormat.RGB24, false);
            var oldActive = RenderTexture.active;
            try
            {
                foreach (var item in otherRenderers) item.Renderer.enabled = false;
                animator.enabled = false;
                var camera = cameraObject.GetComponent<Camera>();
                camera.CopyFrom(sourceCamera);
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.14f, 0.15f, 0.17f, 1f);
                camera.cullingMask = ~0;
                camera.fieldOfView = 34f;
                camera.targetTexture = target;
                clip.SampleAnimation(model.gameObject, 0f);
                FrameCamera(camera, model, sourceCamera, width / (float)height);
                for (var index = 0; index < ReviewTimes.Length; index++)
                {
                    clip.SampleAnimation(model.gameObject, clip.length * ReviewTimes[index]);
                    camera.Render();
                    RenderTexture.active = target;
                    panel.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                    panel.Apply();
                    var pixels = panel.GetPixels32();
                    if (pixels.Any(pixel => pixel.r >= 240 && pixel.b >= 240 && pixel.g <= 24))
                        throw new InvalidOperationException("Kursa move review contains Unity magenta fallback.");
                    strip.SetPixels32(index * width, 0, width, height, pixels);
                }
                strip.Apply();
                File.WriteAllBytes(destination, strip.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = oldActive;
                cameraObject.GetComponent<Camera>().targetTexture = null;
                foreach (var item in otherRenderers) item.Restore();
                foreach (var snapshot in snapshots) snapshot.Restore();
                animator.enabled = animatorEnabled;
                UnityEngine.Object.DestroyImmediate(panel);
                UnityEngine.Object.DestroyImmediate(strip);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static void FrameCamera(Camera camera, Transform model, Camera source, float aspect)
        {
            var renderers = model.GetComponentsInChildren<Renderer>(false).Where(item => item.enabled && item.gameObject.activeInHierarchy).ToArray();
            if (renderers.Length == 0) throw new InvalidOperationException("Kursa_03_Move has no visible renderer.");
            var bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++) bounds.Encapsulate(renderers[index].bounds);
            var direction = source.transform.position - bounds.center;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f) direction = Vector3.back;
            direction.Normalize();
            camera.aspect = aspect;
            var vertical = bounds.extents.y / Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f);
            var horizontalFov = 2f * Mathf.Atan(Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f) * aspect);
            var horizontal = Mathf.Max(bounds.extents.x, bounds.extents.z) / Mathf.Tan(horizontalFov * 0.5f);
            var distance = Mathf.Max(vertical, horizontal) * 1.18f;
            camera.transform.position = bounds.center + direction * distance + Vector3.up * bounds.extents.y * 0.02f;
            camera.transform.rotation = Quaternion.LookRotation(bounds.center - camera.transform.position, Vector3.up);
        }

        private static void RequireAnimationSlotContract(Transform placement)
        {
            foreach (var slotName in SlotNames)
            {
                var model = RequireModel(RequireChild(placement, slotName));
                var enabled = model.GetComponentsInChildren<Animator>(true).Where(item => item.enabled).ToArray();
                if (slotName == "Kursa_02_Idle")
                {
                    if (enabled.Length != 1 || AssetDatabase.GetAssetPath(enabled[0].runtimeAnimatorController) != KursaGroundedIdleAnimationTool.ControllerPath)
                        throw new InvalidOperationException("Kursa_02_Idle animation contract differs.");
                }
                else if (slotName == MoveSlotName)
                {
                    if (enabled.Length != 1 || AssetDatabase.GetAssetPath(enabled[0].runtimeAnimatorController) != ControllerPath)
                        throw new InvalidOperationException("Kursa_03_Move animation contract differs.");
                }
                else if (enabled.Length != 0)
                {
                    throw new InvalidOperationException(slotName + " unexpectedly has an enabled Animator.");
                }
            }
        }

        private static Scene RequireScene(bool clean)
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath) throw new InvalidOperationException("Open CargoRunMvp before working on Kursa move.");
            if (clean && scene.isDirty) throw new InvalidOperationException("CargoRunMvp has unsaved changes.");
            return scene;
        }

        private static GameObject RequirePlacement(Scene scene) => scene.GetRootGameObjects().SingleOrDefault(item => item.name == PlacementRootName) ?? throw new InvalidOperationException("Approved Kursa placement is missing.");

        private static void RequireSlotContract(Transform placement)
        {
            if (placement.childCount != SlotNames.Length) throw new InvalidOperationException("Kursa slot count differs.");
            for (var index = 0; index < SlotNames.Length; index++)
            {
                var slot = placement.GetChild(index);
                if (slot.name != SlotNames[index] || slot.childCount != 1 || slot.GetChild(0).name != ModelName)
                    throw new InvalidOperationException("Kursa slot contract differs at " + index + ".");
            }
        }

        private static Transform RequireChild(Transform parent, string name)
        {
            var matches = Enumerable.Range(0, parent.childCount).Select(parent.GetChild).Where(item => item.name == name).ToArray();
            if (matches.Length != 1) throw new InvalidOperationException("Required direct child differs: " + name + ".");
            return matches[0];
        }

        private static Transform RequireModel(Transform slot)
        {
            if (slot.childCount != 1 || slot.GetChild(0).name != ModelName) throw new InvalidOperationException(slot.name + " model contract differs.");
            return slot.GetChild(0);
        }

        private static SkinnedMeshRenderer RequireRenderer(Transform model, string context) => model.GetComponentsInChildren<SkinnedMeshRenderer>(true).SingleOrDefault() ?? throw new InvalidOperationException(context + " must contain one skinned renderer.");

        private static SkinnedMeshRenderer RequireAssetRenderer(
            string assetPath,
            string context)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath) ??
                throw new InvalidOperationException(context + " is missing: " + assetPath + ".");
            return RequireRenderer(asset.transform, context);
        }

        private static string[] OtherSlotSignatures(Transform placement) => SlotNames.Where(item => item != MoveSlotName).Select(item => RecursiveSignature(RequireChild(placement, item))).ToArray();
        private static string[] OtherRootSignatures(Scene scene, GameObject placement) => scene.GetRootGameObjects().Where(item => item != placement).OrderBy(item => item.name, StringComparer.Ordinal).Select(item => RecursiveSignature(item.transform)).ToArray();

        private static string RecursiveSignature(Transform root)
        {
            var builder = new StringBuilder();
            foreach (var item in root.GetComponentsInChildren<Transform>(true))
            {
                builder.Append(item.name).Append('|').Append(item.gameObject.activeSelf).Append('|').Append(Num(item.localPosition.x)).Append(',').Append(Num(item.localPosition.y)).Append(',').Append(Num(item.localPosition.z)).Append('|').Append(Num(item.localRotation.x)).Append(',').Append(Num(item.localRotation.y)).Append(',').Append(Num(item.localRotation.z)).Append(',').Append(Num(item.localRotation.w)).Append('|').Append(Num(item.localScale.x)).Append(',').Append(Num(item.localScale.y)).Append(',').Append(Num(item.localScale.z));
                foreach (var renderer in item.GetComponents<Renderer>())
                {
                    builder.Append("|R:").Append(renderer.enabled);
                    if (renderer is SkinnedMeshRenderer skinned) builder.Append(':').Append(AssetDatabase.GetAssetPath(skinned.sharedMesh));
                    foreach (var material in renderer.sharedMaterials) builder.Append(':').Append(AssetDatabase.GetAssetPath(material));
                }
                foreach (var animator in item.GetComponents<Animator>()) builder.Append("|A:").Append(animator.enabled).Append(':').Append(animator.applyRootMotion).Append(':').Append(AssetDatabase.GetAssetPath(animator.runtimeAnimatorController));
            }
            return builder.ToString();
        }

        private static void RequireEqual(string[] before, string[] after, string message)
        {
            if (!before.SequenceEqual(after, StringComparer.Ordinal)) throw new InvalidOperationException(message);
        }

        private static void WriteReport(Metrics value)
        {
            var destination = Absolute(ReportPath);
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ?? throw new InvalidOperationException("Invalid report folder."));
            File.WriteAllLines(destination, new[]
            {
                "Result=PASS",
                "Target=Approved Kursa Enemy Placement/Kursa_03_Move",
                "Source=enemies model/KUŠkursa walking.fbx",
                "UnitySource=Assets/_Project/Art/Enemies/Kursa/Animations/Models/KUŠkursa walking.fbx",
                "SourceSha256=" + ExpectedMoveSha256,
                "SelectedMixamoTake=Armature|mixamo.com|Layer0",
                "Length=" + Num(value.Length),
                "FrameRate=" + Num(FrameRate),
                "Loop=True",
                "RootMotion=False",
                "RootHorizontalRange=" + Num(value.RootHorizontalRange),
                "MaximumSourcePositionError=" + Num(value.MaximumSourcePositionError),
                "MaximumSourceRotationError=" + Num(value.MaximumSourceRotationError),
                "MaximumSourceScaleError=" + Num(value.MaximumSourceScaleError),
                "MovingBones=" + value.MovingBones,
                "GroundRange=" + Num(value.GroundRange),
                "LoopPositionError=" + Num(value.LoopPositionError),
                "LoopRotationError=" + Num(value.LoopRotationError),
                "MoveModelLocalY=" + Num(value.MoveModelLocalY),
                "OtherKursaCommonModelY=" + Num(value.OtherKursaCommonModelY),
                "ModelLocalForwardHeadAlignment=True",
                "HeadDirectionBasis=HeadToHeadFrontAlignedToModelLocalPositiveZ",
                "HeadUpBasis=HeadToHeadEndAlignedToModelLocalPositiveY",
                "EyeAttachment=PerVertexUvChannelsOnSkinnedFaceMesh",
                "AppearanceMesh=" + AppearanceMeshPath,
                "WalkingVertices=True",
                "WalkingNormals=True",
                "WalkingTangents=True",
                "WalkingUV0=True",
                "WalkingBoneWeights=True",
                "WalkingBindPoses=True",
                "WalkingTriangleData=True",
                "ApprovedSubmeshAssignments=True",
                "StaticSharedMaterials=True",
                "ApprovedEyeUvChannels=True",
                "StaticArmGeometry=False",
                "StaticArmBindPoses=False",
                "BoneSpecificCorrections=ModelLocalForwardHeadPose",
                "AnimationCorrections=HorizontalRootPositionAndModelLocalForwardHeadPose",
                "OtherSlotsUnchanged=True",
                "OtherSceneRootsUnchanged=True"
            }, Encoding.UTF8);
        }

        private static void RequireHash(string assetPath, string expected)
        {
            using var stream = File.OpenRead(Absolute(assetPath));
            using var sha = SHA256.Create();
            var actual = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
            if (!string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Kursa asset hash differs: " + assetPath + ".");
        }

        private static string Absolute(string relative) => Path.GetFullPath(Path.Combine(Application.dataPath, "..", relative));
        private static string Num(float value) => value.ToString("R", CultureInfo.InvariantCulture);

        private readonly struct Metrics
        {
            public readonly float Length;
            public readonly float RootHorizontalRange;
            public readonly float MaximumSourcePositionError;
            public readonly float MaximumSourceRotationError;
            public readonly float MaximumSourceScaleError;
            public readonly int MovingBones;
            public readonly float GroundRange;
            public readonly float LoopPositionError;
            public readonly float LoopRotationError;
            public readonly float MoveModelLocalY;
            public readonly float OtherKursaCommonModelY;

            public Metrics(
                float length,
                float rootHorizontalRange,
                float maximumSourcePositionError,
                float maximumSourceRotationError,
                float maximumSourceScaleError,
                int movingBones,
                float groundRange,
                float loopPositionError,
                float loopRotationError,
                float moveModelLocalY,
                float otherKursaCommonModelY)
            {
                Length = length;
                RootHorizontalRange = rootHorizontalRange;
                MaximumSourcePositionError = maximumSourcePositionError;
                MaximumSourceRotationError = maximumSourceRotationError;
                MaximumSourceScaleError = maximumSourceScaleError;
                MovingBones = movingBones;
                GroundRange = groundRange;
                LoopPositionError = loopPositionError;
                LoopRotationError = loopRotationError;
                MoveModelLocalY = moveModelLocalY;
                OtherKursaCommonModelY = otherKursaCommonModelY;
            }
        }

        private readonly struct GroundMetrics
        {
            public readonly float Common;
            public readonly float Minimum;
            public readonly float Maximum;
            public float Range => Maximum - Minimum;

            public GroundMetrics(float common, float minimum, float maximum)
            {
                Common = common;
                Minimum = minimum;
                Maximum = maximum;
            }
        }

        private sealed class MeshTriangle
        {
            public readonly int Id;
            public readonly int SubMesh;
            public readonly int[] Indices;
            public readonly string Signature;

            public MeshTriangle(
                int id,
                int subMesh,
                int[] indices,
                string signature)
            {
                Id = id;
                SubMesh = subMesh;
                Indices = indices;
                Signature = signature;
            }
        }

        private sealed class TransformKeys
        {
            public readonly List<Keyframe> PositionX = new List<Keyframe>();
            public readonly List<Keyframe> PositionY = new List<Keyframe>();
            public readonly List<Keyframe> PositionZ = new List<Keyframe>();
            public readonly List<Keyframe> RotationX = new List<Keyframe>();
            public readonly List<Keyframe> RotationY = new List<Keyframe>();
            public readonly List<Keyframe> RotationZ = new List<Keyframe>();
            public readonly List<Keyframe> RotationW = new List<Keyframe>();

            public void Add(float time, Vector3 position, Quaternion rotation)
            {
                PositionX.Add(new Keyframe(time, position.x));
                PositionY.Add(new Keyframe(time, position.y));
                PositionZ.Add(new Keyframe(time, position.z));
                RotationX.Add(new Keyframe(time, rotation.x));
                RotationY.Add(new Keyframe(time, rotation.y));
                RotationZ.Add(new Keyframe(time, rotation.z));
                RotationW.Add(new Keyframe(time, rotation.w));
            }
        }

        private readonly struct LocalPose
        {
            public readonly Vector3 Position;
            public readonly Vector3 Scale;
            public readonly Quaternion Rotation;
            public LocalPose(Transform item) { Position = item.localPosition; Scale = item.localScale; Rotation = item.localRotation; }
        }

        private readonly struct TransformState
        {
            private readonly Transform item;
            private readonly Vector3 position;
            private readonly Vector3 scale;
            private readonly Quaternion rotation;
            public TransformState(Transform value) { item = value; position = value.localPosition; scale = value.localScale; rotation = value.localRotation; }
            public void Restore() { if (item == null) return; item.localPosition = position; item.localScale = scale; item.localRotation = rotation; }
        }

        private readonly struct RendererState
        {
            public readonly Renderer Renderer;
            private readonly bool enabled;
            public RendererState(Renderer renderer) { Renderer = renderer; enabled = renderer.enabled; }
            public void Restore() { if (Renderer != null) Renderer.enabled = enabled; }
        }
    }
}
