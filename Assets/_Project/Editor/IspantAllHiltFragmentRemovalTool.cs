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

namespace Bellerophon.Editor.IspantCargoRunScene
{
    internal static class IspantAllHiltFragmentRemovalTool
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementName = "Approved Ispant Enemy Placement";
        private const string BodyName = "char1";
        private const string ReferenceWithHiltPath =
            "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_06_BodyWaistHiltSeparated.asset";
        private const string ReferenceWithoutHiltPath =
            "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_06_BodyWaistHiltRemoved.asset";
        private const string Slot06RecoverySourcePath =
            "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_06_BodyWaistDebrisRemoved.asset";
        private const string OutputMeshFolder =
            "Assets/_Project/Art/Enemies/Ispant/Models";
        private const string ValidationFolder =
            "docs/validation/ispant_all_hilt_fragment_removal_2026-08-25";
        private const string InspectionPath =
            ValidationFolder + "/Ispant_All_HiltFragment_Inspection.txt";
        private const string PreviewPath =
            ValidationFolder + "/Ispant_All_HiltFragment_Preview.png";
        private const string FinalPath =
            ValidationFolder + "/Ispant_All_HiltFragment_Final.png";
        private const string FramePath =
            ValidationFolder + "/Ispant_All_HiltFragment_Frames.txt";
        private const string OutputSuffix = "_AllHiltFragmentRemoved";
        private const int ExpectedSlotCount = 12;
        private const int CaptureLayer = 30;
        private const int PanelWidth = 450;
        private const int PanelHeight = 540;
        private const int SlotsPerRow = 4;
        private const float SignatureScale = 100000f;
        private static readonly int[] Slot06HiltComponentIndices =
            { 554, 565, 569, 573, 575, 576, 579, 580, 582 };

