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
    public static class ApprovedCockpitConsoleBootstrap
    {
        public const string RootName = "Approved Cockpit 02 Console";

        private const string SampleRootRelativePath = "artSample/ck_ctl02_low";
        private const string ComparisonRootName = "unity_applied_comparison";
        private const string SourceFbxRelativePath = "artSample/ck_ctl02_low/exports/ck_ctl02_low.fbx";
        private const string UnityAssetDirectory = "Assets/_Project/Art/Ship/Cockpit";
        private const string UnityFbxPath = UnityAssetDirectory + "/ck_ctl02_low.fbx";
        private const string BodyMaterialPath = UnityAssetDirectory + "/M_CkCtl02_Body.mat";
        private const string FrameMaterialPath = UnityAssetDirectory + "/M_CkCtl02_Frame.mat";
        private const string PanelMaterialPath = UnityAssetDirectory + "/M_CkCtl02_Panel.mat";
        private const string DarkMaterialPath = UnityAssetDirectory + "/M_CkCtl02_Dark.mat";
        private const string RubberMaterialPath = UnityAssetDirectory + "/M_CkCtl02_Rubber.mat";
        private const string BrassMaterialPath = UnityAssetDirectory + "/M_CkCtl02_Brass.mat";
        private const string LabelMaterialPath = UnityAssetDirectory + "/M_CkCtl02_Label.mat";
        private const string WearMaterialPath = UnityAssetDirectory + "/M_CkCtl02_Wear.mat";
        private const string AmberMaterialPath = UnityAssetDirectory + "/M_CkCtl02_Amber.mat";
        private const string RedMaterialPath = UnityAssetDirectory + "/M_CkCtl02_Red.mat";
        private const string GreenMaterialPath = UnityAssetDirectory + "/M_CkCtl02_Green.mat";

        private static readonly Vector3 CockpitCenter = new Vector3(0f, 0f, 18f);
        private static readonly Vector3 ConsoleWorldPosition = CockpitCenter + new Vector3(0f, 0f, -2.35f);
        private static readonly Quaternion ConsoleWorldRotation = Quaternion.identity;
        private static readonly Vector3 ConsoleWorldScale = Vector3.one;
        private static readonly string[] RemovedAfterEditorReviewObjectPaths =
        {
            "CK-02 main cockpit console sample/context asset pilot_seat scale reference only",
            "CK-02 placement context - cockpit front only",
            "cam_detail",
            "cam_front",
            "cam_player",
            "cam_side",
            "cam_top",
            "green console spill light",
            "large cockpit console key light",
            "warm edge inspection light",
            "CK-02 main cockpit console sample/left caution label strip",
            "CK-02 main cockpit console sample/left mechanical switch panel",
            "CK-02 main cockpit console sample/left toggle switch 1-1",
            "CK-02 main cockpit console sample/left toggle switch 1-2",
            "CK-02 main cockpit console sample/left toggle switch 1-3",
            "CK-02 main cockpit console sample/left toggle switch 1-4",
            "CK-02 main cockpit console sample/left toggle switch 2-1",
            "CK-02 main cockpit console sample/left toggle switch 2-2",
            "CK-02 main cockpit console sample/left toggle switch 2-3",
            "CK-02 main cockpit console sample/left toggle switch 2-4",
            "CK-02 main cockpit console sample/paint worn bright edge chip 1",
            "CK-02 main cockpit console sample/paint worn bright edge chip 2",
            "CK-02 main cockpit console sample/paint worn bright edge chip 3",
            "CK-02 main cockpit console sample/paint worn bright edge chip 4",
            "CK-02 main cockpit console sample/right caution label strip",
            "CK-02 main cockpit console sample/right lever forward stop block",
            "CK-02 main cockpit console sample/right lever travel notch 1",
            "CK-02 main cockpit console sample/right lever travel notch 2",
            "CK-02 main cockpit console sample/right lever travel notch 3",
            "CK-02 main cockpit console sample/right lever travel notch 4",
            "CK-02 main cockpit console sample/right lever travel notch 5",
            "CK-02 main cockpit console sample/right mechanical switch panel",
            "CK-02 main cockpit console sample/right toggle switch 1-1",
            "CK-02 main cockpit console sample/right toggle switch 1-2",
            "CK-02 main cockpit console sample/right toggle switch 1-3",
            "CK-02 main cockpit console sample/right toggle switch 1-4",
            "CK-02 main cockpit console sample/right toggle switch 2-1",
            "CK-02 main cockpit console sample/right toggle switch 2-2",
            "CK-02 main cockpit console sample/right toggle switch 2-3",
            "CK-02 main cockpit console sample/right toggle switch 2-4"
        };
        private static readonly TransformOverride[] EditorReviewTransformOverrides =
        {
            new TransformOverride(
                "CK-02 main cockpit console sample/central blank armored control plate",
                new Vector3(0f, 0.00684f, 0.009f),
                Quaternion.Euler(50f, 0f, 0f),
                Vector3.one),
            new TransformOverride(
                "CK-02 main cockpit console sample/F interaction anchor plate",
                new Vector3(0f, -0.00178f, 0.0062f),
                Quaternion.Euler(354f, 0f, 0f),
                Vector3.one),
            new TransformOverride(
                "CK-02 main cockpit console sample/F interaction letter marker",
                new Vector3(0f, -0.00212f, 0.00614f),
                Quaternion.Euler(84.00002f, 0f, 0f),
                Vector3.one)
        };

        [MenuItem("Bellerophon/Bootstrap/Ensure Approved Cockpit 02 Console")]
        public static void EnsureApprovedCockpitConsole()
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

            DeleteGeneratedObject(RootName);
            CopyApprovedSourceFbx();

            var source = AssetDatabase.LoadAssetAtPath<GameObject>(UnityFbxPath);
            if (source == null)
            {
                throw new InvalidOperationException("Approved cockpit console source FBX failed to import: " + UnityFbxPath);
            }

            var root = PrefabUtility.InstantiatePrefab(source) as GameObject;
            if (root == null)
            {
                throw new InvalidOperationException("Approved cockpit console source FBX could not be instantiated: " + UnityFbxPath);
            }

            root.name = RootName;
            root.transform.position = ConsoleWorldPosition;
            root.transform.rotation = ConsoleWorldRotation;
            root.transform.localScale = ConsoleWorldScale;

            var materials = EnsureMaterials();
            RemovePreviewOnlyObjects(root.transform);
            RemoveEditorReviewObjects(root.transform);
            ApplyEditorReviewTransformOverrides(root.transform);
            ApplyApprovedMaterials(root.transform, materials);
            DisableAllColliders(root.transform);
            ModelingInspectionModeBootstrap.ApplyFreeCameraForModeling();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, Phase4CargoShipGrayboxBootstrap.CargoRunScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            ValidateScene();
            Debug.Log("Approved cockpit 02 console applied.");
        }

        [MenuItem("Bellerophon/Validation/Validate Approved Cockpit 02 Console")]
        public static void ValidateScene()
        {
            EditorSceneManager.OpenScene(Phase4CargoShipGrayboxBootstrap.CargoRunScenePath, OpenSceneMode.Single);

            ApprovedCockpitStructureBootstrap.ValidateScene();
            ApprovedCockpitWindowBootstrap.ValidateScene();

            var root = RequireObject(RootName);
            if (!root.activeInHierarchy)
            {
                throw new InvalidOperationException(RootName + " must be active after user approval.");
            }

            var positionDelta = Vector3.Distance(root.transform.position, ConsoleWorldPosition);
            if (positionDelta > 0.025f)
            {
                throw new InvalidOperationException("Approved cockpit console is not placed in front of the approved cockpit window. Delta=" + positionDelta.ToString("0.000"));
            }

            var rotationDelta = Quaternion.Angle(root.transform.rotation, ConsoleWorldRotation);
            if (rotationDelta > 0.25f)
            {
                throw new InvalidOperationException("Approved cockpit console rotation changed unexpectedly. RotationDelta=" + rotationDelta.ToString("0.000"));
            }

            var renderers = root.GetComponentsInChildren<Renderer>(true);
            var enabledRenderers = 0;
            var hasHelm = false;
            var hasForwardLever = false;
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
                hasForwardLever |= lowerName.Contains("forward lever") || lowerName.Contains("push lever");
            }

            if (enabledRenderers < 55)
            {
                throw new InvalidOperationException("Approved cockpit console renderer count is too low: " + enabledRenderers);
            }

            if (!hasHelm || !hasForwardLever)
            {
                throw new InvalidOperationException("Approved cockpit console must contain the helm wheel and right forward lever. HasHelm=" + hasHelm + "; HasForwardLever=" + hasForwardLever);
            }

            if (ContainsNamedTransform(root.transform, "central monitor") ||
                ContainsNamedTransform(root.transform, "console_screen") ||
                ContainsNamedTransform(root.transform, "big_screen") ||
                ContainsNamedTransform(root.transform, "central green status screen") ||
                ContainsNamedTransform(root.transform, "front broad window screen proxy") ||
                ContainsNamedTransform(root.transform, "pilot_seat"))
            {
                throw new InvalidOperationException("Approved cockpit console contains a rejected preview/detail object.");
            }

            ValidateEditorReviewRemovedObjects(root.transform);
            ValidateEditorReviewTransformOverrides(root.transform);

            var bounds = GetRendererBounds(root.transform);
            if (bounds.size.x < 4.4f || bounds.size.x > 6.2f ||
                bounds.size.y < 1.8f || bounds.size.y > 3.5f ||
                bounds.size.z < 1.0f || bounds.size.z > 2.8f)
            {
                throw new InvalidOperationException("Approved cockpit console bounds are outside the approved sample scale. Size=" + FormatVector(bounds.size));
            }

            var enabledColliders = CountEnabledColliders(root.transform);
            if (enabledColliders != 0)
            {
                throw new InvalidOperationException("Approved cockpit console must not introduce gameplay colliders. EnabledColliders=" + enabledColliders);
            }

            CargoShipVisualModelingBootstrap.ValidateScene();
            ModelingInspectionModeBootstrap.ValidateScene();
            ModelingInspectionModeBootstrap.ValidateFreeCamera();
            Debug.Log(
                "Approved cockpit 02 console validation passed. Renderers=" +
                enabledRenderers +
                "; EnabledColliders=0; BoundsSize=" +
                FormatVector(bounds.size));
        }

        [MenuItem("Bellerophon/Validation/Capture Approved Cockpit 02 Console Comparison")]
        public static void CaptureUnityComparison()
        {
            ValidateScene();

            var projectRoot = Directory.GetParent(Application.dataPath);
            if (projectRoot == null)
            {
                throw new InvalidOperationException("Could not resolve project root for cockpit console comparison output.");
            }

            var outputRoot = Path.Combine(projectRoot.FullName, SampleRootRelativePath, ComparisonRootName);
            Directory.CreateDirectory(outputRoot);

            CaptureAppliedView(
                Path.Combine(outputRoot, "unity_01_front.png"),
                ConsoleWorldPosition + new Vector3(0f, 2.35f, 5.25f),
                ConsoleWorldPosition + new Vector3(0f, 1.28f, -0.1f),
                36f,
                false,
                5f,
                Vector3.up);
            CaptureAppliedView(
                Path.Combine(outputRoot, "unity_02_player.png"),
                ConsoleWorldPosition + new Vector3(0f, 1.58f, 3.2f),
                ConsoleWorldPosition + new Vector3(0f, 1.3f, -0.15f),
                32f,
                false,
                5f,
                Vector3.up);
            CaptureAppliedView(
                Path.Combine(outputRoot, "unity_03_side.png"),
                ConsoleWorldPosition + new Vector3(4.8f, 2.0f, 2.4f),
                ConsoleWorldPosition + new Vector3(0.2f, 1.02f, -0.15f),
                42f,
                false,
                5f,
                Vector3.up);
            CaptureAppliedView(
                Path.Combine(outputRoot, "unity_04_top.png"),
                ConsoleWorldPosition + new Vector3(0f, 7.2f, 0.2f),
                ConsoleWorldPosition + new Vector3(0f, 0f, 0.05f),
                45f,
                true,
                6.0f,
                Vector3.forward);
            CaptureAppliedView(
                Path.Combine(outputRoot, "unity_05_detail.png"),
                ConsoleWorldPosition + new Vector3(-1.8f, 2.25f, 2.7f),
                ConsoleWorldPosition + new Vector3(0.6f, 1.45f, -0.25f),
                48f,
                false,
                5f,
                Vector3.up);

            WriteComparisonIndex(outputRoot);
            AssetDatabase.Refresh();
            Debug.Log("Approved cockpit 02 console Unity comparison snapshots saved: " + outputRoot);
        }

        [MenuItem("Bellerophon/Validation/Capture Approved Cockpit 02 Console Current Objects")]
        public static void CaptureCurrentEditorObjects()
        {
            var currentRoot = RequireObject(RootName);
            CopyApprovedSourceFbx();

            var source = AssetDatabase.LoadAssetAtPath<GameObject>(UnityFbxPath);
            if (source == null)
            {
                throw new InvalidOperationException("Approved cockpit console source FBX failed to import: " + UnityFbxPath);
            }

            var expectedRoot = PrefabUtility.InstantiatePrefab(source) as GameObject;
            if (expectedRoot == null)
            {
                throw new InvalidOperationException("Approved cockpit console source FBX could not be instantiated for comparison: " + UnityFbxPath);
            }

            expectedRoot.name = RootName + " Expected Capture";
            expectedRoot.hideFlags = HideFlags.HideAndDontSave;
            expectedRoot.transform.position = ConsoleWorldPosition;
            expectedRoot.transform.rotation = ConsoleWorldRotation;
            expectedRoot.transform.localScale = ConsoleWorldScale;

            try
            {
                var rawExpectedTransforms = CollectTransformPaths(expectedRoot.transform);
                var rawExpectedRenderers = CollectRendererStates(expectedRoot.transform);
                var currentTransformsBeforeScriptFilter = CollectTransformPaths(currentRoot.transform);
                var currentRenderersBeforeScriptFilter = CollectRendererStates(currentRoot.transform);
                var rawMissingTransforms = GetMissingPaths(rawExpectedTransforms, currentTransformsBeforeScriptFilter);
                var rawMissingRenderers = GetMissingPaths(rawExpectedRenderers, currentRenderersBeforeScriptFilter);
                var rawTopMissingTransforms = GetTopMissingPaths(rawMissingTransforms);

                RemovePreviewOnlyObjects(expectedRoot.transform);
                RemoveEditorReviewObjects(expectedRoot.transform);
                ApplyEditorReviewTransformOverrides(expectedRoot.transform);

                var expectedTransforms = CollectTransformPaths(expectedRoot.transform);
                var currentTransforms = CollectTransformPaths(currentRoot.transform);
                var expectedRenderers = CollectRendererStates(expectedRoot.transform);
                var currentRenderers = CollectRendererStates(currentRoot.transform);
                var expectedTransformStates = CollectTransformStates(expectedRoot.transform);
                var currentTransformStates = CollectTransformStates(currentRoot.transform);

                var missingTransforms = GetMissingPaths(expectedTransforms, currentTransforms);
                var addedTransforms = GetMissingPaths(currentTransforms, expectedTransforms);
                var missingRenderers = GetMissingPaths(expectedRenderers, currentRenderers);
                var addedRenderers = GetMissingPaths(currentRenderers, expectedRenderers);
                var topMissingTransforms = GetTopMissingPaths(missingTransforms);
                var changedTransforms = GetTransformChanges(expectedTransformStates, currentTransformStates);

                var projectRoot = Directory.GetParent(Application.dataPath);
                if (projectRoot == null)
                {
                    throw new InvalidOperationException("Could not resolve project root for cockpit console current object capture.");
                }

                var outputRoot = Path.Combine(projectRoot.FullName, SampleRootRelativePath, "editor_current");
                Directory.CreateDirectory(outputRoot);

                var builder = new StringBuilder();
                builder.AppendLine("Approved Cockpit 02 Console current object capture");
                builder.AppendLine("Note=Compare current open scene console against script-generated expected console. This command does not reapply or save the scene.");
                builder.AppendLine("CurrentRoot=" + currentRoot.name);
                AppendMatchingRootSection(builder, FindNamedObjects(RootName));
                builder.AppendLine("RawSourceTransformCount=" + rawExpectedTransforms.Count);
                builder.AppendLine("RawSourceRendererCount=" + rawExpectedRenderers.Count);
                builder.AppendLine("RawMissingTransformCountBeforeScriptFilter=" + rawMissingTransforms.Count);
                builder.AppendLine("RawMissingRendererCountBeforeScriptFilter=" + rawMissingRenderers.Count);
                builder.AppendLine("ExpectedTransformCount=" + expectedTransforms.Count);
                builder.AppendLine("CurrentTransformCount=" + currentTransforms.Count);
                builder.AppendLine("ExpectedRendererCount=" + expectedRenderers.Count);
                builder.AppendLine("CurrentRendererCount=" + currentRenderers.Count);
                builder.AppendLine("MissingTransformCount=" + missingTransforms.Count);
                builder.AppendLine("AddedTransformCount=" + addedTransforms.Count);
                builder.AppendLine("MissingRendererCount=" + missingRenderers.Count);
                builder.AppendLine("AddedRendererCount=" + addedRenderers.Count);
                builder.AppendLine("ChangedTransformCount=" + changedTransforms.Count);
                AppendPathSection(builder, "RawTopMissingTransformsBeforeScriptFilter", rawTopMissingTransforms);
                AppendPathSection(builder, "RawMissingTransformsBeforeScriptFilter", rawMissingTransforms);
                AppendPathSection(builder, "RawMissingRenderersBeforeScriptFilter", rawMissingRenderers);
                AppendPathSection(builder, "TopMissingTransformsForScript", topMissingTransforms);
                AppendPathSection(builder, "MissingTransforms", missingTransforms);
                AppendPathSection(builder, "AddedTransforms", addedTransforms);
                AppendPathSection(builder, "MissingRenderers", missingRenderers);
                AppendPathSection(builder, "AddedRenderers", addedRenderers);
                AppendTransformChangeSection(builder, "ChangedTransforms", changedTransforms);
                AppendNamedTransformSection(builder, "central blank armored control plate", expectedTransformStates, currentTransformStates);

                File.WriteAllText(Path.Combine(outputRoot, "console_objects.txt"), builder.ToString(), new UTF8Encoding(false));
                AssetDatabase.Refresh();
                Debug.Log("Approved cockpit 02 console current object capture saved: " + outputRoot);
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
                throw new InvalidOperationException("Could not resolve project root for cockpit console source FBX.");
            }

            var sourcePath = Path.Combine(projectRoot.FullName, SourceFbxRelativePath);
            if (!File.Exists(sourcePath))
            {
                throw new FileNotFoundException("Missing approved cockpit console source FBX.", sourcePath);
            }

            var targetDirectory = Path.Combine(projectRoot.FullName, UnityAssetDirectory);
            Directory.CreateDirectory(targetDirectory);
            File.Copy(sourcePath, Path.Combine(projectRoot.FullName, UnityFbxPath), true);
            AssetDatabase.ImportAsset(UnityFbxPath, ImportAssetOptions.ForceUpdate);
        }

        private static ConsoleMaterials EnsureMaterials()
        {
            Directory.CreateDirectory(UnityAssetDirectory);
            return new ConsoleMaterials(
                EnsureMaterial(BodyMaterialPath, new Color(0.12f, 0.15f, 0.145f, 1f), 0.18f, 0.28f, false),
                EnsureMaterial(FrameMaterialPath, new Color(0.065f, 0.075f, 0.072f, 1f), 0.2f, 0.26f, false),
                EnsureMaterial(PanelMaterialPath, new Color(0.07f, 0.082f, 0.078f, 1f), 0.16f, 0.30f, false),
                EnsureMaterial(DarkMaterialPath, new Color(0.015f, 0.016f, 0.015f, 1f), 0f, 0.08f, false),
                EnsureMaterial(RubberMaterialPath, new Color(0.006f, 0.006f, 0.005f, 1f), 0f, 0.06f, false),
                EnsureMaterial(BrassMaterialPath, new Color(0.56f, 0.42f, 0.20f, 1f), 0.3f, 0.34f, false),
                EnsureMaterial(LabelMaterialPath, new Color(0.78f, 0.73f, 0.62f, 1f), 0f, 0.32f, false),
                EnsureMaterial(WearMaterialPath, new Color(0.66f, 0.64f, 0.57f, 1f), 0.35f, 0.38f, false),
                EnsureMaterial(AmberMaterialPath, new Color(0.95f, 0.62f, 0.16f, 1f), 0f, 0.42f, true),
                EnsureMaterial(RedMaterialPath, new Color(0.85f, 0.06f, 0.035f, 1f), 0f, 0.52f, true),
                EnsureMaterial(GreenMaterialPath, new Color(0.10f, 0.85f, 0.46f, 1f), 0f, 0.50f, true));
        }

        private static Material EnsureMaterial(string path, Color color, float metallic, float smoothness, bool emissive)
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
            material.SetOverrideTag("RenderType", "Opaque");
            material.renderQueue = -1;
            material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");

            if (emissive)
            {
                material.EnableKeyword("_EMISSION");
                var emission = color * 1.4f;
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

        private static void ApplyApprovedMaterials(Transform root, ConsoleMaterials materials)
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

        private static Material ResolveMaterial(string sourceName, string objectName, ConsoleMaterials materials)
        {
            if (sourceName.Contains("brass") ||
                objectName.Contains("helm") ||
                objectName.Contains("spoke") ||
                objectName.Contains("wheel") ||
                objectName.Contains("knob"))
            {
                return materials.Brass;
            }

            if (sourceName.Contains("rubber") ||
                objectName.Contains("rubber") ||
                objectName.Contains("grip") ||
                objectName.Contains("hand rest"))
            {
                return materials.Rubber;
            }

            if (sourceName.Contains("green") || objectName.Contains("indicator light"))
            {
                return materials.Green;
            }

            if (sourceName.Contains("red") || objectName.Contains("red") || objectName.Contains("stop block"))
            {
                return materials.Red;
            }

            if (sourceName.Contains("amber") ||
                objectName.Contains("caution") ||
                objectName.Contains("interaction") ||
                objectName.Contains("anchor"))
            {
                return materials.Amber;
            }

            if (sourceName.Contains("label") ||
                objectName.Contains("label") ||
                objectName.Contains("gauge face") ||
                objectName.Contains("travel notch") ||
                objectName.Contains("letter"))
            {
                return materials.Label;
            }

            if (sourceName.Contains("wear") ||
                objectName.Contains("wear") ||
                objectName.Contains("chip") ||
                objectName.Contains("bolt") ||
                objectName.Contains("rail"))
            {
                return materials.Wear;
            }

            if (sourceName.Contains("panel") ||
                objectName.Contains("panel") ||
                objectName.Contains("switch") ||
                objectName.Contains("gauge bezel"))
            {
                return materials.Panel;
            }

            if (sourceName.Contains("dark") ||
                objectName.Contains("dark") ||
                objectName.Contains("slot") ||
                objectName.Contains("shaft") ||
                objectName.Contains("post") ||
                objectName.Contains("pivot"))
            {
                return materials.Dark;
            }

            if (objectName.Contains("coaming") ||
                objectName.Contains("cheek") ||
                objectName.Contains("frame") ||
                objectName.Contains("mounting") ||
                objectName.Contains("bearing"))
            {
                return materials.Frame;
            }

            return materials.Body;
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
                    lowerName.Contains("cockpit floor footprint proxy") ||
                    lowerName.Contains("front broad window screen proxy") ||
                    lowerName.Contains("front lower sill proxy") ||
                    lowerName.Contains("front upper frame proxy") ||
                    lowerName.Contains("cockpit wall proxy") ||
                    lowerName.Contains("player clearance marker") ||
                    lowerName.Contains("console front anchor marker") ||
                    lowerName.Contains("pilot_seat") ||
                    target.GetComponent<Camera>() != null ||
                    target.GetComponent<Light>() != null)
                {
                    UnityEngine.Object.DestroyImmediate(target.gameObject);
                }
            }
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

        private static void ValidateEditorReviewRemovedObjects(Transform root)
        {
            for (var i = 0; i < RemovedAfterEditorReviewObjectPaths.Length; i++)
            {
                if (FindRelativeTransform(root, RemovedAfterEditorReviewObjectPaths[i]) != null)
                {
                    throw new InvalidOperationException("Approved cockpit console contains an object removed after editor review: " + RemovedAfterEditorReviewObjectPaths[i]);
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
                    throw new InvalidOperationException("Missing cockpit console transform override target: " + overrideValue.Path);
                }

                target.localPosition = overrideValue.LocalPosition;
                target.localRotation = overrideValue.LocalRotation;
                target.localScale = overrideValue.LocalScale;
            }
        }

        private static void ValidateEditorReviewTransformOverrides(Transform root)
        {
            for (var i = 0; i < EditorReviewTransformOverrides.Length; i++)
            {
                var overrideValue = EditorReviewTransformOverrides[i];
                var target = FindRelativeTransform(root, overrideValue.Path);
                if (target == null)
                {
                    throw new InvalidOperationException("Missing cockpit console transform override target: " + overrideValue.Path);
                }

                var current = new TransformState(target.localPosition, target.localRotation, target.localScale);
                var expected = new TransformState(overrideValue.LocalPosition, overrideValue.LocalRotation, overrideValue.LocalScale);
                if (!expected.IsCloseTo(current))
                {
                    throw new InvalidOperationException("Cockpit console transform override is not applied: " + overrideValue.Path);
                }
            }
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

        private static List<string> CollectRendererStates(Transform root)
        {
            var paths = new List<string>();
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                paths.Add(GetRelativePath(root, renderer.transform) + "|enabled=" + renderer.enabled + "|active=" + renderer.gameObject.activeInHierarchy);
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

        private static List<string> GetTopMissingPaths(List<string> missingPaths)
        {
            var missingSet = new HashSet<string>(missingPaths, StringComparer.Ordinal);
            var topPaths = new List<string>();
            for (var i = 0; i < missingPaths.Count; i++)
            {
                var path = missingPaths[i];
                var slash = path.IndexOf('/');
                var hasMissingParent = false;
                while (slash >= 0)
                {
                    if (missingSet.Contains(path.Substring(0, slash)))
                    {
                        hasMissingParent = true;
                        break;
                    }

                    slash = path.IndexOf('/', slash + 1);
                }

                if (!hasMissingParent)
                {
                    topPaths.Add(path);
                }
            }

            return topPaths;
        }

        private static void AppendPathSection(StringBuilder builder, string title, List<string> paths)
        {
            builder.AppendLine(title + "=" + paths.Count);
            for (var i = 0; i < paths.Count; i++)
            {
                builder.AppendLine("- " + paths[i]);
            }
        }

        private static void AppendTransformChangeSection(StringBuilder builder, string title, List<TransformChange> changes)
        {
            builder.AppendLine(title + "=" + changes.Count);
            for (var i = 0; i < changes.Count; i++)
            {
                var change = changes[i];
                builder.AppendLine("- " + change.Path);
                builder.AppendLine("  expectedLocalPosition=" + FormatVectorPrecise(change.Expected.LocalPosition));
                builder.AppendLine("  currentLocalPosition=" + FormatVectorPrecise(change.Current.LocalPosition));
                builder.AppendLine("  expectedLocalEuler=" + FormatVectorPrecise(change.Expected.LocalRotation.eulerAngles));
                builder.AppendLine("  currentLocalEuler=" + FormatVectorPrecise(change.Current.LocalRotation.eulerAngles));
                builder.AppendLine("  expectedLocalScale=" + FormatVectorPrecise(change.Expected.LocalScale));
                builder.AppendLine("  currentLocalScale=" + FormatVectorPrecise(change.Current.LocalScale));
            }
        }

        private static void AppendNamedTransformSection(
            StringBuilder builder,
            string needle,
            Dictionary<string, TransformState> expected,
            Dictionary<string, TransformState> current)
        {
            builder.AppendLine("NamedTransformSearch=" + needle);
            foreach (var pair in current)
            {
                if (pair.Key.IndexOf(needle, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                builder.AppendLine("- " + pair.Key);
                if (expected.TryGetValue(pair.Key, out var expectedState))
                {
                    builder.AppendLine("  expectedLocalPosition=" + FormatVectorPrecise(expectedState.LocalPosition));
                    builder.AppendLine("  expectedLocalEuler=" + FormatVectorPrecise(expectedState.LocalRotation.eulerAngles));
                    builder.AppendLine("  expectedLocalScale=" + FormatVectorPrecise(expectedState.LocalScale));
                }

                builder.AppendLine("  currentLocalPosition=" + FormatVectorPrecise(pair.Value.LocalPosition));
                builder.AppendLine("  currentLocalEuler=" + FormatVectorPrecise(pair.Value.LocalRotation.eulerAngles));
                builder.AppendLine("  currentLocalScale=" + FormatVectorPrecise(pair.Value.LocalScale));
            }
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

        private static List<GameObject> FindNamedObjects(string objectName)
        {
            var found = new List<GameObject>();
            var transforms = UnityEngine.Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (var i = 0; i < transforms.Length; i++)
            {
                if (transforms[i] != null && transforms[i].gameObject.name == objectName)
                {
                    found.Add(transforms[i].gameObject);
                }
            }

            return found;
        }

        private static void AppendMatchingRootSection(StringBuilder builder, List<GameObject> roots)
        {
            builder.AppendLine("MatchingRootCount=" + roots.Count);
            for (var i = 0; i < roots.Count; i++)
            {
                var root = roots[i];
                builder.AppendLine(
                    "- " +
                    GetScenePath(root.transform) +
                    "|active=" +
                    root.activeInHierarchy +
                    "|position=" +
                    FormatVector(root.transform.position) +
                    "|transforms=" +
                    root.GetComponentsInChildren<Transform>(true).Length +
                    "|renderers=" +
                    root.GetComponentsInChildren<Renderer>(true).Length);
            }
        }

        private static string GetScenePath(Transform transform)
        {
            var segments = new List<string>();
            var current = transform;
            while (current != null)
            {
                segments.Add(current.name);
                current = current.parent;
            }

            segments.Reverse();
            return string.Join("/", segments);
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
            var cameraObject = new GameObject("Approved Cockpit Console Comparison Camera")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            var keyLightObject = new GameObject("Approved Cockpit Console Comparison Key Light")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            var fillLightObject = new GameObject("Approved Cockpit Console Comparison Fill Light")
            {
                hideFlags = HideFlags.HideAndDontSave
            };

            try
            {
                keyLightObject.transform.position = ConsoleWorldPosition + new Vector3(0f, 4.8f, 2.6f);
                var keyLight = keyLightObject.AddComponent<Light>();
                keyLight.type = LightType.Rectangle;
                keyLight.color = new Color(1f, 0.95f, 0.86f, 1f);
                keyLight.intensity = 520f;
                keyLight.range = 12f;
                keyLight.areaSize = new Vector2(6.5f, 6.5f);

                fillLightObject.transform.position = ConsoleWorldPosition + new Vector3(-2.6f, 1.8f, -1.4f);
                var fillLight = fillLightObject.AddComponent<Light>();
                fillLight.type = LightType.Point;
                fillLight.color = new Color(0.34f, 0.78f, 0.62f, 1f);
                fillLight.intensity = 90f;
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
            builder.AppendLine("<head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width, initial-scale=1\"><title>ck_ctl02_low Unity 적용 비교</title>");
            builder.AppendLine("<style>body{margin:0;background:#111514;color:#ece5d8;font-family:Arial,sans-serif}main{max-width:1400px;margin:0 auto;padding:24px}h1{font-size:27px;margin:0 0 8px}.meta{color:#cfc6b8;margin:0 0 18px}.grid{display:grid;gap:18px}.pair{display:grid;grid-template-columns:1fr 1fr;gap:12px;border:1px solid #3c4643;background:#1c2220;border-radius:6px;padding:12px}.pair h2{grid-column:1/-1;font-size:18px;margin:0}.pair img{display:block;width:100%;height:auto;background:#050807}.label{font-size:13px;color:#ddd3c3;margin:6px 0 0}@media(max-width:900px){.pair{grid-template-columns:1fr}}</style>");
            builder.AppendLine("</head><body><main>");
            builder.AppendLine("<h1>ck_ctl02_low Unity 적용 비교</h1>");
            builder.AppendLine("<p class=\"meta\">왼쪽은 승인된 Blender artSample 렌더이고, 오른쪽은 CargoRunMvp 조종실 안에 배치한 Unity 캡처입니다. 중앙/전면 화면은 CK-01이 담당하며, CK-02는 타륜과 오른쪽 전진 레버를 가진 낮은 본체 조종대입니다.</p>");
            builder.AppendLine("<section class=\"grid\">");
            AddComparisonPair(builder, "01 정면", "../renders/01_front.png", "unity_01_front.png");
            AddComparisonPair(builder, "02 플레이어 시점", "../renders/02_player.png", "unity_02_player.png");
            AddComparisonPair(builder, "03 측면", "../renders/03_side.png", "unity_03_side.png");
            AddComparisonPair(builder, "04 상단", "../renders/04_top.png", "unity_04_top.png");
            AddComparisonPair(builder, "05 상세", "../renders/05_detail.png", "unity_05_detail.png");
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

        private static Bounds GetRendererBounds(Transform root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException("No renderers found under " + root.name);
            }

            var hasBounds = false;
            var bounds = new Bounds();
            for (var i = 0; i < renderers.Length; i++)
            {
                if (!renderers[i].enabled)
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
                throw new InvalidOperationException("No enabled renderers found under " + root.name);
            }

            return bounds;
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
            return value.x.ToString("0.######", CultureInfo.InvariantCulture) +
                "," +
                value.y.ToString("0.######", CultureInfo.InvariantCulture) +
                "," +
                value.z.ToString("0.######", CultureInfo.InvariantCulture);
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

        private readonly struct ConsoleMaterials
        {
            public ConsoleMaterials(
                Material body,
                Material frame,
                Material panel,
                Material dark,
                Material rubber,
                Material brass,
                Material label,
                Material wear,
                Material amber,
                Material red,
                Material green)
            {
                Body = body;
                Frame = frame;
                Panel = panel;
                Dark = dark;
                Rubber = rubber;
                Brass = brass;
                Label = label;
                Wear = wear;
                Amber = amber;
                Red = red;
                Green = green;
            }

            public Material Body { get; }
            public Material Frame { get; }
            public Material Panel { get; }
            public Material Dark { get; }
            public Material Rubber { get; }
            public Material Brass { get; }
            public Material Label { get; }
            public Material Wear { get; }
            public Material Amber { get; }
            public Material Red { get; }
            public Material Green { get; }
        }
    }
}
