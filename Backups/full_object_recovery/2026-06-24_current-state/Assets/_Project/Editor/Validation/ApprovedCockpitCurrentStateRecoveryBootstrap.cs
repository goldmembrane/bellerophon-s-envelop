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
    public static class ApprovedCockpitCurrentStateRecoveryBootstrap
    {
        private const string SnapshotRootName = "Approved Cockpit Current State Recovery Snapshot";
        private const string SnapshotPrefabPath = "Assets/_Project/Art/Ship/Cockpit/Recovery/ApprovedCockpitCurrentState.prefab";

        private static readonly string[] RootNames =
        {
            ApprovedCockpitStructureBootstrap.RootName,
            ApprovedCockpitWindowBootstrap.RootName,
            ApprovedCockpitConsoleBootstrap.RootName,
            ApprovedCockpitWarningBootstrap.RootName,
            ApprovedCockpitDestroyedConsoleBootstrap.RootName,
            ApprovedCockpitDirectionBootstrap.RootName,
            ApprovedCockpitLightingBootstrap.RootName
        };

        [MenuItem("Bellerophon/Bootstrap/Capture Approved Cockpit Current State Recovery Snapshot")]
        public static void CaptureCurrentStateRecoverySnapshot()
        {
            RequireCargoRunActiveScene();

            var snapshotRoot = new GameObject(SnapshotRootName);
            snapshotRoot.transform.position = Vector3.zero;
            snapshotRoot.transform.rotation = Quaternion.identity;
            snapshotRoot.transform.localScale = Vector3.one;

            try
            {
                for (var i = 0; i < RootNames.Length; i++)
                {
                    var source = RequireSceneRoot(RootNames[i]);
                    var clone = UnityEngine.Object.Instantiate(source, snapshotRoot.transform, true);
                    clone.name = source.name;
                }

                EnsureSnapshotDirectory();
                var saved = PrefabUtility.SaveAsPrefabAsset(snapshotRoot, SnapshotPrefabPath, out var success);
                if (!success || saved == null)
                {
                    throw new InvalidOperationException("Failed to save approved cockpit current state recovery snapshot: " + SnapshotPrefabPath);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(snapshotRoot);
            }

            AssetDatabase.Refresh();
            ValidateSnapshotAgainstCurrentScene();
            Debug.Log("Approved cockpit current state recovery snapshot captured: " + SnapshotPrefabPath);
        }

        [MenuItem("Bellerophon/Bootstrap/Restore Approved Cockpit Current State")]
        public static void RestoreCurrentState()
        {
            var snapshot = LoadSnapshotPrefab();
            var scene = EditorSceneManager.OpenScene(Phase4CargoShipGrayboxBootstrap.CargoRunScenePath, OpenSceneMode.Single);

            for (var i = 0; i < RootNames.Length; i++)
            {
                DeleteSceneRoots(RootNames[i]);
            }

            var instance = PrefabUtility.InstantiatePrefab(snapshot) as GameObject;
            if (instance == null)
            {
                throw new InvalidOperationException("Failed to instantiate approved cockpit current state recovery snapshot.");
            }

            try
            {
                var children = new List<Transform>();
                for (var i = 0; i < instance.transform.childCount; i++)
                {
                    children.Add(instance.transform.GetChild(i));
                }

                if (children.Count != RootNames.Length)
                {
                    throw new InvalidOperationException("Approved cockpit recovery snapshot root count mismatch. Expected=" + RootNames.Length + "; Current=" + children.Count);
                }

                for (var i = 0; i < RootNames.Length; i++)
                {
                    var child = FindChild(instance.transform, RootNames[i]);
                    if (child == null)
                    {
                        throw new InvalidOperationException("Approved cockpit recovery snapshot is missing root: " + RootNames[i]);
                    }

                    child.SetParent(null, true);
                    child.gameObject.name = RootNames[i];
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, Phase4CargoShipGrayboxBootstrap.CargoRunScenePath);
            AssetDatabase.Refresh();
            ValidateSnapshotAgainstCurrentScene();
            Debug.Log("Approved cockpit current state restored from recovery snapshot: " + SnapshotPrefabPath);
        }

        [MenuItem("Bellerophon/Validation/Validate Approved Cockpit Current State Recovery Snapshot")]
        public static void ValidateSnapshotAgainstCurrentScene()
        {
            RequireCargoRunActiveScene();
            var snapshot = LoadSnapshotPrefab();

            var snapshotByRoot = new Dictionary<string, Transform>(StringComparer.Ordinal);
            for (var i = 0; i < snapshot.transform.childCount; i++)
            {
                var child = snapshot.transform.GetChild(i);
                snapshotByRoot[child.name] = child;
            }

            var sceneSignature = new StringBuilder();
            var snapshotSignature = new StringBuilder();
            for (var i = 0; i < RootNames.Length; i++)
            {
                var rootName = RootNames[i];
                var sceneRoot = RequireSceneRoot(rootName);
                if (!snapshotByRoot.TryGetValue(rootName, out var snapshotRoot))
                {
                    throw new InvalidOperationException("Approved cockpit recovery snapshot is missing root: " + rootName);
                }

                AppendRootSignature(sceneSignature, sceneRoot.transform);
                AppendRootSignature(snapshotSignature, snapshotRoot);
            }

            var sceneValue = sceneSignature.ToString();
            var snapshotValue = snapshotSignature.ToString();
            if (!string.Equals(sceneValue, snapshotValue, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Approved cockpit recovery snapshot does not match the current cockpit scene state.");
            }

            Debug.Log("Approved cockpit current state recovery snapshot validation passed. Roots=" + RootNames.Length + "; SignatureLength=" + sceneValue.Length);
        }

        private static void EnsureSnapshotDirectory()
        {
            var projectRoot = Directory.GetParent(Application.dataPath);
            if (projectRoot == null)
            {
                throw new InvalidOperationException("Could not resolve project root for cockpit recovery snapshot.");
            }

            var fullPath = Path.Combine(projectRoot.FullName, SnapshotPrefabPath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath) ?? projectRoot.FullName);
        }

        private static GameObject LoadSnapshotPrefab()
        {
            var snapshot = AssetDatabase.LoadAssetAtPath<GameObject>(SnapshotPrefabPath);
            if (snapshot == null)
            {
                throw new InvalidOperationException("Missing approved cockpit current state recovery snapshot: " + SnapshotPrefabPath);
            }

            return snapshot;
        }

        private static void RequireCargoRunActiveScene()
        {
            var activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid())
            {
                throw new InvalidOperationException("No active scene is open for approved cockpit current state recovery.");
            }

            var normalizedActivePath = activeScene.path.Replace('\\', '/');
            var normalizedCargoPath = Phase4CargoShipGrayboxBootstrap.CargoRunScenePath.Replace('\\', '/');
            if (!string.Equals(normalizedActivePath, normalizedCargoPath, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Current active scene is not CargoRunMvp. ActiveScene=" + activeScene.path);
            }
        }

        private static GameObject RequireSceneRoot(string rootName)
        {
            var roots = FindSceneRoots(rootName);
            if (roots.Count == 0)
            {
                throw new InvalidOperationException("Missing approved cockpit scene root: " + rootName);
            }

            if (roots.Count > 1)
            {
                throw new InvalidOperationException("Duplicate approved cockpit scene roots found: " + rootName + "; Count=" + roots.Count);
            }

            return roots[0];
        }

        private static void DeleteSceneRoots(string rootName)
        {
            var roots = FindSceneRoots(rootName);
            for (var i = 0; i < roots.Count; i++)
            {
                UnityEngine.Object.DestroyImmediate(roots[i]);
            }
        }

        private static List<GameObject> FindSceneRoots(string rootName)
        {
            var found = new List<GameObject>();
            var roots = SceneManager.GetActiveScene().GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                if (roots[i].name == rootName)
                {
                    found.Add(roots[i]);
                }
            }

            return found;
        }

        private static Transform FindChild(Transform parent, string childName)
        {
            for (var i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (child.name == childName)
                {
                    return child;
                }
            }

            return null;
        }

        private static void AppendRootSignature(StringBuilder builder, Transform root)
        {
            var lines = new List<string>();
            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                var transform = transforms[i];
                var path = GetRelativePath(root, transform);
                var line = new StringBuilder();
                line.Append(path)
                    .Append("|active=")
                    .Append(transform.gameObject.activeSelf)
                    .Append("|pos=")
                    .Append(FormatVector(transform.localPosition))
                    .Append("|rot=")
                    .Append(FormatQuaternion(transform.localRotation))
                    .Append("|scale=")
                    .Append(FormatVector(transform.localScale));

                var renderer = transform.GetComponent<Renderer>();
                if (renderer != null)
                {
                    line.Append("|renderer=")
                        .Append(renderer.enabled)
                        .Append("|materials=");
                    var materials = renderer.sharedMaterials;
                    for (var materialIndex = 0; materialIndex < materials.Length; materialIndex++)
                    {
                        if (materialIndex > 0)
                        {
                            line.Append(",");
                        }

                        line.Append(materials[materialIndex] == null ? "<null>" : materials[materialIndex].name);
                    }
                }

                var light = transform.GetComponent<Light>();
                if (light != null)
                {
                    line.Append("|light=")
                        .Append(light.enabled)
                        .Append("|type=")
                        .Append(light.type)
                        .Append("|intensity=")
                        .Append(FormatFloat(light.intensity))
                        .Append("|range=")
                        .Append(FormatFloat(light.range));
                }

                var collider = transform.GetComponent<Collider>();
                if (collider != null)
                {
                    line.Append("|collider=")
                        .Append(collider.enabled);
                }

                lines.Add(line.ToString());
            }

            lines.Sort(StringComparer.Ordinal);
            for (var i = 0; i < lines.Count; i++)
            {
                builder.AppendLine(lines[i]);
            }
        }

        private static string GetRelativePath(Transform root, Transform transform)
        {
            if (transform == root)
            {
                return root.name;
            }

            var parts = new List<string>();
            var current = transform;
            while (current != null && current != root)
            {
                parts.Add(current.name);
                current = current.parent;
            }

            parts.Add(root.name);
            parts.Reverse();
            return string.Join("/", parts);
        }

        private static string FormatVector(Vector3 value)
        {
            return FormatFloat(value.x) + "," + FormatFloat(value.y) + "," + FormatFloat(value.z);
        }

        private static string FormatQuaternion(Quaternion value)
        {
            return FormatFloat(value.x) + "," + FormatFloat(value.y) + "," + FormatFloat(value.z) + "," + FormatFloat(value.w);
        }

        private static string FormatFloat(float value)
        {
            if (Mathf.Abs(value) < 0.0000005f)
            {
                value = 0f;
            }

            return value.ToString("0.######", CultureInfo.InvariantCulture);
        }
    }
}