        [MenuItem("Bellerophon/Enemies/Ispant/Inspect All Hilt Fragment Removal")]
        public static void InspectAllIspantHiltFragmentRemoval()
        {
            var scene = RequireActiveScene(false);
            using (new ReadableMeshImportScope(scene))
            {
                var reference = LoadReferenceSelection();
                var contexts = BuildSlotContexts(scene, reference);
                AppendInspection("INSPECT", scene, reference, contexts, null);
                var slotSummary = string.Join(
                    ";",
                    contexts.Select(item =>
                        item.Slot.name + ":Matched=" + item.Selection.TriangleCount +
                        ",Missing=" + item.Selection.MissingReferenceTriangles +
                        ",SharedVertices=" + item.Selection.SharedVertices +
                        ",Total=" + item.Selection.TotalTriangles +
                        ",Mesh=" + AssetDatabase.GetAssetPath(item.SourceMesh)));
                Debug.Log(
                    "IspantAllHiltFragmentRemovalInspected" +
                    ", Slots=" + contexts.Count +
                    ", ReferenceTriangles=" + reference.TriangleCount +
                    ", SceneDirty=" + scene.isDirty +
                    ", SlotSummary=" + slotSummary +
                    ", Report=" + InspectionPath + ".");
            }
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Preview All Hilt Fragment Removal")]
        public static void PreviewAllIspantHiltFragmentRemoval()
        {
            var scene = RequireActiveScene(false);
            var wasDirty = scene.isDirty;
            using (new ReadableMeshImportScope(scene))
            {
                var reference = LoadReferenceSelection();
                var contexts = BuildSlotContexts(scene, reference);
                var frames = CapturePreview(contexts);
                WriteFrames(frames);
                AppendInspection("PREVIEW", scene, reference, contexts, frames);
                Debug.Log(
                    "IspantAllHiltFragmentRemovalPreviewCaptured" +
                    ", Slots=" + contexts.Count +
                    ", Image=" + PreviewPath +
                    ", SceneChanged=False, VisualVerdict=PendingDirectReview.");
            }
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException(
                    "The all-Ispant hilt preview changed the scene dirty state.");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Apply All Hilt Fragment Removal")]
        public static void ApplyAllIspantHiltFragmentRemoval()
        {
            var scene = RequireActiveScene(true);
            using (new ReadableMeshImportScope(scene))
            {
            var reference = LoadReferenceSelection();
            var contexts = BuildSlotContexts(scene, reference);
            RequireTargetPresent(contexts);
            var frames = CalculateCurrentFrames(contexts);
            WriteFrames(frames);

            var outsideSignature = OutsidePlacementSignature(scene);
            var controllers = contexts.ToDictionary(
                item => item.Slot.name,
                item => item.Animator != null ? item.Animator.runtimeAnimatorController : null,
                StringComparer.Ordinal);
            var rootBones = contexts.ToDictionary(
                item => item.Slot.name,
                item => item.Body.rootBone,
                StringComparer.Ordinal);
            var bones = contexts.ToDictionary(
                item => item.Slot.name,
                item => item.Body.bones.ToArray(),
                StringComparer.Ordinal);
            var materials = contexts.ToDictionary(
                item => item.Slot.name,
                item => item.Body.sharedMaterials.ToArray(),
                StringComparer.Ordinal);

            var cleanedBySource = new Dictionary<Mesh, Mesh>();
            var outputPaths = new Dictionary<Mesh, string>();
            foreach (var context in contexts)
            {
                if (!cleanedBySource.TryGetValue(context.SourceMesh, out var cleaned))
                {
                    var outputPath = OutputPathFor(context.SourceMesh);
                    cleaned = CreateOrUpdateCleanedMesh(
                        context.SourceMesh, context.Selection, outputPath);
                    RequireMeshPreserved(context.SourceMesh, cleaned, context.Selection);
                    cleanedBySource.Add(context.SourceMesh, cleaned);
                    outputPaths.Add(context.SourceMesh, outputPath);
                }

                context.Body.sharedMesh = cleaned;
                EditorUtility.SetDirty(context.Body);
            }

            foreach (var context in contexts)
            {
                if (context.Animator != null &&
                    context.Animator.runtimeAnimatorController != controllers[context.Slot.name])
                    throw new InvalidOperationException(
                        context.Slot.name + " Animator controller changed during mesh removal.");
                if (context.Body.rootBone != rootBones[context.Slot.name] ||
                    !context.Body.bones.SequenceEqual(bones[context.Slot.name]))
                    throw new InvalidOperationException(
                        context.Slot.name + " renderer bones changed during mesh removal.");
                if (!context.Body.sharedMaterials.SequenceEqual(materials[context.Slot.name]))
                    throw new InvalidOperationException(
                        context.Slot.name + " renderer materials changed during mesh removal.");
            }

            if (!string.Equals(outsideSignature, OutsidePlacementSignature(scene),
                    StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "A scene root outside the Ispant placement changed during mesh removal.");

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved after all-Ispant hilt removal.");
            AssetDatabase.SaveAssets();

            AppendInspection("APPLY", scene, reference, contexts, frames, outputPaths);
            Debug.Log(
                "IspantAllHiltFragmentRemovalApplied" +
                ", Slots=" + contexts.Count +
                ", UniqueSourceMeshes=" + cleanedBySource.Count +
                ", Scene=" + ScenePath +
                ", AnimationsChanged=False, MaterialsChanged=False, BonesChanged=False.");
            }
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Capture All Hilt Fragment Removal")]
        public static void CaptureAllIspantHiltFragmentRemoval()
        {
            var scene = RequireActiveScene(false);
            var wasDirty = scene.isDirty;
            var reference = LoadReferenceSelection();
            var contexts = BuildSlotContexts(scene, reference);
            foreach (var context in contexts)
            {
                var path = AssetDatabase.GetAssetPath(context.SourceMesh);
                if (!context.SourceMesh.name.EndsWith(OutputSuffix, StringComparison.Ordinal) &&
                    !Path.GetFileNameWithoutExtension(path)
                        .EndsWith(OutputSuffix, StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        context.Slot.name + " does not use an all-hilt-cleaned mesh: " + path);
                if (context.Selection.TriangleCount != 0)
                    throw new InvalidOperationException(
                        context.Slot.name + " still contains " +
                        context.Selection.TriangleCount + " target triangles.");
            }

            var destination = Absolute(FinalPath);
            if (File.Exists(destination))
                throw new InvalidOperationException(
                    "The one-time all-Ispant final capture already exists: " + FinalPath);
            var frames = ReadFrames();
            CaptureFinal(contexts, frames, destination);
            AppendInspection("FINAL_CAPTURE", scene, reference, contexts, frames.Values);
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException(
                    "The all-Ispant final capture changed the scene dirty state.");
            Debug.Log(
                "IspantAllHiltFragmentRemovalFinalCaptured" +
                ", Slots=" + contexts.Count +
                ", RemainingTargetTriangles=0" +
                ", Image=" + FinalPath +
                ", SceneChanged=False, VisualVerdict=PendingDirectReview.");
        }

        private static Scene RequireActiveScene(bool requireClean)
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded ||
                !string.Equals(scene.path, ScenePath, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    "CargoRunMvp must already be the active loaded scene. Current=" + scene.path);
            if (requireClean && scene.isDirty)
                throw new InvalidOperationException(
                    "CargoRunMvp has unsaved editor changes. The hilt removal did not run.");
            return scene;
        }

        private static List<SlotContext> BuildSlotContexts(
            Scene scene, ReferenceSelection reference)
        {
            var placement = scene.GetRootGameObjects()
                .SingleOrDefault(item => item.name == PlacementName) ??
                throw new InvalidOperationException(
                    "The approved Ispant placement root is missing.");
            var slots = placement.transform.Cast<Transform>()
                .Where(item => item.name.StartsWith("Ispant_", StringComparison.Ordinal))
                .OrderBy(item => item.name, StringComparer.Ordinal)
                .ToArray();
            if (slots.Length != ExpectedSlotCount)
                throw new InvalidOperationException(
                    "Expected 12 Ispant slots, found " + slots.Length + ".");

            var contexts = new List<SlotContext>(slots.Length);
            foreach (var slot in slots)
            {
                var bodies = slot.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                    .Where(item => item.name == BodyName && item.sharedMesh != null)
                    .ToArray();
                if (bodies.Length != 1)
                    throw new InvalidOperationException(
                        slot.name + " must contain exactly one usable char1 body, found " +
                        bodies.Length + ".");
                var body = bodies[0];
                var mesh = body.sharedMesh;
                if (!mesh.isReadable)
                    throw new InvalidOperationException(
                        slot.name + " body mesh is not readable: " +
                        AssetDatabase.GetAssetPath(mesh));
                var sourceMesh = mesh;
                var usesSlot06Recovery =
                    slot.name == "Ispant_06_SheathSwordDrawMusket" &&
                    !IsOutputMesh(mesh);
                if (usesSlot06Recovery)
                    sourceMesh = AssetDatabase.LoadAssetAtPath<Mesh>(
                        Slot06RecoverySourcePath) ??
                        throw new InvalidOperationException(
                            "The slot 06 pre-failure recovery body is missing: " +
                            Slot06RecoverySourcePath);
                var selection = usesSlot06Recovery
                    ? SelectComponentIndices(
                        sourceMesh, Slot06HiltComponentIndices, reference)
                    : SelectTriangles(sourceMesh, reference);
                contexts.Add(new SlotContext(
                    slot, body, sourceMesh, FindAnimator(slot), selection));
            }
            return contexts;
        }

        private static bool IsOutputMesh(Mesh mesh)
        {
            var path = AssetDatabase.GetAssetPath(mesh);
            return mesh.name.EndsWith(OutputSuffix, StringComparison.Ordinal) ||
                   Path.GetFileNameWithoutExtension(path)
                       .EndsWith(OutputSuffix, StringComparison.Ordinal);
        }

        private static Animator FindAnimator(Transform slot)
        {
            var animators = slot.GetComponentsInChildren<Animator>(true);
            return animators.Length > 0 ? animators[0] : null;
        }

        private static ReferenceSelection LoadReferenceSelection()
        {
            var withHilt = AssetDatabase.LoadAssetAtPath<Mesh>(ReferenceWithHiltPath) ??
                           throw new InvalidOperationException(
                               "The separated waist-hilt reference mesh is missing: " +
                               ReferenceWithHiltPath);
            var withoutHilt = AssetDatabase.LoadAssetAtPath<Mesh>(ReferenceWithoutHiltPath) ??
                              throw new InvalidOperationException(
                                  "The removed waist-hilt reference mesh is missing: " +
                                  ReferenceWithoutHiltPath);
            if (!withHilt.isReadable || !withoutHilt.isReadable)
                throw new InvalidOperationException(
                    "The waist-hilt reference meshes must be readable.");

            var remaining = TriangleCounts(withoutHilt);
            var target = new Dictionary<TriangleKey, int>();
            var uvTarget = new Dictionary<UvTriangleKey, int>();
            var bounds = new Bounds();
            var hasBounds = false;
            var vertices = withHilt.vertices;
            var uv = withHilt.uv;
            if (uv.Length != vertices.Length)
                throw new InvalidOperationException(
                    "The waist-hilt reference mesh has no complete UV0 data.");
            foreach (var triangle in ReadTriangles(withHilt))
            {
                if (remaining.TryGetValue(triangle.Key, out var count) && count > 0)
                {
                    remaining[triangle.Key] = count - 1;
                    continue;
                }

                target.TryGetValue(triangle.Key, out var targetCount);
                target[triangle.Key] = targetCount + 1;
                var uvKey = new UvTriangleKey(
                    new UvKey(uv[triangle.A]),
                    new UvKey(uv[triangle.B]),
                    new UvKey(uv[triangle.C]));
                uvTarget.TryGetValue(uvKey, out var uvTargetCount);
                uvTarget[uvKey] = uvTargetCount + 1;
                Encapsulate(ref bounds, ref hasBounds, vertices[triangle.A]);
                Encapsulate(ref bounds, ref hasBounds, vertices[triangle.B]);
                Encapsulate(ref bounds, ref hasBounds, vertices[triangle.C]);
            }
            var triangleCount = target.Values.Sum();
            if (triangleCount == 0 || !hasBounds)
                throw new InvalidOperationException(
                    "The waist-hilt reference pair contains no removed triangle difference.");
            return new ReferenceSelection(
                target, uvTarget, triangleCount, bounds);
        }

        private static MeshSelection SelectTriangles(
            Mesh mesh, ReferenceSelection reference)
        {
            var positionSelection = SelectTrianglesByPosition(mesh, reference);
            if (positionSelection.MissingReferenceTriangles == 0)
                return positionSelection;
            var uvSelection = SelectTrianglesByUv(mesh, reference);
            return uvSelection.TriangleCount > positionSelection.TriangleCount
                ? uvSelection
                : positionSelection;
        }

        private static MeshSelection SelectTrianglesByPosition(
            Mesh mesh, ReferenceSelection reference)
        {
            var needed = new Dictionary<TriangleKey, int>(reference.TargetCounts);
            var selected = new Dictionary<int, HashSet<int>>();
            var selectedVertices = new HashSet<int>();
            var keptVertices = new HashSet<int>();
            var vertices = mesh.vertices;
            var bounds = new Bounds();
            var hasBounds = false;
            var selectedCount = 0;

            foreach (var triangle in ReadTriangles(mesh))
            {
                var isSelected = needed.TryGetValue(triangle.Key, out var count) && count > 0;
                if (isSelected)
                {
                    needed[triangle.Key] = count - 1;
                    if (!selected.TryGetValue(triangle.SubMesh, out var ordinals))
                    {
                        ordinals = new HashSet<int>();
                        selected.Add(triangle.SubMesh, ordinals);
                    }
                    ordinals.Add(triangle.Ordinal);
                    selectedVertices.Add(triangle.A);
                    selectedVertices.Add(triangle.B);
                    selectedVertices.Add(triangle.C);
                    Encapsulate(ref bounds, ref hasBounds, vertices[triangle.A]);
                    Encapsulate(ref bounds, ref hasBounds, vertices[triangle.B]);
                    Encapsulate(ref bounds, ref hasBounds, vertices[triangle.C]);
                    selectedCount++;
                }
                else
                {
                    keptVertices.Add(triangle.A);
                    keptVertices.Add(triangle.B);
                    keptVertices.Add(triangle.C);
                }
            }

            var sharedVertices = selectedVertices.Count(item => keptVertices.Contains(item));
            return new MeshSelection(
                selected, selectedVertices, selectedCount,
                needed.Values.Sum(), sharedVertices,
                hasBounds ? bounds : reference.LocalBounds,
                TotalTriangles(mesh));
        }

        private static MeshSelection SelectTrianglesByUv(
            Mesh mesh, ReferenceSelection reference)
        {
            var uv = mesh.uv;
            if (uv.Length != mesh.vertexCount)
                return new MeshSelection(
                    new Dictionary<int, HashSet<int>>(),
                    new HashSet<int>(),
                    0,
                    reference.TriangleCount,
                    0,
                    reference.LocalBounds,
                    TotalTriangles(mesh));

            var needed = new Dictionary<UvTriangleKey, int>(
                reference.UvTargetCounts);
            var selected = new Dictionary<int, HashSet<int>>();
            var selectedVertices = new HashSet<int>();
            var keptVertices = new HashSet<int>();
            var vertices = mesh.vertices;
            var bounds = new Bounds();
            var hasBounds = false;
            var selectedCount = 0;

            foreach (var triangle in ReadTriangles(mesh))
            {
                var key = new UvTriangleKey(
                    new UvKey(uv[triangle.A]),
                    new UvKey(uv[triangle.B]),
                    new UvKey(uv[triangle.C]));
                var isSelected = needed.TryGetValue(key, out var count) && count > 0;
                if (isSelected)
                {
                    needed[key] = count - 1;
                    if (!selected.TryGetValue(triangle.SubMesh, out var ordinals))
                    {
                        ordinals = new HashSet<int>();
                        selected.Add(triangle.SubMesh, ordinals);
                    }
                    ordinals.Add(triangle.Ordinal);
                    selectedVertices.Add(triangle.A);
                    selectedVertices.Add(triangle.B);
                    selectedVertices.Add(triangle.C);
                    Encapsulate(ref bounds, ref hasBounds, vertices[triangle.A]);
                    Encapsulate(ref bounds, ref hasBounds, vertices[triangle.B]);
                    Encapsulate(ref bounds, ref hasBounds, vertices[triangle.C]);
                    selectedCount++;
                }
                else
                {
                    keptVertices.Add(triangle.A);
                    keptVertices.Add(triangle.B);
                    keptVertices.Add(triangle.C);
                }
            }

            var sharedVertices = selectedVertices.Count(
                item => keptVertices.Contains(item));
            return new MeshSelection(
                selected, selectedVertices, selectedCount,
                needed.Values.Sum(), sharedVertices,
                hasBounds ? bounds : reference.LocalBounds,
                TotalTriangles(mesh));
        }

        private static Dictionary<TriangleKey, int> TriangleCounts(Mesh mesh)
        {
            var counts = new Dictionary<TriangleKey, int>();
            foreach (var triangle in ReadTriangles(mesh))
            {
                counts.TryGetValue(triangle.Key, out var count);
                counts[triangle.Key] = count + 1;
            }
            return counts;
        }

        private static IEnumerable<TriangleRecord> ReadTriangles(Mesh mesh)
        {
            var vertices = mesh.vertices;
            for (var subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
            {
                var triangles = mesh.GetTriangles(subMesh);
                for (var index = 0; index < triangles.Length; index += 3)
                {
                    var a = triangles[index];
                    var b = triangles[index + 1];
                    var c = triangles[index + 2];
                    yield return new TriangleRecord(
                        subMesh, index / 3, a, b, c,
                        new TriangleKey(
                            subMesh,
                            new VertexKey(vertices[a]),
                            new VertexKey(vertices[b]),
                            new VertexKey(vertices[c])));
                }
            }
        }

        private static int TotalTriangles(Mesh mesh)
        {
            var count = 0;
            for (var subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
                count += (int)mesh.GetIndexCount(subMesh) / 3;
            return count;
        }

        private static List<ComponentDescriptor> FindComponents(
            Mesh mesh, MeshSelection selection)
        {
            var records = ReadTriangles(mesh)
                .Where(item => selection == null ||
                    selection.SelectedOrdinals.TryGetValue(
                        item.SubMesh, out var ordinals) &&
                    ordinals.Contains(item.Ordinal))
                .ToArray();
            var triangleIndicesByVertex = new Dictionary<int, List<int>>();
            for (var index = 0; index < records.Length; index++)
            {
                AddTriangleIndex(triangleIndicesByVertex, records[index].A, index);
                AddTriangleIndex(triangleIndicesByVertex, records[index].B, index);
                AddTriangleIndex(triangleIndicesByVertex, records[index].C, index);
            }

            var visited = new bool[records.Length];
            var components = new List<ComponentDescriptor>();
            for (var start = 0; start < records.Length; start++)
            {
                if (visited[start]) continue;
                var queue = new Queue<int>();
                var componentVertices = new HashSet<int>();
                var componentTriangles = new List<TriangleRecord>();
                visited[start] = true;
                queue.Enqueue(start);
                while (queue.Count > 0)
                {
                    var current = queue.Dequeue();
                    var triangle = records[current];
                    componentTriangles.Add(triangle);
                    foreach (var vertexIndex in new[] { triangle.A, triangle.B, triangle.C })
                    {
                        componentVertices.Add(vertexIndex);
                        foreach (var neighbor in triangleIndicesByVertex[vertexIndex])
                        {
                            if (visited[neighbor]) continue;
                            visited[neighbor] = true;
                            queue.Enqueue(neighbor);
                        }
                    }
                }
                components.Add(new ComponentDescriptor(
                    componentTriangles, componentVertices));
            }
            return components;
        }

        private static void AddTriangleIndex(
            IDictionary<int, List<int>> indicesByVertex,
            int vertexIndex,
            int triangleIndex)
        {
            if (!indicesByVertex.TryGetValue(vertexIndex, out var indices))
            {
                indices = new List<int>();
                indicesByVertex.Add(vertexIndex, indices);
            }
            indices.Add(triangleIndex);
        }

        private static MeshSelection SelectComponentIndices(
            Mesh mesh,
            IEnumerable<int> componentIndices,
            ReferenceSelection reference)
        {
            var components = FindComponents(mesh, null);
            var indices = new HashSet<int>(componentIndices);
            if (indices.Count == 0 || indices.Any(item => item < 0 || item >= components.Count))
                throw new InvalidOperationException(
                    "The slot 06 hilt component selection is outside the recovery mesh.");

            var selectedOrdinals = new Dictionary<int, HashSet<int>>();
            var selectedVertices = new HashSet<int>();
            var keptVertices = new HashSet<int>();
            var triangleCount = 0;

            for (var componentIndex = 0;
                 componentIndex < components.Count;
                 componentIndex++)
            {
                var component = components[componentIndex];
                if (!indices.Contains(componentIndex))
                {
                    keptVertices.UnionWith(component.VertexIndices);
                    continue;
                }

                selectedVertices.UnionWith(component.VertexIndices);
                triangleCount += component.TriangleCount;
                foreach (var triangle in component.Triangles)
                {
                    if (!selectedOrdinals.TryGetValue(
                            triangle.SubMesh, out var ordinals))
                    {
                        ordinals = new HashSet<int>();
                        selectedOrdinals.Add(triangle.SubMesh, ordinals);
                    }
                    ordinals.Add(triangle.Ordinal);
                }
            }

            if (triangleCount != 120)
                throw new InvalidOperationException(
                    "The slot 06 visual hilt selection must contain 120 triangles, found " +
                    triangleCount + ".");
            var sharedVertices = selectedVertices.Count(
                item => keptVertices.Contains(item));
            if (sharedVertices != 0)
                throw new InvalidOperationException(
                    "The slot 06 visual hilt selection shares vertices with preserved geometry.");
            return new MeshSelection(
                selectedOrdinals,
                selectedVertices,
                triangleCount,
                0,
                sharedVertices,
                reference.LocalBounds,
                TotalTriangles(mesh));
        }

        private static void BakeSourceMesh(
            SlotContext context, Mesh sourceMesh, Mesh destination)
        {
            if (sourceMesh == context.Body.sharedMesh)
            {
                context.Body.BakeMesh(destination);
                return;
            }

            var temporaryObject = HiddenObject(
                context.Slot.name + "_AlternateBodyBake");
            try
            {
                temporaryObject.transform.SetPositionAndRotation(
                    context.Body.transform.position,
                    context.Body.transform.rotation);
                temporaryObject.transform.localScale = context.Body.transform.lossyScale;
                var temporaryRenderer =
                    temporaryObject.AddComponent<SkinnedMeshRenderer>();
                temporaryRenderer.sharedMesh = sourceMesh;
                temporaryRenderer.rootBone = context.Body.rootBone;
                temporaryRenderer.bones = context.Body.bones;
                temporaryRenderer.BakeMesh(destination);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(temporaryObject);
            }
        }

        private static void RequireTargetPresent(IEnumerable<SlotContext> contexts)
        {
            foreach (var context in contexts)
            {
                if (context.Slot.name == "Ispant_04_DrawSword" &&
                    context.Selection.TriangleCount == 0)
                    continue;
                if (context.Selection.TriangleCount == 0)
                    throw new InvalidOperationException(
                        context.Slot.name +
                        " has no triangle matching the approved waist-hilt reference difference.");
                if (context.Selection.MissingReferenceTriangles != 0 ||
                    context.Selection.SharedVertices != 0)
                    throw new InvalidOperationException(
                        context.Slot.name +
                        " has an incomplete or attached waist-hilt selection.");
            }
        }

        private static Mesh CreateOrUpdateCleanedMesh(
            Mesh source, MeshSelection selection, string outputPath)
        {
            var clone = BuildSubsetMesh(
                source, selection, false,
                Path.GetFileNameWithoutExtension(outputPath));
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(outputPath);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(clone, outputPath);
                existing = clone;
            }
            else
            {
                EditorUtility.CopySerialized(clone, existing);
                existing.name = clone.name;
                EditorUtility.SetDirty(existing);
                UnityEngine.Object.DestroyImmediate(clone);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(outputPath, ImportAssetOptions.ForceUpdate);
            return AssetDatabase.LoadAssetAtPath<Mesh>(outputPath) ?? existing;
        }

        private static Mesh BuildSubsetMesh(
            Mesh source, MeshSelection selection, bool keepSelected, string name)
        {
            var result = UnityEngine.Object.Instantiate(source);
            result.name = name;
            for (var subMesh = 0; subMesh < source.subMeshCount; subMesh++)
            {
                var sourceTriangles = source.GetTriangles(subMesh);
                selection.SelectedOrdinals.TryGetValue(subMesh, out var selected);
                var output = new List<int>(sourceTriangles.Length);
                for (var ordinal = 0; ordinal < sourceTriangles.Length / 3; ordinal++)
                {
                    var isSelected = selected != null && selected.Contains(ordinal);
                    if (isSelected != keepSelected)
                        continue;
                    var offset = ordinal * 3;
                    output.Add(sourceTriangles[offset]);
                    output.Add(sourceTriangles[offset + 1]);
                    output.Add(sourceTriangles[offset + 2]);
                }
                result.SetTriangles(output, subMesh, false);
            }
            result.RecalculateBounds();
            return result;
        }

        private static void RequireMeshPreserved(
            Mesh source, Mesh cleaned, MeshSelection selection)
        {
            if (cleaned.vertexCount != source.vertexCount ||
                cleaned.subMeshCount != source.subMeshCount ||
                cleaned.bindposes.Length != source.bindposes.Length ||
                cleaned.blendShapeCount != source.blendShapeCount ||
                TotalTriangles(cleaned) != TotalTriangles(source) - selection.TriangleCount)
                throw new InvalidOperationException(
                    "The cleaned mesh changed data outside the selected target triangles: " +
                    source.name);
            if (!source.bindposes.SequenceEqual(cleaned.bindposes) ||
                !source.boneWeights.SequenceEqual(cleaned.boneWeights))
                throw new InvalidOperationException(
                    "The cleaned mesh changed bind poses or bone weights: " + source.name);
        }

        private static string OutputPathFor(Mesh source)
        {
            var sourcePath = AssetDatabase.GetAssetPath(source);
            var stem = Path.GetFileNameWithoutExtension(sourcePath);
            if (string.IsNullOrWhiteSpace(stem)) stem = source.name;
            if (!string.Equals(stem, source.name, StringComparison.OrdinalIgnoreCase))
                stem += "_" + source.name;
            stem = Sanitize(stem);
            return OutputMeshFolder + "/" + stem + OutputSuffix + ".asset";
        }

        private static string Sanitize(string value)
        {
            foreach (var invalid in Path.GetInvalidFileNameChars())
                value = value.Replace(invalid, '_');
            return value.Replace(' ', '_');
        }

        private static List<CaptureFrame> CapturePreview(List<SlotContext> contexts)
        {
            var rows = Mathf.CeilToInt(contexts.Count / (float)SlotsPerRow);
            const int groups = 3;
            var sheet = new Texture2D(
                PanelWidth * SlotsPerRow * groups,
                PanelHeight * rows,
                TextureFormat.RGB24, false);
            Fill(sheet, new Color32(16, 18, 22, 255));
            var frames = new List<CaptureFrame>(contexts.Count);

            try
            {
                for (var index = 0; index < contexts.Count; index++)
                {
                    var context = contexts[index];
                    var baked = new Mesh { name = context.Slot.name + "_BakedPreview" };
                    BakeSourceMesh(context, context.SourceMesh, baked);
                    var frame = CalculateFrame(context, baked);
                    frames.Add(frame);
                    var after = BuildSubsetMesh(
                        baked, context.Selection, false, context.Slot.name + "_AfterPreview");
                    var selected = BuildSubsetMesh(
                        baked, context.Selection, true, context.Slot.name + "_SelectedPreview");
                    OffsetAlongNormals(selected, 0.0025f);
                    var column = index % SlotsPerRow;
                    var row = rows - 1 - index / SlotsPerRow;
                    RenderPanel(
                        context, baked, null, frame, sheet,
                        (column * groups) * PanelWidth, row * PanelHeight);
                    RenderPanel(
                        context, baked, selected, frame, sheet,
                        (column * groups + 1) * PanelWidth, row * PanelHeight);
                    RenderPanel(
                        context, after, null, frame, sheet,
                        (column * groups + 2) * PanelWidth, row * PanelHeight);
                    UnityEngine.Object.DestroyImmediate(after);
                    UnityEngine.Object.DestroyImmediate(selected);
                    UnityEngine.Object.DestroyImmediate(baked);
                }
                sheet.Apply();
                var destination = Absolute(PreviewPath);
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
                File.WriteAllBytes(destination, sheet.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sheet);
            }
            return frames;
        }

        private static void CaptureFinal(
            List<SlotContext> contexts,
            IReadOnlyDictionary<string, CaptureFrame> frames,
            string destination)
        {
            var rows = Mathf.CeilToInt(contexts.Count / (float)SlotsPerRow);
            var sheet = new Texture2D(
                PanelWidth * SlotsPerRow,
                PanelHeight * rows,
                TextureFormat.RGB24, false);
            Fill(sheet, new Color32(16, 18, 22, 255));
            try
            {
                for (var index = 0; index < contexts.Count; index++)
                {
                    var context = contexts[index];
                    if (!frames.TryGetValue(context.Slot.name, out var frame))
                        throw new InvalidOperationException(
                            "No preview frame was recorded for " + context.Slot.name + ".");
                    var baked = new Mesh { name = context.Slot.name + "_BakedFinal" };
                    context.Body.BakeMesh(baked);
                    var column = index % SlotsPerRow;
                    var row = rows - 1 - index / SlotsPerRow;
                    RenderPanel(
                        context, baked, null, frame, sheet,
                        column * PanelWidth, row * PanelHeight);
                    UnityEngine.Object.DestroyImmediate(baked);
                }
                sheet.Apply();
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
                File.WriteAllBytes(destination, sheet.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sheet);
            }
        }

        private static List<CaptureFrame> CalculateCurrentFrames(
            IEnumerable<SlotContext> contexts)
        {
            var frames = new List<CaptureFrame>();
            foreach (var context in contexts)
            {
                var baked = new Mesh { name = context.Slot.name + "_FrameBake" };
                BakeSourceMesh(context, context.SourceMesh, baked);
                frames.Add(CalculateFrame(context, baked));
                UnityEngine.Object.DestroyImmediate(baked);
            }
            return frames;
        }

        private static CaptureFrame CalculateFrame(SlotContext context, Mesh baked)
        {
            var vertices = baked.vertices;
            var bounds = new Bounds();
            var hasBounds = false;
            if (context.Slot.name != "Ispant_06_SheathSwordDrawMusket")
            foreach (var index in context.Selection.VertexIndices)
            {
                if (index < 0 || index >= vertices.Length) continue;
                Encapsulate(
                    ref bounds, ref hasBounds,
                    context.Body.transform.TransformPoint(vertices[index]));
            }
            if (!hasBounds)
            {
                var local = context.Selection.LocalBounds;
                for (var x = -1; x <= 1; x += 2)
                for (var y = -1; y <= 1; y += 2)
                for (var z = -1; z <= 1; z += 2)
                {
                    var corner = local.center + Vector3.Scale(
                        local.extents, new Vector3(x, y, z));
                    Encapsulate(
                        ref bounds, ref hasBounds,
                        context.Body.transform.TransformPoint(corner));
                }
            }
            if (!hasBounds)
                throw new InvalidOperationException(
                    "No posed target bounds could be calculated for " + context.Slot.name + ".");

            var aspect = PanelWidth / (float)PanelHeight;
            var orthographicSize = Mathf.Max(
                0.75f,
                bounds.extents.y * 1.65f,
                bounds.extents.x / aspect * 1.65f);
            var direction = (context.Slot.forward - context.Slot.right * 0.24f +
                             context.Slot.up * 0.04f).normalized;
            return new CaptureFrame(
                context.Slot.name,
                bounds.center + context.Slot.up * 0.04f,
                orthographicSize,
                direction,
                context.Slot.up);
        }

        private static void RenderPanel(
            SlotContext context,
            Mesh mainMesh,
            Mesh overlayMesh,
            CaptureFrame frame,
            Texture2D sheet,
            int destinationX,
            int destinationY)
        {
            var target = new RenderTexture(
                PanelWidth, PanelHeight, 24, RenderTextureFormat.ARGB32);
            var panel = new Texture2D(
                PanelWidth, PanelHeight, TextureFormat.RGB24, false);
            var cameraObject = HiddenObject(context.Slot.name + "_HiltReviewCamera");
            var camera = cameraObject.AddComponent<Camera>();
            var keyObject = HiddenObject(context.Slot.name + "_HiltReviewKey");
            var key = keyObject.AddComponent<Light>();
            var fillObject = HiddenObject(context.Slot.name + "_HiltReviewFill");
            var fill = fillObject.AddComponent<Light>();
            var mainObject = CreateMeshObject(
                context.Slot.name + "_HiltReviewMain",
                mainMesh,
                NormalizeMaterials(context.Body.sharedMaterials, mainMesh.subMeshCount),
                context.Body.transform);
            var auxiliaryObjects = new List<GameObject>();
            var auxiliaryMeshes = new List<Mesh>();
            foreach (var renderer in context.Slot.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (renderer == context.Body || !renderer.enabled ||
                    !renderer.gameObject.activeInHierarchy || renderer.sharedMesh == null)
                    continue;
                var baked = new Mesh
                {
                    name = renderer.name + "_HiltReviewBaked"
                };
                renderer.BakeMesh(baked);
                auxiliaryMeshes.Add(baked);
                auxiliaryObjects.Add(CreateMeshObject(
                    renderer.name + "_HiltReviewAuxiliary",
                    baked,
                    NormalizeMaterials(renderer.sharedMaterials, baked.subMeshCount),
                    renderer.transform));
            }
            foreach (var filter in context.Slot.GetComponentsInChildren<MeshFilter>(true))
            {
                var renderer = filter.GetComponent<MeshRenderer>();
                if (renderer == null || !renderer.enabled ||
                    !renderer.gameObject.activeInHierarchy || filter.sharedMesh == null)
                    continue;
                auxiliaryObjects.Add(CreateMeshObject(
                    filter.name + "_HiltReviewAuxiliary",
                    filter.sharedMesh,
                    NormalizeMaterials(
                        renderer.sharedMaterials, filter.sharedMesh.subMeshCount),
                    filter.transform));
            }
            GameObject overlayObject = null;
            Material overlayMaterial = null;
            if (overlayMesh != null)
            {
                overlayMaterial = CreateOverlayMaterial();
                overlayObject = CreateMeshObject(
                    context.Slot.name + "_HiltReviewSelection",
                    overlayMesh,
                    Enumerable.Repeat(overlayMaterial, overlayMesh.subMeshCount).ToArray(),
                    context.Body.transform);
            }

            var oldActive = RenderTexture.active;
            try
            {
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.055f, 0.063f, 0.075f, 1f);
                camera.orthographic = true;
                camera.orthographicSize = frame.OrthographicSize;
                camera.aspect = PanelWidth / (float)PanelHeight;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 100f;
                camera.cullingMask = 1 << CaptureLayer;
                camera.targetTexture = target;
                camera.transform.position = frame.Center + frame.Direction * 6f;
                camera.transform.rotation = Quaternion.LookRotation(
                    frame.Center - camera.transform.position, frame.Up);

                key.type = LightType.Directional;
                key.intensity = 1.25f;
                key.color = new Color(1f, 0.95f, 0.88f);
                key.cullingMask = 1 << CaptureLayer;
                key.transform.rotation = Quaternion.LookRotation(
                    -frame.Direction - frame.Up * 0.45f, frame.Up);
                fill.type = LightType.Directional;
                fill.intensity = 0.7f;
                fill.color = new Color(0.68f, 0.78f, 1f);
                fill.cullingMask = 1 << CaptureLayer;
                fill.transform.rotation = Quaternion.LookRotation(
                    frame.Direction - frame.Up * 0.15f, frame.Up);

                camera.Render();
                RenderTexture.active = target;
                panel.ReadPixels(new Rect(0f, 0f, PanelWidth, PanelHeight), 0, 0);
                panel.Apply();
                sheet.SetPixels32(
                    destinationX, destinationY,
                    PanelWidth, PanelHeight,
                    panel.GetPixels32());
            }
            finally
            {
                RenderTexture.active = oldActive;
                camera.targetTexture = null;
                UnityEngine.Object.DestroyImmediate(mainObject);
                foreach (var item in auxiliaryObjects)
                    UnityEngine.Object.DestroyImmediate(item);
                foreach (var item in auxiliaryMeshes)
                    UnityEngine.Object.DestroyImmediate(item);
                if (overlayObject != null) UnityEngine.Object.DestroyImmediate(overlayObject);
                if (overlayMaterial != null) UnityEngine.Object.DestroyImmediate(overlayMaterial);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(keyObject);
                UnityEngine.Object.DestroyImmediate(fillObject);
                UnityEngine.Object.DestroyImmediate(panel);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        private static GameObject CreateMeshObject(
            string name, Mesh mesh, Material[] materials, Transform sourceTransform)
        {
            var item = HiddenObject(name);
            item.layer = CaptureLayer;
            item.transform.SetPositionAndRotation(
                sourceTransform.position, sourceTransform.rotation);
            item.transform.localScale = sourceTransform.lossyScale;
            item.AddComponent<MeshFilter>().sharedMesh = mesh;
            item.AddComponent<MeshRenderer>().sharedMaterials = materials;
            return item;
        }

        private static GameObject HiddenObject(string name)
        {
            var item = new GameObject(name)
            {
                hideFlags = HideFlags.HideAndDontSave,
                layer = CaptureLayer
            };
            return item;
        }

        private static Material[] NormalizeMaterials(Material[] source, int count)
        {
            if (source.Length == count) return source;
            if (source.Length == 0)
                throw new InvalidOperationException("The Ispant body has no material.");
            var result = new Material[count];
            for (var index = 0; index < count; index++)
                result[index] = source[Mathf.Min(index, source.Length - 1)];
            return result;
        }

        private static Material CreateOverlayMaterial()
        {
            var shader = Shader.Find("Unlit/Color") ?? Shader.Find("Standard") ??
                         throw new InvalidOperationException(
                             "No shader is available for the hilt selection overlay.");
            var material = new Material(shader)
            {
                name = "IspantHiltSelectionOverlay",
                hideFlags = HideFlags.HideAndDontSave,
                color = new Color(1f, 0.02f, 0.02f, 1f)
            };
            return material;
        }

        private static void OffsetAlongNormals(Mesh mesh, float amount)
        {
            var vertices = mesh.vertices;
            var normals = mesh.normals;
            if (normals.Length != vertices.Length) return;
            for (var index = 0; index < vertices.Length; index++)
                vertices[index] += normals[index] * amount;
            mesh.vertices = vertices;
            mesh.RecalculateBounds();
        }

        private static void Fill(Texture2D texture, Color32 color)
        {
            var pixels = new Color32[texture.width * texture.height];
            for (var index = 0; index < pixels.Length; index++) pixels[index] = color;
            texture.SetPixels32(pixels);
        }

        private static void WriteFrames(IEnumerable<CaptureFrame> frames)
        {
            var builder = new StringBuilder();
            foreach (var frame in frames)
            {
                builder.Append(frame.SlotName).Append('|')
                    .Append(Vec(frame.Center)).Append('|')
                    .Append(Num(frame.OrthographicSize)).Append('|')
                    .Append(Vec(frame.Direction)).Append('|')
                    .Append(Vec(frame.Up)).AppendLine();
            }
            var destination = Absolute(FramePath);
            Directory.CreateDirectory(Path.GetDirectoryName(destination));
            File.WriteAllText(destination, builder.ToString(), Encoding.UTF8);
        }

        private static Dictionary<string, CaptureFrame> ReadFrames()
        {
            var path = Absolute(FramePath);
            if (!File.Exists(path))
                throw new InvalidOperationException(
                    "The approved preview frame record is missing: " + FramePath);
            var result = new Dictionary<string, CaptureFrame>(StringComparer.Ordinal);
            foreach (var line in File.ReadAllLines(path, Encoding.UTF8))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var values = line.Split('|');
                if (values.Length != 5)
                    throw new InvalidOperationException("Invalid preview frame: " + line);
                result.Add(
                    values[0],
                    new CaptureFrame(
                        values[0], ParseVector(values[1]), Parse(values[2]),
                        ParseVector(values[3]), ParseVector(values[4])));
            }
            return result;
        }

        private static void AppendInspection(
            string phase,
            Scene scene,
            ReferenceSelection reference,
            IEnumerable<SlotContext> contexts,
            IEnumerable<CaptureFrame> frames,
            IReadOnlyDictionary<Mesh, string> outputPaths = null)
        {
            var frameMap = frames != null
                ? frames.ToDictionary(item => item.SlotName, StringComparer.Ordinal)
                : new Dictionary<string, CaptureFrame>(StringComparer.Ordinal);
            var builder = new StringBuilder();
            builder.AppendLine("=== " + phase + " " +
                               DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) + " ===");
            builder.AppendLine("Scene=" + scene.path);
            builder.AppendLine("SceneDirty=" + scene.isDirty);
            builder.AppendLine("ReferenceWithHilt=" + ReferenceWithHiltPath);
            builder.AppendLine("ReferenceWithoutHilt=" + ReferenceWithoutHiltPath);
            builder.AppendLine("ReferenceTargetTriangles=" + reference.TriangleCount);
            builder.AppendLine("ReferenceLocalBounds=" + BoundsText(reference.LocalBounds));
            foreach (var context in contexts)
            {
                builder.Append("Slot=").Append(context.Slot.name)
                    .Append("|Mesh=").Append(AssetDatabase.GetAssetPath(context.SourceMesh))
                    .Append("|MeshName=").Append(context.SourceMesh.name)
                    .Append("|Vertices=").Append(context.SourceMesh.vertexCount)
                    .Append("|Triangles=").Append(context.Selection.TotalTriangles)
                    .Append("|MatchedTargetTriangles=").Append(context.Selection.TriangleCount)
                    .Append("|MissingReferenceTriangles=").Append(context.Selection.MissingReferenceTriangles)
                    .Append("|SelectedVertices=").Append(context.Selection.VertexIndices.Count)
                    .Append("|SharedVerticesWithKeptFaces=").Append(context.Selection.SharedVertices)
                    .Append("|Animator=").Append(
                        context.Animator != null
                            ? AssetDatabase.GetAssetPath(context.Animator.runtimeAnimatorController)
                            : "None");
                if (outputPaths != null && outputPaths.TryGetValue(context.SourceMesh, out var output))
                    builder.Append("|OutputMesh=").Append(output);
                if (frameMap.TryGetValue(context.Slot.name, out var frame))
                    builder.Append("|CaptureCenter=").Append(Vec(frame.Center))
                        .Append("|CaptureOrtho=").Append(Num(frame.OrthographicSize));
                builder.AppendLine();
            }
            builder.AppendLine();
            var destination = Absolute(InspectionPath);
            Directory.CreateDirectory(Path.GetDirectoryName(destination));
            File.AppendAllText(destination, builder.ToString(), Encoding.UTF8);
        }

        private static string OutsidePlacementSignature(Scene scene)
        {
            var builder = new StringBuilder();
            foreach (var root in scene.GetRootGameObjects()
                         .Where(item => item.name != PlacementName)
                         .OrderBy(item => item.name, StringComparer.Ordinal))
            {
                foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                {
                    builder.Append(AnimationUtility.CalculateTransformPath(transform, root.transform))
                        .Append('|').Append(transform.gameObject.activeSelf)
                        .Append('|').Append(Vec(transform.localPosition))
                        .Append('|').Append(Quat(transform.localRotation))
                        .Append('|').Append(Vec(transform.localScale)).AppendLine();
                }
            }
            return builder.ToString();
        }

        private static string Absolute(string projectRelative)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", projectRelative));
        }

        private static void Encapsulate(
            ref Bounds bounds, ref bool hasBounds, Vector3 point)
        {
            if (!hasBounds)
            {
                bounds = new Bounds(point, Vector3.zero);
                hasBounds = true;
            }
            else bounds.Encapsulate(point);
        }

        private static string Vec(Vector3 value)
        {
            return Num(value.x) + "," + Num(value.y) + "," + Num(value.z);
        }

        private static string Quat(Quaternion value)
        {
            return Num(value.x) + "," + Num(value.y) + "," +
                   Num(value.z) + "," + Num(value.w);
        }

        private static string Num(float value)
        {
            return value.ToString("0.######", CultureInfo.InvariantCulture);
        }

        private static float Parse(string value)
        {
            return float.Parse(value, CultureInfo.InvariantCulture);
        }

        private static Vector3 ParseVector(string value)
        {
            var items = value.Split(',');
            if (items.Length != 3)
                throw new InvalidOperationException("Invalid vector: " + value);
            return new Vector3(Parse(items[0]), Parse(items[1]), Parse(items[2]));
        }

        private static string BoundsText(Bounds value)
        {
            return "Center=" + Vec(value.center) + "|Size=" + Vec(value.size);
        }

        private sealed class ReadableMeshImportScope : IDisposable
        {
            private readonly List<ReadableState> readableStates = new List<ReadableState>();
            private bool disposed;

            public ReadableMeshImportScope(Scene scene)
            {
                try
                {
                    var paths = scene.GetRootGameObjects()
                        .Where(item => item.name == PlacementName)
                        .SelectMany(item => item.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                        .Where(item => item.name == BodyName && item.sharedMesh != null &&
                                       !item.sharedMesh.isReadable)
                        .Select(item => AssetDatabase.GetAssetPath(item.sharedMesh))
                        .Concat(new[] { Slot06RecoverySourcePath }.Where(path =>
                        {
                            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                            return mesh != null && !mesh.isReadable;
                        }))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(item => item, StringComparer.OrdinalIgnoreCase)
                        .ToArray();

                    foreach (var path in paths)
                    {
                        var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                        if (importer != null)
                        {
                            readableStates.Add(
                                new ReadableState(path, importer.isReadable, true));
                            if (importer.isReadable) continue;
                            importer.isReadable = true;
                            importer.SaveAndReimport();
                            continue;
                        }

                        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                        if (mesh == null)
                            throw new InvalidOperationException(
                                "A non-readable Ispant mesh asset could not be loaded: " + path);
                        var serializedMesh = new SerializedObject(mesh);
                        var readableProperty = serializedMesh.FindProperty("m_IsReadable");
                        if (readableProperty == null)
                            throw new InvalidOperationException(
                                "A non-readable Ispant mesh has no serialized readability flag: " +
                                path);
                        readableStates.Add(
                            new ReadableState(path, readableProperty.boolValue, false));
                        if (readableProperty.boolValue) continue;
                        readableProperty.boolValue = true;
                        serializedMesh.ApplyModifiedPropertiesWithoutUndo();
                        EditorUtility.SetDirty(mesh);
                        AssetDatabase.SaveAssetIfDirty(mesh);
                        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                    }
                }
                catch
                {
                    RestoreReadabilityStates();
                    throw;
                }
            }

            public void Dispose()
            {
                if (disposed) return;
                disposed = true;
                RestoreReadabilityStates();
            }

            private void RestoreReadabilityStates()
            {
                for (var index = readableStates.Count - 1; index >= 0; index--)
                {
                    var state = readableStates[index];
                    if (state.UsesModelImporter)
                    {
                        var importer = AssetImporter.GetAtPath(state.Path) as ModelImporter;
                        if (importer == null || importer.isReadable == state.WasReadable) continue;
                        importer.isReadable = state.WasReadable;
                        importer.SaveAndReimport();
                        continue;
                    }

                    var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(state.Path);
                    if (mesh == null) continue;
                    var serializedMesh = new SerializedObject(mesh);
                    var readableProperty = serializedMesh.FindProperty("m_IsReadable");
                    if (readableProperty == null ||
                        readableProperty.boolValue == state.WasReadable) continue;
                    readableProperty.boolValue = state.WasReadable;
                    serializedMesh.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(mesh);
                    AssetDatabase.SaveAssetIfDirty(mesh);
                    AssetDatabase.ImportAsset(state.Path, ImportAssetOptions.ForceUpdate);
                }
            }

            private readonly struct ReadableState
            {
                public ReadableState(
                    string path, bool wasReadable, bool usesModelImporter)
                {
                    Path = path;
                    WasReadable = wasReadable;
                    UsesModelImporter = usesModelImporter;
                }

                public string Path { get; }
                public bool WasReadable { get; }
                public bool UsesModelImporter { get; }
            }
        }

        private sealed class SlotContext
        {
            public SlotContext(
                Transform slot,
                SkinnedMeshRenderer body,
                Mesh sourceMesh,
                Animator animator,
                MeshSelection selection)
            {
                Slot = slot;
                Body = body;
                SourceMesh = sourceMesh;
                Animator = animator;
                Selection = selection;
            }

            public Transform Slot { get; }
            public SkinnedMeshRenderer Body { get; }
            public Mesh SourceMesh { get; }
            public Animator Animator { get; }
            public MeshSelection Selection { get; }
        }

        private sealed class ComponentDescriptor
        {
            public ComponentDescriptor(
                List<TriangleRecord> triangles,
                HashSet<int> vertexIndices)
            {
                Triangles = triangles;
                VertexIndices = vertexIndices;
            }

            public List<TriangleRecord> Triangles { get; }
            public HashSet<int> VertexIndices { get; }
            public int TriangleCount => Triangles.Count;
            public int VertexCount => VertexIndices.Count;
        }

        private sealed class ReferenceSelection
        {
            public ReferenceSelection(
                Dictionary<TriangleKey, int> targetCounts,
                Dictionary<UvTriangleKey, int> uvTargetCounts,
                int triangleCount,
                Bounds localBounds)
            {
                TargetCounts = targetCounts;
                UvTargetCounts = uvTargetCounts;
                TriangleCount = triangleCount;
                LocalBounds = localBounds;
            }

            public Dictionary<TriangleKey, int> TargetCounts { get; }
            public Dictionary<UvTriangleKey, int> UvTargetCounts { get; }
            public int TriangleCount { get; }
            public Bounds LocalBounds { get; }
        }

        private sealed class MeshSelection
        {
            public MeshSelection(
                Dictionary<int, HashSet<int>> selectedOrdinals,
                HashSet<int> vertexIndices,
                int triangleCount,
                int missingReferenceTriangles,
                int sharedVertices,
                Bounds localBounds,
                int totalTriangles)
            {
                SelectedOrdinals = selectedOrdinals;
                VertexIndices = vertexIndices;
                TriangleCount = triangleCount;
                MissingReferenceTriangles = missingReferenceTriangles;
                SharedVertices = sharedVertices;
                LocalBounds = localBounds;
                TotalTriangles = totalTriangles;
            }

            public Dictionary<int, HashSet<int>> SelectedOrdinals { get; }
            public HashSet<int> VertexIndices { get; }
            public int TriangleCount { get; }
            public int MissingReferenceTriangles { get; }
            public int SharedVertices { get; }
            public Bounds LocalBounds { get; }
            public int TotalTriangles { get; }
        }

        private readonly struct TriangleRecord
        {
            public TriangleRecord(
                int subMesh, int ordinal, int a, int b, int c, TriangleKey key)
            {
                SubMesh = subMesh;
                Ordinal = ordinal;
                A = a;
                B = b;
                C = c;
                Key = key;
            }

            public int SubMesh { get; }
            public int Ordinal { get; }
            public int A { get; }
            public int B { get; }
            public int C { get; }
            public TriangleKey Key { get; }
        }

        private readonly struct VertexKey : IEquatable<VertexKey>, IComparable<VertexKey>
        {
            public VertexKey(Vector3 value)
            {
                X = Mathf.RoundToInt(value.x * SignatureScale);
                Y = Mathf.RoundToInt(value.y * SignatureScale);
                Z = Mathf.RoundToInt(value.z * SignatureScale);
            }

            private int X { get; }
            private int Y { get; }
            private int Z { get; }

            public int CompareTo(VertexKey other)
            {
                var result = X.CompareTo(other.X);
                if (result != 0) return result;
                result = Y.CompareTo(other.Y);
                return result != 0 ? result : Z.CompareTo(other.Z);
            }

            public bool Equals(VertexKey other)
            {
                return X == other.X && Y == other.Y && Z == other.Z;
            }

            public override bool Equals(object obj)
            {
                return obj is VertexKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hash = X;
                    hash = hash * 397 ^ Y;
                    return hash * 397 ^ Z;
                }
            }
        }

        private readonly struct TriangleKey : IEquatable<TriangleKey>
        {
            public TriangleKey(int subMesh, VertexKey a, VertexKey b, VertexKey c)
            {
                var values = new[] { a, b, c };
                Array.Sort(values);
                A = values[0];
                B = values[1];
                C = values[2];
            }

            private VertexKey A { get; }
            private VertexKey B { get; }
            private VertexKey C { get; }

            public bool Equals(TriangleKey other)
            {
                return A.Equals(other.A) && B.Equals(other.B) && C.Equals(other.C);
            }

            public override bool Equals(object obj)
            {
                return obj is TriangleKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hash = A.GetHashCode();
                    hash = hash * 397 ^ B.GetHashCode();
                    return hash * 397 ^ C.GetHashCode();
                }
            }
        }

        private readonly struct UvKey : IEquatable<UvKey>, IComparable<UvKey>
        {
            public UvKey(Vector2 value)
            {
                X = Mathf.RoundToInt(value.x * SignatureScale);
                Y = Mathf.RoundToInt(value.y * SignatureScale);
            }

            private int X { get; }
            private int Y { get; }

            public int CompareTo(UvKey other)
            {
                var result = X.CompareTo(other.X);
                return result != 0 ? result : Y.CompareTo(other.Y);
            }

            public bool Equals(UvKey other)
            {
                return X == other.X && Y == other.Y;
            }

            public override bool Equals(object obj)
            {
                return obj is UvKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return X * 397 ^ Y;
                }
            }
        }

        private readonly struct UvTriangleKey : IEquatable<UvTriangleKey>
        {
            public UvTriangleKey(UvKey a, UvKey b, UvKey c)
            {
                var values = new[] { a, b, c };
                Array.Sort(values);
                A = values[0];
                B = values[1];
                C = values[2];
            }

            private UvKey A { get; }
            private UvKey B { get; }
            private UvKey C { get; }

            public bool Equals(UvTriangleKey other)
            {
                return A.Equals(other.A) && B.Equals(other.B) && C.Equals(other.C);
            }

            public override bool Equals(object obj)
            {
                return obj is UvTriangleKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hash = A.GetHashCode();
                    hash = hash * 397 ^ B.GetHashCode();
                    return hash * 397 ^ C.GetHashCode();
                }
            }
        }

        private readonly struct CaptureFrame
        {
            public CaptureFrame(
                string slotName,
                Vector3 center,
                float orthographicSize,
                Vector3 direction,
                Vector3 up)
            {
                SlotName = slotName;
                Center = center;
                OrthographicSize = orthographicSize;
                Direction = direction;
                Up = up;
            }

            public string SlotName { get; }
            public Vector3 Center { get; }
            public float OrthographicSize { get; }
            public Vector3 Direction { get; }
            public Vector3 Up { get; }
        }
    }

