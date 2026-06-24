using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.Validation
{
    public static class ApprovedControlRoomCurrentStateCapture
    {
        private const string GeneratedSnapshotPath = "Assets/_Project/Editor/Validation/ApprovedControlRoomCurrentStateSnapshot.cs";

        [MenuItem("Bellerophon/Validation/Capture Approved Control Room Current State")]
        public static void CaptureCurrentEditorObjects()
        {
            var activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid())
            {
                throw new InvalidOperationException("No active scene is open for control room current state capture.");
            }

            var normalizedActivePath = activeScene.path.Replace('\\', '/');
            var normalizedCargoPath = Phase4CargoShipGrayboxBootstrap.CargoRunScenePath.Replace('\\', '/');
            if (!string.Equals(normalizedActivePath, normalizedCargoPath, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Current active scene is not CargoRunMvp. ActiveScene=" + activeScene.path);
            }

            var roots = FindApprovedControlRoomRoots();
            if (roots.Count == 0)
            {
                throw new InvalidOperationException("No Approved Control Room roots were found in the active scene.");
            }

            var projectRoot = Directory.GetParent(Application.dataPath);
            if (projectRoot == null)
            {
                throw new InvalidOperationException("Could not resolve project root for control room current state capture.");
            }

            var outputRoot = Path.Combine(projectRoot.FullName, "artSample", "control_room_current", "editor_current");
            Directory.CreateDirectory(outputRoot);

            var capturedAt = DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture);
            var rootSnapshots = new List<RootSnapshot>();
            for (var i = 0; i < roots.Count; i++)
            {
                rootSnapshots.Add(CaptureRoot(roots[i]));
            }

            var markdownPath = Path.Combine(outputRoot, "control_room_current_objects.md");
            File.WriteAllText(markdownPath, BuildMarkdown(capturedAt, activeScene.path, rootSnapshots), new UTF8Encoding(false));

            var generatedPath = Path.Combine(projectRoot.FullName, GeneratedSnapshotPath.Replace('/', Path.DirectorySeparatorChar));
            File.WriteAllText(generatedPath, BuildGeneratedSource(capturedAt, activeScene.path, rootSnapshots), new UTF8Encoding(false));

            Debug.Log(
                "Approved control room current state capture saved: " +
                markdownPath +
                "; GeneratedSnapshot=" +
                generatedPath +
                "; RootCount=" +
                rootSnapshots.Count.ToString(CultureInfo.InvariantCulture));
        }

        private static List<GameObject> FindApprovedControlRoomRoots()
        {
            var roots = new List<GameObject>();
            var transforms = UnityEngine.Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (var i = 0; i < transforms.Length; i++)
            {
                var transform = transforms[i];
                if (transform == null ||
                    transform.parent != null ||
                    !transform.name.StartsWith("Approved Control Room ", StringComparison.Ordinal))
                {
                    continue;
                }

                roots.Add(transform.gameObject);
            }

            roots.Sort((left, right) => string.CompareOrdinal(left.name, right.name));
            return roots;
        }

        private static RootSnapshot CaptureRoot(GameObject root)
        {
            var snapshots = new List<ObjectSnapshot>();
            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                var transform = transforms[i];
                if (transform == null)
                {
                    continue;
                }

                var renderer = transform.GetComponent<Renderer>();
                snapshots.Add(new ObjectSnapshot(
                    GetIndexPath(root.transform, transform),
                    GetRelativeNamePath(root.transform, transform),
                    transform.name,
                    transform.localPosition,
                    transform.localRotation,
                    transform.localScale,
                    transform.gameObject.activeSelf,
                    GetComponentSignature(transform.gameObject),
                    renderer != null,
                    renderer != null && renderer.enabled,
                    renderer != null ? GetMaterialPaths(renderer) : Array.Empty<string>(),
                    renderer != null ? GetMaterialNames(renderer) : Array.Empty<string>()));
            }

            snapshots.Sort((left, right) => CompareIndexPaths(left.IndexPath, right.IndexPath));
            return new RootSnapshot(root.name, snapshots);
        }

        private static string BuildMarkdown(string capturedAt, string scenePath, IReadOnlyList<RootSnapshot> roots)
        {
            var builder = new StringBuilder();
            builder.AppendLine("# Control Room Current Editor Objects");
            builder.AppendLine();
            builder.AppendLine("현재 열린 CargoRunMvp 씬에서 `Approved Control Room ...` 루트 전체를 재생성 없이 캡처한 최종 복구 기준입니다.");
            builder.AppendLine();
            builder.AppendLine("- 캡처 시각: " + capturedAt);
            builder.AppendLine("- 씬: `" + scenePath + "`");
            builder.AppendLine("- 생성된 복구 스크립트: `" + GeneratedSnapshotPath + "`");
            builder.AppendLine("- 캡처 루트 수: `" + roots.Count.ToString(CultureInfo.InvariantCulture) + "`");
            builder.AppendLine();

            for (var i = 0; i < roots.Count; i++)
            {
                var root = roots[i];
                builder.AppendLine("## " + root.Name);
                builder.AppendLine();
                builder.AppendLine("| IndexPath | NamePath | Active | LocalPosition | LocalRotation | LocalScale | Renderer | Materials |");
                builder.AppendLine("| --- | --- | --- | --- | --- | --- | --- | --- |");
                for (var j = 0; j < root.Objects.Count; j++)
                {
                    var obj = root.Objects[j];
                    builder.Append("| `").Append(obj.IndexPath).Append("` ");
                    builder.Append("| `").Append(obj.NamePath).Append("` ");
                    builder.Append("| `").Append(obj.ActiveSelf ? "true" : "false").Append("` ");
                    builder.Append("| `").Append(FormatVector(obj.LocalPosition)).Append("` ");
                    builder.Append("| `").Append(FormatQuaternion(obj.LocalRotation)).Append("` ");
                    builder.Append("| `").Append(FormatVector(obj.LocalScale)).Append("` ");
                    builder.Append("| `").Append(obj.HasRenderer ? (obj.RendererEnabled ? "enabled" : "disabled") : "none").Append("` ");
                    builder.Append("| `").Append(string.Join(", ", obj.MaterialNames)).AppendLine("` |");
                }

                builder.AppendLine();
            }

            return builder.ToString();
        }

        private static string BuildGeneratedSource(string capturedAt, string scenePath, IReadOnlyList<RootSnapshot> roots)
        {
            var builder = new StringBuilder();
            builder.AppendLine("// <auto-generated>");
            builder.AppendLine("// Captured control room current editor state. Do not edit by hand unless explicitly requested.");
            builder.AppendLine("// </auto-generated>");
            builder.AppendLine("using System;");
            builder.AppendLine("using System.Collections.Generic;");
            builder.AppendLine("using System.Globalization;");
            builder.AppendLine("using System.Text;");
            builder.AppendLine("using UnityEditor;");
            builder.AppendLine("using UnityEngine;");
            builder.AppendLine("using UnityEngine.SceneManagement;");
            builder.AppendLine();
            builder.AppendLine("namespace Bellerophon.Editor.Validation");
            builder.AppendLine("{");
            builder.AppendLine("    public static class ApprovedControlRoomCurrentStateSnapshot");
            builder.AppendLine("    {");
            builder.AppendLine("        public const string CapturedAt = " + Quote(capturedAt) + ";");
            builder.AppendLine("        public const string CapturedScenePath = " + Quote(scenePath) + ";");
            builder.AppendLine();
            builder.AppendLine("        private static readonly CapturedRoot[] Roots =");
            builder.AppendLine("        {");
            for (var i = 0; i < roots.Count; i++)
            {
                var root = roots[i];
                builder.AppendLine("            new CapturedRoot(");
                builder.AppendLine("                " + Quote(root.Name) + ",");
                builder.AppendLine("                new CapturedObject[]");
                builder.AppendLine("                {");
                for (var j = 0; j < root.Objects.Count; j++)
                {
                    var obj = root.Objects[j];
                    builder.AppendLine("                    new CapturedObject(");
                    builder.AppendLine("                        " + Quote(obj.IndexPath) + ",");
                    builder.AppendLine("                        " + Quote(obj.NamePath) + ",");
                    builder.AppendLine("                        " + Quote(obj.Name) + ",");
                    builder.AppendLine("                        " + FormatSourceVector(obj.LocalPosition) + ",");
                    builder.AppendLine("                        " + FormatSourceQuaternion(obj.LocalRotation) + ",");
                    builder.AppendLine("                        " + FormatSourceVector(obj.LocalScale) + ",");
                    builder.AppendLine("                        " + FormatBool(obj.ActiveSelf) + ",");
                    builder.AppendLine("                        " + Quote(obj.ComponentSignature) + ",");
                    builder.AppendLine("                        " + FormatBool(obj.HasRenderer) + ",");
                    builder.AppendLine("                        " + FormatBool(obj.RendererEnabled) + ",");
                    builder.AppendLine("                        " + FormatSourceStringArray(obj.MaterialPaths) + "),");
                }

                builder.AppendLine("                }),");
            }

            builder.AppendLine("        };");
            builder.AppendLine();
            builder.AppendLine("        [MenuItem(\"Bellerophon/Bootstrap/Restore Approved Control Room Current State Snapshot\")]");
            builder.AppendLine("        public static void RestoreCapturedControlRoomCurrentState()");
            builder.AppendLine("        {");
            builder.AppendLine("            var activeScene = SceneManager.GetActiveScene();");
            builder.AppendLine("            if (!activeScene.IsValid())");
            builder.AppendLine("            {");
            builder.AppendLine("                throw new InvalidOperationException(\"No active scene is open for control room current state restore.\");");
            builder.AppendLine("            }");
            builder.AppendLine();
            builder.AppendLine("            var normalizedActivePath = activeScene.path.Replace('\\\\', '/');");
            builder.AppendLine("            var normalizedCargoPath = Phase4CargoShipGrayboxBootstrap.CargoRunScenePath.Replace('\\\\', '/');");
            builder.AppendLine("            if (!string.Equals(normalizedActivePath, normalizedCargoPath, StringComparison.OrdinalIgnoreCase))");
            builder.AppendLine("            {");
            builder.AppendLine("                throw new InvalidOperationException(\"Current active scene is not CargoRunMvp. ActiveScene=\" + activeScene.path);");
            builder.AppendLine("            }");
            builder.AppendLine();
            builder.AppendLine("            var applied = 0;");
            builder.AppendLine("            for (var i = 0; i < Roots.Length; i++)");
            builder.AppendLine("            {");
            builder.AppendLine("                var root = RequireRoot(Roots[i].Name);");
            builder.AppendLine("                for (var j = 0; j < Roots[i].Objects.Length; j++)");
            builder.AppendLine("                {");
            builder.AppendLine("                    ApplyObject(root.transform, Roots[i].Objects[j]);");
            builder.AppendLine("                    applied++;");
            builder.AppendLine("                }");
            builder.AppendLine("            }");
            builder.AppendLine();
            builder.AppendLine("            EditorUtility.SetDirty(SceneManager.GetActiveScene().GetRootGameObjects()[0]);");
            builder.AppendLine("            Debug.Log(\"Approved control room current state snapshot restored. CapturedAt=\" + CapturedAt + \"; Roots=\" + Roots.Length.ToString(CultureInfo.InvariantCulture) + \"; Objects=\" + applied.ToString(CultureInfo.InvariantCulture));");
            builder.AppendLine("        }");
            builder.AppendLine();
            builder.AppendLine("        private static void ApplyObject(Transform root, CapturedObject captured)");
            builder.AppendLine("        {");
            builder.AppendLine("            var transform = FindByIndexPath(root, captured.IndexPath);");
            builder.AppendLine("            if (transform == null)");
            builder.AppendLine("            {");
            builder.AppendLine("                throw new InvalidOperationException(\"Missing captured control room object: \" + root.name + \"/\" + captured.NamePath + \" [\" + captured.IndexPath + \"]\");");
            builder.AppendLine("            }");
            builder.AppendLine();
            builder.AppendLine("            if (!string.Equals(transform.name, captured.Name, StringComparison.Ordinal))");
            builder.AppendLine("            {");
            builder.AppendLine("                throw new InvalidOperationException(\"Captured control room object name mismatch: expected \" + captured.Name + \", actual \" + transform.name + \", indexPath=\" + captured.IndexPath);");
            builder.AppendLine("            }");
            builder.AppendLine();
            builder.AppendLine("            var componentSignature = GetComponentSignature(transform.gameObject);");
            builder.AppendLine("            if (!string.Equals(componentSignature, captured.ComponentSignature, StringComparison.Ordinal))");
            builder.AppendLine("            {");
            builder.AppendLine("                throw new InvalidOperationException(\"Captured control room object component mismatch: \" + root.name + \"/\" + captured.NamePath);");
            builder.AppendLine("            }");
            builder.AppendLine();
            builder.AppendLine("            transform.localPosition = captured.LocalPosition;");
            builder.AppendLine("            transform.localRotation = captured.LocalRotation;");
            builder.AppendLine("            transform.localScale = captured.LocalScale;");
            builder.AppendLine("            transform.gameObject.SetActive(captured.ActiveSelf);");
            builder.AppendLine();
            builder.AppendLine("            var renderer = transform.GetComponent<Renderer>();");
            builder.AppendLine("            if ((renderer != null) != captured.HasRenderer)");
            builder.AppendLine("            {");
            builder.AppendLine("                throw new InvalidOperationException(\"Captured control room object renderer mismatch: \" + root.name + \"/\" + captured.NamePath);");
            builder.AppendLine("            }");
            builder.AppendLine();
            builder.AppendLine("            if (renderer == null)");
            builder.AppendLine("            {");
            builder.AppendLine("                return;");
            builder.AppendLine("            }");
            builder.AppendLine();
            builder.AppendLine("            renderer.enabled = captured.RendererEnabled;");
            builder.AppendLine("            ApplyMaterialPaths(renderer, captured.MaterialPaths);");
            builder.AppendLine("        }");
            builder.AppendLine();
            builder.AppendLine("        private static void ApplyMaterialPaths(Renderer renderer, string[] materialPaths)");
            builder.AppendLine("        {");
            builder.AppendLine("            if (materialPaths == null || materialPaths.Length == 0)");
            builder.AppendLine("            {");
            builder.AppendLine("                return;");
            builder.AppendLine("            }");
            builder.AppendLine();
            builder.AppendLine("            var materials = renderer.sharedMaterials;");
            builder.AppendLine("            if (materials.Length != materialPaths.Length)");
            builder.AppendLine("            {");
            builder.AppendLine("                throw new InvalidOperationException(\"Captured material count mismatch: \" + renderer.name);");
            builder.AppendLine("            }");
            builder.AppendLine();
            builder.AppendLine("            var changed = false;");
            builder.AppendLine("            for (var i = 0; i < materialPaths.Length; i++)");
            builder.AppendLine("            {");
            builder.AppendLine("                if (string.IsNullOrWhiteSpace(materialPaths[i]))");
            builder.AppendLine("                {");
            builder.AppendLine("                    continue;");
            builder.AppendLine("                }");
            builder.AppendLine();
            builder.AppendLine("                var material = AssetDatabase.LoadAssetAtPath<Material>(materialPaths[i]);");
            builder.AppendLine("                if (material == null)");
            builder.AppendLine("                {");
            builder.AppendLine("                    throw new InvalidOperationException(\"Captured material asset was not found: \" + materialPaths[i]);");
            builder.AppendLine("                }");
            builder.AppendLine();
            builder.AppendLine("                materials[i] = material;");
            builder.AppendLine("                changed = true;");
            builder.AppendLine("            }");
            builder.AppendLine();
            builder.AppendLine("            if (changed)");
            builder.AppendLine("            {");
            builder.AppendLine("                renderer.sharedMaterials = materials;");
            builder.AppendLine("            }");
            builder.AppendLine("        }");
            builder.AppendLine();
            builder.AppendLine("        private static GameObject RequireRoot(string rootName)");
            builder.AppendLine("        {");
            builder.AppendLine("            var roots = SceneManager.GetActiveScene().GetRootGameObjects();");
            builder.AppendLine("            for (var i = 0; i < roots.Length; i++)");
            builder.AppendLine("            {");
            builder.AppendLine("                if (roots[i] != null && string.Equals(roots[i].name, rootName, StringComparison.Ordinal))");
            builder.AppendLine("                {");
            builder.AppendLine("                    return roots[i];");
            builder.AppendLine("                }");
            builder.AppendLine("            }");
            builder.AppendLine();
            builder.AppendLine("            throw new InvalidOperationException(\"Missing captured control room root: \" + rootName);");
            builder.AppendLine("        }");
            builder.AppendLine();
            builder.AppendLine("        private static Transform FindByIndexPath(Transform root, string indexPath)");
            builder.AppendLine("        {");
            builder.AppendLine("            if (string.Equals(indexPath, \".\", StringComparison.Ordinal))");
            builder.AppendLine("            {");
            builder.AppendLine("                return root;");
            builder.AppendLine("            }");
            builder.AppendLine();
            builder.AppendLine("            var current = root;");
            builder.AppendLine("            var parts = indexPath.Split('/');");
            builder.AppendLine("            for (var i = 0; i < parts.Length; i++)");
            builder.AppendLine("            {");
            builder.AppendLine("                if (!int.TryParse(parts[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out var index) || index < 0 || index >= current.childCount)");
            builder.AppendLine("                {");
            builder.AppendLine("                    return null;");
            builder.AppendLine("                }");
            builder.AppendLine();
            builder.AppendLine("                current = current.GetChild(index);");
            builder.AppendLine("            }");
            builder.AppendLine();
            builder.AppendLine("            return current;");
            builder.AppendLine("        }");
            builder.AppendLine();
            builder.AppendLine("        private static string GetComponentSignature(GameObject obj)");
            builder.AppendLine("        {");
            builder.AppendLine("            var components = obj.GetComponents<Component>();");
            builder.AppendLine("            var builder = new StringBuilder();");
            builder.AppendLine("            for (var i = 0; i < components.Length; i++)");
            builder.AppendLine("            {");
            builder.AppendLine("                if (i > 0)");
            builder.AppendLine("                {");
            builder.AppendLine("                    builder.Append(\"|\");");
            builder.AppendLine("                }");
            builder.AppendLine();
            builder.AppendLine("                builder.Append(components[i] == null ? \"<missing>\" : components[i].GetType().FullName);");
            builder.AppendLine("            }");
            builder.AppendLine();
            builder.AppendLine("            return builder.ToString();");
            builder.AppendLine("        }");
            builder.AppendLine();
            builder.AppendLine("        private readonly struct CapturedRoot");
            builder.AppendLine("        {");
            builder.AppendLine("            public CapturedRoot(string name, CapturedObject[] objects)");
            builder.AppendLine("            {");
            builder.AppendLine("                Name = name;");
            builder.AppendLine("                Objects = objects;");
            builder.AppendLine("            }");
            builder.AppendLine();
            builder.AppendLine("            public string Name { get; }");
            builder.AppendLine("            public CapturedObject[] Objects { get; }");
            builder.AppendLine("        }");
            builder.AppendLine();
            builder.AppendLine("        private readonly struct CapturedObject");
            builder.AppendLine("        {");
            builder.AppendLine("            public CapturedObject(string indexPath, string namePath, string name, Vector3 localPosition, Quaternion localRotation, Vector3 localScale, bool activeSelf, string componentSignature, bool hasRenderer, bool rendererEnabled, string[] materialPaths)");
            builder.AppendLine("            {");
            builder.AppendLine("                IndexPath = indexPath;");
            builder.AppendLine("                NamePath = namePath;");
            builder.AppendLine("                Name = name;");
            builder.AppendLine("                LocalPosition = localPosition;");
            builder.AppendLine("                LocalRotation = localRotation;");
            builder.AppendLine("                LocalScale = localScale;");
            builder.AppendLine("                ActiveSelf = activeSelf;");
            builder.AppendLine("                ComponentSignature = componentSignature;");
            builder.AppendLine("                HasRenderer = hasRenderer;");
            builder.AppendLine("                RendererEnabled = rendererEnabled;");
            builder.AppendLine("                MaterialPaths = materialPaths;");
            builder.AppendLine("            }");
            builder.AppendLine();
            builder.AppendLine("            public string IndexPath { get; }");
            builder.AppendLine("            public string NamePath { get; }");
            builder.AppendLine("            public string Name { get; }");
            builder.AppendLine("            public Vector3 LocalPosition { get; }");
            builder.AppendLine("            public Quaternion LocalRotation { get; }");
            builder.AppendLine("            public Vector3 LocalScale { get; }");
            builder.AppendLine("            public bool ActiveSelf { get; }");
            builder.AppendLine("            public string ComponentSignature { get; }");
            builder.AppendLine("            public bool HasRenderer { get; }");
            builder.AppendLine("            public bool RendererEnabled { get; }");
            builder.AppendLine("            public string[] MaterialPaths { get; }");
            builder.AppendLine("        }");
            builder.AppendLine("    }");
            builder.AppendLine("}");
            return builder.ToString();
        }

        private static string[] GetMaterialPaths(Renderer renderer)
        {
            var materials = renderer.sharedMaterials;
            var paths = new string[materials.Length];
            for (var i = 0; i < materials.Length; i++)
            {
                paths[i] = materials[i] == null ? string.Empty : AssetDatabase.GetAssetPath(materials[i]);
            }

            return paths;
        }

        private static string[] GetMaterialNames(Renderer renderer)
        {
            var materials = renderer.sharedMaterials;
            var names = new string[materials.Length];
            for (var i = 0; i < materials.Length; i++)
            {
                names[i] = materials[i] == null ? "<null>" : materials[i].name;
            }

            return names;
        }

        private static string GetComponentSignature(GameObject obj)
        {
            var components = obj.GetComponents<Component>();
            var builder = new StringBuilder();
            for (var i = 0; i < components.Length; i++)
            {
                if (i > 0)
                {
                    builder.Append("|");
                }

                builder.Append(components[i] == null ? "<missing>" : components[i].GetType().FullName);
            }

            return builder.ToString();
        }

        private static string GetIndexPath(Transform root, Transform transform)
        {
            if (transform == root)
            {
                return ".";
            }

            var indices = new List<int>();
            var current = transform;
            while (current != null && current != root)
            {
                var parent = current.parent;
                if (parent == null)
                {
                    break;
                }

                indices.Add(current.GetSiblingIndex());
                current = parent;
            }

            indices.Reverse();
            return string.Join("/", indices);
        }

        private static string GetRelativeNamePath(Transform root, Transform transform)
        {
            if (transform == root)
            {
                return ".";
            }

            var segments = new List<string>();
            var current = transform;
            while (current != null && current != root)
            {
                segments.Add(current.name);
                current = current.parent;
            }

            segments.Reverse();
            return string.Join("/", segments);
        }

        private static int CompareIndexPaths(string left, string right)
        {
            if (string.Equals(left, right, StringComparison.Ordinal))
            {
                return 0;
            }

            if (left == ".")
            {
                return -1;
            }

            if (right == ".")
            {
                return 1;
            }

            var leftParts = left.Split('/');
            var rightParts = right.Split('/');
            var count = Math.Min(leftParts.Length, rightParts.Length);
            for (var i = 0; i < count; i++)
            {
                var leftIndex = int.Parse(leftParts[i], CultureInfo.InvariantCulture);
                var rightIndex = int.Parse(rightParts[i], CultureInfo.InvariantCulture);
                var compare = leftIndex.CompareTo(rightIndex);
                if (compare != 0)
                {
                    return compare;
                }
            }

            return leftParts.Length.CompareTo(rightParts.Length);
        }

        private static string FormatSourceStringArray(IReadOnlyList<string> values)
        {
            if (values == null || values.Count == 0)
            {
                return "Array.Empty<string>()";
            }

            var builder = new StringBuilder();
            builder.Append("new[] { ");
            for (var i = 0; i < values.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(Quote(values[i]));
            }

            builder.Append(" }");
            return builder.ToString();
        }

        private static string FormatSourceVector(Vector3 value)
        {
            return "new Vector3(" +
                   FormatFloat(value.x) +
                   ", " +
                   FormatFloat(value.y) +
                   ", " +
                   FormatFloat(value.z) +
                   ")";
        }

        private static string FormatSourceQuaternion(Quaternion value)
        {
            return "new Quaternion(" +
                   FormatFloat(value.x) +
                   ", " +
                   FormatFloat(value.y) +
                   ", " +
                   FormatFloat(value.z) +
                   ", " +
                   FormatFloat(value.w) +
                   ")";
        }

        private static string FormatVector(Vector3 value)
        {
            return value.x.ToString("0.######", CultureInfo.InvariantCulture) +
                   "," +
                   value.y.ToString("0.######", CultureInfo.InvariantCulture) +
                   "," +
                   value.z.ToString("0.######", CultureInfo.InvariantCulture);
        }

        private static string FormatQuaternion(Quaternion value)
        {
            return value.x.ToString("0.######", CultureInfo.InvariantCulture) +
                   "," +
                   value.y.ToString("0.######", CultureInfo.InvariantCulture) +
                   "," +
                   value.z.ToString("0.######", CultureInfo.InvariantCulture) +
                   "," +
                   value.w.ToString("0.######", CultureInfo.InvariantCulture);
        }

        private static string FormatFloat(float value)
        {
            if (Mathf.Abs(value) < 0.0000005f)
            {
                value = 0f;
            }

            return value.ToString("0.######", CultureInfo.InvariantCulture) + "f";
        }

        private static string FormatBool(bool value)
        {
            return value ? "true" : "false";
        }

        private static string Quote(string value)
        {
            if (value == null)
            {
                return "null";
            }

            var builder = new StringBuilder();
            builder.Append('"');
            for (var i = 0; i < value.Length; i++)
            {
                var c = value[i];
                switch (c)
                {
                    case '\\':
                        builder.Append("\\\\");
                        break;
                    case '"':
                        builder.Append("\\\"");
                        break;
                    case '\r':
                        builder.Append("\\r");
                        break;
                    case '\n':
                        builder.Append("\\n");
                        break;
                    case '\t':
                        builder.Append("\\t");
                        break;
                    default:
                        if (c < 32 || c > 126)
                        {
                            builder.Append("\\u");
                            builder.Append(((int)c).ToString("X4", CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            builder.Append(c);
                        }

                        break;
                }
            }

            builder.Append('"');
            return builder.ToString();
        }

        private readonly struct RootSnapshot
        {
            public RootSnapshot(string name, IReadOnlyList<ObjectSnapshot> objects)
            {
                Name = name;
                Objects = objects;
            }

            public string Name { get; }
            public IReadOnlyList<ObjectSnapshot> Objects { get; }
        }

        private readonly struct ObjectSnapshot
        {
            public ObjectSnapshot(
                string indexPath,
                string namePath,
                string name,
                Vector3 localPosition,
                Quaternion localRotation,
                Vector3 localScale,
                bool activeSelf,
                string componentSignature,
                bool hasRenderer,
                bool rendererEnabled,
                IReadOnlyList<string> materialPaths,
                IReadOnlyList<string> materialNames)
            {
                IndexPath = indexPath;
                NamePath = namePath;
                Name = name;
                LocalPosition = localPosition;
                LocalRotation = localRotation;
                LocalScale = localScale;
                ActiveSelf = activeSelf;
                ComponentSignature = componentSignature;
                HasRenderer = hasRenderer;
                RendererEnabled = rendererEnabled;
                MaterialPaths = materialPaths;
                MaterialNames = materialNames;
            }

            public string IndexPath { get; }
            public string NamePath { get; }
            public string Name { get; }
            public Vector3 LocalPosition { get; }
            public Quaternion LocalRotation { get; }
            public Vector3 LocalScale { get; }
            public bool ActiveSelf { get; }
            public string ComponentSignature { get; }
            public bool HasRenderer { get; }
            public bool RendererEnabled { get; }
            public IReadOnlyList<string> MaterialPaths { get; }
            public IReadOnlyList<string> MaterialNames { get; }
        }
    }
}
