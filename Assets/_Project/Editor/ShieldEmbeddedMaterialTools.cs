using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.Validation
{
    internal static class ShieldEmbeddedMaterialTools
    {
        private const string ScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string OriginalModelPath = "item model/shield.fbx";
        internal const string ModelPath = "Assets/_Project/Art/Items/Shield/shield.fbx";
        private const string ReviewDirectory = "docs/validation/shield_handle_materials_2026-09-07";
        private const string TextureDirectory = "Assets/_Project/Art/Items/Shield/Textures";
        private const string MaterialDirectory = "Assets/_Project/Art/Items/Shield/Materials";
        private const string AutomaticTextureDirectory = "Assets/_Project/Art/Items/Shield/shield.fbm";
        private static readonly string[] TargetNames =
        {
            "Shield_Draw", "Shield_Idle", "Shield_Stow", "Shield_Raise",
            "Shield_Block_Idle", "Shield_Lower", "Shield_Block_Impact", "Shield_Break_Reaction"
        };

        internal static void Inspect()
        {
            Scene scene = RequireScene();
            Directory.CreateDirectory(ReviewDirectory);
            if (HashBytes(File.ReadAllBytes(OriginalModelPath)) != HashBytes(File.ReadAllBytes(ModelPath)))
                throw new InvalidOperationException("The imported shield FBX differs from the specified original.");

            List<FbxNode> roots = ReadFbx();
            FbxNode objects = roots.Single(node => node.Name == "Objects");
            FbxNode connections = roots.Single(node => node.Name == "Connections");
            var report = new StringBuilder();
            report.AppendLine("Original/imported FBX SHA256=" + HashBytes(File.ReadAllBytes(OriginalModelPath)));
            report.AppendLine("FBX materials=" + objects.Children.Count(node => node.Name == "Material") +
                " textures=" + objects.Children.Count(node => node.Name == "Texture") +
                " videos=" + objects.Children.Count(node => node.Name == "Video"));
            foreach (FbxNode node in objects.Children.Where(item =>
                item.Name == "Material" || item.Name == "Texture" || item.Name == "Video"))
                DescribeNode(report, node, string.Empty);
            DescribeNode(report, connections, string.Empty);

            GameObject imported = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath) ??
                throw new InvalidOperationException("Imported shield prefab is missing.");
            report.AppendLine("Unity imported shield hierarchy:");
            foreach (Renderer renderer in imported.GetComponentsInChildren<Renderer>(true))
            {
                string path = AnimationUtility.CalculateTransformPath(renderer.transform, imported.transform);
                report.AppendLine("Renderer path=" + path + " type=" + renderer.GetType().Name +
                    " bounds=" + renderer.bounds + " materials=" +
                    string.Join("|", renderer.sharedMaterials.Select(material => material == null
                        ? "NULL"
                        : material.name + "@" + AssetDatabase.GetAssetPath(material))));
                var filter = renderer.GetComponent<MeshFilter>();
                if (filter != null && filter.sharedMesh != null)
                {
                    report.AppendLine("Mesh=" + filter.sharedMesh.name + " vertices=" + filter.sharedMesh.vertexCount +
                        " subMeshes=" + filter.sharedMesh.subMeshCount + " localBounds=" +
                        FormatBounds(filter.sharedMesh.bounds));
                    DescribeConnectedComponents(report, filter.sharedMesh);
                }
            }

            foreach (string targetName in TargetNames)
            {
                Transform target = FindUnique(scene, targetName).transform;
                Transform forearm = RequireNamedTransform(target, "RightForeArm");
                Transform shield = forearm.Cast<Transform>().Single(child => child.name == "Shield_RightForeArm");
                report.AppendLine(targetName + " shieldLocalPosition=" + shield.localPosition.ToString("R") +
                    " shieldLocalRotation=" + shield.localRotation.ToString("R") +
                    " shieldLocalScale=" + shield.localScale.ToString("R"));
            }

            File.WriteAllText(ReviewDirectory + "/inspection.txt", report.ToString(), Encoding.UTF8);
            Debug.Log("Shield embedded material and geometry inspection complete. " + report);
        }

        internal static void Apply()
        {
            Scene scene = RequireScene();
            if (scene.isDirty)
                throw new InvalidOperationException("Save the current approved Shield scene changes before material application.");
            Transform[] targets = TargetNames.Select(name => FindUnique(scene, name).transform).ToArray();
            string transformsBefore = DescribeTransforms(targets);
            string sourceHash = HashBytes(File.ReadAllBytes(OriginalModelPath));
            if (sourceHash != HashBytes(File.ReadAllBytes(ModelPath)))
                throw new InvalidOperationException("The imported shield FBX differs from the specified original.");

            List<FbxNode> roots = ReadFbx();
            FbxNode objects = roots.Single(node => node.Name == "Objects");
            FbxNode connections = roots.Single(node => node.Name == "Connections");
            FbxNode authoredMaterial = objects.Children.Single(node => node.Name == "Material");
            string materialName = Convert.ToString(authoredMaterial.Values[1]).Split('\0')[0];
            long materialId = Convert.ToInt64(authoredMaterial.Values[0]);
            var bindings = new Dictionary<string, FbxNode>(StringComparer.Ordinal);
            foreach (FbxNode connection in connections.Children.Where(node => node.Values.Count >= 4 &&
                Convert.ToString(node.Values[0]) == "OP" && Convert.ToInt64(node.Values[2]) == materialId))
            {
                long textureId = Convert.ToInt64(connection.Values[1]);
                FbxNode videoConnection = connections.Children.First(node => node.Values.Count >= 3 &&
                    Convert.ToString(node.Values[0]) == "OO" && Convert.ToInt64(node.Values[2]) == textureId);
                long videoId = Convert.ToInt64(videoConnection.Values[1]);
                bindings.Add(Convert.ToString(connection.Values[3]), objects.Children.Single(node =>
                    node.Name == "Video" && Convert.ToInt64(node.Values[0]) == videoId));
            }
            foreach (string slot in new[] { "DiffuseColor", "NormalMap", "ShininessExponent", "ReflectionFactor" })
                if (!bindings.ContainsKey(slot))
                    throw new InvalidDataException("The shield FBX lacks authored texture binding " + slot + ".");

            CreateFolder(TextureDirectory);
            CreateFolder(MaterialDirectory);
            var paths = new Dictionary<string, string>(StringComparer.Ordinal);
            var report = new StringBuilder();
            foreach (KeyValuePair<string, FbxNode> binding in bindings)
            {
                string filename = Path.GetFileName(Convert.ToString(binding.Value.Children
                    .Single(node => node.Name == "RelativeFilename").Values[0]));
                byte[] bytes = (byte[])binding.Value.Children.Single(node => node.Name == "Content").Values[0];
                string path = TextureDirectory + "/" + filename;
                if (File.Exists(path) && !File.ReadAllBytes(path).SequenceEqual(bytes))
                    throw new InvalidOperationException("Existing Shield texture differs from its embedded bytes: " + path);
                if (!File.Exists(path)) File.WriteAllBytes(path, bytes);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter ??
                    throw new InvalidOperationException("Shield texture importer is unavailable: " + path);
                importer.textureType = binding.Key == "NormalMap" ? TextureImporterType.NormalMap : TextureImporterType.Default;
                importer.sRGBTexture = binding.Key == "DiffuseColor";
                importer.maxTextureSize = 8192;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
                if (!File.ReadAllBytes(path).SequenceEqual(bytes))
                    throw new InvalidDataException("Extracted Shield texture bytes changed: " + path);
                paths.Add(binding.Key, path);
                report.AppendLine(binding.Key + " -> " + path + " embeddedSHA256=" + HashBytes(bytes));
            }

            string packedPath = PackMetallicSmoothness(paths["ReflectionFactor"], paths["ShininessExponent"]);
            string materialPath = MaterialDirectory + "/" + materialName + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                Material embedded = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<Material>()
                    .SingleOrDefault(item => item.name == materialName);
                if (embedded == null)
                    throw new InvalidOperationException("The original imported Shield material was not found.");
                material = new Material(embedded) { name = materialName };
                AssetDatabase.CreateAsset(material, materialPath);
            }
            if (material.shader.name != "Universal Render Pipeline/Lit")
                throw new InvalidOperationException("Unexpected Shield shader; no arbitrary shader replacement was performed.");
            Texture2D baseColor = AssetDatabase.LoadAssetAtPath<Texture2D>(paths["DiffuseColor"]);
            material.SetTexture("_BaseMap", baseColor);
            material.SetTexture("_MainTex", baseColor);
            material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(paths["NormalMap"]));
            material.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>(packedPath));
            material.SetFloat("_Metallic", 1f);
            material.SetFloat("_Smoothness", 1f);
            material.SetFloat("_SmoothnessTextureChannel", 0f);
            material.EnableKeyword("_NORMALMAP");
            material.EnableKeyword("_METALLICSPECGLOSSMAP");
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssetIfDirty(material);

            var modelImporter = AssetImporter.GetAtPath(ModelPath) as ModelImporter ??
                throw new InvalidOperationException("Shield model importer is unavailable.");
            modelImporter.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), materialName), material);
            modelImporter.materialLocation = ModelImporterMaterialLocation.External;
            modelImporter.materialName = ModelImporterMaterialName.BasedOnMaterialName;
            modelImporter.materialSearch = ModelImporterMaterialSearch.Local;
            modelImporter.SaveAndReimport();
            if (AssetDatabase.IsValidFolder(AutomaticTextureDirectory) &&
                !AssetDatabase.DeleteAsset(AutomaticTextureDirectory))
                throw new InvalidOperationException("Unity's duplicate automatic Shield texture folder could not be removed.");

            GameObject imported = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath) ??
                throw new InvalidOperationException("The remapped Shield prefab is unavailable.");
            Dictionary<string, Renderer> sourceRenderers = imported.GetComponentsInChildren<Renderer>(true)
                .ToDictionary(renderer => AnimationUtility.CalculateTransformPath(renderer.transform, imported.transform));
            foreach (Transform target in targets)
            {
                Transform shield = FindShield(target);
                foreach (Renderer renderer in shield.GetComponentsInChildren<Renderer>(true))
                {
                    string relativePath = AnimationUtility.CalculateTransformPath(renderer.transform, shield);
                    Material[] importedMaterials = sourceRenderers[relativePath].sharedMaterials;
                    if (importedMaterials.Any(item => item != material))
                        throw new InvalidOperationException("Shield material remap did not resolve to the extracted material.");
                    Undo.RecordObject(renderer, "Apply Shield embedded material");
                    renderer.sharedMaterials = importedMaterials;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                    EditorUtility.SetDirty(renderer);
                }
            }
            if (transformsBefore != DescribeTransforms(targets))
                throw new InvalidOperationException("Shield material application changed a target transform.");
            if (sourceHash != HashBytes(File.ReadAllBytes(OriginalModelPath)) ||
                sourceHash != HashBytes(File.ReadAllBytes(ModelPath)))
                throw new InvalidOperationException("Shield FBX content changed during material application.");
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("CargoRunMvp could not be saved after Shield material application.");
            report.AppendLine("Original/imported FBX unchanged SHA256=" + sourceHash);
            report.AppendLine("All eight Shield renderers use=" + materialPath);
            report.AppendLine("All target hierarchy transforms unchanged=True");
            report.AppendLine("Persistent local FBX material remap=True");
            report.AppendLine("Duplicate automatic shield.fbm extraction removed=True");
            DescribeMaterial(report, material);
            File.WriteAllText(ReviewDirectory + "/application.txt", report.ToString(), Encoding.UTF8);
            Debug.Log("Original embedded Shield textures and material applied to all eight targets. " + report);
        }

        private static string PackMetallicSmoothness(string metallicPath, string roughnessPath)
        {
            var metallic = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            var roughness = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            Texture2D packed = null;
            try
            {
                if (!metallic.LoadImage(File.ReadAllBytes(metallicPath)) ||
                    !roughness.LoadImage(File.ReadAllBytes(roughnessPath)))
                    throw new InvalidDataException("Embedded Shield PBR image decoding failed.");
                if (metallic.width != roughness.width || metallic.height != roughness.height)
                    throw new InvalidDataException("Embedded Shield PBR dimensions differ; no resampling was performed.");
                Color32[] pixels = metallic.GetPixels32();
                Color32[] roughnessPixels = roughness.GetPixels32();
                for (int index = 0; index < pixels.Length; index++)
                    pixels[index] = new Color32(pixels[index].r, pixels[index].r, pixels[index].r,
                        (byte)(255 - roughnessPixels[index].r));
                packed = new Texture2D(metallic.width, metallic.height, TextureFormat.RGBA32, false, true);
                packed.SetPixels32(pixels);
                packed.Apply();
                string path = TextureDirectory + "/shield_metallic_smoothness.png";
                File.WriteAllBytes(path, packed.EncodeToPNG());
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.sRGBTexture = false;
                importer.maxTextureSize = 8192;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
                return path;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(metallic);
                UnityEngine.Object.DestroyImmediate(roughness);
                if (packed != null) UnityEngine.Object.DestroyImmediate(packed);
            }
        }

        private static void CreateFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            CreateFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        private static void DescribeMaterial(StringBuilder report, Material material)
        {
            report.AppendLine("Material=" + material.name + " shader=" + material.shader.name +
                " asset=" + AssetDatabase.GetAssetPath(material));
            foreach (string property in new[] { "_BaseMap", "_BumpMap", "_MetallicGlossMap" })
            {
                Texture texture = material.GetTexture(property);
                report.AppendLine(property + "=" + (texture == null
                    ? "NULL"
                    : texture.name + "@" + AssetDatabase.GetAssetPath(texture)));
            }
        }

        private static string DescribeTransforms(IEnumerable<Transform> targets) =>
            string.Join("\n", targets.SelectMany(target => target.GetComponentsInChildren<Transform>(true))
                .Select(item => item.GetInstanceID() + "|" + (item.parent == null ? 0 : item.parent.GetInstanceID()) + "|" +
                    item.localPosition.ToString("R") + "|" + item.localRotation.ToString("R") + "|" +
                    item.localScale.ToString("R")));

        private static Transform FindShield(Transform target)
        {
            Transform forearm = RequireNamedTransform(target, "RightForeArm");
            Transform[] matches = forearm.Cast<Transform>().Where(child => child.name == "Shield_RightForeArm").ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException(target.name + " requires one right-forearm Shield instance.");
            return matches[0];
        }

        private sealed class FbxNode
        {
            internal string Name;
            internal readonly List<object> Values = new List<object>();
            internal readonly List<FbxNode> Children = new List<FbxNode>();
        }

        private static List<FbxNode> ReadFbx()
        {
            using (var reader = new BinaryReader(File.OpenRead(OriginalModelPath)))
            {
                string header = Encoding.ASCII.GetString(reader.ReadBytes(23));
                if (!header.StartsWith("Kaydara FBX Binary", StringComparison.Ordinal))
                    throw new InvalidDataException("The source is not a binary FBX.");
                bool wide = reader.ReadUInt32() >= 7500;
                var roots = new List<FbxNode>();
                while (reader.BaseStream.Position < reader.BaseStream.Length)
                {
                    FbxNode node = ReadNode(reader, wide);
                    if (node == null) break;
                    roots.Add(node);
                }
                return roots;
            }
        }

        private static FbxNode ReadNode(BinaryReader reader, bool wide)
        {
            long end = wide ? (long)reader.ReadUInt64() : reader.ReadUInt32();
            long count = wide ? (long)reader.ReadUInt64() : reader.ReadUInt32();
            if (wide) reader.ReadUInt64(); else reader.ReadUInt32();
            int nameLength = reader.ReadByte();
            if (end == 0) return null;
            var node = new FbxNode { Name = Encoding.UTF8.GetString(reader.ReadBytes(nameLength)) };
            for (long index = 0; index < count; index++)
            {
                char type = (char)reader.ReadByte();
                object value;
                switch (type)
                {
                    case 'Y': value = reader.ReadInt16(); break;
                    case 'C': value = reader.ReadByte(); break;
                    case 'I': value = reader.ReadInt32(); break;
                    case 'F': value = reader.ReadSingle(); break;
                    case 'D': value = reader.ReadDouble(); break;
                    case 'L': value = reader.ReadInt64(); break;
                    case 'S': value = Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadInt32())); break;
                    case 'R': value = reader.ReadBytes(reader.ReadInt32()); break;
                    case 'f': case 'd': case 'l': case 'i': case 'b': case 'c':
                        uint arrayCount = reader.ReadUInt32();
                        reader.ReadUInt32();
                        uint bytes = reader.ReadUInt32();
                        reader.BaseStream.Seek(bytes, SeekOrigin.Current);
                        value = "[array " + arrayCount + "]";
                        break;
                    default: throw new InvalidDataException("Unsupported FBX property " + type);
                }
                node.Values.Add(value);
            }
            while (reader.BaseStream.Position < end)
            {
                FbxNode child = ReadNode(reader, wide);
                if (child == null) break;
                node.Children.Add(child);
            }
            reader.BaseStream.Position = end;
            return node;
        }

        private static void DescribeNode(StringBuilder report, FbxNode node, string indent)
        {
            report.AppendLine(indent + node.Name + " | " + string.Join(" | ", node.Values.Select(value =>
                value is byte[] bytes
                    ? "[embedded bytes=" + bytes.Length + " SHA256=" + HashBytes(bytes) + "]"
                    : Convert.ToString(value).Replace('\0', '|'))));
            foreach (FbxNode child in node.Children) DescribeNode(report, child, indent + "  ");
        }

        private static Scene RequireScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Shield asset inspection requires Edit Mode.");
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.isLoaded || scene.path != ScenePath)
                throw new InvalidOperationException("The existing CargoRunMvp scene must be active.");
            return scene;
        }

        private static GameObject FindUnique(Scene scene, string name)
        {
            GameObject[] matches = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Where(transform => transform.name == name)
                .Select(transform => transform.gameObject)
                .ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException(name + " expected exactly once, found " + matches.Length + ".");
            return matches[0];
        }

        private static Transform RequireNamedTransform(Transform root, string name)
        {
            Transform[] matches = root.GetComponentsInChildren<Transform>(true)
                .Where(item => item.name == name).ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException(root.name + " requires one " + name + ".");
            return matches[0];
        }

        private static string HashBytes(byte[] bytes)
        {
            using (SHA256 sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", string.Empty);
        }

        private static void DescribeConnectedComponents(StringBuilder report, Mesh mesh)
        {
            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;
            var parent = Enumerable.Range(0, vertices.Length).ToArray();
            var coincident = new Dictionary<Vector3, int>();
            for (int index = 0; index < vertices.Length; index++)
            {
                if (coincident.TryGetValue(vertices[index], out int existing)) Union(parent, existing, index);
                else coincident.Add(vertices[index], index);
            }
            for (int index = 0; index < triangles.Length; index += 3)
            {
                Union(parent, triangles[index], triangles[index + 1]);
                Union(parent, triangles[index], triangles[index + 2]);
            }
            var groups = Enumerable.Range(0, vertices.Length)
                .GroupBy(index => Find(parent, index))
                .Select(group =>
                {
                    int[] indices = group.ToArray();
                    Bounds bounds = new Bounds(vertices[indices[0]], Vector3.zero);
                    foreach (int index in indices.Skip(1)) bounds.Encapsulate(vertices[index]);
                    int triangleCount = 0;
                    for (int triangle = 0; triangle < triangles.Length; triangle += 3)
                        if (Find(parent, triangles[triangle]) == group.Key) triangleCount++;
                    return new { VertexCount = indices.Length, TriangleCount = triangleCount, Bounds = bounds };
                })
                .OrderByDescending(group => group.VertexCount)
                .ToArray();
            report.AppendLine("ConnectedComponents=" + groups.Length);
            for (int index = 0; index < groups.Length; index++)
                report.AppendLine("Component[" + index + "] vertices=" + groups[index].VertexCount +
                    " triangles=" + groups[index].TriangleCount + " " + FormatBounds(groups[index].Bounds));
        }

        private static int Find(int[] parent, int value)
        {
            while (parent[value] != value)
            {
                parent[value] = parent[parent[value]];
                value = parent[value];
            }
            return value;
        }

        private static void Union(int[] parent, int left, int right)
        {
            int leftRoot = Find(parent, left);
            int rightRoot = Find(parent, right);
            if (leftRoot != rightRoot) parent[rightRoot] = leftRoot;
        }

        private static string FormatBounds(Bounds bounds) =>
            "center=" + bounds.center.ToString("R") + " size=" + bounds.size.ToString("R") +
            " min=" + bounds.min.ToString("R") + " max=" + bounds.max.ToString("R");
    }
}
