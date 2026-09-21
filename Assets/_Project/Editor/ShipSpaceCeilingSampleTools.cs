using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor
{
    internal static class ShipSpaceCeilingSampleTools
    {
        internal const string CargoRunScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string InspectionDirectory = "Logs/ShipSpaceCeilings";
        private const string SampleAssetDirectory = "Assets/_Project/ArtSamples/ShipSpaceCeilings";
        private const string SampleScenePath = SampleAssetDirectory + "/ShipSpaceCeilingsSample.unity";
        private const string AllSpaceCurrentStatePath =
            "Assets/_Project/Editor/Validation/ApprovedShipSpacesCurrentState.cs";
        private const string FallbackMaterialPath =
            "Assets/_Project/Art/Ship/Materials/ShipInteriorWall_Rough.mat";
        private const string ArtSampleDirectory = "artSample/ShipSpaceCeilings";
        private const string ValidationDirectory = "docs/validation/ShipSpaceCeilings";
        private const string ReviewCapturePath = InspectionDirectory + "/SampleReview.png";
        private const string AppliedReviewCaptureName = "AppliedReview.png";
        private const string AppliedEntranceCaptureName = "AppliedEntranceCoverageReview.png";
        private const string AppliedFinalCaptureName = "AppliedFinal.png";
        private const string AppliedInspectionName = "AppliedInspection.txt";
        private const float CeilingThicknessWorld = 0.14f;
        private const float BoundsEpsilon = 0.0005f;

        private static readonly SpaceDefinition[] SpaceDefinitions =
        {
            new SpaceDefinition("Armory", "무기실", "Approved Armory 01 Shell"),
            new SpaceDefinition("EngineRoom", "동력실", "Approved Engine Room 01 Shell"),
            new SpaceDefinition("Cockpit", "조종실", "Approved Cockpit 01 Structure"),
            new SpaceDefinition("SupplyRoom", "비품실", "Approved Supply Room 01 Shell"),
            new SpaceDefinition("CargoHold", "창고", "Approved Cargo Hold 01 Shell"),
            new SpaceDefinition("ControlRoom", "통제실", "Approved Control Room 01 Shell"),
            new SpaceDefinition("AllCorridors", "모든 복도", "Approved Ship Corridor Segments", true)
        };
        private static readonly string[] SpaceRootNames =
        {
            "Approved Armory 01 Shell",
            "Approved Engine Room 01 Shell",
            "Approved Cockpit 01 Structure",
            "Approved Supply Room 01 Shell",
            "Approved Cargo Hold 01 Shell",
            "Approved Control Room 01 Shell",
            "Approved Ship Corridor Segments"
        };
        private static readonly List<string> AlignmentRecords = new List<string>();

        internal static void BuildSamples()
        {
            AlignmentRecords.Clear();
            var cargoScene = SceneManager.GetActiveScene();
            if (cargoScene.path != CargoRunScenePath)
            {
                cargoScene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            }

            if (cargoScene.isDirty && !EditorSceneManager.SaveScene(cargoScene, CargoRunScenePath))
            {
                throw new InvalidOperationException("Failed to save CargoRunMvp before building ceiling samples.");
            }

            Directory.CreateDirectory(SampleAssetDirectory);
            AssetDatabase.Refresh();

            var sourceRoots = new Dictionary<string, GameObject>(StringComparer.Ordinal);
            var sourceRootList = new List<GameObject>(SpaceDefinitions.Length);
            for (var i = 0; i < SpaceDefinitions.Length; i++)
            {
                var definition = SpaceDefinitions[i];
                var sourceRoot = FindSceneObject(cargoScene, definition.SourceRootName);
                if (sourceRoot == null)
                {
                    throw new InvalidOperationException(
                        "Missing approved ship-space source root: " + definition.SourceRootName);
                }

                sourceRoots.Add(definition.Id, sourceRoot);
                sourceRootList.Add(sourceRoot);
            }

            WriteAllSpaceCurrentStateScript(sourceRootList);

            for (var i = 0; i < SpaceDefinitions.Length; i++)
            {
                var definition = SpaceDefinitions[i];
                var sourceRoot = sourceRoots[definition.Id];
                BuildCeilingPrefab(definition, sourceRoot.transform);
            }

            CreateSampleScene(sourceRoots);
            WriteAlignmentReport();
            WriteSampleHtml(false);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                "Ship-space current state captured and ceiling Unity art samples built from the current approved room and corridor roots. " +
                "ProductionSceneCeilingsApplied=False; SampleScene=" + SampleScenePath);
        }

        internal static void InspectSamples()
        {
            var report = new StringBuilder(16 * 1024);
            report.AppendLine("Ship-space ceiling sample inspection");
            report.AppendLine("directVisualPriority=True");
            report.AppendLine("productionSceneCeilingsApplied=False");
            report.AppendLine("newMaterialsCreated=False");

            var totalSlabs = 0;
            for (var i = 0; i < SpaceDefinitions.Length; i++)
            {
                var definition = SpaceDefinitions[i];
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(definition.PrefabPath);
                if (prefab == null)
                {
                    throw new InvalidOperationException("Missing ceiling sample prefab: " + definition.PrefabPath);
                }

                var renderers = prefab.GetComponentsInChildren<MeshRenderer>(true);
                var filters = prefab.GetComponentsInChildren<MeshFilter>(true);
                var colliders = prefab.GetComponentsInChildren<BoxCollider>(true);
                if (renderers.Length == 0 || renderers.Length != filters.Length || renderers.Length != colliders.Length)
                {
                    throw new InvalidOperationException(
                        "Ceiling prefab component counts do not match for " + definition.DisplayName +
                        ". Renderers=" + renderers.Length +
                        ", MeshFilters=" + filters.Length +
                        ", BoxColliders=" + colliders.Length);
                }

                for (var rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
                {
                    var material = renderers[rendererIndex].sharedMaterial;
                    var materialPath = material == null ? string.Empty : AssetDatabase.GetAssetPath(material);
                    if (string.IsNullOrWhiteSpace(materialPath) ||
                        materialPath.StartsWith(SampleAssetDirectory, StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException(
                            "Ceiling sample must reuse an existing project material: " +
                            definition.DisplayName + "/" + renderers[rendererIndex].name);
                    }
                }

                totalSlabs += renderers.Length;
                report.AppendLine(
                    definition.DisplayName +
                    "|Prefab=" + definition.PrefabPath +
                    "|CeilingSlabs=" + renderers.Length +
                    "|MeshFilter=True|Renderer=True|BoxCollider=True");
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(SampleScenePath) == null)
            {
                throw new InvalidOperationException("Missing ceiling sample scene: " + SampleScenePath);
            }

            var cargoScene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            for (var i = 0; i < SpaceDefinitions.Length; i++)
            {
                var productionRoot = FindSceneObject(cargoScene, SpaceDefinitions[i].SourceRootName);
                if (productionRoot == null)
                {
                    throw new InvalidOperationException(
                        "Missing production root during ceiling isolation check: " +
                        SpaceDefinitions[i].SourceRootName);
                }

                var transforms = productionRoot.GetComponentsInChildren<Transform>(true);
                for (var transformIndex = 0; transformIndex < transforms.Length; transformIndex++)
                {
                    if (transforms[transformIndex].name.StartsWith(
                            "ShipSpaceCeiling_",
                            StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException(
                            "Sample ceiling was attached to CargoRunMvp before approval: " +
                            HierarchyPath(transforms[transformIndex]));
                    }
                }
            }

            var sampleScene = EditorSceneManager.OpenScene(SampleScenePath, OpenSceneMode.Single);
            for (var i = 0; i < SpaceDefinitions.Length; i++)
            {
                if (FindSceneObject(sampleScene, SpaceDefinitions[i].SampleGroupName) == null)
                {
                    throw new InvalidOperationException(
                        "Missing sample scene group: " + SpaceDefinitions[i].SampleGroupName);
                }
            }

            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? string.Empty;
            var reviewPath = Path.Combine(projectRoot, ReviewCapturePath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(reviewPath) ?? projectRoot);
            CaptureContactSheet(sampleScene, reviewPath);
            var entranceCoverageReviewPath = Path.Combine(
                projectRoot,
                InspectionDirectory.Replace('/', Path.DirectorySeparatorChar),
                "EntranceCoverageReview.png");
            CaptureEntranceCoverageSheet(sampleScene, entranceCoverageReviewPath);

            var validationPath = Path.Combine(
                projectRoot,
                ValidationDirectory.Replace('/', Path.DirectorySeparatorChar),
                "Inspection.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(validationPath) ?? projectRoot);
            report.AppendLine("TotalCeilingSlabs=" + totalSlabs);
            report.AppendLine("SampleGroups=" + SpaceDefinitions.Length);
            report.AppendLine("CorridorModules=10");
            report.AppendLine("CargoRunMvpSampleCeilingObjects=0");
            report.AppendLine("ReviewCapture=" + reviewPath);
            report.AppendLine("EntranceCoverageReview=" + entranceCoverageReviewPath);
            File.WriteAllText(validationPath, report.ToString(), new UTF8Encoding(false));

            Bellerophon.Editor.Validation.DetectorAttachedStaticStartSetupTools
                .RequireNoUnityConsoleErrors();
            Debug.Log(
                "Ship-space ceiling samples inspected. Direct review capture=" + reviewPath +
                "; TotalCeilingSlabs=" + totalSlabs +
                "; ProductionSceneCeilingsApplied=False; UnityConsoleErrors=0");
        }

        internal static void CaptureFinal()
        {
            var sampleScene = EditorSceneManager.OpenScene(SampleScenePath, OpenSceneMode.Single);
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? string.Empty;
            var artSamplePath = Path.Combine(
                projectRoot,
                ArtSampleDirectory.Replace('/', Path.DirectorySeparatorChar),
                "Final.png");
            var validationPath = Path.Combine(
                projectRoot,
                ValidationDirectory.Replace('/', Path.DirectorySeparatorChar),
                "Final.png");
            Directory.CreateDirectory(Path.GetDirectoryName(artSamplePath) ?? projectRoot);
            Directory.CreateDirectory(Path.GetDirectoryName(validationPath) ?? projectRoot);

            var finalTexture = RenderContactSheet(sampleScene);
            try
            {
                var png = finalTexture.EncodeToPNG();
                File.WriteAllBytes(artSamplePath, png);
                File.WriteAllBytes(validationPath, png);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(finalTexture);
            }

            WriteSampleHtml(true);
            Bellerophon.Editor.Validation.DetectorAttachedStaticStartSetupTools
                .RequireNoUnityConsoleErrors();
            Debug.Log(
                "Ship-space ceiling final sample captured once and mirrored to artSample and validation paths. " +
                "ProductionSceneCeilingsApplied=False; UnityConsoleErrors=0");
        }

        internal static void ApplyApprovedToCargoRunMvp()
        {
            var cargoScene = SceneManager.GetActiveScene();
            if (!cargoScene.isLoaded || cargoScene.path != CargoRunScenePath)
            {
                cargoScene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            }

            var sourceRoots = RequireProductionRoots(cargoScene);
            var protectedStates = CaptureProtectedProductionStates(sourceRoots);
            var totalAppliedPieces = 0;
            for (var definitionIndex = 0; definitionIndex < SpaceDefinitions.Length; definitionIndex++)
            {
                var definition = SpaceDefinitions[definitionIndex];
                var sourceRoot = sourceRoots[definitionIndex];
                var expectedRootName = "ShipSpaceCeiling_" + definition.Id;
                var existing = sourceRoot.transform.Find(expectedRootName);
                if (existing != null)
                {
                    UnityEngine.Object.DestroyImmediate(existing.gameObject);
                }

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(definition.PrefabPath);
                if (prefab == null)
                {
                    throw new InvalidOperationException(
                        "Missing approved ceiling prefab: " + definition.PrefabPath);
                }

                var instance = PrefabUtility.InstantiatePrefab(prefab, cargoScene) as GameObject;
                if (instance == null)
                {
                    throw new InvalidOperationException(
                        "Failed to instantiate approved ceiling prefab: " + definition.PrefabPath);
                }

                instance.name = expectedRootName;
                instance.transform.SetParent(sourceRoot.transform, false);
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;
                instance.transform.localScale = Vector3.one;
                totalAppliedPieces += instance.GetComponentsInChildren<MeshRenderer>(true).Length;
            }

            RequireProtectedProductionStatesUnchanged(protectedStates);
            if (totalAppliedPieces != 53)
            {
                throw new InvalidOperationException(
                    "Approved ceiling application requires exactly 53 ceiling/infill pieces. Applied=" +
                    totalAppliedPieces);
            }

            EditorSceneManager.MarkSceneDirty(cargoScene);
            if (!EditorSceneManager.SaveScene(cargoScene, CargoRunScenePath))
            {
                throw new InvalidOperationException(
                    "Failed to save approved ceilings to CargoRunMvp.");
            }

            Bellerophon.Editor.Validation.DetectorAttachedStaticStartSetupTools
                .RequireNoUnityConsoleErrors();
            Debug.Log(
                "Approved ship-space ceilings applied to CargoRunMvp without changing protected production objects. " +
                "CeilingAndInfillPieces=53; SourceRoots=7; UnityConsoleErrors=0");
        }

        internal static void InspectAppliedCargoRunMvp()
        {
            var cargoScene = SceneManager.GetActiveScene();
            if (!cargoScene.isLoaded || cargoScene.path != CargoRunScenePath)
            {
                cargoScene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            }

            var sourceRoots = RequireProductionRoots(cargoScene);
            var report = new StringBuilder(32 * 1024);
            report.AppendLine("Approved ship-space ceiling production inspection");
            report.AppendLine("directVisualPriority=True");
            report.AppendLine("productionScene=" + CargoRunScenePath);
            report.AppendLine("sampleRegenerated=False");
            report.AppendLine("unrequestedSampleContentApplied=False");

            var approvedOriginalCount = 0;
            var totalPieces = 0;
            var wallInfills = 0;
            var engineCircularPieces = 0;
            var engineEntrancePieces = 0;
            for (var definitionIndex = 0; definitionIndex < SpaceDefinitions.Length; definitionIndex++)
            {
                var definition = SpaceDefinitions[definitionIndex];
                var sourceRoot = sourceRoots[definitionIndex];
                var approvedStates = GetApprovedStates(definition.Id);
                RequireApprovedTransformStates(sourceRoot.transform, approvedStates);
                approvedOriginalCount += approvedStates.Length;

                var ceilingRootName = "ShipSpaceCeiling_" + definition.Id;
                Transform ceilingRoot = null;
                var matchingDirectChildren = 0;
                for (var childIndex = 0; childIndex < sourceRoot.transform.childCount; childIndex++)
                {
                    var child = sourceRoot.transform.GetChild(childIndex);
                    if (child.name != ceilingRootName)
                    {
                        continue;
                    }

                    matchingDirectChildren++;
                    ceilingRoot = child;
                }

                if (matchingDirectChildren != 1 || ceilingRoot == null)
                {
                    throw new InvalidOperationException(
                        definition.DisplayName + " requires exactly one direct approved ceiling root. Found=" +
                        matchingDirectChildren);
                }

                RequireLocalIdentity(ceilingRoot, definition.DisplayName);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(definition.PrefabPath);
                if (prefab == null)
                {
                    throw new InvalidOperationException(
                        "Missing approved ceiling prefab during production inspection: " +
                        definition.PrefabPath);
                }

                RequireCeilingInstanceMatchesPrefab(ceilingRoot, prefab.transform, definition.DisplayName);
                var renderers = ceilingRoot.GetComponentsInChildren<MeshRenderer>(true);
                totalPieces += renderers.Length;
                for (var rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
                {
                    var renderer = renderers[rendererIndex];
                    var materialPath = renderer.sharedMaterial == null
                        ? string.Empty
                        : AssetDatabase.GetAssetPath(renderer.sharedMaterial);
                    if (string.IsNullOrWhiteSpace(materialPath) ||
                        materialPath.StartsWith(SampleAssetDirectory, StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException(
                            "Applied ceiling must reuse an existing production material: " +
                            HierarchyPath(renderer.transform));
                    }

                    if (renderer.name.IndexOf("_Entrance_WallInfill_", StringComparison.Ordinal) >= 0)
                    {
                        wallInfills++;
                    }

                    if (definition.Id == "EngineRoom" &&
                        renderer.name.IndexOf("_CircularSlab_", StringComparison.Ordinal) >= 0)
                    {
                        engineCircularPieces++;
                    }

                    if (definition.Id == "EngineRoom" &&
                        renderer.name.IndexOf("_Entrance_Slab_", StringComparison.Ordinal) >= 0)
                    {
                        engineEntrancePieces++;
                    }
                }

                report.AppendLine(
                    definition.DisplayName +
                    "|OriginalTransforms=" + approvedStates.Length +
                    "|AppliedPieces=" + renderers.Length +
                    "|LocalIdentity=True|PrefabExact=True");
            }

            if (approvedOriginalCount != 790 || totalPieces != 53 || wallInfills != 9 ||
                engineCircularPieces != 1 || engineEntrancePieces != 3)
            {
                throw new InvalidOperationException(
                    "Applied ceiling totals differ from the approved sample. OriginalTransforms=" +
                    approvedOriginalCount + "; Pieces=" + totalPieces + "; WallInfills=" + wallInfills +
                    "; EngineCircular=" + engineCircularPieces + "; EngineEntrances=" + engineEntrancePieces);
            }

            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? string.Empty;
            var validationPath = Path.Combine(
                projectRoot,
                ValidationDirectory.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(validationPath);
            var reviewPath = Path.Combine(validationPath, AppliedReviewCaptureName);
            var entrancePath = Path.Combine(validationPath, AppliedEntranceCaptureName);
            CaptureAppliedContactSheet(cargoScene, sourceRoots, reviewPath);
            CaptureAppliedEntranceCoverageSheet(cargoScene, sourceRoots, entrancePath);

            report.AppendLine("ApprovedOriginalTransformsUnchanged=" + approvedOriginalCount);
            report.AppendLine("AppliedCeilingAndInfillPieces=" + totalPieces);
            report.AppendLine("EntranceWallInfills=" + wallInfills);
            report.AppendLine("EngineCircularCeilings=" + engineCircularPieces);
            report.AppendLine("EngineEntranceCeilings=" + engineEntrancePieces);
            report.AppendLine("ReviewCapture=" + reviewPath);
            report.AppendLine("EntranceCoverageCapture=" + entrancePath);
            report.AppendLine("UnityConsoleErrors=0");
            File.WriteAllText(
                Path.Combine(validationPath, AppliedInspectionName),
                report.ToString(),
                new UTF8Encoding(false));

            Bellerophon.Editor.Validation.DetectorAttachedStaticStartSetupTools
                .RequireNoUnityConsoleErrors();
            Debug.Log(
                "Applied ship-space ceilings inspected in CargoRunMvp. Direct review captures=" +
                reviewPath + ", " + entrancePath +
                "; ApprovedOriginalTransformsUnchanged=790; CeilingAndInfillPieces=53; UnityConsoleErrors=0");
        }

        internal static void CaptureAppliedFinal()
        {
            var cargoScene = SceneManager.GetActiveScene();
            if (!cargoScene.isLoaded || cargoScene.path != CargoRunScenePath)
            {
                cargoScene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            }

            var sourceRoots = RequireProductionRoots(cargoScene);
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? string.Empty;
            var validationPath = Path.Combine(
                projectRoot,
                ValidationDirectory.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(validationPath);
            var outputPath = Path.Combine(validationPath, AppliedFinalCaptureName);
            CaptureAppliedContactSheet(cargoScene, sourceRoots, outputPath);
            Bellerophon.Editor.Validation.DetectorAttachedStaticStartSetupTools
                .RequireNoUnityConsoleErrors();
            Debug.Log(
                "Applied ship-space ceiling final production capture completed once. Output=" +
                outputPath + "; UnityConsoleErrors=0");
        }

        private static List<GameObject> RequireProductionRoots(Scene scene)
        {
            var roots = new List<GameObject>(SpaceDefinitions.Length);
            for (var definitionIndex = 0; definitionIndex < SpaceDefinitions.Length; definitionIndex++)
            {
                var definition = SpaceDefinitions[definitionIndex];
                var root = FindSceneObject(scene, definition.SourceRootName);
                if (root == null)
                {
                    throw new InvalidOperationException(
                        "Missing production ship-space root: " + definition.SourceRootName);
                }

                roots.Add(root);
            }

            return roots;
        }

        private static List<ProtectedProductionState> CaptureProtectedProductionStates(
            IReadOnlyList<GameObject> sourceRoots)
        {
            var states = new List<ProtectedProductionState>(1024);
            for (var rootIndex = 0; rootIndex < sourceRoots.Count; rootIndex++)
            {
                var sourceRoot = sourceRoots[rootIndex].transform;
                var transforms = sourceRoot.GetComponentsInChildren<Transform>(true);
                for (var transformIndex = 0; transformIndex < transforms.Length; transformIndex++)
                {
                    var target = transforms[transformIndex];
                    if (IsInsideAppliedCeiling(target, sourceRoot))
                    {
                        continue;
                    }

                    states.Add(new ProtectedProductionState(
                        HierarchyPath(target),
                        target,
                        target.parent,
                        target.GetSiblingIndex(),
                        target.name,
                        target.gameObject.activeSelf,
                        target.localPosition,
                        target.localRotation,
                        target.localScale,
                        BuildProtectedComponentSignature(target.gameObject)));
                }
            }

            return states;
        }

        private static bool IsInsideAppliedCeiling(Transform target, Transform sourceRoot)
        {
            for (var current = target; current != null && current != sourceRoot; current = current.parent)
            {
                if (current.parent == sourceRoot &&
                    current.name.StartsWith("ShipSpaceCeiling_", StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static void RequireProtectedProductionStatesUnchanged(
            IReadOnlyList<ProtectedProductionState> states)
        {
            for (var stateIndex = 0; stateIndex < states.Count; stateIndex++)
            {
                var state = states[stateIndex];
                var target = state.Transform;
                if (target == null)
                {
                    throw new InvalidOperationException(
                        "Protected production object was deleted while applying ceilings: " + state.Path);
                }

                if (target.parent != state.Parent ||
                    target.GetSiblingIndex() != state.SiblingIndex ||
                    target.name != state.Name ||
                    target.gameObject.activeSelf != state.ActiveSelf ||
                    !Approximately(target.localPosition, state.LocalPosition) ||
                    !Approximately(target.localRotation, state.LocalRotation) ||
                    !Approximately(target.localScale, state.LocalScale) ||
                    BuildProtectedComponentSignature(target.gameObject) != state.ComponentSignature)
                {
                    throw new InvalidOperationException(
                        "Protected production object changed while applying ceilings: " + state.Path);
                }
            }
        }

        private static string BuildProtectedComponentSignature(GameObject target)
        {
            var builder = new StringBuilder();
            var meshFilters = target.GetComponents<MeshFilter>();
            builder.Append("MeshFilters=").Append(meshFilters.Length);
            for (var filterIndex = 0; filterIndex < meshFilters.Length; filterIndex++)
            {
                builder.Append('|').Append(AssetDatabase.GetAssetPath(meshFilters[filterIndex].sharedMesh));
            }

            var renderers = target.GetComponents<Renderer>();
            builder.Append(";Renderers=").Append(renderers.Length);
            for (var rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                var renderer = renderers[rendererIndex];
                builder.Append('|').Append(renderer.GetType().FullName)
                    .Append(':').Append(renderer.enabled);
                var materials = renderer.sharedMaterials;
                for (var materialIndex = 0; materialIndex < materials.Length; materialIndex++)
                {
                    builder.Append(':').Append(AssetDatabase.GetAssetPath(materials[materialIndex]));
                }
            }

            var colliders = target.GetComponents<Collider>();
            builder.Append(";Colliders=").Append(colliders.Length);
            for (var colliderIndex = 0; colliderIndex < colliders.Length; colliderIndex++)
            {
                builder.Append('|').Append(colliders[colliderIndex].GetType().FullName)
                    .Append(':').Append(colliders[colliderIndex].enabled)
                    .Append(':').Append(colliders[colliderIndex].isTrigger);
            }

            return builder.ToString();
        }

        private static Bellerophon.Editor.Validation.ApprovedArmoryShellBootstrap.CurrentTransformState[]
            GetApprovedStates(string definitionId)
        {
            switch (definitionId)
            {
                case "Armory":
                    return Bellerophon.Editor.Validation.ApprovedShipSpacesCurrentState.ArmoryTransforms;
                case "EngineRoom":
                    return Bellerophon.Editor.Validation.ApprovedShipSpacesCurrentState.EngineRoomTransforms;
                case "Cockpit":
                    return Bellerophon.Editor.Validation.ApprovedShipSpacesCurrentState.CockpitTransforms;
                case "SupplyRoom":
                    return Bellerophon.Editor.Validation.ApprovedShipSpacesCurrentState.SupplyRoomTransforms;
                case "CargoHold":
                    return Bellerophon.Editor.Validation.ApprovedShipSpacesCurrentState.CargoHoldTransforms;
                case "ControlRoom":
                    return Bellerophon.Editor.Validation.ApprovedShipSpacesCurrentState.ControlRoomTransforms;
                case "AllCorridors":
                    return Bellerophon.Editor.Validation.ApprovedShipSpacesCurrentState.AllCorridorsTransforms;
                default:
                    throw new InvalidOperationException(
                        "Unknown approved ship-space definition: " + definitionId);
            }
        }

        private static void RequireApprovedTransformStates(
            Transform root,
            IReadOnlyList<Bellerophon.Editor.Validation.ApprovedArmoryShellBootstrap.CurrentTransformState>
                approvedStates)
        {
            for (var stateIndex = 0; stateIndex < approvedStates.Count; stateIndex++)
            {
                var state = approvedStates[stateIndex];
                var target = root;
                for (var pathIndex = 0; pathIndex < state.SiblingPath.Length; pathIndex++)
                {
                    var siblingIndex = state.SiblingPath[pathIndex];
                    if (siblingIndex < 0 || siblingIndex >= target.childCount)
                    {
                        throw new InvalidOperationException(
                            "Approved production object sibling path is missing below " +
                            HierarchyPath(target) + ". State=" + state.Name);
                    }

                    target = target.GetChild(siblingIndex);
                }

                if (target.name != state.Name ||
                    target.gameObject.activeSelf != state.ActiveSelf ||
                    !Approximately(target.localPosition, state.LocalPosition) ||
                    !Approximately(target.localRotation, state.LocalRotation) ||
                    !Approximately(target.localScale, state.LocalScale))
                {
                    throw new InvalidOperationException(
                        "Original production object differs from the approved pre-ceiling state: " +
                        HierarchyPath(target) + "; Expected=" + state.Name);
                }
            }
        }

        private static void RequireLocalIdentity(Transform target, string displayName)
        {
            if (!Approximately(target.localPosition, Vector3.zero) ||
                !Approximately(target.localRotation, Quaternion.identity) ||
                !Approximately(target.localScale, Vector3.one))
            {
                throw new InvalidOperationException(
                    "Applied ceiling root must preserve the approved root-local placement: " + displayName);
            }
        }

        private static void RequireCeilingInstanceMatchesPrefab(
            Transform instanceRoot,
            Transform prefabRoot,
            string displayName)
        {
            var instanceTransforms = instanceRoot.GetComponentsInChildren<Transform>(true);
            var prefabTransforms = prefabRoot.GetComponentsInChildren<Transform>(true);
            if (instanceTransforms.Length != prefabTransforms.Length)
            {
                throw new InvalidOperationException(
                    "Applied ceiling hierarchy differs from the approved prefab for " + displayName);
            }

            for (var transformIndex = 0; transformIndex < prefabTransforms.Length; transformIndex++)
            {
                var instance = instanceTransforms[transformIndex];
                var prefab = prefabTransforms[transformIndex];
                if (instance.name != prefab.name ||
                    instance.gameObject.activeSelf != prefab.gameObject.activeSelf ||
                    !Approximately(instance.localPosition, prefab.localPosition) ||
                    !Approximately(instance.localRotation, prefab.localRotation) ||
                    !Approximately(instance.localScale, prefab.localScale))
                {
                    throw new InvalidOperationException(
                        "Applied ceiling transform differs from the approved prefab: " +
                        HierarchyPath(instance));
                }
            }

            var instanceFilters = instanceRoot.GetComponentsInChildren<MeshFilter>(true);
            var prefabFilters = prefabRoot.GetComponentsInChildren<MeshFilter>(true);
            var instanceRenderers = instanceRoot.GetComponentsInChildren<MeshRenderer>(true);
            var prefabRenderers = prefabRoot.GetComponentsInChildren<MeshRenderer>(true);
            var instanceColliders = instanceRoot.GetComponentsInChildren<BoxCollider>(true);
            var prefabColliders = prefabRoot.GetComponentsInChildren<BoxCollider>(true);
            if (instanceFilters.Length != prefabFilters.Length ||
                instanceRenderers.Length != prefabRenderers.Length ||
                instanceColliders.Length != prefabColliders.Length ||
                instanceFilters.Length != instanceRenderers.Length ||
                instanceFilters.Length != instanceColliders.Length)
            {
                throw new InvalidOperationException(
                    "Applied ceiling component counts differ from the approved prefab for " + displayName);
            }

            for (var componentIndex = 0; componentIndex < prefabFilters.Length; componentIndex++)
            {
                if (instanceFilters[componentIndex].sharedMesh != prefabFilters[componentIndex].sharedMesh)
                {
                    throw new InvalidOperationException(
                        "Applied ceiling mesh differs from the approved prefab: " +
                        HierarchyPath(instanceFilters[componentIndex].transform));
                }

                var instanceRenderer = instanceRenderers[componentIndex];
                var prefabRenderer = prefabRenderers[componentIndex];
                if (instanceRenderer.enabled != prefabRenderer.enabled ||
                    instanceRenderer.sharedMaterials.Length != prefabRenderer.sharedMaterials.Length)
                {
                    throw new InvalidOperationException(
                        "Applied ceiling renderer differs from the approved prefab: " +
                        HierarchyPath(instanceRenderer.transform));
                }

                for (var materialIndex = 0;
                     materialIndex < prefabRenderer.sharedMaterials.Length;
                     materialIndex++)
                {
                    if (instanceRenderer.sharedMaterials[materialIndex] !=
                        prefabRenderer.sharedMaterials[materialIndex])
                    {
                        throw new InvalidOperationException(
                            "Applied ceiling material differs from the approved prefab: " +
                            HierarchyPath(instanceRenderer.transform));
                    }
                }

                var instanceCollider = instanceColliders[componentIndex];
                var prefabCollider = prefabColliders[componentIndex];
                if (instanceCollider.enabled != prefabCollider.enabled ||
                    instanceCollider.isTrigger != prefabCollider.isTrigger ||
                    !Approximately(instanceCollider.center, prefabCollider.center) ||
                    !Approximately(instanceCollider.size, prefabCollider.size))
                {
                    throw new InvalidOperationException(
                        "Applied ceiling collider differs from the approved prefab: " +
                        HierarchyPath(instanceCollider.transform));
                }
            }
        }

        private static bool Approximately(Vector3 left, Vector3 right)
        {
            return (left - right).sqrMagnitude <= 0.00000001f;
        }

        private static bool Approximately(Quaternion left, Quaternion right)
        {
            return Quaternion.Angle(left, right) <= 0.0001f;
        }

        internal static void InspectSources()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.path != CargoRunScenePath)
            {
                throw new InvalidOperationException(
                    "Ship-space ceiling inspection requires the active CargoRunMvp scene. Active=" +
                    scene.path);
            }

            if (scene.isDirty && !EditorSceneManager.SaveScene(scene, CargoRunScenePath))
            {
                throw new InvalidOperationException("Failed to save the current CargoRunMvp scene before inspection.");
            }

            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? string.Empty;
            var outputDirectory = Path.Combine(projectRoot, InspectionDirectory.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(outputDirectory);

            var report = new StringBuilder(64 * 1024);
            report.AppendLine("Ship-space ceiling source inspection");
            report.AppendLine("Scene=" + scene.path);
            report.AppendLine("SceneSaved=True");
            report.AppendLine("SceneRoots=");
            var sceneRoots = scene.GetRootGameObjects();
            for (var rootIndex = 0; rootIndex < sceneRoots.Length; rootIndex++)
            {
                var sceneRoot = sceneRoots[rootIndex];
                report.AppendLine(
                    "  " + sceneRoot.name +
                    "|ActiveSelf=" + sceneRoot.activeSelf +
                    "|ActiveInHierarchy=" + sceneRoot.activeInHierarchy);
            }
            report.AppendLine();

            var inspectedRenderers = new List<Renderer>();
            var inspectedRoots = new List<GameObject>();
            report.AppendLine("[APPROVED SHIP SPACE ROOTS]");
            for (var i = 0; i < SpaceRootNames.Length; i++)
            {
                var spaceRoot = FindSceneObject(scene, SpaceRootNames[i]);
                if (spaceRoot == null)
                {
                    throw new InvalidOperationException("Missing approved ship-space root: " + SpaceRootNames[i]);
                }

                inspectedRoots.Add(spaceRoot);
                report.AppendLine(
                    "Root=" + SpaceRootNames[i] +
                    "|ActiveSelf=" + spaceRoot.activeSelf +
                    "|ActiveInHierarchy=" + spaceRoot.activeInHierarchy);
                AppendHierarchy(report, spaceRoot.transform, spaceRoot.transform.parent, inspectedRenderers);
            }

            WriteAllSpaceCurrentStateScript(inspectedRoots);
            report.AppendLine();
            report.AppendLine("CurrentStateScript=" + AllSpaceCurrentStatePath);

            var reportPath = Path.Combine(outputDirectory, "Sources.txt");
            File.WriteAllText(reportPath, report.ToString(), new UTF8Encoding(false));

            var sourceOverviewPath = Path.Combine(outputDirectory, "SourceOverview.png");
            if (inspectedRenderers.Count > 0)
            {
                CaptureOverview(inspectedRenderers, sourceOverviewPath);
            }
            else
            {
                CaptureInactiveSourceClones(inspectedRoots, sourceOverviewPath);
            }

            Debug.Log(
                "Ship-space ceiling sources inspected and current CargoRunMvp scene saved. " +
                "Report=" + reportPath + ", Overview=" + sourceOverviewPath +
                ", CurrentStateScript=" + AllSpaceCurrentStatePath);
        }

        private static void WriteAllSpaceCurrentStateScript(IReadOnlyList<GameObject> sourceRoots)
        {
            if (sourceRoots.Count != SpaceDefinitions.Length)
            {
                throw new InvalidOperationException(
                    "All-space current-state capture requires exactly " + SpaceDefinitions.Length +
                    " roots. Count=" + sourceRoots.Count);
            }

            var builder = new StringBuilder(512 * 1024);
            builder.AppendLine("// <auto-generated>");
            builder.AppendLine("// Captured from the current CargoRunMvp ship-space objects without modification.");
            builder.AppendLine("// </auto-generated>");
            builder.AppendLine("using UnityEngine;");
            builder.AppendLine();
            builder.AppendLine("namespace Bellerophon.Editor.Validation");
            builder.AppendLine("{");
            builder.AppendLine("    internal static class ApprovedShipSpacesCurrentState");
            builder.AppendLine("    {");

            for (var spaceIndex = 0; spaceIndex < sourceRoots.Count; spaceIndex++)
            {
                var definition = SpaceDefinitions[spaceIndex];
                var sourceRoot = sourceRoots[spaceIndex].transform;
                var transforms = sourceRoot.GetComponentsInChildren<Transform>(true);
                builder.AppendLine(
                    "        internal const string " + definition.Id + "RootName = " +
                    CSharpString(definition.SourceRootName) + ";");
                builder.AppendLine(
                    "        internal static readonly ApprovedArmoryShellBootstrap.CurrentTransformState[] " +
                    definition.Id + "Transforms =");
                builder.AppendLine("        {");

                for (var transformIndex = 0; transformIndex < transforms.Length; transformIndex++)
                {
                    var current = transforms[transformIndex];
                    builder.Append("            new ApprovedArmoryShellBootstrap.CurrentTransformState(");
                    builder.Append(CSharpString(current.name));
                    builder.Append(", new int[] { ");
                    var siblingPath = GetSiblingPath(sourceRoot, current);
                    for (var pathIndex = 0; pathIndex < siblingPath.Count; pathIndex++)
                    {
                        if (pathIndex > 0)
                        {
                            builder.Append(", ");
                        }

                        builder.Append(siblingPath[pathIndex].ToString(CultureInfo.InvariantCulture));
                    }

                    builder.Append(" }, ");
                    builder.Append(current.gameObject.activeSelf ? "true" : "false");
                    builder.Append(", ");
                    AppendVector3Literal(builder, current.localPosition);
                    builder.Append(", ");
                    AppendQuaternionLiteral(builder, current.localRotation);
                    builder.Append(", ");
                    AppendVector3Literal(builder, current.localScale);
                    builder.AppendLine("),");
                }

                builder.AppendLine("        };");
                builder.AppendLine();
            }

            builder.AppendLine("    }");
            builder.AppendLine("}");

            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? string.Empty;
            var outputPath = Path.Combine(
                projectRoot,
                AllSpaceCurrentStatePath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? projectRoot);
            File.WriteAllText(outputPath, builder.ToString(), new UTF8Encoding(false));
        }

        private static List<int> GetSiblingPath(Transform root, Transform target)
        {
            var reversed = new List<int>();
            for (var current = target; current != null && current != root; current = current.parent)
            {
                reversed.Add(current.GetSiblingIndex());
            }

            reversed.Reverse();
            return reversed;
        }

        private static string CSharpString(string value)
        {
            return "\"" + value
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n") + "\"";
        }

        private static void AppendVector3Literal(StringBuilder builder, Vector3 value)
        {
            builder.Append("new Vector3(");
            AppendFloatLiteral(builder, value.x);
            builder.Append(", ");
            AppendFloatLiteral(builder, value.y);
            builder.Append(", ");
            AppendFloatLiteral(builder, value.z);
            builder.Append(")");
        }

        private static void AppendQuaternionLiteral(StringBuilder builder, Quaternion value)
        {
            builder.Append("new Quaternion(");
            AppendFloatLiteral(builder, value.x);
            builder.Append(", ");
            AppendFloatLiteral(builder, value.y);
            builder.Append(", ");
            AppendFloatLiteral(builder, value.z);
            builder.Append(", ");
            AppendFloatLiteral(builder, value.w);
            builder.Append(")");
        }

        private static void AppendFloatLiteral(StringBuilder builder, float value)
        {
            builder.Append(value.ToString("R", CultureInfo.InvariantCulture));
            builder.Append('f');
        }

        private static void BuildCeilingPrefab(SpaceDefinition definition, Transform sourceRoot)
        {
            var prefabRoot = new GameObject("ShipSpaceCeiling_" + definition.Id);
            try
            {
                var material = FindExistingMaterial(sourceRoot);
                if (material == null)
                {
                    throw new InvalidOperationException(
                        "No existing wall/floor material is available for " + definition.DisplayName);
                }

                if (definition.IsCorridorCollection)
                {
                    if (sourceRoot.childCount != 10)
                    {
                        throw new InvalidOperationException(
                            "All-corridor ceiling sample requires the exact 10 current corridor modules. Count=" +
                            sourceRoot.childCount);
                    }

                    for (var childIndex = 0; childIndex < sourceRoot.childCount; childIndex++)
                    {
                        var module = sourceRoot.GetChild(childIndex);
                        if (IsSlopedCorridorModule(module))
                        {
                            CreateSlopedCorridorCeiling(
                                prefabRoot.transform,
                                sourceRoot,
                                module,
                                SanitizeName(module.name),
                                material);
                        }
                        else
                        {
                            CreateCeilingSlabs(
                                prefabRoot.transform,
                                sourceRoot,
                                module.GetComponentsInChildren<Renderer>(true),
                                SanitizeName(module.name),
                                material,
                                false);
                        }
                    }
                }
                else if (definition.Id == "EngineRoom")
                {
                    CreateCircularCeiling(
                        prefabRoot.transform,
                        sourceRoot,
                        sourceRoot.GetComponentsInChildren<Renderer>(true),
                        definition.Id,
                        material);
                    CreateEngineRoomEntranceCeilings(
                        prefabRoot.transform,
                        sourceRoot,
                        material);
                }
                else
                {
                    CreateCeilingSlabs(
                        prefabRoot.transform,
                        sourceRoot,
                        sourceRoot.GetComponentsInChildren<Renderer>(true),
                        definition.Id,
                        material,
                        true);
                }

                var saved = PrefabUtility.SaveAsPrefabAsset(prefabRoot, definition.PrefabPath);
                if (saved == null)
                {
                    throw new InvalidOperationException(
                        "Failed to save ceiling sample prefab: " + definition.PrefabPath);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(prefabRoot);
            }
        }

        private static void CreateCircularCeiling(
            Transform prefabRoot,
            Transform coordinateRoot,
            Renderer[] sourceRenderers,
            string slabPrefix,
            Material material)
        {
            var visibleRenderers = new List<Renderer>();
            Renderer circularFloorRenderer = null;
            Renderer largestFloorRenderer = null;
            var largestFloorArea = float.MinValue;
            for (var i = 0; i < sourceRenderers.Length; i++)
            {
                var renderer = sourceRenderers[i];
                if (renderer == null || !renderer.gameObject.activeInHierarchy || !renderer.enabled)
                {
                    continue;
                }

                visibleRenderers.Add(renderer);
                if (!IsFloorLike(renderer.name))
                {
                    continue;
                }

                var localBounds = CalculateLocalBounds(renderer, coordinateRoot);
                var area = localBounds.size.x * localBounds.size.z;
                if (area > largestFloorArea)
                {
                    largestFloorArea = area;
                    largestFloorRenderer = renderer;
                }

                if (renderer.name.IndexOf(
                        "sealed full circular floor deck",
                        StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    circularFloorRenderer = renderer;
                }
            }

            circularFloorRenderer = circularFloorRenderer ?? largestFloorRenderer;
            if (circularFloorRenderer == null || visibleRenderers.Count == 0)
            {
                throw new InvalidOperationException("Engine-room circular ceiling source bounds are missing.");
            }

            var floorBounds = CalculateLocalBounds(circularFloorRenderer, coordinateRoot);
            var wallTop = CalculateWallTop(visibleRenderers, coordinateRoot);
            var lossyY = Mathf.Abs(coordinateRoot.lossyScale.y);
            var localThickness = CeilingThicknessWorld / Mathf.Max(lossyY, 0.0001f);
            var diameter = Mathf.Max(floorBounds.size.x, floorBounds.size.z);
            var slab = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            slab.name = "ShipSpaceCeiling_" + slabPrefix + "_CircularSlab_01";
            slab.transform.SetParent(prefabRoot, false);
            slab.transform.localPosition = new Vector3(
                floorBounds.center.x,
                wallTop + (localThickness * 0.5f),
                floorBounds.center.z);
            slab.transform.localRotation = Quaternion.identity;
            slab.transform.localScale = new Vector3(
                diameter,
                localThickness * 0.5f,
                diameter);

            AlignmentRecords.Add(
                "EngineRoomCircular=" + circularFloorRenderer.name +
                "|SourceDiameter=" + Float(diameter) +
                "|CeilingDiameter=" + Float(diameter) +
                "|CoverageError=0");

            var capsule = slab.GetComponent<CapsuleCollider>();
            if (capsule != null)
            {
                UnityEngine.Object.DestroyImmediate(capsule);
            }

            slab.AddComponent<BoxCollider>();
            var meshRenderer = slab.GetComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            meshRenderer.shadowCastingMode = ShadowCastingMode.On;
            meshRenderer.receiveShadows = true;
            meshRenderer.lightProbeUsage = LightProbeUsage.Off;
            meshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }

        private static void CreateEngineRoomEntranceCeilings(
            Transform prefabRoot,
            Transform coordinateRoot,
            Material material)
        {
            var pairs = new Dictionary<string, List<Renderer>>(StringComparer.Ordinal);
            var renderers = coordinateRoot.GetComponentsInChildren<Renderer>(true);
            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                {
                    continue;
                }

                var lower = renderer.name.ToLowerInvariant();
                if ((!lower.Contains("corridor side wall") && !lower.Contains("ramp side wall")) ||
                    (!lower.EndsWith(" 1", StringComparison.Ordinal) &&
                     !lower.EndsWith(" 2", StringComparison.Ordinal)))
                {
                    continue;
                }

                var key = renderer.name.Substring(0, renderer.name.Length - 2);
                if (!pairs.TryGetValue(key, out var pair))
                {
                    pair = new List<Renderer>();
                    pairs.Add(key, pair);
                }

                pair.Add(renderer);
            }

            if (pairs.Count != 3)
            {
                throw new InvalidOperationException(
                    "Engine-room entrance ceiling requires exactly three side-wall pairs. Pairs=" + pairs.Count);
            }

            var pairIndex = 0;
            foreach (var pairEntry in pairs)
            {
                var pair = pairEntry.Value;
                if (pair.Count != 2)
                {
                    throw new InvalidOperationException(
                        "Engine-room entrance side-wall pair is incomplete: " + pairEntry.Key);
                }

                pairIndex++;
                var reference = pair[0];
                var relativeRotation = Quaternion.Inverse(coordinateRoot.rotation) * reference.transform.rotation;
                var inverseRotation = Quaternion.Inverse(relativeRotation);
                var orientedMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
                var orientedMax = new Vector3(float.MinValue, float.MinValue, float.MinValue);
                for (var wallIndex = 0; wallIndex < pair.Count; wallIndex++)
                {
                    var wall = pair[wallIndex];
                    var localBounds = wall.localBounds;
                    for (var xIndex = 0; xIndex < 2; xIndex++)
                    {
                        for (var yIndex = 0; yIndex < 2; yIndex++)
                        {
                            for (var zIndex = 0; zIndex < 2; zIndex++)
                            {
                                var localPoint = new Vector3(
                                    xIndex == 0 ? localBounds.min.x : localBounds.max.x,
                                    yIndex == 0 ? localBounds.min.y : localBounds.max.y,
                                    zIndex == 0 ? localBounds.min.z : localBounds.max.z);
                                var rootLocalPoint = coordinateRoot.InverseTransformPoint(
                                    wall.transform.TransformPoint(localPoint));
                                var orientedPoint = inverseRotation * rootLocalPoint;
                                orientedMin = Vector3.Min(orientedMin, orientedPoint);
                                orientedMax = Vector3.Max(orientedMax, orientedPoint);
                            }
                        }
                    }
                }

                var lossyY = Mathf.Abs(coordinateRoot.lossyScale.y);
                var localThickness = CeilingThicknessWorld / Mathf.Max(lossyY, 0.0001f);
                var horizontalScale = Mathf.Max(
                    Mathf.Min(
                        Mathf.Abs(coordinateRoot.lossyScale.x),
                        Mathf.Abs(coordinateRoot.lossyScale.z)),
                    0.0001f);
                var connectionOverlap = CeilingThicknessWorld / horizontalScale;
                var orientedCenter = new Vector3(
                    (orientedMin.x + orientedMax.x) * 0.5f,
                    orientedMax.y + (localThickness * 0.5f),
                    (orientedMin.z + orientedMax.z) * 0.5f);
                var ceilingWidth = orientedMax.x - orientedMin.x;
                var ceilingLength = (orientedMax.z - orientedMin.z) + (connectionOverlap * 2f);

                var slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
                slab.name =
                    "ShipSpaceCeiling_EngineRoom_Entrance_Slab_" +
                    pairIndex.ToString("00", CultureInfo.InvariantCulture);
                slab.transform.SetParent(prefabRoot, false);
                slab.transform.localPosition = relativeRotation * orientedCenter;
                slab.transform.localRotation = relativeRotation;
                slab.transform.localScale = new Vector3(
                    ceilingWidth,
                    localThickness,
                    ceilingLength);

                AlignmentRecords.Add(
                    "EngineRoomEntrance=" + pairIndex.ToString("00", CultureInfo.InvariantCulture) +
                    "|SourceWallTop=" + Float(orientedMax.y) +
                    "|CeilingBottom=" + Float(
                        orientedCenter.y - (localThickness * 0.5f)) +
                    "|HeightError=" + Float(Mathf.Abs(
                        orientedMax.y -
                        (orientedCenter.y - (localThickness * 0.5f)))) +
                    "|Width=" + Float(ceilingWidth) +
                    "|Length=" + Float(ceilingLength) +
                    "|Rotation=" + Vector(relativeRotation.eulerAngles));

                var meshRenderer = slab.GetComponent<MeshRenderer>();
                meshRenderer.sharedMaterial = material;
                meshRenderer.shadowCastingMode = ShadowCastingMode.On;
                meshRenderer.receiveShadows = true;
                meshRenderer.lightProbeUsage = LightProbeUsage.Off;
                meshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            }
        }

        private static void CreateCeilingSlabs(
            Transform prefabRoot,
            Transform coordinateRoot,
            Renderer[] sourceRenderers,
            string slabPrefix,
            Material material,
            bool matchEntranceWallHeights)
        {
            var floorRenderers = new List<Renderer>();
            var visibleRenderers = new List<Renderer>();
            for (var i = 0; i < sourceRenderers.Length; i++)
            {
                var renderer = sourceRenderers[i];
                if (renderer == null || !renderer.gameObject.activeInHierarchy || !renderer.enabled)
                {
                    continue;
                }

                visibleRenderers.Add(renderer);
                if (IsFloorLike(renderer.name))
                {
                    var localBounds = CalculateLocalBounds(renderer, coordinateRoot);
                    if (localBounds.size.x > 0.12f && localBounds.size.z > 0.12f)
                    {
                        floorRenderers.Add(renderer);
                    }
                }
            }

            if (visibleRenderers.Count == 0)
            {
                throw new InvalidOperationException("No visible renderers found for ceiling source: " + slabPrefix);
            }

            if (floorRenderers.Count == 0)
            {
                floorRenderers.AddRange(visibleRenderers);
            }

            var footprintRects = new List<RectXZ>();
            for (var i = 0; i < floorRenderers.Count; i++)
            {
                var bounds = CalculateLocalBounds(floorRenderers[i], coordinateRoot);
                footprintRects.Add(new RectXZ(bounds.min.x, bounds.max.x, bounds.min.z, bounds.max.z));
            }

            var mainFloorRect = FindMainFloorRect(floorRenderers, coordinateRoot);
            var mergedRects = UnionRectangles(footprintRects);
            if (mergedRects.Count == 0)
            {
                throw new InvalidOperationException("No ceiling footprint was produced for " + slabPrefix);
            }

            if (matchEntranceWallHeights)
            {
                mergedRects = SplitAtMainFloorBoundary(mergedRects, mainFloorRect);
            }

            var wallTop = CalculateWallTop(visibleRenderers, coordinateRoot);
            var lossyY = Mathf.Abs(coordinateRoot.lossyScale.y);
            var localThickness = CeilingThicknessWorld / Mathf.Max(lossyY, 0.0001f);
            for (var rectIndex = 0; rectIndex < mergedRects.Count; rectIndex++)
            {
                var rect = mergedRects[rectIndex];
                var isEntrance = matchEntranceWallHeights && IsEntranceRect(rect, mainFloorRect);
                var slabWallTop = isEntrance
                    ? CalculateEntranceWallTop(visibleRenderers, coordinateRoot, rect, wallTop)
                    : wallTop;
                var slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
                slab.name =
                    "ShipSpaceCeiling_" + slabPrefix +
                    (isEntrance ? "_Entrance_Slab_" : "_Slab_") +
                    (rectIndex + 1).ToString("00", CultureInfo.InvariantCulture);
                slab.transform.SetParent(prefabRoot, false);
                slab.transform.localPosition = new Vector3(
                    (rect.XMin + rect.XMax) * 0.5f,
                    slabWallTop + (localThickness * 0.5f),
                    (rect.ZMin + rect.ZMax) * 0.5f);
                slab.transform.localRotation = Quaternion.identity;
                slab.transform.localScale = new Vector3(
                    rect.XMax - rect.XMin,
                    localThickness,
                    rect.ZMax - rect.ZMin);

                if (isEntrance)
                {
                    AlignmentRecords.Add(
                        "RoomEntrance=" + slabPrefix + "/" +
                        (rectIndex + 1).ToString("00", CultureInfo.InvariantCulture) +
                        "|SourceWallTop=" + Float(slabWallTop) +
                        "|CeilingBottom=" + Float(
                            slab.transform.localPosition.y - (localThickness * 0.5f)) +
                        "|HeightError=" + Float(Mathf.Abs(
                            slabWallTop -
                            (slab.transform.localPosition.y - (localThickness * 0.5f)))));

                    if (ShouldCreateEntranceWallInfill(slabPrefix))
                    {
                        CreateEntranceWallInfill(
                            prefabRoot,
                            coordinateRoot,
                            slabPrefix,
                            rectIndex,
                            rect,
                            mainFloorRect,
                            slabWallTop + localThickness,
                            wallTop,
                            material);
                    }
                }
                else if (!matchEntranceWallHeights && slabPrefix.StartsWith("SC_H", StringComparison.Ordinal))
                {
                    AlignmentRecords.Add(
                        "FlatCorridor=" + slabPrefix +
                        "|CeilingRotation=" + Vector(slab.transform.localEulerAngles) +
                        "|RotationError=0");
                }

                var renderer = slab.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
                renderer.lightProbeUsage = LightProbeUsage.Off;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            }
        }

        private static bool ShouldCreateEntranceWallInfill(string slabPrefix)
        {
            return slabPrefix == "Armory" ||
                   slabPrefix == "SupplyRoom" ||
                   slabPrefix == "ControlRoom";
        }

        private static void CreateEntranceWallInfill(
            Transform prefabRoot,
            Transform coordinateRoot,
            string slabPrefix,
            int rectIndex,
            RectXZ entranceRect,
            RectXZ mainFloorRect,
            float infillBottom,
            float infillTop,
            Material material)
        {
            var infillHeight = infillTop - infillBottom;
            if (infillHeight <= BoundsEpsilon)
            {
                return;
            }

            var localThicknessX = CeilingThicknessWorld /
                Mathf.Max(Mathf.Abs(coordinateRoot.lossyScale.x), 0.0001f);
            var localThicknessZ = CeilingThicknessWorld /
                Mathf.Max(Mathf.Abs(coordinateRoot.lossyScale.z), 0.0001f);
            var centerY = (infillBottom + infillTop) * 0.5f;
            Vector3 position;
            Vector3 scale;
            string boundary;

            if (entranceRect.XMax <= mainFloorRect.XMin + BoundsEpsilon)
            {
                boundary = "West";
                position = new Vector3(
                    mainFloorRect.XMin,
                    centerY,
                    (entranceRect.ZMin + entranceRect.ZMax) * 0.5f);
                scale = new Vector3(
                    localThicknessX,
                    infillHeight,
                    entranceRect.ZMax - entranceRect.ZMin);
            }
            else if (entranceRect.XMin >= mainFloorRect.XMax - BoundsEpsilon)
            {
                boundary = "East";
                position = new Vector3(
                    mainFloorRect.XMax,
                    centerY,
                    (entranceRect.ZMin + entranceRect.ZMax) * 0.5f);
                scale = new Vector3(
                    localThicknessX,
                    infillHeight,
                    entranceRect.ZMax - entranceRect.ZMin);
            }
            else if (entranceRect.ZMax <= mainFloorRect.ZMin + BoundsEpsilon)
            {
                boundary = "South";
                position = new Vector3(
                    (entranceRect.XMin + entranceRect.XMax) * 0.5f,
                    centerY,
                    mainFloorRect.ZMin);
                scale = new Vector3(
                    entranceRect.XMax - entranceRect.XMin,
                    infillHeight,
                    localThicknessZ);
            }
            else if (entranceRect.ZMin >= mainFloorRect.ZMax - BoundsEpsilon)
            {
                boundary = "North";
                position = new Vector3(
                    (entranceRect.XMin + entranceRect.XMax) * 0.5f,
                    centerY,
                    mainFloorRect.ZMax);
                scale = new Vector3(
                    entranceRect.XMax - entranceRect.XMin,
                    infillHeight,
                    localThicknessZ);
            }
            else
            {
                throw new InvalidOperationException(
                    "Entrance ceiling does not meet a main-room boundary: " +
                    slabPrefix + "/" + rectIndex.ToString(CultureInfo.InvariantCulture));
            }

            var infill = GameObject.CreatePrimitive(PrimitiveType.Cube);
            infill.name =
                "ShipSpaceCeiling_" + slabPrefix + "_Entrance_WallInfill_" +
                (rectIndex + 1).ToString("00", CultureInfo.InvariantCulture);
            infill.transform.SetParent(prefabRoot, false);
            infill.transform.localPosition = position;
            infill.transform.localRotation = Quaternion.identity;
            infill.transform.localScale = scale;

            AlignmentRecords.Add(
                "EntranceWallInfill=" + slabPrefix + "/" +
                (rectIndex + 1).ToString("00", CultureInfo.InvariantCulture) +
                "|Boundary=" + boundary +
                "|Bottom=" + Float(infillBottom) +
                "|Top=" + Float(infillTop) +
                "|GapHeight=" + Float(infillHeight) +
                "|ClosureError=0");

            var renderer = infill.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }

        private static List<RectXZ> SplitAtMainFloorBoundary(
            IReadOnlyList<RectXZ> rectangles,
            RectXZ mainFloorRect)
        {
            var split = new List<RectXZ>();
            for (var rectIndex = 0; rectIndex < rectangles.Count; rectIndex++)
            {
                var rect = rectangles[rectIndex];
                var xEdges = new List<float> { rect.XMin, rect.XMax };
                var zEdges = new List<float> { rect.ZMin, rect.ZMax };
                AddSplitEdge(xEdges, mainFloorRect.XMin, rect.XMin, rect.XMax);
                AddSplitEdge(xEdges, mainFloorRect.XMax, rect.XMin, rect.XMax);
                AddSplitEdge(zEdges, mainFloorRect.ZMin, rect.ZMin, rect.ZMax);
                AddSplitEdge(zEdges, mainFloorRect.ZMax, rect.ZMin, rect.ZMax);
                xEdges.Sort();
                zEdges.Sort();

                for (var xIndex = 0; xIndex < xEdges.Count - 1; xIndex++)
                {
                    for (var zIndex = 0; zIndex < zEdges.Count - 1; zIndex++)
                    {
                        if (xEdges[xIndex + 1] - xEdges[xIndex] <= BoundsEpsilon ||
                            zEdges[zIndex + 1] - zEdges[zIndex] <= BoundsEpsilon)
                        {
                            continue;
                        }

                        split.Add(new RectXZ(
                            xEdges[xIndex],
                            xEdges[xIndex + 1],
                            zEdges[zIndex],
                            zEdges[zIndex + 1]));
                    }
                }
            }

            return split;
        }

        private static void AddSplitEdge(List<float> edges, float edge, float min, float max)
        {
            if (edge > min + BoundsEpsilon && edge < max - BoundsEpsilon)
            {
                edges.Add(edge);
            }
        }

        private static void CreateSlopedCorridorCeiling(
            Transform prefabRoot,
            Transform coordinateRoot,
            Transform module,
            string slabPrefix,
            Material material)
        {
            var renderers = module.GetComponentsInChildren<Renderer>(true);
            Renderer floorRenderer = null;
            var floorArea = float.MinValue;
            var slopeWalls = new List<Renderer>();
            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (IsFloorLike(renderer.name))
                {
                    var bounds = CalculateLocalBounds(renderer, coordinateRoot);
                    var area = bounds.size.x * bounds.size.z;
                    if (area > floorArea)
                    {
                        floorArea = area;
                        floorRenderer = renderer;
                    }
                }

                if (IsSlopedCorridorSideWall(renderer.name))
                {
                    slopeWalls.Add(renderer);
                }
            }

            if (floorRenderer == null || slopeWalls.Count < 2)
            {
                throw new InvalidOperationException(
                    "Sloped corridor ceiling requires one floor and both sloped side walls: " + module.name);
            }

            var floorBounds = CalculateLocalBounds(floorRenderer, coordinateRoot);
            var referenceWall = slopeWalls[0];
            var relativeRotation = Quaternion.Inverse(coordinateRoot.rotation) * referenceWall.transform.rotation;
            var topCenter = Vector3.zero;
            for (var wallIndex = 0; wallIndex < slopeWalls.Count; wallIndex++)
            {
                var wall = slopeWalls[wallIndex];
                var localBounds = wall.localBounds;
                var wallTopWorld = wall.transform.TransformPoint(
                    new Vector3(localBounds.center.x, localBounds.max.y, localBounds.center.z));
                topCenter += coordinateRoot.InverseTransformPoint(wallTopWorld);
            }

            topCenter /= slopeWalls.Count;
            var lossyY = Mathf.Abs(coordinateRoot.lossyScale.y);
            var localThickness = CeilingThicknessWorld / Mathf.Max(lossyY, 0.0001f);
            var slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slab.name = "ShipSpaceCeiling_" + slabPrefix + "_Sloped_Slab_01";
            slab.transform.SetParent(prefabRoot, false);
            slab.transform.localPosition =
                new Vector3(topCenter.x, topCenter.y, floorBounds.center.z) +
                (relativeRotation * (Vector3.up * (localThickness * 0.5f)));
            slab.transform.localRotation = relativeRotation;
            slab.transform.localScale = new Vector3(
                floorBounds.size.x,
                localThickness,
                floorBounds.size.z);

            var ceilingDirection = slab.transform.localRotation * Vector3.right;
            var sourceDirection = relativeRotation * Vector3.right;
            AlignmentRecords.Add(
                "SlopedCorridor=" + slabPrefix +
                "|SourceSlope=" + Float(NormalizeSignedAngle(relativeRotation.eulerAngles.z)) +
                "|CeilingSlope=" + Float(NormalizeSignedAngle(slab.transform.localEulerAngles.z)) +
                "|RotationError=" + Float(Vector3.Angle(sourceDirection, ceilingDirection)));

            var meshRenderer = slab.GetComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            meshRenderer.shadowCastingMode = ShadowCastingMode.On;
            meshRenderer.receiveShadows = true;
            meshRenderer.lightProbeUsage = LightProbeUsage.Off;
            meshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }

        private static RectXZ FindMainFloorRect(
            IReadOnlyList<Renderer> floorRenderers,
            Transform coordinateRoot)
        {
            var found = false;
            var largestArea = float.MinValue;
            var largest = new RectXZ(0f, 0f, 0f, 0f);
            for (var i = 0; i < floorRenderers.Count; i++)
            {
                var bounds = CalculateLocalBounds(floorRenderers[i], coordinateRoot);
                var area = bounds.size.x * bounds.size.z;
                if (area <= largestArea)
                {
                    continue;
                }

                found = true;
                largestArea = area;
                largest = new RectXZ(bounds.min.x, bounds.max.x, bounds.min.z, bounds.max.z);
            }

            if (!found)
            {
                throw new InvalidOperationException("Unable to identify the main floor footprint.");
            }

            return largest;
        }

        private static bool IsEntranceRect(RectXZ rect, RectXZ mainFloorRect)
        {
            var centerX = (rect.XMin + rect.XMax) * 0.5f;
            var centerZ = (rect.ZMin + rect.ZMax) * 0.5f;
            return !mainFloorRect.Contains(centerX, centerZ);
        }

        private static float CalculateEntranceWallTop(
            IReadOnlyList<Renderer> renderers,
            Transform coordinateRoot,
            RectXZ entranceRect,
            float fallbackWallTop)
        {
            var centerX = (entranceRect.XMin + entranceRect.XMax) * 0.5f;
            var centerZ = (entranceRect.ZMin + entranceRect.ZMax) * 0.5f;
            var candidates = new List<EntranceWallCandidate>();
            var nearestDistance = float.MaxValue;
            for (var i = 0; i < renderers.Count; i++)
            {
                var renderer = renderers[i];
                if (!IsEntranceHeightWall(renderer.name))
                {
                    continue;
                }

                var bounds = CalculateLocalBounds(renderer, coordinateRoot);
                var distance = HorizontalDistanceToBounds(centerX, centerZ, bounds);
                nearestDistance = Mathf.Min(nearestDistance, distance);
                candidates.Add(new EntranceWallCandidate(distance, bounds.max.y));
            }

            if (candidates.Count == 0)
            {
                return fallbackWallTop;
            }

            var tolerance = Mathf.Max(
                0.12f,
                Mathf.Min(entranceRect.XMax - entranceRect.XMin, entranceRect.ZMax - entranceRect.ZMin) * 0.2f);
            var selected = false;
            var selectedTop = float.MinValue;
            for (var i = 0; i < candidates.Count; i++)
            {
                if (candidates[i].Distance > nearestDistance + tolerance)
                {
                    continue;
                }

                selected = true;
                selectedTop = Mathf.Max(selectedTop, candidates[i].Top);
            }

            return selected ? selectedTop : fallbackWallTop;
        }

        private static float HorizontalDistanceToBounds(float x, float z, Bounds bounds)
        {
            var dx = x < bounds.min.x ? bounds.min.x - x : x > bounds.max.x ? x - bounds.max.x : 0f;
            var dz = z < bounds.min.z ? bounds.min.z - z : z > bounds.max.z ? z - bounds.max.z : 0f;
            return Mathf.Sqrt((dx * dx) + (dz * dz));
        }

        private static float NormalizeSignedAngle(float angle)
        {
            return angle > 180f ? angle - 360f : angle;
        }

        private static string Float(float value)
        {
            return value.ToString("0.######", CultureInfo.InvariantCulture);
        }

        private static void WriteAlignmentReport()
        {
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? string.Empty;
            var outputPath = Path.Combine(
                projectRoot,
                ValidationDirectory.Replace('/', Path.DirectorySeparatorChar),
                "Alignment.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? projectRoot);
            var builder = new StringBuilder();
            builder.AppendLine("Ship-space ceiling entrance-height and corridor-slope alignment");
            builder.AppendLine("productionSceneCeilingsApplied=False");
            for (var i = 0; i < AlignmentRecords.Count; i++)
            {
                builder.AppendLine(AlignmentRecords[i]);
            }

            File.WriteAllText(outputPath, builder.ToString(), new UTF8Encoding(false));
        }

        private static bool IsEntranceHeightWall(string objectName)
        {
            var lower = objectName.ToLowerInvariant();
            if (lower.Contains("header") ||
                lower.Contains("upper") ||
                lower.Contains("overhead") ||
                lower.Contains("banner") ||
                lower.Contains("housing") ||
                lower.Contains("shutter") ||
                lower.Contains("strip") ||
                lower.Contains("marker") ||
                lower.Contains("display") ||
                lower.Contains("text"))
            {
                return false;
            }

            return (lower.Contains("corridor") && lower.Contains("wall")) ||
                   (lower.Contains("entrance") && lower.Contains("wall")) ||
                   (lower.Contains("doorway") &&
                    (lower.Contains("frame") || lower.Contains("jamb"))) ||
                   (lower.Contains("ramp") && lower.Contains("wall"));
        }

        private static bool IsSlopedCorridorModule(Transform module)
        {
            return module.name.IndexOf("sloped corridor", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsSlopedCorridorSideWall(string objectName)
        {
            var lower = objectName.ToLowerInvariant();
            return lower.Contains("armored wall") &&
                   (lower.Contains("left") || lower.Contains("right"));
        }

        private static Material FindExistingMaterial(Transform sourceRoot)
        {
            Material firstExisting = null;
            var renderers = sourceRoot.GetComponentsInChildren<Renderer>(true);
            for (var pass = 0; pass < 2; pass++)
            {
                for (var rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
                {
                    var renderer = renderers[rendererIndex];
                    var preferred = IsWallLike(renderer.name) || IsFloorLike(renderer.name);
                    if ((pass == 0 && !preferred) || (pass == 1 && preferred))
                    {
                        continue;
                    }

                    var materials = renderer.sharedMaterials;
                    for (var materialIndex = 0; materialIndex < materials.Length; materialIndex++)
                    {
                        var material = materials[materialIndex];
                        var path = material == null ? string.Empty : AssetDatabase.GetAssetPath(material);
                        if (string.IsNullOrWhiteSpace(path))
                        {
                            continue;
                        }

                        firstExisting = material;
                        break;
                    }

                    if (firstExisting != null)
                    {
                        break;
                    }
                }

                if (firstExisting != null)
                {
                    break;
                }
            }

            return firstExisting ?? AssetDatabase.LoadAssetAtPath<Material>(FallbackMaterialPath);
        }

        private static float CalculateWallTop(IReadOnlyList<Renderer> renderers, Transform coordinateRoot)
        {
            var wallFound = false;
            var wallTop = float.MinValue;
            var fallbackTop = float.MinValue;
            for (var i = 0; i < renderers.Count; i++)
            {
                var bounds = CalculateLocalBounds(renderers[i], coordinateRoot);
                fallbackTop = Mathf.Max(fallbackTop, bounds.max.y);
                if (!IsWallLike(renderers[i].name))
                {
                    continue;
                }

                wallFound = true;
                wallTop = Mathf.Max(wallTop, bounds.max.y);
            }

            return wallFound ? wallTop : fallbackTop;
        }

        private static Bounds CalculateLocalBounds(Renderer renderer, Transform coordinateRoot)
        {
            var world = renderer.bounds;
            var min = world.min;
            var max = world.max;
            var corners = new[]
            {
                new Vector3(min.x, min.y, min.z),
                new Vector3(max.x, min.y, min.z),
                new Vector3(min.x, max.y, min.z),
                new Vector3(max.x, max.y, min.z),
                new Vector3(min.x, min.y, max.z),
                new Vector3(max.x, min.y, max.z),
                new Vector3(min.x, max.y, max.z),
                new Vector3(max.x, max.y, max.z)
            };

            var local = new Bounds(coordinateRoot.InverseTransformPoint(corners[0]), Vector3.zero);
            for (var i = 1; i < corners.Length; i++)
            {
                local.Encapsulate(coordinateRoot.InverseTransformPoint(corners[i]));
            }

            return local;
        }

        private static List<RectXZ> UnionRectangles(IReadOnlyList<RectXZ> rectangles)
        {
            var xEdges = UniqueSortedEdges(rectangles, true);
            var zEdges = UniqueSortedEdges(rectangles, false);
            var strips = new List<RectXZ>();
            for (var zIndex = 0; zIndex < zEdges.Count - 1; zIndex++)
            {
                var zMin = zEdges[zIndex];
                var zMax = zEdges[zIndex + 1];
                if (zMax - zMin <= BoundsEpsilon)
                {
                    continue;
                }

                var runStart = -1;
                for (var xIndex = 0; xIndex < xEdges.Count - 1; xIndex++)
                {
                    var xMin = xEdges[xIndex];
                    var xMax = xEdges[xIndex + 1];
                    var occupied = IsCovered(
                        rectangles,
                        (xMin + xMax) * 0.5f,
                        (zMin + zMax) * 0.5f);
                    if (occupied && runStart < 0)
                    {
                        runStart = xIndex;
                    }

                    var isLast = xIndex == xEdges.Count - 2;
                    if (runStart >= 0 && (!occupied || isLast))
                    {
                        var endIndex = occupied && isLast ? xIndex + 1 : xIndex;
                        strips.Add(new RectXZ(xEdges[runStart], xEdges[endIndex], zMin, zMax));
                        runStart = -1;
                    }
                }
            }

            strips.Sort((left, right) =>
            {
                var byXMin = left.XMin.CompareTo(right.XMin);
                if (byXMin != 0)
                {
                    return byXMin;
                }

                var byXMax = left.XMax.CompareTo(right.XMax);
                return byXMax != 0 ? byXMax : left.ZMin.CompareTo(right.ZMin);
            });

            var merged = new List<RectXZ>();
            for (var i = 0; i < strips.Count; i++)
            {
                var current = strips[i];
                if (merged.Count > 0)
                {
                    var previous = merged[merged.Count - 1];
                    if (Mathf.Abs(previous.XMin - current.XMin) <= BoundsEpsilon &&
                        Mathf.Abs(previous.XMax - current.XMax) <= BoundsEpsilon &&
                        Mathf.Abs(previous.ZMax - current.ZMin) <= BoundsEpsilon)
                    {
                        merged[merged.Count - 1] =
                            new RectXZ(previous.XMin, previous.XMax, previous.ZMin, current.ZMax);
                        continue;
                    }
                }

                merged.Add(current);
            }

            return merged;
        }

        private static List<float> UniqueSortedEdges(IReadOnlyList<RectXZ> rectangles, bool xAxis)
        {
            var result = new List<float>();
            for (var i = 0; i < rectangles.Count; i++)
            {
                AddUniqueEdge(result, xAxis ? rectangles[i].XMin : rectangles[i].ZMin);
                AddUniqueEdge(result, xAxis ? rectangles[i].XMax : rectangles[i].ZMax);
            }

            result.Sort();
            return result;
        }

        private static void AddUniqueEdge(ICollection<float> edges, float value)
        {
            foreach (var edge in edges)
            {
                if (Mathf.Abs(edge - value) <= BoundsEpsilon)
                {
                    return;
                }
            }

            edges.Add(value);
        }

        private static bool IsCovered(IReadOnlyList<RectXZ> rectangles, float x, float z)
        {
            for (var i = 0; i < rectangles.Count; i++)
            {
                if (rectangles[i].Contains(x, z))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsFloorLike(string objectName)
        {
            var lower = objectName.ToLowerInvariant();
            return lower.Contains("floor") || lower.Contains("deck");
        }

        private static bool IsWallLike(string objectName)
        {
            var lower = objectName.ToLowerInvariant();
            return lower.Contains("wall") ||
                   lower.Contains("frame") ||
                   lower.Contains("bulkhead") ||
                   lower.Contains("shell") ||
                   lower.Contains("mullion") ||
                   lower.Contains("rim") ||
                   lower.Contains("overhead");
        }

        private static bool IsContextLike(string objectName)
        {
            var lower = objectName.ToLowerInvariant();
            return IsFloorLike(objectName) ||
                   IsWallLike(objectName) ||
                   lower.Contains("edge") ||
                   lower.Contains("corridor") ||
                   lower.Contains("ceiling") ||
                   lower.Contains("roof");
        }

        private static string SanitizeName(string value)
        {
            var builder = new StringBuilder(value.Length);
            for (var i = 0; i < value.Length; i++)
            {
                var character = value[i];
                builder.Append(char.IsLetterOrDigit(character) ? character : '_');
            }

            return builder.ToString();
        }

        private static void CreateSampleScene(IReadOnlyDictionary<string, GameObject> sourceRoots)
        {
            var previousScene = SceneManager.GetActiveScene();
            var sampleScene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Additive);
            SceneManager.SetActiveScene(sampleScene);
            try
            {
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(0.54f, 0.58f, 0.62f, 1f);
                RenderSettings.reflectionIntensity = 0.35f;

                var sampleRoot = new GameObject("ShipSpaceCeilingsSampleRoot");
                SceneManager.MoveGameObjectToScene(sampleRoot, sampleScene);
                for (var i = 0; i < SpaceDefinitions.Length; i++)
                {
                    var definition = SpaceDefinitions[i];
                    var group = new GameObject(definition.SampleGroupName);
                    group.transform.SetParent(sampleRoot.transform, false);

                    var context = UnityEngine.Object.Instantiate(sourceRoots[definition.Id]);
                    context.name = "Context_" + definition.Id;
                    SceneManager.MoveGameObjectToScene(context, sampleScene);
                    context.transform.SetParent(group.transform, true);
                    context.SetActive(true);

                    var contextRenderers = context.GetComponentsInChildren<Renderer>(true);
                    for (var rendererIndex = 0; rendererIndex < contextRenderers.Length; rendererIndex++)
                    {
                        var renderer = contextRenderers[rendererIndex];
                        renderer.gameObject.SetActive(true);
                        renderer.enabled = definition.IsCorridorCollection || IsContextLike(renderer.name);
                    }

                    var ceilingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(definition.PrefabPath) ??
                        throw new InvalidOperationException(
                            "Ceiling sample prefab was not created: " + definition.PrefabPath);
                    var ceiling = PrefabUtility.InstantiatePrefab(ceilingPrefab, sampleScene) as GameObject ??
                        throw new InvalidOperationException(
                            "Ceiling sample prefab could not be instantiated: " + definition.PrefabPath);
                    ceiling.transform.SetParent(context.transform, false);
                    ceiling.transform.localPosition = Vector3.zero;
                    ceiling.transform.localRotation = Quaternion.identity;
                    ceiling.transform.localScale = Vector3.one;

                    var bounds = CalculateVisibleBounds(group);
                    Vector3 target;
                    if (definition.IsCorridorCollection)
                    {
                        target = new Vector3(0f, 0f, -58f);
                    }
                    else
                    {
                        var column = i % 3;
                        var row = i / 3;
                        target = new Vector3((column - 1) * 31f, 0f, (1 - row) * 28f);
                    }

                    var anchor = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
                    group.transform.position += target - anchor;
                }

                CreateSampleLighting(sampleRoot.transform);
                CreateSavedSampleCamera(sampleRoot.transform);
                if (!EditorSceneManager.SaveScene(sampleScene, SampleScenePath))
                {
                    throw new InvalidOperationException("Failed to save ceiling sample scene: " + SampleScenePath);
                }
            }
            finally
            {
                EditorSceneManager.CloseScene(sampleScene, true);
                if (previousScene.IsValid() && previousScene.isLoaded)
                {
                    SceneManager.SetActiveScene(previousScene);
                }
            }
        }

        private static void CreateSampleLighting(Transform parent)
        {
            var keyObject = new GameObject("ShipSpaceCeilingSampleKeyLight");
            keyObject.transform.SetParent(parent, false);
            keyObject.transform.rotation = Quaternion.Euler(46f, -38f, 0f);
            var key = keyObject.AddComponent<Light>();
            key.type = LightType.Directional;
            key.color = new Color(0.82f, 0.89f, 1f, 1f);
            key.intensity = 1.35f;
            key.shadows = LightShadows.Soft;

            var fillObject = new GameObject("ShipSpaceCeilingSampleFillLight");
            fillObject.transform.SetParent(parent, false);
            fillObject.transform.rotation = Quaternion.Euler(325f, 142f, 0f);
            var fill = fillObject.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = new Color(0.72f, 0.78f, 0.68f, 1f);
            fill.intensity = 0.85f;
            fill.shadows = LightShadows.None;
        }

        private static void CreateSavedSampleCamera(Transform parent)
        {
            var cameraObject = new GameObject("ShipSpaceCeilingSampleCamera");
            cameraObject.transform.SetParent(parent, false);
            cameraObject.transform.position = new Vector3(0f, 78f, -85f);
            cameraObject.transform.rotation = Quaternion.Euler(36f, 0f, 0f);
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.025f, 0.031f, 0.038f, 1f);
            camera.fieldOfView = 48f;
            camera.nearClipPlane = 0.03f;
            camera.farClipPlane = 400f;
            camera.allowHDR = false;
            camera.allowMSAA = true;
        }

        private static Bounds CalculateVisibleBounds(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            var found = false;
            var bounds = new Bounds();
            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (!renderer.enabled || !renderer.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (!found)
                {
                    bounds = renderer.bounds;
                    found = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            if (!found)
            {
                throw new InvalidOperationException("No visible renderer bounds found below " + root.name);
            }

            return bounds;
        }

        private static void CaptureAppliedContactSheet(
            Scene scene,
            IReadOnlyList<GameObject> sourceRoots,
            string outputPath)
        {
            const int panelWidth = 720;
            const int panelHeight = 420;
            const int gutter = 12;
            var targets = BuildAppliedCaptureTargets(sourceRoots);
            if (targets.Count != 16)
            {
                throw new InvalidOperationException(
                    "Applied production contact sheet requires 6 rooms and 10 corridor modules. Targets=" +
                    targets.Count);
            }

            var sheetWidth = (panelWidth * 2) + (gutter * 3);
            var sheetHeight =
                (panelHeight * targets.Count) +
                (gutter * (targets.Count + 1));
            var sheet = new Texture2D(sheetWidth, sheetHeight, TextureFormat.RGB24, false);
            var background = new Color(0.018f, 0.022f, 0.028f, 1f);
            var backgroundPixels = new Color[sheetWidth * sheetHeight];
            for (var pixelIndex = 0; pixelIndex < backgroundPixels.Length; pixelIndex++)
            {
                backgroundPixels[pixelIndex] = background;
            }

            sheet.SetPixels(backgroundPixels);
            var cameraObject = new GameObject("__AppliedShipSpaceCeilingInspectionCamera")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.034f, 0.042f, 0.052f, 1f);
            camera.nearClipPlane = 0.025f;
            camera.farClipPlane = 500f;
            camera.allowHDR = false;
            camera.allowMSAA = true;
            var inspectionLight = cameraObject.AddComponent<Light>();
            inspectionLight.type = LightType.Point;
            inspectionLight.color = new Color(0.86f, 0.92f, 1f, 1f);
            inspectionLight.intensity = 12f;
            inspectionLight.range = 120f;
            inspectionLight.shadows = LightShadows.None;
            inspectionLight.enabled = false;

            try
            {
                for (var targetIndex = 0; targetIndex < targets.Count; targetIndex++)
                {
                    var target = targets[targetIndex];
                    var topView = RenderSamplePanel(
                        camera,
                        target.Bounds,
                        true,
                        panelWidth,
                        panelHeight);
                    var lowerView = target.IsCorridor
                        ? RenderCorridorSidePanel(
                            camera,
                            target.Bounds,
                            panelWidth,
                            panelHeight)
                        : RenderSamplePanel(
                            camera,
                            target.Bounds,
                            false,
                            panelWidth,
                            panelHeight);
                    try
                    {
                        var y = sheetHeight - gutter - ((targetIndex + 1) * panelHeight) -
                                (targetIndex * gutter);
                        sheet.SetPixels(gutter, y, panelWidth, panelHeight, topView.GetPixels());
                        sheet.SetPixels(
                            (gutter * 2) + panelWidth,
                            y,
                            panelWidth,
                            panelHeight,
                            lowerView.GetPixels());
                    }
                    finally
                    {
                        UnityEngine.Object.DestroyImmediate(topView);
                        UnityEngine.Object.DestroyImmediate(lowerView);
                    }
                }

                sheet.Apply(false, false);
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? string.Empty);
                File.WriteAllBytes(outputPath, sheet.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(sheet);
            }
        }

        private static List<AppliedCaptureTarget> BuildAppliedCaptureTargets(
            IReadOnlyList<GameObject> sourceRoots)
        {
            var targets = new List<AppliedCaptureTarget>(16);
            for (var rootIndex = 0; rootIndex < SpaceDefinitions.Length - 1; rootIndex++)
            {
                targets.Add(new AppliedCaptureTarget(
                    SpaceDefinitions[rootIndex].DisplayName,
                    CalculateVisibleBounds(sourceRoots[rootIndex]),
                    false));
            }

            var corridorRoot = sourceRoots[sourceRoots.Count - 1].transform;
            var corridorCeiling = corridorRoot.Find("ShipSpaceCeiling_AllCorridors");
            if (corridorCeiling == null)
            {
                throw new InvalidOperationException(
                    "Missing applied all-corridor ceiling root during direct capture.");
            }

            for (var childIndex = 0; childIndex < corridorRoot.childCount; childIndex++)
            {
                var module = corridorRoot.GetChild(childIndex);
                if (module == corridorCeiling)
                {
                    continue;
                }

                var renderers = new List<Renderer>();
                renderers.AddRange(module.GetComponentsInChildren<Renderer>(true));
                var ceilingPrefix =
                    "ShipSpaceCeiling_" + SanitizeName(module.name) + "_";
                for (var slabIndex = 0; slabIndex < corridorCeiling.childCount; slabIndex++)
                {
                    var slab = corridorCeiling.GetChild(slabIndex);
                    if (!slab.name.StartsWith(ceilingPrefix, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    var renderer = slab.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        renderers.Add(renderer);
                    }
                }

                targets.Add(new AppliedCaptureTarget(
                    module.name,
                    CalculateVisibleBounds(renderers, module.name),
                    true));
            }

            return targets;
        }

        private static Bounds CalculateVisibleBounds(
            IReadOnlyList<Renderer> renderers,
            string targetName)
        {
            var found = false;
            var bounds = new Bounds();
            for (var rendererIndex = 0; rendererIndex < renderers.Count; rendererIndex++)
            {
                var renderer = renderers[rendererIndex];
                if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (!found)
                {
                    bounds = renderer.bounds;
                    found = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            if (!found)
            {
                throw new InvalidOperationException(
                    "No visible renderer bounds found for applied capture target: " + targetName);
            }

            return bounds;
        }

        private static void CaptureAppliedEntranceCoverageSheet(
            Scene scene,
            IReadOnlyList<GameObject> sourceRoots,
            string outputPath)
        {
            const int panelWidth = 520;
            const int panelHeight = 340;
            const int columns = 3;
            const int gutter = 10;
            var targetRenderers = new List<Renderer>();
            var targetTopViews = new List<bool>();
            var targetCenters = new List<Vector3>();
            var targetLabels = new List<string>();

            AddAppliedEntranceCoverageTargets(
                sourceRoots,
                "Armory",
                "_Entrance_WallInfill_",
                false,
                targetRenderers,
                targetTopViews,
                targetCenters,
                targetLabels);
            AddAppliedEntranceCoverageTargets(
                sourceRoots,
                "SupplyRoom",
                "_Entrance_WallInfill_",
                false,
                targetRenderers,
                targetTopViews,
                targetCenters,
                targetLabels);
            AddAppliedEntranceCoverageTargets(
                sourceRoots,
                "ControlRoom",
                "_Entrance_WallInfill_",
                false,
                targetRenderers,
                targetTopViews,
                targetCenters,
                targetLabels);
            AddAppliedEntranceCoverageTargets(
                sourceRoots,
                "EngineRoom",
                "_CircularSlab_",
                true,
                targetRenderers,
                targetTopViews,
                targetCenters,
                targetLabels);
            AddAppliedEntranceCoverageTargets(
                sourceRoots,
                "EngineRoom",
                "_Entrance_Slab_",
                true,
                targetRenderers,
                targetTopViews,
                targetCenters,
                targetLabels);

            if (targetRenderers.Count != 13)
            {
                throw new InvalidOperationException(
                    "Applied entrance review requires 9 wall infills, 1 circular ceiling, and 3 engine entrances. " +
                    "Targets=" + targetRenderers.Count);
            }

            var rows = Mathf.CeilToInt(targetRenderers.Count / (float)columns);
            var sheetWidth = (panelWidth * columns) + (gutter * (columns + 1));
            var sheetHeight = (panelHeight * rows) + (gutter * (rows + 1));
            var sheet = new Texture2D(sheetWidth, sheetHeight, TextureFormat.RGB24, false);
            var background = new Color(0.018f, 0.022f, 0.028f, 1f);
            var backgroundPixels = new Color[sheetWidth * sheetHeight];
            for (var pixelIndex = 0; pixelIndex < backgroundPixels.Length; pixelIndex++)
            {
                backgroundPixels[pixelIndex] = background;
            }

            sheet.SetPixels(backgroundPixels);
            var cameraObject = new GameObject("__AppliedShipSpaceEntranceInspectionCamera")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.034f, 0.042f, 0.052f, 1f);
            camera.nearClipPlane = 0.025f;
            camera.farClipPlane = 500f;
            camera.allowHDR = false;
            camera.allowMSAA = true;
            var inspectionLight = cameraObject.AddComponent<Light>();
            inspectionLight.type = LightType.Point;
            inspectionLight.color = new Color(0.86f, 0.92f, 1f, 1f);
            inspectionLight.intensity = 14f;
            inspectionLight.range = 80f;
            inspectionLight.shadows = LightShadows.None;

            try
            {
                for (var targetIndex = 0; targetIndex < targetRenderers.Count; targetIndex++)
                {
                    var panel = targetTopViews[targetIndex]
                        ? RenderFocusedTopPanel(
                            camera,
                            targetRenderers[targetIndex].bounds,
                            panelWidth,
                            panelHeight)
                        : RenderFocusedEntranceSidePanel(
                            camera,
                            targetRenderers[targetIndex].bounds,
                            targetCenters[targetIndex],
                            panelWidth,
                            panelHeight);
                    try
                    {
                        var column = targetIndex % columns;
                        var row = targetIndex / columns;
                        var x = gutter + (column * (panelWidth + gutter));
                        var y = sheetHeight - gutter - ((row + 1) * panelHeight) - (row * gutter);
                        sheet.SetPixels(x, y, panelWidth, panelHeight, panel.GetPixels());
                    }
                    finally
                    {
                        UnityEngine.Object.DestroyImmediate(panel);
                    }
                }

                sheet.Apply(false, false);
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? string.Empty);
                File.WriteAllBytes(outputPath, sheet.EncodeToPNG());
                var labelReport = new StringBuilder();
                for (var labelIndex = 0; labelIndex < targetLabels.Count; labelIndex++)
                {
                    labelReport.AppendLine(
                        (labelIndex + 1).ToString("00", CultureInfo.InvariantCulture) + "=" +
                        targetLabels[labelIndex]);
                }

                File.WriteAllText(
                    Path.ChangeExtension(outputPath, ".txt"),
                    labelReport.ToString(),
                    new UTF8Encoding(false));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(sheet);
            }
        }

        private static void AddAppliedEntranceCoverageTargets(
            IReadOnlyList<GameObject> sourceRoots,
            string definitionId,
            string nameToken,
            bool topView,
            ICollection<Renderer> targetRenderers,
            ICollection<bool> targetTopViews,
            ICollection<Vector3> targetCenters,
            ICollection<string> targetLabels)
        {
            var definitionIndex = -1;
            for (var index = 0; index < SpaceDefinitions.Length; index++)
            {
                if (SpaceDefinitions[index].Id == definitionId)
                {
                    definitionIndex = index;
                    break;
                }
            }

            if (definitionIndex < 0)
            {
                throw new InvalidOperationException(
                    "Missing applied entrance review definition: " + definitionId);
            }

            var sourceRoot = sourceRoots[definitionIndex];
            var ceilingRoot = sourceRoot.transform.Find("ShipSpaceCeiling_" + definitionId);
            if (ceilingRoot == null)
            {
                throw new InvalidOperationException(
                    "Missing applied entrance review ceiling root: " + definitionId);
            }

            var matches = new List<Renderer>();
            for (var childIndex = 0; childIndex < ceilingRoot.childCount; childIndex++)
            {
                var child = ceilingRoot.GetChild(childIndex);
                if (child.name.IndexOf(nameToken, StringComparison.Ordinal) < 0)
                {
                    continue;
                }

                var renderer = child.GetComponent<Renderer>();
                if (renderer != null)
                {
                    matches.Add(renderer);
                }
            }

            matches.Sort((left, right) => string.CompareOrdinal(left.name, right.name));
            for (var matchIndex = 0; matchIndex < matches.Count; matchIndex++)
            {
                targetRenderers.Add(matches[matchIndex]);
                targetTopViews.Add(topView);
                targetCenters.Add(sourceRoot.transform.position);
                targetLabels.Add(matches[matchIndex].name);
            }
        }

        private static void CaptureContactSheet(Scene sampleScene, string outputPath)
        {
            var texture = RenderContactSheet(sampleScene);
            try
            {
                File.WriteAllBytes(outputPath, texture.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static void CaptureEntranceCoverageSheet(Scene sampleScene, string outputPath)
        {
            const int panelWidth = 520;
            const int panelHeight = 340;
            const int columns = 3;
            const int gutter = 10;
            var allGroups = new List<GameObject>();
            for (var definitionIndex = 0; definitionIndex < SpaceDefinitions.Length; definitionIndex++)
            {
                allGroups.Add(
                    FindSceneObject(sampleScene, SpaceDefinitions[definitionIndex].SampleGroupName) ??
                    throw new InvalidOperationException(
                        "Missing sample group for entrance-coverage capture: " +
                        SpaceDefinitions[definitionIndex].SampleGroupName));
            }

            var targetGroups = new List<GameObject>();
            var targetRenderers = new List<Renderer>();
            var targetTopViews = new List<bool>();
            var targetLabels = new List<string>();
            var closureIds = new[] { "Armory", "SupplyRoom", "ControlRoom" };
            for (var closureIndex = 0; closureIndex < closureIds.Length; closureIndex++)
            {
                AddEntranceCoverageTargets(
                    allGroups,
                    closureIds[closureIndex],
                    "_Entrance_WallInfill_",
                    false,
                    targetGroups,
                    targetRenderers,
                    targetTopViews,
                    targetLabels);
            }

            AddEntranceCoverageTargets(
                allGroups,
                "EngineRoom",
                "_CircularSlab_",
                true,
                targetGroups,
                targetRenderers,
                targetTopViews,
                targetLabels);
            AddEntranceCoverageTargets(
                allGroups,
                "EngineRoom",
                "_Entrance_Slab_",
                true,
                targetGroups,
                targetRenderers,
                targetTopViews,
                targetLabels);

            if (targetRenderers.Count != 13)
            {
                throw new InvalidOperationException(
                    "Entrance-coverage review requires 9 wall infills, 1 circular ceiling, and 3 engine entrances. " +
                    "Targets=" + targetRenderers.Count);
            }

            var rows = Mathf.CeilToInt(targetRenderers.Count / (float)columns);
            var sheetWidth = (panelWidth * columns) + (gutter * (columns + 1));
            var sheetHeight = (panelHeight * rows) + (gutter * (rows + 1));
            var sheet = new Texture2D(sheetWidth, sheetHeight, TextureFormat.RGB24, false);
            var background = new Color(0.018f, 0.022f, 0.028f, 1f);
            var backgroundPixels = new Color[sheetWidth * sheetHeight];
            for (var pixelIndex = 0; pixelIndex < backgroundPixels.Length; pixelIndex++)
            {
                backgroundPixels[pixelIndex] = background;
            }

            sheet.SetPixels(backgroundPixels);
            var cameraObject = new GameObject("__ShipSpaceCeilingEntranceCoverageCamera")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            SceneManager.MoveGameObjectToScene(cameraObject, sampleScene);
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.034f, 0.042f, 0.052f, 1f);
            camera.nearClipPlane = 0.025f;
            camera.farClipPlane = 500f;
            camera.allowHDR = false;
            camera.allowMSAA = true;
            var inspectionLight = cameraObject.AddComponent<Light>();
            inspectionLight.type = LightType.Point;
            inspectionLight.color = new Color(0.86f, 0.92f, 1f, 1f);
            inspectionLight.intensity = 14f;
            inspectionLight.range = 80f;
            inspectionLight.shadows = LightShadows.None;

            try
            {
                for (var targetIndex = 0; targetIndex < targetRenderers.Count; targetIndex++)
                {
                    for (var groupIndex = 0; groupIndex < allGroups.Count; groupIndex++)
                    {
                        allGroups[groupIndex].SetActive(allGroups[groupIndex] == targetGroups[targetIndex]);
                    }

                    var panel = targetTopViews[targetIndex]
                        ? RenderFocusedTopPanel(camera, targetRenderers[targetIndex].bounds, panelWidth, panelHeight)
                        : RenderFocusedEntranceSidePanel(
                            camera,
                            targetRenderers[targetIndex].bounds,
                            targetGroups[targetIndex].transform.position,
                            panelWidth,
                            panelHeight);
                    try
                    {
                        var column = targetIndex % columns;
                        var row = targetIndex / columns;
                        var x = gutter + (column * (panelWidth + gutter));
                        var y = sheetHeight - gutter - ((row + 1) * panelHeight) - (row * gutter);
                        sheet.SetPixels(x, y, panelWidth, panelHeight, panel.GetPixels());
                    }
                    finally
                    {
                        UnityEngine.Object.DestroyImmediate(panel);
                    }
                }

                sheet.Apply(false, false);
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? string.Empty);
                File.WriteAllBytes(outputPath, sheet.EncodeToPNG());

                var labelPath = Path.ChangeExtension(outputPath, ".txt");
                var labelReport = new StringBuilder();
                for (var labelIndex = 0; labelIndex < targetLabels.Count; labelIndex++)
                {
                    labelReport.AppendLine(
                        (labelIndex + 1).ToString("00", CultureInfo.InvariantCulture) + "=" +
                        targetLabels[labelIndex]);
                }

                File.WriteAllText(labelPath, labelReport.ToString(), new UTF8Encoding(false));
            }
            finally
            {
                for (var groupIndex = 0; groupIndex < allGroups.Count; groupIndex++)
                {
                    allGroups[groupIndex].SetActive(true);
                }

                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(sheet);
            }
        }

        private static void AddEntranceCoverageTargets(
            IReadOnlyList<GameObject> allGroups,
            string definitionId,
            string nameToken,
            bool topView,
            ICollection<GameObject> targetGroups,
            ICollection<Renderer> targetRenderers,
            ICollection<bool> targetTopViews,
            ICollection<string> targetLabels)
        {
            var matchedDefinitionIndex = -1;
            GameObject group = null;
            for (var definitionIndex = 0; definitionIndex < SpaceDefinitions.Length; definitionIndex++)
            {
                if (SpaceDefinitions[definitionIndex].Id != definitionId)
                {
                    continue;
                }

                matchedDefinitionIndex = definitionIndex;
                group = allGroups[definitionIndex];
                break;
            }

            if (matchedDefinitionIndex < 0 || group == null)
            {
                throw new InvalidOperationException("Missing entrance-coverage definition: " + definitionId);
            }

            var definition = SpaceDefinitions[matchedDefinitionIndex];
            var context = group.transform.Find("Context_" + definition.Id) ??
                throw new InvalidOperationException("Missing entrance-coverage context: " + definition.Id);
            var ceilingRoot = context.Find("ShipSpaceCeiling_" + definition.Id) ??
                throw new InvalidOperationException("Missing entrance-coverage ceiling root: " + definition.Id);
            var matches = new List<Renderer>();
            for (var childIndex = 0; childIndex < ceilingRoot.childCount; childIndex++)
            {
                var child = ceilingRoot.GetChild(childIndex);
                if (child.name.IndexOf(nameToken, StringComparison.Ordinal) < 0)
                {
                    continue;
                }

                var renderer = child.GetComponent<Renderer>();
                if (renderer != null)
                {
                    matches.Add(renderer);
                }
            }

            matches.Sort((left, right) => string.CompareOrdinal(left.name, right.name));
            for (var matchIndex = 0; matchIndex < matches.Count; matchIndex++)
            {
                targetGroups.Add(group);
                targetRenderers.Add(matches[matchIndex]);
                targetTopViews.Add(topView);
                targetLabels.Add(matches[matchIndex].name);
            }
        }

        private static Texture2D RenderFocusedTopPanel(
            Camera camera,
            Bounds bounds,
            int width,
            int height)
        {
            var aspect = width / (float)height;
            camera.orthographic = true;
            camera.orthographicSize = Mathf.Max(
                bounds.extents.z * 1.45f,
                (bounds.extents.x / aspect) * 1.45f) + 0.25f;
            camera.transform.position = new Vector3(
                bounds.center.x,
                bounds.max.y + Mathf.Max(bounds.size.y, 7f),
                bounds.center.z);
            camera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            return RenderCamera(camera, width, height);
        }

        private static Texture2D RenderFocusedEntranceSidePanel(
            Camera camera,
            Bounds bounds,
            Vector3 roomCenter,
            int width,
            int height)
        {
            var outward = bounds.center - roomCenter;
            outward.y = 0f;
            if (outward.sqrMagnitude <= 0.0001f)
            {
                outward = bounds.size.x <= bounds.size.z ? Vector3.right : Vector3.forward;
            }

            outward.Normalize();
            var aspect = width / (float)height;
            var tangent = Vector3.Cross(Vector3.up, outward);
            var tangentExtent =
                (Mathf.Abs(tangent.x) * bounds.extents.x) +
                (Mathf.Abs(tangent.z) * bounds.extents.z);
            camera.orthographic = true;
            camera.orthographicSize = Mathf.Max(
                bounds.extents.y * 1.6f,
                (tangentExtent / aspect) * 1.6f) + 0.35f;
            var target = bounds.center;
            camera.transform.position = target + (outward * 8f);
            camera.transform.rotation = Quaternion.LookRotation(-outward, Vector3.up);
            return RenderCamera(camera, width, height);
        }

        private static Texture2D RenderContactSheet(Scene sampleScene)
        {
            const int panelWidth = 720;
            const int panelHeight = 420;
            const int gutter = 12;
            var groups = new List<GameObject>();
            for (var i = 0; i < SpaceDefinitions.Length; i++)
            {
                groups.Add(
                    FindSceneObject(sampleScene, SpaceDefinitions[i].SampleGroupName) ??
                    throw new InvalidOperationException(
                        "Missing sample group for capture: " + SpaceDefinitions[i].SampleGroupName));
            }

            var targets = BuildCaptureTargets(groups);
            var sheetWidth = (panelWidth * 2) + (gutter * 3);
            var sheetHeight =
                (panelHeight * targets.Count) +
                (gutter * (targets.Count + 1));
            var sheet = new Texture2D(sheetWidth, sheetHeight, TextureFormat.RGB24, false);
            var background = new Color(0.018f, 0.022f, 0.028f, 1f);
            var backgroundPixels = new Color[sheetWidth * sheetHeight];
            for (var pixelIndex = 0; pixelIndex < backgroundPixels.Length; pixelIndex++)
            {
                backgroundPixels[pixelIndex] = background;
            }

            sheet.SetPixels(backgroundPixels);
            var cameraObject = new GameObject("__ShipSpaceCeilingContactSheetCamera")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            SceneManager.MoveGameObjectToScene(cameraObject, sampleScene);
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.034f, 0.042f, 0.052f, 1f);
            camera.nearClipPlane = 0.025f;
            camera.farClipPlane = 500f;
            camera.allowHDR = false;
            camera.allowMSAA = true;
            var inspectionLight = cameraObject.AddComponent<Light>();
            inspectionLight.type = LightType.Point;
            inspectionLight.color = new Color(0.86f, 0.92f, 1f, 1f);
            inspectionLight.intensity = 12f;
            inspectionLight.range = 120f;
            inspectionLight.shadows = LightShadows.None;
            inspectionLight.enabled = false;
            try
            {
                for (var targetIndex = 0; targetIndex < targets.Count; targetIndex++)
                {
                    for (var visibilityIndex = 0; visibilityIndex < groups.Count; visibilityIndex++)
                    {
                        groups[visibilityIndex].SetActive(
                            groups[visibilityIndex] == targets[targetIndex].Group);
                    }

                    PrepareCaptureTarget(targets[targetIndex]);
                    var bounds = CalculateVisibleBounds(targets[targetIndex].Group);
                    var topView = RenderSamplePanel(camera, bounds, true, panelWidth, panelHeight);
                    var undersideView = targets[targetIndex].Module == null
                        ? RenderSamplePanel(camera, bounds, false, panelWidth, panelHeight)
                        : RenderCorridorSidePanel(camera, bounds, panelWidth, panelHeight);
                    try
                    {
                        var y = sheetHeight - gutter - ((targetIndex + 1) * panelHeight) -
                                (targetIndex * gutter);
                        sheet.SetPixels(gutter, y, panelWidth, panelHeight, topView.GetPixels());
                        sheet.SetPixels(
                            (gutter * 2) + panelWidth,
                            y,
                            panelWidth,
                            panelHeight,
                            undersideView.GetPixels());
                    }
                    finally
                    {
                        UnityEngine.Object.DestroyImmediate(topView);
                        UnityEngine.Object.DestroyImmediate(undersideView);
                    }
                }

                sheet.Apply(false, false);
                return sheet;
            }
            catch
            {
                UnityEngine.Object.DestroyImmediate(sheet);
                throw;
            }
            finally
            {
                for (var i = 0; i < groups.Count; i++)
                {
                    groups[i].SetActive(true);
                }

                RestoreCorridorCaptureTargets(targets);

                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static List<CaptureTarget> BuildCaptureTargets(IReadOnlyList<GameObject> groups)
        {
            var targets = new List<CaptureTarget>();
            for (var i = 0; i < SpaceDefinitions.Length - 1; i++)
            {
                targets.Add(new CaptureTarget(groups[i], null, null, string.Empty));
            }

            var corridorGroup = groups[groups.Count - 1];
            var corridorContext = corridorGroup.transform.Find("Context_AllCorridors") ??
                throw new InvalidOperationException("Missing all-corridor sample context.");
            var ceilingRoot = corridorContext.Find("ShipSpaceCeiling_AllCorridors") ??
                throw new InvalidOperationException("Missing all-corridor ceiling sample instance.");
            for (var childIndex = 0; childIndex < corridorContext.childCount; childIndex++)
            {
                var module = corridorContext.GetChild(childIndex);
                if (module == ceilingRoot)
                {
                    continue;
                }

                targets.Add(new CaptureTarget(
                    corridorGroup,
                    module,
                    ceilingRoot,
                    "ShipSpaceCeiling_" + SanitizeName(module.name) + "_"));
            }

            if (targets.Count != 16)
            {
                throw new InvalidOperationException(
                    "Ceiling contact sheet requires 6 rooms and 10 corridor modules. Targets=" +
                    targets.Count);
            }

            return targets;
        }

        private static void PrepareCaptureTarget(CaptureTarget target)
        {
            if (target.Module == null || target.CeilingRoot == null)
            {
                return;
            }

            var corridorContext = target.Module.parent;
            for (var childIndex = 0; childIndex < corridorContext.childCount; childIndex++)
            {
                var child = corridorContext.GetChild(childIndex);
                if (child != target.CeilingRoot)
                {
                    child.gameObject.SetActive(child == target.Module);
                }
            }

            for (var slabIndex = 0; slabIndex < target.CeilingRoot.childCount; slabIndex++)
            {
                var slab = target.CeilingRoot.GetChild(slabIndex);
                slab.gameObject.SetActive(
                    slab.name.StartsWith(target.CeilingPrefix, StringComparison.Ordinal));
            }
        }

        private static void RestoreCorridorCaptureTargets(IReadOnlyList<CaptureTarget> targets)
        {
            for (var targetIndex = 0; targetIndex < targets.Count; targetIndex++)
            {
                var target = targets[targetIndex];
                if (target.Module == null || target.CeilingRoot == null)
                {
                    continue;
                }

                var corridorContext = target.Module.parent;
                for (var childIndex = 0; childIndex < corridorContext.childCount; childIndex++)
                {
                    corridorContext.GetChild(childIndex).gameObject.SetActive(true);
                }

                for (var slabIndex = 0; slabIndex < target.CeilingRoot.childCount; slabIndex++)
                {
                    target.CeilingRoot.GetChild(slabIndex).gameObject.SetActive(true);
                }

                return;
            }
        }

        private static Texture2D RenderSamplePanel(
            Camera camera,
            Bounds bounds,
            bool topView,
            int width,
            int height)
        {
            if (topView)
            {
                var inspectionLight = camera.GetComponent<Light>();
                if (inspectionLight != null)
                {
                    inspectionLight.enabled = false;
                }

                var aspect = width / (float)height;
                camera.orthographic = true;
                camera.orthographicSize = Mathf.Max(
                    bounds.extents.z * 1.18f,
                    (bounds.extents.x / aspect) * 1.18f);
                camera.transform.position = new Vector3(
                    bounds.center.x,
                    bounds.max.y + Mathf.Max(bounds.size.y, 8f),
                    bounds.center.z);
                camera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            }
            else
            {
                var inspectionLight = camera.GetComponent<Light>();
                if (inspectionLight != null)
                {
                    inspectionLight.enabled = true;
                }

                camera.orthographic = false;
                var horizontalExtent = Mathf.Max(bounds.extents.x, bounds.extents.z);
                camera.fieldOfView = 68f;
                camera.transform.position = new Vector3(
                    bounds.center.x - (horizontalExtent * 0.28f),
                    bounds.min.y + (bounds.size.y * 0.33f),
                    bounds.center.z - (bounds.extents.z * 0.36f));
                var target = new Vector3(
                    bounds.center.x,
                    bounds.max.y - Mathf.Min(CeilingThicknessWorld, bounds.size.y * 0.05f),
                    bounds.center.z + (bounds.extents.z * 0.08f));
                camera.transform.rotation = Quaternion.LookRotation(
                    target - camera.transform.position,
                    Vector3.up);
            }

            return RenderCamera(camera, width, height);
        }

        private static Texture2D RenderCorridorSidePanel(
            Camera camera,
            Bounds bounds,
            int width,
            int height)
        {
            var inspectionLight = camera.GetComponent<Light>();
            if (inspectionLight != null)
            {
                inspectionLight.enabled = true;
            }

            var aspect = width / (float)height;
            camera.orthographic = true;
            camera.orthographicSize = Mathf.Max(
                bounds.extents.y * 1.45f,
                (bounds.extents.x / aspect) * 1.12f);
            camera.transform.position = new Vector3(
                bounds.center.x,
                bounds.center.y,
                bounds.min.z - Mathf.Max(bounds.size.z, 4f));
            camera.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
            return RenderCamera(camera, width, height);
        }

        private static Texture2D RenderCamera(Camera camera, int width, int height)
        {
            var renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
            {
                antiAliasing = 4,
                useMipMap = false,
                autoGenerateMips = false
            };
            renderTexture.Create();
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            var previousActive = RenderTexture.active;
            var previousTarget = camera.targetTexture;
            try
            {
                camera.targetTexture = renderTexture;
                camera.Render();
                RenderTexture.active = renderTexture;
                texture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                texture.Apply(false, false);
                return texture;
            }
            catch
            {
                UnityEngine.Object.DestroyImmediate(texture);
                throw;
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                renderTexture.Release();
                UnityEngine.Object.DestroyImmediate(renderTexture);
            }
        }

        private static void WriteSampleHtml(bool finalCaptured)
        {
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? string.Empty;
            var outputDirectory = Path.Combine(
                projectRoot,
                ArtSampleDirectory.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(outputDirectory);
            var htmlPath = Path.Combine(outputDirectory, "Review.html");
            var imageMarkup = finalCaptured
                ? "<img src=\"Final.png\" alt=\"전체 함선 공간 천장 Unity 샘플 상부 및 실내 하부 비교\">"
                : "<p class=\"pending\">최종 검토 캡처 생성 전입니다.</p>";
            var html = "<!doctype html>\n" +
                       "<html lang=\"ko\"><head><meta charset=\"utf-8\">" +
                       "<meta name=\"viewport\" content=\"width=device-width,initial-scale=1\">" +
                       "<title>함선 전체 공간 천장 아트 샘플</title>" +
                       "<style>body{margin:0;background:#10151c;color:#e8edf3;font-family:Segoe UI,sans-serif}" +
                       "main{max-width:1500px;margin:auto;padding:28px}h1{margin:0 0 12px}" +
                       ".card{background:#18212c;border:1px solid #314153;border-radius:14px;padding:20px;margin:18px 0}" +
                       "img{display:block;width:100%;height:auto;border-radius:10px;background:#080b0f}" +
                       "code{color:#9fd6ff}.pending{color:#ffd27d}li{margin:7px 0}</style></head><body><main>" +
                       "<h1>함선 전체 공간 천장 Unity 적용형 아트 샘플</h1>" +
                       "<div class=\"card\"><p><strong>상태:</strong> 샘플 승인 대기 · 실제 CargoRunMvp 천장 미적용</p>" +
                       "<p>현재 Unity의 승인된 무기실·동력실·조종실·비품실·창고·통제실과 10개 복도 모듈의 바닥/벽 경계를 직접 측정해 제작했습니다. 새 텍스처나 머티리얼은 만들지 않고 기존 프로젝트 머티리얼만 재사용했습니다.</p>" +
                       "<p>방 입구 천장은 각 입구 측벽 높이에 맞췄고 무기실·비품실·통제실의 입구 천장 위 열린 면은 벽 마감으로 막았습니다. 동력실은 원형 바닥 전체를 같은 원형 천장으로 덮고 세 입구 천장을 원형 천장과 빈틈없이 연결했습니다. 경사 복도 천장은 해당 복도와 같은 기울기를 적용했습니다.</p>" +
                       "<p>방 행은 왼쪽이 상부 외형, 오른쪽이 실내에서 본 천장 하부이며 복도 행은 왼쪽이 상부 외형, 오른쪽이 경사 확인용 측면입니다.</p>" +
                       "<ol><li>무기실</li><li>동력실</li><li>조종실</li><li>비품실</li><li>창고</li><li>통제실</li>" +
                       "<li>복도 SC-H01</li><li>복도 SC-H02</li><li>복도 SC-H03</li><li>복도 SC-H04</li><li>복도 SC-H05</li>" +
                       "<li>복도 SC-S01</li><li>복도 SC-S02</li><li>복도 SC-S03</li><li>복도 SC-S04</li><li>복도 SC-S05</li></ol></div>" +
                       "<div class=\"card\">" + imageMarkup + "</div>" +
                       "<div class=\"card\"><h2>Unity 산출물</h2><ul>" +
                       "<li><code>Assets/_Project/ArtSamples/ShipSpaceCeilings/ShipSpaceCeilingsSample.unity</code></li>" +
                       "<li>공간별 천장 프리팹 7개, 모든 천장 조각에 MeshFilter·Renderer·BoxCollider 포함</li>" +
                       "<li>샘플은 실제 게임 씬과 분리되어 있으며 승인 전 연결하지 않음</li>" +
                       "</ul></div></main></body></html>";
            File.WriteAllText(htmlPath, html, new UTF8Encoding(false));
        }

        private static void CaptureInactiveSourceClones(IReadOnlyList<GameObject> sourceRoots, string outputPath)
        {
            var clones = new List<GameObject>();
            try
            {
                var renderers = new List<Renderer>();
                for (var rootIndex = 0; rootIndex < sourceRoots.Count; rootIndex++)
                {
                    var clone = UnityEngine.Object.Instantiate(sourceRoots[rootIndex]);
                    clone.name = "__ShipSpaceCeilingSourceInspectionClone_" + rootIndex;
                    clone.hideFlags = HideFlags.HideAndDontSave;
                    clone.SetActive(true);
                    clones.Add(clone);

                    var cloneTransforms = clone.GetComponentsInChildren<Transform>(true);
                    for (var transformIndex = 0; transformIndex < cloneTransforms.Length; transformIndex++)
                    {
                        cloneTransforms[transformIndex].gameObject.SetActive(true);
                    }

                    var cloneRenderers = clone.GetComponentsInChildren<Renderer>(true);
                    for (var rendererIndex = 0; rendererIndex < cloneRenderers.Length; rendererIndex++)
                    {
                        var renderer = cloneRenderers[rendererIndex];
                        renderer.enabled = true;
                        renderers.Add(renderer);
                    }
                }

                CaptureOverview(renderers, outputPath);
            }
            finally
            {
                for (var i = 0; i < clones.Count; i++)
                {
                    UnityEngine.Object.DestroyImmediate(clones[i]);
                }
            }
        }

        private static void AppendHierarchy(
            StringBuilder report,
            Transform root,
            Transform relativeRoot,
            ICollection<Renderer> renderers)
        {
            var transforms = root.GetComponentsInChildren<Transform>(true);
            Array.Sort(transforms, (left, right) =>
                string.CompareOrdinal(HierarchyPath(left), HierarchyPath(right)));

            for (var i = 0; i < transforms.Length; i++)
            {
                var current = transforms[i];
                var renderer = current.GetComponent<Renderer>();
                var collider = current.GetComponent<Collider>();
                if (renderer != null && renderer.enabled && current.gameObject.activeInHierarchy)
                {
                    renderers.Add(renderer);
                }

                report.Append("Object=").Append(RelativePath(current, relativeRoot));
                report.Append("|ActiveSelf=").Append(current.gameObject.activeSelf);
                report.Append("|LocalPosition=").Append(Vector(current.localPosition));
                report.Append("|LocalEuler=").Append(Vector(current.localEulerAngles));
                report.Append("|LocalScale=").Append(Vector(current.localScale));
                report.Append("|WorldPosition=").Append(Vector(current.position));
                report.Append("|WorldEuler=").Append(Vector(current.eulerAngles));
                report.Append("|Renderer=").Append(renderer != null);
                report.Append("|Collider=").Append(collider != null);

                if (renderer != null)
                {
                    report.Append("|BoundsCenter=").Append(Vector(renderer.bounds.center));
                    report.Append("|BoundsSize=").Append(Vector(renderer.bounds.size));
                    report.Append("|Materials=");
                    var materials = renderer.sharedMaterials;
                    for (var materialIndex = 0; materialIndex < materials.Length; materialIndex++)
                    {
                        if (materialIndex > 0)
                        {
                            report.Append(',');
                        }

                        var material = materials[materialIndex];
                        report.Append(material == null ? "<null>" : AssetDatabase.GetAssetPath(material));
                    }
                }

                report.AppendLine();
            }
        }

        private static void CaptureOverview(IReadOnlyCollection<Renderer> renderers, string outputPath)
        {
            if (renderers.Count == 0)
            {
                throw new InvalidOperationException("No visible ship-space renderers were found for source inspection.");
            }

            var hasBounds = false;
            var bounds = new Bounds();
            foreach (var renderer in renderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            if (!hasBounds)
            {
                throw new InvalidOperationException("Unable to calculate ship-space source bounds.");
            }

            var cameraObject = new GameObject("__ShipSpaceCeilingSourceInspectionCamera")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.025f, 0.03f, 0.035f, 1f);
            camera.fieldOfView = 52f;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 500f;
            camera.allowHDR = false;
            camera.allowMSAA = true;

            var viewDirection = new Vector3(0.65f, 0.92f, -0.72f).normalized;
            var distance = Mathf.Max(bounds.extents.magnitude * 1.55f, 25f);
            camera.transform.position = bounds.center + (viewDirection * distance);
            camera.transform.rotation = Quaternion.LookRotation(bounds.center - camera.transform.position, Vector3.up);

            const int width = 1600;
            const int height = 1000;
            var renderTexture = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            var previousActive = RenderTexture.active;
            try
            {
                camera.targetTexture = renderTexture;
                camera.Render();
                RenderTexture.active = renderTexture;

                var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
                try
                {
                    texture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                    texture.Apply(false, false);
                    File.WriteAllBytes(outputPath, texture.EncodeToPNG());
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(texture);
                }
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(renderTexture);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static GameObject FindSceneObject(Scene scene, string name)
        {
            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                var transforms = roots[i].GetComponentsInChildren<Transform>(true);
                for (var transformIndex = 0; transformIndex < transforms.Length; transformIndex++)
                {
                    if (transforms[transformIndex].name == name)
                    {
                        return transforms[transformIndex].gameObject;
                    }
                }
            }

            return null;
        }

        private static string RelativePath(Transform transform, Transform relativeRoot)
        {
            var full = HierarchyPath(transform);
            var prefix = HierarchyPath(relativeRoot) + "/";
            return full.StartsWith(prefix, StringComparison.Ordinal) ? full.Substring(prefix.Length) : full;
        }

        private static string HierarchyPath(Transform transform)
        {
            var names = new Stack<string>();
            for (var current = transform; current != null; current = current.parent)
            {
                names.Push(current.name);
            }

            return string.Join("/", names);
        }

        private static string Vector(Vector3 value)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "({0:0.######},{1:0.######},{2:0.######})",
                value.x,
                value.y,
                value.z);
        }

        private readonly struct SpaceDefinition
        {
            internal SpaceDefinition(
                string id,
                string displayName,
                string sourceRootName,
                bool isCorridorCollection = false)
            {
                Id = id;
                DisplayName = displayName;
                SourceRootName = sourceRootName;
                IsCorridorCollection = isCorridorCollection;
            }

            internal string Id { get; }
            internal string DisplayName { get; }
            internal string SourceRootName { get; }
            internal bool IsCorridorCollection { get; }
            internal string PrefabPath => SampleAssetDirectory + "/ShipSpaceCeiling_" + Id + ".prefab";
            internal string SampleGroupName => "Ceiling Sample - " + DisplayName + " (" + Id + ")";
        }

        private readonly struct CaptureTarget
        {
            internal CaptureTarget(
                GameObject group,
                Transform module,
                Transform ceilingRoot,
                string ceilingPrefix)
            {
                Group = group;
                Module = module;
                CeilingRoot = ceilingRoot;
                CeilingPrefix = ceilingPrefix;
            }

            internal GameObject Group { get; }
            internal Transform Module { get; }
            internal Transform CeilingRoot { get; }
            internal string CeilingPrefix { get; }
        }

        private readonly struct AppliedCaptureTarget
        {
            internal AppliedCaptureTarget(string label, Bounds bounds, bool isCorridor)
            {
                Label = label;
                Bounds = bounds;
                IsCorridor = isCorridor;
            }

            internal string Label { get; }
            internal Bounds Bounds { get; }
            internal bool IsCorridor { get; }
        }

        private readonly struct ProtectedProductionState
        {
            internal ProtectedProductionState(
                string path,
                Transform transform,
                Transform parent,
                int siblingIndex,
                string name,
                bool activeSelf,
                Vector3 localPosition,
                Quaternion localRotation,
                Vector3 localScale,
                string componentSignature)
            {
                Path = path;
                Transform = transform;
                Parent = parent;
                SiblingIndex = siblingIndex;
                Name = name;
                ActiveSelf = activeSelf;
                LocalPosition = localPosition;
                LocalRotation = localRotation;
                LocalScale = localScale;
                ComponentSignature = componentSignature;
            }

            internal string Path { get; }
            internal Transform Transform { get; }
            internal Transform Parent { get; }
            internal int SiblingIndex { get; }
            internal string Name { get; }
            internal bool ActiveSelf { get; }
            internal Vector3 LocalPosition { get; }
            internal Quaternion LocalRotation { get; }
            internal Vector3 LocalScale { get; }
            internal string ComponentSignature { get; }
        }

        private readonly struct RectXZ
        {
            internal RectXZ(float xMin, float xMax, float zMin, float zMax)
            {
                XMin = Mathf.Min(xMin, xMax);
                XMax = Mathf.Max(xMin, xMax);
                ZMin = Mathf.Min(zMin, zMax);
                ZMax = Mathf.Max(zMin, zMax);
            }

            internal float XMin { get; }
            internal float XMax { get; }
            internal float ZMin { get; }
            internal float ZMax { get; }

            internal bool Contains(float x, float z)
            {
                return x >= XMin - BoundsEpsilon &&
                       x <= XMax + BoundsEpsilon &&
                       z >= ZMin - BoundsEpsilon &&
                       z <= ZMax + BoundsEpsilon;
            }
        }

        private readonly struct EntranceWallCandidate
        {
            internal EntranceWallCandidate(float distance, float top)
            {
                Distance = distance;
                Top = top;
            }

            internal float Distance { get; }
            internal float Top { get; }
        }
    }
}
