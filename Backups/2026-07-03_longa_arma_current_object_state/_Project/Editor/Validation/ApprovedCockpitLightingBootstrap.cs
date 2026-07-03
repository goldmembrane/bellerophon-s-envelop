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
    public static class ApprovedCockpitLightingBootstrap
    {
        public const string RootName = "Approved Cockpit 12 Inspection Lighting";

        private const string SampleRootRelativePath = "artSample/ck_light12";
        private const string ComparisonRootName = "unity_applied_comparison";
        private const string SourceFbxRelativePath = "artSample/ck_light12/exports/ck_light12.fbx";
        private const string UnityAssetDirectory = "Assets/_Project/Art/Ship/Cockpit";
        private const string UnityFbxPath = UnityAssetDirectory + "/ck_light12.fbx";
        private const string FixtureMaterialPath = UnityAssetDirectory + "/M_CkLight12_Fixture.mat";
        private const string DarkMaterialPath = UnityAssetDirectory + "/M_CkLight12_Dark.mat";
        private const string WornMaterialPath = UnityAssetDirectory + "/M_CkLight12_Worn.mat";
        private const string CoolMaterialPath = UnityAssetDirectory + "/M_CkLight12_Cool.mat";
        private const string WarmMaterialPath = UnityAssetDirectory + "/M_CkLight12_Warm.mat";
        private const string CyanMaterialPath = UnityAssetDirectory + "/M_CkLight12_Cyan.mat";
        private const string PoolMaterialPath = UnityAssetDirectory + "/M_CkLight12_LightFootprint.mat";

        private static readonly Vector3 CockpitCenter = new Vector3(0f, 0f, 18f);
        private static readonly Vector3 RootWorldPosition = CockpitCenter;
        private static readonly Vector3 FrontCeilingTargetCenter = CockpitCenter + new Vector3(0f, 2.54f, -2.55f);
        private static readonly Vector3 LeftEngineEntranceTargetCenter = CockpitCenter + new Vector3(-3.32f, 1.56f, 1.10f);
        private static readonly Vector3 RightControlEntranceTargetCenter = CockpitCenter + new Vector3(3.32f, 1.56f, 1.10f);
        private static readonly Vector3 RearCargoEntranceTargetCenter = CockpitCenter + new Vector3(0f, 2.02f, 3.08f);
        private static readonly Vector3 ConsoleWorkTargetCenter = CockpitCenter + new Vector3(0f, 0.45f, -1.58f);
        private static readonly Vector3 WarningScreenBarCenter = CockpitCenter + new Vector3(0f, 2.68f, -3.45f);
        private static readonly Vector3 WarningCeilingBeaconCenter = CockpitCenter + new Vector3(0f, 2.92f, 0f);
        private static readonly Vector3 ReviewedRearCargoRendererCenter = CockpitCenter + new Vector3(0f, 1.55f, 3.42f);
        private static readonly Vector3 ReviewedRearCargoRuntimeLightLocalPosition = new Vector3(0.09f, 1.87f, 3.446f);
        private static readonly string[] EditorReviewRemovedTransformPaths =
        {
            "CK-12 cockpit inspection lighting sample/left ceiling-to-wall lighting conduit",
            "CK-12 cockpit inspection lighting sample/left forward angled inspection pod armored yoke",
            "CK-12 cockpit inspection lighting sample/left forward angled inspection pod recessed maintenance lens",
            "CK-12 cockpit inspection lighting sample/left wall cool white service lens",
            "CK-12 cockpit inspection lighting sample/left wall inspection strip recessed backing",
            "CK-12 cockpit inspection lighting sample/left wall pale inspection wash",
            "CK-12 cockpit inspection lighting sample/left wall upper amber inspection tick",
            "CK-12 cockpit inspection lighting sample/right ceiling-to-wall lighting conduit",
            "CK-12 cockpit inspection lighting sample/right forward angled inspection pod armored yoke",
            "CK-12 cockpit inspection lighting sample/right forward angled inspection pod left pivot bolt",
            "CK-12 cockpit inspection lighting sample/right forward angled inspection pod recessed maintenance lens",
            "CK-12 cockpit inspection lighting sample/right forward angled inspection pod right pivot bolt",
            "CK-12 cockpit inspection lighting sample/right wall cool white service lens",
            "CK-12 cockpit inspection lighting sample/right wall inspection strip recessed backing",
            "CK-12 cockpit inspection lighting sample/right wall pale inspection wash",
            "CK-12 cockpit inspection lighting sample/right wall upper amber inspection tick",
            "CK-12 cockpit inspection lighting sample/console front soft cyan work pool",
            "CK-12 cockpit inspection lighting sample/console front underdeck inspection strip backing",
            "CK-12 cockpit inspection lighting sample/console front underdeck soft cyan lens",
            "CK-12 cockpit inspection lighting sample/floor toe-kick inspection marker 1",
            "CK-12 cockpit inspection lighting sample/floor toe-kick inspection marker 2",
            "CK-12 cockpit inspection lighting sample/floor toe-kick inspection marker 3",
            "CK-12 cockpit inspection lighting sample/floor toe-kick inspection marker 4",
            "CK-12 cockpit inspection lighting sample/floor toe-kick worn metal lip 1",
            "CK-12 cockpit inspection lighting sample/floor toe-kick worn metal lip 2",
            "CK-12 cockpit inspection lighting sample/floor toe-kick worn metal lip 3",
            "CK-12 cockpit inspection lighting sample/floor toe-kick worn metal lip 4",
            "CK-12 cockpit inspection lighting sample/front ceiling soft light pool 1",
            "CK-12 cockpit inspection lighting sample/front ceiling soft light pool 2",
            "CK-12 cockpit inspection lighting sample/front ceiling soft light pool 3",
            "CK-12 cockpit inspection lighting sample/front ceiling soft light pool 4",
            "CK-12 cockpit inspection lighting sample/low voltage cable along front screen frame",
            "CK-12 cockpit inspection lighting sample/left forward angled inspection pod left pivot bolt",
            "CK-12 cockpit inspection lighting sample/left forward angled inspection pod right pivot bolt",
            "CK-12 cockpit inspection lighting sample/left rear service pod armored yoke",
            "CK-12 cockpit inspection lighting sample/left rear service pod left pivot bolt",
            "CK-12 cockpit inspection lighting sample/left rear service pod recessed maintenance lens",
            "CK-12 cockpit inspection lighting sample/left rear service pod right pivot bolt",
            "CK-12 cockpit inspection lighting sample/right rear service pod armored yoke",
            "CK-12 cockpit inspection lighting sample/right rear service pod left pivot bolt",
            "CK-12 cockpit inspection lighting sample/right rear service pod recessed maintenance lens",
            "CK-12 cockpit inspection lighting sample/right rear service pod right pivot bolt"
        };

        private static readonly TransformOverride[] EditorReviewTransformOverrides =
        {
            new TransformOverride("CK-12 cockpit inspection lighting sample/rear cargo threshold dark retaining ring 1", new Vector3(0.0058f, -0.0401f, 0.0296f), Quaternion.Euler(89.98022f, 0f, 0f), new Vector3(1f, 1f, 1f)),
            new TransformOverride("CK-12 cockpit inspection lighting sample/rear cargo threshold dark retaining ring 2", new Vector3(0f, -0.0401f, 0.0296f), Quaternion.Euler(89.98022f, 0f, 0f), new Vector3(1f, 1f, 1f)),
            new TransformOverride("CK-12 cockpit inspection lighting sample/rear cargo threshold dark retaining ring 3", new Vector3(-0.0058f, -0.0401f, 0.0296f), Quaternion.Euler(89.98022f, 0f, 0f), new Vector3(1f, 1f, 1f)),
            new TransformOverride("CK-12 cockpit inspection lighting sample/rear cargo threshold round downlight 1", new Vector3(0.0058f, -0.0402f, 0.0296f), Quaternion.Euler(89.98022f, 0f, 0f), new Vector3(1f, 1f, 1f)),
            new TransformOverride("CK-12 cockpit inspection lighting sample/rear cargo threshold round downlight 2", new Vector3(0f, -0.0402f, 0.0296f), Quaternion.Euler(89.98022f, 0f, 0f), new Vector3(1f, 1f, 1f)),
            new TransformOverride("CK-12 cockpit inspection lighting sample/rear cargo threshold round downlight 3", new Vector3(-0.0058f, -0.0402f, 0.0296f), Quaternion.Euler(89.98022f, 0f, 0f), new Vector3(1f, 1f, 1f)),
            new TransformOverride("CK-12 cockpit inspection lighting sample/rear threshold inspection light mounting rail", new Vector3(0f, -0.0393f, 0.0301f), Quaternion.Euler(0f, 0f, 0f), new Vector3(1f, 1f, 1f)),
            new TransformOverride("CK-12 cockpit inspection lighting sample/rear threshold warm floor pool 1", new Vector3(0.0058f, -0.03685f, 0.0003f), Quaternion.Euler(0f, 0f, 0f), new Vector3(0.58f, 0.28f, 0.025f)),
            new TransformOverride("CK-12 cockpit inspection lighting sample/rear threshold warm floor pool 2", new Vector3(0f, -0.03685f, 0.0003f), Quaternion.Euler(0f, 0f, 0f), new Vector3(0.58f, 0.28f, 0.025f)),
            new TransformOverride("CK-12 cockpit inspection lighting sample/rear threshold warm floor pool 3", new Vector3(-0.0058f, -0.03685f, 0.0003f), Quaternion.Euler(0f, 0f, 0f), new Vector3(0.58f, 0.28f, 0.025f))
        };

        [MenuItem("Bellerophon/Bootstrap/Ensure Approved Cockpit 12 Inspection Lighting")]
        public static void EnsureApprovedCockpitLighting()
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

            if (FindNamedObject(ApprovedCockpitWarningBootstrap.RootName) == null)
            {
                ApprovedCockpitWarningBootstrap.EnsureApprovedCockpitWarning();
                scene = EditorSceneManager.OpenScene(Phase4CargoShipGrayboxBootstrap.CargoRunScenePath, OpenSceneMode.Single);
            }

            if (FindNamedObject(ApprovedCockpitDirectionBootstrap.RootName) == null)
            {
                ApprovedCockpitDirectionBootstrap.EnsureApprovedCockpitDirection();
                scene = EditorSceneManager.OpenScene(Phase4CargoShipGrayboxBootstrap.CargoRunScenePath, OpenSceneMode.Single);
            }

            DeleteGeneratedObject(RootName);
            CopyApprovedSourceFbx();

            var source = AssetDatabase.LoadAssetAtPath<GameObject>(UnityFbxPath);
            if (source == null)
            {
                throw new InvalidOperationException("Approved cockpit lighting source FBX failed to import: " + UnityFbxPath);
            }

            var root = PrefabUtility.InstantiatePrefab(source) as GameObject;
            if (root == null)
            {
                throw new InvalidOperationException("Approved cockpit lighting source FBX could not be instantiated: " + UnityFbxPath);
            }

            root.name = RootName;
            root.transform.position = RootWorldPosition;
            root.transform.rotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;

            RemovePreviewOnlyObjects(root.transform);
            PositionApprovedGroups(root.transform);
            RemoveEditorReviewObjects(root.transform);
            ApplyEditorReviewTransformOverrides(root.transform);
            ApplyApprovedMaterials(root.transform, EnsureMaterials());
            DisableAllColliders(root.transform);
            AddRuntimeInspectionLights(root.transform);
            ModelingInspectionModeBootstrap.ApplyFreeCameraForModeling();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, Phase4CargoShipGrayboxBootstrap.CargoRunScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            ValidateScene();
            Debug.Log("Approved cockpit 12 inspection lighting applied.");
        }

        [MenuItem("Bellerophon/Validation/Validate Approved Cockpit 12 Inspection Lighting")]
        public static void ValidateScene()
        {
            EditorSceneManager.OpenScene(Phase4CargoShipGrayboxBootstrap.CargoRunScenePath, OpenSceneMode.Single);

            ApprovedCockpitStructureBootstrap.ValidateScene();
            ApprovedCockpitWindowBootstrap.ValidateScene();
            ApprovedCockpitConsoleBootstrap.ValidateScene();
            ApprovedCockpitWarningBootstrap.ValidateScene();
            ApprovedCockpitDirectionBootstrap.ValidateScene();

            var root = RequireObject(RootName);
            if (!root.activeInHierarchy)
            {
                throw new InvalidOperationException(RootName + " must be active after user approval.");
            }

            var renderers = root.GetComponentsInChildren<Renderer>(true);
            var enabledRenderers = 0;
            var hasFrontCeiling = false;
            var hasRearEntrance = false;
            var hasConsoleWork = false;
            for (var i = 0; i < renderers.Length; i++)
            {
                if (!renderers[i].enabled)
                {
                    continue;
                }

                enabledRenderers++;
                var lowerName = renderers[i].gameObject.name.ToLowerInvariant();
                hasFrontCeiling |= IsFrontCeilingPart(lowerName);
                hasRearEntrance |= IsRearCargoEntrancePart(lowerName);
                hasConsoleWork |= IsConsoleWorkPart(lowerName);
            }

            if (enabledRenderers < 45)
            {
                throw new InvalidOperationException("Approved cockpit 12 lighting renderer count is too low: " + enabledRenderers);
            }

            if (!hasFrontCeiling || !hasRearEntrance || !hasConsoleWork)
            {
                throw new InvalidOperationException(
                    "Approved cockpit 12 lighting is missing required groups. FrontCeiling=" +
                    hasFrontCeiling +
                    "; RearEntrance=" +
                    hasRearEntrance +
                    "; ConsoleWork=" +
                    hasConsoleWork);
            }

            ValidateNoPreviewObjects(root.transform);
            ValidateEditorReviewRemovedObjects(root.transform);
            ValidateGroupCenter(root.transform, IsFrontCeilingPart, FrontCeilingTargetCenter, "front ceiling inspection lights", 0.36f);
            ValidateGroupCenter(root.transform, IsRearCargoEntrancePart, ReviewedRearCargoRendererCenter, "rear cargo entrance inspection lights", 0.20f);
            ValidateGroupCenter(root.transform, IsConsoleWorkPart, ConsoleWorkTargetCenter, "console work lights", 0.42f);
            ValidateDoesNotOverlapWarningLights(root.transform);
            ValidateRuntimeLightCoverage(root.transform);

            var enabledColliders = CountEnabledColliders(root.transform);
            if (enabledColliders != 0)
            {
                throw new InvalidOperationException("Approved cockpit 12 lighting must not introduce gameplay colliders. EnabledColliders=" + enabledColliders);
            }

            CargoShipVisualModelingBootstrap.ValidateScene();
            ModelingInspectionModeBootstrap.ValidateScene();
            ModelingInspectionModeBootstrap.ValidateFreeCamera();
            Debug.Log(
                "Approved cockpit 12 inspection lighting validation passed. Renderers=" +
                enabledRenderers +
                "; EnabledColliders=0; EditorReviewApplied=True; RuntimeLights=3; WarningOverlap=False");
        }

        [MenuItem("Bellerophon/Validation/Capture Approved Cockpit 12 Inspection Lighting Comparison")]
        public static void CaptureUnityComparison()
        {
            ValidateScene();

            var projectRoot = Directory.GetParent(Application.dataPath);
            if (projectRoot == null)
            {
                throw new InvalidOperationException("Could not resolve project root for cockpit lighting comparison output.");
            }

            var outputRoot = Path.Combine(projectRoot.FullName, SampleRootRelativePath, ComparisonRootName);
            Directory.CreateDirectory(outputRoot);

            CaptureAppliedView(
                Path.Combine(outputRoot, "unity_01_front.png"),
                CockpitCenter + new Vector3(0f, 2.05f, 5.2f),
                CockpitCenter + new Vector3(0f, 1.55f, -1.30f),
                38f,
                false,
                5f,
                Vector3.up);
            CaptureAppliedView(
                Path.Combine(outputRoot, "unity_02_player.png"),
                CockpitCenter + new Vector3(0f, 1.62f, 3.2f),
                CockpitCenter + new Vector3(0f, 1.45f, -1.10f),
                34f,
                false,
                5f,
                Vector3.up);
            CaptureAppliedView(
                Path.Combine(outputRoot, "unity_03_side.png"),
                CockpitCenter + new Vector3(5.2f, 2.2f, 1.6f),
                CockpitCenter + new Vector3(0.4f, 1.55f, 0.15f),
                42f,
                false,
                5f,
                Vector3.up);
            CaptureAppliedView(
                Path.Combine(outputRoot, "unity_04_top.png"),
                CockpitCenter + new Vector3(0f, 8.4f, 0.4f),
                CockpitCenter + new Vector3(0f, 1.0f, 0.2f),
                36f,
                true,
                5.6f,
                Vector3.forward);
            CaptureAppliedView(
                Path.Combine(outputRoot, "unity_05_ceiling_detail.png"),
                FrontCeilingTargetCenter + new Vector3(-1.9f, 0.65f, 1.6f),
                FrontCeilingTargetCenter,
                42f,
                false,
                5f,
                Vector3.up);
            CaptureAppliedView(
                Path.Combine(outputRoot, "unity_06_service_detail.png"),
                RightControlEntranceTargetCenter + new Vector3(1.7f, 0.25f, 1.4f),
                RightControlEntranceTargetCenter,
                42f,
                false,
                5f,
                Vector3.up);

            WriteComparisonIndex(outputRoot);
            AssetDatabase.Refresh();
            Debug.Log("Approved cockpit 12 inspection lighting Unity comparison snapshots saved: " + outputRoot);
        }

        [MenuItem("Bellerophon/Validation/Capture Approved Cockpit 12 Lighting Current Objects")]
        public static void CaptureCurrentEditorObjects()
        {
            var activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid())
            {
                throw new InvalidOperationException("No active scene is open for cockpit lighting current object capture.");
            }

            var normalizedActivePath = activeScene.path.Replace('\\', '/');
            var normalizedCargoPath = Phase4CargoShipGrayboxBootstrap.CargoRunScenePath.Replace('\\', '/');
            if (!string.Equals(normalizedActivePath, normalizedCargoPath, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Current active scene is not CargoRunMvp. ActiveScene=" + activeScene.path);
            }

            var currentRoot = RequireObject(RootName);
            CopyApprovedSourceFbx();

            var source = AssetDatabase.LoadAssetAtPath<GameObject>(UnityFbxPath);
            if (source == null)
            {
                throw new InvalidOperationException("Approved cockpit 12 source FBX failed to import for current object capture: " + UnityFbxPath);
            }

            var expectedRoot = PrefabUtility.InstantiatePrefab(source) as GameObject;
            if (expectedRoot == null)
            {
                throw new InvalidOperationException("Approved cockpit 12 source FBX could not be instantiated for current object capture: " + UnityFbxPath);
            }

            expectedRoot.name = RootName + " Expected Capture";
            expectedRoot.hideFlags = HideFlags.HideAndDontSave;
            expectedRoot.transform.position = RootWorldPosition;
            expectedRoot.transform.rotation = Quaternion.identity;
            expectedRoot.transform.localScale = Vector3.one;

            try
            {
                RemovePreviewOnlyObjects(expectedRoot.transform);
                PositionApprovedGroups(expectedRoot.transform);
                RemoveEditorReviewObjects(expectedRoot.transform);
                ApplyEditorReviewTransformOverrides(expectedRoot.transform);
                AddRuntimeInspectionLights(expectedRoot.transform);

                var expectedTransforms = CollectTransformPaths(expectedRoot.transform);
                var currentTransforms = CollectTransformPaths(currentRoot.transform);
                var expectedTransformStates = CollectTransformStates(expectedRoot.transform);
                var currentTransformStates = CollectTransformStates(currentRoot.transform);
                var missingTransforms = GetMissingPaths(expectedTransforms, currentTransforms);
                var addedTransforms = GetMissingPaths(currentTransforms, expectedTransforms);
                var changedTransforms = GetTransformChanges(expectedTransformStates, currentTransformStates);

                var projectRoot = Directory.GetParent(Application.dataPath);
                if (projectRoot == null)
                {
                    throw new InvalidOperationException("Could not resolve project root for cockpit 12 current object capture.");
                }

                var outputRoot = Path.Combine(projectRoot.FullName, SampleRootRelativePath, "editor_current");
                Directory.CreateDirectory(outputRoot);

                var builder = new StringBuilder();
                builder.AppendLine("# Approved Cockpit 12 Current Object Capture");
                builder.AppendLine();
                builder.AppendLine("이 파일은 현재 열린 Unity 씬의 CK-12 편집 상태와 스크립트 생성 기준을 비교한 결과입니다.");
                builder.AppendLine("이 명령은 씬을 다시 적용하거나 저장하지 않습니다.");
                builder.AppendLine();
                builder.AppendLine("CurrentRoot=" + currentRoot.name);
                builder.AppendLine("ExpectedRoot=" + expectedRoot.name);
                builder.AppendLine("ExpectedTransformCount=" + expectedTransforms.Count);
                builder.AppendLine("CurrentTransformCount=" + currentTransforms.Count);
                builder.AppendLine("MissingTransformCount=" + missingTransforms.Count);
                builder.AppendLine("AddedTransformCount=" + addedTransforms.Count);
                builder.AppendLine("ChangedTransformCount=" + changedTransforms.Count);
                builder.AppendLine();
                AppendPathSection(builder, "MissingTransforms", missingTransforms);
                builder.AppendLine();
                AppendPathSection(builder, "AddedTransforms", addedTransforms);
                builder.AppendLine();
                AppendTransformChangeSection(builder, "ChangedTransforms", changedTransforms);
                builder.AppendLine();
                AppendTransformOverrideCandidates(builder, changedTransforms);

                File.WriteAllText(Path.Combine(outputRoot, "ck12_current_objects.md"), builder.ToString(), new UTF8Encoding(false));
                AssetDatabase.Refresh();
                Debug.Log("Approved cockpit 12 lighting current object capture saved: " + outputRoot);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(expectedRoot);
            }
        }

        private static void CopyApprovedSourceFbx()
        {
            var projectRoot = Directory.GetParent(Application.dataPath);
            if (projectRoot == null)
            {
                throw new InvalidOperationException("Could not resolve project root for cockpit lighting source FBX.");
            }

            var sourcePath = Path.Combine(projectRoot.FullName, SourceFbxRelativePath);
            if (!File.Exists(sourcePath))
            {
                throw new FileNotFoundException("Missing approved cockpit lighting source FBX.", sourcePath);
            }

            var targetDirectory = Path.Combine(projectRoot.FullName, UnityAssetDirectory);
            Directory.CreateDirectory(targetDirectory);
            File.Copy(sourcePath, Path.Combine(projectRoot.FullName, UnityFbxPath), true);
            AssetDatabase.ImportAsset(UnityFbxPath, ImportAssetOptions.ForceUpdate);
        }

        private static LightingMaterials EnsureMaterials()
        {
            Directory.CreateDirectory(UnityAssetDirectory);
            return new LightingMaterials(
                EnsureMaterial(FixtureMaterialPath, new Color(0.045f, 0.052f, 0.050f, 1f), 0.24f, 0.18f, false, false),
                EnsureMaterial(DarkMaterialPath, new Color(0.015f, 0.017f, 0.016f, 1f), 0.08f, 0.10f, false, false),
                EnsureMaterial(WornMaterialPath, new Color(0.34f, 0.34f, 0.30f, 1f), 0.32f, 0.26f, false, false),
                EnsureMaterial(CoolMaterialPath, new Color(0.68f, 0.86f, 1.0f, 1f), 0f, 0.32f, true, false, 1.55f),
                EnsureMaterial(WarmMaterialPath, new Color(1.0f, 0.58f, 0.20f, 1f), 0f, 0.34f, true, false, 1.10f),
                EnsureMaterial(CyanMaterialPath, new Color(0.18f, 0.86f, 0.86f, 1f), 0f, 0.34f, true, false, 0.90f),
                EnsureMaterial(PoolMaterialPath, new Color(0.34f, 0.72f, 0.92f, 0.08f), 0f, 0.18f, false, true));
        }

        private static Material EnsureMaterial(
            string path,
            Color color,
            float metallic,
            float smoothness,
            bool emissive,
            bool transparent,
            float emissionMultiplier = 1f)
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

            if (transparent)
            {
                SetFloat(material, "_Surface", 1f);
                SetFloat(material, "_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                SetFloat(material, "_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                SetFloat(material, "_ZWrite", 0f);
                material.SetOverrideTag("RenderType", "Transparent");
                material.renderQueue = 3000;
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }
            else
            {
                SetFloat(material, "_Surface", 0f);
                SetFloat(material, "_ZWrite", 1f);
                material.SetOverrideTag("RenderType", "Opaque");
                material.renderQueue = -1;
                material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }

            if (emissive)
            {
                material.EnableKeyword("_EMISSION");
                var emission = color * emissionMultiplier;
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
                    lowerName.Contains("camera") ||
                    lowerName.Contains("combined inspection glow") ||
                    lowerName.Contains("service glow") ||
                    lowerName.Contains("threshold warm glow") ||
                    target.GetComponent<Camera>() != null ||
                    target.GetComponent<Light>() != null)
                {
                    UnityEngine.Object.DestroyImmediate(target.gameObject);
                }
            }
        }

        private static void PositionApprovedGroups(Transform root)
        {
            MoveRendererGroup(root, IsFrontCeilingPart, FrontCeilingTargetCenter, "front ceiling inspection lights");
            MoveRendererGroup(root, IsLeftEngineEntrancePart, LeftEngineEntranceTargetCenter, "left engine entrance inspection lights");
            MoveRendererGroup(root, IsRightControlEntrancePart, RightControlEntranceTargetCenter, "right control entrance inspection lights");
            MoveRendererGroup(root, IsRearCargoEntrancePart, RearCargoEntranceTargetCenter, "rear cargo entrance inspection lights");
            MoveRendererGroup(root, IsConsoleWorkPart, ConsoleWorkTargetCenter, "console work lights");
        }

        private static void RemoveEditorReviewObjects(Transform root)
        {
            for (var i = 0; i < EditorReviewRemovedTransformPaths.Length; i++)
            {
                var target = FindRelativeTransform(root, EditorReviewRemovedTransformPaths[i]);
                if (target != null)
                {
                    UnityEngine.Object.DestroyImmediate(target.gameObject);
                }
            }
        }

        private static void ApplyEditorReviewTransformOverrides(Transform root)
        {
            for (var i = 0; i < EditorReviewTransformOverrides.Length; i++)
            {
                var overrideValue = EditorReviewTransformOverrides[i];
                var target = FindRelativeTransform(root, overrideValue.Path);
                if (target == null)
                {
                    throw new InvalidOperationException("Missing cockpit 12 transform override target: " + overrideValue.Path);
                }

                target.localPosition = overrideValue.LocalPosition;
                target.localRotation = overrideValue.LocalRotation;
                target.localScale = overrideValue.LocalScale;
            }
        }

        private static void MoveRendererGroup(Transform root, Predicate<string> predicate, Vector3 targetCenter, string groupName)
        {
            var bounds = GetGroupBounds(root, predicate, groupName);
            var delta = targetCenter - bounds.center;
            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                var transform = transforms[i];
                if (transform == root || transform == null)
                {
                    continue;
                }

                var lowerName = transform.gameObject.name.ToLowerInvariant();
                if (predicate(lowerName))
                {
                    transform.position += delta;
                }
            }
        }

        private static void ApplyApprovedMaterials(Transform root, LightingMaterials materials)
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

        private static Material ResolveMaterial(string sourceName, string objectName, LightingMaterials materials)
        {
            if (sourceName.Contains("pool") ||
                sourceName.Contains("wash") ||
                sourceName.Contains("footprint") ||
                objectName.Contains("pool") ||
                objectName.Contains("wash") ||
                objectName.Contains("footprint"))
            {
                return materials.Pool;
            }

            if (sourceName.Contains("cyan") || objectName.Contains("cyan") || objectName.Contains("underdeck"))
            {
                return materials.Cyan;
            }

            if (sourceName.Contains("warm") ||
                sourceName.Contains("amber") ||
                objectName.Contains("warm") ||
                objectName.Contains("amber") ||
                objectName.Contains("downlight") ||
                objectName.Contains("toe-kick"))
            {
                return materials.Warm;
            }

            if (sourceName.Contains("cool") ||
                sourceName.Contains("white") ||
                objectName.Contains("cool") ||
                objectName.Contains("white") ||
                objectName.Contains("lens"))
            {
                return materials.Cool;
            }

            if (objectName.Contains("screw") || objectName.Contains("bolt") || objectName.Contains("bracket"))
            {
                return materials.Worn;
            }

            if (sourceName.Contains("dark") || objectName.Contains("dark") || objectName.Contains("lip") || objectName.Contains("conduit"))
            {
                return materials.Dark;
            }

            return materials.Fixture;
        }

        private static void AddRuntimeInspectionLights(Transform root)
        {
            AddPointLight(root, "CK-12 rear cargo corridor inspection light", RootWorldPosition + ReviewedRearCargoRuntimeLightLocalPosition, new Color(1f, 0.72f, 0.42f, 1f), 1.05f, 3.0f);
            AddPointLight(root, "CK-12 front ceiling inspection light", FrontCeilingTargetCenter + new Vector3(0f, -0.10f, 0.25f), new Color(0.72f, 0.90f, 1f, 1f), 0.85f, 2.6f);
            AddPointLight(root, "CK-12 console underdeck work light", ConsoleWorkTargetCenter + new Vector3(0f, 0f, 0.15f), new Color(0.18f, 0.84f, 0.86f, 1f), 0.65f, 2.2f);
        }

        private static void AddPointLight(Transform parent, string name, Vector3 position, Color color, float intensity, float range)
        {
            var lightObject = new GameObject(name);
            lightObject.transform.SetParent(parent, true);
            lightObject.transform.position = position;
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
        }

        private static void ValidateGroupCenter(Transform root, Predicate<string> predicate, Vector3 expectedCenter, string groupName, float tolerance)
        {
            var bounds = GetGroupBounds(root, predicate, groupName);
            var delta = Vector3.Distance(bounds.center, expectedCenter);
            if (delta > tolerance)
            {
                throw new InvalidOperationException("Approved cockpit 12 " + groupName + " is not in the approved location. Delta=" + delta.ToString("0.000") + "; Expected=" + FormatVector(expectedCenter) + "; Current=" + FormatVector(bounds.center));
            }
        }

        private static void ValidateRuntimeLightCoverage(Transform root)
        {
            var lights = root.GetComponentsInChildren<Light>(true);
            if (lights.Length != 3)
            {
                throw new InvalidOperationException("Approved cockpit 12 lighting must match the current editor-reviewed runtime light count. RuntimeLights=" + lights.Length);
            }

            RequireLightNear(root, "front ceiling inspection light", FrontCeilingTargetCenter, 0.45f);
            RequireLightNear(root, "rear cargo corridor inspection light", RootWorldPosition + ReviewedRearCargoRuntimeLightLocalPosition, 0.05f);
            RequireLightNear(root, "console underdeck work light", ConsoleWorkTargetCenter, 0.45f);
        }

        private static void RequireLightNear(Transform root, string name, Vector3 target, float tolerance)
        {
            var lights = root.GetComponentsInChildren<Light>(true);
            for (var i = 0; i < lights.Length; i++)
            {
                if (Vector3.Distance(lights[i].transform.position, target) <= tolerance)
                {
                    return;
                }
            }

            throw new InvalidOperationException("Approved cockpit 12 is missing a runtime inspection light near " + name + ".");
        }

        private static void ValidateDoesNotOverlapWarningLights(Transform root)
        {
            ValidateGroupDistance(root, IsFrontCeilingPart, WarningScreenBarCenter, "CK-04 screen warning bar", 0.72f);
            ValidateGroupDistance(root, IsFrontCeilingPart, WarningCeilingBeaconCenter, "CK-04 ceiling beacon", 1.75f);

            var lights = root.GetComponentsInChildren<Light>(true);
            for (var i = 0; i < lights.Length; i++)
            {
                var lightPosition = lights[i].transform.position;
                if (Vector3.Distance(lightPosition, WarningScreenBarCenter) < 0.65f ||
                    Vector3.Distance(lightPosition, WarningCeilingBeaconCenter) < 1.25f)
                {
                    throw new InvalidOperationException("Approved cockpit 12 light overlaps an existing CK-04 warning light: " + lights[i].name);
                }
            }
        }

        private static void ValidateGroupDistance(Transform root, Predicate<string> predicate, Vector3 warningCenter, string warningName, float minimumDistance)
        {
            var bounds = GetGroupBounds(root, predicate, "warning overlap check");
            var distance = Vector3.Distance(bounds.center, warningCenter);
            if (distance < minimumDistance)
            {
                throw new InvalidOperationException("Approved cockpit 12 lighting overlaps " + warningName + ". Distance=" + distance.ToString("0.000"));
            }
        }

        private static Bounds GetGroupBounds(Transform root, Predicate<string> predicate, string groupName)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            var hasBounds = false;
            var bounds = new Bounds();
            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null || !renderer.enabled || !predicate(renderer.gameObject.name.ToLowerInvariant()))
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                    continue;
                }

                bounds.Encapsulate(renderer.bounds);
            }

            if (!hasBounds)
            {
                throw new InvalidOperationException("Missing approved cockpit 12 renderer group: " + groupName);
            }

            return bounds;
        }

        private static void ValidateNoPreviewObjects(Transform root)
        {
            if (ContainsNamedTransform(root, "context") ||
                ContainsNamedTransform(root, "camera") ||
                ContainsNamedTransform(root, "combined inspection glow") ||
                ContainsNamedTransform(root, "service glow") ||
                ContainsNamedTransform(root, "threshold warm glow"))
            {
                throw new InvalidOperationException("Approved cockpit 12 lighting contains preview-only sample objects.");
            }
        }

        private static void ValidateEditorReviewRemovedObjects(Transform root)
        {
            for (var i = 0; i < EditorReviewRemovedTransformPaths.Length; i++)
            {
                if (FindRelativeTransform(root, EditorReviewRemovedTransformPaths[i]) != null)
                {
                    throw new InvalidOperationException("Approved cockpit 12 still contains an editor-removed object: " + EditorReviewRemovedTransformPaths[i]);
                }
            }
        }

        private static bool IsFrontCeilingPart(string lowerName)
        {
            return lowerName.Contains("front ceiling") ||
                lowerName.Contains("low voltage cable along front screen frame");
        }

        private static bool IsLeftEngineEntrancePart(string lowerName)
        {
            return lowerName.Contains("left wall") ||
                lowerName.Contains("left ceiling-to-wall") ||
                lowerName.Contains("left forward angled inspection pod");
        }

        private static bool IsRightControlEntrancePart(string lowerName)
        {
            return lowerName.Contains("right wall") ||
                lowerName.Contains("right ceiling-to-wall") ||
                lowerName.Contains("right forward angled inspection pod");
        }

        private static bool IsRearCargoEntrancePart(string lowerName)
        {
            return lowerName.Contains("rear cargo threshold") ||
                lowerName.Contains("rear threshold") ||
                lowerName.Contains("left rear service pod") ||
                lowerName.Contains("right rear service pod");
        }

        private static bool IsConsoleWorkPart(string lowerName)
        {
            return lowerName.Contains("console front") ||
                lowerName.Contains("floor toe-kick");
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
            var cameraObject = new GameObject("Approved Cockpit Lighting Comparison Camera")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            var keyLightObject = new GameObject("Approved Cockpit Lighting Comparison Key Light")
            {
                hideFlags = HideFlags.HideAndDontSave
            };

            try
            {
                keyLightObject.transform.position = CockpitCenter + new Vector3(0f, 4.7f, 1.8f);
                var keyLight = keyLightObject.AddComponent<Light>();
                keyLight.type = LightType.Point;
                keyLight.color = new Color(0.82f, 0.92f, 1f, 1f);
                keyLight.intensity = 0.9f;
                keyLight.range = 9f;
                keyLight.shadows = LightShadows.None;

                var camera = cameraObject.AddComponent<Camera>();
                camera.transform.position = cameraPosition;
                camera.transform.LookAt(lookAt, cameraUp);
                camera.fieldOfView = fieldOfView;
                camera.orthographic = orthographic;
                camera.orthographicSize = orthographicSize;
                camera.nearClipPlane = 0.02f;
                camera.farClipPlane = 100f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.014f, 0.016f, 0.018f, 1f);
                camera.allowHDR = false;
                camera.allowMSAA = true;
                CaptureCamera(camera, path, 1600, 1000);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(keyLightObject);
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
            builder.AppendLine("<head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width, initial-scale=1\"><title>CK-12 Unity comparison</title>");
            builder.AppendLine("<style>body{margin:0;background:#111514;color:#ece5d8;font-family:Arial,sans-serif}main{max-width:1400px;margin:0 auto;padding:24px}h1{font-size:27px;margin:0 0 8px}.meta{color:#cfc6b8;margin:0 0 18px}.grid{display:grid;gap:18px}.pair{display:grid;grid-template-columns:1fr 1fr;gap:12px;border:1px solid #3c4643;background:#1c2220;border-radius:6px;padding:12px}.pair h2{grid-column:1/-1;font-size:18px;margin:0}.pair img{display:block;width:100%;height:auto;background:#050807}.label{font-size:13px;color:#ddd3c3;margin:6px 0 0}@media(max-width:900px){.pair{grid-template-columns:1fr}}</style>");
            builder.AppendLine("</head><body><main>");
            builder.AppendLine("<h1>CK-12 Unity comparison</h1>");
            builder.AppendLine("<p class=\"meta\">Left: approved conditional artSample. Right: Unity placement after the current editor review, with the rear threshold, front ceiling, and console inspection lights preserved. CK-04 warning lights are preserved and not overlapped by ceiling inspection lights.</p>");
            builder.AppendLine("<section class=\"grid\">");
            AddComparisonPair(builder, "01 Front", "../renders/01_front.png", "unity_01_front.png");
            AddComparisonPair(builder, "02 Player view", "../renders/02_player.png", "unity_02_player.png");
            AddComparisonPair(builder, "03 Side", "../renders/03_side.png", "unity_03_side.png");
            AddComparisonPair(builder, "04 Top", "../renders/04_top.png", "unity_04_top.png");
            AddComparisonPair(builder, "05 Ceiling detail", "../renders/05_ceiling_detail.png", "unity_05_ceiling_detail.png");
            AddComparisonPair(builder, "06 Service detail", "../renders/06_service_detail.png", "unity_06_service_detail.png");
            builder.AppendLine("</section></main></body></html>");
            File.WriteAllText(Path.Combine(outputRoot, "index.html"), builder.ToString(), new UTF8Encoding(false));
        }

        private static void AddComparisonPair(StringBuilder builder, string title, string approvedPath, string appliedPath)
        {
            builder.AppendLine("<article class=\"pair\">");
            builder.Append("<h2>").Append(title).AppendLine("</h2>");
            builder.Append("<div><a href=\"").Append(approvedPath).Append("\"><img src=\"").Append(approvedPath).Append("\" alt=\"approved artSample\"></a><p class=\"label\">approved artSample</p></div>");
            builder.AppendLine();
            builder.Append("<div><a href=\"").Append(appliedPath).Append("\"><img src=\"").Append(appliedPath).Append("\" alt=\"Unity result\"></a><p class=\"label\">Unity result</p></div>");
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

        private static Transform FindRelativeTransform(Transform root, string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                return null;
            }

            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                if (transforms[i] == root)
                {
                    continue;
                }

                if (string.Equals(GetRelativePath(root, transforms[i]), relativePath, StringComparison.Ordinal))
                {
                    return transforms[i];
                }
            }

            return null;
        }

        private static string GetRelativePath(Transform root, Transform transform)
        {
            if (transform == root)
            {
                return string.Empty;
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

        private static List<string> CollectTransformPaths(Transform root)
        {
            var paths = new List<string>();
            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                if (transforms[i] == root)
                {
                    continue;
                }

                paths.Add(GetRelativePath(root, transforms[i]));
            }

            paths.Sort(StringComparer.Ordinal);
            return paths;
        }

        private static Dictionary<string, TransformState> CollectTransformStates(Transform root)
        {
            var states = new Dictionary<string, TransformState>(StringComparer.Ordinal);
            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                if (transforms[i] == root)
                {
                    continue;
                }

                states[GetRelativePath(root, transforms[i])] = new TransformState(
                    transforms[i].localPosition,
                    transforms[i].localRotation,
                    transforms[i].localScale);
            }

            return states;
        }

        private static List<TransformChange> GetTransformChanges(
            Dictionary<string, TransformState> expected,
            Dictionary<string, TransformState> current)
        {
            var changes = new List<TransformChange>();
            foreach (var pair in expected)
            {
                if (!current.TryGetValue(pair.Key, out var currentState))
                {
                    continue;
                }

                if (!pair.Value.IsCloseTo(currentState))
                {
                    changes.Add(new TransformChange(pair.Key, pair.Value, currentState));
                }
            }

            changes.Sort((left, right) => string.Compare(left.Path, right.Path, StringComparison.Ordinal));
            return changes;
        }

        private static List<string> GetMissingPaths(List<string> expected, List<string> current)
        {
            var currentSet = new HashSet<string>(current, StringComparer.Ordinal);
            var missing = new List<string>();
            for (var i = 0; i < expected.Count; i++)
            {
                if (!currentSet.Contains(expected[i]))
                {
                    missing.Add(expected[i]);
                }
            }

            return missing;
        }

        private static void AppendPathSection(StringBuilder builder, string title, List<string> paths)
        {
            builder.AppendLine("## " + title);
            builder.AppendLine();
            for (var i = 0; i < paths.Count; i++)
            {
                builder.AppendLine("- " + paths[i]);
            }
        }

        private static void AppendTransformChangeSection(StringBuilder builder, string title, List<TransformChange> changes)
        {
            builder.AppendLine("## " + title);
            builder.AppendLine();
            for (var i = 0; i < changes.Count; i++)
            {
                var change = changes[i];
                builder.AppendLine("- " + change.Path);
                builder.AppendLine("  - expectedLocalPosition=" + FormatVectorPrecise(change.Expected.LocalPosition));
                builder.AppendLine("  - currentLocalPosition=" + FormatVectorPrecise(change.Current.LocalPosition));
                builder.AppendLine("  - expectedLocalEuler=" + FormatVectorPrecise(change.Expected.LocalRotation.eulerAngles));
                builder.AppendLine("  - currentLocalEuler=" + FormatVectorPrecise(change.Current.LocalRotation.eulerAngles));
                builder.AppendLine("  - expectedLocalScale=" + FormatVectorPrecise(change.Expected.LocalScale));
                builder.AppendLine("  - currentLocalScale=" + FormatVectorPrecise(change.Current.LocalScale));
            }
        }

        private static void AppendTransformOverrideCandidates(StringBuilder builder, List<TransformChange> changes)
        {
            builder.AppendLine("## CSharp Transform Override Candidates");
            builder.AppendLine();
            builder.AppendLine("```csharp");
            for (var i = 0; i < changes.Count; i++)
            {
                var change = changes[i];
                builder.Append("new TransformOverride(\"")
                    .Append(EscapeCSharpString(change.Path))
                    .Append("\", new Vector3(")
                    .Append(FormatFloat(change.Current.LocalPosition.x))
                    .Append("f, ")
                    .Append(FormatFloat(change.Current.LocalPosition.y))
                    .Append("f, ")
                    .Append(FormatFloat(change.Current.LocalPosition.z))
                    .Append("f), Quaternion.Euler(")
                    .Append(FormatFloat(change.Current.LocalRotation.eulerAngles.x))
                    .Append("f, ")
                    .Append(FormatFloat(change.Current.LocalRotation.eulerAngles.y))
                    .Append("f, ")
                    .Append(FormatFloat(change.Current.LocalRotation.eulerAngles.z))
                    .Append("f), new Vector3(")
                    .Append(FormatFloat(change.Current.LocalScale.x))
                    .Append("f, ")
                    .Append(FormatFloat(change.Current.LocalScale.y))
                    .Append("f, ")
                    .Append(FormatFloat(change.Current.LocalScale.z))
                    .AppendLine("f)),");
            }

            builder.AppendLine("```");
        }

        private static void DeleteGeneratedObject(string objectName)
        {
            var existing = FindNamedObject(objectName);
            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing);
            }
        }

        private static bool ContainsNamedTransform(Transform root, string needle)
        {
            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                if (transforms[i].name.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
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

        private static string FormatVector(Vector3 value)
        {
            return value.x.ToString("0.00") + "," + value.y.ToString("0.00") + "," + value.z.ToString("0.00");
        }

        private static string FormatVectorPrecise(Vector3 value)
        {
            return FormatFloat(value.x) + "," + FormatFloat(value.y) + "," + FormatFloat(value.z);
        }

        private static string FormatFloat(float value)
        {
            return value.ToString("0.######", CultureInfo.InvariantCulture);
        }

        private static string EscapeCSharpString(string value)
        {
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private readonly struct TransformState
        {
            public TransformState(Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
            {
                LocalPosition = localPosition;
                LocalRotation = localRotation;
                LocalScale = localScale;
            }

            public Vector3 LocalPosition { get; }
            public Quaternion LocalRotation { get; }
            public Vector3 LocalScale { get; }

            public bool IsCloseTo(TransformState other)
            {
                return Vector3.Distance(LocalPosition, other.LocalPosition) <= 0.0005f &&
                    Quaternion.Angle(LocalRotation, other.LocalRotation) <= 0.05f &&
                    Vector3.Distance(LocalScale, other.LocalScale) <= 0.0005f;
            }
        }

        private readonly struct TransformChange
        {
            public TransformChange(string path, TransformState expected, TransformState current)
            {
                Path = path;
                Expected = expected;
                Current = current;
            }

            public string Path { get; }
            public TransformState Expected { get; }
            public TransformState Current { get; }
        }

        private readonly struct TransformOverride
        {
            public TransformOverride(string path, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
            {
                Path = path;
                LocalPosition = localPosition;
                LocalRotation = localRotation;
                LocalScale = localScale;
            }

            public string Path { get; }
            public Vector3 LocalPosition { get; }
            public Quaternion LocalRotation { get; }
            public Vector3 LocalScale { get; }
        }

        private readonly struct LightingMaterials
        {
            public LightingMaterials(
                Material fixture,
                Material dark,
                Material worn,
                Material cool,
                Material warm,
                Material cyan,
                Material pool)
            {
                Fixture = fixture;
                Dark = dark;
                Worn = worn;
                Cool = cool;
                Warm = warm;
                Cyan = cyan;
                Pool = pool;
            }

            public Material Fixture { get; }
            public Material Dark { get; }
            public Material Worn { get; }
            public Material Cool { get; }
            public Material Warm { get; }
            public Material Cyan { get; }
            public Material Pool { get; }
        }
    }
}