    internal static class IspantLeftWaistHiltCorrectionTool
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementName = "Approved Ispant Enemy Placement";
        private const string BodyName = "char1";
        private const string BaseSourcePath =
            "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_New_Direct_Source.fbx";
        private const string DrawSourcePath =
            "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_New_DrawSword_Body.asset";
        private const string Slot06SourcePath =
            "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_06_BodyWaistDebrisRemoved.asset";
        private const string ReferenceWithHiltPath =
            "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_06_BodyWaistHiltSeparated.asset";
        private const string ReferenceWithoutHiltPath =
            "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_06_BodyWaistHiltRemoved.asset";
        private const string ValidationFolder =
            "docs/validation/ispant_all_left_waist_hilt_correction_2026-08-25";
        private const string InspectionPath = ValidationFolder + "/Inspection.txt";
        private const string NonSlot06PreviewPath =
            ValidationFolder + "/TargetMask_Slots1To5_7To12.png";
        private const string Slot06PreviewPath =
            ValidationFolder + "/TargetMask_Slot6.png";
        private const string FinalComparisonPath =
            ValidationFolder + "/FinalComparison.png";
        private const string RetryValidationFolder =
            "docs/validation/ispant_all_left_waist_hilt_correction_retry_2026-08-25";
        private const string RetryInspectionPath =
            RetryValidationFolder + "/Inspection.txt";
        private const string RetryCurrentInspectionPath =
            RetryValidationFolder + "/CurrentStateInspection.txt";
        private const string RetryDirectInspection01Path =
            RetryValidationFolder + "/PlacedScene_DirectInspection_01.png";
        private const string RetryDirectInspection02Path =
            RetryValidationFolder + "/PlacedScene_DirectInspection_02.png";
        private const string RetryFinalPath =
            RetryValidationFolder + "/PlacedScene_Final.png";
        private const string CorrectedBodySuffix =
            "_BodyLeftWaistHiltCorrected.asset";
        private const string Slot06Name = "Ispant_06_SheathSwordDrawMusket";
        private const string Slot04Name = "Ispant_04_DrawSword";
        private const string Slot05Name = "Ispant_05_RunningOneHandedSwordAttack";
        private const string WaistSwordPath =
            "Ispant_New_Direct_Model/Armature/Ispant_Approved_LongSword_10K";
        private const string Slot06HandSwordPath =
            "Ispant_New_Direct_Model/Ispant_06_LegacyHandSword";
        private const string Slot06ClipPath =
            "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_06_New_SheathingSword_Loop.anim";
        private const string Slot06DamagedBodyPath =
            "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_06_BodyWaistDebrisRemoved_AllHiltFragmentRemoved.asset";
        private const int ExpectedSlotCount = 12;
        private const int CaptureLayer = 30;
        private const int PanelWidth = 450;
        private const int PanelHeight = 540;
        private const int SlotsPerRow = 4;
        private const float SignatureScale = 100000f;
        private static readonly int[] PreviousSlot06RemovalComponents =
            { 554, 565, 569, 573, 575, 576, 579, 580, 582 };
        private static readonly int[] Slot06TargetComponents = { 558 };
        private static readonly int[][] Slot06PreviewCandidateSets =
        {
            new[] { 558 },
            new[] { 556 },
            new[] { 559 },
            new[] { 560 },
            new[] { 562 },
            new[] { 354 },
            new[] { 572 },
            new[] { 367 },
            new[] { 558, 560, 562 },
            new[] { 556, 558, 559, 560, 562 }
        };
        private static string pendingBridgeOperation;

        [MenuItem("Bellerophon/Enemies/Ispant/Inspect Left Waist Hilt Correction")]
        public static void InspectAllIspantLeftWaistHiltCorrection()
        {
            IspantPreModelingRestoreTool.InspectIspantPreModelingRestore();
        }

