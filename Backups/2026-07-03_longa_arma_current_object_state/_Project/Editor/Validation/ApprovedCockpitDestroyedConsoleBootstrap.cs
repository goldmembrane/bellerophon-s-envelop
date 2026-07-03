using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Bellerophon.Core.Session;
using Bellerophon.Core.Ship;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.Validation
{
    public static class ApprovedCockpitDestroyedConsoleBootstrap
    {
        public const string RootName = "Approved Cockpit 09 Destroyed Console";
        public const string SwitcherRootName = "Approved Cockpit 09 Damage Visual Switcher";

        private const string SampleRootRelativePath = "artSample/ck_dmg09";
        private const string ComparisonRootName = "unity_applied_comparison";
        private const string SourceFbxRelativePath = "artSample/ck_dmg09/exports/ck_dmg09.fbx";
        private const string UnityAssetDirectory = "Assets/_Project/Art/Ship/Cockpit";
        private const string UnityFbxPath = UnityAssetDirectory + "/ck_dmg09.fbx";
        private const string BurntMetalMaterialPath = UnityAssetDirectory + "/M_CkDmg09_BurntMetal.mat";
        private const string CharcoalMaterialPath = UnityAssetDirectory + "/M_CkDmg09_Charcoal.mat";
        private const string ExposedMetalMaterialPath = UnityAssetDirectory + "/M_CkDmg09_ExposedMetal.mat";
        private const string BrassMaterialPath = UnityAssetDirectory + "/M_CkDmg09_Brass.mat";
        private const string RedGlowMaterialPath = UnityAssetDirectory + "/M_CkDmg09_RedGlow.mat";
        private const string AmberGlowMaterialPath = UnityAssetDirectory + "/M_CkDmg09_AmberGlow.mat";
        private const string CyanGlowMaterialPath = UnityAssetDirectory + "/M_CkDmg09_CyanGlow.mat";
        private const string ScorchMaterialPath = UnityAssetDirectory + "/M_CkDmg09_Scorch.mat";
        private const string SmokeMaterialPath = UnityAssetDirectory + "/M_CkDmg09_Smoke.mat";
        private static readonly Vector3 DestroyedRootPositionOffset = Vector3.zero;
        private static readonly Vector3 DestroyedRootRotationEulerOffset = Vector3.zero;
        private static readonly Vector3 DestroyedRootScaleMultiplier = Vector3.one;
        private static readonly string[] RemovedAfterEditorReviewObjectPaths =
        {
        };
        private static readonly TransformOverride[] EditorReviewTransformOverrides =
        {
            new TransformOverride(
                "CK-09 damage overlay on ck_ctl02_low/ck02 fallen original helm support torn end",
                new Vector3(0.00552f, -0.00034f, 0.00738f),
                Quaternion.Euler(23.58839f, 86.42368f, 88.56732f),
                Vector3.one),
            new TransformOverride(
                "helm bearing housing",
                new Vector3(-1.025223f, 1.038347f, -0.090881f),
                Quaternion.Euler(65.93703f, 2.742236f, 334.7937f),
                new Vector3(100f, 100f, 100f)),
            new TransformOverride(
                "helm outer hand knob 1",
                new Vector3(-1.383537f, 1.207247f, 0.304952f),
                Quaternion.Euler(338.3513f, 30.0132f, 349.2324f),
                new Vector3(99.99999f, 99.99999f, 100f)),
            new TransformOverride(
                "helm outer hand knob 3",
                new Vector3(-0.595f, 1.192f, 0.233f),
                Quaternion.Euler(338.3513f, 30.0132f, 349.2324f),
                new Vector3(99.99999f, 99.99999f, 100f)),
            new TransformOverride(
                "helm outer hand knob 4",
                new Vector3(-0.467643f, 1.027565f, -0.141759f),
                Quaternion.Euler(338.3513f, 30.0132f, 349.2324f),
                new Vector3(99.99999f, 99.99999f, 100f)),
            new TransformOverride(
                "helm outer hand knob 5",
                new Vector3(-0.666909f, 0.869446f, -0.486714f),
                Quaternion.Euler(338.3513f, 30.0132f, 349.2324f),
                new Vector3(99.99999f, 99.99999f, 100f)),
            new TransformOverride(
                "helm outer hand knob 8",
                new Vector3(-1.582803f, 1.049128f, -0.040003f),
                Quaternion.Euler(338.3513f, 30.0132f, 349.2324f),
                new Vector3(99.99999f, 99.99999f, 100f)),
            new TransformOverride(
                "helm radial spoke 1",
                new Vector3(-1.220376f, 1.130337f, 0.124707f),
                Quaternion.Euler(342.4458f, 317.8481f, 286.7253f),
                new Vector3(99.99999f, 99.99999f, 99.99999f)),
            new TransformOverride(
                "helm radial spoke 3",
                new Vector3(-0.790905f, 1.122033f, 0.085519f),
                Quaternion.Euler(344.0749f, 53.02683f, 71.72095f),
                new Vector3(99.99999f, 99.99999f, 100f)),
            new TransformOverride(
                "helm radial spoke 4",
                new Vector3(-0.721541f, 1.032475f, -0.118591f),
                Quaternion.Euler(1.103134f, 95.21371f, 65.96082f),
                new Vector3(99.99999f, 99.99998f, 99.99998f)),
            new TransformOverride(
                "helm radial spoke 5",
                new Vector3(-0.83007f, 0.946356f, -0.306468f),
                Quaternion.Euler(17.55419f, 137.8481f, 73.27473f),
                new Vector3(100f, 99.99998f, 99.99999f)),
            new TransformOverride(
                "helm radial spoke 8",
                new Vector3(-1.328905f, 1.044219f, -0.06317f),
                Quaternion.Euler(358.8969f, 275.2137f, 294.0392f),
                new Vector3(100f, 99.99998f, 100f)),
            new TransformOverride(
                "helm worn brass hub cap",
                new Vector3(0.721463f, 0.869f, -0.475f),
                Quaternion.Euler(65.93703f, 2.742236f, 334.7937f),
                new Vector3(100f, 100f, 100f)),
            new TransformOverride(
                "large ship helm wheel ring",
                new Vector3(-1.025223f, 1.038347f, -0.090881f),
                Quaternion.Euler(65.93703f, 2.742236f, 334.7937f),
                new Vector3(100f, 100f, 100f)),
        };

        [MenuItem("Bellerophon/Bootstrap/Ensure Approved Cockpit 09 Destroyed Console")]
        public static void EnsureApprovedCockpitDestroyedConsole()
        {
            var scene = EditorSceneManager.OpenScene(Phase4CargoShipGrayboxBootstrap.CargoRunScenePath, OpenSceneMode.Single);

            var normalRoot = RequireObject(ApprovedCockpitConsoleBootstrap.RootName);
            DeleteGeneratedObject(RootName);
            DeleteGeneratedObject(SwitcherRootName);
            CopyApprovedSourceFbx();

            var source = AssetDatabase.LoadAssetAtPath<GameObject>(UnityFbxPath);
            if (source == null)
            {
                throw new InvalidOperationException("Approved cockpit destroyed console source FBX failed to import: " + UnityFbxPath);
            }

            var destroyedRoot = PrefabUtility.InstantiatePrefab(source) as GameObject;
            if (destroyedRoot == null)
            {
                throw new InvalidOperationException("Approved cockpit destroyed console source FBX could not be instantiated: " + UnityFbxPath);
            }

            destroyedRoot.name = RootName;
            ApplyRootPlacement(destroyedRoot.transform, normalRoot.transform);

            RemovePreviewOnlyObjects(destroyedRoot.transform);
            RemoveEditorReviewObjects(destroyedRoot.transform);
            ApplyEditorReviewTransformOverrides(destroyedRoot.transform);
            ApplyApprovedMaterials(destroyedRoot.transform, EnsureMaterials());
            DisableAllColliders(destroyedRoot.transform);
            destroyedRoot.SetActive(false);

            var interactionState = RequireShipDeviceState();
            var switcherObject = new GameObject(SwitcherRootName);
            var switcher = switcherObject.AddComponent<CockpitConsoleDamageVisualSwitcher>();
            switcher.Configure(interactionState, normalRoot, destroyedRoot);
            interactionState.SetShipState(ShipState.CreateDefault());
            switcher.Refresh();

            EditorUtility.SetDirty(switcherObject);
            EditorUtility.SetDirty(normalRoot);
            EditorUtility.SetDirty(destroyedRoot);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, Phase4CargoShipGrayboxBootstrap.CargoRunScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            ValidateScene();
            Debug.Log("Approved cockpit 09 destroyed console applied.");
        }

        [MenuItem("Bellerophon/Validation/Validate Approved Cockpit 09 Destroyed Console")]
        public static void ValidateScene()
        {
            EditorSceneManager.OpenScene(Phase4CargoShipGrayboxBootstrap.CargoRunScenePath, OpenSceneMode.Single);

            ApprovedCockpitStructureBootstrap.ValidateScene();
            ApprovedCockpitWindowBootstrap.ValidateScene();
            ApprovedCockpitConsoleBootstrap.ValidateScene();

            var normalRoot = RequireObject(ApprovedCockpitConsoleBootstrap.RootName);
            var destroyedRoot = RequireObject(RootName);
            var switcher = RequireSwitcher();
            var interactionState = RequireShipDeviceState();

            if (switcher.InteractionState != interactionState)
            {
                throw new InvalidOperationException("Cockpit 09 visual switcher does not reference the scene ShipDeviceInteractionState.");
            }

            if (switcher.NormalConsoleRoot != normalRoot)
            {
                throw new InvalidOperationException("Cockpit 09 visual switcher must keep the approved normal CK-02 console as the recoverable root.");
            }

            if (switcher.DestroyedConsoleRoot != destroyedRoot)
            {
                throw new InvalidOperationException("Cockpit 09 visual switcher does not reference the destroyed CK-09 console root.");
            }

            var normalState = ShipState.CreateDefault();
            interactionState.SetShipState(normalState);
            switcher.Refresh();
            if (!normalRoot.activeSelf || destroyedRoot.activeSelf || switcher.IsDestroyedVisualActive)
            {
                throw new InvalidOperationException("Cockpit 09 default state must show CK-02 and hide CK-09 so the normal console remains recoverable.");
            }

            ValidateDestroyedRootPlacement(normalRoot, destroyedRoot);
            var renderers = ValidateDestroyedRootContents(destroyedRoot);
            ValidateNoPreviewObjects(destroyedRoot.transform);

            var enabledColliders = CountEnabledColliders(destroyedRoot.transform);
            if (enabledColliders != 0)
            {
                throw new InvalidOperationException("Approved cockpit 09 destroyed console must not introduce gameplay colliders. EnabledColliders=" + enabledColliders);
            }

            interactionState.SetShipState(normalState.WithRoom(ShipRoomId.Cockpit, new ShipRoomState(0, 100)));
            switcher.Refresh();
            if (normalRoot.activeSelf || !destroyedRoot.activeSelf || !switcher.IsDestroyedVisualActive)
            {
                throw new InvalidOperationException("Cockpit 09 destroyed state must hide CK-02 and show CK-09 at zero cockpit durability.");
            }

            interactionState.SetShipState(normalState.WithRoom(ShipRoomId.Cockpit, new ShipRoomState(100, 100)));
            switcher.Refresh();
            if (!normalRoot.activeSelf || destroyedRoot.activeSelf || switcher.IsDestroyedVisualActive)
            {
                throw new InvalidOperationException("Cockpit 09 repaired state must restore CK-02 and hide CK-09.");
            }

            Debug.Log(
                "Approved cockpit 09 destroyed console validation passed. NormalRecoverable=True; DestroyedDefaultHidden=True; Renderers=" +
                renderers +
                "; EnabledColliders=0");
        }

        [MenuItem("Bellerophon/Validation/Capture Approved Cockpit 09 Destroyed Console Comparison")]
        public static void CaptureUnityComparison()
        {
            ValidateScene();

            var projectRoot = Directory.GetParent(Application.dataPath);
            if (projectRoot == null)
            {
                throw new InvalidOperationException("Could not resolve project root for cockpit 09 comparison output.");
            }

            var outputRoot = Path.Combine(projectRoot.FullName, SampleRootRelativePath, ComparisonRootName);
            Directory.CreateDirectory(outputRoot);

            var normalRoot = RequireObject(ApprovedCockpitConsoleBootstrap.RootName);
            var destroyedRoot = RequireObject(RootName);
            var switcher = RequireSwitcher();
            var interactionState = RequireShipDeviceState();
            var normalState = ShipState.CreateDefault();
            var consolePosition = normalRoot.transform.position;

            interactionState.SetShipState(normalState.WithRoom(ShipRoomId.Cockpit, new ShipRoomState(0, 100)));
            switcher.Refresh();

            try
            {
                CaptureAppliedView(
                    Path.Combine(outputRoot, "unity_01_front.png"),
                    consolePosition + new Vector3(0f, 2.35f, 5.25f),
                    consolePosition + new Vector3(0f, 1.28f, -0.1f),
                    36f,
                    false,
                    5f,
                    Vector3.up);
                CaptureAppliedView(
                    Path.Combine(outputRoot, "unity_02_player.png"),
                    consolePosition + new Vector3(0f, 1.58f, 3.2f),
                    consolePosition + new Vector3(0f, 1.3f, -0.15f),
                    32f,
                    false,
                    5f,
                    Vector3.up);
                CaptureAppliedView(
                    Path.Combine(outputRoot, "unity_03_side.png"),
                    consolePosition + new Vector3(4.8f, 2.0f, 2.4f),
                    consolePosition + new Vector3(0.2f, 1.02f, -0.15f),
                    42f,
                    false,
                    5f,
                    Vector3.up);
                CaptureAppliedView(
                    Path.Combine(outputRoot, "unity_04_top.png"),
                    consolePosition + new Vector3(0f, 7.2f, 0.2f),
                    consolePosition + new Vector3(0f, 0f, 0.05f),
                    45f,
                    true,
                    6f,
                    Vector3.forward);
                CaptureAppliedView(
                    Path.Combine(outputRoot, "unity_05_detail.png"),
                    consolePosition + new Vector3(-1.8f, 2.25f, 2.7f),
                    consolePosition + new Vector3(0.6f, 1.45f, -0.25f),
                    48f,
                    false,
                    5f,
                    Vector3.up);
            }
            finally
            {
                interactionState.SetShipState(normalState);
                switcher.Refresh();
                normalRoot.SetActive(true);
                destroyedRoot.SetActive(false);
            }

            WriteComparisonIndex(outputRoot);
            AssetDatabase.Refresh();
            Debug.Log("Approved cockpit 09 destroyed console Unity comparison snapshots saved: " + outputRoot);
        }

        [MenuItem("Bellerophon/Inspection/Show Approved Cockpit 09 Destroyed Console")]
        public static void ShowDestroyedConsoleForInspection()
        {
            EditorSceneManager.OpenScene(Phase4CargoShipGrayboxBootstrap.CargoRunScenePath, OpenSceneMode.Single);

            var normalRoot = RequireObject(ApprovedCockpitConsoleBootstrap.RootName);
            var destroyedRoot = RequireObject(RootName);
            normalRoot.SetActive(false);
            destroyedRoot.SetActive(true);

            EditorUtility.SetDirty(normalRoot);
            EditorUtility.SetDirty(destroyedRoot);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null)
            {
                sceneView.LookAt(destroyedRoot.transform.position + new Vector3(0f, 1.1f, 0f), Quaternion.Euler(20f, 180f, 0f), 5.0f);
                sceneView.Repaint();
            }

            Debug.Log("Approved cockpit 09 destroyed console shown for inspection. Normal CK-02 console hidden but preserved.");
        }

        [MenuItem("Bellerophon/Inspection/Show Approved Cockpit 02 Normal Console")]
        public static void ShowNormalConsoleForInspection()
        {
            var scene = EditorSceneManager.OpenScene(Phase4CargoShipGrayboxBootstrap.CargoRunScenePath, OpenSceneMode.Single);

            var normalRoot = RequireObject(ApprovedCockpitConsoleBootstrap.RootName);
            var destroyedRoot = RequireObject(RootName);
            var switcher = RequireSwitcher();
            var interactionState = RequireShipDeviceState();

            interactionState.SetShipState(ShipState.CreateDefault());
            switcher.Refresh();
            normalRoot.SetActive(true);
            destroyedRoot.SetActive(false);

            EditorUtility.SetDirty(normalRoot);
            EditorUtility.SetDirty(destroyedRoot);
            EditorUtility.SetDirty(switcher);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, Phase4CargoShipGrayboxBootstrap.CargoRunScenePath);

            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null)
            {
                sceneView.LookAt(normalRoot.transform.position + new Vector3(0f, 1.1f, 0f), Quaternion.Euler(20f, 180f, 0f), 5.0f);
                sceneView.Repaint();
            }

            Debug.Log("Approved cockpit 02 normal console shown for inspection. CK-09 destroyed console hidden until cockpit durability reaches zero.");
        }

        [MenuItem("Bellerophon/Inspection/Capture Approved Cockpit 09 Current Objects")]
        public static void CaptureCurrentEditorObjects()
        {
            var currentRoot = RequireObject(RootName);
            var normalRoot = RequireObject(ApprovedCockpitConsoleBootstrap.RootName);
            CopyApprovedSourceFbx();

            var source = AssetDatabase.LoadAssetAtPath<GameObject>(UnityFbxPath);
            if (source == null)
            {
                throw new InvalidOperationException("Approved cockpit 09 source FBX failed to import for current object capture: " + UnityFbxPath);
            }

            var expectedRoot = PrefabUtility.InstantiatePrefab(source) as GameObject;
            if (expectedRoot == null)
            {
                throw new InvalidOperationException("Approved cockpit 09 source FBX could not be instantiated for current object capture: " + UnityFbxPath);
            }

            expectedRoot.name = RootName + " Expected Capture";
            expectedRoot.hideFlags = HideFlags.HideAndDontSave;
            ApplyRootPlacement(expectedRoot.transform, normalRoot.transform);

            try
            {
                RemovePreviewOnlyObjects(expectedRoot.transform);
                RemoveEditorReviewObjects(expectedRoot.transform);
                ApplyEditorReviewTransformOverrides(expectedRoot.transform);

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
                    throw new InvalidOperationException("Could not resolve project root for cockpit 09 current object capture.");
                }

                var outputRoot = Path.Combine(projectRoot.FullName, SampleRootRelativePath, "editor_current");
                Directory.CreateDirectory(outputRoot);

                var builder = new StringBuilder();
                builder.AppendLine("# Approved Cockpit 09 Current Object Capture");
                builder.AppendLine();
                builder.AppendLine("이 파일은 현재 열린 Unity 씬의 CK-09 편집 상태와 스크립트 생성 기준을 비교한 결과입니다.");
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
                builder.AppendLine("## Root Transform Candidate");
                builder.AppendLine();
                builder.AppendLine("PositionOffset=" + FormatVectorPrecise(currentRoot.transform.position - normalRoot.transform.position));
                builder.AppendLine("RotationEulerOffset=" + FormatVectorPrecise((Quaternion.Inverse(normalRoot.transform.rotation) * currentRoot.transform.rotation).eulerAngles));
                builder.AppendLine("ScaleMultiplier=" + FormatVectorPrecise(GetScaleMultiplier(currentRoot.transform.localScale, normalRoot.transform.localScale)));
                builder.AppendLine();
                AppendPathSection(builder, "MissingTransforms", missingTransforms);
                builder.AppendLine();
                AppendPathSection(builder, "AddedTransforms", addedTransforms);
                builder.AppendLine();
                AppendTransformChangeSection(builder, "ChangedTransforms", changedTransforms);
                builder.AppendLine();
                AppendTransformOverrideCandidates(builder, changedTransforms);

                File.WriteAllText(Path.Combine(outputRoot, "ck09_current_objects.md"), builder.ToString(), new UTF8Encoding(false));
                AssetDatabase.Refresh();
                Debug.Log("Approved cockpit 09 current object capture saved: " + outputRoot);
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
                throw new InvalidOperationException("Could not resolve project root for cockpit 09 source FBX.");
            }

            var sourcePath = Path.Combine(projectRoot.FullName, SourceFbxRelativePath);
            if (!File.Exists(sourcePath))
            {
                throw new FileNotFoundException("Missing approved cockpit 09 source FBX.", sourcePath);
            }

            var targetDirectory = Path.Combine(projectRoot.FullName, UnityAssetDirectory);
            Directory.CreateDirectory(targetDirectory);
            File.Copy(sourcePath, Path.Combine(projectRoot.FullName, UnityFbxPath), true);
            AssetDatabase.ImportAsset(UnityFbxPath, ImportAssetOptions.ForceUpdate);
        }

        private static DestroyedConsoleMaterials EnsureMaterials()
        {
            Directory.CreateDirectory(UnityAssetDirectory);
            return new DestroyedConsoleMaterials(
                EnsureMaterial(BurntMetalMaterialPath, new Color(0.045f, 0.046f, 0.040f, 1f), 0.08f, 0.08f, false, false),
                EnsureMaterial(CharcoalMaterialPath, new Color(0.006f, 0.006f, 0.005f, 1f), 0f, 0.04f, false, false),
                EnsureMaterial(ExposedMetalMaterialPath, new Color(0.50f, 0.48f, 0.42f, 1f), 0.32f, 0.18f, false, false),
                EnsureMaterial(BrassMaterialPath, new Color(0.30f, 0.22f, 0.12f, 1f), 0.18f, 0.14f, false, false),
                EnsureMaterial(RedGlowMaterialPath, new Color(0.58f, 0.045f, 0.020f, 1f), 0f, 0.18f, true, false),
                EnsureMaterial(AmberGlowMaterialPath, new Color(0.72f, 0.36f, 0.055f, 1f), 0f, 0.18f, true, false),
                EnsureMaterial(CyanGlowMaterialPath, new Color(0.075f, 0.48f, 0.56f, 1f), 0f, 0.18f, true, false),
                EnsureMaterial(ScorchMaterialPath, new Color(0f, 0f, 0f, 0.54f), 0f, 0.04f, false, true),
                EnsureMaterial(SmokeMaterialPath, new Color(0.15f, 0.15f, 0.14f, 0.12f), 0f, 0.06f, false, true));
        }

        private static Material EnsureMaterial(
            string path,
            Color color,
            float metallic,
            float smoothness,
            bool emissive,
            bool transparent)
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
                var emission = color * 0.85f;
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

                if (IsPreviewOnlyObject(target))
                {
                    UnityEngine.Object.DestroyImmediate(target.gameObject);
                }
            }
        }

        private static bool IsPreviewOnlyObject(Transform target)
        {
            var lowerName = target.name.ToLowerInvariant();
            return lowerName.Contains("context") ||
                lowerName.Contains("camera") ||
                lowerName.Contains("softbox") ||
                lowerName.Contains("spill") ||
                target.GetComponent<Camera>() != null ||
                target.GetComponent<Light>() != null;
        }

        private static void RemoveEditorReviewObjects(Transform root)
        {
            for (var i = 0; i < RemovedAfterEditorReviewObjectPaths.Length; i++)
            {
                var target = FindRelativeTransform(root, RemovedAfterEditorReviewObjectPaths[i]);
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
                    throw new InvalidOperationException("Missing cockpit 09 transform override target: " + overrideValue.Path);
                }

                target.localPosition = overrideValue.LocalPosition;
                target.localRotation = overrideValue.LocalRotation;
                target.localScale = overrideValue.LocalScale;
            }
        }

        private static void ApplyApprovedMaterials(Transform root, DestroyedConsoleMaterials materials)
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

        private static Material ResolveMaterial(string sourceName, string objectName, DestroyedConsoleMaterials materials)
        {
            if (sourceName.Contains("smoke") || objectName.Contains("smoke") || objectName.Contains("wisp"))
            {
                return materials.Smoke;
            }

            if (sourceName.Contains("scorch") ||
                sourceName.Contains("soot") ||
                objectName.Contains("scorch") ||
                objectName.Contains("soot") ||
                objectName.Contains("burn mark"))
            {
                return materials.Scorch;
            }

            if (objectName.Contains("single-piece heavy lower console base") ||
                objectName.Contains("sloped main control deck") ||
                objectName.Contains("raised rear instrument coaming") ||
                objectName.Contains("recessed black hand rest strip") ||
                objectName.Contains("front armored lower kick plate") ||
                objectName.Contains("angled side cheek") ||
                objectName.Contains("floor bolted console foot"))
            {
                return materials.BurntMetal;
            }

            if (objectName.Contains("control plate") ||
                objectName.Contains("mechanical switch panel") ||
                objectName.Contains("lever recessed slot") ||
                objectName.Contains("collapsed right switch panel"))
            {
                return materials.BurntMetal;
            }

            if (sourceName.Contains("cyan") || objectName.Contains("cyan"))
            {
                return materials.CyanGlow;
            }

            if (sourceName.Contains("amber") || objectName.Contains("amber"))
            {
                return materials.AmberGlow;
            }

            var redSource = sourceName.Contains("red") || sourceName.Contains("spark") || sourceName.Contains("glow");
            var redPart = objectName.Contains("red") ||
                objectName.Contains("spark") ||
                objectName.Contains("wire") ||
                objectName.Contains("horizontal grip") ||
                objectName.Contains("thumb") ||
                objectName.Contains("throttle") ||
                objectName.Contains("glow");
            if (redSource && redPart)
            {
                return materials.RedGlow;
            }

            if (sourceName.Contains("brass") ||
                objectName.Contains("helm") ||
                objectName.Contains("spoke") ||
                objectName.Contains("wheel") ||
                objectName.Contains("knob"))
            {
                return materials.Brass;
            }

            if (sourceName.Contains("exposed") ||
                sourceName.Contains("wear") ||
                objectName.Contains("exposed") ||
                objectName.Contains("wear") ||
                objectName.Contains("chip") ||
                objectName.Contains("cut") ||
                objectName.Contains("debris") ||
                objectName.Contains("shrapnel") ||
                objectName.Contains("fragment") ||
                objectName.Contains("bolt"))
            {
                return materials.ExposedMetal;
            }

            if (sourceName.Contains("charcoal") ||
                sourceName.Contains("rubber") ||
                sourceName.Contains("dark") ||
                objectName.Contains("charcoal") ||
                objectName.Contains("rubber") ||
                objectName.Contains("cavity") ||
                objectName.Contains("blackened") ||
                objectName.Contains("dark"))
            {
                return materials.Charcoal;
            }

            return materials.BurntMetal;
        }

        private static void ValidateDestroyedRootPlacement(GameObject normalRoot, GameObject destroyedRoot)
        {
            var expectedPosition = normalRoot.transform.position + DestroyedRootPositionOffset;
            var expectedRotation = normalRoot.transform.rotation * Quaternion.Euler(DestroyedRootRotationEulerOffset);
            var expectedScale = Vector3.Scale(normalRoot.transform.localScale, DestroyedRootScaleMultiplier);

            var positionDelta = Vector3.Distance(expectedPosition, destroyedRoot.transform.position);
            if (positionDelta > 0.01f)
            {
                throw new InvalidOperationException("CK-09 destroyed console does not match the scripted editor-reviewed position. Delta=" + positionDelta.ToString("0.000"));
            }

            var rotationDelta = Quaternion.Angle(expectedRotation, destroyedRoot.transform.rotation);
            if (rotationDelta > 0.1f)
            {
                throw new InvalidOperationException("CK-09 destroyed console does not match the scripted editor-reviewed rotation. RotationDelta=" + rotationDelta.ToString("0.000"));
            }

            var scaleDelta = Vector3.Distance(expectedScale, destroyedRoot.transform.localScale);
            if (scaleDelta > 0.01f)
            {
                throw new InvalidOperationException("CK-09 destroyed console does not match the scripted editor-reviewed scale. ScaleDelta=" + scaleDelta.ToString("0.000"));
            }
        }

        private static int ValidateDestroyedRootContents(GameObject destroyedRoot)
        {
            var renderers = destroyedRoot.GetComponentsInChildren<Renderer>(true);
            var enabledRenderers = 0;
            var hasHelm = false;
            var hasLever = false;
            var hasWire = false;
            var hasDebris = false;

            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (!renderer.enabled)
                {
                    continue;
                }

                enabledRenderers++;
                var lowerName = renderer.gameObject.name.ToLowerInvariant();
                hasHelm |= lowerName.Contains("helm") || lowerName.Contains("spoke") || lowerName.Contains("wheel");
                hasLever |= lowerName.Contains("lever");
                hasWire |= lowerName.Contains("wire");
                hasDebris |= lowerName.Contains("debris") || lowerName.Contains("shrapnel") || lowerName.Contains("fragment");
            }

            if (enabledRenderers < 25)
            {
                throw new InvalidOperationException("Approved cockpit 09 destroyed console renderer count is too low: " + enabledRenderers);
            }

            if (!hasHelm || !hasLever || !hasWire || !hasDebris)
            {
                throw new InvalidOperationException(
                    "Approved cockpit 09 destroyed console is missing expected damaged CK-02 parts. HasHelm=" +
                    hasHelm +
                    "; HasLever=" +
                    hasLever +
                    "; HasWire=" +
                    hasWire +
                    "; HasDebris=" +
                    hasDebris);
            }

            return enabledRenderers;
        }

        private static void ValidateNoPreviewObjects(Transform root)
        {
            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                var transform = transforms[i];
                if (transform == root)
                {
                    continue;
                }

                if (IsPreviewOnlyObject(transform))
                {
                    throw new InvalidOperationException("Approved cockpit 09 destroyed console contains a preview-only object: " + transform.name);
                }
            }
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
            var cameraObject = new GameObject("Approved Cockpit 09 Comparison Camera")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            var keyLightObject = new GameObject("Approved Cockpit 09 Comparison Key Light")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            var fillLightObject = new GameObject("Approved Cockpit 09 Comparison Fill Light")
            {
                hideFlags = HideFlags.HideAndDontSave
            };

            try
            {
                var normalRoot = RequireObject(ApprovedCockpitConsoleBootstrap.RootName);
                var consolePosition = normalRoot.transform.position;

                keyLightObject.transform.position = consolePosition + new Vector3(0f, 4.8f, 2.6f);
                var keyLight = keyLightObject.AddComponent<Light>();
                keyLight.type = LightType.Rectangle;
                keyLight.color = new Color(1f, 0.90f, 0.80f, 1f);
                keyLight.intensity = 470f;
                keyLight.range = 12f;
                keyLight.areaSize = new Vector2(6.5f, 6.5f);

                fillLightObject.transform.position = consolePosition + new Vector3(-2.6f, 1.8f, -1.4f);
                var fillLight = fillLightObject.AddComponent<Light>();
                fillLight.type = LightType.Point;
                fillLight.color = new Color(0.75f, 0.18f, 0.10f, 1f);
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
                camera.backgroundColor = new Color(0.014f, 0.016f, 0.018f, 1f);
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
            builder.AppendLine("<head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width, initial-scale=1\"><title>CK-09 Unity comparison</title>");
            builder.AppendLine("<style>body{margin:0;background:#111514;color:#ece5d8;font-family:Arial,sans-serif}main{max-width:1400px;margin:0 auto;padding:24px}h1{font-size:27px;margin:0 0 8px}.meta{color:#cfc6b8;margin:0 0 18px}.grid{display:grid;gap:18px}.pair{display:grid;grid-template-columns:1fr 1fr;gap:12px;border:1px solid #3c4643;background:#1c2220;border-radius:6px;padding:12px}.pair h2{grid-column:1/-1;font-size:18px;margin:0}.pair img{display:block;width:100%;height:auto;background:#050807}.label{font-size:13px;color:#ddd3c3;margin:6px 0 0}@media(max-width:900px){.pair{grid-template-columns:1fr}}</style>");
            builder.AppendLine("</head><body><main>");
            builder.AppendLine("<h1>CK-09 Unity comparison</h1>");
            builder.AppendLine("<p class=\"meta\">Left: approved artSample render. Right: CK-09 placed at the preserved CK-02 console position in CargoRunMvp. The normal CK-02 root remains recoverable and is hidden only while cockpit durability is zero.</p>");
            builder.AppendLine("<section class=\"grid\">");
            AddComparisonPair(builder, "01 Front", "../renders/01_front.png", "unity_01_front.png");
            AddComparisonPair(builder, "02 Player view", "../renders/02_player.png", "unity_02_player.png");
            AddComparisonPair(builder, "03 Side", "../renders/03_side.png", "unity_03_side.png");
            AddComparisonPair(builder, "04 Top", "../renders/04_top.png", "unity_04_top.png");
            AddComparisonPair(builder, "05 Detail", "../renders/05_detail.png", "unity_05_detail.png");
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

        private static CockpitConsoleDamageVisualSwitcher RequireSwitcher()
        {
            var switcherRoot = RequireObject(SwitcherRootName);
            var switcher = switcherRoot.GetComponent<CockpitConsoleDamageVisualSwitcher>();
            if (switcher == null)
            {
                throw new InvalidOperationException("Missing CockpitConsoleDamageVisualSwitcher on " + SwitcherRootName);
            }

            return switcher;
        }

        private static ShipDeviceInteractionState RequireShipDeviceState()
        {
            var activeScene = SceneManager.GetActiveScene();
            var states = Resources.FindObjectsOfTypeAll<ShipDeviceInteractionState>();
            for (var i = 0; i < states.Length; i++)
            {
                var state = states[i];
                if (state == null ||
                    EditorUtility.IsPersistent(state) ||
                    state.gameObject.scene != activeScene)
                {
                    continue;
                }

                return state;
            }

            throw new InvalidOperationException("Missing scene ShipDeviceInteractionState required for cockpit 09 visual switching.");
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

        private static void ApplyRootPlacement(Transform destroyedRoot, Transform normalRoot)
        {
            destroyedRoot.SetPositionAndRotation(
                normalRoot.position + DestroyedRootPositionOffset,
                normalRoot.rotation * Quaternion.Euler(DestroyedRootRotationEulerOffset));
            destroyedRoot.localScale = Vector3.Scale(normalRoot.localScale, DestroyedRootScaleMultiplier);
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

        private static Vector3 GetScaleMultiplier(Vector3 value, Vector3 baseline)
        {
            return new Vector3(
                baseline.x == 0f ? 1f : value.x / baseline.x,
                baseline.y == 0f ? 1f : value.y / baseline.y,
                baseline.z == 0f ? 1f : value.z / baseline.z);
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

        private readonly struct DestroyedConsoleMaterials
        {
            public DestroyedConsoleMaterials(
                Material burntMetal,
                Material charcoal,
                Material exposedMetal,
                Material brass,
                Material redGlow,
                Material amberGlow,
                Material cyanGlow,
                Material scorch,
                Material smoke)
            {
                BurntMetal = burntMetal;
                Charcoal = charcoal;
                ExposedMetal = exposedMetal;
                Brass = brass;
                RedGlow = redGlow;
                AmberGlow = amberGlow;
                CyanGlow = cyanGlow;
                Scorch = scorch;
                Smoke = smoke;
            }

            public Material BurntMetal { get; }
            public Material Charcoal { get; }
            public Material ExposedMetal { get; }
            public Material Brass { get; }
            public Material RedGlow { get; }
            public Material AmberGlow { get; }
            public Material CyanGlow { get; }
            public Material Scorch { get; }
            public Material Smoke { get; }
        }
    }
}
