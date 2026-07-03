using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Bellerophon.Editor.Validation
{
    public static class ApprovedCockpitWarningBootstrap
    {
        public const string RootName = "Approved Cockpit 04 Warning";

        private const string SampleRootRelativePath = "artSample/ck_warn04";
        private const string ComparisonRootName = "unity_applied_comparison";
        private const string SourceFbxRelativePath = "artSample/ck_warn04/exports/ck_warn04.fbx";
        private const string UnityAssetDirectory = "Assets/_Project/Art/Ship/Cockpit";
        private const string UnityFbxPath = UnityAssetDirectory + "/ck_warn04.fbx";
        private const string BodyMaterialPath = UnityAssetDirectory + "/M_CkWarn04_Body.mat";
        private const string DarkMaterialPath = UnityAssetDirectory + "/M_CkWarn04_Dark.mat";
        private const string RubberMaterialPath = UnityAssetDirectory + "/M_CkWarn04_Rubber.mat";
        private const string RedMaterialPath = UnityAssetDirectory + "/M_CkWarn04_Red.mat";
        private const string GlowMaterialPath = UnityAssetDirectory + "/M_CkWarn04_Glow.mat";
        private const string HazardMaterialPath = UnityAssetDirectory + "/M_CkWarn04_Hazard.mat";
        private const string WearMaterialPath = UnityAssetDirectory + "/M_CkWarn04_Wear.mat";
        private const string DecalMaterialPath = UnityAssetDirectory + "/M_CkWarn04_Decal.mat";
        private const string DecalTexturePath = "Assets/Sci-Fi Styled Modular Pack/Textures/projector_warning.png";

        private static readonly Vector3 CockpitCenter = new Vector3(0f, 0f, 18f);
        private static readonly Vector3 RootWorldPosition = CockpitCenter;
        private static readonly Vector3 ScreenBarTargetCenter = CockpitCenter + new Vector3(0f, 2.68f, -3.45f);
        private static readonly Vector3 CeilingBeaconTargetCenter = CockpitCenter + new Vector3(0.23f, 2.92f, 0f);
        private static readonly string[] RemovedAfterEditorReviewObjectNames =
        {
            "upper alarm bar hazard tab 1",
            "upper alarm bar hazard tab 2",
            "central ceiling emergency rotary beacon ceiling power cable"
        };

        [MenuItem("Bellerophon/Bootstrap/Ensure Approved Cockpit 04 Warning")]
        public static void EnsureApprovedCockpitWarning()
        {
            var scene = EditorSceneManager.OpenScene(Phase4CargoShipGrayboxBootstrap.CargoRunScenePath, OpenSceneMode.Single);

            CargoShipVisualModelingBootstrap.DisableVisualModeling();
            ModelingInspectionModeBootstrap.DisableTutorialLogicForModeling();
            scene = EditorSceneManager.OpenScene(Phase4CargoShipGrayboxBootstrap.CargoRunScenePath, OpenSceneMode.Single);

            if (FindNamedObject(ApprovedCockpitStructureBootstrap.RootName) == null)
            {
                ApprovedCockpitStructureBootstrap.EnsureApprovedCockpitStructure();
                scene = EditorSceneManager.OpenScene(Phase4CargoShipGrayboxBootstrap.CargoRunScenePath, OpenSceneMode.Single);
            }

            if (FindNamedObject(ApprovedCockpitWindowBootstrap.RootName) == null)
            {
                ApprovedCockpitWindowBootstrap.EnsureApprovedCockpitWindow();
                scene = EditorSceneManager.OpenScene(Phase4CargoShipGrayboxBootstrap.CargoRunScenePath, OpenSceneMode.Single);
            }

            if (FindNamedObject(ApprovedCockpitConsoleBootstrap.RootName) == null)
            {
                ApprovedCockpitConsoleBootstrap.EnsureApprovedCockpitConsole();
                scene = EditorSceneManager.OpenScene(Phase4CargoShipGrayboxBootstrap.CargoRunScenePath, OpenSceneMode.Single);
            }

            DeleteGeneratedObject(RootName);
            CopyApprovedSourceFbx();

            var source = AssetDatabase.LoadAssetAtPath<GameObject>(UnityFbxPath);
            if (source == null)
            {
                throw new InvalidOperationException("Approved cockpit warning source FBX failed to import: " + UnityFbxPath);
            }

            var root = PrefabUtility.InstantiatePrefab(source) as GameObject;
            if (root == null)
            {
                throw new InvalidOperationException("Approved cockpit warning source FBX could not be instantiated: " + UnityFbxPath);
            }

            root.name = RootName;
            root.transform.position = RootWorldPosition;
            root.transform.rotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;

            RemovePreviewOnlyObjects(root.transform);
            RemoveNonApprovedMeshes(root.transform);
            PositionApprovedGroups(root.transform);

            var materials = EnsureMaterials();
            ApplyApprovedMaterials(root.transform, materials);
            DisableAllColliders(root.transform);
            ModelingInspectionModeBootstrap.ApplyFreeCameraForModeling();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, Phase4CargoShipGrayboxBootstrap.CargoRunScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            ValidateScene();
            Debug.Log("Approved cockpit 04 warning applied.");
        }

        [MenuItem("Bellerophon/Validation/Validate Approved Cockpit 04 Warning")]
        public static void ValidateScene()
        {
            EditorSceneManager.OpenScene(Phase4CargoShipGrayboxBootstrap.CargoRunScenePath, OpenSceneMode.Single);

            ApprovedCockpitStructureBootstrap.ValidateScene();
            ApprovedCockpitWindowBootstrap.ValidateScene();
            ApprovedCockpitConsoleBootstrap.ValidateScene();

            var root = RequireObject(RootName);
            if (!root.activeInHierarchy)
            {
                throw new InvalidOperationException(RootName + " must be active after user approval.");
            }

            var renderers = root.GetComponentsInChildren<Renderer>(true);
            var enabledRenderers = 0;
            var hasScreenBar = false;
            var hasCeilingBeacon = false;
            for (var i = 0; i < renderers.Length; i++)
            {
                if (!renderers[i].enabled)
                {
                    continue;
                }

                enabledRenderers++;
                var lowerName = renderers[i].gameObject.name.ToLowerInvariant();
                hasScreenBar |= IsScreenBarPart(lowerName);
                hasCeilingBeacon |= IsCeilingBeaconPart(lowerName);
            }

            if (enabledRenderers < 18)
            {
                throw new InvalidOperationException("Approved cockpit warning renderer count is too low: " + enabledRenderers);
            }

            if (!hasScreenBar || !hasCeilingBeacon)
            {
                throw new InvalidOperationException("Approved cockpit warning must contain the screen-top alarm bar and central ceiling beacon. HasScreenBar=" + hasScreenBar + "; HasCeilingBeacon=" + hasCeilingBeacon);
            }

            ValidateNoPreviewOrRejectedObjects(root.transform);
            ValidateGroupPlacement(root.transform);

            var enabledColliders = CountEnabledColliders(root.transform);
            if (enabledColliders != 0)
            {
                throw new InvalidOperationException("Approved cockpit warning must not introduce gameplay colliders. EnabledColliders=" + enabledColliders);
            }

            CargoShipVisualModelingBootstrap.ValidateScene();
            ModelingInspectionModeBootstrap.ValidateScene();
            ModelingInspectionModeBootstrap.ValidateFreeCamera();
            Debug.Log(
                "Approved cockpit 04 warning validation passed. Renderers=" +
                enabledRenderers +
                "; EnabledColliders=0; CeilingCenter=" +
                FormatVector(CeilingBeaconTargetCenter));
        }

        [MenuItem("Bellerophon/Validation/Capture Approved Cockpit 04 Warning Comparison")]
        public static void CaptureUnityComparison()
        {
            ValidateScene();

            var projectRoot = Directory.GetParent(Application.dataPath);
            if (projectRoot == null)
            {
                throw new InvalidOperationException("Could not resolve project root for cockpit warning comparison output.");
            }

            var outputRoot = Path.Combine(projectRoot.FullName, SampleRootRelativePath, ComparisonRootName);
            Directory.CreateDirectory(outputRoot);

            CaptureAppliedView(
                Path.Combine(outputRoot, "unity_01_front.png"),
                CockpitCenter + new Vector3(0f, 2.05f, 2.8f),
                CockpitCenter + new Vector3(0f, 2.55f, -3.25f),
                38f,
                false,
                5f,
                Vector3.up);
            CaptureAppliedView(
                Path.Combine(outputRoot, "unity_02_player.png"),
                CockpitCenter + new Vector3(0f, 1.62f, 1.65f),
                CockpitCenter + new Vector3(0f, 2.75f, -0.35f),
                43f,
                false,
                5f,
                Vector3.up);
            CaptureAppliedView(
                Path.Combine(outputRoot, "unity_03_side.png"),
                CockpitCenter + new Vector3(3.8f, 2.25f, 1.2f),
                CockpitCenter + new Vector3(0f, 2.65f, -2.2f),
                44f,
                false,
                5f,
                Vector3.up);
            CaptureAppliedView(
                Path.Combine(outputRoot, "unity_04_top.png"),
                CockpitCenter + new Vector3(0f, 7.4f, 0.05f),
                CockpitCenter + new Vector3(0f, 2.2f, -1.4f),
                42f,
                true,
                5.4f,
                Vector3.forward);
            CaptureAppliedView(
                Path.Combine(outputRoot, "unity_05_detail.png"),
                ScreenBarTargetCenter + new Vector3(0f, 0.12f, 1.55f),
                ScreenBarTargetCenter + new Vector3(0f, -0.03f, 0f),
                36f,
                false,
                5f,
                Vector3.up);
            CaptureAppliedView(
                Path.Combine(outputRoot, "unity_06_ceiling.png"),
                CeilingBeaconTargetCenter + new Vector3(1.35f, -0.58f, 1.35f),
                CeilingBeaconTargetCenter + new Vector3(0f, -0.1f, 0f),
                36f,
                false,
                5f,
                Vector3.up);

            WriteComparisonIndex(outputRoot);
            AssetDatabase.Refresh();
            Debug.Log("Approved cockpit 04 warning Unity comparison snapshots saved: " + outputRoot);
        }

        [MenuItem("Bellerophon/Validation/Capture Approved Cockpit 04 Warning Current Objects")]
        public static void CaptureCurrentEditorObjects()
        {
            var root = FindNamedObject(RootName);
            if (root == null)
            {
                throw new InvalidOperationException("Cannot capture current cockpit warning objects because the scene object is missing: " + RootName);
            }

            var projectRoot = Directory.GetParent(Application.dataPath);
            if (projectRoot == null)
            {
                throw new InvalidOperationException("Could not resolve project root for cockpit warning object capture.");
            }

            var outputRoot = Path.Combine(projectRoot.FullName, SampleRootRelativePath, "editor_current");
            Directory.CreateDirectory(outputRoot);
            var outputPath = Path.Combine(outputRoot, "warning_objects.txt");

            var expectedNames = CollectApprovedSourceRendererNames();
            var currentNames = CollectApprovedRendererNames(root.transform);
            var transformChanges = CaptureEditorTransformChanges(root.transform);
            var statusChanges = CaptureEditorStatusChanges(root.transform);
            var materialChanges = CaptureEditorMaterialChanges(root.transform);
            expectedNames.Sort(StringComparer.OrdinalIgnoreCase);
            currentNames.Sort(StringComparer.OrdinalIgnoreCase);

            var missingNames = new List<string>();
            for (var i = 0; i < expectedNames.Count; i++)
            {
                if (!currentNames.Contains(expectedNames[i]))
                {
                    missingNames.Add(expectedNames[i]);
                }
            }

            var builder = new StringBuilder();
            builder.AppendLine("# Approved Cockpit 04 Warning current editor objects");
            builder.AppendLine("# Generated from the currently open Unity editor scene without reopening or regenerating the scene.");
            builder.AppendLine();
            builder.Append("Root = ").AppendLine(root.name);
            builder.Append("ExpectedApprovedRendererCount = ").AppendLine(expectedNames.Count.ToString());
            builder.Append("CurrentApprovedRendererCount = ").AppendLine(currentNames.Count.ToString());
            builder.Append("MissingApprovedRendererCount = ").AppendLine(missingNames.Count.ToString());
            builder.Append("ChangedTransformCount = ").AppendLine(transformChanges.Count.ToString());
            builder.Append("ChangedStatusCount = ").AppendLine(statusChanges.Count.ToString());
            builder.Append("ChangedMaterialCount = ").AppendLine(materialChanges.Count.ToString());
            builder.AppendLine();
            builder.AppendLine("[MissingApprovedRenderers]");
            AppendNameList(builder, missingNames);
            builder.AppendLine();
            builder.AppendLine("[ChangedTransforms]");
            AppendTransformChanges(builder, transformChanges);
            builder.AppendLine();
            builder.AppendLine("[ChangedStatuses]");
            AppendStatusChanges(builder, statusChanges);
            builder.AppendLine();
            builder.AppendLine("[ChangedMaterials]");
            AppendMaterialChanges(builder, materialChanges);
            builder.AppendLine();
            builder.AppendLine("[CurrentApprovedRenderers]");
            AppendNameList(builder, currentNames);
            builder.AppendLine();
            builder.AppendLine("[ExpectedApprovedRenderers]");
            AppendNameList(builder, expectedNames);

            File.WriteAllText(outputPath, builder.ToString(), new UTF8Encoding(false));
            AssetDatabase.Refresh();
            Debug.Log("Approved cockpit 04 warning current object capture saved: " + outputPath);
        }

        [MenuItem("Bellerophon/Validation/Capture Approved Cockpit 04 Warning Backup Objects")]
        public static void CaptureBackupEditorObjects()
        {
            var projectRoot = Directory.GetParent(Application.dataPath);
            if (projectRoot == null)
            {
                throw new InvalidOperationException("Could not resolve project root for cockpit warning backup object capture.");
            }

            var backupPath = Path.Combine(projectRoot.FullName, "Temp", "__Backupscenes", "0.backup");
            if (!File.Exists(backupPath))
            {
                backupPath = Path.Combine(
                    projectRoot.FullName,
                    SampleRootRelativePath,
                    "editor_current",
                    "scene_recovery_2026-06-15_173146.backup");
            }

            if (!File.Exists(backupPath))
            {
                throw new FileNotFoundException("Missing Unity scene recovery backup.", backupPath);
            }

            const string tempSceneAssetPath = "Assets/_Project/Scenes/__RecoveredWarningCapture.unity";
            File.Copy(backupPath, Path.Combine(projectRoot.FullName, tempSceneAssetPath), true);
            AssetDatabase.ImportAsset(tempSceneAssetPath, ImportAssetOptions.ForceUpdate);

            try
            {
                EditorSceneManager.OpenScene(tempSceneAssetPath, OpenSceneMode.Single);
                CaptureCurrentEditorObjectsToFile("warning_backup_objects.txt", "Generated from Unity scene recovery backup.");
            }
            finally
            {
                EditorSceneManager.OpenScene(Phase4CargoShipGrayboxBootstrap.CargoRunScenePath, OpenSceneMode.Single);
                AssetDatabase.DeleteAsset(tempSceneAssetPath);
            }

            Debug.Log("Approved cockpit 04 warning backup object capture saved.");
        }

        [MenuItem("Bellerophon/Validation/Capture Approved Cockpit Backup Diff")]
        public static void CaptureApprovedCockpitBackupDiff()
        {
            var projectRoot = Directory.GetParent(Application.dataPath);
            if (projectRoot == null)
            {
                throw new InvalidOperationException("Could not resolve project root for approved cockpit backup diff.");
            }

            var backupPath = Path.Combine(
                projectRoot.FullName,
                SampleRootRelativePath,
                "editor_current",
                "scene_recovery_2026-06-15_173146.backup");
            if (!File.Exists(backupPath))
            {
                throw new FileNotFoundException("Missing preserved Unity scene recovery backup.", backupPath);
            }

            const string tempSceneAssetPath = "Assets/_Project/Scenes/__RecoveredCockpitDiff.unity";
            var backupEntries = new Dictionary<string, SceneRendererSnapshot>(StringComparer.Ordinal);
            var currentEntries = new Dictionary<string, SceneRendererSnapshot>(StringComparer.Ordinal);
            try
            {
                File.Copy(backupPath, Path.Combine(projectRoot.FullName, tempSceneAssetPath), true);
                AssetDatabase.ImportAsset(tempSceneAssetPath, ImportAssetOptions.ForceUpdate);

                EditorSceneManager.OpenScene(tempSceneAssetPath, OpenSceneMode.Single);
                CollectApprovedCockpitSceneSnapshots(backupEntries);

                EditorSceneManager.OpenScene(Phase4CargoShipGrayboxBootstrap.CargoRunScenePath, OpenSceneMode.Single);
                CollectApprovedCockpitSceneSnapshots(currentEntries);
            }
            finally
            {
                EditorSceneManager.OpenScene(Phase4CargoShipGrayboxBootstrap.CargoRunScenePath, OpenSceneMode.Single);
                AssetDatabase.DeleteAsset(tempSceneAssetPath);
            }

            var outputRoot = Path.Combine(projectRoot.FullName, SampleRootRelativePath, "editor_current");
            Directory.CreateDirectory(outputRoot);
            var outputPath = Path.Combine(outputRoot, "cockpit_backup_diff.txt");
            WriteCockpitBackupDiff(outputPath, currentEntries, backupEntries);
            AssetDatabase.Refresh();
            Debug.Log("Approved cockpit backup diff saved: " + outputPath);
        }

        [MenuItem("Bellerophon/Validation/Capture Scene Backup Diff")]
        public static void CaptureSceneBackupDiff()
        {
            var projectRoot = Directory.GetParent(Application.dataPath);
            if (projectRoot == null)
            {
                throw new InvalidOperationException("Could not resolve project root for scene backup diff.");
            }

            var backupPath = Path.Combine(
                projectRoot.FullName,
                SampleRootRelativePath,
                "editor_current",
                "scene_recovery_2026-06-15_173146.backup");
            if (!File.Exists(backupPath))
            {
                throw new FileNotFoundException("Missing preserved Unity scene recovery backup.", backupPath);
            }

            const string tempSceneAssetPath = "Assets/_Project/Scenes/__RecoveredSceneDiff.unity";
            var backupEntries = new Dictionary<string, SceneRendererSnapshot>(StringComparer.Ordinal);
            var currentEntries = new Dictionary<string, SceneRendererSnapshot>(StringComparer.Ordinal);
            try
            {
                File.Copy(backupPath, Path.Combine(projectRoot.FullName, tempSceneAssetPath), true);
                AssetDatabase.ImportAsset(tempSceneAssetPath, ImportAssetOptions.ForceUpdate);

                EditorSceneManager.OpenScene(tempSceneAssetPath, OpenSceneMode.Single);
                CollectWholeSceneRendererSnapshots(backupEntries);

                EditorSceneManager.OpenScene(Phase4CargoShipGrayboxBootstrap.CargoRunScenePath, OpenSceneMode.Single);
                CollectWholeSceneRendererSnapshots(currentEntries);
            }
            finally
            {
                EditorSceneManager.OpenScene(Phase4CargoShipGrayboxBootstrap.CargoRunScenePath, OpenSceneMode.Single);
                AssetDatabase.DeleteAsset(tempSceneAssetPath);
            }

            var outputRoot = Path.Combine(projectRoot.FullName, SampleRootRelativePath, "editor_current");
            Directory.CreateDirectory(outputRoot);
            var outputPath = Path.Combine(outputRoot, "scene_backup_diff.txt");
            WriteCockpitBackupDiff(outputPath, currentEntries, backupEntries);
            AssetDatabase.Refresh();
            Debug.Log("Scene backup diff saved: " + outputPath);
        }

        [MenuItem("Bellerophon/Validation/Capture Scene Transform Backup Diff")]
        public static void CaptureSceneTransformBackupDiff()
        {
            var projectRoot = Directory.GetParent(Application.dataPath);
            if (projectRoot == null)
            {
                throw new InvalidOperationException("Could not resolve project root for scene transform backup diff.");
            }

            var backupPath = Path.Combine(
                projectRoot.FullName,
                SampleRootRelativePath,
                "editor_current",
                "scene_recovery_2026-06-15_173146.backup");
            if (!File.Exists(backupPath))
            {
                throw new FileNotFoundException("Missing preserved Unity scene recovery backup.", backupPath);
            }

            const string tempSceneAssetPath = "Assets/_Project/Scenes/__RecoveredSceneTransformDiff.unity";
            var backupEntries = new Dictionary<string, SceneTransformSnapshot>(StringComparer.Ordinal);
            var currentEntries = new Dictionary<string, SceneTransformSnapshot>(StringComparer.Ordinal);
            try
            {
                File.Copy(backupPath, Path.Combine(projectRoot.FullName, tempSceneAssetPath), true);
                AssetDatabase.ImportAsset(tempSceneAssetPath, ImportAssetOptions.ForceUpdate);

                EditorSceneManager.OpenScene(tempSceneAssetPath, OpenSceneMode.Single);
                CollectWholeSceneTransformSnapshots(backupEntries);

                EditorSceneManager.OpenScene(Phase4CargoShipGrayboxBootstrap.CargoRunScenePath, OpenSceneMode.Single);
                CollectWholeSceneTransformSnapshots(currentEntries);
            }
            finally
            {
                EditorSceneManager.OpenScene(Phase4CargoShipGrayboxBootstrap.CargoRunScenePath, OpenSceneMode.Single);
                AssetDatabase.DeleteAsset(tempSceneAssetPath);
            }

            var outputRoot = Path.Combine(projectRoot.FullName, SampleRootRelativePath, "editor_current");
            Directory.CreateDirectory(outputRoot);
            var outputPath = Path.Combine(outputRoot, "scene_transform_backup_diff.txt");
            WriteSceneTransformBackupDiff(outputPath, currentEntries, backupEntries);
            AssetDatabase.Refresh();
            Debug.Log("Scene transform backup diff saved: " + outputPath);
        }

        private static void CaptureCurrentEditorObjectsToFile(string fileName, string note)
        {
            var root = FindNamedObject(RootName);
            if (root == null)
            {
                throw new InvalidOperationException("Cannot capture current cockpit warning objects because the scene object is missing: " + RootName);
            }

            var projectRoot = Directory.GetParent(Application.dataPath);
            if (projectRoot == null)
            {
                throw new InvalidOperationException("Could not resolve project root for cockpit warning object capture.");
            }

            var outputRoot = Path.Combine(projectRoot.FullName, SampleRootRelativePath, "editor_current");
            Directory.CreateDirectory(outputRoot);
            var outputPath = Path.Combine(outputRoot, fileName);

            var expectedNames = CollectApprovedSourceRendererNames();
            var currentNames = CollectApprovedRendererNames(root.transform);
            var transformChanges = CaptureEditorTransformChanges(root.transform);
            var statusChanges = CaptureEditorStatusChanges(root.transform);
            var materialChanges = CaptureEditorMaterialChanges(root.transform);
            expectedNames.Sort(StringComparer.OrdinalIgnoreCase);
            currentNames.Sort(StringComparer.OrdinalIgnoreCase);

            var missingNames = new List<string>();
            for (var i = 0; i < expectedNames.Count; i++)
            {
                if (!currentNames.Contains(expectedNames[i]))
                {
                    missingNames.Add(expectedNames[i]);
                }
            }

            var builder = new StringBuilder();
            builder.AppendLine("# Approved Cockpit 04 Warning current editor objects");
            builder.Append("# ").AppendLine(note);
            builder.AppendLine();
            builder.Append("Root = ").AppendLine(root.name);
            builder.Append("ExpectedApprovedRendererCount = ").AppendLine(expectedNames.Count.ToString());
            builder.Append("CurrentApprovedRendererCount = ").AppendLine(currentNames.Count.ToString());
            builder.Append("MissingApprovedRendererCount = ").AppendLine(missingNames.Count.ToString());
            builder.Append("ChangedTransformCount = ").AppendLine(transformChanges.Count.ToString());
            builder.Append("ChangedStatusCount = ").AppendLine(statusChanges.Count.ToString());
            builder.Append("ChangedMaterialCount = ").AppendLine(materialChanges.Count.ToString());
            builder.AppendLine();
            builder.AppendLine("[MissingApprovedRenderers]");
            AppendNameList(builder, missingNames);
            builder.AppendLine();
            builder.AppendLine("[ChangedTransforms]");
            AppendTransformChanges(builder, transformChanges);
            builder.AppendLine();
            builder.AppendLine("[ChangedStatuses]");
            AppendStatusChanges(builder, statusChanges);
            builder.AppendLine();
            builder.AppendLine("[ChangedMaterials]");
            AppendMaterialChanges(builder, materialChanges);
            builder.AppendLine();
            builder.AppendLine("[CurrentApprovedRenderers]");
            AppendNameList(builder, currentNames);
            builder.AppendLine();
            builder.AppendLine("[ExpectedApprovedRenderers]");
            AppendNameList(builder, expectedNames);

            File.WriteAllText(outputPath, builder.ToString(), new UTF8Encoding(false));
            AssetDatabase.Refresh();
            Debug.Log("Approved cockpit 04 warning current object capture saved: " + outputPath);
        }

        private static void CollectApprovedCockpitSceneSnapshots(IDictionary<string, SceneRendererSnapshot> output)
        {
            CollectSceneRendererSnapshots(ApprovedCockpitStructureBootstrap.RootName, output);
            CollectSceneRendererSnapshots(ApprovedCockpitWindowBootstrap.RootName, output);
            CollectSceneRendererSnapshots(ApprovedCockpitConsoleBootstrap.RootName, output);
            CollectSceneRendererSnapshots(RootName, output);
        }

        private static void CollectWholeSceneRendererSnapshots(IDictionary<string, SceneRendererSnapshot> output)
        {
            var roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                CollectSceneRendererSnapshots(roots[i].name, roots[i].transform, output);
            }
        }

        private static void CollectWholeSceneTransformSnapshots(IDictionary<string, SceneTransformSnapshot> output)
        {
            var roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                CollectTransformSnapshots(roots[i].name, roots[i].transform, output);
            }
        }

        private static void CollectTransformSnapshots(string rootName, Transform root, IDictionary<string, SceneTransformSnapshot> output)
        {
            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                var transform = transforms[i];
                if (transform == null)
                {
                    continue;
                }

                var relativePath = GetRelativePath(root, transform);
                var key = rootName + "/" + relativePath;
                output[key] = new SceneTransformSnapshot(
                    key,
                    transform.gameObject.name,
                    transform.gameObject.activeSelf,
                    transform.gameObject.activeInHierarchy,
                    transform.position,
                    transform.rotation,
                    transform.lossyScale);
            }
        }

        private static void CollectSceneRendererSnapshots(string rootName, IDictionary<string, SceneRendererSnapshot> output)
        {
            var root = FindNamedObject(rootName);
            if (root == null)
            {
                output[rootName] = new SceneRendererSnapshot(rootName, rootName, false, false, false, Vector3.zero, Quaternion.identity, Vector3.zero, Array.Empty<string>());
                return;
            }

            CollectSceneRendererSnapshots(rootName, root.transform, output);
        }

        private static void CollectSceneRendererSnapshots(string rootName, Transform root, IDictionary<string, SceneRendererSnapshot> output)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                var relativePath = GetRelativePath(root.transform, renderer.transform);
                var key = rootName + "/" + relativePath;
                output[key] = new SceneRendererSnapshot(
                    key,
                    renderer.gameObject.name,
                    true,
                    renderer.gameObject.activeSelf,
                    renderer.enabled,
                    renderer.transform.position,
                    renderer.transform.rotation,
                    renderer.transform.lossyScale,
                    CollectCurrentMaterialNames(renderer));
            }
        }

        private static void WriteCockpitBackupDiff(
            string outputPath,
            Dictionary<string, SceneRendererSnapshot> currentEntries,
            Dictionary<string, SceneRendererSnapshot> backupEntries)
        {
            var removed = new List<SceneRendererSnapshot>();
            var added = new List<SceneRendererSnapshot>();
            var changed = new List<SceneRendererChange>();

            foreach (var current in currentEntries)
            {
                if (!backupEntries.ContainsKey(current.Key))
                {
                    removed.Add(current.Value);
                }
            }

            foreach (var backup in backupEntries)
            {
                if (!currentEntries.TryGetValue(backup.Key, out var current))
                {
                    added.Add(backup.Value);
                    continue;
                }

                var hasChange =
                    current.ActiveSelf != backup.Value.ActiveSelf ||
                    current.RendererEnabled != backup.Value.RendererEnabled ||
                    Vector3.Distance(current.Position, backup.Value.Position) > 0.003f ||
                    Quaternion.Angle(current.Rotation, backup.Value.Rotation) > 0.05f ||
                    Vector3.Distance(current.Scale, backup.Value.Scale) > 0.003f ||
                    !HaveSameMaterialNames(current.Materials, backup.Value.Materials);
                if (hasChange)
                {
                    changed.Add(new SceneRendererChange(current, backup.Value));
                }
            }

            removed.Sort((left, right) => string.Compare(left.Key, right.Key, StringComparison.Ordinal));
            added.Sort((left, right) => string.Compare(left.Key, right.Key, StringComparison.Ordinal));
            changed.Sort((left, right) => string.Compare(left.Current.Key, right.Current.Key, StringComparison.Ordinal));

            var builder = new StringBuilder();
            builder.AppendLine("# Approved Cockpit Backup Diff");
            builder.AppendLine("# Current = saved CargoRunMvp. Backup = preserved Unity scene recovery backup.");
            builder.AppendLine();
            builder.Append("CurrentRendererCount = ").AppendLine(currentEntries.Count.ToString());
            builder.Append("BackupRendererCount = ").AppendLine(backupEntries.Count.ToString());
            builder.Append("RemovedInBackupCount = ").AppendLine(removed.Count.ToString());
            builder.Append("AddedInBackupCount = ").AppendLine(added.Count.ToString());
            builder.Append("ChangedInBackupCount = ").AppendLine(changed.Count.ToString());
            builder.AppendLine();
            builder.AppendLine("[RemovedInBackup]");
            AppendSceneSnapshotList(builder, removed);
            builder.AppendLine();
            builder.AppendLine("[AddedInBackup]");
            AppendSceneSnapshotList(builder, added);
            builder.AppendLine();
            builder.AppendLine("[ChangedInBackup]");
            AppendSceneChangeList(builder, changed);

            File.WriteAllText(outputPath, builder.ToString(), new UTF8Encoding(false));
        }

        private static void AppendSceneSnapshotList(StringBuilder builder, List<SceneRendererSnapshot> snapshots)
        {
            if (snapshots.Count == 0)
            {
                builder.AppendLine("None");
                return;
            }

            for (var i = 0; i < snapshots.Count; i++)
            {
                var snapshot = snapshots[i];
                builder.Append("- ").AppendLine(snapshot.Key);
                builder.Append("  Name = ").AppendLine(snapshot.Name);
                builder.Append("  ActiveSelf = ").AppendLine(snapshot.ActiveSelf.ToString());
                builder.Append("  RendererEnabled = ").AppendLine(snapshot.RendererEnabled.ToString());
                builder.Append("  Position = ").AppendLine(FormatVectorCtor(snapshot.Position));
                builder.Append("  Rotation = ").AppendLine(FormatQuaternionCtor(snapshot.Rotation));
                builder.Append("  Euler = ").AppendLine(FormatVectorCtor(snapshot.Rotation.eulerAngles));
                builder.Append("  Scale = ").AppendLine(FormatVectorCtor(snapshot.Scale));
                builder.Append("  Materials = ").AppendLine(string.Join(", ", snapshot.Materials));
            }
        }

        private static void AppendSceneChangeList(StringBuilder builder, List<SceneRendererChange> changes)
        {
            if (changes.Count == 0)
            {
                builder.AppendLine("None");
                return;
            }

            for (var i = 0; i < changes.Count; i++)
            {
                var change = changes[i];
                builder.Append("- ").AppendLine(change.Current.Key);
                builder.Append("  Current.activeSelf = ").AppendLine(change.Current.ActiveSelf.ToString());
                builder.Append("  Backup.activeSelf = ").AppendLine(change.Backup.ActiveSelf.ToString());
                builder.Append("  Current.rendererEnabled = ").AppendLine(change.Current.RendererEnabled.ToString());
                builder.Append("  Backup.rendererEnabled = ").AppendLine(change.Backup.RendererEnabled.ToString());
                builder.Append("  Current.position = ").AppendLine(FormatVectorCtor(change.Current.Position));
                builder.Append("  Backup.position = ").AppendLine(FormatVectorCtor(change.Backup.Position));
                builder.Append("  Current.rotation = ").AppendLine(FormatQuaternionCtor(change.Current.Rotation));
                builder.Append("  Backup.rotation = ").AppendLine(FormatQuaternionCtor(change.Backup.Rotation));
                builder.Append("  Current.euler = ").AppendLine(FormatVectorCtor(change.Current.Rotation.eulerAngles));
                builder.Append("  Backup.euler = ").AppendLine(FormatVectorCtor(change.Backup.Rotation.eulerAngles));
                builder.Append("  Current.scale = ").AppendLine(FormatVectorCtor(change.Current.Scale));
                builder.Append("  Backup.scale = ").AppendLine(FormatVectorCtor(change.Backup.Scale));
                builder.Append("  Current.materials = ").AppendLine(string.Join(", ", change.Current.Materials));
                builder.Append("  Backup.materials = ").AppendLine(string.Join(", ", change.Backup.Materials));
            }
        }

        private static void WriteSceneTransformBackupDiff(
            string outputPath,
            Dictionary<string, SceneTransformSnapshot> currentEntries,
            Dictionary<string, SceneTransformSnapshot> backupEntries)
        {
            var removed = new List<SceneTransformSnapshot>();
            var added = new List<SceneTransformSnapshot>();
            var changed = new List<SceneTransformChange>();

            foreach (var current in currentEntries)
            {
                if (!backupEntries.ContainsKey(current.Key))
                {
                    removed.Add(current.Value);
                }
            }

            foreach (var backup in backupEntries)
            {
                if (!currentEntries.TryGetValue(backup.Key, out var current))
                {
                    added.Add(backup.Value);
                    continue;
                }

                var hasChange =
                    current.ActiveSelf != backup.Value.ActiveSelf ||
                    current.ActiveInHierarchy != backup.Value.ActiveInHierarchy ||
                    Vector3.Distance(current.Position, backup.Value.Position) > 0.003f ||
                    Quaternion.Angle(current.Rotation, backup.Value.Rotation) > 0.05f ||
                    Vector3.Distance(current.Scale, backup.Value.Scale) > 0.003f;
                if (hasChange)
                {
                    changed.Add(new SceneTransformChange(current, backup.Value));
                }
            }

            removed.Sort((left, right) => string.Compare(left.Key, right.Key, StringComparison.Ordinal));
            added.Sort((left, right) => string.Compare(left.Key, right.Key, StringComparison.Ordinal));
            changed.Sort((left, right) => string.Compare(left.Current.Key, right.Current.Key, StringComparison.Ordinal));

            var builder = new StringBuilder();
            builder.AppendLine("# Scene Transform Backup Diff");
            builder.AppendLine("# Current = saved CargoRunMvp. Backup = preserved Unity scene recovery backup.");
            builder.AppendLine();
            builder.Append("CurrentTransformCount = ").AppendLine(currentEntries.Count.ToString());
            builder.Append("BackupTransformCount = ").AppendLine(backupEntries.Count.ToString());
            builder.Append("RemovedInBackupCount = ").AppendLine(removed.Count.ToString());
            builder.Append("AddedInBackupCount = ").AppendLine(added.Count.ToString());
            builder.Append("ChangedInBackupCount = ").AppendLine(changed.Count.ToString());
            builder.AppendLine();
            builder.AppendLine("[RemovedInBackup]");
            AppendTransformSnapshotList(builder, removed);
            builder.AppendLine();
            builder.AppendLine("[AddedInBackup]");
            AppendTransformSnapshotList(builder, added);
            builder.AppendLine();
            builder.AppendLine("[ChangedInBackup]");
            AppendTransformChangeList(builder, changed);

            File.WriteAllText(outputPath, builder.ToString(), new UTF8Encoding(false));
        }

        private static void AppendTransformSnapshotList(StringBuilder builder, List<SceneTransformSnapshot> snapshots)
        {
            if (snapshots.Count == 0)
            {
                builder.AppendLine("None");
                return;
            }

            for (var i = 0; i < snapshots.Count; i++)
            {
                var snapshot = snapshots[i];
                builder.Append("- ").AppendLine(snapshot.Key);
                builder.Append("  Name = ").AppendLine(snapshot.Name);
                builder.Append("  ActiveSelf = ").AppendLine(snapshot.ActiveSelf.ToString());
                builder.Append("  ActiveInHierarchy = ").AppendLine(snapshot.ActiveInHierarchy.ToString());
                builder.Append("  Position = ").AppendLine(FormatVectorCtor(snapshot.Position));
                builder.Append("  Rotation = ").AppendLine(FormatQuaternionCtor(snapshot.Rotation));
                builder.Append("  Euler = ").AppendLine(FormatVectorCtor(snapshot.Rotation.eulerAngles));
                builder.Append("  Scale = ").AppendLine(FormatVectorCtor(snapshot.Scale));
            }
        }

        private static void AppendTransformChangeList(StringBuilder builder, List<SceneTransformChange> changes)
        {
            if (changes.Count == 0)
            {
                builder.AppendLine("None");
                return;
            }

            for (var i = 0; i < changes.Count; i++)
            {
                var change = changes[i];
                builder.Append("- ").AppendLine(change.Current.Key);
                builder.Append("  Current.activeSelf = ").AppendLine(change.Current.ActiveSelf.ToString());
                builder.Append("  Backup.activeSelf = ").AppendLine(change.Backup.ActiveSelf.ToString());
                builder.Append("  Current.activeInHierarchy = ").AppendLine(change.Current.ActiveInHierarchy.ToString());
                builder.Append("  Backup.activeInHierarchy = ").AppendLine(change.Backup.ActiveInHierarchy.ToString());
                builder.Append("  Current.position = ").AppendLine(FormatVectorCtor(change.Current.Position));
                builder.Append("  Backup.position = ").AppendLine(FormatVectorCtor(change.Backup.Position));
                builder.Append("  Current.rotation = ").AppendLine(FormatQuaternionCtor(change.Current.Rotation));
                builder.Append("  Backup.rotation = ").AppendLine(FormatQuaternionCtor(change.Backup.Rotation));
                builder.Append("  Current.euler = ").AppendLine(FormatVectorCtor(change.Current.Rotation.eulerAngles));
                builder.Append("  Backup.euler = ").AppendLine(FormatVectorCtor(change.Backup.Rotation.eulerAngles));
                builder.Append("  Current.scale = ").AppendLine(FormatVectorCtor(change.Current.Scale));
                builder.Append("  Backup.scale = ").AppendLine(FormatVectorCtor(change.Backup.Scale));
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

            parts.Reverse();
            return string.Join("/", parts);
        }

        private static void CopyApprovedSourceFbx()
        {
            var projectRoot = Directory.GetParent(Application.dataPath);
            if (projectRoot == null)
            {
                throw new InvalidOperationException("Could not resolve project root for cockpit warning source FBX.");
            }

            var sourcePath = Path.Combine(projectRoot.FullName, SourceFbxRelativePath);
            if (!File.Exists(sourcePath))
            {
                throw new FileNotFoundException("Missing approved cockpit warning source FBX.", sourcePath);
            }

            var targetDirectory = Path.Combine(projectRoot.FullName, UnityAssetDirectory);
            Directory.CreateDirectory(targetDirectory);
            File.Copy(sourcePath, Path.Combine(projectRoot.FullName, UnityFbxPath), true);
            AssetDatabase.ImportAsset(UnityFbxPath, ImportAssetOptions.ForceUpdate);
        }

        private static WarningMaterials EnsureMaterials()
        {
            Directory.CreateDirectory(UnityAssetDirectory);
            var decalTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(DecalTexturePath);
            return new WarningMaterials(
                EnsureMaterial(BodyMaterialPath, new Color(0.105f, 0.118f, 0.112f, 1f), 0.42f, 0.34f, false, null),
                EnsureMaterial(DarkMaterialPath, new Color(0.012f, 0.013f, 0.012f, 1f), 0.15f, 0.18f, false, null),
                EnsureMaterial(RubberMaterialPath, new Color(0.006f, 0.006f, 0.005f, 1f), 0f, 0.08f, false, null),
                EnsureMaterial(RedMaterialPath, new Color(0.78f, 0.04f, 0.025f, 1f), 0f, 0.58f, true, null),
                EnsureMaterial(GlowMaterialPath, new Color(1f, 0.08f, 0.045f, 1f), 0f, 0.72f, true, null),
                EnsureMaterial(HazardMaterialPath, new Color(0.88f, 0.66f, 0.16f, 1f), 0.08f, 0.42f, false, null),
                EnsureMaterial(WearMaterialPath, new Color(0.56f, 0.55f, 0.50f, 1f), 0.55f, 0.30f, false, null),
                EnsureMaterial(DecalMaterialPath, Color.white, 0f, 0.36f, false, decalTexture));
        }

        private static Material EnsureMaterial(
            string path,
            Color color,
            float metallic,
            float smoothness,
            bool emissive,
            Texture2D mainTexture)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader != null && material.shader != shader)
                {
                    material.shader = shader;
                }
            }

            material.color = color;
            SetColor(material, "_BaseColor", color);
            SetColor(material, "_Color", color);
            SetFloat(material, "_Metallic", Mathf.Clamp01(metallic));
            SetFloat(material, "_Smoothness", Mathf.Clamp01(smoothness));
            SetFloat(material, "_Surface", 0f);
            SetTexture(material, "_BaseMap", mainTexture);
            SetTexture(material, "_MainTex", mainTexture);
            material.SetOverrideTag("RenderType", "Opaque");
            material.renderQueue = -1;
            material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");

            if (emissive)
            {
                material.EnableKeyword("_EMISSION");
                var emission = color * 1.55f;
                emission.a = 1f;
                SetColor(material, "_EmissionColor", emission);
            }
            else
            {
                material.DisableKeyword("_EMISSION");
                SetColor(material, "_EmissionColor", Color.black);
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static void ApplyApprovedMaterials(Transform root, WarningMaterials materials)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                var shared = renderer.sharedMaterials;
                var objectName = renderer.gameObject.name.ToLowerInvariant();
                for (var j = 0; j < shared.Length; j++)
                {
                    var sourceName = shared[j] != null ? shared[j].name.ToLowerInvariant() : string.Empty;
                    shared[j] = ResolveMaterial(sourceName, objectName, materials);
                }

                renderer.sharedMaterials = shared;
            }
        }

        private static List<StatusChange> CaptureEditorStatusChanges(Transform currentRoot)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(UnityFbxPath);
            if (source == null)
            {
                throw new InvalidOperationException("Approved cockpit warning source FBX is not imported: " + UnityFbxPath);
            }

            var expectedRoot = PrefabUtility.InstantiatePrefab(source) as GameObject;
            if (expectedRoot == null)
            {
                throw new InvalidOperationException("Approved cockpit warning source FBX could not be instantiated for status capture: " + UnityFbxPath);
            }

            try
            {
                expectedRoot.name = RootName + " Expected Status Capture";
                expectedRoot.hideFlags = HideFlags.HideAndDontSave;
                expectedRoot.transform.position = RootWorldPosition;
                expectedRoot.transform.rotation = Quaternion.identity;
                expectedRoot.transform.localScale = Vector3.one;

                RemovePreviewOnlyObjects(expectedRoot.transform);
                RemoveNonApprovedMeshes(expectedRoot.transform);
                PositionApprovedGroups(expectedRoot.transform);

                var expectedByName = CollectApprovedRenderersByName(expectedRoot.transform);
                var currentByName = CollectApprovedRenderersByName(currentRoot);
                var changes = new List<StatusChange>();
                foreach (var entry in expectedByName)
                {
                    if (!currentByName.TryGetValue(entry.Key, out var current))
                    {
                        continue;
                    }

                    var expected = entry.Value;
                    if (expected.gameObject.activeSelf == current.gameObject.activeSelf &&
                        expected.enabled == current.enabled)
                    {
                        continue;
                    }

                    changes.Add(
                        new StatusChange(
                            entry.Key,
                            expected.gameObject.activeSelf,
                            current.gameObject.activeSelf,
                            expected.enabled,
                            current.enabled));
                }

                changes.Sort((left, right) => string.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase));
                return changes;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(expectedRoot);
            }
        }

        private static List<MaterialChange> CaptureEditorMaterialChanges(Transform currentRoot)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(UnityFbxPath);
            if (source == null)
            {
                throw new InvalidOperationException("Approved cockpit warning source FBX is not imported: " + UnityFbxPath);
            }

            var sourceByName = CollectApprovedRenderersByName(source.transform);
            var currentByName = CollectApprovedRenderersByName(currentRoot);
            var changes = new List<MaterialChange>();
            foreach (var entry in sourceByName)
            {
                var lowerName = entry.Key.ToLowerInvariant();
                if (IsRemovedAfterEditorReviewObject(lowerName) ||
                    !currentByName.TryGetValue(entry.Key, out var current))
                {
                    continue;
                }

                var expectedMaterials = ResolveExpectedMaterialNames(entry.Value);
                var currentMaterials = CollectCurrentMaterialNames(current);
                if (HaveSameMaterialNames(expectedMaterials, currentMaterials))
                {
                    continue;
                }

                changes.Add(new MaterialChange(entry.Key, expectedMaterials, currentMaterials));
            }

            changes.Sort((left, right) => string.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase));
            return changes;
        }

        private static string[] ResolveExpectedMaterialNames(Renderer sourceRenderer)
        {
            var shared = sourceRenderer.sharedMaterials;
            var output = new string[shared.Length];
            var objectName = sourceRenderer.gameObject.name.ToLowerInvariant();
            for (var i = 0; i < shared.Length; i++)
            {
                var sourceName = shared[i] != null ? shared[i].name.ToLowerInvariant() : string.Empty;
                output[i] = ResolveMaterialAssetName(sourceName, objectName);
            }

            return output;
        }

        private static string[] CollectCurrentMaterialNames(Renderer renderer)
        {
            var shared = renderer.sharedMaterials;
            var output = new string[shared.Length];
            for (var i = 0; i < shared.Length; i++)
            {
                output[i] = shared[i] != null ? NormalizeMaterialName(shared[i].name) : string.Empty;
            }

            return output;
        }

        private static bool HaveSameMaterialNames(string[] expected, string[] current)
        {
            if (expected.Length != current.Length)
            {
                return false;
            }

            for (var i = 0; i < expected.Length; i++)
            {
                if (!expected[i].Equals(current[i], StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }

        private static Material ResolveMaterial(string sourceName, string objectName, WarningMaterials materials)
        {
            if (sourceName.Contains("projector_warning") ||
                sourceName.Contains("decal") ||
                objectName.Contains("decal") ||
                objectName.Contains("projected warning"))
            {
                return materials.Decal;
            }

            if (sourceName.Contains("glow") ||
                objectName.Contains("glow") ||
                objectName.Contains("soft glow"))
            {
                return materials.Glow;
            }

            if (sourceName.Contains("red") ||
                objectName.Contains("red") ||
                objectName.Contains("lamp") ||
                objectName.Contains("glass dome") ||
                objectName.Contains("lens"))
            {
                return materials.Red;
            }

            if (sourceName.Contains("hazard") ||
                objectName.Contains("hazard") ||
                objectName.Contains("warning stripe") ||
                objectName.Contains("warning decal"))
            {
                return materials.Hazard;
            }

            if (sourceName.Contains("rubber") ||
                objectName.Contains("rubber") ||
                objectName.Contains("cable") ||
                objectName.Contains("seal"))
            {
                return materials.Rubber;
            }

            if (sourceName.Contains("dark") ||
                objectName.Contains("baffle") ||
                objectName.Contains("grille slot") ||
                objectName.Contains("sounder grille"))
            {
                return materials.Dark;
            }

            if (sourceName.Contains("wear") ||
                objectName.Contains("rib") ||
                objectName.Contains("ring") ||
                objectName.Contains("spindle") ||
                objectName.Contains("plate") ||
                objectName.Contains("mount"))
            {
                return materials.Wear;
            }

            return materials.Body;
        }

        private static string ResolveMaterialAssetName(string sourceName, string objectName)
        {
            if (sourceName.Contains("projector_warning") ||
                sourceName.Contains("decal") ||
                objectName.Contains("decal") ||
                objectName.Contains("projected warning"))
            {
                return MaterialName(DecalMaterialPath);
            }

            if (sourceName.Contains("glow") ||
                objectName.Contains("glow") ||
                objectName.Contains("soft glow"))
            {
                return MaterialName(GlowMaterialPath);
            }

            if (sourceName.Contains("red") ||
                objectName.Contains("red") ||
                objectName.Contains("lamp") ||
                objectName.Contains("glass dome") ||
                objectName.Contains("lens"))
            {
                return MaterialName(RedMaterialPath);
            }

            if (sourceName.Contains("hazard") ||
                objectName.Contains("hazard") ||
                objectName.Contains("warning stripe") ||
                objectName.Contains("warning decal"))
            {
                return MaterialName(HazardMaterialPath);
            }

            if (sourceName.Contains("rubber") ||
                objectName.Contains("rubber") ||
                objectName.Contains("cable") ||
                objectName.Contains("seal"))
            {
                return MaterialName(RubberMaterialPath);
            }

            if (sourceName.Contains("dark") ||
                objectName.Contains("baffle") ||
                objectName.Contains("grille slot") ||
                objectName.Contains("sounder grille"))
            {
                return MaterialName(DarkMaterialPath);
            }

            if (sourceName.Contains("wear") ||
                objectName.Contains("rib") ||
                objectName.Contains("ring") ||
                objectName.Contains("spindle") ||
                objectName.Contains("plate") ||
                objectName.Contains("mount"))
            {
                return MaterialName(WearMaterialPath);
            }

            return MaterialName(BodyMaterialPath);
        }

        private static string MaterialName(string materialPath)
        {
            return Path.GetFileNameWithoutExtension(materialPath);
        }

        private static string NormalizeMaterialName(string materialName)
        {
            return materialName.Replace(" (Instance)", string.Empty);
        }

        private static List<string> CollectApprovedSourceRendererNames()
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(UnityFbxPath);
            if (source == null)
            {
                throw new InvalidOperationException("Approved cockpit warning source FBX is not imported: " + UnityFbxPath);
            }

            return CollectApprovedRendererNames(source.transform);
        }

        private static List<string> CollectApprovedRendererNames(Transform root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            var names = new List<string>();
            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                var lowerName = renderer.gameObject.name.ToLowerInvariant();
                if (IsRemovedAfterEditorReviewObject(lowerName))
                {
                    continue;
                }

                if (!IsScreenBarPart(lowerName) && !IsCeilingBeaconPart(lowerName))
                {
                    continue;
                }

                if (!names.Contains(renderer.gameObject.name))
                {
                    names.Add(renderer.gameObject.name);
                }
            }

            return names;
        }

        private static List<TransformChange> CaptureEditorTransformChanges(Transform currentRoot)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(UnityFbxPath);
            if (source == null)
            {
                throw new InvalidOperationException("Approved cockpit warning source FBX is not imported: " + UnityFbxPath);
            }

            var expectedRoot = PrefabUtility.InstantiatePrefab(source) as GameObject;
            if (expectedRoot == null)
            {
                throw new InvalidOperationException("Approved cockpit warning source FBX could not be instantiated for current object capture: " + UnityFbxPath);
            }

            try
            {
                expectedRoot.name = RootName + " Expected Capture";
                expectedRoot.hideFlags = HideFlags.HideAndDontSave;
                expectedRoot.transform.position = RootWorldPosition;
                expectedRoot.transform.rotation = Quaternion.identity;
                expectedRoot.transform.localScale = Vector3.one;

                RemovePreviewOnlyObjects(expectedRoot.transform);
                RemoveNonApprovedMeshes(expectedRoot.transform);
                PositionApprovedGroups(expectedRoot.transform);

                var expectedByName = CollectApprovedRenderersByName(expectedRoot.transform);
                var currentByName = CollectApprovedRenderersByName(currentRoot);
                var changes = new List<TransformChange>();
                foreach (var entry in expectedByName)
                {
                    if (!currentByName.TryGetValue(entry.Key, out var current))
                    {
                        continue;
                    }

                    var expected = entry.Value.transform;
                    var currentTransform = current.transform;
                    if (!HasTransformDifference(expected, currentTransform))
                    {
                        continue;
                    }

                    changes.Add(
                        new TransformChange(
                            entry.Key,
                            expected.position,
                            currentTransform.position,
                            expected.rotation,
                            currentTransform.rotation,
                            expected.lossyScale,
                            currentTransform.lossyScale));
                }

                changes.Sort((left, right) => string.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase));
                return changes;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(expectedRoot);
            }
        }

        private static Dictionary<string, Renderer> CollectApprovedRenderersByName(Transform root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            var output = new Dictionary<string, Renderer>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                var lowerName = renderer.gameObject.name.ToLowerInvariant();
                if (IsRemovedAfterEditorReviewObject(lowerName) ||
                    (!IsScreenBarPart(lowerName) &&
                     !IsCeilingBeaconPart(lowerName)))
                {
                    continue;
                }

                output[renderer.gameObject.name] = renderer;
            }

            return output;
        }

        private static bool HasTransformDifference(Transform expected, Transform current)
        {
            return Vector3.Distance(expected.position, current.position) > 0.003f ||
                   Quaternion.Angle(expected.rotation, current.rotation) > 0.05f ||
                   Vector3.Distance(expected.lossyScale, current.lossyScale) > 0.003f;
        }

        private static void AppendNameList(StringBuilder builder, List<string> names)
        {
            if (names.Count == 0)
            {
                builder.AppendLine("None");
                return;
            }

            for (var i = 0; i < names.Count; i++)
            {
                builder.Append("- ").AppendLine(names[i]);
            }
        }

        private static void AppendTransformChanges(StringBuilder builder, List<TransformChange> changes)
        {
            if (changes.Count == 0)
            {
                builder.AppendLine("None");
                return;
            }

            for (var i = 0; i < changes.Count; i++)
            {
                var change = changes[i];
                builder.Append("- ").AppendLine(change.Name);
                builder.Append("  Expected.position = ").AppendLine(FormatVectorCtor(change.ExpectedPosition));
                builder.Append("  Current.position = ").AppendLine(FormatVectorCtor(change.CurrentPosition));
                builder.Append("  Expected.rotation = ").AppendLine(FormatQuaternionCtor(change.ExpectedRotation));
                builder.Append("  Current.rotation = ").AppendLine(FormatQuaternionCtor(change.CurrentRotation));
                builder.Append("  Expected.euler = ").AppendLine(FormatVectorCtor(change.ExpectedRotation.eulerAngles));
                builder.Append("  Current.euler = ").AppendLine(FormatVectorCtor(change.CurrentRotation.eulerAngles));
                builder.Append("  Expected.scale = ").AppendLine(FormatVectorCtor(change.ExpectedScale));
                builder.Append("  Current.scale = ").AppendLine(FormatVectorCtor(change.CurrentScale));
            }
        }

        private static void AppendStatusChanges(StringBuilder builder, List<StatusChange> changes)
        {
            if (changes.Count == 0)
            {
                builder.AppendLine("None");
                return;
            }

            for (var i = 0; i < changes.Count; i++)
            {
                var change = changes[i];
                builder.Append("- ").AppendLine(change.Name);
                builder.Append("  Expected.activeSelf = ").AppendLine(change.ExpectedActiveSelf.ToString());
                builder.Append("  Current.activeSelf = ").AppendLine(change.CurrentActiveSelf.ToString());
                builder.Append("  Expected.rendererEnabled = ").AppendLine(change.ExpectedRendererEnabled.ToString());
                builder.Append("  Current.rendererEnabled = ").AppendLine(change.CurrentRendererEnabled.ToString());
            }
        }

        private static void AppendMaterialChanges(StringBuilder builder, List<MaterialChange> changes)
        {
            if (changes.Count == 0)
            {
                builder.AppendLine("None");
                return;
            }

            for (var i = 0; i < changes.Count; i++)
            {
                var change = changes[i];
                builder.Append("- ").AppendLine(change.Name);
                builder.Append("  Expected.materials = ").AppendLine(string.Join(", ", change.ExpectedMaterials));
                builder.Append("  Current.materials = ").AppendLine(string.Join(", ", change.CurrentMaterials));
            }
        }

        private static void RemovePreviewOnlyObjects(Transform root)
        {
            var targets = root.GetComponentsInChildren<Transform>(true);
            for (var i = targets.Length - 1; i >= 0; i--)
            {
                var target = targets[i];
                if (target == root || target == null)
                {
                    continue;
                }

                var lowerName = target.name.ToLowerInvariant();
                if (lowerName.Contains("context") ||
                    lowerName.Contains("proxy") ||
                    lowerName.Contains("cockpit floor footprint") ||
                    lowerName.Contains("front broad screen") ||
                    lowerName.Contains("front lower sill") ||
                    lowerName.Contains("front upper frame proxy") ||
                    lowerName.Contains("cockpit wall") ||
                    lowerName.Contains("cockpit ceiling proxy") ||
                    target.GetComponent<Camera>() != null ||
                    target.GetComponent<Light>() != null)
                {
                    UnityEngine.Object.DestroyImmediate(target.gameObject);
                }
            }
        }

        private static void RemoveNonApprovedMeshes(Transform root)
        {
            var targets = root.GetComponentsInChildren<Transform>(true);
            for (var i = targets.Length - 1; i >= 0; i--)
            {
                var target = targets[i];
                if (target == root || target == null)
                {
                    continue;
                }

                var lowerName = target.name.ToLowerInvariant();
                if (target.GetComponent<Renderer>() != null &&
                    (IsRemovedAfterEditorReviewObject(lowerName) ||
                     (!IsScreenBarPart(lowerName) &&
                      !IsCeilingBeaconPart(lowerName))))
                {
                    UnityEngine.Object.DestroyImmediate(target.gameObject);
                }
            }
        }

        private static void PositionApprovedGroups(Transform root)
        {
            MoveRendererGroup(root, IsScreenBarPart, ScreenBarTargetCenter, "screen-top alarm bar");
            MoveRendererGroup(root, IsCeilingBeaconPart, CeilingBeaconTargetCenter, "central ceiling warning beacon");
        }

        private static void MoveRendererGroup(Transform root, Func<string, bool> matcher, Vector3 targetCenter, string label)
        {
            var renderers = CollectRenderers(root, matcher);
            if (renderers.Count == 0)
            {
                throw new InvalidOperationException("No renderers found for approved cockpit warning group: " + label);
            }

            var bounds = GetRendererBounds(renderers);
            var delta = targetCenter - bounds.center;
            var topTransforms = CollectTopTransforms(renderers);
            for (var i = 0; i < topTransforms.Count; i++)
            {
                topTransforms[i].position += delta;
            }
        }

        private static List<Renderer> CollectRenderers(Transform root, Func<string, bool> matcher)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            var output = new List<Renderer>();
            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null || !renderer.enabled)
                {
                    continue;
                }

                var lowerName = renderer.gameObject.name.ToLowerInvariant();
                if (IsRemovedAfterEditorReviewObject(lowerName))
                {
                    continue;
                }

                if (matcher(lowerName))
                {
                    output.Add(renderer);
                }
            }

            return output;
        }

        private static List<Transform> CollectTopTransforms(List<Renderer> renderers)
        {
            var allTransforms = new List<Transform>();
            for (var i = 0; i < renderers.Count; i++)
            {
                if (!allTransforms.Contains(renderers[i].transform))
                {
                    allTransforms.Add(renderers[i].transform);
                }
            }

            var topTransforms = new List<Transform>();
            for (var i = 0; i < allTransforms.Count; i++)
            {
                var candidate = allTransforms[i];
                var ancestor = candidate.parent;
                var hasGroupAncestor = false;
                while (ancestor != null)
                {
                    if (allTransforms.Contains(ancestor))
                    {
                        hasGroupAncestor = true;
                        break;
                    }

                    ancestor = ancestor.parent;
                }

                if (!hasGroupAncestor)
                {
                    topTransforms.Add(candidate);
                }
            }

            return topTransforms;
        }

        private static void ValidateNoPreviewOrRejectedObjects(Transform root)
        {
            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                var lowerName = transforms[i].name.ToLowerInvariant();
                if (IsRemovedAfterEditorReviewObject(lowerName) ||
                    lowerName.Contains("context") ||
                    lowerName.Contains("proxy") ||
                    lowerName.Contains("left cockpit wall") ||
                    lowerName.Contains("right cockpit wall") ||
                    lowerName.Contains("side warning") ||
                    lowerName.Contains("wall warning") ||
                    lowerName.Contains("status panel") ||
                    lowerName.Contains("auto manual") ||
                    lowerName.Contains("autopilot"))
                {
                    throw new InvalidOperationException("Approved cockpit warning contains a rejected preview/detail object: " + transforms[i].name);
                }
            }
        }

        private static void ValidateGroupPlacement(Transform root)
        {
            var screenBarRenderers = CollectRenderers(root, IsScreenBarPart);
            var ceilingRenderers = CollectRenderers(root, IsCeilingBeaconPart);
            var screenBounds = GetRendererBounds(screenBarRenderers);
            var ceilingBounds = GetRendererBounds(ceilingRenderers);

            var screenDelta = Vector3.Distance(screenBounds.center, ScreenBarTargetCenter);
            if (screenDelta > 0.08f)
            {
                throw new InvalidOperationException("Approved cockpit warning screen-top bar is not placed above the approved front screen. Delta=" + screenDelta.ToString("0.000"));
            }

            var ceilingDelta = Vector3.Distance(ceilingBounds.center, CeilingBeaconTargetCenter);
            if (ceilingDelta > 0.08f)
            {
                throw new InvalidOperationException("Approved cockpit warning ceiling beacon is not placed at the cockpit ceiling center. Delta=" + ceilingDelta.ToString("0.000"));
            }

            if (screenBounds.size.x < 2.5f || screenBounds.size.x > 4.2f ||
                screenBounds.size.y < 0.08f || screenBounds.size.y > 0.8f ||
                screenBounds.size.z < 0.04f || screenBounds.size.z > 0.9f)
            {
                throw new InvalidOperationException("Approved cockpit warning screen-top bar bounds are outside the approved sample scale. Size=" + FormatVector(screenBounds.size));
            }

            if (ceilingBounds.size.x < 0.55f || ceilingBounds.size.x > 1.55f ||
                ceilingBounds.size.y < 0.35f || ceilingBounds.size.y > 1.25f ||
                ceilingBounds.size.z < 0.55f || ceilingBounds.size.z > 1.55f)
            {
                throw new InvalidOperationException("Approved cockpit warning ceiling beacon bounds are outside the approved sample scale. Size=" + FormatVector(ceilingBounds.size));
            }
        }

        private static bool IsScreenBarPart(string lowerName)
        {
            return lowerName.Contains("upper frame narrow red alarm bar") ||
                   lowerName.Contains("upper frame red alarm bar") ||
                   lowerName.Contains("upper alarm bar") ||
                   lowerName.Contains("front alarm bar") ||
                   lowerName.Contains("small projected warning decal");
        }

        private static bool IsCeilingBeaconPart(string lowerName)
        {
            return lowerName.Contains("central ceiling emergency rotary beacon");
        }

        private static bool IsRemovedAfterEditorReviewObject(string lowerName)
        {
            for (var i = 0; i < RemovedAfterEditorReviewObjectNames.Length; i++)
            {
                if (lowerName.Equals(RemovedAfterEditorReviewObjectNames[i], StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static Bounds GetRendererBounds(List<Renderer> renderers)
        {
            if (renderers.Count == 0)
            {
                throw new InvalidOperationException("No renderers were supplied for cockpit warning bounds.");
            }

            var hasBounds = false;
            var bounds = new Bounds();
            for (var i = 0; i < renderers.Count; i++)
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

            if (!hasBounds)
            {
                throw new InvalidOperationException("No enabled renderers were supplied for cockpit warning bounds.");
            }

            return bounds;
        }

        private static void DisableAllColliders(Transform root)
        {
            var colliders = root.GetComponentsInChildren<Collider>(true);
            for (var i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = false;
            }
        }

        private static void CaptureAppliedView(
            string path,
            Vector3 cameraPosition,
            Vector3 lookAt,
            float fieldOfView,
            bool orthographic,
            float orthographicSize,
            Vector3 cameraUp)
        {
            var cameraObject = new GameObject("Approved Cockpit Warning Comparison Camera")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            var keyLightObject = new GameObject("Approved Cockpit Warning Comparison Key Light")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            var fillLightObject = new GameObject("Approved Cockpit Warning Comparison Fill Light")
            {
                hideFlags = HideFlags.HideAndDontSave
            };

            try
            {
                keyLightObject.transform.position = CockpitCenter + new Vector3(0f, 4.8f, 0.9f);
                var keyLight = keyLightObject.AddComponent<Light>();
                keyLight.type = LightType.Rectangle;
                keyLight.color = new Color(1f, 0.92f, 0.82f, 1f);
                keyLight.intensity = 430f;
                keyLight.range = 12f;
                keyLight.areaSize = new Vector2(5.8f, 5.8f);

                fillLightObject.transform.position = CockpitCenter + new Vector3(-2.4f, 2.1f, -2.4f);
                var fillLight = fillLightObject.AddComponent<Light>();
                fillLight.type = LightType.Point;
                fillLight.color = new Color(0.55f, 0.62f, 0.68f, 1f);
                fillLight.intensity = 85f;
                fillLight.range = 9f;

                var camera = cameraObject.AddComponent<Camera>();
                camera.transform.position = cameraPosition;
                camera.transform.LookAt(lookAt, cameraUp);
                camera.fieldOfView = fieldOfView;
                camera.orthographic = orthographic;
                camera.orthographicSize = orthographicSize;
                camera.nearClipPlane = 0.02f;
                camera.farClipPlane = 100f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.014f, 0.015f, 0.016f, 1f);
                camera.allowHDR = false;
                camera.allowMSAA = true;
                CaptureCamera(camera, path, 1600, 1000);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(keyLightObject);
                UnityEngine.Object.DestroyImmediate(fillLightObject);
            }
        }

        private static void CaptureCamera(Camera camera, string path, int width, int height)
        {
            var previousTargetTexture = camera.targetTexture;
            var previousActiveTexture = RenderTexture.active;
            var renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);

            try
            {
                camera.targetTexture = renderTexture;
                camera.Render();
                RenderTexture.active = renderTexture;
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTargetTexture;
                RenderTexture.active = previousActiveTexture;
                UnityEngine.Object.DestroyImmediate(renderTexture);
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static void WriteComparisonIndex(string outputRoot)
        {
            var builder = new StringBuilder();
            builder.AppendLine("<!doctype html>");
            builder.AppendLine("<html lang=\"ko\">");
            builder.AppendLine("<head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width, initial-scale=1\"><title>ck_warn04 Unity 적용 비교</title>");
            builder.AppendLine("<style>body{margin:0;background:#111514;color:#ece5d8;font-family:Arial,sans-serif}main{max-width:1400px;margin:0 auto;padding:24px}h1{font-size:27px;margin:0 0 8px}.meta{color:#cfc6b8;margin:0 0 18px}.grid{display:grid;gap:18px}.pair{display:grid;grid-template-columns:1fr 1fr;gap:12px;border:1px solid #3c4643;background:#1c2220;border-radius:6px;padding:12px}.pair h2{grid-column:1/-1;font-size:18px;margin:0}.pair img{display:block;width:100%;height:auto;background:#050807}.label{font-size:13px;color:#ddd3c3;margin:6px 0 0}@media(max-width:900px){.pair{grid-template-columns:1fr}}</style>");
            builder.AppendLine("</head><body><main>");
            builder.AppendLine("<h1>ck_warn04 Unity 적용 비교</h1>");
            builder.AppendLine("<p class=\"meta\">왼쪽은 승인된 Blender artSample 렌더이고, 오른쪽은 CargoRunMvp 조종실에 배치한 Unity 캡처입니다. 좌우 벽면 경고등은 제외했고, 천장 경고등은 조종실 천장 중앙에 맞췄습니다.</p>");
            builder.AppendLine("<section class=\"grid\">");
            AddComparisonPair(builder, "01 정면", "../renders/01_front.png", "unity_01_front.png");
            AddComparisonPair(builder, "02 플레이어 시점", "../renders/02_player.png", "unity_02_player.png");
            AddComparisonPair(builder, "03 측면", "../renders/03_side.png", "unity_03_side.png");
            AddComparisonPair(builder, "04 상단", "../renders/04_top.png", "unity_04_top.png");
            AddComparisonPair(builder, "05 스크린 상단 상세", "../renders/05_detail.png", "unity_05_detail.png");
            AddComparisonPair(builder, "06 천장 경고등 상세", "../renders/06_ceiling.png", "unity_06_ceiling.png");
            builder.AppendLine("</section></main></body></html>");
            File.WriteAllText(Path.Combine(outputRoot, "index.html"), builder.ToString(), new UTF8Encoding(false));
        }

        private static void AddComparisonPair(StringBuilder builder, string title, string approvedPath, string appliedPath)
        {
            builder.AppendLine("<article class=\"pair\">");
            builder.Append("<h2>").Append(title).AppendLine("</h2>");
            builder.Append("<div><a href=\"").Append(approvedPath).Append("\"><img src=\"").Append(approvedPath).Append("\" alt=\"승인 artSample\"></a><p class=\"label\">승인 artSample</p></div>");
            builder.AppendLine();
            builder.Append("<div><a href=\"").Append(appliedPath).Append("\"><img src=\"").Append(appliedPath).Append("\" alt=\"Unity 적용 결과\"></a><p class=\"label\">Unity 적용 결과</p></div>");
            builder.AppendLine();
            builder.AppendLine("</article>");
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

        private static void DeleteGeneratedObject(string objectName)
        {
            var existing = FindNamedObject(objectName);
            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing);
            }
        }

        private static int CountEnabledColliders(Transform root)
        {
            var count = 0;
            var colliders = root.GetComponentsInChildren<Collider>(true);
            for (var i = 0; i < colliders.Length; i++)
            {
                if (colliders[i].enabled)
                {
                    count++;
                }
            }

            return count;
        }

        private static void SetColor(Material material, string property, Color color)
        {
            if (material.HasProperty(property))
            {
                material.SetColor(property, color);
            }
        }

        private static void SetFloat(Material material, string property, float value)
        {
            if (material.HasProperty(property))
            {
                material.SetFloat(property, value);
            }
        }

        private static void SetTexture(Material material, string property, Texture texture)
        {
            if (material.HasProperty(property))
            {
                material.SetTexture(property, texture);
            }
        }

        private static string FormatVector(Vector3 value)
        {
            return value.x.ToString("0.00") + "," + value.y.ToString("0.00") + "," + value.z.ToString("0.00");
        }

        private static string FormatVectorCtor(Vector3 value)
        {
            return "new Vector3(" +
                   FormatFloat(value.x) +
                   "f, " +
                   FormatFloat(value.y) +
                   "f, " +
                   FormatFloat(value.z) +
                   "f)";
        }

        private static string FormatQuaternionCtor(Quaternion value)
        {
            return "new Quaternion(" +
                   FormatFloat(value.x) +
                   "f, " +
                   FormatFloat(value.y) +
                   "f, " +
                   FormatFloat(value.z) +
                   "f, " +
                   FormatFloat(value.w) +
                   "f)";
        }

        private static string FormatFloat(float value)
        {
            if (Mathf.Abs(value) < 0.0000005f)
            {
                value = 0f;
            }

            return value.ToString("0.######", CultureInfo.InvariantCulture);
        }

        private readonly struct TransformChange
        {
            public TransformChange(
                string name,
                Vector3 expectedPosition,
                Vector3 currentPosition,
                Quaternion expectedRotation,
                Quaternion currentRotation,
                Vector3 expectedScale,
                Vector3 currentScale)
            {
                Name = name;
                ExpectedPosition = expectedPosition;
                CurrentPosition = currentPosition;
                ExpectedRotation = expectedRotation;
                CurrentRotation = currentRotation;
                ExpectedScale = expectedScale;
                CurrentScale = currentScale;
            }

            public string Name { get; }
            public Vector3 ExpectedPosition { get; }
            public Vector3 CurrentPosition { get; }
            public Quaternion ExpectedRotation { get; }
            public Quaternion CurrentRotation { get; }
            public Vector3 ExpectedScale { get; }
            public Vector3 CurrentScale { get; }
        }

        private readonly struct StatusChange
        {
            public StatusChange(
                string name,
                bool expectedActiveSelf,
                bool currentActiveSelf,
                bool expectedRendererEnabled,
                bool currentRendererEnabled)
            {
                Name = name;
                ExpectedActiveSelf = expectedActiveSelf;
                CurrentActiveSelf = currentActiveSelf;
                ExpectedRendererEnabled = expectedRendererEnabled;
                CurrentRendererEnabled = currentRendererEnabled;
            }

            public string Name { get; }
            public bool ExpectedActiveSelf { get; }
            public bool CurrentActiveSelf { get; }
            public bool ExpectedRendererEnabled { get; }
            public bool CurrentRendererEnabled { get; }
        }

        private readonly struct MaterialChange
        {
            public MaterialChange(string name, string[] expectedMaterials, string[] currentMaterials)
            {
                Name = name;
                ExpectedMaterials = expectedMaterials;
                CurrentMaterials = currentMaterials;
            }

            public string Name { get; }
            public string[] ExpectedMaterials { get; }
            public string[] CurrentMaterials { get; }
        }

        private readonly struct SceneRendererSnapshot
        {
            public SceneRendererSnapshot(
                string key,
                string name,
                bool exists,
                bool activeSelf,
                bool rendererEnabled,
                Vector3 position,
                Quaternion rotation,
                Vector3 scale,
                string[] materials)
            {
                Key = key;
                Name = name;
                Exists = exists;
                ActiveSelf = activeSelf;
                RendererEnabled = rendererEnabled;
                Position = position;
                Rotation = rotation;
                Scale = scale;
                Materials = materials;
            }

            public string Key { get; }
            public string Name { get; }
            public bool Exists { get; }
            public bool ActiveSelf { get; }
            public bool RendererEnabled { get; }
            public Vector3 Position { get; }
            public Quaternion Rotation { get; }
            public Vector3 Scale { get; }
            public string[] Materials { get; }
        }

        private readonly struct SceneRendererChange
        {
            public SceneRendererChange(SceneRendererSnapshot current, SceneRendererSnapshot backup)
            {
                Current = current;
                Backup = backup;
            }

            public SceneRendererSnapshot Current { get; }
            public SceneRendererSnapshot Backup { get; }
        }

        private readonly struct SceneTransformSnapshot
        {
            public SceneTransformSnapshot(
                string key,
                string name,
                bool activeSelf,
                bool activeInHierarchy,
                Vector3 position,
                Quaternion rotation,
                Vector3 scale)
            {
                Key = key;
                Name = name;
                ActiveSelf = activeSelf;
                ActiveInHierarchy = activeInHierarchy;
                Position = position;
                Rotation = rotation;
                Scale = scale;
            }

            public string Key { get; }
            public string Name { get; }
            public bool ActiveSelf { get; }
            public bool ActiveInHierarchy { get; }
            public Vector3 Position { get; }
            public Quaternion Rotation { get; }
            public Vector3 Scale { get; }
        }

        private readonly struct SceneTransformChange
        {
            public SceneTransformChange(SceneTransformSnapshot current, SceneTransformSnapshot backup)
            {
                Current = current;
                Backup = backup;
            }

            public SceneTransformSnapshot Current { get; }
            public SceneTransformSnapshot Backup { get; }
        }

        private readonly struct WarningMaterials
        {
            public WarningMaterials(
                Material body,
                Material dark,
                Material rubber,
                Material red,
                Material glow,
                Material hazard,
                Material wear,
                Material decal)
            {
                Body = body;
                Dark = dark;
                Rubber = rubber;
                Red = red;
                Glow = glow;
                Hazard = hazard;
                Wear = wear;
                Decal = decal;
            }

            public Material Body { get; }
            public Material Dark { get; }
            public Material Rubber { get; }
            public Material Red { get; }
            public Material Glow { get; }
            public Material Hazard { get; }
            public Material Wear { get; }
            public Material Decal { get; }
        }
    }
}