        private static void InspectAllIspantLeftWaistHiltCorrectionOriginal()
        {
            var scene = RequireScene();
            var wasDirty = scene.isDirty;
            var placement = RequirePlacement(scene);
            using (new ReadableAssetScope(
                       BaseSourcePath,
                       DrawSourcePath,
                       Slot06SourcePath,
                       ReferenceWithHiltPath,
                       ReferenceWithoutHiltPath))
            {
                var reference = BuildReference();
                var sources = new[]
                {
                    new SourceDescriptor(
                        "Slots1To3_5_7To12",
                        LoadMesh(BaseSourcePath, BodyName),
                        RequireBody(placement, "Ispant_01_Static")),
                    new SourceDescriptor(
                        "Slot4",
                        LoadMesh(DrawSourcePath, null),
                        RequireBody(placement, "Ispant_04_DrawSword")),
                    new SourceDescriptor(
                        "Slot6",
                        LoadMesh(Slot06SourcePath, null),
                        RequireBody(placement, "Ispant_06_SheathSwordDrawMusket"))
                };

                var builder = new StringBuilder();
                builder.AppendLine("Ispant left-waist hilt correction inspection");
                builder.AppendLine("Scene=" + scene.path);
                builder.AppendLine("SceneDirty=" + scene.isDirty);
                builder.AppendLine("ReferenceWithHilt=" + ReferenceWithHiltPath);
                builder.AppendLine("ReferenceWithoutHilt=" + ReferenceWithoutHiltPath);
                builder.AppendLine("ReferenceTriangles=" + reference.TriangleCount);
                builder.AppendLine("ReferenceBounds=" + BoundsText(reference.Bounds));
                builder.AppendLine();

                AppendCurrentSceneState(builder, placement);

                foreach (var source in sources)
                    AppendSourceInspection(builder, source, reference);

                var destination = Absolute(RetryCurrentInspectionPath);
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
                File.WriteAllText(destination, builder.ToString(), Encoding.UTF8);
                if (scene.isDirty != wasDirty)
                    throw new InvalidOperationException(
                        "The correction inspection changed the CargoRunMvp dirty state.");
                Debug.Log(
                    "IspantLeftWaistHiltCorrectionInspected" +
                    ", Sources=" + sources.Length +
                    ", ReferenceTriangles=" + reference.TriangleCount +
                    ", SceneDirty=" + scene.isDirty +
                    ", Report=" + RetryCurrentInspectionPath + ".");
            }
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Preview Left Waist Hilt Correction")]
        public static void PreviewAllIspantLeftWaistHiltCorrection()
        {
            var bridgeOperation = pendingBridgeOperation;
            pendingBridgeOperation = null;
            if (string.Equals(bridgeOperation, "Apply", StringComparison.Ordinal))
            {
                ApplyAllIspantLeftWaistHiltCorrection();
                return;
            }
            if (string.Equals(bridgeOperation, "Capture", StringComparison.Ordinal))
            {
                CaptureAllIspantLeftWaistHiltCorrection();
                return;
            }

            var scene = RequireScene();
            var wasDirty = scene.isDirty;
            var placement = RequirePlacement(scene);
            using (new ReadableAssetScope(
                       BaseSourcePath,
                       DrawSourcePath,
                       Slot06SourcePath,
                       ReferenceWithHiltPath,
                       ReferenceWithoutHiltPath))
            {
                var reference = BuildReference();
                var contexts = BuildCorrectionContexts(placement, reference);
                CaptureCorrectionPreview(
                    contexts.Where(item => item.Slot.name != Slot06Name).ToArray(),
                    NonSlot06PreviewPath);
                CaptureSlot06CandidatePreview(
                    contexts.Single(item => item.Slot.name == Slot06Name));
                var slot06 = contexts.Single(item => item.Slot.name == Slot06Name);
                Debug.Log(
                    "IspantLeftWaistHiltCorrectionPreviewCaptured" +
                    ", Slots=" + contexts.Count +
                    ", NonSlot06BodyTriangles=" +
                    contexts.Where(item => item.Slot.name != Slot06Name)
                        .Sum(item => item.Selection.TriangleCount) +
                    ", Slot06TargetTriangles=" + slot06.Selection.TriangleCount +
                    ", Slot06TargetComponents=" +
                    string.Join(",", Slot06TargetComponents) +
                    ", Slot06CandidateOrder=" +
                    string.Join(";", Slot06PreviewCandidateSets.Select(
                        set => string.Join("+", set))) +
                    ", Images=" + NonSlot06PreviewPath + ";" + Slot06PreviewPath +
                    ", SceneChanged=False, VisualVerdict=PendingDirectReview.");
            }
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException(
                    "The correction preview changed the CargoRunMvp dirty state.");
        }

        public static void SetBridgeOperation(string operation)
        {
            if (!string.Equals(operation, "Apply", StringComparison.Ordinal) &&
                !string.Equals(operation, "Capture", StringComparison.Ordinal))
                throw new ArgumentOutOfRangeException(nameof(operation), operation, null);
            pendingBridgeOperation = operation;
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Apply Left Waist Hilt Correction")]
        public static void ApplyAllIspantLeftWaistHiltCorrection()
        {
            var scene = RequireScene();
            if (scene.isDirty)
                throw new InvalidOperationException(
                    "CargoRunMvp has unsaved editor changes. The correction was not applied.");
            var placement = RequirePlacement(scene);
            using (new ReadableAssetScope(
                       BaseSourcePath,
                       DrawSourcePath,
                       Slot06SourcePath,
                       ReferenceWithHiltPath,
                       ReferenceWithoutHiltPath))
            {
                var reference = BuildReference();
                var contexts = BuildCorrectionContexts(placement, reference);
                var controllers = contexts.ToDictionary(
                    item => item.Slot.name,
                    item => FindAnimator(item.Slot)?.runtimeAnimatorController,
                    StringComparer.Ordinal);
                var roots = contexts.ToDictionary(
                    item => item.Slot.name,
                    item => item.Body.rootBone,
                    StringComparer.Ordinal);
                var bones = contexts.ToDictionary(
                    item => item.Slot.name,
                    item => item.Body.bones.ToArray(),
                    StringComparer.Ordinal);
                var materials = contexts.ToDictionary(
                    item => item.Slot.name,
                    item => item.Body.sharedMaterials.ToArray(),
                    StringComparer.Ordinal);
                var correctedBodyPaths = new List<string>();

                foreach (var context in contexts.Where(item => item.Slot.name != Slot06Name))
                {
                    if (context.Slot.name != Slot04Name)
                    {
                        var outputPath = CorrectedBodyPath(context.Slot.name);
                        var corrected = CreateOrUpdateCorrectedBody(
                            context.SourceMesh, context.Selection, outputPath);
                        RequireCompatibleBodyMesh(context.Body.sharedMesh, corrected);
                        context.Body.sharedMesh = corrected;
                        EditorUtility.SetDirty(context.Body);
                        correctedBodyPaths.Add(outputPath);
                    }

                    var shouldRemainVisible = context.Slot.name == Slot05Name;
                    if (context.WaistSword.enabled != shouldRemainVisible)
                    {
                        context.WaistSword.enabled = shouldRemainVisible;
                        EditorUtility.SetDirty(context.WaistSword);
                    }
                }

                var slot06 = contexts.Single(item => item.Slot.name == Slot06Name);
                var recoveredBody = LoadMesh(Slot06SourcePath, null);
                RequireCompatibleBodyMesh(slot06.Body.sharedMesh, recoveredBody);
                slot06.Body.sharedMesh = recoveredBody;
                EditorUtility.SetDirty(slot06.Body);

                var slot06HandSword = RequireRendererAtPath(
                    slot06.Slot, Slot06HandSwordPath, false);
                slot06HandSword.enabled = false;
                EditorUtility.SetDirty(slot06HandSword);

                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(Slot06ClipPath) ??
                           throw new InvalidOperationException(
                               "The slot 6 sheathing clip is missing: " + Slot06ClipPath);
                var curveSignature = AnimationCurveSignatureExceptVisibility(clip);
                var cutoff = ApplySlot06SwordVisibilityCurve(clip);
                if (!string.Equals(
                        curveSignature,
                        AnimationCurveSignatureExceptVisibility(clip),
                        StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        "A slot 6 animation curve outside sword visibility changed.");

                foreach (var context in contexts)
                {
                    if (FindAnimator(context.Slot)?.runtimeAnimatorController !=
                        controllers[context.Slot.name])
                        throw new InvalidOperationException(
                            context.Slot.name + " Animator controller changed.");
                    if (context.Body.rootBone != roots[context.Slot.name] ||
                        !context.Body.bones.SequenceEqual(bones[context.Slot.name]))
                        throw new InvalidOperationException(
                            context.Slot.name + " body bones changed.");
                    if (!context.Body.sharedMaterials.SequenceEqual(
                            materials[context.Slot.name]))
                        throw new InvalidOperationException(
                            context.Slot.name + " body materials changed.");
                }

                AssetDatabase.SaveAssets();
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene, ScenePath))
                    throw new InvalidOperationException(
                        "CargoRunMvp could not be saved after the correction.");
                AssetDatabase.SaveAssets();

                var retryInspection = Absolute(RetryInspectionPath);
                Directory.CreateDirectory(Path.GetDirectoryName(retryInspection));
                File.AppendAllText(
                    retryInspection,
                    Environment.NewLine +
                    "=== APPLY_RETRY ===" + Environment.NewLine +
                    "Scene=" + ScenePath + Environment.NewLine +
                    "CorrectedBodies=" + string.Join(",", correctedBodyPaths) +
                    Environment.NewLine +
                    "HiddenWaistSwordSlots=" + string.Join(",", contexts
                        .Where(item => item.Slot.name != Slot06Name &&
                                       item.Slot.name != Slot05Name)
                        .Select(item => item.Slot.name)) + Environment.NewLine +
                    "PreservedHandSwordSlot=" + Slot05Name + Environment.NewLine +
                    "Slot06Body=" + Slot06SourcePath + Environment.NewLine +
                    "Slot06HandSwordDefaultEnabled=False" + Environment.NewLine +
                    "Slot06VisibilityCutoff=" + Number(cutoff) + Environment.NewLine +
                    "AnimationsChanged=Slot06SwordVisibilityOnly" + Environment.NewLine +
                    "MaterialsChanged=False|BonesChanged=False" + Environment.NewLine,
                    Encoding.UTF8);
                Debug.Log(
                    "IspantLeftWaistHiltCorrectionApplied" +
                    ", Slots=" + contexts.Count +
                    ", CorrectedBodies=" + correctedBodyPaths.Count +
                    ", HiddenWaistSwords=10" +
                    ", PreservedHandSword=" + Slot05Name +
                    ", Slot06BodyRestored=True" +
                    ", Slot06VisibilityCutoff=" + Number(cutoff) +
                    ", MaterialsChanged=False, BonesChanged=False.");
            }
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Apply Left Waist Hilt Retry")]
        public static void ApplyIspantLeftWaistHiltRetry()
        {
            ApplyAllIspantLeftWaistHiltCorrection();
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Capture Placed Left Waist Hilt Retry Inspection")]
        public static void CapturePlacedIspantLeftWaistHiltRetryInspection()
        {
            var destinationPath = !File.Exists(Absolute(RetryDirectInspection01Path))
                ? RetryDirectInspection01Path
                : !File.Exists(Absolute(RetryDirectInspection02Path))
                    ? RetryDirectInspection02Path
                    : throw new InvalidOperationException(
                        "The two approved direct-inspection captures already exist.");
            CapturePlacedIspantLeftWaistHiltRetry(
                destinationPath,
                "DIRECT_INSPECTION");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Capture Left Waist Hilt Retry Final")]
        public static void CaptureIspantLeftWaistHiltRetryFinal()
        {
            if (File.Exists(Absolute(RetryFinalPath)))
                throw new InvalidOperationException(
                    "The one-time retry final capture already exists: " + RetryFinalPath);
            CapturePlacedIspantLeftWaistHiltRetry(RetryFinalPath, "FINAL_CAPTURE");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Inspect Left Waist Hilt Retry")]
        public static void InspectIspantLeftWaistHiltRetry()
        {
            var scene = RequireScene();
            if (scene.isDirty)
                throw new InvalidOperationException(
                    "CargoRunMvp has unsaved editor changes. Retry inspection was not run.");
            var wasDirty = scene.isDirty;
            var placement = RequirePlacement(scene);
            using (new ReadableAssetScope(
                       BaseSourcePath,
                       DrawSourcePath,
                       Slot06SourcePath,
                       ReferenceWithHiltPath,
                       ReferenceWithoutHiltPath))
            {
                var reference = BuildReference();
                var baseSource = LoadMesh(BaseSourcePath, BodyName);
                var slot06Source = LoadMesh(Slot06SourcePath, null);
                var failures = new List<string>();
                var builder = new StringBuilder();
                builder.AppendLine("Ispant left-waist hilt correction retry inspection");
                builder.AppendLine("Scene=" + scene.path);
                builder.AppendLine("SceneDirty=" + scene.isDirty);
                builder.AppendLine("DirectInspectionFirst=True");
                builder.AppendLine("ReferenceTriangles=" + reference.TriangleCount);

                var slots = placement.Cast<Transform>()
                    .Where(item => item.name.StartsWith("Ispant_", StringComparison.Ordinal))
                    .OrderBy(item => item.name, StringComparer.Ordinal)
                    .ToArray();
                if (slots.Length != ExpectedSlotCount)
                    failures.Add(
                        "Expected 12 placed Ispant slots, found " + slots.Length + ".");

                foreach (var slot in slots)
                {
                    var body = RequireBody(placement, slot.name);
                    var bodyPath = AssetDatabase.GetAssetPath(body.sharedMesh);
                    var targetTriangles =
                        SelectReferenceTriangles(body.sharedMesh, reference).TriangleCount;
                    if (slot.name == Slot06Name)
                    {
                        if (!string.Equals(
                                bodyPath,
                                Slot06SourcePath,
                                StringComparison.OrdinalIgnoreCase) ||
                            TotalTriangles(body.sharedMesh) != TotalTriangles(slot06Source))
                            failures.Add(slot.name + " body is not the intact recovery source.");
                        var handSword = RequireRendererAtPath(
                            slot, Slot06HandSwordPath, false);
                        if (handSword.enabled)
                            failures.Add(
                                slot.name + " legacy hand sword default visibility is enabled.");
                    }
                    else
                    {
                        var waistSword = RequireWaistSword(slot, false);
                        var shouldBeVisible = slot.name == Slot05Name;
                        if (waistSword.enabled != shouldBeVisible)
                            failures.Add(
                                slot.name + " waist-sword default visibility is incorrect.");

                        if (slot.name == Slot04Name)
                        {
                            if (targetTriangles != 0)
                                failures.Add(
                                    slot.name + " unexpectedly contains reference hilt triangles.");
                        }
                        else
                        {
                            var expectedPath = CorrectedBodyPath(slot.name);
                            if (!string.Equals(
                                    bodyPath,
                                    expectedPath,
                                    StringComparison.OrdinalIgnoreCase))
                                failures.Add(
                                    slot.name + " does not use its deterministic corrected body.");
                            if (targetTriangles != 0 ||
                                TotalTriangles(body.sharedMesh) !=
                                TotalTriangles(baseSource) - reference.TriangleCount)
                                failures.Add(
                                    slot.name + " still contains or over-removes hilt geometry.");
                        }
                    }

                    builder.AppendLine(
                        slot.name +
                        "|Body=" + bodyPath +
                        "|Triangles=" + TotalTriangles(body.sharedMesh) +
                        "|RemainingReferenceTriangles=" + targetTriangles);
                }

                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(Slot06ClipPath) ??
                           throw new InvalidOperationException(
                               "The slot 6 sheathing clip is missing: " + Slot06ClipPath);
                var visibilityBinding = AnimationUtility.GetCurveBindings(clip)
                    .SingleOrDefault(binding =>
                        string.Equals(
                            binding.path,
                            "Ispant_06_LegacyHandSword",
                            StringComparison.Ordinal) &&
                        string.Equals(
                            binding.propertyName,
                            "m_Enabled",
                            StringComparison.Ordinal));
                var visibilityCurve = string.IsNullOrEmpty(visibilityBinding.propertyName)
                    ? null
                    : AnimationUtility.GetEditorCurve(clip, visibilityBinding);
                var expectedCutoff = CalculateSlot06SwordDepartureTime(clip);
                var hiddenKeys = visibilityCurve == null
                    ? Array.Empty<Keyframe>()
                    : visibilityCurve.keys.Where(key => key.value < 0.5f).ToArray();
                var actualCutoff = hiddenKeys.Length > 0
                    ? hiddenKeys.Min(key => key.time)
                    : float.NaN;
                if (visibilityCurve == null ||
                    visibilityCurve.Evaluate(0f) < 0.5f ||
                    float.IsNaN(actualCutoff) ||
                    actualCutoff > expectedCutoff + 1f / 60f ||
                    visibilityCurve.Evaluate(clip.length) >= 0.5f)
                    failures.Add(
                        "Slot 6 sword visibility is not hidden at the first departure from hand use.");
                builder.AppendLine(
                    "Slot06ExpectedDeparture=" + Number(expectedCutoff) +
                    "|Slot06VisibilityCutoff=" + Number(actualCutoff));
                builder.AppendLine("Failures=" + failures.Count);
                foreach (var failure in failures)
                    builder.AppendLine("FAIL=" + failure);

                var reportPath = Absolute(RetryInspectionPath);
                Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
                File.AppendAllText(
                    reportPath,
                    Environment.NewLine +
                    "=== SECONDARY_INSPECTION ===" + Environment.NewLine +
                    builder,
                    Encoding.UTF8);
                if (scene.isDirty != wasDirty)
                    throw new InvalidOperationException(
                        "The retry inspection changed the CargoRunMvp dirty state.");
                if (failures.Count > 0)
                    throw new InvalidOperationException(
                        "The retry inspection found " + failures.Count +
                        " issue(s). Report=" + RetryInspectionPath);
                Debug.Log(
                    "IspantLeftWaistHiltRetryInspected" +
                    ", Slots=" + slots.Length +
                    ", Failures=0" +
                    ", Report=" + RetryInspectionPath + ".");
            }
        }

        private static void CapturePlacedIspantLeftWaistHiltRetry(
            string destinationPath,
            string stage)
        {
            var scene = RequireScene();
            if (scene.isDirty)
                throw new InvalidOperationException(
                    "CargoRunMvp has unsaved editor changes. Placed-scene capture was not run.");
            var wasDirty = scene.isDirty;
            var placement = RequirePlacement(scene);
            CapturePlacedSceneSheet(placement, Absolute(destinationPath));

            var reportPath = Absolute(RetryInspectionPath);
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
            File.AppendAllText(
                reportPath,
                Environment.NewLine +
                "=== " + stage + " ===" + Environment.NewLine +
                "Image=" + destinationPath + Environment.NewLine +
                "Source=ActualPlacedSceneObjects" + Environment.NewLine +
                "TargetObjectsManipulated=False" + Environment.NewLine +
                "CameraFramingOnly=True" + Environment.NewLine,
                Encoding.UTF8);
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException(
                    "The placed-scene capture changed the CargoRunMvp dirty state.");
            Debug.Log(
                "IspantLeftWaistHiltRetryPlacedSceneCaptured" +
                ", Stage=" + stage +
                ", Image=" + destinationPath +
                ", TargetObjectsManipulated=False, SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Capture Left Waist Hilt Correction")]
        public static void CaptureAllIspantLeftWaistHiltCorrection()
        {
            var scene = RequireScene();
            if (scene.isDirty)
                throw new InvalidOperationException(
                    "CargoRunMvp has unsaved editor changes. Final capture was not run.");
            var wasDirty = scene.isDirty;
            var destination = Absolute(FinalComparisonPath);
            if (File.Exists(destination))
                throw new InvalidOperationException(
                    "The one-time final comparison already exists: " + FinalComparisonPath);
            var placement = RequirePlacement(scene);
            using (new ReadableAssetScope(
                       BaseSourcePath,
                       DrawSourcePath,
                       Slot06SourcePath,
                       Slot06DamagedBodyPath,
                       ReferenceWithHiltPath,
                       ReferenceWithoutHiltPath))
            {
                var reference = BuildReference();
                var contexts = BuildFinalCorrectionContexts(placement, reference);
                RequireFinalCorrectionState(contexts, reference);
                CaptureFinalCorrectionComparison(contexts, destination);
                File.AppendAllText(
                    Absolute(InspectionPath),
                    Environment.NewLine +
                    "=== FINAL_CAPTURE ===" + Environment.NewLine +
                    "Image=" + FinalComparisonPath + Environment.NewLine +
                    "Layout=Rows1To3:Before|After per slot; Bottom:Slot6 animation start|end" +
                    Environment.NewLine +
                    "SceneChanged=False|VisualVerdict=PendingDirectReview" +
                    Environment.NewLine,
                    Encoding.UTF8);
                if (scene.isDirty != wasDirty)
                    throw new InvalidOperationException(
                        "The final comparison changed the CargoRunMvp dirty state.");
                Debug.Log(
                    "IspantLeftWaistHiltCorrectionFinalCaptured" +
                    ", Slots=" + contexts.Count +
                    ", Image=" + FinalComparisonPath +
                    ", SceneChanged=False, VisualVerdict=PendingDirectReview.");
            }
        }

        private static List<CorrectionContext> BuildFinalCorrectionContexts(
            Transform placement,
            ReferenceDescriptor reference)
        {
            var slots = placement.Cast<Transform>()
                .Where(item => item.name.StartsWith("Ispant_", StringComparison.Ordinal))
                .OrderBy(item => item.name, StringComparer.Ordinal)
                .ToArray();
            if (slots.Length != ExpectedSlotCount)
                throw new InvalidOperationException(
                    "Expected 12 placed Ispant slots, found " + slots.Length + ".");
            var baseSource = LoadMesh(BaseSourcePath, BodyName);
            var drawSource = LoadMesh(DrawSourcePath, null);
            var slot06Source = LoadMesh(Slot06SourcePath, null);
            return slots.Select(slot =>
            {
                var body = RequireBody(placement, slot.name);
                if (slot.name == Slot06Name)
                    return new CorrectionContext(
                        slot,
                        body,
                        slot06Source,
                        SelectComponents(slot06Source, PreviousSlot06RemovalComponents),
                        RequireRendererAtPath(slot, Slot06HandSwordPath, false));
                var source = slot.name == Slot04Name ? drawSource : baseSource;
                return new CorrectionContext(
                    slot,
                    body,
                    source,
                    SelectReferenceTriangles(source, reference),
                    RequireWaistSword(slot, false));
            }).ToList();
        }

        private static void RequireFinalCorrectionState(
            IReadOnlyList<CorrectionContext> contexts,
            ReferenceDescriptor reference)
        {
            foreach (var context in contexts)
            {
                if (context.Slot.name == Slot06Name)
                {
                    if (!string.Equals(
                            AssetDatabase.GetAssetPath(context.Body.sharedMesh),
                            Slot06SourcePath,
                            StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException(
                            "Slot 6 does not use the restored pre-damage body.");
                    if (context.WaistSword.enabled)
                        throw new InvalidOperationException(
                            "Slot 6 hand sword default visibility is still enabled.");
                    continue;
                }

                var shouldRemainVisible = context.Slot.name == Slot05Name;
                if (context.WaistSword.enabled != shouldRemainVisible)
                    throw new InvalidOperationException(
                        context.Slot.name + " waist-sword visibility is incorrect.");
                if (context.Slot.name != Slot04Name &&
                    SelectReferenceTriangles(context.Body.sharedMesh, reference).TriangleCount != 0)
                    throw new InvalidOperationException(
                        context.Slot.name + " still contains embedded hilt triangles.");
            }

            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(Slot06ClipPath) ??
                       throw new InvalidOperationException(
                           "The slot 6 sheathing clip is missing.");
            var binding = AnimationUtility.GetCurveBindings(clip).Single(item =>
                string.Equals(item.path, "Ispant_06_LegacyHandSword", StringComparison.Ordinal) &&
                string.Equals(item.propertyName, "m_Enabled", StringComparison.Ordinal));
            var curve = AnimationUtility.GetEditorCurve(clip, binding);
            if (curve == null || curve.Evaluate(0f) < 0.5f ||
                curve.Evaluate(clip.length) >= 0.5f)
                throw new InvalidOperationException(
                    "Slot 6 sword visibility does not preserve hand use and hide the sheathed state.");
        }

        private static void CaptureFinalCorrectionComparison(
            IReadOnlyList<CorrectionContext> contexts,
            string destination)
        {
            var columns = Mathf.Min(SlotsPerRow, contexts.Count);
            var rows = Mathf.CeilToInt(contexts.Count / (float)columns);
            const int groups = 2;
            var sheet = new Texture2D(
                PanelWidth * columns * groups,
                PanelHeight * (rows + 1),
                TextureFormat.RGB24,
                false);
            Fill(sheet, new Color32(16, 18, 22, 255));
            try
            {
                for (var index = 0; index < contexts.Count; index++)
                {
                    var context = contexts[index];
                    var beforeSource = context.Slot.name == Slot06Name
                        ? LoadMesh(Slot06DamagedBodyPath, null)
                        : context.SourceMesh;
                    var before = new Mesh { name = context.Slot.name + "_FinalBefore" };
                    var after = new Mesh { name = context.Slot.name + "_FinalAfter" };
                    try
                    {
                        BakeSourceMesh(context, beforeSource, before);
                        context.Body.BakeMesh(after);
                        var frame = CalculateTargetRendererFrame(context);
                        var column = index % columns;
                        var row = rows - index / columns;
                        RenderCorrectionPanel(
                            context,
                            before,
                            Array.Empty<OverlayDescriptor>(),
                            true,
                            frame,
                            sheet,
                            (column * groups) * PanelWidth,
                            row * PanelHeight);
                        RenderCorrectionPanel(
                            context,
                            after,
                            Array.Empty<OverlayDescriptor>(),
                            context.WaistSword.enabled,
                            frame,
                            sheet,
                            (column * groups + 1) * PanelWidth,
                            row * PanelHeight);
                    }
                    finally
                    {
                        UnityEngine.Object.DestroyImmediate(before);
                        UnityEngine.Object.DestroyImmediate(after);
                    }
                }

                var slot06 = contexts.Single(item => item.Slot.name == Slot06Name);
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(Slot06ClipPath) ??
                           throw new InvalidOperationException(
                               "The slot 6 sheathing clip is missing.");
                RenderSlot06AnimationSample(slot06, clip, 0f, sheet, 0, 0);
                RenderSlot06AnimationSample(
                    slot06, clip, clip.length, sheet, PanelWidth, 0);

                sheet.Apply();
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
                File.WriteAllBytes(destination, sheet.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sheet);
            }
        }

        private static void RenderSlot06AnimationSample(
            CorrectionContext context,
            AnimationClip clip,
            float time,
            Texture2D sheet,
            int destinationX,
            int destinationY)
        {
            var animator = FindAnimator(context.Slot) ??
                           throw new InvalidOperationException(
                               "Slot 6 Animator is missing.");
            var startedAnimationMode = !AnimationMode.InAnimationMode();
            if (startedAnimationMode) AnimationMode.StartAnimationMode();
            try
            {
                AnimationMode.BeginSampling();
                AnimationMode.SampleAnimationClip(animator.gameObject, clip, time);
                AnimationMode.EndSampling();
                var baked = new Mesh { name = "Slot06AnimationSample" };
                try
                {
                    context.Body.BakeMesh(baked);
                    RenderCorrectionPanel(
                        context,
                        baked,
                        Array.Empty<OverlayDescriptor>(),
                        context.WaistSword.enabled,
                        CalculateTargetRendererFrame(context),
                        sheet,
                        destinationX,
                        destinationY);
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(baked);
                }
            }
            finally
            {
                if (startedAnimationMode && AnimationMode.InAnimationMode())
                    AnimationMode.StopAnimationMode();
            }
        }

        private static CaptureFrame CalculateTargetRendererFrame(
            CorrectionContext context)
        {
            var bounds = context.WaistSword.bounds;
            var aspect = PanelWidth / (float)PanelHeight;
            var orthographicSize = Mathf.Max(
                0.72f,
                bounds.extents.y * 1.55f,
                bounds.extents.x / aspect * 1.55f);
            var direction = (context.Slot.forward - context.Slot.right * 0.24f +
                             context.Slot.up * 0.04f).normalized;
            return new CaptureFrame(
                bounds.center + context.Slot.up * 0.04f,
                orthographicSize,
                direction,
                context.Slot.up);
        }

        private static Animator FindAnimator(Transform slot)
        {
            return slot.GetComponentsInChildren<Animator>(true).FirstOrDefault();
        }

        private static Renderer RequireRendererAtPath(
            Transform slot,
            string path,
            bool requireVisible)
        {
            var matches = slot.GetComponentsInChildren<Renderer>(true)
                .Where(item =>
                    string.Equals(
                        AnimationUtility.CalculateTransformPath(item.transform, slot),
                        path,
                        StringComparison.Ordinal) &&
                    RendererMesh(item) != null)
                .ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException(
                    slot.name + " must contain one renderer at " + path +
                    ", found " + matches.Length + ".");
            if (requireVisible &&
                (!matches[0].enabled || !matches[0].gameObject.activeInHierarchy))
                throw new InvalidOperationException(
                    slot.name + " target renderer is not currently visible: " + path);
            return matches[0];
        }

        private static void RequireCompatibleBodyMesh(Mesh current, Mesh replacement)
        {
            if (current == null || replacement == null ||
                current.bindposes.Length != replacement.bindposes.Length ||
                current.subMeshCount != replacement.subMeshCount)
                throw new InvalidOperationException(
                    "The recovered slot 6 body is not compatible with the current rig.");
        }

        private static string CorrectedBodyPath(string slotName)
        {
            if (string.IsNullOrEmpty(slotName) ||
                !slotName.StartsWith("Ispant_", StringComparison.Ordinal) ||
                slotName.Length < 9)
                throw new InvalidOperationException(
                    "The corrected-body slot name is invalid: " + slotName);
            return "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_" +
                   slotName.Substring(7, 2) + CorrectedBodySuffix;
        }

        private static Mesh CreateOrUpdateCorrectedBody(
            Mesh source,
            RemovalSelection selection,
            string outputPath)
        {
            if (selection.TriangleCount <= 0 || selection.SharedVertices != 0)
                throw new InvalidOperationException(
                    "The corrected body requires a detached non-empty hilt selection: " +
                    outputPath);

            var clone = BuildSubsetMesh(
                source,
                selection,
                false,
                Path.GetFileNameWithoutExtension(outputPath));
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(outputPath);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(clone, outputPath);
                existing = clone;
            }
            else
            {
                EditorUtility.CopySerialized(clone, existing);
                existing.name = clone.name;
                EditorUtility.SetDirty(existing);
                UnityEngine.Object.DestroyImmediate(clone);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(outputPath, ImportAssetOptions.ForceUpdate);
            var corrected = AssetDatabase.LoadAssetAtPath<Mesh>(outputPath) ?? existing;
            if (corrected.vertexCount != source.vertexCount ||
                corrected.subMeshCount != source.subMeshCount ||
                corrected.bindposes.Length != source.bindposes.Length ||
                corrected.blendShapeCount != source.blendShapeCount ||
                TotalTriangles(corrected) !=
                TotalTriangles(source) - selection.TriangleCount ||
                !source.bindposes.SequenceEqual(corrected.bindposes) ||
                !source.boneWeights.SequenceEqual(corrected.boneWeights))
                throw new InvalidOperationException(
                    "The corrected body changed data outside the detached hilt: " +
                    outputPath);
            return corrected;
        }

        private static float ApplySlot06SwordVisibilityCurve(AnimationClip clip)
        {
            var bindings = AnimationUtility.GetCurveBindings(clip);
            var visibility = bindings.SingleOrDefault(binding =>
                string.Equals(binding.path, "Ispant_06_LegacyHandSword", StringComparison.Ordinal) &&
                string.Equals(binding.propertyName, "m_Enabled", StringComparison.Ordinal));
            if (string.IsNullOrEmpty(visibility.propertyName))
                throw new InvalidOperationException(
                    "The slot 6 sword visibility binding is missing.");
            var cutoff = CalculateSlot06SwordDepartureTime(clip);
            var lead = Mathf.Max(0f, cutoff - 1f / 60f);
            var curve = new AnimationCurve(
                new Keyframe(0f, 1f),
                new Keyframe(lead, 1f),
                new Keyframe(cutoff, 0f),
                new Keyframe(clip.length, 0f));
            for (var index = 0; index < curve.length; index++)
            {
                AnimationUtility.SetKeyLeftTangentMode(
                    curve, index, AnimationUtility.TangentMode.Constant);
                AnimationUtility.SetKeyRightTangentMode(
                    curve, index, AnimationUtility.TangentMode.Constant);
            }
            AnimationUtility.SetEditorCurve(clip, visibility, curve);
            EditorUtility.SetDirty(clip);
            return cutoff;
        }

        private static float CalculateSlot06SwordDepartureTime(AnimationClip clip)
        {
            var bindings = AnimationUtility.GetCurveBindings(clip);
            var positionBindings = new[]
            {
                "m_LocalPosition.x",
                "m_LocalPosition.y",
                "m_LocalPosition.z"
            }.Select(property => bindings.Single(binding =>
                string.Equals(
                    binding.path,
                    "Ispant_06_LegacyHandSword",
                    StringComparison.Ordinal) &&
                string.Equals(
                    binding.propertyName,
                    property,
                    StringComparison.Ordinal)))
             .ToArray();
            var positionCurves = positionBindings
                .Select(binding => AnimationUtility.GetEditorCurve(clip, binding))
                .ToArray();
            Vector3 PositionAt(float time) => new Vector3(
                positionCurves[0].Evaluate(time),
                positionCurves[1].Evaluate(time),
                positionCurves[2].Evaluate(time));

            var start = PositionAt(0f);
            var end = PositionAt(clip.length);
            var threshold = Mathf.Max(0.03f, Vector3.Distance(start, end) * 0.04f);
            var cutoff = clip.length * 0.2f;
            const int samples = 240;
            for (var index = 1; index < samples; index++)
            {
                var time = clip.length * index / samples;
                if (Vector3.Distance(PositionAt(time), start) < threshold) continue;
                cutoff = time;
                break;
            }
            return Mathf.Clamp(cutoff, 1f / 60f, clip.length - 1f / 60f);
        }

        private static string AnimationCurveSignatureExceptVisibility(AnimationClip clip)
        {
            var builder = new StringBuilder();
            foreach (var binding in AnimationUtility.GetCurveBindings(clip)
                         .Where(binding =>
                             !(string.Equals(
                                   binding.path,
                                   "Ispant_06_LegacyHandSword",
                                   StringComparison.Ordinal) &&
                               string.Equals(
                                   binding.propertyName,
                                   "m_Enabled",
                                   StringComparison.Ordinal)))
                         .OrderBy(binding => binding.path, StringComparer.Ordinal)
                         .ThenBy(binding => binding.propertyName, StringComparer.Ordinal))
            {
                builder.Append(binding.path).Append('|')
                    .Append(binding.type.FullName).Append('|')
                    .Append(binding.propertyName).Append(':');
                var curve = AnimationUtility.GetEditorCurve(clip, binding);
                if (curve != null)
                foreach (var key in curve.keys)
                    builder.Append(Number(key.time)).Append(',')
                        .Append(Number(key.value)).Append(',')
                        .Append(Number(key.inTangent)).Append(',')
                        .Append(Number(key.outTangent)).Append(';');
                builder.AppendLine();
            }
            return builder.ToString();
        }

        private static List<CorrectionContext> BuildCorrectionContexts(
            Transform placement,
            ReferenceDescriptor reference)
        {
            var slots = placement.Cast<Transform>()
                .Where(item => item.name.StartsWith("Ispant_", StringComparison.Ordinal))
                .OrderBy(item => item.name, StringComparer.Ordinal)
                .ToArray();
            if (slots.Length != ExpectedSlotCount)
                throw new InvalidOperationException(
                    "Expected 12 placed Ispant slots, found " + slots.Length + ".");

            var baseSource = LoadMesh(BaseSourcePath, BodyName);
            var drawSource = LoadMesh(DrawSourcePath, null);
            var slot06Source = LoadMesh(Slot06SourcePath, null);
            var contexts = new List<CorrectionContext>(slots.Length);
            foreach (var slot in slots)
            {
                var body = RequireBody(placement, slot.name);
                Mesh source;
                RemovalSelection selection;
                Renderer waistSword = null;
                if (slot.name == Slot06Name)
                {
                    source = slot06Source;
                    selection = BuildRemovalSelection(source, triangle => false);
                }
                else
                {
                    source = slot.name == Slot04Name ? drawSource : baseSource;
                    selection = SelectReferenceTriangles(source, reference);
                    waistSword = RequireWaistSword(slot, false);
                }

                if (slot.name == Slot04Name && selection.TriangleCount != 0)
                    throw new InvalidOperationException(
                        "Slot 4 unexpectedly contains embedded reference hilt triangles.");
                if (slot.name != Slot04Name && slot.name != Slot06Name &&
                    selection.TriangleCount != reference.TriangleCount)
                    throw new InvalidOperationException(
                        slot.name + " must select all " + reference.TriangleCount +
                        " embedded reference triangles, found " +
                        selection.TriangleCount + ".");
                if (selection.SharedVertices != 0)
                    throw new InvalidOperationException(
                        slot.name + " target shares vertices with preserved geometry: " +
                        selection.SharedVertices + ".");
                contexts.Add(new CorrectionContext(
                    slot, body, source, selection, waistSword));
            }
            return contexts;
        }

        private static Renderer RequireWaistSword(
            Transform slot,
            bool requireVisible = true)
        {
            return RequireRendererAtPath(slot, WaistSwordPath, requireVisible);
        }

        private static Mesh RendererMesh(Renderer renderer)
        {
            if (renderer is SkinnedMeshRenderer skinned)
                return skinned.sharedMesh;
            return renderer is MeshRenderer
                ? renderer.GetComponent<MeshFilter>()?.sharedMesh
                : null;
        }

        private static RemovalSelection SelectReferenceTriangles(
            Mesh mesh,
            ReferenceDescriptor reference)
        {
            return BuildRemovalSelection(
                mesh,
                triangle => reference.PositionKeys.Contains(triangle.PositionKey));
        }

        private static RemovalSelection SelectComponents(
            Mesh mesh,
            IEnumerable<int> componentIndices)
        {
            var components = FindComponents(mesh);
            var requested = new HashSet<int>(componentIndices);
            if (requested.Count == 0 ||
                requested.Any(index => index < 0 || index >= components.Count))
                throw new InvalidOperationException(
                    "The slot 6 correction component selection is outside the source mesh.");
            var selectedTriangles = new HashSet<TriangleOrdinal>();
            foreach (var componentIndex in requested)
            foreach (var triangle in components[componentIndex].Triangles)
                selectedTriangles.Add(
                    new TriangleOrdinal(triangle.SubMesh, triangle.Ordinal));
            return BuildRemovalSelection(
                mesh,
                triangle => selectedTriangles.Contains(
                    new TriangleOrdinal(triangle.SubMesh, triangle.Ordinal)));
        }

        private static RemovalSelection BuildRemovalSelection(
            Mesh mesh,
            Func<TriangleRecord, bool> predicate)
        {
            var selectedOrdinals = new Dictionary<int, HashSet<int>>();
            var selectedVertices = new HashSet<int>();
            var keptVertices = new HashSet<int>();
            var bounds = new Bounds();
            var hasBounds = false;
            var vertices = mesh.vertices;
            var triangleCount = 0;
            foreach (var triangle in ReadTriangles(mesh))
            {
                if (predicate(triangle))
                {
                    if (!selectedOrdinals.TryGetValue(
                            triangle.SubMesh, out var ordinals))
                    {
                        ordinals = new HashSet<int>();
                        selectedOrdinals.Add(triangle.SubMesh, ordinals);
                    }
                    ordinals.Add(triangle.Ordinal);
                    foreach (var vertexIndex in new[]
                             { triangle.A, triangle.B, triangle.C })
                    {
                        selectedVertices.Add(vertexIndex);
                        Encapsulate(
                            ref bounds, ref hasBounds, vertices[vertexIndex]);
                    }
                    triangleCount++;
                }
                else
                {
                    keptVertices.Add(triangle.A);
                    keptVertices.Add(triangle.B);
                    keptVertices.Add(triangle.C);
                }
            }
            return new RemovalSelection(
                selectedOrdinals,
                selectedVertices,
                triangleCount,
                selectedVertices.Count(keptVertices.Contains),
                bounds,
                TotalTriangles(mesh));
        }

        private static void CaptureCorrectionPreview(
            IReadOnlyList<CorrectionContext> contexts,
            string relativePath)
        {
            if (contexts.Count == 0)
                throw new InvalidOperationException(
                    "The correction preview has no slot contexts.");
            var columns = Mathf.Min(SlotsPerRow, contexts.Count);
            var rows = Mathf.CeilToInt(contexts.Count / (float)columns);
            const int groups = 3;
            var sheet = new Texture2D(
                PanelWidth * columns * groups,
                PanelHeight * rows,
                TextureFormat.RGB24,
                false);
            Fill(sheet, new Color32(16, 18, 22, 255));
            try
            {
                for (var index = 0; index < contexts.Count; index++)
                {
                    var context = contexts[index];
                    var baked = new Mesh { name = context.Slot.name + "_CorrectionBefore" };
                    Mesh after = null;
                    Mesh selectedBody = null;
                    try
                    {
                        BakeSourceMesh(context, context.SourceMesh, baked);
                        after = BuildSubsetMesh(
                            baked,
                            context.Selection,
                            false,
                            context.Slot.name + "_CorrectionAfter");
                        if (context.Selection.TriangleCount > 0)
                        {
                            selectedBody = BuildSubsetMesh(
                                baked,
                                context.Selection,
                                true,
                                context.Slot.name + "_CorrectionSelectedBody");
                            OffsetAlongNormals(selectedBody, 0.0025f);
                        }
                        var frame = CalculateCorrectionFrame(context, baked);
                        var column = index % columns;
                        var row = rows - 1 - index / columns;
                        RenderCorrectionPanel(
                            context,
                            baked,
                            Array.Empty<OverlayDescriptor>(),
                            true,
                            frame,
                            sheet,
                            (column * groups) * PanelWidth,
                            row * PanelHeight);
                        var overlays = new List<OverlayDescriptor>();
                        if (selectedBody != null)
                            overlays.Add(new OverlayDescriptor(
                                selectedBody,
                                context.Body.transform,
                                context.Body.sharedMaterials));
                        var selectedPanelX =
                            (column * groups + 1) * PanelWidth;
                        RenderCorrectionPanel(
                            context,
                            baked,
                            overlays,
                            true,
                            frame,
                            sheet,
                            selectedPanelX,
                            row * PanelHeight);
                        if (context.WaistSword != null)
                            DrawBoundsOutline(
                                sheet,
                                selectedPanelX,
                                row * PanelHeight,
                                context.WaistSword.bounds,
                                frame);
                        RenderCorrectionPanel(
                            context,
                            after,
                            Array.Empty<OverlayDescriptor>(),
                            false,
                            frame,
                            sheet,
                            (column * groups + 2) * PanelWidth,
                            row * PanelHeight);
                    }
                    finally
                    {
                        UnityEngine.Object.DestroyImmediate(selectedBody);
                        UnityEngine.Object.DestroyImmediate(after);
                        UnityEngine.Object.DestroyImmediate(baked);
                    }
                }
                sheet.Apply();
                var destination = Absolute(relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
                File.WriteAllBytes(destination, sheet.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sheet);
            }
        }

        private static void CaptureSlot06CandidatePreview(
            CorrectionContext sourceContext)
        {
            var candidateContexts = Slot06PreviewCandidateSets
                .Select(set => new CorrectionContext(
                    sourceContext.Slot,
                    sourceContext.Body,
                    sourceContext.SourceMesh,
                    SelectComponents(sourceContext.SourceMesh, set),
                    null))
                .ToArray();
            CaptureCorrectionPreview(candidateContexts, Slot06PreviewPath);
        }

        private static void DrawBoundsOutline(
            Texture2D sheet,
            int destinationX,
            int destinationY,
            Bounds worldBounds,
            CaptureFrame frame)
        {
            var rotation = Quaternion.LookRotation(-frame.Direction, frame.Up);
            var right = rotation * Vector3.right;
            var up = rotation * Vector3.up;
            var minX = float.PositiveInfinity;
            var minY = float.PositiveInfinity;
            var maxX = float.NegativeInfinity;
            var maxY = float.NegativeInfinity;
            for (var x = -1; x <= 1; x += 2)
            for (var y = -1; y <= 1; y += 2)
            for (var z = -1; z <= 1; z += 2)
            {
                var corner = worldBounds.center + Vector3.Scale(
                    worldBounds.extents, new Vector3(x, y, z));
                var relative = corner - frame.Center;
                var panelX = Vector3.Dot(relative, right) /
                             (2f * frame.OrthographicSize *
                              (PanelWidth / (float)PanelHeight)) + 0.5f;
                var panelY = Vector3.Dot(relative, up) /
                             (2f * frame.OrthographicSize) + 0.5f;
                minX = Mathf.Min(minX, panelX);
                minY = Mathf.Min(minY, panelY);
                maxX = Mathf.Max(maxX, panelX);
                maxY = Mathf.Max(maxY, panelY);
            }
            var left = Mathf.Clamp(
                Mathf.FloorToInt(minX * PanelWidth) - 5, 0, PanelWidth - 1);
            var rightPixel = Mathf.Clamp(
                Mathf.CeilToInt(maxX * PanelWidth) + 5, 0, PanelWidth - 1);
            var bottom = Mathf.Clamp(
                Mathf.FloorToInt(minY * PanelHeight) - 5, 0, PanelHeight - 1);
            var top = Mathf.Clamp(
                Mathf.CeilToInt(maxY * PanelHeight) + 5, 0, PanelHeight - 1);
            var red = new Color32(255, 8, 8, 255);
            const int thickness = 5;
            for (var offset = 0; offset < thickness; offset++)
            {
                for (var px = left; px <= rightPixel; px++)
                {
                    sheet.SetPixel(
                        destinationX + px,
                        destinationY + Mathf.Clamp(bottom + offset, 0, PanelHeight - 1),
                        red);
                    sheet.SetPixel(
                        destinationX + px,
                        destinationY + Mathf.Clamp(top - offset, 0, PanelHeight - 1),
                        red);
                }
                for (var py = bottom; py <= top; py++)
                {
                    sheet.SetPixel(
                        destinationX + Mathf.Clamp(left + offset, 0, PanelWidth - 1),
                        destinationY + py,
                        red);
                    sheet.SetPixel(
                        destinationX + Mathf.Clamp(rightPixel - offset, 0, PanelWidth - 1),
                        destinationY + py,
                        red);
                }
            }
        }

        private static void BakeSourceMesh(
            CorrectionContext context,
            Mesh source,
            Mesh destination)
        {
            if (source == context.Body.sharedMesh)
            {
                context.Body.BakeMesh(destination);
                return;
            }
            var temporary = HiddenObject(context.Slot.name + "_CorrectionBake");
            try
            {
                temporary.transform.SetPositionAndRotation(
                    context.Body.transform.position,
                    context.Body.transform.rotation);
                temporary.transform.localScale = context.Body.transform.lossyScale;
                var renderer = temporary.AddComponent<SkinnedMeshRenderer>();
                renderer.sharedMesh = source;
                renderer.rootBone = context.Body.rootBone;
                renderer.bones = context.Body.bones;
                renderer.BakeMesh(destination);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(temporary);
            }
        }

        private static Mesh BuildSubsetMesh(
            Mesh source,
            RemovalSelection selection,
            bool keepSelected,
            string name)
        {
            var result = UnityEngine.Object.Instantiate(source);
            result.name = name;
            for (var subMesh = 0; subMesh < source.subMeshCount; subMesh++)
            {
                var sourceTriangles = source.GetTriangles(subMesh);
                selection.SelectedOrdinals.TryGetValue(subMesh, out var selected);
                var output = new List<int>(sourceTriangles.Length);
                for (var ordinal = 0; ordinal < sourceTriangles.Length / 3; ordinal++)
                {
                    var isSelected = selected != null && selected.Contains(ordinal);
                    if (isSelected != keepSelected) continue;
                    var offset = ordinal * 3;
                    output.Add(sourceTriangles[offset]);
                    output.Add(sourceTriangles[offset + 1]);
                    output.Add(sourceTriangles[offset + 2]);
                }
                result.SetTriangles(output, subMesh, false);
            }
            result.RecalculateBounds();
            return result;
        }

        private static CaptureFrame CalculateCorrectionFrame(
            CorrectionContext context,
            Mesh baked)
        {
            var bounds = new Bounds();
            var hasBounds = false;
            var vertices = baked.vertices;
            foreach (var vertexIndex in context.Selection.Vertices)
            {
                if (vertexIndex < 0 || vertexIndex >= vertices.Length) continue;
                Encapsulate(
                    ref bounds,
                    ref hasBounds,
                    context.Body.transform.TransformPoint(vertices[vertexIndex]));
            }
            if (context.WaistSword != null)
            {
                if (!hasBounds)
                {
                    bounds = context.WaistSword.bounds;
                    hasBounds = true;
                }
                else bounds.Encapsulate(context.WaistSword.bounds);
            }
            if (!hasBounds)
                throw new InvalidOperationException(
                    "No correction target bounds could be calculated for " +
                    context.Slot.name + ".");
            var aspect = PanelWidth / (float)PanelHeight;
            var orthographicSize = Mathf.Max(
                0.72f,
                bounds.extents.y * 1.55f,
                bounds.extents.x / aspect * 1.55f);
            var direction = (context.Slot.forward - context.Slot.right * 0.24f +
                             context.Slot.up * 0.04f).normalized;
            return new CaptureFrame(
                bounds.center + context.Slot.up * 0.04f,
                orthographicSize,
                direction,
                context.Slot.up);
        }

        private static void CapturePlacedSceneSheet(
            Transform placement,
            string destination)
        {
            var slots = placement.Cast<Transform>()
                .Where(item => item.name.StartsWith("Ispant_", StringComparison.Ordinal))
                .OrderBy(item => item.name, StringComparer.Ordinal)
                .ToArray();
            if (slots.Length != ExpectedSlotCount)
                throw new InvalidOperationException(
                    "Expected 12 placed Ispant slots, found " + slots.Length + ".");

            var columns = Mathf.Min(SlotsPerRow, slots.Length);
            var rows = Mathf.CeilToInt(slots.Length / (float)columns);
            var sheet = new Texture2D(
                PanelWidth * columns,
                PanelHeight * rows,
                TextureFormat.RGB24,
                false);
            var target = new RenderTexture(
                PanelWidth,
                PanelHeight,
                24,
                RenderTextureFormat.ARGB32);
            var panel = new Texture2D(
                PanelWidth,
                PanelHeight,
                TextureFormat.RGB24,
                false);
            var cameraObject = HiddenObject("IspantRetryPlacedSceneCamera");
            var camera = cameraObject.AddComponent<Camera>();
            Fill(sheet, new Color32(16, 18, 22, 255));
            try
            {
                camera.enabled = false;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.055f, 0.063f, 0.075f, 1f);
                camera.orthographic = true;
                camera.aspect = PanelWidth / (float)PanelHeight;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 7.5f;
                camera.cullingMask = ~0;
                camera.targetTexture = target;

                for (var index = 0; index < slots.Length; index++)
                {
                    var slot = slots[index];
                    var body = RequireBody(placement, slot.name);
                    var frame = CalculatePlacedSceneFrame(slot, body);
                    camera.orthographicSize = frame.OrthographicSize;
                    camera.transform.position = frame.Center + frame.Direction * 6f;
                    camera.transform.rotation = Quaternion.LookRotation(
                        frame.Center - camera.transform.position,
                        frame.Up);

                    var oldActive = RenderTexture.active;
                    try
                    {
                        camera.Render();
                        RenderTexture.active = target;
                        panel.ReadPixels(
                            new Rect(0f, 0f, PanelWidth, PanelHeight),
                            0,
                            0);
                        panel.Apply();
                    }
                    finally
                    {
                        RenderTexture.active = oldActive;
                    }

                    var column = index % columns;
                    var row = rows - 1 - index / columns;
                    sheet.SetPixels32(
                        column * PanelWidth,
                        row * PanelHeight,
                        PanelWidth,
                        PanelHeight,
                        panel.GetPixels32());
                }

                sheet.Apply();
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
                File.WriteAllBytes(destination, sheet.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = null;
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(panel);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(sheet);
            }
        }

        private static CaptureFrame CalculatePlacedSceneFrame(
            Transform slot,
            SkinnedMeshRenderer body)
        {
            var scale = Mathf.Max(
                Mathf.Abs(body.transform.lossyScale.x),
                Mathf.Abs(body.transform.lossyScale.y),
                Mathf.Abs(body.transform.lossyScale.z));
            var center = body.bounds.center +
                         slot.up * body.bounds.extents.y * 0.12f -
                         slot.right * body.bounds.extents.x * 0.18f;
            var orthographicSize = Mathf.Max(
                0.5f * scale,
                body.bounds.extents.y * 0.48f);
            var direction = (slot.forward - slot.right * 0.24f +
                             slot.up * 0.04f).normalized;
            return new CaptureFrame(
                center,
                orthographicSize,
                direction,
                slot.up);
        }

        private static void RenderCorrectionPanel(
            CorrectionContext context,
            Mesh bodyMesh,
            IEnumerable<OverlayDescriptor> overlays,
            bool includeWaistSword,
            CaptureFrame frame,
            Texture2D sheet,
            int destinationX,
            int destinationY)
        {
            var target = new RenderTexture(
                PanelWidth, PanelHeight, 24, RenderTextureFormat.ARGB32);
            var panel = new Texture2D(
                PanelWidth, PanelHeight, TextureFormat.RGB24, false);
            var cameraObject = HiddenObject(context.Slot.name + "_CorrectionCamera");
            var camera = cameraObject.AddComponent<Camera>();
            var keyObject = HiddenObject(context.Slot.name + "_CorrectionKey");
            var key = keyObject.AddComponent<Light>();
            var fillObject = HiddenObject(context.Slot.name + "_CorrectionFill");
            var fill = fillObject.AddComponent<Light>();
            var bodyObject = CreateMeshObject(
                context.Slot.name + "_CorrectionBody",
                bodyMesh,
                NormalizeMaterials(context.Body.sharedMaterials, bodyMesh.subMeshCount),
                context.Body.transform);
            var auxiliaryObjects = new List<GameObject>();
            var auxiliaryMeshes = new List<Mesh>();
            var overlayObjects = new List<GameObject>();
            var overlayMaterials = new List<Material>();
            try
            {
                foreach (var renderer in
                         context.Slot.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    var isTargetRenderer = renderer == context.WaistSword;
                    if (renderer == context.Body ||
                        ((!renderer.enabled || !renderer.gameObject.activeInHierarchy) &&
                         !(isTargetRenderer && includeWaistSword)) ||
                        renderer.sharedMesh == null ||
                        (isTargetRenderer && !includeWaistSword))
                        continue;
                    var baked = new Mesh
                    {
                        name = renderer.name + "_CorrectionAuxiliary"
                    };
                    renderer.BakeMesh(baked);
                    auxiliaryMeshes.Add(baked);
                    auxiliaryObjects.Add(CreateMeshObject(
                        renderer.name + "_CorrectionAuxiliary",
                        baked,
                        NormalizeMaterials(
                            renderer.sharedMaterials, baked.subMeshCount),
                        renderer.transform));
                }
                foreach (var filter in
                         context.Slot.GetComponentsInChildren<MeshFilter>(true))
                {
                    var renderer = filter.GetComponent<MeshRenderer>();
                    var isTargetRenderer = renderer == context.WaistSword;
                    if (renderer == null ||
                        ((!renderer.enabled || !renderer.gameObject.activeInHierarchy) &&
                         !(isTargetRenderer && includeWaistSword)) ||
                        filter.sharedMesh == null ||
                        (isTargetRenderer && !includeWaistSword))
                        continue;
                    auxiliaryObjects.Add(CreateMeshObject(
                        filter.name + "_CorrectionAuxiliary",
                        filter.sharedMesh,
                        NormalizeMaterials(
                            renderer.sharedMaterials,
                            filter.sharedMesh.subMeshCount),
                        filter.transform));
                }
                foreach (var overlay in overlays)
                {
                    var materials = CreateOverlayMaterials(
                        overlay.SourceMaterials, overlay.Mesh.subMeshCount);
                    overlayMaterials.AddRange(materials);
                    overlayObjects.Add(CreateMeshObject(
                        context.Slot.name + "_CorrectionOverlay",
                        overlay.Mesh,
                        materials,
                        overlay.Transform));
                }

                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.055f, 0.063f, 0.075f, 1f);
                camera.orthographic = true;
                camera.orthographicSize = frame.OrthographicSize;
                camera.aspect = PanelWidth / (float)PanelHeight;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 100f;
                camera.cullingMask = 1 << CaptureLayer;
                camera.targetTexture = target;
                camera.transform.position = frame.Center + frame.Direction * 6f;
                camera.transform.rotation = Quaternion.LookRotation(
                    frame.Center - camera.transform.position, frame.Up);

                key.type = LightType.Directional;
                key.intensity = 1.25f;
                key.color = new Color(1f, 0.95f, 0.88f);
                key.cullingMask = 1 << CaptureLayer;
                key.transform.rotation = Quaternion.LookRotation(
                    -frame.Direction - frame.Up * 0.45f, frame.Up);
                fill.type = LightType.Directional;
                fill.intensity = 0.7f;
                fill.color = new Color(0.68f, 0.78f, 1f);
                fill.cullingMask = 1 << CaptureLayer;
                fill.transform.rotation = Quaternion.LookRotation(
                    frame.Direction - frame.Up * 0.15f, frame.Up);

                var oldActive = RenderTexture.active;
                try
                {
                    camera.Render();
                    RenderTexture.active = target;
                    panel.ReadPixels(
                        new Rect(0f, 0f, PanelWidth, PanelHeight), 0, 0);
                    panel.Apply();
                    sheet.SetPixels32(
                        destinationX,
                        destinationY,
                        PanelWidth,
                        PanelHeight,
                        panel.GetPixels32());
                }
                finally
                {
                    RenderTexture.active = oldActive;
                }
            }
            finally
            {
                camera.targetTexture = null;
                UnityEngine.Object.DestroyImmediate(bodyObject);
                foreach (var item in auxiliaryObjects)
                    UnityEngine.Object.DestroyImmediate(item);
                foreach (var item in auxiliaryMeshes)
                    UnityEngine.Object.DestroyImmediate(item);
                foreach (var item in overlayObjects)
                    UnityEngine.Object.DestroyImmediate(item);
                foreach (var item in overlayMaterials)
                    UnityEngine.Object.DestroyImmediate(item);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(keyObject);
                UnityEngine.Object.DestroyImmediate(fillObject);
                UnityEngine.Object.DestroyImmediate(panel);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        private static GameObject CreateMeshObject(
            string name,
            Mesh mesh,
            Material[] materials,
            Transform sourceTransform)
        {
            var item = HiddenObject(name);
            item.transform.SetPositionAndRotation(
                sourceTransform.position, sourceTransform.rotation);
            item.transform.localScale = sourceTransform.lossyScale;
            item.AddComponent<MeshFilter>().sharedMesh = mesh;
            item.AddComponent<MeshRenderer>().sharedMaterials = materials;
            return item;
        }

        private static GameObject HiddenObject(string name)
        {
            return new GameObject(name)
            {
                hideFlags = HideFlags.HideAndDontSave,
                layer = CaptureLayer
            };
        }

        private static Material[] NormalizeMaterials(
            Material[] source,
            int count)
        {
            if (source.Length == count) return source;
            if (source.Length == 0)
                throw new InvalidOperationException(
                    "The Ispant renderer has no material.");
            var result = new Material[count];
            for (var index = 0; index < count; index++)
                result[index] = source[Mathf.Min(index, source.Length - 1)];
            return result;
        }

        private static Material[] CreateOverlayMaterials(
            Material[] source,
            int count)
        {
            var normalized = NormalizeMaterials(source, count);
            var result = new Material[count];
            for (var index = 0; index < count; index++)
            {
                var original = normalized[index] ??
                               throw new InvalidOperationException(
                                   "The correction overlay source material is missing.");
                var material = new Material(original)
                {
                    name = "IspantLeftWaistCorrectionOverlay_" + index,
                    hideFlags = HideFlags.HideAndDontSave
                };
                var alpha = original.color.a;
                var red = new Color(1f, 0.015f, 0.015f, alpha);
                if (material.HasProperty("_BaseColor"))
                    material.SetColor("_BaseColor", red);
                if (material.HasProperty("_Color"))
                    material.SetColor("_Color", red);
                if (material.HasProperty("_EmissionColor"))
                {
                    material.SetColor("_EmissionColor", new Color(0.8f, 0f, 0f, 1f));
                    material.EnableKeyword("_EMISSION");
                }
                result[index] = material;
            }
            return result;
        }

        private static void OffsetAlongNormals(Mesh mesh, float amount)
        {
            var vertices = mesh.vertices;
            var normals = mesh.normals;
            if (normals.Length != vertices.Length) return;
            for (var index = 0; index < vertices.Length; index++)
                vertices[index] += normals[index] * amount;
            mesh.vertices = vertices;
            mesh.RecalculateBounds();
        }

        private static void Fill(Texture2D texture, Color32 color)
        {
            var pixels = new Color32[texture.width * texture.height];
            for (var index = 0; index < pixels.Length; index++)
                pixels[index] = color;
            texture.SetPixels32(pixels);
        }

        private static void AppendCurrentSceneState(
            StringBuilder builder,
            Transform placement)
        {
            builder.AppendLine("=== CurrentSceneSlots ===");
            foreach (var slot in placement.Cast<Transform>()
                         .Where(item => item.name.StartsWith("Ispant_", StringComparison.Ordinal))
                         .OrderBy(item => item.name, StringComparer.Ordinal))
            {
                builder.AppendLine("Slot=" + slot.name);
                foreach (var renderer in slot.GetComponentsInChildren<Renderer>(true))
                {
                    Mesh mesh = null;
                    if (renderer is SkinnedMeshRenderer skinned)
                        mesh = skinned.sharedMesh;
                    else if (renderer is MeshRenderer)
                        mesh = renderer.GetComponent<MeshFilter>()?.sharedMesh;
                    builder.Append("Renderer=")
                        .Append(AnimationUtility.CalculateTransformPath(
                            renderer.transform, slot))
                        .Append("|Type=").Append(renderer.GetType().Name)
                        .Append("|Enabled=").Append(renderer.enabled)
                        .Append("|Active=").Append(renderer.gameObject.activeInHierarchy)
                        .Append("|Mesh=").Append(
                            mesh != null ? AssetDatabase.GetAssetPath(mesh) : "None")
                        .Append("|MeshName=").Append(mesh != null ? mesh.name : "None")
                        .Append("|Triangles=").Append(mesh != null ? TotalTriangles(mesh) : 0)
                        .Append("|WorldBounds=").Append(BoundsText(renderer.bounds))
                        .AppendLine();
                    if (mesh != null &&
                        renderer is MeshRenderer &&
                        renderer.name.IndexOf("Sword", StringComparison.OrdinalIgnoreCase) >= 0)
                        AppendSwordComponentInspection(builder, slot, renderer, mesh);
                }
                AppendSwordAnimationInspection(builder, slot);
            }
            builder.AppendLine();
        }

        private static void AppendSwordComponentInspection(
            StringBuilder builder,
            Transform slot,
            Renderer renderer,
            Mesh mesh)
        {
            var components = FindComponents(mesh);
            var vertices = mesh.vertices;
            var toSlot = slot.worldToLocalMatrix * renderer.transform.localToWorldMatrix;
            builder.Append("SwordMesh=")
                .Append(AnimationUtility.CalculateTransformPath(renderer.transform, slot))
                .Append("|Components=").Append(components.Count)
                .Append("|RendererToSlotPosition=")
                .Append(VectorText(slot.InverseTransformPoint(renderer.transform.position)))
                .Append("|RendererToSlotRotation=")
                .Append(VectorText((Quaternion.Inverse(slot.rotation) * renderer.transform.rotation).eulerAngles))
                .Append("|LocalPosition=").Append(VectorText(renderer.transform.localPosition))
                .Append("|LocalRotation=").Append(VectorText(renderer.transform.localEulerAngles))
                .Append("|RendererLossyScale=").Append(VectorText(renderer.transform.lossyScale))
                .AppendLine();
            foreach (var indexed in components
                         .Select((component, index) => new { Component = component, Index = index })
                         .OrderByDescending(item => item.Component.Triangles.Count)
                         .ThenBy(item => item.Index)
                         .Take(40))
            {
                var component = indexed.Component;
                var meshBounds = new Bounds();
                var slotBounds = new Bounds();
                var hasMeshBounds = false;
                var hasSlotBounds = false;
                foreach (var vertexIndex in component.Vertices)
                {
                    var vertex = vertices[vertexIndex];
                    Encapsulate(ref meshBounds, ref hasMeshBounds, vertex);
                    Encapsulate(ref slotBounds, ref hasSlotBounds, toSlot.MultiplyPoint3x4(vertex));
                }
                builder.Append("SwordComponent=").Append(indexed.Index)
                    .Append("|Triangles=").Append(component.Triangles.Count)
                    .Append("|Vertices=").Append(component.Vertices.Count)
                    .Append("|MeshBounds=").Append(BoundsText(meshBounds))
                    .Append("|SlotBounds=").Append(BoundsText(slotBounds))
                    .Append("|SubMeshes=").Append(string.Join(",", component.Triangles
                        .Select(item => item.SubMesh)
                        .Distinct()
                        .OrderBy(item => item)))
                    .AppendLine();
            }
        }

        private static void AppendSwordAnimationInspection(
            StringBuilder builder,
            Transform slot)
        {
            var animator = slot.GetComponentsInChildren<Animator>(true).FirstOrDefault();
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                builder.AppendLine("SwordAnimation=None");
                return;
            }

            var clips = animator.runtimeAnimatorController.animationClips
                .Where(item => item != null)
                .Distinct()
                .OrderBy(item => item.name, StringComparer.Ordinal)
                .ToArray();
            builder.Append("AnimatorController=")
                .Append(AssetDatabase.GetAssetPath(animator.runtimeAnimatorController))
                .Append("|ControllerType=")
                .Append(animator.runtimeAnimatorController.GetType().Name)
                .AppendLine();
            foreach (var clip in clips)
            {
                var swordBindings = AnimationUtility.GetCurveBindings(clip)
                    .Where(binding =>
                        binding.path.IndexOf("Sword", StringComparison.OrdinalIgnoreCase) >= 0)
                    .OrderBy(binding => binding.path, StringComparer.Ordinal)
                    .ThenBy(binding => binding.propertyName, StringComparer.Ordinal)
                    .ToArray();
                builder.Append("SwordAnimation=").Append(clip.name)
                    .Append("|Path=").Append(AssetDatabase.GetAssetPath(clip))
                    .Append("|Length=").Append(Number(clip.length))
                    .Append("|Bindings=").Append(swordBindings.Length)
                    .AppendLine();
                foreach (var binding in swordBindings)
                {
                    var curve = AnimationUtility.GetEditorCurve(clip, binding);
                    builder.Append("SwordCurve=").Append(binding.path)
                        .Append("|Property=").Append(binding.propertyName)
                        .Append("|Keys=").Append(curve != null ? curve.length : 0);
                    if (curve != null && curve.length > 0)
                    {
                        builder.Append("|First=")
                            .Append(Number(curve.keys[0].time)).Append(":")
                            .Append(Number(curve.keys[0].value))
                            .Append("|Last=")
                            .Append(Number(curve.keys[curve.length - 1].time)).Append(":")
                            .Append(Number(curve.keys[curve.length - 1].value));
                    }
                    builder.AppendLine();
                }
            }
        }

        private static void AppendSourceInspection(
            StringBuilder builder,
            SourceDescriptor source,
            ReferenceDescriptor reference)
        {
            var components = FindComponents(source.Mesh);
            ComponentStatistics[] statistics;
            using (var texture = TextureSampler.Create(source.Body.sharedMaterials))
            {
                statistics = components
                    .Select((component, index) =>
                        BuildStatistics(source, component, index, reference, texture))
                    .ToArray();
            }

            builder.AppendLine("=== " + source.Label + " ===");
            builder.AppendLine("Mesh=" + AssetDatabase.GetAssetPath(source.Mesh));
            builder.AppendLine("MeshName=" + source.Mesh.name);
            builder.AppendLine("Vertices=" + source.Mesh.vertexCount);
            builder.AppendLine("Triangles=" + TotalTriangles(source.Mesh));
            builder.AppendLine("Components=" + components.Count);
            builder.AppendLine("BodyBones=" + source.Body.bones.Length);
            builder.AppendLine("-- ClosestToReferenceBounds --");
            foreach (var item in statistics
                         .OrderBy(value => value.Distance)
                         .ThenByDescending(value => value.UvMatches)
                         .Take(50))
                builder.AppendLine(item.ToLine());
            builder.AppendLine("-- MostReferenceMatches --");
            foreach (var item in statistics
                         .Where(value => value.PositionMatches > 0 || value.UvMatches > 0)
                         .OrderByDescending(value => value.PositionMatches)
                         .ThenByDescending(value => value.UvMatches)
                         .ThenBy(value => value.Distance)
                         .Take(100))
                builder.AppendLine(item.ToLine());
            builder.AppendLine("-- LeftWaistRegionComponents --");
            foreach (var item in statistics
                         .Where(value =>
                             value.Triangles >= 2 && value.Triangles <= 300 &&
                             value.Bounds.center.x >= -0.8f && value.Bounds.center.x <= 0.05f &&
                             value.Bounds.center.y >= 0.45f && value.Bounds.center.y <= 1.35f &&
                             value.Bounds.center.z >= -0.35f && value.Bounds.center.z <= 0.35f)
                         .OrderBy(value => value.Bounds.center.y)
                         .ThenBy(value => value.Bounds.center.x)
                         .ThenBy(value => value.Index))
                builder.AppendLine(item.ToLine());
            if (string.Equals(source.Label, "Slot6", StringComparison.Ordinal))
            {
                builder.AppendLine("-- ReferenceShapeMatches --");
                foreach (var match in components
                             .Select((component, index) => new
                             {
                                 Index = index,
                                 Matches = component.Triangles.Count(triangle =>
                                     reference.ShapeKeys.Contains(
                                         TriangleShapeKey(source.Mesh, triangle))),
                                 Triangles = component.Triangles.Count
                             })
                             .Where(item => item.Matches > 0)
                             .OrderByDescending(item => item.Matches)
                             .ThenBy(item => item.Index))
                    builder.AppendLine(
                        "ShapeMatchComponent=" + match.Index +
                        "|Matches=" + match.Matches +
                        "|Triangles=" + match.Triangles);
                builder.AppendLine("-- PreviousSlot06RemovalComponents --");
                foreach (var componentIndex in PreviousSlot06RemovalComponents)
                {
                    var item = statistics.Single(value => value.Index == componentIndex);
                    builder.AppendLine(item.ToLine());
                }
            }
            builder.AppendLine();
        }

        private static ComponentStatistics BuildStatistics(
            SourceDescriptor source,
            ComponentDescriptor component,
            int index,
            ReferenceDescriptor reference,
            TextureSampler texture)
        {
            var vertices = source.Mesh.vertices;
            var uv = source.Mesh.uv;
            var bounds = new Bounds();
            var hasBounds = false;
            foreach (var vertexIndex in component.Vertices)
                Encapsulate(ref bounds, ref hasBounds, vertices[vertexIndex]);

            var positionMatches = 0;
            var uvMatches = 0;
            foreach (var triangle in component.Triangles)
            {
                if (reference.PositionKeys.Contains(triangle.PositionKey))
                    positionMatches++;
                if (uv.Length == vertices.Length &&
                    reference.UvKeys.Contains(
                        new UvTriangleKey(
                            triangle.SubMesh,
                            new UvKey(uv[triangle.A]),
                            new UvKey(uv[triangle.B]),
                            new UvKey(uv[triangle.C]))))
                    uvMatches++;
            }

            var dominantBone = DominantBone(source, component.Vertices);
            var averageColor = texture != null && uv.Length == vertices.Length
                ? texture.Average(component.Vertices.Select(vertexIndex => uv[vertexIndex]))
                : Color.clear;
            return new ComponentStatistics(
                index,
                component.Triangles.Count,
                component.Vertices.Count,
                bounds,
                Vector3.Distance(bounds.center, reference.Bounds.center),
                positionMatches,
                uvMatches,
                dominantBone,
                string.Join(",", component.Triangles
                    .Select(item => item.SubMesh)
                    .Distinct()
                    .OrderBy(item => item)),
                averageColor);
        }

        private static string DominantBone(
            SourceDescriptor source,
            IEnumerable<int> vertexIndices)
        {
            var weights = source.Mesh.boneWeights;
            if (weights.Length != source.Mesh.vertexCount)
                return "None";
            var totals = new Dictionary<int, float>();
            foreach (var vertexIndex in vertexIndices)
            {
                var weight = weights[vertexIndex];
                AddWeight(totals, weight.boneIndex0, weight.weight0);
                AddWeight(totals, weight.boneIndex1, weight.weight1);
                AddWeight(totals, weight.boneIndex2, weight.weight2);
                AddWeight(totals, weight.boneIndex3, weight.weight3);
            }
            if (totals.Count == 0) return "None";
            var dominant = totals.OrderByDescending(item => item.Value).First();
            var name = dominant.Key >= 0 && dominant.Key < source.Body.bones.Length &&
                       source.Body.bones[dominant.Key] != null
                ? source.Body.bones[dominant.Key].name
                : "Bone" + dominant.Key;
            return name + "(" + dominant.Key + "," +
                   dominant.Value.ToString("0.###", CultureInfo.InvariantCulture) + ")";
        }

        private static void AddWeight(
            IDictionary<int, float> totals,
            int boneIndex,
            float weight)
        {
            if (weight <= 0f) return;
            totals.TryGetValue(boneIndex, out var total);
            totals[boneIndex] = total + weight;
        }

        private static ReferenceDescriptor BuildReference()
        {
            var withHilt = LoadMesh(ReferenceWithHiltPath, null);
            var withoutHilt = LoadMesh(ReferenceWithoutHiltPath, null);
            var remaining = TriangleCounts(withoutHilt);
            var positionKeys = new HashSet<PositionTriangleKey>();
            var uvKeys = new HashSet<UvTriangleKey>();
            var shapeKeys = new HashSet<string>(StringComparer.Ordinal);
            var bounds = new Bounds();
            var hasBounds = false;
            var vertices = withHilt.vertices;
            var uv = withHilt.uv;
            foreach (var triangle in ReadTriangles(withHilt))
            {
                if (remaining.TryGetValue(triangle.PositionKey, out var count) && count > 0)
                {
                    remaining[triangle.PositionKey] = count - 1;
                    continue;
                }
                positionKeys.Add(triangle.PositionKey);
                shapeKeys.Add(TriangleShapeKey(withHilt, triangle));
                if (uv.Length == vertices.Length)
                    uvKeys.Add(new UvTriangleKey(
                        triangle.SubMesh,
                        new UvKey(uv[triangle.A]),
                        new UvKey(uv[triangle.B]),
                        new UvKey(uv[triangle.C])));
                Encapsulate(ref bounds, ref hasBounds, vertices[triangle.A]);
                Encapsulate(ref bounds, ref hasBounds, vertices[triangle.B]);
                Encapsulate(ref bounds, ref hasBounds, vertices[triangle.C]);
            }
            if (positionKeys.Count == 0 || !hasBounds)
                throw new InvalidOperationException(
                    "The waist-hilt reference pair has no triangle difference.");
            return new ReferenceDescriptor(positionKeys, uvKeys, shapeKeys, bounds);
        }

        private static string TriangleShapeKey(Mesh mesh, TriangleRecord triangle)
        {
            var vertices = mesh.vertices;
            var lengths = new[]
            {
                (vertices[triangle.A] - vertices[triangle.B]).sqrMagnitude,
                (vertices[triangle.B] - vertices[triangle.C]).sqrMagnitude,
                (vertices[triangle.C] - vertices[triangle.A]).sqrMagnitude
            };
            Array.Sort(lengths);
            return string.Join(",", lengths.Select(value =>
                Math.Round(value, 10, MidpointRounding.AwayFromZero)
                    .ToString("0.##########", CultureInfo.InvariantCulture)));
        }

        private static Dictionary<PositionTriangleKey, int> TriangleCounts(Mesh mesh)
        {
            var result = new Dictionary<PositionTriangleKey, int>();
            foreach (var triangle in ReadTriangles(mesh))
            {
                result.TryGetValue(triangle.PositionKey, out var count);
                result[triangle.PositionKey] = count + 1;
            }
            return result;
        }

        private static List<ComponentDescriptor> FindComponents(Mesh mesh)
        {
            var triangles = ReadTriangles(mesh).ToArray();
            var byVertex = new Dictionary<int, List<int>>();
            for (var index = 0; index < triangles.Length; index++)
            {
                AddTriangle(byVertex, triangles[index].A, index);
                AddTriangle(byVertex, triangles[index].B, index);
                AddTriangle(byVertex, triangles[index].C, index);
            }

            var visited = new bool[triangles.Length];
            var result = new List<ComponentDescriptor>();
            for (var start = 0; start < triangles.Length; start++)
            {
                if (visited[start]) continue;
                var queue = new Queue<int>();
                var componentTriangles = new List<TriangleRecord>();
                var componentVertices = new HashSet<int>();
                visited[start] = true;
                queue.Enqueue(start);
                while (queue.Count > 0)
                {
                    var current = queue.Dequeue();
                    var triangle = triangles[current];
                    componentTriangles.Add(triangle);
                    foreach (var vertex in new[] { triangle.A, triangle.B, triangle.C })
                    {
                        componentVertices.Add(vertex);
                        foreach (var neighbor in byVertex[vertex])
                        {
                            if (visited[neighbor]) continue;
                            visited[neighbor] = true;
                            queue.Enqueue(neighbor);
                        }
                    }
                }
                result.Add(new ComponentDescriptor(componentTriangles, componentVertices));
            }
            return result;
        }

        private static void AddTriangle(
            IDictionary<int, List<int>> lookup,
            int vertex,
            int triangle)
        {
            if (!lookup.TryGetValue(vertex, out var indices))
            {
                indices = new List<int>();
                lookup.Add(vertex, indices);
            }
            indices.Add(triangle);
        }

        private static IEnumerable<TriangleRecord> ReadTriangles(Mesh mesh)
        {
            var vertices = mesh.vertices;
            for (var subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
            {
                var indices = mesh.GetTriangles(subMesh);
                for (var offset = 0; offset < indices.Length; offset += 3)
                {
                    var a = indices[offset];
                    var b = indices[offset + 1];
                    var c = indices[offset + 2];
                    yield return new TriangleRecord(
                        subMesh,
                        offset / 3,
                        a,
                        b,
                        c,
                        new PositionTriangleKey(
                            subMesh,
                            new PositionKey(vertices[a]),
                            new PositionKey(vertices[b]),
                            new PositionKey(vertices[c])));
                }
            }
        }

        private static int TotalTriangles(Mesh mesh)
        {
            var total = 0;
            for (var index = 0; index < mesh.subMeshCount; index++)
                total += (int)mesh.GetIndexCount(index) / 3;
            return total;
        }

        private static Scene RequireScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded ||
                !string.Equals(scene.path, ScenePath, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    "CargoRunMvp must be the active loaded scene. Current=" + scene.path);
            return scene;
        }

        private static Transform RequirePlacement(Scene scene)
        {
            return scene.GetRootGameObjects()
                       .SingleOrDefault(item => item.name == PlacementName)?.transform ??
                   throw new InvalidOperationException(
                       "The approved Ispant placement root is missing.");
        }

        private static SkinnedMeshRenderer RequireBody(
            Transform placement,
            string slotName)
        {
            var slot = placement.Cast<Transform>()
                .SingleOrDefault(item => item.name == slotName) ??
                throw new InvalidOperationException("Missing Ispant slot: " + slotName);
            return slot.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                       .SingleOrDefault(item => item.name == BodyName && item.sharedMesh != null) ??
                   throw new InvalidOperationException(
                       slotName + " has no unique char1 body renderer.");
        }

        private static Mesh LoadMesh(string path, string requiredName)
        {
            var meshes = AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<Mesh>()
                .Where(item => string.IsNullOrEmpty(requiredName) || item.name == requiredName)
                .ToArray();
            if (meshes.Length != 1)
                throw new InvalidOperationException(
                    "Expected one mesh at " + path +
                    (string.IsNullOrEmpty(requiredName) ? string.Empty : "/" + requiredName) +
                    ", found " + meshes.Length + ".");
            if (!meshes[0].isReadable)
                throw new InvalidOperationException("Mesh is not readable: " + path);
            return meshes[0];
        }

        private static string Absolute(string relativePath)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", relativePath));
        }

        private static void Encapsulate(
            ref Bounds bounds,
            ref bool hasBounds,
            Vector3 point)
        {
            if (!hasBounds)
            {
                bounds = new Bounds(point, Vector3.zero);
                hasBounds = true;
            }
            else bounds.Encapsulate(point);
        }

        private static string BoundsText(Bounds bounds)
        {
            return "Center=" + VectorText(bounds.center) +
                   "|Size=" + VectorText(bounds.size);
        }

        private static string VectorText(Vector3 value)
        {
            return Number(value.x) + "," + Number(value.y) + "," + Number(value.z);
        }

        private static string Number(float value)
        {
            return value.ToString("0.######", CultureInfo.InvariantCulture);
        }

        private sealed class ReadableAssetScope : IDisposable
        {
            private readonly List<ReadableState> states = new List<ReadableState>();

            public ReadableAssetScope(params string[] paths)
            {
                foreach (var path in paths.Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                    if (importer != null)
                    {
                        states.Add(new ReadableState(path, importer.isReadable, true));
                        if (!importer.isReadable)
                        {
                            importer.isReadable = true;
                            importer.SaveAndReimport();
                        }
                        continue;
                    }

                    foreach (var mesh in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Mesh>())
                    {
                        var serialized = new SerializedObject(mesh);
                        var property = serialized.FindProperty("m_IsReadable");
                        if (property == null) continue;
                        states.Add(new ReadableState(path, property.boolValue, false));
                        if (property.boolValue) continue;
                        property.boolValue = true;
                        serialized.ApplyModifiedPropertiesWithoutUndo();
                        EditorUtility.SetDirty(mesh);
                        AssetDatabase.SaveAssetIfDirty(mesh);
                        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                    }
                }
            }

            public void Dispose()
            {
                foreach (var state in states.AsEnumerable().Reverse())
                {
                    if (state.UsesImporter)
                    {
                        var importer = AssetImporter.GetAtPath(state.Path) as ModelImporter;
                        if (importer == null || importer.isReadable == state.WasReadable) continue;
                        importer.isReadable = state.WasReadable;
                        importer.SaveAndReimport();
                        continue;
                    }

                    foreach (var mesh in AssetDatabase.LoadAllAssetsAtPath(state.Path).OfType<Mesh>())
                    {
                        var serialized = new SerializedObject(mesh);
                        var property = serialized.FindProperty("m_IsReadable");
                        if (property == null || property.boolValue == state.WasReadable) continue;
                        property.boolValue = state.WasReadable;
                        serialized.ApplyModifiedPropertiesWithoutUndo();
                        EditorUtility.SetDirty(mesh);
                        AssetDatabase.SaveAssetIfDirty(mesh);
                    }
                    AssetDatabase.ImportAsset(state.Path, ImportAssetOptions.ForceUpdate);
                }
            }

            private readonly struct ReadableState
            {
                public ReadableState(string path, bool wasReadable, bool usesImporter)
                {
                    Path = path;
                    WasReadable = wasReadable;
                    UsesImporter = usesImporter;
                }

                public string Path { get; }
                public bool WasReadable { get; }
                public bool UsesImporter { get; }
            }
        }

        private sealed class SourceDescriptor
        {
            public SourceDescriptor(string label, Mesh mesh, SkinnedMeshRenderer body)
            {
                Label = label;
                Mesh = mesh;
                Body = body;
            }

            public string Label { get; }
            public Mesh Mesh { get; }
            public SkinnedMeshRenderer Body { get; }
        }

        private sealed class CorrectionContext
        {
            public CorrectionContext(
                Transform slot,
                SkinnedMeshRenderer body,
                Mesh sourceMesh,
                RemovalSelection selection,
                Renderer waistSword)
            {
                Slot = slot;
                Body = body;
                SourceMesh = sourceMesh;
                Selection = selection;
                WaistSword = waistSword;
            }

            public Transform Slot { get; }
            public SkinnedMeshRenderer Body { get; }
            public Mesh SourceMesh { get; }
            public RemovalSelection Selection { get; }
            public Renderer WaistSword { get; }
        }

        private sealed class RemovalSelection
        {
            public RemovalSelection(
                Dictionary<int, HashSet<int>> selectedOrdinals,
                HashSet<int> vertices,
                int triangleCount,
                int sharedVertices,
                Bounds bounds,
                int totalTriangles)
            {
                SelectedOrdinals = selectedOrdinals;
                Vertices = vertices;
                TriangleCount = triangleCount;
                SharedVertices = sharedVertices;
                Bounds = bounds;
                TotalTriangles = totalTriangles;
            }

            public Dictionary<int, HashSet<int>> SelectedOrdinals { get; }
            public HashSet<int> Vertices { get; }
            public int TriangleCount { get; }
            public int SharedVertices { get; }
            public Bounds Bounds { get; }
            public int TotalTriangles { get; }
        }

        private readonly struct TriangleOrdinal : IEquatable<TriangleOrdinal>
        {
            public TriangleOrdinal(int subMesh, int ordinal)
            {
                SubMesh = subMesh;
                Ordinal = ordinal;
            }

            private int SubMesh { get; }
            private int Ordinal { get; }

            public bool Equals(TriangleOrdinal other) =>
                SubMesh == other.SubMesh && Ordinal == other.Ordinal;
            public override bool Equals(object obj) =>
                obj is TriangleOrdinal other && Equals(other);
            public override int GetHashCode() => SubMesh * 397 ^ Ordinal;
        }

        private readonly struct OverlayDescriptor
        {
            public OverlayDescriptor(
                Mesh mesh,
                Transform transform,
                Material[] sourceMaterials)
            {
                Mesh = mesh;
                Transform = transform;
                SourceMaterials = sourceMaterials;
            }

            public Mesh Mesh { get; }
            public Transform Transform { get; }
            public Material[] SourceMaterials { get; }
        }

        private readonly struct CaptureFrame
        {
            public CaptureFrame(
                Vector3 center,
                float orthographicSize,
                Vector3 direction,
                Vector3 up)
            {
                Center = center;
                OrthographicSize = orthographicSize;
                Direction = direction;
                Up = up;
            }

            public Vector3 Center { get; }
            public float OrthographicSize { get; }
            public Vector3 Direction { get; }
            public Vector3 Up { get; }
        }

        private sealed class TextureSampler : IDisposable
        {
            private readonly Texture2D texture;
            private readonly Vector2 scale;
            private readonly Vector2 offset;

            private TextureSampler(Texture2D texture, Vector2 scale, Vector2 offset)
            {
                this.texture = texture;
                this.scale = scale;
                this.offset = offset;
            }

            public static TextureSampler Create(Material[] materials)
            {
                var material = materials != null && materials.Length > 0
                    ? materials[0]
                    : null;
                if (material == null) return null;
                var property = material.HasProperty("_BaseMap")
                    ? "_BaseMap"
                    : material.HasProperty("_MainTex") ? "_MainTex" : null;
                if (property == null || !(material.GetTexture(property) is Texture source))
                    return null;

                const int size = 512;
                var target = RenderTexture.GetTemporary(
                    size, size, 0, RenderTextureFormat.ARGB32,
                    RenderTextureReadWrite.Linear);
                var oldActive = RenderTexture.active;
                var readable = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
                try
                {
                    Graphics.Blit(source, target);
                    RenderTexture.active = target;
                    readable.ReadPixels(new Rect(0f, 0f, size, size), 0, 0);
                    readable.Apply();
                }
                finally
                {
                    RenderTexture.active = oldActive;
                    RenderTexture.ReleaseTemporary(target);
                }
                return new TextureSampler(
                    readable,
                    material.GetTextureScale(property),
                    material.GetTextureOffset(property));
            }

            public Color Average(IEnumerable<Vector2> coordinates)
            {
                var total = Color.clear;
                var count = 0;
                foreach (var coordinate in coordinates)
                {
                    var transformed = Vector2.Scale(coordinate, scale) + offset;
                    transformed.x = Mathf.Repeat(transformed.x, 1f);
                    transformed.y = Mathf.Repeat(transformed.y, 1f);
                    total += texture.GetPixelBilinear(transformed.x, transformed.y);
                    count++;
                }
                return count > 0 ? total / count : Color.clear;
            }

            public void Dispose()
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private sealed class ReferenceDescriptor
        {
            public ReferenceDescriptor(
                HashSet<PositionTriangleKey> positionKeys,
                HashSet<UvTriangleKey> uvKeys,
                HashSet<string> shapeKeys,
                Bounds bounds)
            {
                PositionKeys = positionKeys;
                UvKeys = uvKeys;
                ShapeKeys = shapeKeys;
                Bounds = bounds;
            }

            public HashSet<PositionTriangleKey> PositionKeys { get; }
            public HashSet<UvTriangleKey> UvKeys { get; }
            public HashSet<string> ShapeKeys { get; }
            public Bounds Bounds { get; }
            public int TriangleCount => PositionKeys.Count;
        }

        private sealed class ComponentDescriptor
        {
            public ComponentDescriptor(
                List<TriangleRecord> triangles,
                HashSet<int> vertices)
            {
                Triangles = triangles;
                Vertices = vertices;
            }

            public List<TriangleRecord> Triangles { get; }
            public HashSet<int> Vertices { get; }
        }

        private sealed class ComponentStatistics
        {
            public ComponentStatistics(
                int index,
                int triangles,
                int vertices,
                Bounds bounds,
                float distance,
                int positionMatches,
                int uvMatches,
                string dominantBone,
                string subMeshes,
                Color averageColor)
            {
                Index = index;
                Triangles = triangles;
                Vertices = vertices;
                Bounds = bounds;
                Distance = distance;
                PositionMatches = positionMatches;
                UvMatches = uvMatches;
                DominantBone = dominantBone;
                SubMeshes = subMeshes;
                AverageColor = averageColor;
            }

            public int Index { get; }
            public int Triangles { get; }
            public int Vertices { get; }
            public Bounds Bounds { get; }
            public float Distance { get; }
            public int PositionMatches { get; }
            public int UvMatches { get; }
            public string DominantBone { get; }
            public string SubMeshes { get; }
            public Color AverageColor { get; }

            public string ToLine()
            {
                return "Component=" + Index +
                       "|Triangles=" + Triangles +
                       "|Vertices=" + Vertices +
                       "|Bounds=" + BoundsText(Bounds) +
                       "|Distance=" + Number(Distance) +
                       "|PositionMatches=" + PositionMatches +
                       "|UvMatches=" + UvMatches +
                       "|DominantBone=" + DominantBone +
                       "|SubMeshes=" + SubMeshes +
                       "|AverageColor=" + Number(AverageColor.r) + "," +
                       Number(AverageColor.g) + "," + Number(AverageColor.b) +
                       "|Luminance=" + Number(
                           AverageColor.r * 0.2126f +
                           AverageColor.g * 0.7152f +
                           AverageColor.b * 0.0722f);
            }
        }

        private readonly struct TriangleRecord
        {
            public TriangleRecord(
                int subMesh,
                int ordinal,
                int a,
                int b,
                int c,
                PositionTriangleKey positionKey)
            {
                SubMesh = subMesh;
                Ordinal = ordinal;
                A = a;
                B = b;
                C = c;
                PositionKey = positionKey;
            }

            public int SubMesh { get; }
            public int Ordinal { get; }
            public int A { get; }
            public int B { get; }
            public int C { get; }
            public PositionTriangleKey PositionKey { get; }
        }

        private readonly struct PositionKey : IEquatable<PositionKey>, IComparable<PositionKey>
        {
            public PositionKey(Vector3 value)
            {
                X = Mathf.RoundToInt(value.x * SignatureScale);
                Y = Mathf.RoundToInt(value.y * SignatureScale);
                Z = Mathf.RoundToInt(value.z * SignatureScale);
            }

            private int X { get; }
            private int Y { get; }
            private int Z { get; }

            public int CompareTo(PositionKey other)
            {
                var result = X.CompareTo(other.X);
                if (result != 0) return result;
                result = Y.CompareTo(other.Y);
                return result != 0 ? result : Z.CompareTo(other.Z);
            }

            public bool Equals(PositionKey other) =>
                X == other.X && Y == other.Y && Z == other.Z;
            public override bool Equals(object obj) =>
                obj is PositionKey other && Equals(other);
            public override int GetHashCode()
            {
                unchecked
                {
                    var hash = X;
                    hash = hash * 397 ^ Y;
                    return hash * 397 ^ Z;
                }
            }
        }

        private readonly struct PositionTriangleKey : IEquatable<PositionTriangleKey>
        {
            public PositionTriangleKey(
                int subMesh,
                PositionKey a,
                PositionKey b,
                PositionKey c)
            {
                var values = new[] { a, b, c };
                Array.Sort(values);
                SubMesh = subMesh;
                A = values[0];
                B = values[1];
                C = values[2];
            }

            private int SubMesh { get; }
            private PositionKey A { get; }
            private PositionKey B { get; }
            private PositionKey C { get; }

            public bool Equals(PositionTriangleKey other) =>
                SubMesh == other.SubMesh && A.Equals(other.A) &&
                B.Equals(other.B) && C.Equals(other.C);
            public override bool Equals(object obj) =>
                obj is PositionTriangleKey other && Equals(other);
            public override int GetHashCode()
            {
                unchecked
                {
                    var hash = SubMesh;
                    hash = hash * 397 ^ A.GetHashCode();
                    hash = hash * 397 ^ B.GetHashCode();
                    return hash * 397 ^ C.GetHashCode();
                }
            }
        }

        private readonly struct UvKey : IEquatable<UvKey>, IComparable<UvKey>
        {
            public UvKey(Vector2 value)
            {
                X = Mathf.RoundToInt(value.x * SignatureScale);
                Y = Mathf.RoundToInt(value.y * SignatureScale);
            }

            private int X { get; }
            private int Y { get; }

            public int CompareTo(UvKey other)
            {
                var result = X.CompareTo(other.X);
                return result != 0 ? result : Y.CompareTo(other.Y);
            }
            public bool Equals(UvKey other) => X == other.X && Y == other.Y;
            public override bool Equals(object obj) => obj is UvKey other && Equals(other);
            public override int GetHashCode() => X * 397 ^ Y;
        }

        private readonly struct UvTriangleKey : IEquatable<UvTriangleKey>
        {
            public UvTriangleKey(int subMesh, UvKey a, UvKey b, UvKey c)
            {
                var values = new[] { a, b, c };
                Array.Sort(values);
                SubMesh = subMesh;
                A = values[0];
                B = values[1];
                C = values[2];
            }

            private int SubMesh { get; }
            private UvKey A { get; }
            private UvKey B { get; }
            private UvKey C { get; }

            public bool Equals(UvTriangleKey other) =>
                SubMesh == other.SubMesh && A.Equals(other.A) &&
                B.Equals(other.B) && C.Equals(other.C);
            public override bool Equals(object obj) =>
                obj is UvTriangleKey other && Equals(other);
            public override int GetHashCode()
            {
                unchecked
                {
                    var hash = SubMesh;
                    hash = hash * 397 ^ A.GetHashCode();
                    hash = hash * 397 ^ B.GetHashCode();
                    return hash * 397 ^ C.GetHashCode();
                }
            }
        }
    }

    internal static class IspantPreModelingRestoreTool
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementName = "Approved Ispant Enemy Placement";
        private const string BodyName = "char1";
        private const string BaseBodyPath =
            "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_New_Direct_Source.fbx";
        private const string DrawBodyPath =
            "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_New_DrawSword_Body.asset";
        private const string Slot06BodyPath =
            "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_06_BodyWaistDebrisRemoved.asset";
        private const string Slot06Name = "Ispant_06_SheathSwordDrawMusket";
        private const string Slot04Name = "Ispant_04_DrawSword";
        private const string Slot05Name = "Ispant_05_RunningOneHandedSwordAttack";
        private const string WaistSwordPath =
            "Ispant_New_Direct_Model/Armature/Ispant_Approved_LongSword_10K";
        private const string Slot06HandSwordPath =
            "Ispant_New_Direct_Model/Ispant_06_LegacyHandSword";
        private const string Slot06ClipPath =
            "Assets/_Project/Art/Enemies/Ispant/Animations/Ispant_06_New_SheathingSword_Loop.anim";
        private const string Slot06ControllerPath =
            "Assets/_Project/Art/Enemies/Ispant/Controllers/Ispant_06_New_SheathingSword_Loop.controller";
        private const string ValidationFolder =
            "docs/validation/ispant_pre_modeling_restore_2026-08-25";
        private const string InspectionPath = ValidationFolder + "/Inspection.txt";
        private const string BridgeStagePath = ValidationFolder + "/BridgeStage.txt";
        private const string DirectInspectionPath =
            ValidationFolder + "/PlacedScene_DirectInspection.png";
        private const string FinalPath = ValidationFolder + "/PlacedScene_Final.png";
        private const int ExpectedSlotCount = 12;
        private const int PanelWidth = 450;
        private const int PanelHeight = 540;
        private const int SlotsPerRow = 4;

        private static readonly string[] CorrectionAssetPaths =
        {
            "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_01_BodyLeftWaistHiltCorrected.asset",
            "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_02_BodyLeftWaistHiltCorrected.asset",
            "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_03_BodyLeftWaistHiltCorrected.asset",
            "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_05_BodyLeftWaistHiltCorrected.asset",
            "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_07_BodyLeftWaistHiltCorrected.asset",
            "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_08_BodyLeftWaistHiltCorrected.asset",
            "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_09_BodyLeftWaistHiltCorrected.asset",
            "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_10_BodyLeftWaistHiltCorrected.asset",
            "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_11_BodyLeftWaistHiltCorrected.asset",
            "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_12_BodyLeftWaistHiltCorrected.asset",
            "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_New_Direct_Source_char1_AllHiltFragmentRemoved.asset",
            "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_New_DrawSword_Body_AllHiltFragmentRemoved.asset",
            "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_06_BodyWaistDebrisRemoved_AllHiltFragmentRemoved.asset",
            "Assets/_Project/Art/Enemies/Ispant/Models/Ispant_06_BodyMarkedHiltFragmentRemoved.asset"
        };

        [MenuItem("Bellerophon/Enemies/Ispant/Inspect Pre-Modeling Restore")]
        public static void InspectIspantPreModelingRestore()
        {
            var stagePath = Absolute(BridgeStagePath);
            var stage = File.Exists(stagePath)
                ? File.ReadAllText(stagePath, Encoding.UTF8).Trim()
                : string.Empty;
            if (string.IsNullOrEmpty(stage))
            {
                RestoreIspantPreModelingState();
                WriteBridgeStage(stagePath, "RESTORED");
                return;
            }
            if (string.Equals(stage, "RESTORED", StringComparison.Ordinal))
            {
                CapturePlacedIspantPreModelingRestoreInspection();
                WriteBridgeStage(stagePath, "DIRECT_CAPTURED");
                return;
            }
            if (string.Equals(stage, "DIRECT_CAPTURED", StringComparison.Ordinal))
            {
                InspectIspantPreModelingRestoreResult();
                WriteBridgeStage(stagePath, "SECONDARY_PASSED");
                return;
            }
            if (string.Equals(stage, "SECONDARY_PASSED", StringComparison.Ordinal))
            {
                CaptureIspantPreModelingRestoreFinal();
                WriteBridgeStage(stagePath, "FINAL_CAPTURED");
                return;
            }
            throw new InvalidOperationException(
                "The pre-modeling restore bridge has no next stage. Current=" + stage);
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Restore Pre-Modeling State")]
        public static void RestoreIspantPreModelingState()
        {
            var scene = RequireScene();
            if (scene.isDirty)
                throw new InvalidOperationException(
                    "CargoRunMvp has unsaved editor changes. The restore was not applied.");
            var placement = RequirePlacement(scene);
            var slots = RequireSlots(placement);
            var outsideSignature = OutsidePlacementSignature(scene);
            var controllers = slots.ToDictionary(
                slot => slot.name,
                slot => RequireAnimator(slot).runtimeAnimatorController,
                StringComparer.Ordinal);
            var roots = slots.ToDictionary(
                slot => slot.name,
                slot => RequireBody(slot).rootBone,
                StringComparer.Ordinal);
            var bones = slots.ToDictionary(
                slot => slot.name,
                slot => RequireBody(slot).bones.ToArray(),
                StringComparer.Ordinal);
            var materials = slots.ToDictionary(
                slot => slot.name,
                slot => RequireBody(slot).sharedMaterials.ToArray(),
                StringComparer.Ordinal);

            var baseBody = LoadMesh(BaseBodyPath, BodyName);
            var drawBody = LoadMesh(DrawBodyPath, null);
            var slot06Body = LoadMesh(Slot06BodyPath, null);
            foreach (var slot in slots)
            {
                var body = RequireBody(slot);
                var restoredBody = slot.name == Slot06Name
                    ? slot06Body
                    : slot.name == Slot04Name || slot.name == Slot05Name
                        ? drawBody
                        : baseBody;
                body.sharedMesh = restoredBody;
                EditorUtility.SetDirty(body);

                if (slot.name == Slot06Name)
                {
                    var handSword = RequireRenderer(slot, Slot06HandSwordPath);
                    handSword.enabled = true;
                    EditorUtility.SetDirty(handSword);
                }
                else
                {
                    var waistSword = RequireRenderer(slot, WaistSwordPath);
                    waistSword.enabled = true;
                    EditorUtility.SetDirty(waistSword);
                }
            }

            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(Slot06ClipPath) ??
                       throw new InvalidOperationException(
                           "The existing slot 6 sheathing clip is missing: " + Slot06ClipPath);
            var nonVisibilitySignature = AnimationCurveSignatureExceptVisibility(clip);
            RestoreSlot06SwordVisibility(clip, slots.Single(slot => slot.name == Slot06Name));
            if (!string.Equals(
                    nonVisibilitySignature,
                    AnimationCurveSignatureExceptVisibility(clip),
                    StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "A slot 6 animation curve outside sword visibility changed during restore.");

            foreach (var slot in slots)
            {
                var body = RequireBody(slot);
                if (RequireAnimator(slot).runtimeAnimatorController != controllers[slot.name])
                    throw new InvalidOperationException(
                        slot.name + " Animator controller changed during restore.");
                if (body.rootBone != roots[slot.name] ||
                    !body.bones.SequenceEqual(bones[slot.name]))
                    throw new InvalidOperationException(
                        slot.name + " body skeleton changed during restore.");
                if (!body.sharedMaterials.SequenceEqual(materials[slot.name]))
                    throw new InvalidOperationException(
                        slot.name + " body materials changed during restore.");
            }
            if (!string.Equals(
                    outsideSignature,
                    OutsidePlacementSignature(scene),
                    StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "A scene root outside Approved Ispant Enemy Placement changed.");

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved after the Ispant restore.");

            var deleted = new List<string>();
            foreach (var path in CorrectionAssetPaths)
            {
                if (AssetDatabase.LoadMainAssetAtPath(path) == null) continue;
                if (!AssetDatabase.DeleteAsset(path))
                    throw new InvalidOperationException(
                        "The correction asset could not be removed: " + path);
                deleted.Add(path);
            }
            AssetDatabase.SaveAssets();
            WriteInspection("RESTORED", placement, true, deleted);
            Debug.Log(
                "IspantPreModelingStateRestored" +
                ", Slots=" + slots.Length +
                ", RestoredBodies=12" +
                ", ExistingAnimatorControllersPreserved=12" +
                ", Slot06ExistingAnimationPreserved=True" +
                ", RemovedCorrectionAssets=" + deleted.Count +
                ", OutsidePlacementChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Capture Pre-Modeling Restore Inspection")]
        public static void CapturePlacedIspantPreModelingRestoreInspection()
        {
            CapturePlacedScene(
                DirectInspectionPath,
                "DIRECT_VISUAL_INSPECTION",
                requireRestoredState: false);
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Inspect Pre-Modeling Restore Result")]
        public static void InspectIspantPreModelingRestoreResult()
        {
            var scene = RequireScene();
            if (scene.isDirty)
                throw new InvalidOperationException(
                    "CargoRunMvp has unsaved editor changes. Result inspection was not run.");
            var wasDirty = scene.isDirty;
            var placement = RequirePlacement(scene);
            var failures = FindRestoreFailures(placement);
            WriteInspection("SECONDARY_INSPECTION", placement, true, Array.Empty<string>(), failures);
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException(
                    "The restore result inspection changed the scene dirty state.");
            if (failures.Count > 0)
                throw new InvalidOperationException(
                    "The pre-modeling restore inspection found " + failures.Count +
                    " issue(s). Report=" + InspectionPath);
            Debug.Log(
                "IspantPreModelingRestoreResultInspected" +
                ", Slots=12, Failures=0" +
                ", ExistingAnimatorControllersConnected=12" +
                ", SceneChanged=False, Report=" + InspectionPath + ".");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Capture Pre-Modeling Restore Final")]
        public static void CaptureIspantPreModelingRestoreFinal()
        {
            CapturePlacedScene(FinalPath, "FINAL_CAPTURE", requireRestoredState: true);
        }

        private static void CapturePlacedScene(
            string relativePath,
            string stage,
            bool requireRestoredState)
        {
            var scene = RequireScene();
            if (scene.isDirty)
                throw new InvalidOperationException(
                    "CargoRunMvp has unsaved editor changes. Capture was not run.");
            var wasDirty = scene.isDirty;
            var placement = RequirePlacement(scene);
            if (requireRestoredState)
            {
                var failures = FindRestoreFailures(placement);
                if (failures.Count > 0)
                    throw new InvalidOperationException(
                        "The final capture was blocked by " + failures.Count +
                        " restore inspection issue(s).");
            }

            var destination = Absolute(relativePath);
            if (File.Exists(destination))
                throw new InvalidOperationException(
                    "The one-time placed-scene capture already exists: " + relativePath);
            CaptureLeftWaistCloseupSheet(placement, destination);
            AppendInspection(
                Environment.NewLine +
                "=== " + stage + " ===" + Environment.NewLine +
                "Image=" + relativePath + Environment.NewLine +
                "Source=ActualPlacedCargoRunMvpSceneObjects" + Environment.NewLine +
                "View=UserMarkedLeftWaistCloseup" + Environment.NewLine +
                "TargetObjectsManipulated=False" + Environment.NewLine +
                "CameraFramingOnly=True" + Environment.NewLine);
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException(
                    "The placed-scene capture changed the scene dirty state.");
            Debug.Log(
                "IspantPreModelingRestorePlacedSceneCaptured" +
                ", Stage=" + stage +
                ", Image=" + relativePath +
                ", TargetObjectsManipulated=False, SceneChanged=False.");
        }

        private static List<string> FindRestoreFailures(Transform placement)
        {
            var failures = new List<string>();
            var slots = RequireSlots(placement);
            foreach (var slot in slots)
            {
                var body = RequireBody(slot);
                var expectedPath = ExpectedBodyPath(slot.name);
                var actualPath = AssetDatabase.GetAssetPath(body.sharedMesh);
                if (!string.Equals(actualPath, expectedPath, StringComparison.OrdinalIgnoreCase))
                    failures.Add(
                        slot.name + " body mismatch. Expected=" + expectedPath +
                        ", Actual=" + actualPath);

                var animator = RequireAnimator(slot);
                if (animator.runtimeAnimatorController == null)
                    failures.Add(slot.name + " has no Animator Controller.");

                if (slot.name == Slot06Name)
                {
                    var controllerPath = AssetDatabase.GetAssetPath(
                        animator.runtimeAnimatorController);
                    if (!string.Equals(
                            controllerPath,
                            Slot06ControllerPath,
                            StringComparison.OrdinalIgnoreCase))
                        failures.Add(
                            slot.name + " existing loop controller is not connected. Actual=" +
                            controllerPath);
                    if (!RequireRenderer(slot, Slot06HandSwordPath).enabled)
                        failures.Add(slot.name + " existing animated hand sword is disabled.");
                }
                else if (!RequireRenderer(slot, WaistSwordPath).enabled)
                {
                    failures.Add(slot.name + " original waist sword renderer is disabled.");
                }
            }

            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(Slot06ClipPath);
            if (clip == null)
            {
                failures.Add("The existing slot 6 sheathing animation clip is missing.");
            }
            else
            {
                var binding = FindVisibilityBinding(clip);
                var curve = string.IsNullOrEmpty(binding.propertyName)
                    ? null
                    : AnimationUtility.GetEditorCurve(clip, binding);
                if (curve == null || curve.length == 0 ||
                    curve.keys.Any(key => key.value < 0.5f) ||
                    curve.Evaluate(0f) < 0.5f || curve.Evaluate(clip.length) < 0.5f)
                    failures.Add(
                        "The slot 6 existing animated sword is not visible through its loop.");
            }

            foreach (var path in CorrectionAssetPaths)
            {
                if (AssetDatabase.LoadMainAssetAtPath(path) != null)
                    failures.Add("Correction asset still exists: " + path);
            }
            return failures;
        }

        private static void RestoreSlot06SwordVisibility(
            AnimationClip clip,
            Transform slot)
        {
            var binding = FindVisibilityBinding(clip);
            if (string.IsNullOrEmpty(binding.propertyName))
            {
                var renderer = RequireRenderer(slot, Slot06HandSwordPath);
                binding = EditorCurveBinding.FloatCurve(
                    "Ispant_06_LegacyHandSword",
                    renderer.GetType(),
                    "m_Enabled");
            }
            var curve = new AnimationCurve(
                new Keyframe(0f, 1f),
                new Keyframe(clip.length, 1f));
            for (var index = 0; index < curve.length; index++)
            {
                AnimationUtility.SetKeyLeftTangentMode(
                    curve, index, AnimationUtility.TangentMode.Constant);
                AnimationUtility.SetKeyRightTangentMode(
                    curve, index, AnimationUtility.TangentMode.Constant);
            }
            AnimationUtility.SetEditorCurve(clip, binding, curve);
            EditorUtility.SetDirty(clip);
        }

        private static EditorCurveBinding FindVisibilityBinding(AnimationClip clip)
        {
            return AnimationUtility.GetCurveBindings(clip).SingleOrDefault(binding =>
                string.Equals(
                    binding.path,
                    "Ispant_06_LegacyHandSword",
                    StringComparison.Ordinal) &&
                string.Equals(binding.propertyName, "m_Enabled", StringComparison.Ordinal));
        }

        private static string AnimationCurveSignatureExceptVisibility(AnimationClip clip)
        {
            var builder = new StringBuilder();
            foreach (var binding in AnimationUtility.GetCurveBindings(clip)
                         .Where(binding =>
                             !(string.Equals(
                                   binding.path,
                                   "Ispant_06_LegacyHandSword",
                                   StringComparison.Ordinal) &&
                               string.Equals(
                                   binding.propertyName,
                                   "m_Enabled",
                                   StringComparison.Ordinal)))
                         .OrderBy(binding => binding.path, StringComparer.Ordinal)
                         .ThenBy(binding => binding.propertyName, StringComparer.Ordinal))
            {
                builder.Append(binding.path).Append('|')
                    .Append(binding.type.FullName).Append('|')
                    .Append(binding.propertyName).Append(':');
                var curve = AnimationUtility.GetEditorCurve(clip, binding);
                if (curve != null)
                foreach (var key in curve.keys)
                    builder.Append(Number(key.time)).Append(',')
                        .Append(Number(key.value)).Append(',')
                        .Append(Number(key.inTangent)).Append(',')
                        .Append(Number(key.outTangent)).Append(';');
                builder.AppendLine();
            }
            return builder.ToString();
        }

        private static void WriteInspection(
            string stage,
            Transform placement,
            bool append,
            IReadOnlyCollection<string> deleted,
            IReadOnlyCollection<string> failures = null)
        {
            var builder = new StringBuilder();
            builder.AppendLine("=== " + stage + " ===");
            builder.AppendLine("Scene=" + ScenePath);
            builder.AppendLine("Target=Approved Ispant Enemy Placement/Ispant_01..12");
            builder.AppendLine("Baseline=Before 2026-08-25 Ispant hilt-modeling attempts");
            builder.AppendLine("ExistingAnimationConnectionsPreserved=True");
            foreach (var slot in RequireSlots(placement))
            {
                var body = RequireBody(slot);
                var animator = RequireAnimator(slot);
                builder.Append(slot.name)
                    .Append("|Body=").Append(AssetDatabase.GetAssetPath(body.sharedMesh))
                    .Append("|BodyMesh=").Append(body.sharedMesh.name)
                    .Append("|Controller=").Append(
                        animator.runtimeAnimatorController != null
                            ? AssetDatabase.GetAssetPath(animator.runtimeAnimatorController)
                            : "None");
                if (slot.name == Slot06Name)
                    builder.Append("|AnimatedHandSwordEnabled=")
                        .Append(RequireRenderer(slot, Slot06HandSwordPath).enabled);
                else
                    builder.Append("|OriginalWaistSwordEnabled=")
                        .Append(RequireRenderer(slot, WaistSwordPath).enabled);
                builder.AppendLine();
            }

            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(Slot06ClipPath);
            if (clip != null)
            {
                var binding = FindVisibilityBinding(clip);
                var curve = string.IsNullOrEmpty(binding.propertyName)
                    ? null
                    : AnimationUtility.GetEditorCurve(clip, binding);
                builder.Append("Slot06VisibilityCurve=");
                if (curve == null) builder.AppendLine("None");
                else
                {
                    builder.AppendLine(string.Join(
                        ";",
                        curve.keys.Select(key => Number(key.time) + ":" + Number(key.value))));
                }
            }
            builder.AppendLine("DeletedCorrectionAssets=" + deleted.Count);
            foreach (var path in deleted) builder.AppendLine("Deleted=" + path);
            if (failures != null)
            {
                builder.AppendLine("Failures=" + failures.Count);
                foreach (var failure in failures) builder.AppendLine("FAIL=" + failure);
            }
            builder.AppendLine();

            var destination = Absolute(InspectionPath);
            Directory.CreateDirectory(Path.GetDirectoryName(destination));
            if (append && File.Exists(destination))
                File.AppendAllText(destination, builder.ToString(), Encoding.UTF8);
            else
                File.WriteAllText(destination, builder.ToString(), Encoding.UTF8);
        }

        private static void AppendInspection(string value)
        {
            var destination = Absolute(InspectionPath);
            Directory.CreateDirectory(Path.GetDirectoryName(destination));
            File.AppendAllText(destination, value, Encoding.UTF8);
        }

        private static void WriteBridgeStage(string path, string stage)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, stage + Environment.NewLine, Encoding.UTF8);
            Debug.Log("IspantPreModelingRestoreBridgeStage=" + stage + ".");
        }

        private static void CaptureLeftWaistCloseupSheet(
            Transform placement,
            string destination)
        {
            var slots = RequireSlots(placement);
            var columns = Mathf.Min(SlotsPerRow, slots.Length);
            var rows = Mathf.CeilToInt(slots.Length / (float)columns);
            var sheet = new Texture2D(
                PanelWidth * columns,
                PanelHeight * rows,
                TextureFormat.RGB24,
                false);
            var target = new RenderTexture(
                PanelWidth,
                PanelHeight,
                24,
                RenderTextureFormat.ARGB32);
            var panel = new Texture2D(
                PanelWidth,
                PanelHeight,
                TextureFormat.RGB24,
                false);
            var cameraObject = new GameObject("IspantPreModelingRestoreCamera")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            var camera = cameraObject.AddComponent<Camera>();
            Fill(sheet, new Color32(16, 18, 22, 255));
            try
            {
                camera.enabled = false;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.055f, 0.063f, 0.075f, 1f);
                camera.orthographic = true;
                camera.aspect = PanelWidth / (float)PanelHeight;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = 8f;
                camera.cullingMask = ~0;
                camera.targetTexture = target;

                for (var index = 0; index < slots.Length; index++)
                {
                    var slot = slots[index];
                    var body = RequireBody(slot);
                    var center = body.bounds.center -
                                 slot.up * body.bounds.extents.y * 0.08f -
                                 slot.right * body.bounds.extents.x * 0.28f;
                    var direction = (slot.forward - slot.right * 0.24f +
                                     slot.up * 0.04f).normalized;
                    camera.orthographicSize = Mathf.Max(
                        0.34f,
                        body.bounds.extents.y * 0.34f);
                    camera.transform.position = center + direction * 6f;
                    camera.transform.rotation = Quaternion.LookRotation(
                        center - camera.transform.position,
                        slot.up);

                    var oldActive = RenderTexture.active;
                    try
                    {
                        camera.Render();
                        RenderTexture.active = target;
                        panel.ReadPixels(
                            new Rect(0f, 0f, PanelWidth, PanelHeight),
                            0,
                            0);
                        panel.Apply();
                    }
                    finally
                    {
                        RenderTexture.active = oldActive;
                    }

                    var column = index % columns;
                    var row = rows - 1 - index / columns;
                    sheet.SetPixels32(
                        column * PanelWidth,
                        row * PanelHeight,
                        PanelWidth,
                        PanelHeight,
                        panel.GetPixels32());
                }

                sheet.Apply();
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
                File.WriteAllBytes(destination, sheet.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = null;
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(panel);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(sheet);
            }
        }

        private static Scene RequireScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded ||
                !string.Equals(scene.path, ScenePath, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    "CargoRunMvp must be the active loaded scene. Current=" + scene.path);
            return scene;
        }

        private static Transform RequirePlacement(Scene scene)
        {
            return scene.GetRootGameObjects()
                       .SingleOrDefault(item => item.name == PlacementName)?.transform ??
                   throw new InvalidOperationException(
                       "The approved Ispant placement root is missing.");
        }

        private static Transform[] RequireSlots(Transform placement)
        {
            var slots = placement.Cast<Transform>()
                .Where(item => item.name.StartsWith("Ispant_", StringComparison.Ordinal))
                .OrderBy(item => item.name, StringComparer.Ordinal)
                .ToArray();
            if (slots.Length != ExpectedSlotCount)
                throw new InvalidOperationException(
                    "Expected 12 placed Ispant slots, found " + slots.Length + ".");
            return slots;
        }

        private static SkinnedMeshRenderer RequireBody(Transform slot)
        {
            return slot.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                       .SingleOrDefault(item => item.name == BodyName && item.sharedMesh != null) ??
                   throw new InvalidOperationException(
                       slot.name + " has no unique char1 body renderer.");
        }

        private static Animator RequireAnimator(Transform slot)
        {
            return slot.GetComponentsInChildren<Animator>(true).FirstOrDefault() ??
                   throw new InvalidOperationException(slot.name + " has no Animator.");
        }

        private static Renderer RequireRenderer(Transform slot, string path)
        {
            var target = slot.Find(path) ??
                         throw new InvalidOperationException(
                             slot.name + " is missing renderer path " + path + ".");
            return target.GetComponent<Renderer>() ??
                   throw new InvalidOperationException(
                       slot.name + " has no Renderer at " + path + ".");
        }

        private static Mesh LoadMesh(string path, string requiredName)
        {
            var meshes = AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<Mesh>()
                .Where(item => string.IsNullOrEmpty(requiredName) || item.name == requiredName)
                .ToArray();
            if (meshes.Length != 1)
                throw new InvalidOperationException(
                    "Expected one mesh at " + path +
                    (string.IsNullOrEmpty(requiredName) ? string.Empty : "/" + requiredName) +
                    ", found " + meshes.Length + ".");
            return meshes[0];
        }

        private static string ExpectedBodyPath(string slotName)
        {
            if (slotName == Slot06Name) return Slot06BodyPath;
            if (slotName == Slot04Name || slotName == Slot05Name) return DrawBodyPath;
            return BaseBodyPath;
        }

        private static string OutsidePlacementSignature(Scene scene)
        {
            var builder = new StringBuilder();
            foreach (var root in scene.GetRootGameObjects()
                         .Where(item => item.name != PlacementName)
                         .OrderBy(item => item.name, StringComparer.Ordinal))
            {
                foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                {
                    builder.Append(AnimationUtility.CalculateTransformPath(
                            transform,
                            root.transform))
                        .Append('|').Append(transform.gameObject.activeSelf)
                        .Append('|').Append(VectorText(transform.localPosition))
                        .Append('|').Append(QuaternionText(transform.localRotation))
                        .Append('|').Append(VectorText(transform.localScale))
                        .AppendLine();
                }
            }
            return builder.ToString();
        }

        private static string Absolute(string relativePath)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", relativePath));
        }

        private static void Fill(Texture2D texture, Color32 color)
        {
            var pixels = Enumerable.Repeat(color, texture.width * texture.height).ToArray();
            texture.SetPixels32(pixels);
            texture.Apply();
        }

        private static string VectorText(Vector3 value)
        {
            return Number(value.x) + "," + Number(value.y) + "," + Number(value.z);
        }

        private static string QuaternionText(Quaternion value)
        {
            return Number(value.x) + "," + Number(value.y) + "," +
                   Number(value.z) + "," + Number(value.w);
        }

        private static string Number(float value)
        {
            return value.ToString("0.######", CultureInfo.InvariantCulture);
        }
    }
}
