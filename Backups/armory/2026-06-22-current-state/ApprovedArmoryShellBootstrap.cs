using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.Validation
{
    public static class ApprovedArmoryShellBootstrap
    {
        public const string RootName = "Approved Armory 01 Shell";

        private const string UnityAssetDirectory = "Assets/_Project/Art/Ship/Armory";
        private const string UnityFbxPath = UnityAssetDirectory + "/armory_shell.fbx";
        private const string SampleFbxRelativePath = "artSample/armory_shell/exports/armory_shell.fbx";
        private const string ApprovalStatusRelativePath = "artSample/armory_shell/APPROVAL_STATUS.json";
        private const string CurrentStateUnityPath = "Assets/_Project/Editor/Validation/ApprovedArmoryShellCurrentState.cs";

        private static readonly Vector3 ArmoryCenterBelowControlRoom = new Vector3(13.20795f, -4.6f, 19.265f);

        [MenuItem("Bellerophon/Bootstrap/Ensure Approved Armory 01 Shell")]
        public static void EnsureApprovedArmoryShell()
        {
            var scene = EditorSceneManager.OpenScene(Phase4CargoShipGrayboxBootstrap.CargoRunScenePath, OpenSceneMode.Single);
            RequireApprovedSample();

            var protectedRoots = FindSceneRootObjectsExcept(RootName);
            var protectedSnapshots = CaptureProtectedSnapshots(protectedRoots);

            DeleteGeneratedObject(RootName);
            EnsureImportedSampleAsset();

            var samplePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(UnityFbxPath);
            if (samplePrefab == null)
            {
                throw new InvalidOperationException("Approved armory shell FBX was not imported as a prefab asset: " + UnityFbxPath);
            }

            var root = new GameObject(RootName);
            root.transform.SetPositionAndRotation(ArmoryCenterBelowControlRoom, Quaternion.identity);
            root.transform.localScale = Vector3.one;

            var instanceObject = PrefabUtility.InstantiatePrefab(samplePrefab, scene) as GameObject;
            if (instanceObject == null)
            {
                instanceObject = UnityEngine.Object.Instantiate(samplePrefab);
            }

            instanceObject.name = "AR-01 approved armory shell sample model";
            instanceObject.transform.SetParent(root.transform, false);
            instanceObject.transform.localPosition = Vector3.zero;
            instanceObject.transform.localRotation = Quaternion.identity;
            instanceObject.transform.localScale = Vector3.one;

            DisableAllColliders(root.transform);

            var armoryBounds = GetRendererBounds(root.transform);
            EnsureNoOverlap(armoryBounds, protectedRoots);
            EnsureProtectedObjectsUntouched(protectedSnapshots);

            Selection.activeGameObject = root;
            EditorGUIUtility.PingObject(root);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, Phase4CargoShipGrayboxBootstrap.CargoRunScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "Approved armory 01 shell applied. Root=" +
                RootName +
                "; Center=" +
                FormatVector(ArmoryCenterBelowControlRoom) +
                "; Bounds=" +
                FormatBounds(armoryBounds) +
                "; ExistingObjectsUntouched=True");
        }

        [MenuItem("Bellerophon/Bootstrap/Move Approved Armory 01 Shell To Z Below Control Room")]
        public static void MoveApprovedArmoryShellToZBelowControlRoom()
        {
            var scene = EditorSceneManager.OpenScene(Phase4CargoShipGrayboxBootstrap.CargoRunScenePath, OpenSceneMode.Single);
            var root = RequireObject(RootName);
            var protectedRoots = FindSceneRootObjectsExcept(RootName);
            var protectedSnapshots = CaptureProtectedSnapshots(protectedRoots);
            var armorySnapshots = CaptureArmorySnapshots(root.transform);
            var originalPosition = root.transform.position;

            try
            {
                var targetPosition = FindFirstNonOverlappingZBelowPosition(root.transform, protectedRoots);
                root.transform.position = targetPosition;

                EnsureOnlyArmoryRootPositionChanged(root.transform, armorySnapshots, targetPosition);
                var armoryBounds = GetRendererBounds(root.transform);
                EnsureNoOverlap(armoryBounds, protectedRoots);
                EnsureProtectedObjectsUntouched(protectedSnapshots);

                EditorUtility.SetDirty(root);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, Phase4CargoShipGrayboxBootstrap.CargoRunScenePath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log(
                    "Approved armory 01 shell moved on Z axis only. Root=" +
                    RootName +
                    "; From=" +
                    FormatVector(originalPosition) +
                    "; To=" +
                    FormatVector(targetPosition) +
                    "; RootRotationUnchanged=True; RootScaleUnchanged=True; ChildrenUntouched=True; ExistingObjectsUntouched=True");
            }
            catch
            {
                root.transform.position = originalPosition;
                throw;
            }
        }

        [MenuItem("Bellerophon/Bootstrap/Update Approved Armory AR-03 Only")]
        public static void UpdateApprovedArmoryAr03Only()
        {
            var scene = EditorSceneManager.OpenScene(Phase4CargoShipGrayboxBootstrap.CargoRunScenePath, OpenSceneMode.Single);
            var root = RequireObject(RootName);
            var protectedRoots = FindSceneRootObjectsExcept(RootName);
            var protectedSnapshots = CaptureProtectedSnapshots(protectedRoots);
            var nonAr03ArmorySnapshots = CaptureNonAr03ArmorySnapshots(root.transform);

            var stairMaterial = FindFirstSharedMaterial(root.transform, "AR-03 placeholder rear stair tread");
            var railMaterial = FindFirstSharedMaterial(root.transform, "AR-03 placeholder stair side rail");

            DeleteExistingAr03Objects(root.transform);
            CreateAr03FromApprovedSample(root.transform, stairMaterial, railMaterial);

            EnsureProtectedObjectsUntouched(protectedSnapshots);
            EnsureProtectedObjectsUntouched(nonAr03ArmorySnapshots);

            EditorUtility.SetDirty(root);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, Phase4CargoShipGrayboxBootstrap.CargoRunScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "Approved armory AR-03 stairs updated only. Root=" +
                RootName +
                "; Treads=12; Rails=2; ExistingObjectsUntouched=True; NonAr03ArmoryObjectsUntouched=True");
        }

        [MenuItem("Bellerophon/Bootstrap/Capture Approved Armory 01 Current State")]
        public static void CaptureCurrentEditorObjects()
        {
            var scene = RequireCargoRunMvpActiveScene();
            var root = RequireObject(RootName);
            var states = CaptureCurrentTransformStates(root.transform);

            WriteCurrentStateScript(states);

            EditorUtility.SetDirty(root);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, Phase4CargoShipGrayboxBootstrap.CargoRunScenePath);
            AssetDatabase.SaveAssets();

            Debug.Log(
                "Approved armory 01 current state captured. Root=" +
                RootName +
                "; TransformCount=" +
                states.Count.ToString(CultureInfo.InvariantCulture) +
                "; Output=" +
                CurrentStateUnityPath);
        }

        [MenuItem("Bellerophon/Bootstrap/Restore Approved Armory 01 Current State")]
        public static void RestoreApprovedArmoryShellCurrentState()
        {
            var scene = RequireCargoRunMvpActiveScene();
            var root = RequireObject(RootName);
            var protectedRoots = FindSceneRootObjectsExcept(RootName);
            var protectedSnapshots = CaptureProtectedSnapshots(protectedRoots);

            ApplyCapturedTransformStates(root.transform, ApprovedArmoryShellCurrentState.Transforms);
            EnsureExactCapturedHierarchy(root.transform, ApprovedArmoryShellCurrentState.Transforms);
            EnsureProtectedObjectsUntouched(protectedSnapshots);

            EditorUtility.SetDirty(root);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, Phase4CargoShipGrayboxBootstrap.CargoRunScenePath);
            AssetDatabase.SaveAssets();

            Debug.Log(
                "Approved armory 01 current state restored and saved. Root=" +
                RootName +
                "; TransformCount=" +
                ApprovedArmoryShellCurrentState.Transforms.Length.ToString(CultureInfo.InvariantCulture) +
                "; ExistingObjectsUntouched=True");
        }

        private static void RequireApprovedSample()
        {
            var approvalPath = Path.Combine(ProjectRoot, ApprovalStatusRelativePath);
            if (!File.Exists(approvalPath))
            {
                throw new InvalidOperationException("Missing approved armory sample status file: " + approvalPath);
            }

            var approval = File.ReadAllText(approvalPath);
            if (approval.IndexOf("\"approvalState\": \"승인\"", StringComparison.Ordinal) < 0 ||
                approval.IndexOf("\"unityApplicationAllowed\": true", StringComparison.Ordinal) < 0)
            {
                throw new InvalidOperationException("Armory shell sample has not been approved for Unity application: " + approvalPath);
            }

            var sampleFbxPath = Path.Combine(ProjectRoot, SampleFbxRelativePath);
            if (!File.Exists(sampleFbxPath))
            {
                throw new InvalidOperationException("Missing approved armory shell FBX sample: " + sampleFbxPath);
            }
        }

        private static void EnsureImportedSampleAsset()
        {
            var sourcePath = Path.Combine(ProjectRoot, SampleFbxRelativePath);
            var targetDirectory = Path.Combine(ProjectRoot, UnityAssetDirectory);
            var targetPath = Path.Combine(ProjectRoot, UnityFbxPath);

            Directory.CreateDirectory(targetDirectory);
            File.Copy(sourcePath, targetPath, true);
            AssetDatabase.ImportAsset(UnityFbxPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        }

        private static List<GameObject> FindSceneRootObjectsExcept(string excludedRootName)
        {
            var roots = new List<GameObject>();
            var sceneRoots = SceneManager.GetActiveScene().GetRootGameObjects();
            for (var i = 0; i < sceneRoots.Length; i++)
            {
                if (sceneRoots[i] == null || string.Equals(sceneRoots[i].name, excludedRootName, StringComparison.Ordinal))
                {
                    continue;
                }

                roots.Add(sceneRoots[i]);
            }

            return roots;
        }

        private static List<ProtectedTransformSnapshot> CaptureProtectedSnapshots(IEnumerable<GameObject> roots)
        {
            var snapshots = new List<ProtectedTransformSnapshot>();
            foreach (var root in roots)
            {
                if (root == null)
                {
                    continue;
                }

                var transforms = root.GetComponentsInChildren<Transform>(true);
                for (var i = 0; i < transforms.Length; i++)
                {
                    var transform = transforms[i];
                    if (transform == null)
                    {
                        continue;
                    }

                    snapshots.Add(new ProtectedTransformSnapshot(
                        root.name + "/" + GetRelativePath(root.transform, transform),
                        transform,
                        transform.localPosition,
                        transform.localRotation,
                        transform.localScale,
                        transform.gameObject.activeSelf));
                }
            }

            return snapshots;
        }

        private static List<ProtectedTransformSnapshot> CaptureArmorySnapshots(Transform root)
        {
            var snapshots = new List<ProtectedTransformSnapshot>();
            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                var transform = transforms[i];
                if (transform == null)
                {
                    continue;
                }

                snapshots.Add(new ProtectedTransformSnapshot(
                    GetRelativePath(root, transform),
                    transform,
                    transform.localPosition,
                    transform.localRotation,
                    transform.localScale,
                    transform.gameObject.activeSelf));
            }

            return snapshots;
        }

        private static List<CurrentTransformState> CaptureCurrentTransformStates(Transform root)
        {
            var states = new List<CurrentTransformState>();
            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                var transform = transforms[i];
                if (transform == null)
                {
                    continue;
                }

                states.Add(new CurrentTransformState(
                    transform.name,
                    GetSiblingPath(root, transform),
                    transform.gameObject.activeSelf,
                    transform.localPosition,
                    transform.localRotation,
                    transform.localScale));
            }

            return states;
        }

        private static void ApplyCapturedTransformStates(Transform root, IReadOnlyList<CurrentTransformState> states)
        {
            for (var i = 0; i < states.Count; i++)
            {
                var state = states[i];
                var transform = FindTransformBySiblingPath(root, state.SiblingPath);
                if (!string.Equals(transform.name, state.Name, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "Armory current state hierarchy mismatch at sibling path " +
                        FormatSiblingPath(state.SiblingPath) +
                        ". Expected=" +
                        state.Name +
                        "; Actual=" +
                        transform.name);
                }

                transform.localPosition = state.LocalPosition;
                transform.localRotation = state.LocalRotation;
                transform.localScale = state.LocalScale;
                transform.gameObject.SetActive(state.ActiveSelf);
            }
        }

        private static void EnsureExactCapturedHierarchy(Transform root, IReadOnlyList<CurrentTransformState> states)
        {
            var transforms = root.GetComponentsInChildren<Transform>(true);
            if (transforms.Length != states.Count)
            {
                throw new InvalidOperationException(
                    "Armory current state hierarchy count mismatch. Expected=" +
                    states.Count.ToString(CultureInfo.InvariantCulture) +
                    "; Actual=" +
                    transforms.Length.ToString(CultureInfo.InvariantCulture));
            }
        }

        private static Transform FindTransformBySiblingPath(Transform root, IReadOnlyList<int> siblingPath)
        {
            var current = root;
            for (var i = 0; i < siblingPath.Count; i++)
            {
                var childIndex = siblingPath[i];
                if (childIndex < 0 || childIndex >= current.childCount)
                {
                    throw new InvalidOperationException(
                        "Missing armory transform at sibling path " +
                        FormatSiblingPath(siblingPath));
                }

                current = current.GetChild(childIndex);
            }

            return current;
        }

        private static int[] GetSiblingPath(Transform root, Transform transform)
        {
            var reversed = new List<int>();
            var current = transform;
            while (current != null && current != root)
            {
                reversed.Add(current.GetSiblingIndex());
                current = current.parent;
            }

            reversed.Reverse();
            return reversed.ToArray();
        }

        private static void WriteCurrentStateScript(IReadOnlyList<CurrentTransformState> states)
        {
            var outputPath = Path.Combine(ProjectRoot, CurrentStateUnityPath);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            File.WriteAllText(outputPath, BuildCurrentStateScript(states), new UTF8Encoding(false));
        }

        private static string BuildCurrentStateScript(IReadOnlyList<CurrentTransformState> states)
        {
            var builder = new StringBuilder();
            builder.AppendLine("// <auto-generated>");
            builder.AppendLine("// Captured from the current Unity editor armory state.");
            builder.AppendLine("// </auto-generated>");
            builder.AppendLine("using UnityEngine;");
            builder.AppendLine();
            builder.AppendLine("namespace Bellerophon.Editor.Validation");
            builder.AppendLine("{");
            builder.AppendLine("    internal static class ApprovedArmoryShellCurrentState");
            builder.AppendLine("    {");
            builder.AppendLine("        public static readonly ApprovedArmoryShellBootstrap.CurrentTransformState[] Transforms =");
            builder.AppendLine("        {");
            for (var i = 0; i < states.Count; i++)
            {
                builder.Append("            ");
                AppendCurrentTransformState(builder, states[i]);
                builder.AppendLine(",");
            }

            builder.AppendLine("        };");
            builder.AppendLine("    }");
            builder.AppendLine("}");
            return builder.ToString();
        }

        private static void AppendCurrentTransformState(StringBuilder builder, CurrentTransformState state)
        {
            builder.Append("new ApprovedArmoryShellBootstrap.CurrentTransformState(");
            builder.Append(ToCSharpStringLiteral(state.Name));
            builder.Append(", ");
            AppendSiblingPath(builder, state.SiblingPath);
            builder.Append(", ");
            builder.Append(state.ActiveSelf ? "true" : "false");
            builder.Append(", ");
            AppendVector3(builder, state.LocalPosition);
            builder.Append(", ");
            AppendQuaternion(builder, state.LocalRotation);
            builder.Append(", ");
            AppendVector3(builder, state.LocalScale);
            builder.Append(")");
        }

        private static void AppendSiblingPath(StringBuilder builder, IReadOnlyList<int> siblingPath)
        {
            builder.Append("new int[] { ");
            for (var i = 0; i < siblingPath.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(siblingPath[i].ToString(CultureInfo.InvariantCulture));
            }

            builder.Append(" }");
        }

        private static void AppendVector3(StringBuilder builder, Vector3 value)
        {
            builder.Append("new Vector3(");
            builder.Append(FormatFloat(value.x));
            builder.Append(", ");
            builder.Append(FormatFloat(value.y));
            builder.Append(", ");
            builder.Append(FormatFloat(value.z));
            builder.Append(")");
        }

        private static void AppendQuaternion(StringBuilder builder, Quaternion value)
        {
            builder.Append("new Quaternion(");
            builder.Append(FormatFloat(value.x));
            builder.Append(", ");
            builder.Append(FormatFloat(value.y));
            builder.Append(", ");
            builder.Append(FormatFloat(value.z));
            builder.Append(", ");
            builder.Append(FormatFloat(value.w));
            builder.Append(")");
        }

        private static string FormatFloat(float value)
        {
            return value.ToString("R", CultureInfo.InvariantCulture) + "f";
        }

        private static string ToCSharpStringLiteral(string value)
        {
            return "\"" +
                   value
                       .Replace("\\", "\\\\")
                       .Replace("\"", "\\\"")
                       .Replace("\r", "\\r")
                       .Replace("\n", "\\n")
                       .Replace("\t", "\\t") +
                   "\"";
        }

        private static List<ProtectedTransformSnapshot> CaptureNonAr03ArmorySnapshots(Transform root)
        {
            var snapshots = new List<ProtectedTransformSnapshot>();
            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                var transform = transforms[i];
                if (transform == null || IsAr03Transform(root, transform))
                {
                    continue;
                }

                snapshots.Add(new ProtectedTransformSnapshot(
                    RootName + "/" + GetRelativePath(root, transform),
                    transform,
                    transform.localPosition,
                    transform.localRotation,
                    transform.localScale,
                    transform.gameObject.activeSelf));
            }

            return snapshots;
        }

        private static bool IsAr03Transform(Transform root, Transform transform)
        {
            var current = transform;
            while (current != null && current != root)
            {
                if (current.name.StartsWith("AR-03", StringComparison.Ordinal))
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        private static void DeleteExistingAr03Objects(Transform root)
        {
            var removals = new List<Transform>();
            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                var transform = transforms[i];
                if (transform == null || transform == root)
                {
                    continue;
                }

                if (transform.name.StartsWith("AR-03", StringComparison.Ordinal))
                {
                    removals.Add(transform);
                }
            }

            removals.Sort((left, right) => GetDepth(right).CompareTo(GetDepth(left)));
            for (var i = 0; i < removals.Count; i++)
            {
                if (removals[i] != null)
                {
                    UnityEngine.Object.DestroyImmediate(removals[i].gameObject);
                }
            }
        }

        private static int GetDepth(Transform transform)
        {
            var depth = 0;
            var current = transform;
            while (current != null)
            {
                depth++;
                current = current.parent;
            }

            return depth;
        }

        private static void CreateAr03FromApprovedSample(Transform root, Material stairMaterial, Material railMaterial)
        {
            var group = new GameObject("AR-03 stair assembly");
            group.transform.SetParent(root, false);
            group.transform.localPosition = Vector3.zero;
            group.transform.localRotation = Quaternion.identity;
            group.transform.localScale = Vector3.one;

            const int stepCount = 12;
            for (var i = 0; i < stepCount; i++)
            {
                var t = i / (float)(stepCount - 1);
                var blenderY = -3.25f + (t * 1.79f);
                var blenderZ = 0.22f + (t * 2.06f);
                var width = 1.42f - (t * 0.18f);
                CreateAr03Box(
                    "AR-03 placeholder rear stair tread " + (i + 1).ToString("00", CultureInfo.InvariantCulture),
                    group.transform,
                    BlenderToUnity(new Vector3(0f, blenderY, blenderZ)),
                    new Vector3(width, 0.16f, 0.28f),
                    stairMaterial);
            }

            CreateAr03CylinderBetween(
                "AR-03 placeholder stair side rail left",
                group.transform,
                BlenderToUnity(new Vector3(-0.86f, -3.32f, 0.46f)),
                BlenderToUnity(new Vector3(-0.86f, -1.40f, 2.46f)),
                0.030f,
                railMaterial);
            CreateAr03CylinderBetween(
                "AR-03 placeholder stair side rail right",
                group.transform,
                BlenderToUnity(new Vector3(0.86f, -3.32f, 0.46f)),
                BlenderToUnity(new Vector3(0.86f, -1.40f, 2.46f)),
                0.030f,
                railMaterial);
        }

        private static Vector3 BlenderToUnity(Vector3 blender)
        {
            return new Vector3(blender.x, blender.z, -blender.y);
        }

        private static void CreateAr03Box(string name, Transform parent, Vector3 localPosition, Vector3 localScale, Material material)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = localPosition;
            obj.transform.localRotation = Quaternion.identity;
            obj.transform.localScale = localScale;
            ApplyMaterial(obj, material);
            DisableCollider(obj);
        }

        private static void CreateAr03CylinderBetween(
            string name,
            Transform parent,
            Vector3 start,
            Vector3 end,
            float radius,
            Material material)
        {
            var direction = end - start;
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = (start + end) * 0.5f;
            obj.transform.localRotation = Quaternion.FromToRotation(Vector3.up, direction.normalized);
            obj.transform.localScale = new Vector3(radius * 2f, direction.magnitude * 0.5f, radius * 2f);
            ApplyMaterial(obj, material);
            DisableCollider(obj);
        }

        private static void ApplyMaterial(GameObject obj, Material material)
        {
            if (material == null)
            {
                return;
            }

            var renderer = obj.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }
        }

        private static Material FindFirstSharedMaterial(Transform root, string namePrefix)
        {
            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                var transform = transforms[i];
                if (transform == null || !transform.name.StartsWith(namePrefix, StringComparison.Ordinal))
                {
                    continue;
                }

                var renderer = transform.GetComponent<Renderer>();
                if (renderer != null && renderer.sharedMaterial != null)
                {
                    return renderer.sharedMaterial;
                }
            }

            return null;
        }

        private static Vector3 FindFirstNonOverlappingZBelowPosition(Transform root, IReadOnlyList<GameObject> protectedRoots)
        {
            var originalPosition = root.position;
            var basePosition = new Vector3(originalPosition.x, 0f, originalPosition.z);
            var firstZ = basePosition.z - 9.0f;
            const float step = 1.5f;
            const int maxAttempts = 80;

            try
            {
                for (var i = 0; i < maxAttempts; i++)
                {
                    var candidate = new Vector3(basePosition.x, basePosition.y, firstZ - (i * step));
                    root.position = candidate;
                    var bounds = GetRendererBounds(root);
                    if (!IntersectsAnyProtectedBounds(bounds, protectedRoots))
                    {
                        return candidate;
                    }
                }
            }
            finally
            {
                root.position = originalPosition;
            }

            throw new InvalidOperationException(
                "Could not find a non-overlapping Z-below-control-room position for " +
                RootName +
                " after " +
                maxAttempts.ToString(CultureInfo.InvariantCulture) +
                " attempts.");
        }

        private static bool IntersectsAnyProtectedBounds(Bounds armoryBounds, IEnumerable<GameObject> protectedRoots)
        {
            foreach (var root in protectedRoots)
            {
                if (root == null || !TryGetRendererBounds(root.transform, out var protectedBounds))
                {
                    continue;
                }

                if (armoryBounds.Intersects(protectedBounds))
                {
                    return true;
                }
            }

            return false;
        }

        private static void EnsureProtectedObjectsUntouched(IReadOnlyList<ProtectedTransformSnapshot> snapshots)
        {
            for (var i = 0; i < snapshots.Count; i++)
            {
                var snapshot = snapshots[i];
                if (snapshot.Transform == null)
                {
                    throw new InvalidOperationException("Protected object was removed: " + snapshot.Path);
                }

                if (snapshot.Transform.gameObject.activeSelf != snapshot.ActiveSelf)
                {
                    throw new InvalidOperationException("Protected object active state changed: " + snapshot.Path);
                }

                if (Vector3.Distance(snapshot.Transform.localPosition, snapshot.LocalPosition) > 0.0001f ||
                    Quaternion.Angle(snapshot.Transform.localRotation, snapshot.LocalRotation) > 0.001f ||
                    Vector3.Distance(snapshot.Transform.localScale, snapshot.LocalScale) > 0.0001f)
                {
                    throw new InvalidOperationException("Protected object transform changed: " + snapshot.Path);
                }
            }
        }

        private static void EnsureOnlyArmoryRootPositionChanged(
            Transform root,
            IReadOnlyList<ProtectedTransformSnapshot> snapshots,
            Vector3 expectedRootPosition)
        {
            for (var i = 0; i < snapshots.Count; i++)
            {
                var snapshot = snapshots[i];
                if (snapshot.Transform == null)
                {
                    throw new InvalidOperationException("Armory object was removed during move: " + snapshot.Path);
                }

                if (snapshot.Transform.gameObject.activeSelf != snapshot.ActiveSelf)
                {
                    throw new InvalidOperationException("Armory active state changed during move: " + snapshot.Path);
                }

                if (snapshot.Transform == root)
                {
                    if (Vector3.Distance(root.position, expectedRootPosition) > 0.0001f ||
                        Quaternion.Angle(root.localRotation, snapshot.LocalRotation) > 0.001f ||
                        Vector3.Distance(root.localScale, snapshot.LocalScale) > 0.0001f)
                    {
                        throw new InvalidOperationException("Armory root changed beyond position-only move.");
                    }

                    continue;
                }

                if (Vector3.Distance(snapshot.Transform.localPosition, snapshot.LocalPosition) > 0.0001f ||
                    Quaternion.Angle(snapshot.Transform.localRotation, snapshot.LocalRotation) > 0.001f ||
                    Vector3.Distance(snapshot.Transform.localScale, snapshot.LocalScale) > 0.0001f)
                {
                    throw new InvalidOperationException("Armory child transform changed during root position move: " + snapshot.Path);
                }
            }
        }

        private static void EnsureNoOverlap(Bounds armoryBounds, IEnumerable<GameObject> protectedRoots)
        {
            foreach (var root in protectedRoots)
            {
                if (root == null || !TryGetRendererBounds(root.transform, out var protectedBounds))
                {
                    continue;
                }

                if (armoryBounds.Intersects(protectedBounds))
                {
                    throw new InvalidOperationException(
                        "Approved armory shell overlaps existing object root " +
                        root.name +
                        ". ArmoryBounds=" +
                        FormatBounds(armoryBounds) +
                        "; ProtectedBounds=" +
                        FormatBounds(protectedBounds));
                }
            }
        }

        private static Bounds GetRendererBounds(Transform root)
        {
            if (TryGetRendererBounds(root, out var bounds))
            {
                return bounds;
            }

            throw new InvalidOperationException("No renderers found under " + root.name);
        }

        private static bool TryGetRendererBounds(Transform root, out Bounds bounds)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            var hasBounds = false;
            bounds = new Bounds(root.position, Vector3.zero);
            for (var i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null || !renderers[i].enabled)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = renderers[i].bounds;
                    hasBounds = true;
                    continue;
                }

                bounds.Encapsulate(renderers[i].bounds);
            }

            return hasBounds;
        }

        private static void DisableAllColliders(Transform root)
        {
            var colliders = root.GetComponentsInChildren<Collider>(true);
            for (var i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = false;
            }
        }

        private static void DisableCollider(GameObject obj)
        {
            var collider = obj.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }
        }

        private static void DeleteGeneratedObject(string objectName)
        {
            var existing = FindNamedObject(objectName);
            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing);
            }
        }

        private static GameObject FindNamedObject(string objectName)
        {
            var transforms = UnityEngine.Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (var i = 0; i < transforms.Length; i++)
            {
                if (transforms[i] != null && transforms[i].gameObject.name == objectName)
                {
                    return transforms[i].gameObject;
                }
            }

            return null;
        }

        private static GameObject RequireObject(string objectName)
        {
            var found = FindNamedObject(objectName);
            if (found == null)
            {
                throw new InvalidOperationException("Missing object: " + objectName);
            }

            return found;
        }

        private static string GetRelativePath(Transform root, Transform transform)
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

        private static string FormatBounds(Bounds bounds)
        {
            return "center=" + FormatVector(bounds.center) + ",size=" + FormatVector(bounds.size);
        }

        private static string FormatVector(Vector3 value)
        {
            return value.x.ToString("0.00", CultureInfo.InvariantCulture) +
                   "," +
                   value.y.ToString("0.00", CultureInfo.InvariantCulture) +
                   "," +
                   value.z.ToString("0.00", CultureInfo.InvariantCulture);
        }

        private static string FormatSiblingPath(IReadOnlyList<int> siblingPath)
        {
            if (siblingPath.Count == 0)
            {
                return ".";
            }

            var builder = new StringBuilder();
            for (var i = 0; i < siblingPath.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append("/");
                }

                builder.Append(siblingPath[i].ToString(CultureInfo.InvariantCulture));
            }

            return builder.ToString();
        }

        private static Scene RequireCargoRunMvpActiveScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.isLoaded || scene.path != Phase4CargoShipGrayboxBootstrap.CargoRunScenePath)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must be the active open scene before capturing or restoring armory current state. ActiveScene=" +
                    scene.path);
            }

            return scene;
        }

        private static string ProjectRoot =>
            Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        public readonly struct CurrentTransformState
        {
            public CurrentTransformState(
                string name,
                int[] siblingPath,
                bool activeSelf,
                Vector3 localPosition,
                Quaternion localRotation,
                Vector3 localScale)
            {
                Name = name;
                SiblingPath = siblingPath;
                ActiveSelf = activeSelf;
                LocalPosition = localPosition;
                LocalRotation = localRotation;
                LocalScale = localScale;
            }

            public string Name { get; }
            public int[] SiblingPath { get; }
            public bool ActiveSelf { get; }
            public Vector3 LocalPosition { get; }
            public Quaternion LocalRotation { get; }
            public Vector3 LocalScale { get; }
        }

        private readonly struct ProtectedTransformSnapshot
        {
            public ProtectedTransformSnapshot(
                string path,
                Transform transform,
                Vector3 localPosition,
                Quaternion localRotation,
                Vector3 localScale,
                bool activeSelf)
            {
                Path = path;
                Transform = transform;
                LocalPosition = localPosition;
                LocalRotation = localRotation;
                LocalScale = localScale;
                ActiveSelf = activeSelf;
            }

            public string Path { get; }
            public Transform Transform { get; }
            public Vector3 LocalPosition { get; }
            public Quaternion LocalRotation { get; }
            public Vector3 LocalScale { get; }
            public bool ActiveSelf { get; }
        }
    }
}
