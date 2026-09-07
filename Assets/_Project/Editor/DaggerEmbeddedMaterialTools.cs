using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor
{
    internal static class DaggerEmbeddedMaterialTools
    {
        internal const string ModelPath = "Assets/_Project/Art/Items/Dagger/dagger.fbx";
        private const string ReviewDirectory = "docs/validation/dagger_embedded_materials_2026-09-07";
        private const string TextureDirectory = "Assets/_Project/Art/Items/Dagger/Textures";
        private const string MaterialDirectory = "Assets/_Project/Art/Items/Dagger/Materials";
        private static readonly string[] TargetNames =
        {
            "Dagger_Idle", "Dagger_Stab", "Dagger_Throw_Mode",
            "Dagger_Throw_Release", "Dagger_Throw_Cancel"
        };

        internal static void Inspect()
        {
            Scene scene = RequireScene();
            Transform[] targets = FindTargets(scene);
            Directory.CreateDirectory(ReviewDirectory);
            StringBuilder report = new StringBuilder();
            DescribeFbxSource();
            report.AppendLine("Scene dirty: " + scene.isDirty);
            report.AppendLine("Render pipeline: " + UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline);
            foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(ModelPath))
            {
                report.AppendLine(asset.GetType().Name + ": " + asset.name);
                if (asset is Material material) DescribeMaterial(report, material);
            }
            foreach (Transform target in targets)
            {
                Transform item = FindItem(target);
                report.AppendLine(target.name + " item=" + AnimationUtility.CalculateTransformPath(item, target));
                report.AppendLine("position=" + item.localPosition.ToString("F9") + " rotation=" + item.localRotation.ToString("F9") + " scale=" + item.localScale);
                foreach (Renderer renderer in item.GetComponentsInChildren<Renderer>(true))
                {
                    report.AppendLine("Renderer " + renderer.name);
                    foreach (Material material in renderer.sharedMaterials) DescribeMaterial(report, material);
                }
            }
            bool applied = File.Exists(ReviewDirectory + "/application.txt");
            File.WriteAllText(ReviewDirectory + (applied ? "/inspection_after.txt" : "/inspection.txt"), report.ToString(), Encoding.UTF8);
            if (!File.Exists(ReviewDirectory + "/before_Dagger_Idle_side.png"))
                CaptureTargets("before", targets);
            if (applied)
            {
                CaptureSourceReference(targets[0]);
                ComposeSideComparison("interim", "interim_comparison.png");
            }
            Debug.Log("Dagger material inspection and untouched-scene before images saved. " + report);
        }

        internal static void CaptureReview()
        {
            if (File.Exists(ReviewDirectory + "/final.png"))
                throw new InvalidOperationException("The final capture already exists; it will not be overwritten.");
            Transform[] targets = FindTargets(RequireScene());
            string pose = DescribePoses(targets);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialDirectory + "/Material.001.mat");
            if (material == null || targets.Any(t => FindItem(t).GetComponentsInChildren<Renderer>(true)
                .Any(r => r.sharedMaterials.Any(m => m != material))))
                throw new InvalidOperationException("A dagger material slot is not using the restored source material.");
            foreach (string slot in new[] { "_BaseMap", "_BumpMap", "_MetallicGlossMap" })
                if (material.GetTexture(slot) == null || !AssetDatabase.GetAssetPath(material.GetTexture(slot)).StartsWith(TextureDirectory + "/", StringComparison.Ordinal))
                    throw new InvalidOperationException("The restored dagger texture is missing or uses another item: " + slot);
            string hash = HashBytes(File.ReadAllBytes("item model/dagger.fbx"));
            if (hash != HashBytes(File.ReadAllBytes(ModelPath)))
                throw new InvalidOperationException("The imported FBX differs from its original.");
            Type logEntries = typeof(EditorWindow).Assembly.GetType("UnityEditor.LogEntries");
            MethodInfo countsMethod = logEntries?.GetMethod("GetCountsByType", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (countsMethod == null) throw new InvalidOperationException("Editor Console counts could not be read.");
            object[] counts = { 0, 0, 0 };
            countsMethod.Invoke(null, counts);
            if ((int)counts[0] != 0) throw new InvalidOperationException("Unity Console contains errors: " + counts[0]);
            CaptureTargets("after", targets);
            ComposeSideComparison("after", "final.png");
            if (pose != DescribePoses(targets)) throw new InvalidOperationException("Review changed a target pose.");
            File.WriteAllText(ReviewDirectory + "/verification.txt",
                "All five original material references: true\nAll texture references point to Dagger/Textures: true\n" +
                "Original/imported FBX SHA256: " + hash + "\nTarget transforms unchanged by capture: true\n" +
                "Unity Console errors=" + counts[0] + " warnings=" + counts[1] + "\n" +
                "Final comparison left=before right=after; rows=" + string.Join(",", TargetNames), Encoding.UTF8);
            Debug.Log("Dagger material review captured without changing target transforms, meshes, renderers, animation, or scene lights.");
        }

        private static void ComposeSideComparison(string phase, string filename)
        {
            Texture2D sheet = new Texture2D(1440, 720 * TargetNames.Length, TextureFormat.RGB24, false);
            try
            {
                for (int row = 0; row < TargetNames.Length; row++)
                    for (int column = 0; column < 2; column++)
                    {
                        string source = ReviewDirectory + "/" + (column == 0 ? "before" : phase) + "_" + TargetNames[row] + "_side.png";
                        Texture2D frame = new Texture2D(2, 2, TextureFormat.RGB24, false);
                        try
                        {
                            if (!frame.LoadImage(File.ReadAllBytes(source))) throw new InvalidDataException(source);
                            sheet.SetPixels(column * 720, (TargetNames.Length - row - 1) * 720, 720, 720, frame.GetPixels());
                        }
                        finally { UnityEngine.Object.DestroyImmediate(frame); }
                    }
                sheet.Apply();
                File.WriteAllBytes(ReviewDirectory + "/" + filename, sheet.EncodeToPNG());
            }
            finally { UnityEngine.Object.DestroyImmediate(sheet); }
        }

        private static void CaptureSourceReference(Transform target)
        {
            Transform item = FindItem(target);
            Renderer[] live = item.GetComponentsInChildren<Renderer>(true);
            Bounds bounds = live[0].bounds;
            foreach (Renderer renderer in live.Skip(1)) bounds.Encapsulate(renderer.bounds);
            GameObject original = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            CaptureSceneView(target, bounds.center, target.right, 0.36f,
                ReviewDirectory + "/source_reference.png", camera =>
                {
                    // Draw the byte-identical source mesh in the same pose and scene lighting;
                    // the actual target and its renderer are never changed or hidden.
                    camera.cullingMask = 1 << 31;
                    foreach (MeshRenderer renderer in original.GetComponentsInChildren<MeshRenderer>(true))
                    {
                        Mesh mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
                        Matrix4x4 matrix = item.localToWorldMatrix * original.transform.worldToLocalMatrix * renderer.localToWorldMatrix;
                        for (int sub = 0; sub < mesh.subMeshCount; sub++)
                            Graphics.DrawMesh(mesh, matrix, renderer.sharedMaterials[sub], 31, camera, sub);
                    }
                });
        }

        internal static void Apply()
        {
            Scene scene = RequireScene();
            if (scene.isDirty)
                throw new InvalidOperationException("Save the user's scene edits first; this operation will not save unrelated pending edits.");
            Transform[] targets = FindTargets(scene);
            string poseBefore = DescribePoses(targets);
            string sourceHash = HashBytes(File.ReadAllBytes("item model/dagger.fbx"));
            if (sourceHash != HashBytes(File.ReadAllBytes(ModelPath)))
                throw new InvalidOperationException("The imported model is not identical to the specified original FBX.");
            List<FbxNode> roots = ReadFbx();
            FbxNode objects = roots.Single(n => n.Name == "Objects");
            FbxNode connections = roots.Single(n => n.Name == "Connections");
            FbxNode authoredMaterial = objects.Children.Single(n => n.Name == "Material");
            string materialName = ((string)authoredMaterial.Values[1]).Split('\0')[0];
            long materialId = Convert.ToInt64(authoredMaterial.Values[0]);
            Dictionary<string, FbxNode> bindings = new Dictionary<string, FbxNode>();
            foreach (FbxNode connection in connections.Children.Where(n => n.Values.Count >= 4 &&
                Convert.ToString(n.Values[0]) == "OP" && Convert.ToInt64(n.Values[2]) == materialId))
            {
                long textureId = Convert.ToInt64(connection.Values[1]);
                FbxNode texture = objects.Children.Single(n => n.Name == "Texture" && Convert.ToInt64(n.Values[0]) == textureId);
                FbxNode videoConnection = connections.Children.First(n => n.Values.Count >= 3 &&
                    Convert.ToString(n.Values[0]) == "OO" && Convert.ToInt64(n.Values[2]) == textureId);
                long videoId = Convert.ToInt64(videoConnection.Values[1]);
                bindings.Add(Convert.ToString(connection.Values[3]), objects.Children.Single(n =>
                    n.Name == "Video" && Convert.ToInt64(n.Values[0]) == videoId));
            }
            foreach (string slot in new[] { "DiffuseColor", "NormalMap", "ShininessExponent", "ReflectionFactor" })
                if (!bindings.ContainsKey(slot)) throw new InvalidDataException("The authored texture binding is absent: " + slot);

            CreateFolder(TextureDirectory);
            CreateFolder(MaterialDirectory);
            Dictionary<string, string> paths = new Dictionary<string, string>();
            StringBuilder report = new StringBuilder();
            foreach (KeyValuePair<string, FbxNode> binding in bindings)
            {
                string filename = Path.GetFileName(Convert.ToString(binding.Value.Children.Single(n => n.Name == "RelativeFilename").Values[0]));
                byte[] bytes = (byte[])binding.Value.Children.Single(n => n.Name == "Content").Values[0];
                string path = TextureDirectory + "/" + filename;
                if (File.Exists(path) && !File.ReadAllBytes(path).SequenceEqual(bytes))
                    throw new InvalidOperationException("Existing texture differs from FBX; it was not overwritten: " + path);
                if (!File.Exists(path)) File.WriteAllBytes(path, bytes);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = binding.Key == "NormalMap" ? TextureImporterType.NormalMap : TextureImporterType.Default;
                importer.sRGBTexture = binding.Key == "DiffuseColor";
                importer.maxTextureSize = 8192;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
                paths.Add(binding.Key, path);
                report.AppendLine(binding.Key + " -> " + path + " embedded SHA256=" + HashBytes(bytes));
                if (!File.ReadAllBytes(path).SequenceEqual(bytes)) throw new InvalidDataException("Extracted bytes changed: " + path);
            }

            string packedPath = PackMetallicSmoothness(paths["ReflectionFactor"], paths["ShininessExponent"]);
            Material embedded = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<Material>().SingleOrDefault(m => m.name == materialName);
            string materialPath = MaterialDirectory + "/" + materialName + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                if (embedded == null) throw new InvalidOperationException("The original imported material was not found.");
                material = new Material(embedded) { name = materialName };
                AssetDatabase.CreateAsset(material, materialPath);
            }
            if (material.shader.name != "Universal Render Pipeline/Lit")
                throw new InvalidOperationException("Unexpected imported shader; no arbitrary shader replacement was performed.");
            Texture2D baseColor = AssetDatabase.LoadAssetAtPath<Texture2D>(paths["DiffuseColor"]);
            material.SetTexture("_BaseMap", baseColor);
            material.SetTexture("_MainTex", baseColor);
            material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(paths["NormalMap"]));
            material.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>(packedPath));
            // FBX-authored maps supply the values: Unity uses metallic R and 1 - roughness A.
            material.SetFloat("_Metallic", 1f);
            material.SetFloat("_Smoothness", 1f);
            material.SetFloat("_SmoothnessTextureChannel", 0f);
            material.EnableKeyword("_NORMALMAP");
            material.EnableKeyword("_METALLICSPECGLOSSMAP");
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssetIfDirty(material);
            ModelImporter modelImporter = (ModelImporter)AssetImporter.GetAtPath(ModelPath);
            modelImporter.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), materialName), material);
            modelImporter.materialLocation = ModelImporterMaterialLocation.External;
            modelImporter.materialName = ModelImporterMaterialName.BasedOnMaterialName;
            modelImporter.materialSearch = ModelImporterMaterialSearch.Local;
            modelImporter.SaveAndReimport();
            GameObject imported = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            Dictionary<string, Renderer> sourceRenderers = imported.GetComponentsInChildren<Renderer>(true)
                .ToDictionary(r => AnimationUtility.CalculateTransformPath(r.transform, imported.transform));
            foreach (Transform target in targets)
            {
                Transform item = FindItem(target);
                foreach (Renderer renderer in item.GetComponentsInChildren<Renderer>(true))
                {
                    string relativePath = AnimationUtility.CalculateTransformPath(renderer.transform, item);
                    Material[] importedMaterials = sourceRenderers[relativePath].sharedMaterials;
                    if (importedMaterials.Any(m => m != material)) throw new InvalidOperationException("Unexpected material slot mapping.");
                    Undo.RecordObject(renderer, "Restore dagger embedded material");
                    renderer.sharedMaterials = importedMaterials;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                    EditorUtility.SetDirty(renderer);
                }
            }
            if (poseBefore != DescribePoses(targets))
                throw new InvalidOperationException("A target transform changed during material application.");
            if (sourceHash != HashBytes(File.ReadAllBytes(ModelPath)) || sourceHash != HashBytes(File.ReadAllBytes("item model/dagger.fbx")))
                throw new InvalidOperationException("FBX content changed during material application.");
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            report.AppendLine("All target hierarchy transforms and parents unchanged: true");
            report.AppendLine("Source and imported FBX unchanged: " + sourceHash);
            report.AppendLine("All five renderers reference the original named material: " + materialPath);
            report.AppendLine("Persisted FBX remap preserves materials for future PrefabUtility.InstantiatePrefab calls.");
            DescribeMaterial(report, material);
            File.WriteAllText(ReviewDirectory + "/application.txt", report.ToString(), Encoding.UTF8);
            CaptureTargets("interim", targets);
            Debug.Log("Original dagger textures and material restored to all five existing items. " + report);
        }

        private static string PackMetallicSmoothness(string metallicPath, string roughnessPath)
        {
            Texture2D metallic = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            Texture2D roughness = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            Texture2D packed = null;
            try
            {
                if (!metallic.LoadImage(File.ReadAllBytes(metallicPath)) || !roughness.LoadImage(File.ReadAllBytes(roughnessPath)))
                    throw new InvalidDataException("Embedded PBR image decoding failed.");
                if (metallic.width != roughness.width || metallic.height != roughness.height)
                    throw new InvalidDataException("Embedded PBR dimensions differ; no resampling was performed.");
                Color32[] metal = metallic.GetPixels32();
                Color32[] rough = roughness.GetPixels32();
                for (int i = 0; i < metal.Length; i++)
                    metal[i] = new Color32(metal[i].r, metal[i].r, metal[i].r, (byte)(255 - rough[i].r));
                packed = new Texture2D(metallic.width, metallic.height, TextureFormat.RGBA32, false, true);
                packed.SetPixels32(metal);
                packed.Apply();
                string path = TextureDirectory + "/dagger_metallic_smoothness.png";
                File.WriteAllBytes(path, packed.EncodeToPNG());
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
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

        private static string DescribePoses(IEnumerable<Transform> targets)
        {
            return string.Join("\n", targets.SelectMany(t => t.GetComponentsInChildren<Transform>(true)).Select(t =>
                t.GetInstanceID() + "|" + (t.parent == null ? 0 : t.parent.GetInstanceID()) + "|" +
                t.localPosition.ToString("R") + "|" + t.localRotation.ToString("R") + "|" + t.localScale.ToString("R")));
        }

        private static string HashBytes(byte[] bytes)
        {
            using (SHA256 sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "");
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
            if (material == null) { report.AppendLine("NULL MATERIAL"); return; }
            report.AppendLine("  Material=" + material.name + " shader=" + material.shader.name + " asset=" + AssetDatabase.GetAssetPath(material));
            foreach (string property in material.GetTexturePropertyNames())
            {
                Texture texture = material.GetTexture(property);
                report.AppendLine("  " + property + "=" + (texture == null ? "NULL" : texture.name + " @ " + AssetDatabase.GetAssetPath(texture)));
            }
            for (int index = 0; index < ShaderUtil.GetPropertyCount(material.shader); index++)
            {
                string name = ShaderUtil.GetPropertyName(material.shader, index);
                ShaderUtil.ShaderPropertyType type = ShaderUtil.GetPropertyType(material.shader, index);
                if (type == ShaderUtil.ShaderPropertyType.Color)
                    report.AppendLine("  " + name + "=" + material.GetColor(name));
                else if (type == ShaderUtil.ShaderPropertyType.Float || type == ShaderUtil.ShaderPropertyType.Range)
                    report.AppendLine("  " + name + "=" + material.GetFloat(name));
            }
        }

        // Read authored FBX bindings and embedded bytes without involving Unity's name search.
        private sealed class FbxNode
        {
            internal string Name;
            internal readonly List<object> Values = new List<object>();
            internal readonly List<FbxNode> Children = new List<FbxNode>();
        }

        private static List<FbxNode> ReadFbx()
        {
            using (BinaryReader reader = new BinaryReader(File.OpenRead("item model/dagger.fbx")))
            {
                string header = Encoding.ASCII.GetString(reader.ReadBytes(23));
                if (!header.StartsWith("Kaydara FBX Binary"))
                    throw new InvalidDataException("The source is not a binary FBX.");
                bool wide = reader.ReadUInt32() >= 7500;
                List<FbxNode> roots = new List<FbxNode>();
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
            long propertyBytes = wide ? (long)reader.ReadUInt64() : reader.ReadUInt32();
            int nameLength = reader.ReadByte();
            if (end == 0) return null;
            FbxNode node = new FbxNode { Name = Encoding.UTF8.GetString(reader.ReadBytes(nameLength)) };
            for (long i = 0; i < count; i++)
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

        private static void DescribeFbxSource()
        {
            StringBuilder report = new StringBuilder();
            foreach (FbxNode root in ReadFbx())
            {
                if (root.Name == "Objects")
                    foreach (FbxNode node in root.Children.Where(n => n.Name == "Material" || n.Name == "Texture" || n.Name == "Video"))
                        DescribeNode(report, node, "");
                if (root.Name == "Connections") DescribeNode(report, root, "");
            }
            File.WriteAllText(ReviewDirectory + "/source_bindings.txt", report.ToString(), Encoding.UTF8);
        }

        private static void DescribeNode(StringBuilder report, FbxNode node, string indent)
        {
            report.AppendLine(indent + node.Name + " | " + string.Join(" | ", node.Values.Select(v =>
                v is byte[] bytes ? "[embedded bytes=" + bytes.Length + "]" : Convert.ToString(v).Replace('\0', '|'))));
            foreach (FbxNode child in node.Children) DescribeNode(report, child, indent + "  ");
        }

        private static Scene RequireScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Dagger material changes require Edit Mode; the current play session was not changed.");
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.isLoaded || scene.path != "Assets/_Project/Scenes/CargoRunMvp.unity")
                throw new InvalidOperationException("The existing CargoRunMvp scene must be active.");
            return scene;
        }

        private static Transform[] FindTargets(Scene scene)
        {
            Transform layout = scene.GetRootGameObjects().Single(g => g.name == "PlayerAnimationLayout").transform;
            Transform[] transforms = layout.GetComponentsInChildren<Transform>(true);
            return TargetNames.Select(name => transforms.Single(t => t.name == name)).ToArray();
        }

        private static Transform FindItem(Transform target)
        {
            return target.GetComponentsInChildren<Transform>(true).Single(t => t.name == "Dagger_RightHand");
        }

        private static void CaptureTargets(string phase, Transform[] targets)
        {
            Directory.CreateDirectory(ReviewDirectory);
            foreach (Transform target in targets)
            {
                Renderer[] renderers = FindItem(target).GetComponentsInChildren<Renderer>(true);
                Bounds bounds = renderers[0].bounds;
                foreach (Renderer renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
                CaptureSceneView(target, target.position + target.up * 1.05f, target.forward, 1.28f,
                    ReviewDirectory + "/" + phase + "_" + target.name + "_full.png");
                CaptureSceneView(target, bounds.center, target.forward, 0.36f,
                    ReviewDirectory + "/" + phase + "_" + target.name + "_front.png");
                CaptureSceneView(target, bounds.center, target.right, 0.36f,
                    ReviewDirectory + "/" + phase + "_" + target.name + "_side.png");
            }
        }

        private static void CaptureSceneView(Transform target, Vector3 center, Vector3 view, float size, string path, Action<Camera> beforeRender = null)
        {
            GameObject cameraObject = new GameObject("DaggerMaterialReviewCamera", typeof(Camera));
            cameraObject.hideFlags = HideFlags.HideAndDontSave;
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.enabled = false;
            camera.orthographic = true;
            camera.orthographicSize = size;
            camera.aspect = 1f;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 8f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.16f, 0.18f, 0.21f);
            camera.transform.position = center + view.normalized * 3f;
            camera.transform.LookAt(center, target.up);
            RenderTexture previous = RenderTexture.active;
            RenderTexture render = RenderTexture.GetTemporary(720, 720, 24, RenderTextureFormat.ARGB32);
            Texture2D image = new Texture2D(720, 720, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = render;
                beforeRender?.Invoke(camera);
                camera.Render();
                RenderTexture.active = render;
                image.ReadPixels(new Rect(0, 0, 720, 720), 0, 0);
                image.Apply();
                File.WriteAllBytes(path, image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(render);
                UnityEngine.Object.DestroyImmediate(image);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }
    }
}
