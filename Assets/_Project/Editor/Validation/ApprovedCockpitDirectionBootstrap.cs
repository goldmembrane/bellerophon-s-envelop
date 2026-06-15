using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.Validation
{
    public static class ApprovedCockpitDirectionBootstrap
    {
        public const string RootName = "Approved Cockpit 11 Direction";

        private const string SampleRootRelativePath = "artSample/ck_dir11";
        private const string ComparisonRootName = "unity_applied_comparison";
        private const string SourceFbxRelativePath = "artSample/ck_dir11/exports/ck_dir11.fbx";
        private const string UnityAssetDirectory = "Assets/_Project/Art/Ship/Cockpit";
        private const string UnityFbxPath = UnityAssetDirectory + "/ck_dir11.fbx";
        private const string FrameMaterialPath = UnityAssetDirectory + "/M_CkDir11_Frame.mat";
        private const string MountMaterialPath = UnityAssetDirectory + "/M_CkDir11_Mount.mat";
        private const string EngineMaterialPath = UnityAssetDirectory + "/M_CkDir11_Engine.mat";
        private const string ControlMaterialPath = UnityAssetDirectory + "/M_CkDir11_Control.mat";
        private const string CargoMaterialPath = UnityAssetDirectory + "/M_CkDir11_Cargo.mat";
        private const string CargoDimMaterialPath = UnityAssetDirectory + "/M_CkDir11_CargoDim.mat";
        private const string TextMaterialPath = UnityAssetDirectory + "/M_CkDir11_Text.mat";
        private const string WearMaterialPath = UnityAssetDirectory + "/M_CkDir11_Wear.mat";

        private static readonly Vector3 CockpitCenter = new Vector3(0f, 0f, 18f);
        private static readonly Vector3 RootWorldPosition = CockpitCenter;
        private static readonly Vector3 LeftEngineTargetCenter = CockpitCenter + new Vector3(-3.334f, 1.806f, 0.90325f);
        private static readonly Vector3 RightControlTargetCenter = CockpitCenter + new Vector3(3.236f, 1.78f, 0.90275f);
        private static readonly Vector3 RearCargoTargetCenter = CockpitCenter + new Vector3(1.90938f, 1.72f, 3.4449f);
        private static readonly Vector3 CeilingRouteTargetCenter = CockpitCenter + new Vector3(0f, 2.74f, -0.15f);
        private static readonly Vector3 RearFloorArrowTargetCenter = CockpitCenter + new Vector3(0f, 0.08f, 1.95f);
        private static readonly Vector3 LeftWallStripeTargetCenter = CockpitCenter + new Vector3(-4.95f, 1.34f, -0.15f);
        private static readonly Vector3 RightWallStripeTargetCenter = CockpitCenter + new Vector3(4.95f, 1.34f, -0.15f);
        private static readonly Vector3 LeftBackerCenter = CockpitCenter + new Vector3(-3.342f, 1.78f, 1.042f);
        private static readonly Vector3 RightBackerCenter = CockpitCenter + new Vector3(3.231f, 1.78f, 1.02f);
        private static readonly Vector3 RearBackerCenter = CockpitCenter + new Vector3(1.598f, 1.72f, 3.007f);
        private static readonly Quaternion LeftEngineTargetRotation = Quaternion.Euler(-89.98022f, 0f, 0f);
        private static readonly Quaternion RightControlTargetRotation = Quaternion.Euler(-89.98022f, 0f, 0f);
        private static readonly Quaternion RearCargoTargetRotation = Quaternion.Euler(-90f, -90f, 0f);

        [MenuItem("Bellerophon/Bootstrap/Ensure Approved Cockpit 11 Direction")]
        public static void EnsureApprovedCockpitDirection()
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

            DeleteGeneratedObject(RootName);
            CopyApprovedSourceFbx();

            var source = AssetDatabase.LoadAssetAtPath<GameObject>(UnityFbxPath);
            if (source == null)
            {
                throw new InvalidOperationException("Approved cockpit direction source FBX failed to import: " + UnityFbxPath);
            }

            var root = PrefabUtility.InstantiatePrefab(source) as GameObject;
            if (root == null)
            {
                throw new InvalidOperationException("Approved cockpit direction source FBX could not be instantiated: " + UnityFbxPath);
            }

            root.name = RootName;
            root.transform.position = RootWorldPosition;
            root.transform.rotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;

            RemovePreviewOnlyObjects(root.transform);
            RemoveEditorExcludedObjects(root.transform);
            PositionApprovedGroups(root.transform);

            var materials = EnsureMaterials();
            ApplyApprovedMaterials(root.transform, materials);
            CreateWallMountBackers(root.transform, materials);
            DisableAllColliders(root.transform);
            ModelingInspectionModeBootstrap.ApplyFreeCameraForModeling();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, Phase4CargoShipGrayboxBootstrap.CargoRunScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            ValidateScene();
            Debug.Log("Approved cockpit 11 direction applied.");
        }

        [MenuItem("Bellerophon/Validation/Validate Approved Cockpit 11 Direction")]
        public static void ValidateScene()
        {
            EditorSceneManager.OpenScene(Phase4CargoShipGrayboxBootstrap.CargoRunScenePath, OpenSceneMode.Single);

            ApprovedCockpitStructureBootstrap.ValidateScene();
            ApprovedCockpitWindowBootstrap.ValidateScene();
            ApprovedCockpitConsoleBootstrap.ValidateScene();
            ApprovedCockpitWarningBootstrap.ValidateScene();

            var root = RequireObject(RootName);
            if (!root.activeInHierarchy)
            {
                throw new InvalidOperationException(RootName + " must be active after user approval.");
            }

            var renderers = root.GetComponentsInChildren<Renderer>(true);
            var enabledRenderers = 0;
            var hasEngine = false;
            var hasControl = false;
            var hasCargo = false;
            for (var i = 0; i < renderers.Length; i++)
            {
                if (!renderers[i].enabled)
                {
                    continue;
                }

                enabledRenderers++;
                var lowerName = renderers[i].gameObject.name.ToLowerInvariant();
                hasEngine |= IsEnginePart(lowerName);
                hasControl |= IsControlPart(lowerName);
                hasCargo |= IsCargoPart(lowerName);
            }

            if (enabledRenderers < 20)
            {
                throw new InvalidOperationException("Approved cockpit direction renderer count is too low: " + enabledRenderers);
            }

            if (!hasEngine || !hasControl || !hasCargo)
            {
                throw new InvalidOperationException("Approved cockpit direction labels must include ENGINE, CONTROL, and CARGO HOLD. HasEngine=" + hasEngine + "; HasControl=" + hasControl + "; HasCargo=" + hasCargo);
            }

            ValidateNoPreviewObjects(root.transform);
            ValidateGroupCenter(root.transform, IsEngineSignPart, LeftEngineTargetCenter, "left engine sign", 0.18f);
            ValidateGroupCenter(root.transform, IsControlSignPart, RightControlTargetCenter, "right control sign", 0.18f);
            ValidateGroupCenter(root.transform, IsCargoSignPart, RearCargoTargetCenter, "rear cargo sign", 0.18f);
            ValidateGroupCenter(root.transform, IsCeilingRoutePart, CeilingRouteTargetCenter, "ceiling route label", 0.18f);
            ValidateGroupCenter(root.transform, IsRearFloorArrowPart, RearFloorArrowTargetCenter, "rear floor arrow", 0.18f);
            ValidateWallMountBackers(root.transform);
            ValidateRearPassageClearance(root.transform);
            ValidateNoEditorExcludedObjects(root.transform);

            var enabledColliders = CountEnabledColliders(root.transform);
            if (enabledColliders != 0)
            {
                throw new InvalidOperationException("Approved cockpit direction labels must not introduce gameplay colliders. EnabledColliders=" + enabledColliders);
            }

            CargoShipVisualModelingBootstrap.ValidateScene();
            ModelingInspectionModeBootstrap.ValidateScene();
            ModelingInspectionModeBootstrap.ValidateFreeCamera();
            Debug.Log(
                "Approved cockpit 11 direction validation passed. Renderers=" +
                enabledRenderers +
                "; EnabledColliders=0; EngineCenter=" +
                FormatVector(LeftEngineTargetCenter) +
                "; ControlCenter=" +
                FormatVector(RightControlTargetCenter) +
                "; CargoCenter=" +
                FormatVector(RearCargoTargetCenter));
        }

        [MenuItem("Bellerophon/Validation/Capture Approved Cockpit 11 Direction Comparison")]
        public static void CaptureUnityComparison()
        {
            ValidateScene();

            var projectRoot = Directory.GetParent(Application.dataPath);
            if (projectRoot == null)
            {
                throw new InvalidOperationException("Could not resolve project root for cockpit direction comparison output.");
            }

            var outputRoot = Path.Combine(projectRoot.FullName, SampleRootRelativePath, ComparisonRootName);
            Directory.CreateDirectory(outputRoot);

            CaptureAppliedView(
                Path.Combine(outputRoot, "unity_01_front.png"),
                CockpitCenter + new Vector3(0f, 2.12f, 5.6f),
                CockpitCenter + new Vector3(0f, 1.62f, -0.8f),
                38f,
                false,
                5f,
                Vector3.up);
            CaptureAppliedView(
                Path.Combine(outputRoot, "unity_02_player.png"),
                CockpitCenter + new Vector3(0f, 1.72f, 3.2f),
                CockpitCenter + new Vector3(0f, 1.65f, -0.85f),
                34f,
                false,
                5f,
                Vector3.up);
            CaptureAppliedView(
                Path.Combine(outputRoot, "unity_03_rear.png"),
                CockpitCenter + new Vector3(0f, 1.7f, -0.6f),
                CockpitCenter + new Vector3(0f, 1.62f, 2.35f),
                38f,
                false,
                5f,
                Vector3.up);
            CaptureAppliedView(
                Path.Combine(outputRoot, "unity_04_top.png"),
                CockpitCenter + new Vector3(0f, 8.5f, 0.4f),
                CockpitCenter + new Vector3(0f, 0.9f, 0.2f),
                36f,
                true,
                5.4f,
                Vector3.forward);
            CaptureAppliedView(
                Path.Combine(outputRoot, "unity_05_detail.png"),
                CockpitCenter + new Vector3(3.3f, 2.0f, 1.1f),
                RightControlTargetCenter,
                48f,
                false,
                5f,
                Vector3.up);

            WriteComparisonIndex(outputRoot);
            AssetDatabase.Refresh();
            Debug.Log("Approved cockpit 11 direction Unity comparison snapshots saved: " + outputRoot);
        }

        [MenuItem("Bellerophon/Validation/Capture Approved Cockpit 11 Direction Current Objects")]
        public static void CaptureCurrentEditorObjects()
        {
            var activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid())
            {
                throw new InvalidOperationException("No active scene is open for cockpit direction current object capture.");
            }

            var normalizedActivePath = activeScene.path.Replace('\\', '/');
            var normalizedCargoPath = Phase4CargoShipGrayboxBootstrap.CargoRunScenePath.Replace('\\', '/');
            if (!string.Equals(normalizedActivePath, normalizedCargoPath, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Current active scene is not CargoRunMvp. ActiveScene=" + activeScene.path);
            }

            var root = RequireObject(RootName);
            var projectRoot = Directory.GetParent(Application.dataPath);
            if (projectRoot == null)
            {
                throw new InvalidOperationException("Could not resolve project root for cockpit direction current object capture.");
            }

            var outputRoot = Path.Combine(projectRoot.FullName, SampleRootRelativePath, "editor_current");
            Directory.CreateDirectory(outputRoot);

            var builder = new StringBuilder();
            builder.AppendLine("# CK-11 Current Editor Objects");
            builder.AppendLine();
            builder.AppendLine("Captured from the currently open CargoRunMvp scene without regenerating CK-11.");
            builder.AppendLine("Use the C# candidates below to reflect user-edited label placement in ApprovedCockpitDirectionBootstrap.");
            builder.AppendLine();
            AppendVectorCandidate(builder, "LeftEngineTargetCenter", GetGroupBounds(root.transform, IsEngineSignPart, "left engine sign").center);
            AppendVectorCandidate(builder, "RightControlTargetCenter", GetGroupBounds(root.transform, IsControlSignPart, "right control sign").center);
            AppendVectorCandidate(builder, "RearCargoTargetCenter", GetGroupBounds(root.transform, IsCargoSignPart, "rear cargo sign").center);
            AppendVectorCandidate(builder, "CeilingRouteTargetCenter", GetGroupBounds(root.transform, IsCeilingRoutePart, "ceiling route label").center);
            AppendVectorCandidate(builder, "RearFloorArrowTargetCenter", GetGroupBounds(root.transform, IsRearFloorArrowPart, "rear floor arrow").center);
            AppendOptionalGroupCandidate(builder, root.transform, "LeftWallStripeTargetCenter", IsLeftWallStripePart, "left wall stripe");
            AppendOptionalGroupCandidate(builder, root.transform, "RightWallStripeTargetCenter", IsRightWallStripePart, "right wall stripe");
            AppendBackerCandidate(builder, root.transform, "LeftBackerCenter", "left direction sign wall mounting plate");
            AppendBackerCandidate(builder, root.transform, "RightBackerCenter", "right direction sign wall mounting plate");
            AppendBackerCandidate(builder, root.transform, "RearBackerCenter", "rear direction sign wall mounting plate");
            builder.AppendLine();
            AppendEulerCandidate(builder, "EngineSignRotationEuler", GetRepresentativeEuler(root.transform, IsEngineSignPart, "black armored frame", "left engine sign"));
            AppendEulerCandidate(builder, "ControlSignRotationEuler", GetRepresentativeEuler(root.transform, IsControlSignPart, "black armored frame", "right control sign"));
            AppendEulerCandidate(builder, "CargoSignRotationEuler", GetRepresentativeEuler(root.transform, IsCargoSignPart, "black armored frame", "rear cargo sign"));
            AppendBackerScaleCandidate(builder, root.transform, "LeftBackerScale", "left direction sign wall mounting plate");
            AppendBackerScaleCandidate(builder, root.transform, "RightBackerScale", "right direction sign wall mounting plate");
            AppendBackerScaleCandidate(builder, root.transform, "RearBackerScale", "rear direction sign wall mounting plate");

            File.WriteAllText(Path.Combine(outputRoot, "ck11_current_objects.md"), builder.ToString(), new UTF8Encoding(false));
            EditorSceneManager.SaveScene(activeScene);
            AssetDatabase.Refresh();
            Debug.Log("Approved cockpit 11 direction current object capture saved: " + outputRoot);
        }

        private static void CopyApprovedSourceFbx()
        {
            var projectRoot = Directory.GetParent(Application.dataPath);
            if (projectRoot == null)
            {
                throw new InvalidOperationException("Could not resolve project root for cockpit direction source FBX.");
            }

            var sourcePath = Path.Combine(projectRoot.FullName, SourceFbxRelativePath);
            if (!File.Exists(sourcePath))
            {
                throw new FileNotFoundException("Missing approved cockpit direction source FBX.", sourcePath);
            }

            var targetDirectory = Path.Combine(projectRoot.FullName, UnityAssetDirectory);
            Directory.CreateDirectory(targetDirectory);
            File.Copy(sourcePath, Path.Combine(projectRoot.FullName, UnityFbxPath), true);
            AssetDatabase.ImportAsset(UnityFbxPath, ImportAssetOptions.ForceUpdate);
        }

        private static DirectionMaterials EnsureMaterials()
        {
            Directory.CreateDirectory(UnityAssetDirectory);
            return new DirectionMaterials(
                EnsureMaterial(FrameMaterialPath, new Color(0.045f, 0.052f, 0.05f, 1f), 0.28f, 0.30f, false),
                EnsureMaterial(MountMaterialPath, new Color(0.06f, 0.068f, 0.064f, 1f), 0.30f, 0.34f, false),
                EnsureMaterial(EngineMaterialPath, new Color(0.16f, 0.80f, 0.52f, 1f), 0f, 0.42f, true),
                EnsureMaterial(ControlMaterialPath, new Color(0.95f, 0.55f, 0.18f, 1f), 0f, 0.46f, true),
                EnsureMaterial(CargoMaterialPath, new Color(0.22f, 0.56f, 0.95f, 1f), 0f, 0.44f, true),
                EnsureMaterial(CargoDimMaterialPath, new Color(0.08f, 0.28f, 0.52f, 1f), 0f, 0.22f, true),
                EnsureMaterial(TextMaterialPath, new Color(0.96f, 1.00f, 0.84f, 1f), 0f, 0.38f, true, 3.4f),
                EnsureMaterial(WearMaterialPath, new Color(0.64f, 0.62f, 0.54f, 1f), 0.35f, 0.42f, false));
        }

        private static Material EnsureMaterial(string path, Color color, float metallic, float smoothness, bool emissive, float emissionMultiplier = 1.35f)
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

        private static void ApplyApprovedMaterials(Transform root, DirectionMaterials materials)
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

        private static Material ResolveMaterial(string sourceName, string objectName, DirectionMaterials materials)
        {
            if (sourceName.Contains("text") || objectName.Contains("text"))
            {
                return materials.Text;
            }

            if (sourceName.Contains("worn") || objectName.Contains("bolt"))
            {
                return materials.Wear;
            }

            if (sourceName.Contains("mount") || objectName.Contains("rail"))
            {
                return materials.Mount;
            }

            if (sourceName.Contains("frame") || objectName.Contains("black armored frame"))
            {
                return materials.Frame;
            }

            if (sourceName.Contains("cargo") && sourceName.Contains("dim") || objectName.Contains("floor cargo arrow"))
            {
                return materials.CargoDim;
            }

            if (sourceName.Contains("engine") ||
                objectName.Contains("left wall") ||
                (objectName.Contains("engine") && objectName.Contains("luminous label face")))
            {
                return materials.Engine;
            }

            if (sourceName.Contains("control") ||
                objectName.Contains("right wall") ||
                (objectName.Contains("control") && objectName.Contains("luminous label face")))
            {
                return materials.Control;
            }

            if (sourceName.Contains("cargo") ||
                (objectName.Contains("cargo") && objectName.Contains("luminous label face")))
            {
                return materials.Cargo;
            }

            return materials.Frame;
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
                    lowerName.Contains("approved console") ||
                    lowerName.Contains("front screen") ||
                    target.GetComponent<Camera>() != null ||
                    target.GetComponent<Light>() != null)
                {
                    UnityEngine.Object.DestroyImmediate(target.gameObject);
                }
            }
        }

        private static void PositionApprovedGroups(Transform root)
        {
            TransformRendererGroup(root, IsEngineSignPart, LeftEngineTargetCenter, LeftEngineTargetRotation, "black armored frame", "left engine sign");
            TransformRendererGroup(root, IsControlSignPart, RightControlTargetCenter, RightControlTargetRotation, "black armored frame", "right control sign");
            TransformRendererGroup(root, IsCargoSignPart, RearCargoTargetCenter, RearCargoTargetRotation, "black armored frame", "rear cargo sign");
            MoveRendererGroup(root, IsCeilingRoutePart, CeilingRouteTargetCenter, "ceiling route label");
            MoveRendererGroup(root, IsRearFloorArrowPart, RearFloorArrowTargetCenter, "rear floor arrow");
        }

        private static void RemoveEditorExcludedObjects(Transform root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            for (var i = renderers.Length - 1; i >= 0; i--)
            {
                var renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                var lowerName = renderer.gameObject.name.ToLowerInvariant();
                if (IsLeftWallStripePart(lowerName) || IsRightWallStripePart(lowerName))
                {
                    UnityEngine.Object.DestroyImmediate(renderer.gameObject);
                }
            }
        }

        private static void CreateWallMountBackers(Transform root, DirectionMaterials materials)
        {
            CreateWallMountBacker(
                root,
                "left direction sign wall mounting plate",
                LeftBackerCenter,
                new Vector3(0.16f, 0.58f, 1.64f),
                materials.Mount);
            CreateWallMountBacker(
                root,
                "right direction sign wall mounting plate",
                RightBackerCenter,
                new Vector3(0.16f, 0.58f, 1.64f),
                materials.Mount);
            CreateWallMountBacker(
                root,
                "rear direction sign wall mounting plate",
                RearBackerCenter,
                new Vector3(0.16f, 0.58f, 1.86f),
                materials.Mount);
        }

        private static void CreateWallMountBacker(Transform root, string name, Vector3 position, Vector3 scale, Material material)
        {
            var plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plate.name = name;
            plate.transform.SetParent(root, true);
            plate.transform.position = position;
            plate.transform.rotation = Quaternion.identity;
            plate.transform.localScale = scale;

            var collider = plate.GetComponent<Collider>();
            if (collider != null)
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }

            var renderer = plate.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }
        }

        private static void ValidateWallMountBackers(Transform root)
        {
            ValidateNamedBacker(root, "left direction sign wall mounting plate", LeftBackerCenter);
            ValidateNamedBacker(root, "right direction sign wall mounting plate", RightBackerCenter);
            ValidateNamedBacker(root, "rear direction sign wall mounting plate", RearBackerCenter);
        }

        private static void ValidateNamedBacker(Transform root, string name, Vector3 expectedCenter)
        {
            var target = FindRelativeTransform(root, name);
            if (target == null)
            {
                throw new InvalidOperationException("Approved cockpit direction wall mount backer is missing: " + name);
            }

            var delta = Vector3.Distance(target.position, expectedCenter);
            if (delta > 0.04f)
            {
                throw new InvalidOperationException("Approved cockpit direction wall mount backer is not attached at the expected wall position: " + name + "; Delta=" + delta.ToString("0.000"));
            }
        }

        private static void ValidateRearPassageClearance(Transform root)
        {
            var rearSignBounds = GetGroupBounds(root, IsCargoSignPart, "rear cargo sign");
            var rearBacker = FindRelativeTransform(root, "rear direction sign wall mounting plate");
            if (rearBacker == null)
            {
                throw new InvalidOperationException("Approved cockpit direction rear wall mount backer is missing.");
            }

            var rearBackerRenderer = rearBacker.GetComponent<Renderer>();
            if (rearBackerRenderer == null)
            {
                throw new InvalidOperationException("Approved cockpit direction rear wall mount backer renderer is missing.");
            }

            const float minimumCentralPassageHalfWidth = 1.45f;
            if (rearSignBounds.min.x < minimumCentralPassageHalfWidth || rearBackerRenderer.bounds.min.x < minimumCentralPassageHalfWidth)
            {
                throw new InvalidOperationException(
                    "Approved cockpit direction rear cargo sign must stay on the side wall and keep the rear passage clear. SignMinX=" +
                    rearSignBounds.min.x.ToString("0.00") +
                    "; BackerMinX=" +
                    rearBackerRenderer.bounds.min.x.ToString("0.00"));
            }
        }

        private static void ValidateNoEditorExcludedObjects(Transform root)
        {
            if (ContainsRenderer(root, IsLeftWallStripePart) || ContainsRenderer(root, IsRightWallStripePart))
            {
                throw new InvalidOperationException("Approved cockpit direction contains side wall stripe objects removed by editor review.");
            }
        }

        private static void AppendVectorCandidate(StringBuilder builder, string name, Vector3 worldPosition)
        {
            builder.Append("private static readonly Vector3 ")
                .Append(name)
                .Append(" = CockpitCenter + ")
                .Append(FormatSourceVector(worldPosition - CockpitCenter))
                .AppendLine(";");
        }

        private static void AppendBackerCandidate(StringBuilder builder, Transform root, string name, string objectName)
        {
            var target = FindRelativeTransform(root, objectName);
            if (target == null)
            {
                builder.Append("// Missing backer: ").AppendLine(objectName);
                return;
            }

            AppendVectorCandidate(builder, name, target.position);
        }

        private static void AppendOptionalGroupCandidate(StringBuilder builder, Transform root, string name, Predicate<string> predicate, string groupName)
        {
            if (!TryGetGroupBounds(root, predicate, out var bounds))
            {
                builder.Append("// Missing optional group: ").AppendLine(groupName);
                return;
            }

            AppendVectorCandidate(builder, name, bounds.center);
        }

        private static void AppendEulerCandidate(StringBuilder builder, string name, Vector3 euler)
        {
            builder.Append("// ")
                .Append(name)
                .Append(" = ")
                .Append(FormatSourceVector(NormalizeEuler(euler)))
                .AppendLine(";");
        }

        private static void AppendBackerScaleCandidate(StringBuilder builder, Transform root, string name, string objectName)
        {
            var target = FindRelativeTransform(root, objectName);
            if (target == null)
            {
                builder.Append("// Missing backer scale: ").AppendLine(objectName);
                return;
            }

            builder.Append("// ")
                .Append(name)
                .Append(" = ")
                .Append(FormatSourceVector(target.localScale))
                .AppendLine(";");
        }

        private static Vector3 GetRepresentativeEuler(Transform root, Predicate<string> predicate, string preferredNameNeedle, string groupName)
        {
            return FindRepresentativeTransform(root, predicate, preferredNameNeedle, groupName).rotation.eulerAngles;
        }

        private static Transform FindRepresentativeTransform(Transform root, Predicate<string> predicate, string preferredNameNeedle, string groupName)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            Transform fallback = null;
            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null || !predicate(renderer.gameObject.name.ToLowerInvariant()))
                {
                    continue;
                }

                fallback ??= renderer.transform;
                if (renderer.gameObject.name.IndexOf(preferredNameNeedle, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return renderer.transform;
                }
            }

            if (fallback != null)
            {
                return fallback;
            }

            throw new InvalidOperationException("Missing approved cockpit direction renderer group for rotation capture: " + groupName);
        }

        private static Vector3 NormalizeEuler(Vector3 euler)
        {
            return new Vector3(NormalizeEuler(euler.x), NormalizeEuler(euler.y), NormalizeEuler(euler.z));
        }

        private static float NormalizeEuler(float value)
        {
            while (value > 180f)
            {
                value -= 360f;
            }

            while (value < -180f)
            {
                value += 360f;
            }

            return value;
        }

        private static string FormatSourceVector(Vector3 value)
        {
            return "new Vector3(" +
                FormatSourceFloat(value.x) +
                "f, " +
                FormatSourceFloat(value.y) +
                "f, " +
                FormatSourceFloat(value.z) +
                "f)";
        }

        private static string FormatSourceFloat(float value)
        {
            return value.ToString("0.#####", CultureInfo.InvariantCulture);
        }

        private static void TransformRendererGroup(Transform root, Predicate<string> predicate, Vector3 targetCenter, Quaternion targetRepresentativeRotation, string representativeNameNeedle, string groupName)
        {
            var bounds = GetGroupBounds(root, predicate, groupName);
            var pivot = bounds.center;
            var representative = FindRepresentativeTransform(root, predicate, representativeNameNeedle, groupName);
            var rotation = targetRepresentativeRotation * Quaternion.Inverse(representative.rotation);
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null || !predicate(renderer.gameObject.name.ToLowerInvariant()))
                {
                    continue;
                }

                var transform = renderer.transform;
                transform.position = targetCenter + rotation * (transform.position - pivot);
                transform.rotation = rotation * transform.rotation;
            }
        }

        private static void MoveRendererGroup(Transform root, Predicate<string> predicate, Vector3 targetCenter, string groupName)
        {
            var bounds = GetGroupBounds(root, predicate, groupName);
            var delta = targetCenter - bounds.center;
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null || !predicate(renderer.gameObject.name.ToLowerInvariant()))
                {
                    continue;
                }

                renderer.transform.position += delta;
            }
        }

        private static Bounds GetGroupBounds(Transform root, Predicate<string> predicate, string groupName)
        {
            if (TryGetGroupBounds(root, predicate, out var bounds))
            {
                return bounds;
            }

            throw new InvalidOperationException("Missing approved cockpit direction renderer group: " + groupName);
        }

        private static bool TryGetGroupBounds(Transform root, Predicate<string> predicate, out Bounds bounds)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            var hasBounds = false;
            bounds = new Bounds();
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

            return hasBounds;
        }

        private static void ValidateGroupCenter(Transform root, Predicate<string> predicate, Vector3 expectedCenter, string groupName, float tolerance)
        {
            var bounds = GetGroupBounds(root, predicate, groupName);
            var delta = Vector3.Distance(bounds.center, expectedCenter);
            if (delta > tolerance)
            {
                throw new InvalidOperationException("Approved cockpit direction " + groupName + " is not in the approved location. Delta=" + delta.ToString("0.000") + "; Expected=" + FormatVector(expectedCenter) + "; Current=" + FormatVector(bounds.center));
            }
        }

        private static void ValidateNoPreviewObjects(Transform root)
        {
            if (ContainsNamedTransform(root, "context") ||
                ContainsNamedTransform(root, "proxy") ||
                ContainsNamedTransform(root, "approved console") ||
                ContainsNamedTransform(root, "camera") ||
                ContainsNamedTransform(root, "softbox") ||
                ContainsNamedTransform(root, "spill"))
            {
                throw new InvalidOperationException("Approved cockpit direction contains preview-only sample objects.");
            }
        }

        private static bool IsEnginePart(string lowerName)
        {
            return lowerName.Contains("engine");
        }

        private static bool IsControlPart(string lowerName)
        {
            return lowerName.Contains("control");
        }

        private static bool IsCargoPart(string lowerName)
        {
            return lowerName.Contains("cargo");
        }

        private static bool IsEngineSignPart(string lowerName)
        {
            return lowerName.Contains("left engine room direction sign");
        }

        private static bool IsControlSignPart(string lowerName)
        {
            return lowerName.Contains("right control room direction sign");
        }

        private static bool IsCargoSignPart(string lowerName)
        {
            return lowerName.Contains("rear cargo hold direction sign");
        }

        private static bool IsCeilingRoutePart(string lowerName)
        {
            return lowerName.Contains("ceiling route");
        }

        private static bool IsRearFloorArrowPart(string lowerName)
        {
            return lowerName.Contains("rear floor cargo arrow");
        }

        private static bool IsLeftWallStripePart(string lowerName)
        {
            return lowerName.Contains("left wall") && lowerName.Contains("engine");
        }

        private static bool IsRightWallStripePart(string lowerName)
        {
            return lowerName.Contains("right wall") && lowerName.Contains("control");
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
            var cameraObject = new GameObject("Approved Cockpit Direction Comparison Camera")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            var keyLightObject = new GameObject("Approved Cockpit Direction Comparison Key Light")
            {
                hideFlags = HideFlags.HideAndDontSave
            };

            try
            {
                keyLightObject.transform.position = CockpitCenter + new Vector3(0f, 4.8f, 1.8f);
                var keyLight = keyLightObject.AddComponent<Light>();
                keyLight.type = LightType.Rectangle;
                keyLight.color = new Color(0.92f, 0.96f, 0.90f, 1f);
                keyLight.intensity = 420f;
                keyLight.range = 12f;
                keyLight.areaSize = new Vector2(6.5f, 5.0f);

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
            builder.AppendLine("<head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width, initial-scale=1\"><title>ck_dir11 Unity 적용 비교</title>");
            builder.AppendLine("<style>body{margin:0;background:#111514;color:#ece5d8;font-family:Arial,sans-serif}main{max-width:1400px;margin:0 auto;padding:24px}h1{font-size:27px;margin:0 0 8px}.meta{color:#cfc6b8;margin:0 0 18px}.grid{display:grid;gap:18px}.pair{display:grid;grid-template-columns:1fr 1fr;gap:12px;border:1px solid #3c4643;background:#1c2220;border-radius:6px;padding:12px}.pair h2{grid-column:1/-1;font-size:18px;margin:0}.pair img{display:block;width:100%;height:auto;background:#050807}.label{font-size:13px;color:#ddd3c3;margin:6px 0 0}@media(max-width:900px){.pair{grid-template-columns:1fr}}</style>");
            builder.AppendLine("</head><body><main>");
            builder.AppendLine("<h1>ck_dir11 Unity 적용 비교</h1>");
            builder.AppendLine("<p class=\"meta\">왼쪽은 승인된 Blender artSample 렌더이고, 오른쪽은 CargoRunMvp 조종실에 배치한 Unity 캡처입니다. 좌측 ENGINE과 우측 CONTROL은 좌우 벽면에 붙였고, 후방 CARGO HOLD는 통로를 막지 않도록 옆 벽면에 붙였습니다.</p>");
            builder.AppendLine("<section class=\"grid\">");
            AddComparisonPair(builder, "01 정면", "../renders/01_front.png", "unity_01_front.png");
            AddComparisonPair(builder, "02 플레이어 시점", "../renders/02_player.png", "unity_02_player.png");
            AddComparisonPair(builder, "03 후방", "../renders/03_rear.png", "unity_03_rear.png");
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

        private static bool ContainsRenderer(Transform root, Predicate<string> predicate)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            for (var i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null && predicate(renderers[i].gameObject.name.ToLowerInvariant()))
                {
                    return true;
                }
            }

            return false;
        }

        private static Transform FindRelativeTransform(Transform root, string name)
        {
            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                if (string.Equals(transforms[i].name, name, StringComparison.OrdinalIgnoreCase))
                {
                    return transforms[i];
                }
            }

            return null;
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

        private readonly struct DirectionMaterials
        {
            public DirectionMaterials(
                Material frame,
                Material mount,
                Material engine,
                Material control,
                Material cargo,
                Material cargoDim,
                Material text,
                Material wear)
            {
                Frame = frame;
                Mount = mount;
                Engine = engine;
                Control = control;
                Cargo = cargo;
                CargoDim = cargoDim;
                Text = text;
                Wear = wear;
            }

            public Material Frame { get; }
            public Material Mount { get; }
            public Material Engine { get; }
            public Material Control { get; }
            public Material Cargo { get; }
            public Material CargoDim { get; }
            public Material Text { get; }
            public Material Wear { get; }
        }
    }
}
