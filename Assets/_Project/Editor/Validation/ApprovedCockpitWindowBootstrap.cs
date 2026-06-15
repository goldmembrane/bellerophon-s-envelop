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
    public static class ApprovedCockpitWindowBootstrap
    {
        public const string RootName = "Approved Cockpit 01 Window";

        private const string SampleRootRelativePath = "artSample/ck_win01";
        private const string ComparisonRootName = "unity_applied_comparison";
        private const string SourceFbxRelativePath = "artSample/ck_win01/exports/ck_win01.fbx";
        private const string UnityAssetDirectory = "Assets/_Project/Art/Ship/Cockpit";
        private const string UnityFbxPath = UnityAssetDirectory + "/ck_win01.fbx";
        private const string ScreenTexturePath = "Assets/_Project/Art/Props/Stage3Rework/Textures/HD_Stage3_GreenCrtScreen_Albedo.png";
        private const string FrameMaterialPath = UnityAssetDirectory + "/M_CkWin01_Frame.mat";
        private const string TrimMaterialPath = UnityAssetDirectory + "/M_CkWin01_Trim.mat";
        private const string RubberMaterialPath = UnityAssetDirectory + "/M_CkWin01_Rubber.mat";
        private const string GlassMaterialPath = UnityAssetDirectory + "/M_CkWin01_Glass.mat";
        private const string ScreenMaterialPath = UnityAssetDirectory + "/M_CkWin01_Screen.mat";
        private const string LightMaterialPath = UnityAssetDirectory + "/M_CkWin01_Light.mat";
        private const string WarningMaterialPath = UnityAssetDirectory + "/M_CkWin01_Warning.mat";

        private static readonly Vector3 CockpitCenter = new Vector3(0f, 0f, 18f);
        private static readonly Vector3 WindowWorldPosition = CockpitCenter + new Vector3(0f, 0f, -4f);
        private static readonly Quaternion WindowWorldRotation = new Quaternion(0f, 1f, 0f, 0f);
        private static readonly Vector3 WindowWorldScale = Vector3.one;

        [MenuItem("Bellerophon/Bootstrap/Ensure Approved Cockpit 01 Window")]
        public static void EnsureApprovedCockpitWindow()
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

            DeleteGeneratedObject(RootName);
            CopyApprovedSourceFbx();

            var source = AssetDatabase.LoadAssetAtPath<GameObject>(UnityFbxPath);
            if (source == null)
            {
                throw new InvalidOperationException("Approved cockpit window source FBX failed to import: " + UnityFbxPath);
            }

            var root = PrefabUtility.InstantiatePrefab(source) as GameObject;
            if (root == null)
            {
                throw new InvalidOperationException("Approved cockpit window source FBX could not be instantiated: " + UnityFbxPath);
            }

            root.name = RootName;
            root.transform.position = WindowWorldPosition;
            root.transform.rotation = WindowWorldRotation;
            root.transform.localScale = WindowWorldScale;

            var materials = EnsureMaterials();
            RemovePreviewOnlyObjects(root.transform);
            ApplyApprovedMaterials(root.transform, materials);
            ApplyCapturedEditorTransformOverrides(root.transform);
            DisableAllColliders(root.transform);
            ModelingInspectionModeBootstrap.ApplyFreeCameraForModeling();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, Phase4CargoShipGrayboxBootstrap.CargoRunScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            ValidateScene();
            Debug.Log("Approved cockpit 01 window applied.");
        }

        [MenuItem("Bellerophon/Validation/Validate Approved Cockpit 01 Window")]
        public static void ValidateScene()
        {
            EditorSceneManager.OpenScene(Phase4CargoShipGrayboxBootstrap.CargoRunScenePath, OpenSceneMode.Single);

            ApprovedCockpitStructureBootstrap.ValidateScene();
            var root = RequireObject(RootName);
            if (!root.activeInHierarchy)
            {
                throw new InvalidOperationException(RootName + " must be active after user approval.");
            }

            var positionDelta = Vector3.Distance(root.transform.position, WindowWorldPosition);
            if (positionDelta > 0.025f)
            {
                throw new InvalidOperationException("Approved cockpit window is not placed at the wide front aperture. Delta=" + positionDelta.ToString("0.000"));
            }

            var rotationDelta = Quaternion.Angle(root.transform.rotation, WindowWorldRotation);
            if (rotationDelta > 0.25f)
            {
                throw new InvalidOperationException("Approved cockpit window is not facing the cockpit interior. RotationDelta=" + rotationDelta.ToString("0.000"));
            }

            var renderers = root.GetComponentsInChildren<Renderer>(true);
            var enabledRenderers = 0;
            var hasScreen = false;
            var hasGlass = false;
            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (!renderer.enabled)
                {
                    continue;
                }

                enabledRenderers++;
                var lowerName = renderer.gameObject.name.ToLowerInvariant();
                var materials = renderer.sharedMaterials;
                for (var j = 0; j < materials.Length; j++)
                {
                    var material = materials[j];
                    if (material == null)
                    {
                        continue;
                    }

                    var materialName = material.name.ToLowerInvariant();
                    hasScreen |= lowerName.Contains("screen") || materialName.Contains("screen");
                    hasGlass |= lowerName.Contains("glass") || materialName.Contains("glass");
                }
            }

            if (enabledRenderers < 20)
            {
                throw new InvalidOperationException("Approved cockpit window renderer count is too low: " + enabledRenderers);
            }

            if (!hasScreen || !hasGlass)
            {
                throw new InvalidOperationException("Approved cockpit window must contain both screen and glass materials. HasScreen=" + hasScreen + "; HasGlass=" + hasGlass);
            }

            var bounds = GetRendererBounds(root.transform);
            if (bounds.size.x < 8.5f || bounds.size.x > 10.8f ||
                bounds.size.y < 2.7f || bounds.size.y > 3.6f ||
                bounds.size.z < 0.2f || bounds.size.z > 1.25f)
            {
                throw new InvalidOperationException("Approved cockpit window bounds are outside the approved sample scale. Size=" + FormatVector(bounds.size));
            }

            var enabledColliders = CountEnabledColliders(root.transform);
            if (enabledColliders != 0)
            {
                throw new InvalidOperationException("Approved cockpit window must not introduce gameplay colliders. EnabledColliders=" + enabledColliders);
            }

            if (ContainsNamedTransform(root.transform, "mullion") ||
                ContainsNamedTransform(root.transform, "five") ||
                ContainsNamedTransform(root.transform, "window_big_no_side"))
            {
                throw new InvalidOperationException("Approved cockpit window contains the rejected 5-part window structure.");
            }

            CargoShipVisualModelingBootstrap.ValidateScene();
            ModelingInspectionModeBootstrap.ValidateScene();
            ModelingInspectionModeBootstrap.ValidateFreeCamera();
            Debug.Log(
                "Approved cockpit 01 window validation passed. Renderers=" +
                enabledRenderers +
                "; EnabledColliders=0; BoundsSize=" +
                FormatVector(bounds.size));
        }

        [MenuItem("Bellerophon/Validation/Capture Approved Cockpit 01 Window Comparison")]
        public static void CaptureUnityComparison()
        {
            ValidateScene();

            var projectRoot = Directory.GetParent(Application.dataPath);
            if (projectRoot == null)
            {
                throw new InvalidOperationException("Could not resolve project root for cockpit window comparison output.");
            }

            var outputRoot = Path.Combine(projectRoot.FullName, SampleRootRelativePath, ComparisonRootName);
            Directory.CreateDirectory(outputRoot);

            CaptureAppliedView(
                Path.Combine(outputRoot, "unity_01_front.png"),
                WindowWorldPosition + new Vector3(0f, 1.85f, 11f),
                WindowWorldPosition + new Vector3(0f, 1.6f, 0f),
                34f,
                false,
                5f,
                Vector3.up);
            CaptureAppliedView(
                Path.Combine(outputRoot, "unity_02_inside.png"),
                WindowWorldPosition + new Vector3(0f, 1.65f, 5.6f),
                WindowWorldPosition + new Vector3(0f, 1.62f, -0.05f),
                30f,
                false,
                5f,
                Vector3.up);
            CaptureAppliedView(
                Path.Combine(outputRoot, "unity_03_diag.png"),
                WindowWorldPosition + new Vector3(-6.8f, 3.55f, 5.3f),
                WindowWorldPosition + new Vector3(0f, 1.55f, 0.02f),
                38f,
                false,
                5f,
                Vector3.up);
            CaptureAppliedView(
                Path.Combine(outputRoot, "unity_04_top.png"),
                WindowWorldPosition + new Vector3(0f, 8.8f, -0.1f),
                WindowWorldPosition + new Vector3(0f, 0f, -0.1f),
                40f,
                true,
                7.2f,
                Vector3.forward);

            WriteComparisonIndex(outputRoot);
            AssetDatabase.Refresh();
            Debug.Log("Approved cockpit 01 window Unity comparison snapshots saved: " + outputRoot);
        }

        [MenuItem("Bellerophon/Validation/Capture Approved Cockpit 01 Window Current Transforms")]
        public static void CaptureCurrentEditorTransforms()
        {
            var root = FindNamedObject(RootName);
            if (root == null)
            {
                throw new InvalidOperationException("Cannot capture current cockpit window transforms because the scene object is missing: " + RootName);
            }

            var projectRoot = Directory.GetParent(Application.dataPath);
            if (projectRoot == null)
            {
                throw new InvalidOperationException("Could not resolve project root for cockpit window transform capture.");
            }

            var outputRoot = Path.Combine(projectRoot.FullName, SampleRootRelativePath, "editor_current");
            Directory.CreateDirectory(outputRoot);
            var outputPath = Path.Combine(outputRoot, "window_transforms.txt");

            var builder = new StringBuilder();
            builder.AppendLine("# Approved Cockpit 01 Window current editor transforms");
            builder.AppendLine("# Generated from the currently open Unity editor scene.");
            builder.AppendLine();
            AppendWorldTransform(builder, "Root", root.transform);

            var overlay = FindChildByName(root.transform, "approved internal crt screen texture overlay");
            if (overlay != null)
            {
                builder.AppendLine();
                AppendLocalTransform(builder, "ScreenOverlay", overlay);
            }

            var glass = FindChildByName(root.transform, "single panoramic glass pane");
            if (glass != null)
            {
                builder.AppendLine();
                AppendLocalTransform(builder, "GlassPane", glass);
            }

            var source = AssetDatabase.LoadAssetAtPath<GameObject>(UnityFbxPath);
            if (source != null)
            {
                builder.AppendLine();
                builder.AppendLine("# Prefab child transform overrides");
                AppendPrefabTransformOverrides(builder, source.transform, root.transform);
            }

            File.WriteAllText(outputPath, builder.ToString(), new UTF8Encoding(false));
            AssetDatabase.Refresh();
            Debug.Log("Approved cockpit 01 window current transform capture saved: " + outputPath);
        }

        private static void CopyApprovedSourceFbx()
        {
            var projectRoot = Directory.GetParent(Application.dataPath);
            if (projectRoot == null)
            {
                throw new InvalidOperationException("Could not resolve project root for cockpit window source FBX.");
            }

            var sourcePath = Path.Combine(projectRoot.FullName, SourceFbxRelativePath);
            if (!File.Exists(sourcePath))
            {
                throw new FileNotFoundException("Missing approved cockpit window source FBX.", sourcePath);
            }

            var targetDirectory = Path.Combine(projectRoot.FullName, UnityAssetDirectory);
            Directory.CreateDirectory(targetDirectory);
            File.Copy(sourcePath, Path.Combine(projectRoot.FullName, UnityFbxPath), true);
            AssetDatabase.ImportAsset(UnityFbxPath, ImportAssetOptions.ForceUpdate);
        }

        private static CockpitWindowMaterials EnsureMaterials()
        {
            Directory.CreateDirectory(UnityAssetDirectory);
            var screenTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(ScreenTexturePath);
            return new CockpitWindowMaterials(
                EnsureMaterial(FrameMaterialPath, new Color(0.07f, 0.085f, 0.08f, 1f), 0.48f, 0.54f, false, false, null),
                EnsureMaterial(TrimMaterialPath, new Color(0.54f, 0.52f, 0.46f, 1f), 0.62f, 0.46f, false, false, null),
                EnsureMaterial(RubberMaterialPath, new Color(0.012f, 0.012f, 0.011f, 1f), 0f, 0.94f, false, false, null),
                EnsureMaterial(GlassMaterialPath, new Color(0.12f, 0.38f, 0.44f, 0.22f), 0f, 0.12f, true, false, null),
                EnsureScreenMaterial(ScreenMaterialPath, screenTexture),
                EnsureMaterial(LightMaterialPath, new Color(0.64f, 0.92f, 1f, 1f), 0f, 0.2f, false, true, null),
                EnsureMaterial(WarningMaterialPath, new Color(0.86f, 0.66f, 0.18f, 1f), 0f, 0.72f, false, false, null));
        }

        private static Material EnsureScreenMaterial(string path, Texture2D mainTexture)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ??
                         Shader.Find("Unlit/Texture") ??
                         Shader.Find("Standard");
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            else if (shader != null && material.shader != shader)
            {
                material.shader = shader;
            }

            var color = Color.white;
            material.color = color;
            SetColor(material, "_BaseColor", color);
            SetColor(material, "_Color", color);
            SetTexture(material, "_BaseMap", mainTexture);
            SetTexture(material, "_MainTex", mainTexture);
            SetFloat(material, "_Surface", 0f);
            SetFloat(material, "_Blend", 0f);
            SetFloat(material, "_Cull", 0f);
            material.SetOverrideTag("RenderType", "Opaque");
            material.renderQueue = -1;
            material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_EMISSION");
            SetColor(material, "_EmissionColor", Color.black);
            SetTexture(material, "_EmissionMap", null);

            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material EnsureMaterial(
            string path,
            Color color,
            float metallic,
            float smoothness,
            bool transparent,
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
            SetTexture(material, "_BaseMap", mainTexture);
            SetTexture(material, "_MainTex", mainTexture);

            if (transparent)
            {
                SetFloat(material, "_Surface", 1f);
                SetFloat(material, "_Blend", 0f);
                SetFloat(material, "_AlphaClip", 0f);
                material.SetOverrideTag("RenderType", "Transparent");
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }
            else
            {
                SetFloat(material, "_Surface", 0f);
                material.SetOverrideTag("RenderType", "Opaque");
                material.renderQueue = -1;
                material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }

            if (emissive)
            {
                material.EnableKeyword("_EMISSION");
                var emissionColor = color * 1.6f;
                emissionColor.a = 1f;
                SetColor(material, "_EmissionColor", emissionColor);
                SetTexture(material, "_EmissionMap", mainTexture);
            }
            else
            {
                material.DisableKeyword("_EMISSION");
                SetColor(material, "_EmissionColor", Color.black);
                SetTexture(material, "_EmissionMap", null);
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static void ApplyApprovedMaterials(Transform root, CockpitWindowMaterials materials)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                var shared = renderer.sharedMaterials;
                var objectName = renderer.gameObject.name.ToLowerInvariant();
                if ((objectName.Contains("big_screen") || objectName.Contains("panoramic internal screen")) &&
                    shared.Length >= 2)
                {
                    shared[0] = materials.Frame;
                    shared[1] = materials.Screen;
                    renderer.sharedMaterials = shared;
                    continue;
                }

                for (var j = 0; j < shared.Length; j++)
                {
                    var sourceName = shared[j] != null ? shared[j].name.ToLowerInvariant() : string.Empty;
                    shared[j] = ResolveMaterial(sourceName, objectName, materials);
                }

                renderer.sharedMaterials = shared;
            }
        }

        private static Material ResolveMaterial(string sourceName, string objectName, CockpitWindowMaterials materials)
        {
            if (sourceName.Contains("frame"))
            {
                return materials.Frame;
            }

            if (sourceName.Contains("green crt") || sourceName.Contains("internal screen") || sourceName == "screen")
            {
                return materials.Screen;
            }

            if (sourceName.Contains("glass") || objectName.Contains("glass"))
            {
                return materials.Glass;
            }

            if (sourceName.Contains("light") || objectName.Contains("light"))
            {
                return materials.Light;
            }

            if (sourceName.Contains("warning") || objectName.Contains("amber") || objectName.Contains("tag"))
            {
                return materials.Warning;
            }

            if (sourceName.Contains("rubber") || objectName.Contains("gasket"))
            {
                return materials.Rubber;
            }

            if (sourceName.Contains("trim") || sourceName.Contains("rubbed") || objectName.Contains("clamp") || objectName.Contains("bolt") || objectName.Contains("patch"))
            {
                return materials.Trim;
            }

            return materials.Frame;
        }

        private static void ApplyCapturedEditorTransformOverrides(Transform root)
        {
            ApplyTransformOverride(
                root,
                "ck_win01 - cockpit front window sample/front glass and frame module/asset SMP panoramic internal screen/asset SMP panoramic internal screen mesh",
                new Vector3(-0.000466f, 0.00018f, -0.0012f),
                new Quaternion(0.707107f, 0f, 0f, 0.707107f),
                new Vector3(0.01f, 0.01f, 0.01f));
            ApplyTransformOverride(
                root,
                "ck_win01 - cockpit front window sample/front glass and frame module/bottom crash sill",
                new Vector3(0f, -0.0002f, 0.00223f),
                new Quaternion(0f, 0f, 0f, 1f),
                Vector3.one);
            ApplyTransformOverride(
                root,
                "ck_win01 - cockpit front window sample/front glass and frame module/left amber inspection tag",
                new Vector3(0.0442f, -0.00185f, 0.00589f),
                new Quaternion(0f, 0f, 0f, 1f),
                Vector3.one);
            ApplyTransformOverride(
                root,
                "ck_win01 - cockpit front window sample/front glass and frame module/outer lower clamp block +3.92",
                new Vector3(-0.0392f, -0.00069f, 0.00457f),
                new Quaternion(0f, 0f, 0f, 1f),
                Vector3.one);
            ApplyTransformOverride(
                root,
                "ck_win01 - cockpit front window sample/front glass and frame module/outer lower clamp block -3.92",
                new Vector3(0.0392f, -0.00094f, 0.00444f),
                new Quaternion(0f, 0f, 0f, 1f),
                Vector3.one);
            ApplyTransformOverride(
                root,
                "ck_win01 - cockpit front window sample/front glass and frame module/right amber inspection tag",
                new Vector3(-0.0442f, -0.001162f, 0.006106f),
                new Quaternion(0f, 0f, 0f, 1f),
                Vector3.one);
        }

        private static void ApplyTransformOverride(
            Transform root,
            string relativePath,
            Vector3 localPosition,
            Quaternion localRotation,
            Vector3 localScale)
        {
            var target = FindChildByPath(root, relativePath);
            if (target == null)
            {
                throw new InvalidOperationException("Missing captured cockpit window transform path: " + relativePath);
            }

            target.localPosition = localPosition;
            target.localRotation = localRotation;
            target.localScale = localScale;
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
                if (lowerName.Contains("context") || lowerName.Contains("footprint marker"))
                {
                    UnityEngine.Object.DestroyImmediate(target.gameObject);
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
            var cameraObject = new GameObject("Approved Cockpit Window Comparison Camera")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            var keyLightObject = new GameObject("Approved Cockpit Window Comparison Key Light")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            var fillLightObject = new GameObject("Approved Cockpit Window Comparison Fill Light")
            {
                hideFlags = HideFlags.HideAndDontSave
            };

            try
            {
                keyLightObject.transform.position = WindowWorldPosition + new Vector3(0f, 5.6f, -4f);
                var keyLight = keyLightObject.AddComponent<Light>();
                keyLight.type = LightType.Rectangle;
                keyLight.color = new Color(1f, 0.96f, 0.88f, 1f);
                keyLight.intensity = 540f;
                keyLight.range = 14f;
                keyLight.areaSize = new Vector2(7f, 7f);

                fillLightObject.transform.position = WindowWorldPosition + new Vector3(0f, 2.1f, -1.2f);
                var fillLight = fillLightObject.AddComponent<Light>();
                fillLight.type = LightType.Point;
                fillLight.color = new Color(0.42f, 0.8f, 0.7f, 1f);
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
            builder.AppendLine("<head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width, initial-scale=1\"><title>ck_win01 Unity 적용 비교</title>");
            builder.AppendLine("<style>body{margin:0;background:#111514;color:#ece5d8;font-family:Arial,sans-serif}main{max-width:1400px;margin:0 auto;padding:24px}h1{font-size:27px;margin:0 0 8px}.meta{color:#cfc6b8;margin:0 0 18px}.grid{display:grid;gap:18px}.pair{display:grid;grid-template-columns:1fr 1fr;gap:12px;border:1px solid #3c4643;background:#1c2220;border-radius:6px;padding:12px}.pair h2{grid-column:1/-1;font-size:18px;margin:0}.pair img{display:block;width:100%;height:auto;background:#050807}.label{font-size:13px;color:#ddd3c3;margin:6px 0 0}@media(max-width:900px){.pair{grid-template-columns:1fr}}</style>");
            builder.AppendLine("</head><body><main>");
            builder.AppendLine("<h1>ck_win01 Unity 적용 비교</h1>");
            builder.AppendLine("<p class=\"meta\">왼쪽은 승인된 Blender artSample 렌더이고, 오른쪽은 CargoRunMvp 조종실 전면 개구부에 배치한 Unity 캡처입니다. 조종대와 복도 연결은 제외했습니다.</p>");
            builder.AppendLine("<section class=\"grid\">");
            AddComparisonPair(builder, "01 정면", "../renders/01_front.png", "unity_01_front.png");
            AddComparisonPair(builder, "02 실내 시점", "../renders/02_inside.png", "unity_02_inside.png");
            AddComparisonPair(builder, "03 대각", "../renders/03_diag.png", "unity_03_diag.png");
            AddComparisonPair(builder, "04 상단", "../renders/04_top.png", "unity_04_top.png");
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

        private static void AppendWorldTransform(StringBuilder builder, string label, Transform transform)
        {
            builder.Append(label).Append(".position = ").AppendLine(FormatVectorCtor(transform.position));
            builder.Append(label).Append(".rotation = ").AppendLine(FormatQuaternionCtor(transform.rotation));
            builder.Append(label).Append(".euler = ").AppendLine(FormatVectorCtor(transform.rotation.eulerAngles));
            builder.Append(label).Append(".scale = ").AppendLine(FormatVectorCtor(transform.localScale));
        }

        private static void AppendLocalTransform(StringBuilder builder, string label, Transform transform)
        {
            builder.Append(label).Append(".path = ").AppendLine(GetRelativePath(transform.root, transform));
            builder.Append(label).Append(".localPosition = ").AppendLine(FormatVectorCtor(transform.localPosition));
            builder.Append(label).Append(".localRotation = ").AppendLine(FormatQuaternionCtor(transform.localRotation));
            builder.Append(label).Append(".localEuler = ").AppendLine(FormatVectorCtor(transform.localRotation.eulerAngles));
            builder.Append(label).Append(".localScale = ").AppendLine(FormatVectorCtor(transform.localScale));
        }

        private static void AppendPrefabTransformOverrides(StringBuilder builder, Transform sourceRoot, Transform currentRoot)
        {
            var sourceByPath = new Dictionary<string, Transform>(StringComparer.Ordinal);
            CollectTransformsByPath(sourceRoot, sourceRoot, sourceByPath);

            var transforms = currentRoot.GetComponentsInChildren<Transform>(true);
            var overrideCount = 0;
            for (var i = 0; i < transforms.Length; i++)
            {
                var current = transforms[i];
                if (current == currentRoot)
                {
                    continue;
                }

                var path = GetRelativePath(currentRoot, current);
                if (!sourceByPath.TryGetValue(path, out var source))
                {
                    if (current.name.Equals("approved internal crt screen texture overlay", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    builder.Append("ExtraChild[").Append(overrideCount).Append("].path = ").AppendLine(path);
                    AppendLocalTransform(builder, "ExtraChild[" + overrideCount + "]", current);
                    overrideCount++;
                    continue;
                }

                if (!HasTransformDifference(source, current))
                {
                    continue;
                }

                builder.Append("Override[").Append(overrideCount).Append("].path = ").AppendLine(path);
                AppendLocalTransform(builder, "Override[" + overrideCount + "]", current);
                overrideCount++;
            }

            if (overrideCount == 0)
            {
                builder.AppendLine("None");
            }
        }

        private static void CollectTransformsByPath(Transform root, Transform current, IDictionary<string, Transform> output)
        {
            var path = GetRelativePath(root, current);
            output[path] = current;
            for (var i = 0; i < current.childCount; i++)
            {
                CollectTransformsByPath(root, current.GetChild(i), output);
            }
        }

        private static bool HasTransformDifference(Transform source, Transform current)
        {
            return Vector3.Distance(source.localPosition, current.localPosition) > 0.0005f ||
                   Quaternion.Angle(source.localRotation, current.localRotation) > 0.05f ||
                   Vector3.Distance(source.localScale, current.localScale) > 0.0005f;
        }

        private static Bounds GetRendererBounds(Transform root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException("No renderers found under " + root.name);
            }

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
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

        private static Transform FindChildByName(Transform root, string childName)
        {
            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                if (transforms[i].name.Equals(childName, StringComparison.OrdinalIgnoreCase))
                {
                    return transforms[i];
                }
            }

            return null;
        }

        private static Transform FindChildByPath(Transform root, string relativePath)
        {
            var current = root;
            var parts = relativePath.Split('/');
            for (var i = 0; i < parts.Length; i++)
            {
                var found = false;
                for (var childIndex = 0; childIndex < current.childCount; childIndex++)
                {
                    var child = current.GetChild(childIndex);
                    if (!child.name.Equals(parts[i], StringComparison.Ordinal))
                    {
                        continue;
                    }

                    current = child;
                    found = true;
                    break;
                }

                if (!found)
                {
                    return null;
                }
            }

            return current;
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

        private readonly struct CockpitWindowMaterials
        {
            public CockpitWindowMaterials(
                Material frame,
                Material trim,
                Material rubber,
                Material glass,
                Material screen,
                Material light,
                Material warning)
            {
                Frame = frame;
                Trim = trim;
                Rubber = rubber;
                Glass = glass;
                Screen = screen;
                Light = light;
                Warning = warning;
            }

            public Material Frame { get; }

            public Material Trim { get; }

            public Material Rubber { get; }

            public Material Glass { get; }

            public Material Screen { get; }

            public Material Light { get; }

            public Material Warning { get; }
        }
    }
}
