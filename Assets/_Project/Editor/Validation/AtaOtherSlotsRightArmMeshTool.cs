using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.AtaCargoRunScene
{
    internal static class AtaOtherSlotsRightArmMeshTool
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementRootName = "Approved Ata Enemy Placement";
        private const string ModelName = "Ata_Model";
        private const string OutputFolder =
            "Assets/_Project/Art/Enemies/Ata/Animations/RightArmCorrections";
        private const string ReviewFolder =
            "docs/validation/ata_slots_mesh_review_2026-08-12/right_arm_slots";

        private static readonly string[] SlotNames =
        {
            "Ata_01_Static",
            "Ata_02_Idle",
            "Ata_03_Move",
            "Ata_06_Sabotage",
            "Ata_07_BombInstall",
            "Ata_08_Hit",
            "Ata_09_Death"
        };

        public static void InspectAndFix()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath || scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be active and clean before inspecting Ata right arms.");
            }

            var placement = scene.GetRootGameObjects()
                                .SingleOrDefault(root => root.name == PlacementRootName) ??
                            throw new InvalidOperationException(
                                "Approved Ata enemy placement is missing.");
            EnsureFolder(OutputFolder);
            var reports = new List<string>();
            var changed = false;
            foreach (var slotName in SlotNames)
            {
                var slot = placement.transform.Find(slotName) ??
                           throw new InvalidOperationException(slotName + " is missing.");
                var model = slot.Find(ModelName) ??
                            throw new InvalidOperationException(slotName + "/Ata_Model is missing.");
                var renderer = model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                    .SingleOrDefault() ??
                    throw new InvalidOperationException(slotName + " must contain one skinned renderer.");
                var animator = model.GetComponentsInChildren<Animator>(true).SingleOrDefault();
                var clips = animator?.runtimeAnimatorController == null
                    ? Array.Empty<AnimationClip>()
                    : animator.runtimeAnimatorController.animationClips
                        .Where(clip => clip != null)
                        .Distinct()
                        .ToArray();
                var components = FindStretchComponents(model, renderer, animator, clips, out var maximumRatio);
                if (components.Count == 0)
                {
                    reports.Add(
                        slotName + ":Clips=" + clips.Length +
                        ",StretchComponents=0,MaximumRatio=" + Num(maximumRatio) +
                        ",Changed=False");
                    continue;
                }

                var corrected = UnityEngine.Object.Instantiate(renderer.sharedMesh);
                corrected.name = slotName + "_RightArmCorrected";
                BindComponentsToDominantBone(corrected, renderer.bones, components);
                var assetPath = OutputFolder + "/" + corrected.name + ".asset";
                if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath) != null &&
                    !AssetDatabase.DeleteAsset(assetPath))
                {
                    throw new InvalidOperationException(
                        "Existing Ata right-arm correction mesh could not be replaced: " + assetPath);
                }

                AssetDatabase.CreateAsset(corrected, assetPath);
                renderer.sharedMesh = corrected;
                EditorUtility.SetDirty(renderer);
                changed = true;
                reports.Add(
                    slotName + ":Clips=" + clips.Length +
                    ",StretchComponents=" + components.Count +
                    ",ComponentTriangles=" + string.Join(",",
                        components.Select(component => component.Length / 3)) +
                    ",MaximumRatio=" + Num(maximumRatio) +
                    ",Changed=True");
            }

            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene, ScenePath))
                {
                    throw new InvalidOperationException(
                        "CargoRunMvp could not be saved after Ata right-arm corrections.");
                }

                AssetDatabase.SaveAssets();
            }

            Debug.Log(
                "AtaOtherSlotsRightArmMeshInspection Result=PASS, " +
                string.Join(";", reports) +
                ", SceneSaved=" + changed + ".");
        }

        public static void CaptureCurrentRightArmReview()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath || scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be active and clean before capturing Ata right arms.");
            }

            var placement = scene.GetRootGameObjects()
                                .SingleOrDefault(root => root.name == PlacementRootName) ??
                            throw new InvalidOperationException(
                                "Approved Ata enemy placement is missing.");
            var allRenderers = placement.GetComponentsInChildren<Renderer>(true);
            var rendererStates = allRenderers.ToDictionary(item => item, item => item.enabled);
            var cameraObject = new GameObject("Ata Right Arm Review Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.fieldOfView = 28f;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 100f;
            camera.allowHDR = false;
            camera.allowMSAA = true;
            var absoluteFolder = Absolute(ReviewFolder);
            Directory.CreateDirectory(absoluteFolder);
            try
            {
                foreach (var slotName in SlotNames.Concat(new[] { "Ata_05_Command" }))
                {
                    foreach (var item in allRenderers)
                    {
                        item.enabled = false;
                    }

                    var slot = placement.transform.Find(slotName) ??
                               throw new InvalidOperationException(slotName + " is missing.");
                    var model = slot.Find(ModelName) ??
                                throw new InvalidOperationException(slotName + "/Ata_Model is missing.");
                    var renderer = model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                        .SingleOrDefault() ??
                        throw new InvalidOperationException(slotName + " must contain one skinned renderer.");
                    renderer.enabled = true;
                    var animator = model.GetComponentsInChildren<Animator>(true).SingleOrDefault();
                    var animatorEnabled = animator != null && animator.enabled;
                    if (animator != null)
                    {
                        animator.enabled = false;
                    }

                    var transforms = model.GetComponentsInChildren<Transform>(true);
                    var snapshots = transforms.Select(item => new LocalTransformSnapshot(item)).ToArray();
                    var clips = animator?.runtimeAnimatorController == null
                        ? Array.Empty<AnimationClip>()
                        : animator.runtimeAnimatorController.animationClips
                            .Where(clip => clip != null)
                            .Distinct()
                            .ToArray();
                    var sampleCount = clips.Length == 0 ? 1 : 3;
                    try
                    {
                        for (var sampleIndex = 0; sampleIndex < sampleCount; sampleIndex++)
                        {
                            if (clips.Length > 0)
                            {
                                clips[0].SampleAnimation(
                                    model.gameObject,
                                    clips[0].length * sampleIndex / (sampleCount - 1f));
                            }

                            var bounds = renderer.bounds;
                            var lookDirection = Quaternion.AngleAxis(38f, model.up) * model.forward;
                            var target = bounds.center + model.up * bounds.extents.y * 0.18f;
                            var distance = bounds.extents.magnitude /
                                           Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f) * 1.15f;
                            camera.transform.position = target - lookDirection.normalized * distance;
                            camera.transform.rotation = Quaternion.LookRotation(
                                target - camera.transform.position,
                                model.up);
                            CaptureCamera(
                                camera,
                                Path.Combine(
                                    absoluteFolder,
                                    slotName + "_RightArm_" + sampleIndex + ".png"));
                        }
                    }
                    finally
                    {
                        foreach (var snapshot in snapshots)
                        {
                            snapshot.Restore();
                        }

                        if (animator != null)
                        {
                            animator.enabled = animatorEnabled;
                        }
                    }
                }
            }
            finally
            {
                foreach (var state in rendererStates)
                {
                    state.Key.enabled = state.Value;
                }

                UnityEngine.Object.DestroyImmediate(cameraObject);
            }

            Debug.Log(
                "AtaCurrentRightArmReviewCaptured Result=PASS, Folder=" + ReviewFolder +
                ", SceneChanged=False.");
        }

        internal static int CorrectModelForClips(
            string slotName,
            Transform model,
            IReadOnlyList<AnimationClip> clips)
        {
            var renderer = model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .SingleOrDefault() ??
                throw new InvalidOperationException(slotName + " must contain one skinned renderer.");
            var animator = model.GetComponentsInChildren<Animator>(true).SingleOrDefault();
            var components = FindStretchComponents(
                model,
                renderer,
                animator,
                clips,
                out var maximumRatio);
            if (components.Count == 0)
            {
                Debug.Log(
                    "AtaRightArmModelCorrection Slot=" + slotName +
                    ", StretchComponents=0" +
                    ", MaximumRatio=" + Num(maximumRatio) +
                    ", Changed=False");
                return 0;
            }

            EnsureFolder(OutputFolder);
            var corrected = UnityEngine.Object.Instantiate(renderer.sharedMesh);
            corrected.name = slotName + "_RightArmCorrected";
            BindComponentsToDominantBone(corrected, renderer.bones, components);
            var assetPath = OutputFolder + "/" + corrected.name + ".asset";
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath) != null &&
                !AssetDatabase.DeleteAsset(assetPath))
            {
                throw new InvalidOperationException(
                    "Existing Ata right-arm correction mesh could not be replaced: " + assetPath);
            }

            AssetDatabase.CreateAsset(corrected, assetPath);
            renderer.sharedMesh = corrected;
            EditorUtility.SetDirty(renderer);
            Debug.Log(
                "AtaRightArmModelCorrection Slot=" + slotName +
                ", StretchComponents=" + components.Count +
                ", ComponentTriangles=" + string.Join(",",
                    components.Select(component => component.Length / 3)) +
                ", MaximumRatio=" + Num(maximumRatio) +
                ", Changed=True");
            return components.Count;
        }

        private static List<int[]> FindStretchComponents(
            Transform model,
            SkinnedMeshRenderer renderer,
            Animator animator,
            IReadOnlyList<AnimationClip> clips,
            out float maximumRatio)
        {
            var mesh = renderer.sharedMesh;
            var triangles = mesh.GetTriangles(0);
            var weights = mesh.boneWeights;
            var rightArmBones = renderer.bones
                .Select((bone, index) => (bone, index))
                .Where(item => item.bone != null &&
                               (item.bone.name == "RightShoulder" ||
                                item.bone.name == "RightArm" ||
                                item.bone.name == "RightForeArm" ||
                                item.bone.name == "RightHand"))
                .Select(item => item.index)
                .ToHashSet();
            if (rightArmBones.Count != 4 || weights.Length != mesh.vertexCount)
            {
                throw new InvalidOperationException(
                    "Ata right-arm inspection could not resolve skin data.");
            }

            var transforms = model.GetComponentsInChildren<Transform>(true);
            var snapshots = transforms.Select(item => new LocalTransformSnapshot(item)).ToArray();
            var animatorEnabled = animator != null && animator.enabled;
            var reference = new Mesh();
            var sample = new Mesh();
            var stretched = new HashSet<(int, int, int)>();
            maximumRatio = 1f;
            try
            {
                if (animator != null)
                {
                    animator.enabled = false;
                }

                var clipsToInspect = clips.Count == 0
                    ? new AnimationClip[] { null }
                    : clips.ToArray();
                foreach (var clip in clipsToInspect)
                {
                    if (clip != null)
                    {
                        clip.SampleAnimation(model.gameObject, 0f);
                    }

                    renderer.BakeMesh(reference, false);
                    var referenceVertices = reference.vertices;
                    var minimumExpansion = reference.bounds.size.magnitude * 0.025f;
                    var sampleCount = clip == null ? 0 : 24;
                    for (var sampleIndex = 0; sampleIndex <= sampleCount; sampleIndex++)
                    {
                        if (clip != null)
                        {
                            clip.SampleAnimation(
                                model.gameObject,
                                clip.length * sampleIndex / sampleCount);
                        }

                        renderer.BakeMesh(sample, false);
                        var posedVertices = sample.vertices;
                        for (var index = 0; index < triangles.Length; index += 3)
                        {
                            var triangle = (
                                triangles[index],
                                triangles[index + 1],
                                triangles[index + 2]);
                            if (!TriangleHasRightArmWeight(triangle, weights, rightArmBones))
                            {
                                continue;
                            }

                            var referenceEdge = MaximumEdge(referenceVertices, triangle);
                            if (referenceEdge <= 0.000001f)
                            {
                                continue;
                            }

                            var posedEdge = MaximumEdge(posedVertices, triangle);
                            var ratio = posedEdge / referenceEdge;
                            maximumRatio = Mathf.Max(maximumRatio, ratio);
                            if (ratio >= 1.75f && posedEdge - referenceEdge >= minimumExpansion)
                            {
                                stretched.Add(triangle);
                            }
                        }
                    }

                    foreach (var snapshot in snapshots)
                    {
                        snapshot.Restore();
                    }
                }
            }
            finally
            {
                foreach (var snapshot in snapshots)
                {
                    snapshot.Restore();
                }

                if (animator != null)
                {
                    animator.enabled = animatorEnabled;
                }

                UnityEngine.Object.DestroyImmediate(reference);
                UnityEngine.Object.DestroyImmediate(sample);
            }

            if (stretched.Count == 0)
            {
                return new List<int[]>();
            }

            var components = SplitIndexEdgeComponents(triangles)
                .Where(component => Enumerable.Range(0, component.Length / 3)
                    .Any(index => stretched.Contains((
                        component[index * 3],
                        component[index * 3 + 1],
                        component[index * 3 + 2]))))
                .ToList();
            if (components.Any(component => component.Length / 3 > 64) ||
                components.Sum(component => component.Length / 3) > 512)
            {
                throw new InvalidOperationException(
                    "Ata right-arm correction exceeded the separated-component contract.");
            }

            return components;
        }

        private static void BindComponentsToDominantBone(
            Mesh mesh,
            IReadOnlyList<Transform> bones,
            IReadOnlyList<int[]> components)
        {
            var weights = mesh.boneWeights;
            foreach (var component in components)
            {
                var vertices = component.Distinct().ToArray();
                var totals = new Dictionary<int, float>();
                foreach (var vertex in vertices)
                {
                    var weight = weights[vertex];
                    Add(totals, weight.boneIndex0, weight.weight0);
                    Add(totals, weight.boneIndex1, weight.weight1);
                    Add(totals, weight.boneIndex2, weight.weight2);
                    Add(totals, weight.boneIndex3, weight.weight3);
                }

                var dominant = totals.OrderByDescending(item => item.Value).First().Key;
                if (dominant < 0 || dominant >= bones.Count || bones[dominant] == null)
                {
                    throw new InvalidOperationException(
                        "Ata correction component dominant bone is invalid.");
                }

                foreach (var vertex in vertices)
                {
                    weights[vertex] = new BoneWeight
                    {
                        boneIndex0 = dominant,
                        weight0 = 1f
                    };
                }
            }

            mesh.boneWeights = weights;
        }

        private static void Add(IDictionary<int, float> totals, int bone, float weight)
        {
            if (weight <= 0f)
            {
                return;
            }

            totals[bone] = totals.TryGetValue(bone, out var total) ? total + weight : weight;
        }

        private static bool TriangleHasRightArmWeight(
            (int, int, int) triangle,
            IReadOnlyList<BoneWeight> weights,
            ISet<int> bones)
        {
            return HasWeight(weights[triangle.Item1], bones) ||
                   HasWeight(weights[triangle.Item2], bones) ||
                   HasWeight(weights[triangle.Item3], bones);
        }

        private static bool HasWeight(BoneWeight weight, ISet<int> bones)
        {
            return (weight.weight0 > 0.001f && bones.Contains(weight.boneIndex0)) ||
                   (weight.weight1 > 0.001f && bones.Contains(weight.boneIndex1)) ||
                   (weight.weight2 > 0.001f && bones.Contains(weight.boneIndex2)) ||
                   (weight.weight3 > 0.001f && bones.Contains(weight.boneIndex3));
        }

        private static float MaximumEdge(
            IReadOnlyList<Vector3> vertices,
            (int, int, int) triangle)
        {
            return Mathf.Max(
                Vector3.Distance(vertices[triangle.Item1], vertices[triangle.Item2]),
                Vector3.Distance(vertices[triangle.Item2], vertices[triangle.Item3]),
                Vector3.Distance(vertices[triangle.Item3], vertices[triangle.Item1]));
        }

        private static List<int[]> SplitIndexEdgeComponents(IReadOnlyList<int> triangles)
        {
            var edges = new Dictionary<(int, int), List<int>>();
            for (var triangleIndex = 0; triangleIndex < triangles.Count / 3; triangleIndex++)
            {
                var values = new[]
                {
                    triangles[triangleIndex * 3],
                    triangles[triangleIndex * 3 + 1],
                    triangles[triangleIndex * 3 + 2]
                };
                for (var edgeIndex = 0; edgeIndex < 3; edgeIndex++)
                {
                    var first = values[edgeIndex];
                    var second = values[(edgeIndex + 1) % 3];
                    var edge = first < second ? (first, second) : (second, first);
                    if (!edges.TryGetValue(edge, out var connected))
                    {
                        connected = new List<int>();
                        edges.Add(edge, connected);
                    }

                    connected.Add(triangleIndex);
                }
            }

            var adjacency = Enumerable.Range(0, triangles.Count / 3)
                .ToDictionary(index => index, _ => new HashSet<int>());
            foreach (var connected in edges.Values.Where(value => value.Count > 1))
            {
                foreach (var first in connected)
                foreach (var second in connected)
                if (first != second)
                {
                    adjacency[first].Add(second);
                }
            }

            var remaining = adjacency.Keys.ToHashSet();
            var result = new List<int[]>();
            while (remaining.Count > 0)
            {
                var seed = remaining.First();
                remaining.Remove(seed);
                var found = new HashSet<int> { seed };
                var stack = new Stack<int>();
                stack.Push(seed);
                while (stack.Count > 0)
                {
                    foreach (var next in adjacency[stack.Pop()])
                    {
                        if (remaining.Remove(next))
                        {
                            found.Add(next);
                            stack.Push(next);
                        }
                    }
                }

                var component = new List<int>(found.Count * 3);
                foreach (var index in found.OrderBy(value => value))
                {
                    component.Add(triangles[index * 3]);
                    component.Add(triangles[index * 3 + 1]);
                    component.Add(triangles[index * 3 + 2]);
                }

                result.Add(component.ToArray());
            }

            return result;
        }

        private static void EnsureFolder(string path)
        {
            var current = "Assets";
            foreach (var segment in path.Substring("Assets/".Length).Split('/'))
            {
                var next = current + "/" + segment;
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, segment);
                }

                current = next;
            }
        }

        private static void CaptureCamera(Camera camera, string destination)
        {
            var texture = new RenderTexture(720, 720, 24, RenderTextureFormat.ARGB32);
            var image = new Texture2D(720, 720, TextureFormat.RGB24, false);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            try
            {
                camera.targetTexture = texture;
                camera.Render();
                RenderTexture.active = texture;
                image.ReadPixels(new Rect(0, 0, 720, 720), 0, 0);
                image.Apply();
                File.WriteAllBytes(destination, image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                UnityEngine.Object.DestroyImmediate(image);
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static string Absolute(string relativePath)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", relativePath));
        }

        private static string Num(float value) =>
            value.ToString("0.######", System.Globalization.CultureInfo.InvariantCulture);

        private readonly struct LocalTransformSnapshot
        {
            private readonly Transform transform;
            private readonly Vector3 position;
            private readonly Quaternion rotation;
            private readonly Vector3 scale;

            public LocalTransformSnapshot(Transform transform)
            {
                this.transform = transform;
                position = transform.localPosition;
                rotation = transform.localRotation;
                scale = transform.localScale;
            }

            public void Restore()
            {
                transform.localPosition = position;
                transform.localRotation = rotation;
                transform.localScale = scale;
            }
        }
    }
}
