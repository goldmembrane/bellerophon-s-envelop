using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.Validation
{
    public static class ApprovedCockpitStructureBootstrap
    {
        public const string RootName = "Approved Cockpit 01 Structure";

        private const string SampleRootRelativePath = "artSample/cockpit_01";
        private const string ComparisonRootName = "unity_applied_comparison";
        private const string SourceFbxRelativePath = "artSample/cockpit_01/exports/cockpit_01.fbx";
        private const string UnityAssetDirectory = "Assets/_Project/Art/Ship/Cockpit";
        private const string UnityFbxPath = UnityAssetDirectory + "/cockpit_01.fbx";
        private const string WallMaterialPath = UnityAssetDirectory + "/M_Cockpit01_Wall.mat";
        private const string FloorMaterialPath = UnityAssetDirectory + "/M_Cockpit01_Floor.mat";
        private const string FrameMaterialPath = UnityAssetDirectory + "/M_Cockpit01_Frame.mat";
        private const string EdgeMaterialPath = UnityAssetDirectory + "/M_Cockpit01_Edge.mat";

        private static readonly Vector3 CockpitCenter = new Vector3(0f, 0f, 18f);
        private static readonly Bounds ExpectedLocalBounds = new Bounds(
            new Vector3(0f, 1.52f, 0.16f),
            new Vector3(10.24f, 3.4f, 8.56f));

        [MenuItem("Bellerophon/Bootstrap/Ensure Approved Cockpit 01 Structure")]
        public static void EnsureApprovedCockpitStructure()
        {
            var scene = EditorSceneManager.OpenScene(Phase4CargoShipGrayboxBootstrap.CargoRunScenePath, OpenSceneMode.Single);
            DeleteGeneratedObject(RootName);

            CargoShipVisualModelingBootstrap.DisableVisualModeling();
            ModelingInspectionModeBootstrap.DisableTutorialLogicForModeling();
            scene = EditorSceneManager.OpenScene(Phase4CargoShipGrayboxBootstrap.CargoRunScenePath, OpenSceneMode.Single);

            CopyApprovedSourceFbx();
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(UnityFbxPath);
            if (source == null)
            {
                throw new InvalidOperationException("Approved cockpit source FBX failed to import: " + UnityFbxPath);
            }

            var root = PrefabUtility.InstantiatePrefab(source) as GameObject;
            if (root == null)
            {
                throw new InvalidOperationException("Approved cockpit source FBX could not be instantiated: " + UnityFbxPath);
            }

            root.name = RootName;
            root.transform.position = CockpitCenter;
            root.transform.rotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;

            var materials = EnsureMaterials();
            ApplyApprovedMaterials(root.transform, materials);
            DisableAllColliders(root.transform);
            ModelingInspectionModeBootstrap.ApplyFreeCameraForModeling();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, Phase4CargoShipGrayboxBootstrap.CargoRunScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            ValidateScene();
            Debug.Log("Approved cockpit 01 structure applied.");
        }

        [MenuItem("Bellerophon/Validation/Validate Approved Cockpit 01 Structure")]
        public static void ValidateScene()
        {
            EditorSceneManager.OpenScene(Phase4CargoShipGrayboxBootstrap.CargoRunScenePath, OpenSceneMode.Single);

            var root = RequireRootObject(RootName);
            if (!root.activeInHierarchy)
            {
                throw new InvalidOperationException(RootName + " must be active after user approval.");
            }

            var renderers = root.GetComponentsInChildren<Renderer>(true);
            var enabledRenderers = 0;
            for (var i = 0; i < renderers.Length; i++)
            {
                if (renderers[i].enabled)
                {
                    enabledRenderers++;
                }
            }

            if (enabledRenderers < 20)
            {
                throw new InvalidOperationException("Approved cockpit structure renderer count is too low: " + enabledRenderers);
            }

            var enabledColliders = CountEnabledColliders(root.transform);
            if (enabledColliders != 0)
            {
                throw new InvalidOperationException("Approved cockpit structure must not introduce gameplay colliders. EnabledColliders=" + enabledColliders);
            }

            var grayboxRoot = RequireRootObject(Phase4CargoShipGrayboxBootstrap.GrayboxRootName);
            var enabledGrayboxRenderers = CountEnabledRenderers(grayboxRoot.transform);
            if (enabledGrayboxRenderers != 0)
            {
                throw new InvalidOperationException("Legacy graybox renderers must stay disabled while the approved cockpit structure is active. EnabledGrayboxRenderers=" + enabledGrayboxRenderers);
            }

            var grayboxColliders = CountEnabledColliders(grayboxRoot.transform);
            if (grayboxColliders <= 0)
            {
                throw new InvalidOperationException("Gameplay graybox colliders must remain available after applying the approved cockpit structure.");
            }

            var rootPositionDelta = Vector3.Distance(root.transform.position, CockpitCenter);
            if (rootPositionDelta > 0.01f)
            {
                throw new InvalidOperationException("Approved cockpit root was not placed at the cockpit center. Delta=" + rootPositionDelta.ToString("0.000"));
            }

            ValidateBounds(root);
            ValidateExcludedDetails(root.transform);
            CargoShipVisualModelingBootstrap.ValidateScene();
            ModelingInspectionModeBootstrap.ValidateScene();
            ModelingInspectionModeBootstrap.ValidateFreeCamera();
            Debug.Log(
                "Approved cockpit 01 structure validation passed. Renderers=" +
                enabledRenderers +
                "; EnabledColliders=0; EnabledGrayboxRenderers=0; GrayboxColliders=" +
                grayboxColliders);
        }

        [MenuItem("Bellerophon/Validation/Capture Approved Cockpit 01 Unity Comparison")]
        public static void CaptureUnityComparison()
        {
            ValidateScene();

            var projectRoot = Directory.GetParent(Application.dataPath);
            if (projectRoot == null)
            {
                throw new InvalidOperationException("Could not resolve project root for cockpit comparison output.");
            }

            var outputRoot = Path.Combine(projectRoot.FullName, SampleRootRelativePath, ComparisonRootName);
            Directory.CreateDirectory(outputRoot);

            CaptureAppliedView(
                Path.Combine(outputRoot, "unity_01_top.png"),
                CockpitCenter + new Vector3(0f, 13.5f, -0.2f),
                CockpitCenter + new Vector3(0f, 0f, -0.2f),
                42f,
                true,
                5.6f,
                Vector3.forward);
            CaptureAppliedView(
                Path.Combine(outputRoot, "unity_02_front.png"),
                CockpitCenter + new Vector3(0f, 3.0f, 8.5f),
                CockpitCenter + new Vector3(0f, 1.1f, 0.8f),
                35f,
                false,
                5.0f,
                Vector3.up);
            CaptureAppliedView(
                Path.Combine(outputRoot, "unity_03_rear.png"),
                CockpitCenter + new Vector3(0f, 4.2f, -9.5f),
                CockpitCenter + new Vector3(0f, 1.1f, -1.6f),
                38f,
                false,
                5.0f,
                Vector3.up);
            CaptureAppliedView(
                Path.Combine(outputRoot, "unity_04_diag.png"),
                CockpitCenter + new Vector3(8.2f, 5.3f, -7.4f),
                CockpitCenter + new Vector3(0f, 1.2f, 0f),
                34f,
                false,
                5.0f,
                Vector3.up);

            WriteComparisonIndex(outputRoot);
            AssetDatabase.Refresh();
            Debug.Log("Approved cockpit 01 Unity comparison snapshots saved: " + outputRoot);
        }

        private static void CopyApprovedSourceFbx()
        {
            var projectRoot = Directory.GetParent(Application.dataPath);
            if (projectRoot == null)
            {
                throw new InvalidOperationException("Could not resolve project root for cockpit source FBX.");
            }

            var sourcePath = Path.Combine(projectRoot.FullName, SourceFbxRelativePath);
            if (!File.Exists(sourcePath))
            {
                throw new FileNotFoundException("Missing approved cockpit source FBX.", sourcePath);
            }

            var targetDirectory = Path.Combine(projectRoot.FullName, UnityAssetDirectory);
            Directory.CreateDirectory(targetDirectory);
            File.Copy(sourcePath, Path.Combine(projectRoot.FullName, UnityFbxPath), true);
            AssetDatabase.ImportAsset(UnityFbxPath, ImportAssetOptions.ForceUpdate);
        }

        private static CockpitMaterials EnsureMaterials()
        {
            Directory.CreateDirectory(UnityAssetDirectory);
            return new CockpitMaterials(
                EnsureMaterial(WallMaterialPath, new Color(0.32f, 0.34f, 0.32f, 1f), 0.82f),
                EnsureMaterial(FloorMaterialPath, new Color(0.12f, 0.13f, 0.12f, 1f), 0.86f),
                EnsureMaterial(FrameMaterialPath, new Color(0.20f, 0.22f, 0.21f, 1f), 0.82f),
                EnsureMaterial(EdgeMaterialPath, new Color(0.72f, 0.52f, 0.18f, 1f), 0.74f));
        }

        private static Material EnsureMaterial(string path, Color color, float smoothness)
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
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", Mathf.Clamp01(1f - smoothness));
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static void ApplyApprovedMaterials(Transform root, CockpitMaterials materials)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                var lowerName = renderer.gameObject.name.ToLowerInvariant();
                if (lowerName.Contains("floor") && lowerName.Contains("edge"))
                {
                    renderer.sharedMaterial = materials.Edge;
                }
                else if (lowerName.Contains("floor"))
                {
                    renderer.sharedMaterial = materials.Floor;
                }
                else if (lowerName.Contains("edge"))
                {
                    renderer.sharedMaterial = materials.Edge;
                }
                else if (lowerName.Contains("lintel") || lowerName.Contains("return") || lowerName.Contains("rim"))
                {
                    renderer.sharedMaterial = materials.Frame;
                }
                else
                {
                    renderer.sharedMaterial = materials.Wall;
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

        private static void ValidateBounds(GameObject root)
        {
            var bounds = GetRendererBounds(root.transform);
            var localCenter = bounds.center - root.transform.position;
            var expected = ExpectedLocalBounds;

            if (Mathf.Abs(bounds.size.x - expected.size.x) > 0.8f ||
                Mathf.Abs(bounds.size.y - expected.size.y) > 0.8f ||
                Mathf.Abs(bounds.size.z - expected.size.z) > 0.8f)
            {
                throw new InvalidOperationException(
                    "Approved cockpit structure bounds do not match the approved sample scale. Size=" +
                    FormatVector(bounds.size) +
                    "; ExpectedAround=" +
                    FormatVector(expected.size));
            }

            if (Mathf.Abs(localCenter.x - expected.center.x) > 0.65f ||
                Mathf.Abs(localCenter.y - expected.center.y) > 0.65f ||
                Mathf.Abs(localCenter.z - expected.center.z) > 0.65f)
            {
                throw new InvalidOperationException(
                    "Approved cockpit structure bounds center does not match the approved sample placement. LocalCenter=" +
                    FormatVector(localCenter) +
                    "; ExpectedAround=" +
                    FormatVector(expected.center));
            }
        }

        private static void ValidateExcludedDetails(Transform root)
        {
            var forbidden = new[] { "helm", "console", "screen", "glass", "corridor" };
            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                var lowerName = transforms[i].name.ToLowerInvariant();
                for (var j = 0; j < forbidden.Length; j++)
                {
                    if (lowerName.Contains(forbidden[j]))
                    {
                        throw new InvalidOperationException(
                            "Approved cockpit structure contains an excluded detail object: " +
                            transforms[i].name);
                    }
                }
            }
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

        private static void CaptureAppliedView(
            string path,
            Vector3 cameraPosition,
            Vector3 lookAt,
            float fieldOfView,
            bool orthographic,
            float orthographicSize,
            Vector3 cameraUp)
        {
            var cameraObject = new GameObject("Approved Cockpit Applied Comparison Camera")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            var keyLightObject = new GameObject("Approved Cockpit Applied Comparison Key Light")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            var fillLightObject = new GameObject("Approved Cockpit Applied Comparison Fill Light")
            {
                hideFlags = HideFlags.HideAndDontSave
            };

            try
            {
                keyLightObject.transform.position = CockpitCenter + new Vector3(0f, 6.0f, -2.5f);
                var keyLight = keyLightObject.AddComponent<Light>();
                keyLight.type = LightType.Rectangle;
                keyLight.color = new Color(1f, 0.95f, 0.86f, 1f);
                keyLight.intensity = 520f;
                keyLight.range = 12f;
                keyLight.areaSize = new Vector2(7.0f, 7.0f);

                fillLightObject.transform.position = CockpitCenter + new Vector3(0f, 2.6f, 2.8f);
                var fillLight = fillLightObject.AddComponent<Light>();
                fillLight.type = LightType.Point;
                fillLight.color = new Color(0.6f, 0.72f, 0.78f, 1f);
                fillLight.intensity = 95f;
                fillLight.range = 10f;

                var camera = cameraObject.AddComponent<Camera>();
                camera.transform.position = cameraPosition;
                camera.transform.LookAt(lookAt, cameraUp);
                camera.fieldOfView = fieldOfView;
                camera.orthographic = orthographic;
                camera.orthographicSize = orthographicSize;
                camera.nearClipPlane = 0.02f;
                camera.farClipPlane = 100f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.025f, 0.028f, 0.027f, 1f);
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
            builder.AppendLine("<head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width, initial-scale=1\"><title>cockpit_01 Unity 적용 비교</title>");
            builder.AppendLine("<style>body{margin:0;background:#151817;color:#e8e1d2;font-family:Arial,sans-serif}main{max-width:1400px;margin:0 auto;padding:24px}h1{font-size:27px;margin:0 0 8px}.meta{color:#c8c0af;margin:0 0 18px}.grid{display:grid;gap:18px}.pair{display:grid;grid-template-columns:1fr 1fr;gap:12px;border:1px solid #3e453f;background:#202521;border-radius:6px;padding:12px}.pair h2{grid-column:1/-1;font-size:18px;margin:0}.pair img{display:block;width:100%;height:auto;background:#0c0f0e}.label{font-size:13px;color:#d9cfba;margin:6px 0 0}@media(max-width:900px){.pair{grid-template-columns:1fr}}</style>");
            builder.AppendLine("</head><body><main>");
            builder.AppendLine("<h1>cockpit_01 Unity 적용 비교</h1>");
            builder.AppendLine("<p class=\"meta\">왼쪽은 승인된 Blender artSample 렌더이고, 오른쪽은 `CargoRunMvp`에 실제 배치한 조종실 전체 구조 캡처입니다. 복도, 조종대, 유리 세부 구조, 콘솔은 제외했습니다.</p>");
            builder.AppendLine("<section class=\"grid\">");
            AddComparisonPair(builder, "01 상단 구조", "../renders/01_top.png", "unity_01_top.png");
            AddComparisonPair(builder, "02 전면 개구부", "../renders/02_front.png", "unity_02_front.png");
            AddComparisonPair(builder, "03 후방 중심 줄기", "../renders/03_rear.png", "unity_03_rear.png");
            AddComparisonPair(builder, "04 대각 구조", "../renders/04_diag.png", "unity_04_diag.png");
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

        private static GameObject RequireRootObject(string objectName)
        {
            var roots = SceneManager.GetActiveScene().GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                if (roots[i].name == objectName)
                {
                    return roots[i];
                }
            }

            throw new InvalidOperationException("Missing root object: " + objectName);
        }

        private static void DeleteGeneratedObject(string objectName)
        {
            var existing = GameObject.Find(objectName);
            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing);
                return;
            }

            var roots = SceneManager.GetActiveScene().GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                if (roots[i].name == objectName)
                {
                    UnityEngine.Object.DestroyImmediate(roots[i]);
                    return;
                }
            }
        }

        private static int CountEnabledRenderers(Transform root)
        {
            var count = 0;
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            for (var i = 0; i < renderers.Length; i++)
            {
                if (renderers[i].enabled)
                {
                    count++;
                }
            }

            return count;
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

        private static string FormatVector(Vector3 value)
        {
            return value.x.ToString("0.00") + "," + value.y.ToString("0.00") + "," + value.z.ToString("0.00");
        }

        private readonly struct CockpitMaterials
        {
            public CockpitMaterials(Material wall, Material floor, Material frame, Material edge)
            {
                Wall = wall;
                Floor = floor;
                Frame = frame;
                Edge = edge;
            }

            public Material Wall { get; }
            public Material Floor { get; }
            public Material Frame { get; }
            public Material Edge { get; }
        }
    }
}
