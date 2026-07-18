using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.OstinatoApprovedMaterial
{
    // Runtime projection consumes only the three user-approved sample renders.
    internal static class OstinatoApprovedMaterialApplyAndReview
    {
        private const string CargoRunScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string PlacementRootName = "Approved Ostinato Enemy Placement";
        private const string ModelChildName = "Ostinato_Model";
        private const string ModelAssetPath = "Assets/_Project/Art/Enemies/Ostinato/Models/Ostinato.fbx";
        private const string ApprovedRoot = "Assets/_Project/Art/Enemies/Ostinato/ApprovedSample";
        private const string ApprovedTextureRoot = ApprovedRoot + "/Textures";
        private const string GeneratedTextureRoot = ApprovedRoot + "/Generated";
        private const string MaterialRoot = ApprovedRoot + "/Materials";
        private const string FrontReferencePath = ApprovedTextureRoot + "/ostinato_reference_front.png";
        private const string SideReferencePath = ApprovedTextureRoot + "/ostinato_reference_side.png";
        private const string BackReferencePath = ApprovedTextureRoot + "/ostinato_reference_back.png";
        private const string NormalTexturePath = ApprovedTextureRoot + "/ostinato_shell_tissue_normal.png";
        private const string ChitinTexturePath = ApprovedTextureRoot + "/ostinato_olive_rust_chitin_albedo.png";
        private const string BakedAlbedoPath = GeneratedTextureRoot + "/Ostinato_ApprovedReference_Albedo.png";
        private const string BakedMetallicSmoothnessPath =
            GeneratedTextureRoot + "/Ostinato_ApprovedReference_MetallicSmoothness.png";
        private const string ApprovedMaterialPath = MaterialRoot + "/Ostinato_ApprovedReference.mat";
        private const string ApprovedProjectionShaderName = "Bellerophon/Ostinato Approved Sample Projection";
        private const string ApprovedFrontProjectionPath =
            GeneratedTextureRoot + "/Ostinato_ApprovedSample_FrontProjection.png";
        private const string ApprovedSideProjectionPath =
            GeneratedTextureRoot + "/Ostinato_ApprovedSample_SideProjection.png";
        private const string ApprovedBackProjectionPath =
            GeneratedTextureRoot + "/Ostinato_ApprovedSample_BackProjection.png";
        private const string ValidationFolder = "docs/validation/ostinato_approved_material_2026-07-18";
        private const string ApprovedSampleFrontRender =
            "artSample/enemies/ostinato/renders/01_front_current_model_reference_material.png";
        private const string ApprovedSampleSideRender =
            "artSample/enemies/ostinato/renders/02_side_current_model_reference_material.png";
        private const string ApprovedSampleBackRender =
            "artSample/enemies/ostinato/renders/03_back_current_model_reference_material.png";
        private const string FinalComparisonFileName = "Ostinato_ApprovedMaterial_FinalComparison.png";
        private const int PlacementCount = 9;
        private const int BakeSize = 1024;
        private const int CaptureLayer = 31;

        [MenuItem("Bellerophon/Enemies/Ostinato/Inspect Approved Material Target")]
        public static void InspectApprovedOstinatoMaterialTarget()
        {
            var scene = RequireOpenCargoRunScene();
            var sceneWasDirty = scene.isDirty;
            var placementRoot = RequireRoot(scene, PlacementRootName).transform;
            if (placementRoot.childCount != PlacementCount)
            {
                throw new InvalidOperationException(
                    $"Approved Ostinato placement must contain exactly {PlacementCount} slots.");
            }

            var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ModelAssetPath) ??
                throw new InvalidOperationException("Ostinato model asset is missing.");
            var assetRenderers = modelAsset.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (assetRenderers.Length != 1 || assetRenderers[0].sharedMesh == null)
            {
                throw new InvalidOperationException("Ostinato must contain one skinned mesh renderer.");
            }

            var mesh = assetRenderers[0].sharedMesh;
            var uv = mesh.uv;
            if (uv == null || uv.Length != mesh.vertexCount)
            {
                throw new InvalidOperationException("Ostinato mesh does not contain one UV coordinate per vertex.");
            }

            var report = new StringBuilder();
            report.AppendLine("Scene=" + scene.path);
            report.AppendLine("PlacementRoot=" + PlacementRootName);
            report.AppendLine("PlacementCount=" + placementRoot.childCount);
            report.AppendLine("ModelAsset=" + ModelAssetPath);
            report.AppendLine("RendererCount=" + assetRenderers.Length);
            report.AppendLine("Mesh=" + mesh.name);
            report.AppendLine("VertexCount=" + mesh.vertexCount);
            report.AppendLine("SubMeshCount=" + mesh.subMeshCount);
            report.AppendLine("TriangleIndexCount=" + mesh.triangles.Length);
            report.AppendLine("UvCount=" + uv.Length);
            report.AppendLine("UvMin=" + FormatVector2(new Vector2(uv.Min(value => value.x), uv.Min(value => value.y))));
            report.AppendLine("UvMax=" + FormatVector2(new Vector2(uv.Max(value => value.x), uv.Max(value => value.y))));
            report.AppendLine("MeshBoundsCenter=" + FormatVector3(mesh.bounds.center));
            report.AppendLine("MeshBoundsSize=" + FormatVector3(mesh.bounds.size));
            report.AppendLine("BoneCount=" + assetRenderers[0].bones.Length);
            report.AppendLine("Bones=" + string.Join("|", assetRenderers[0].bones.Select(bone => bone != null ? bone.name : "None")));
            report.AppendLine(
                "AssetMaterials=" + string.Join(
                    "|",
                    assetRenderers[0].sharedMaterials.Select(
                        material => material != null
                            ? material.name + ":" + (material.shader != null ? material.shader.name : "NoShader")
                            : "None")));

            for (var index = 0; index < PlacementCount; index++)
            {
                var expectedName = $"Ostinato_{index + 1:00}_Static_Review";
                var slot = placementRoot.GetChild(index);
                if (slot.name != expectedName)
                {
                    throw new InvalidOperationException(
                        $"Expected slot {expectedName}, found {slot.name}.");
                }
                var model = slot.Find(ModelChildName) ??
                    throw new InvalidOperationException(slot.name + " is missing " + ModelChildName + ".");
                var renderer = model.GetComponentsInChildren<SkinnedMeshRenderer>(true).SingleOrDefault() ??
                    throw new InvalidOperationException(slot.name + " must contain one skinned renderer.");
                if (renderer.sharedMesh != mesh)
                {
                    throw new InvalidOperationException(slot.name + " does not use the approved Ostinato mesh.");
                }
                report.AppendLine(
                    $"Slot[{index}]={slot.name},Model={model.name}," +
                    $"Material={string.Join("|", renderer.sharedMaterials.Select(value => value != null ? value.name : "None"))}," +
                    $"Animator={(model.GetComponent<Animator>() != null)}," +
                    $"LocalPosition={FormatVector3(model.localPosition)}," +
                    $"LocalEuler={FormatVector3(model.localEulerAngles)}," +
                    $"LocalScale={FormatVector3(model.localScale)}");
            }

            report.AppendLine("SceneChanged=False");
            report.AppendLine("SelectionCleared=True");
            WriteReport("Ostinato_ApprovedMaterialTargetInspection.txt", report.ToString());
            Selection.activeObject = null;
            if (scene.isDirty != sceneWasDirty)
            {
                throw new InvalidOperationException("Ostinato material inspection changed scene dirty state.");
            }
            Debug.Log(
                $"OstinatoApprovedMaterialTargetInspected Slots={PlacementCount}, Vertices={mesh.vertexCount}, " +
                $"UvCount={uv.Length}, SceneChanged=False, SelectionCleared=True");
        }

        [MenuItem("Bellerophon/Enemies/Ostinato/Apply Approved Material Sample")]
        public static void ApplyApprovedOstinatoMaterialSample()
        {
            var scene = RequireOpenCargoRunScene();
            var placementRoot = RequireRoot(scene, PlacementRootName).transform;
            var otherRootSnapshots = scene.GetRootGameObjects()
                .Where(root => root.transform != placementRoot)
                .Select(root => new RootSnapshot(root))
                .ToArray();
            var approvedTransformSnapshots = placementRoot
                .GetComponentsInChildren<Transform>(true)
                .Select(target => new TransformSnapshot(target))
                .ToArray();
            var modelHashBefore = ComputeSha256(ProjectAbsolutePath(ModelAssetPath));

            if (placementRoot.childCount != PlacementCount)
            {
                throw new InvalidOperationException(
                    $"Approved Ostinato placement must contain exactly {PlacementCount} slots.");
            }

            var importer = AssetImporter.GetAtPath(ModelAssetPath) as ModelImporter ??
                throw new InvalidOperationException("Ostinato model importer is missing.");
            if (!importer.isReadable)
            {
                importer.isReadable = true;
                importer.SaveAndReimport();
            }

            var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ModelAssetPath) ??
                throw new InvalidOperationException("Ostinato model asset is missing after import.");
            var assetRenderer = modelAsset.GetComponentsInChildren<SkinnedMeshRenderer>(true).SingleOrDefault() ??
                throw new InvalidOperationException("Ostinato model must contain one skinned renderer.");
            var mesh = assetRenderer.sharedMesh ??
                throw new InvalidOperationException("Ostinato skinned renderer mesh is missing.");
            if (mesh.vertexCount != 3728 || mesh.subMeshCount != 1 || mesh.uv.Length != mesh.vertexCount)
            {
                throw new InvalidOperationException("Ostinato mesh structure no longer matches the approved sample.");
            }

            Directory.CreateDirectory(ProjectAbsolutePath(GeneratedTextureRoot));
            Directory.CreateDirectory(ProjectAbsolutePath(MaterialRoot));
            CopyApprovedProjectionTexture(ApprovedSampleFrontRender, ApprovedFrontProjectionPath);
            CopyApprovedProjectionTexture(ApprovedSampleSideRender, ApprovedSideProjectionPath);
            CopyApprovedProjectionTexture(ApprovedSampleBackRender, ApprovedBackProjectionPath);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            var material = CreateOrUpdateApprovedMaterial(mesh);
            var rendererPaths = new List<string>();
            var preservedAnimatorStates = new List<AnimatorSnapshot>();
            for (var index = 0; index < PlacementCount; index++)
            {
                var expectedSlotName = $"Ostinato_{index + 1:00}_Static_Review";
                var slot = placementRoot.GetChild(index);
                if (slot.name != expectedSlotName)
                {
                    throw new InvalidOperationException(
                        $"Expected slot {expectedSlotName}, found {slot.name}.");
                }
                var model = slot.Find(ModelChildName) ??
                    throw new InvalidOperationException(slot.name + " is missing " + ModelChildName + ".");
                preservedAnimatorStates.AddRange(model.GetComponentsInChildren<Animator>(true).Select(value => new AnimatorSnapshot(value)));
                var renderers = model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                if (renderers.Length != 1 || renderers[0].sharedMesh != mesh)
                {
                    throw new InvalidOperationException(slot.name + " does not contain the approved Ostinato mesh.");
                }
                renderers[0].sharedMaterials = new[] { material };
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderers[0]);
                EditorUtility.SetDirty(renderers[0]);
                rendererPaths.Add(slot.name + "/" + ModelChildName + "/" + renderers[0].name);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException("CargoRunMvp could not be saved after Ostinato material application.");
            }

            foreach (var snapshot in otherRootSnapshots)
            {
                snapshot.AssertUnchanged();
            }
            foreach (var snapshot in approvedTransformSnapshots)
            {
                snapshot.AssertUnchanged();
            }
            foreach (var snapshot in preservedAnimatorStates)
            {
                snapshot.AssertUnchanged();
            }
            foreach (var renderer in placementRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (renderer.sharedMaterials.Length != 1 || renderer.sharedMaterial != material)
                {
                    throw new InvalidOperationException(renderer.name + " does not use the approved Ostinato material.");
                }
            }

            var modelHashAfter = ComputeSha256(ProjectAbsolutePath(ModelAssetPath));
            if (modelHashBefore != modelHashAfter)
            {
                throw new InvalidOperationException("Ostinato FBX bytes changed during material application.");
            }

            var report = new StringBuilder();
            report.AppendLine("Scene=" + scene.path);
            report.AppendLine("PlacementRoot=" + PlacementRootName);
            report.AppendLine("PlacementCount=" + PlacementCount);
            report.AppendLine("Material=" + ApprovedMaterialPath);
            report.AppendLine("Shader=" + (material.shader != null ? material.shader.name : "None"));
            report.AppendLine(
                "SurfaceSource=" + ApprovedSampleFrontRender + "|" + ApprovedSampleSideRender + "|" +
                ApprovedSampleBackRender);
            report.AppendLine("FrontProjection=" + ApprovedFrontProjectionPath);
            report.AppendLine("SideProjection=" + ApprovedSideProjectionPath);
            report.AppendLine("BackProjection=" + ApprovedBackProjectionPath);
            report.AppendLine("ProjectionSourceBytesCopiedExactly=True");
            report.AppendLine("UvBakeUsed=False");
            report.AppendLine("AutomaticRegionClassificationUsed=False");
            report.AppendLine("MetallicSmoothness=None");
            report.AppendLine("Normal=None");
            report.AppendLine("Renderers=" + string.Join("|", rendererPaths));
            report.AppendLine("SourceSha256Before=" + modelHashBefore);
            report.AppendLine("SourceSha256After=" + modelHashAfter);
            report.AppendLine("GeometryChanged=False");
            report.AppendLine("UvChanged=False");
            report.AppendLine("RigChanged=False");
            report.AppendLine("AnimationChanged=False");
            report.AppendLine("TransformsChanged=False");
            report.AppendLine("OtherSceneRootsChanged=False");
            report.AppendLine("SelectionCleared=True");
            WriteReport("Ostinato_ApprovedMaterialApply.txt", report.ToString());
            Selection.activeObject = null;
            Debug.Log(
                $"OstinatoApprovedMaterialApplied Count={PlacementCount}, " +
                $"ProjectionSourceBytesCopiedExactly=True, Material={ApprovedMaterialPath}, " +
                "GeometryChanged=False, TransformsChanged=False, OtherSceneRootsChanged=False, SelectionCleared=True");
        }

        [MenuItem("Bellerophon/Enemies/Ostinato/Capture Approved Material Review")]
        public static void CaptureApprovedOstinatoMaterialReview()
        {
            RunApprovedOstinatoMaterialReview(true);
        }

        [MenuItem("Bellerophon/Enemies/Ostinato/Inspect Approved Material Render")]
        public static void InspectApprovedOstinatoMaterialRender()
        {
            RunApprovedOstinatoMaterialReview(false);
        }

        private static void RunApprovedOstinatoMaterialReview(bool saveCapture)
        {
            var scene = RequireOpenCargoRunScene();
            var sceneWasDirty = scene.isDirty;
            var placementRoot = RequireRoot(scene, PlacementRootName).transform;
            if (placementRoot.childCount != PlacementCount)
            {
                throw new InvalidOperationException(
                    $"Approved Ostinato placement must contain exactly {PlacementCount} slots.");
            }
            var material = AssetDatabase.LoadAssetAtPath<Material>(ApprovedMaterialPath) ??
                throw new InvalidOperationException("Approved Ostinato material is missing.");
            foreach (var renderer in placementRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (renderer.sharedMaterial != material)
                {
                    throw new InvalidOperationException(renderer.name + " does not use the approved material.");
                }
            }

            var sourceModel = placementRoot.GetChild(0).Find(ModelChildName) ??
                throw new InvalidOperationException("Ostinato first review model is missing.");
            var clone = UnityEngine.Object.Instantiate(sourceModel.gameObject);
            clone.name = "Ostinato_ApprovedMaterial_CaptureClone";
            clone.hideFlags = HideFlags.HideAndDontSave;
            clone.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            clone.transform.localScale = Vector3.one;
            SetCaptureOnly(clone);
            var cameraObject = new GameObject("Ostinato_ApprovedMaterial_CaptureCamera")
            {
                hideFlags = HideFlags.HideAndDontSave,
                layer = CaptureLayer
            };
            Texture2D approvedFrontSample = null;
            Texture2D approvedSideSample = null;
            Texture2D approvedBackSample = null;
            Texture2D frontRender = null;
            Texture2D sideRender = null;
            Texture2D backRender = null;
            Texture2D composite = null;
            var background = new Color(0.945f, 0.929f, 0.875f, 1f);
            try
            {
                var bounds = CalculateRendererBounds(clone.transform);
                var camera = cameraObject.AddComponent<Camera>();
                camera.cullingMask = 1 << CaptureLayer;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = background;
                camera.fieldOfView = 34f;
                camera.nearClipPlane = 0.03f;
                camera.farClipPlane = 1000f;
                camera.allowHDR = false;
                camera.allowMSAA = true;
                var target = bounds.center;
                var distance = CalculateCaptureDistance(bounds, camera.fieldOfView, 800f / 500f);
                var cloneRenderer = clone.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single();
                Debug.Log(
                    "OstinatoProjectionRendererAxes " +
                    "WorldPositiveZInRenderer=" + FormatVector3(cloneRenderer.transform.InverseTransformDirection(Vector3.forward)) + "," +
                    "WorldPositiveXInRenderer=" + FormatVector3(cloneRenderer.transform.InverseTransformDirection(Vector3.right)) + "," +
                    "WorldNegativeZInRenderer=" + FormatVector3(cloneRenderer.transform.InverseTransformDirection(Vector3.back)));
                frontRender = RenderPreview(camera, target + Vector3.forward * distance, target, 800, 500);
                sideRender = RenderPreview(camera, target + Vector3.right * distance, target, 800, 500);
                backRender = RenderPreview(camera, target + Vector3.back * distance, target, 800, 500);
                approvedFrontSample = LoadPng(ProjectAbsolutePath(ApprovedSampleFrontRender));
                approvedSideSample = LoadPng(ProjectAbsolutePath(ApprovedSampleSideRender));
                approvedBackSample = LoadPng(ProjectAbsolutePath(ApprovedSampleBackRender));
                var approvedFrontMetrics = AnalyzePreview(approvedFrontSample, background);
                var approvedSideMetrics = AnalyzePreview(approvedSideSample, background);
                var approvedBackMetrics = AnalyzePreview(approvedBackSample, background);
                var frontMetrics = AnalyzePreview(frontRender, background);
                var sideMetrics = AnalyzePreview(sideRender, background);
                var backMetrics = AnalyzePreview(backRender, background);
                frontMetrics.RequireAppearanceNear(approvedFrontMetrics, "Front");
                sideMetrics.RequireAppearanceNear(approvedSideMetrics, "Side");
                backMetrics.RequireAppearanceNear(approvedBackMetrics, "Back");

                var folder = ProjectAbsolutePath(ValidationFolder);
                Directory.CreateDirectory(folder);
                File.WriteAllLines(
                    Path.Combine(folder, "Ostinato_ApprovedMaterialRenderInspection.txt"),
                    new[]
                    {
                        "ApprovedFront=" + approvedFrontMetrics,
                        "ApprovedSide=" + approvedSideMetrics,
                        "ApprovedBack=" + approvedBackMetrics,
                        "Front=" + frontMetrics,
                        "Side=" + sideMetrics,
                        "Back=" + backMetrics,
                        "PlacementCount=" + PlacementCount,
                        "Material=" + ApprovedMaterialPath,
                        "CaptureSaved=False",
                        "SceneViewFocused=False",
                        "SceneSaved=False",
                        "SelectionCleared=True"
                    });
                if (saveCapture)
                {
                    composite = new Texture2D(1600, 1500, TextureFormat.RGBA32, false, false);
                    FillTexture(composite, new Color32(27, 30, 24, 255));
                    PasteFit(composite, approvedFrontSample, new RectInt(0, 1000, 800, 500));
                    PasteFit(composite, frontRender, new RectInt(800, 1000, 800, 500));
                    PasteFit(composite, approvedSideSample, new RectInt(0, 500, 800, 500));
                    PasteFit(composite, sideRender, new RectInt(800, 500, 800, 500));
                    PasteFit(composite, approvedBackSample, new RectInt(0, 0, 800, 500));
                    PasteFit(composite, backRender, new RectInt(800, 0, 800, 500));
                    composite.Apply(false, false);
                    var finalPath = Path.Combine(folder, FinalComparisonFileName);
                    File.WriteAllBytes(finalPath, composite.EncodeToPNG());
                    File.WriteAllLines(
                        Path.Combine(folder, "Ostinato_ApprovedMaterialCaptureManifest.txt"),
                        new[]
                        {
                            "Capture=" + FinalComparisonFileName,
                            "Rows=ApprovedFront|UnityFront;ApprovedSide|UnitySide;ApprovedBack|UnityBack",
                            "ApprovedFront=" + approvedFrontMetrics,
                            "ApprovedSide=" + approvedSideMetrics,
                            "ApprovedBack=" + approvedBackMetrics,
                            "Front=" + frontMetrics,
                            "Side=" + sideMetrics,
                            "Back=" + backMetrics,
                            "PlacementCount=" + PlacementCount,
                            "Material=" + ApprovedMaterialPath,
                            "SceneViewFocused=False",
                            "SceneSaved=False",
                            "SelectionCleared=True"
                        });
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(composite);
                UnityEngine.Object.DestroyImmediate(approvedFrontSample);
                UnityEngine.Object.DestroyImmediate(approvedSideSample);
                UnityEngine.Object.DestroyImmediate(approvedBackSample);
                UnityEngine.Object.DestroyImmediate(frontRender);
                UnityEngine.Object.DestroyImmediate(sideRender);
                UnityEngine.Object.DestroyImmediate(backRender);
                UnityEngine.Object.DestroyImmediate(clone);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                Selection.activeObject = null;
            }
            if (scene.isDirty != sceneWasDirty)
            {
                throw new InvalidOperationException("Ostinato material capture changed scene dirty state.");
            }
            Debug.Log(
                saveCapture
                    ? "OstinatoApprovedMaterialReviewCaptured Rows=ApprovedFront|UnityFront;ApprovedSide|UnitySide;ApprovedBack|UnityBack, " +
                      "SceneViewFocused=False, SceneSaved=False, SelectionCleared=True"
                    : "OstinatoApprovedMaterialRenderInspected Views=UnityFront|UnitySide|UnityBack, " +
                      "CaptureSaved=False, SceneViewFocused=False, SceneSaved=False, SelectionCleared=True");
        }

        private static Texture2D RenderPreview(
            Camera camera,
            Vector3 cameraPosition,
            Vector3 target,
            int width,
            int height)
        {
            camera.transform.position = cameraPosition;
            camera.transform.rotation = Quaternion.LookRotation(target - cameraPosition, Vector3.up);
            var renderTexture = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            try
            {
                camera.targetTexture = renderTexture;
                camera.Render();
                RenderTexture.active = renderTexture;
                var output = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
                output.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                output.Apply(false, false);
                return output;
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(renderTexture);
            }
        }

        private static PreviewMetrics AnalyzePreview(Texture2D texture, Color background)
        {
            var metrics = new PreviewMetrics();
            var pixels = texture.GetPixels32();
            var firstModelRow = Mathf.FloorToInt(texture.height * 0.12f);
            for (var y = firstModelRow; y < texture.height; y++)
            {
                for (var x = 0; x < texture.width; x++)
                {
                    var color = (Color)pixels[y * texture.width + x];
                    var distance = Mathf.Max(
                        Mathf.Abs(color.r - background.r),
                        Mathf.Max(Mathf.Abs(color.g - background.g), Mathf.Abs(color.b - background.b)));
                    if (distance < 0.10f)
                    {
                        continue;
                    }
                    metrics.Foreground++;
                    metrics.LuminanceSum += color.r * 0.2126 + color.g * 0.7152 + color.b * 0.0722;
                    Color.RGBToHSV(color, out var hue, out var saturation, out var value);
                    if (hue >= 0.11f && hue <= 0.42f && saturation > 0.14f)
                    {
                        metrics.Green++;
                    }
                    if ((hue < 0.12f || hue > 0.92f) && saturation > 0.14f)
                    {
                        metrics.Rust++;
                    }
                    if (saturation < 0.28f && value > 0.12f && value < 0.92f)
                    {
                        metrics.Steel++;
                    }
                }
            }
            metrics.Total = texture.width * texture.height;
            return metrics;
        }

        private static Texture2D LoadPng(string path)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, false);
            if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(path), false))
            {
                UnityEngine.Object.DestroyImmediate(texture);
                throw new InvalidOperationException(path + " could not be decoded.");
            }
            return texture;
        }

        private static void FillTexture(Texture2D texture, Color32 color)
        {
            texture.SetPixels32(Enumerable.Repeat(color, texture.width * texture.height).ToArray());
        }

        private static void PasteFit(Texture2D destination, Texture2D source, RectInt area)
        {
            var scale = Mathf.Min(area.width / (float)source.width, area.height / (float)source.height);
            var width = Mathf.Max(1, Mathf.RoundToInt(source.width * scale));
            var height = Mathf.Max(1, Mathf.RoundToInt(source.height * scale));
            var resized = ResizeTexture(source, width, height);
            try
            {
                var x = area.x + (area.width - width) / 2;
                var y = area.y + (area.height - height) / 2;
                destination.SetPixels32(x, y, width, height, resized.GetPixels32());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(resized);
            }
        }

        private static Texture2D ResizeTexture(Texture2D source, int width, int height)
        {
            var renderTexture = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
            var previousActive = RenderTexture.active;
            try
            {
                Graphics.Blit(source, renderTexture);
                RenderTexture.active = renderTexture;
                var output = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
                output.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                output.Apply(false, false);
                return output;
            }
            finally
            {
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(renderTexture);
            }
        }

        private static Bounds CalculateRendererBounds(Transform root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.enabled && renderer.gameObject.activeInHierarchy)
                .ToArray();
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException(root.name + " contains no visible renderers.");
            }
            var bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }
            return bounds;
        }

        private static float CalculateCaptureDistance(Bounds bounds, float verticalFieldOfView, float aspect)
        {
            var verticalRadians = verticalFieldOfView * Mathf.Deg2Rad;
            var horizontalRadians = 2f * Mathf.Atan(Mathf.Tan(verticalRadians * 0.5f) * aspect);
            var verticalDistance = bounds.extents.y / Mathf.Max(Mathf.Tan(verticalRadians * 0.5f), 0.01f);
            var horizontalDistance = bounds.extents.x / Mathf.Max(Mathf.Tan(horizontalRadians * 0.5f), 0.01f);
            return Mathf.Max(verticalDistance, horizontalDistance) * 1.18f + bounds.extents.z;
        }

        private static void SetCaptureOnly(GameObject root)
        {
            foreach (var target in root.GetComponentsInChildren<Transform>(true))
            {
                target.gameObject.layer = CaptureLayer;
                target.gameObject.hideFlags = HideFlags.HideAndDontSave;
            }
            foreach (var skinned in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                skinned.updateWhenOffscreen = true;
            }
        }

        private static BakeMetrics BakeApprovedReferenceTextures(Mesh mesh)
        {
            var front = ReferenceProjection.Load(ApprovedSampleFrontRender);
            var side = ReferenceProjection.Load(ApprovedSampleSideRender);
            var back = ReferenceProjection.Load(ApprovedSampleBackRender);
            try
            {
                var albedo = Enumerable.Repeat(new Color32(69, 71, 39, 255), BakeSize * BakeSize).ToArray();
                var metallicSmoothness = Enumerable.Repeat(new Color32(0, 0, 0, 133), BakeSize * BakeSize).ToArray();
                var written = new bool[BakeSize * BakeSize];
                var vertices = mesh.vertices;
                var normals = mesh.normals;
                var uv = mesh.uv;
                var triangles = mesh.triangles;
                var bounds = mesh.bounds;
                var metrics = new BakeMetrics();

                for (var triangleIndex = 0; triangleIndex < triangles.Length; triangleIndex += 3)
                {
                    var index0 = triangles[triangleIndex];
                    var index1 = triangles[triangleIndex + 1];
                    var index2 = triangles[triangleIndex + 2];
                    RasterizeTriangle(
                        uv[index0], uv[index1], uv[index2],
                        vertices[index0], vertices[index1], vertices[index2],
                        normals[index0], normals[index1], normals[index2],
                        bounds, front, side, back, albedo, metallicSmoothness, written, metrics);
                }

                DilateTextureIslands(albedo, metallicSmoothness, written, 12);
                SaveTexture(BakedAlbedoPath, albedo, TextureFormat.RGBA32);
                SaveTexture(BakedMetallicSmoothnessPath, metallicSmoothness, TextureFormat.RGBA32);
                metrics.RasterizedPixelCount = written.Count(value => value);
                metrics.RasterizedCoverage = metrics.RasterizedPixelCount / (float)(BakeSize * BakeSize);
                return metrics;
            }
            finally
            {
                front.Dispose();
                side.Dispose();
                back.Dispose();
            }
        }

        private static void RasterizeTriangle(
            Vector2 uv0,
            Vector2 uv1,
            Vector2 uv2,
            Vector3 position0,
            Vector3 position1,
            Vector3 position2,
            Vector3 normal0,
            Vector3 normal1,
            Vector3 normal2,
            Bounds bounds,
            ReferenceProjection front,
            ReferenceProjection side,
            ReferenceProjection back,
            Color32[] albedo,
            Color32[] metallicSmoothness,
            bool[] written,
            BakeMetrics metrics)
        {
            var pixel0 = new Vector2(uv0.x * (BakeSize - 1), uv0.y * (BakeSize - 1));
            var pixel1 = new Vector2(uv1.x * (BakeSize - 1), uv1.y * (BakeSize - 1));
            var pixel2 = new Vector2(uv2.x * (BakeSize - 1), uv2.y * (BakeSize - 1));
            var denominator =
                (pixel1.y - pixel2.y) * (pixel0.x - pixel2.x) +
                (pixel2.x - pixel1.x) * (pixel0.y - pixel2.y);
            if (Mathf.Abs(denominator) < 0.00001f)
            {
                return;
            }

            var minX = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(pixel0.x, Mathf.Min(pixel1.x, pixel2.x))), 0, BakeSize - 1);
            var maxX = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(pixel0.x, Mathf.Max(pixel1.x, pixel2.x))), 0, BakeSize - 1);
            var minY = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(pixel0.y, Mathf.Min(pixel1.y, pixel2.y))), 0, BakeSize - 1);
            var maxY = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(pixel0.y, Mathf.Max(pixel1.y, pixel2.y))), 0, BakeSize - 1);
            for (var y = minY; y <= maxY; y++)
            {
                for (var x = minX; x <= maxX; x++)
                {
                    var sample = new Vector2(x + 0.5f, y + 0.5f);
                    var weight0 =
                        ((pixel1.y - pixel2.y) * (sample.x - pixel2.x) +
                         (pixel2.x - pixel1.x) * (sample.y - pixel2.y)) / denominator;
                    var weight1 =
                        ((pixel2.y - pixel0.y) * (sample.x - pixel2.x) +
                         (pixel0.x - pixel2.x) * (sample.y - pixel2.y)) / denominator;
                    var weight2 = 1f - weight0 - weight1;
                    if (weight0 < -0.001f || weight1 < -0.001f || weight2 < -0.001f)
                    {
                        continue;
                    }

                    var position = position0 * weight0 + position1 * weight1 + position2 * weight2;
                    var normal = (normal0 * weight0 + normal1 * weight1 + normal2 * weight2).normalized;
                    var normalizedPosition = new Vector3(
                        Mathf.InverseLerp(bounds.min.x, bounds.max.x, position.x),
                        Mathf.InverseLerp(bounds.min.y, bounds.max.y, position.y),
                        Mathf.InverseLerp(bounds.min.z, bounds.max.z, position.z));
                    var surface = SampleApprovedSurface(normalizedPosition, normal, front, side, back);
                    var pixelIndex = y * BakeSize + x;
                    albedo[pixelIndex] = surface.Color;
                    metallicSmoothness[pixelIndex] = surface.MetallicSmoothness;
                    if (!written[pixelIndex])
                    {
                        written[pixelIndex] = true;
                        metrics.Add(surface.Region);
                    }
                }
            }
        }

        private static SurfaceSample SampleApprovedSurface(
            Vector3 normalizedPosition,
            Vector3 normal,
            ReferenceProjection front,
            ReferenceProjection side,
            ReferenceProjection back)
        {
            var vertical = 1f - normalizedPosition.y;
            ReferenceProjection primary;
            ReferenceProjection secondary;
            ReferenceProjection tertiary;
            float horizontal;
            if (Mathf.Abs(normal.z) >= Mathf.Abs(normal.x))
            {
                if (normal.z >= 0f)
                {
                    primary = front;
                    secondary = back;
                    tertiary = side;
                    horizontal = normalizedPosition.x;
                }
                else
                {
                    primary = back;
                    secondary = front;
                    tertiary = side;
                    horizontal = 1f - normalizedPosition.x;
                }
            }
            else
            {
                primary = side;
                secondary = front;
                tertiary = back;
                horizontal = normal.x >= 0f ? normalizedPosition.z : 1f - normalizedPosition.z;
            }

            var color = primary.Sample(horizontal, vertical, out var foreground);
            if (!foreground)
            {
                color = secondary.Sample(horizontal, vertical, out foreground);
            }
            if (!foreground)
            {
                color = tertiary.Sample(horizontal, vertical, out foreground);
            }
            if (!foreground)
            {
                color = primary.SampleDensePatch(horizontal, vertical);
            }

            var region = ClassifyRegion(color, normalizedPosition, normal);
            return new SurfaceSample(color, region);
        }

        private static MaterialRegion ClassifyRegion(Color32 color, Vector3 normalizedPosition, Vector3 normal)
        {
            var converted = (Color)color;
            Color.RGBToHSV(converted, out var hue, out var saturation, out var value);
            var bladePosition =
                (normalizedPosition.x < 0.23f || normalizedPosition.x > 0.77f) &&
                normalizedPosition.y > 0.24f && normalizedPosition.y < 0.88f;
            if (bladePosition && saturation < 0.30f && value > 0.14f)
            {
                return MaterialRegion.Blade;
            }
            var eyePosition =
                normalizedPosition.y > 0.82f &&
                normalizedPosition.x > 0.35f && normalizedPosition.x < 0.65f &&
                (normal.z > 0.12f || Mathf.Abs(normal.x) > 0.55f);
            if (eyePosition && saturation > 0.16f)
            {
                return MaterialRegion.Eye;
            }
            if ((hue < 0.125f || hue > 0.92f) && saturation > 0.18f)
            {
                return MaterialRegion.Tissue;
            }
            return MaterialRegion.Chitin;
        }

        private static void DilateTextureIslands(
            Color32[] albedo,
            Color32[] metallicSmoothness,
            bool[] written,
            int passes)
        {
            var offsets = new[] { -1, 1, -BakeSize, BakeSize };
            for (var pass = 0; pass < passes; pass++)
            {
                var previous = (bool[])written.Clone();
                for (var index = 0; index < written.Length; index++)
                {
                    if (previous[index])
                    {
                        continue;
                    }
                    var x = index % BakeSize;
                    var y = index / BakeSize;
                    foreach (var offset in offsets)
                    {
                        var source = index + offset;
                        if (source < 0 || source >= previous.Length || !previous[source])
                        {
                            continue;
                        }
                        var sourceX = source % BakeSize;
                        var sourceY = source / BakeSize;
                        if (Mathf.Abs(sourceX - x) + Mathf.Abs(sourceY - y) != 1)
                        {
                            continue;
                        }
                        albedo[index] = albedo[source];
                        metallicSmoothness[index] = metallicSmoothness[source];
                        written[index] = true;
                        break;
                    }
                }
            }
        }

        private static void SaveTexture(string assetPath, Color32[] pixels, TextureFormat format)
        {
            var texture = new Texture2D(BakeSize, BakeSize, format, false, false);
            try
            {
                texture.SetPixels32(pixels);
                texture.Apply(false, false);
                File.WriteAllBytes(ProjectAbsolutePath(assetPath), texture.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static void ConfigureTextureImporter(string path, bool normalMap, bool sRgb)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter ??
                throw new InvalidOperationException(path + " texture importer is missing.");
            importer.textureType = normalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = normalMap ? false : sRgb;
            importer.mipmapEnabled = true;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Trilinear;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }

        private static void CopyApprovedProjectionTexture(string sourcePath, string destinationPath)
        {
            File.Copy(ProjectAbsolutePath(sourcePath), ProjectAbsolutePath(destinationPath), true);
            AssetDatabase.ImportAsset(destinationPath, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(destinationPath) as TextureImporter ??
                throw new InvalidOperationException(destinationPath + " texture importer is missing.");
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.alphaSource = TextureImporterAlphaSource.None;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            if (ComputeSha256(ProjectAbsolutePath(sourcePath)) != ComputeSha256(ProjectAbsolutePath(destinationPath)))
            {
                throw new InvalidOperationException(destinationPath + " is not a byte-exact copy of the approved sample.");
            }
        }

        private static Material CreateOrUpdateApprovedMaterial(Mesh mesh)
        {
            var shader = Shader.Find(ApprovedProjectionShaderName) ??
                throw new InvalidOperationException(ApprovedProjectionShaderName + " shader is unavailable.");
            var material = AssetDatabase.LoadAssetAtPath<Material>(ApprovedMaterialPath);
            if (material == null)
            {
                material = new Material(shader) { name = "Ostinato_ApprovedReference" };
                AssetDatabase.CreateAsset(material, ApprovedMaterialPath);
            }
            else
            {
                material.shader = shader;
            }

            var frontProjection = AssetDatabase.LoadAssetAtPath<Texture2D>(ApprovedFrontProjectionPath) ??
                throw new InvalidOperationException("Approved Ostinato front projection was not imported.");
            var sideProjection = AssetDatabase.LoadAssetAtPath<Texture2D>(ApprovedSideProjectionPath) ??
                throw new InvalidOperationException("Approved Ostinato side projection was not imported.");
            var backProjection = AssetDatabase.LoadAssetAtPath<Texture2D>(ApprovedBackProjectionPath) ??
                throw new InvalidOperationException("Approved Ostinato back projection was not imported.");
            var chitin = AssetDatabase.LoadAssetAtPath<Texture2D>(ChitinTexturePath) ??
                throw new InvalidOperationException("Approved Ostinato insect chitin texture was not imported.");
            using var frontReference = ReferenceProjection.Load(ApprovedSampleFrontRender);
            using var sideReference = ReferenceProjection.Load(ApprovedSampleSideRender);
            using var backReference = ReferenceProjection.Load(ApprovedSampleBackRender);
            material.SetColor("_BaseColor", Color.white);
            material.SetColor("_Color", Color.white);
            material.SetTexture("_FrontTex", frontProjection);
            material.SetTexture("_SideTex", sideProjection);
            material.SetTexture("_BackTex", backProjection);
            material.SetTexture("_ChitinTex", chitin);
            material.SetVector("_FrontRect", frontReference.UvRect);
            material.SetVector("_SideRect", sideReference.UvRect);
            material.SetVector("_BackRect", backReference.UvRect);
            material.SetVector("_BoundsMin", mesh.bounds.min);
            material.SetVector("_BoundsSize", mesh.bounds.size);
            material.SetFloat("_ProjectionSharpness", 16f);
            material.SetTexture("_BaseMap", null);
            material.SetTexture("_MainTex", null);
            material.SetTexture("_BumpMap", null);
            material.SetTexture("_MetallicGlossMap", null);
            material.SetTexture("_EmissionMap", null);
            material.SetFloat("_BumpScale", 0f);
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Smoothness", 0f);
            material.SetFloat("_ReceiveShadows", 0f);
            material.SetFloat("_EnvironmentReflections", 0f);
            material.SetFloat("_SpecularHighlights", 0f);
            material.SetColor("_EmissionColor", Color.black);
            material.DisableKeyword("_NORMALMAP");
            material.DisableKeyword("_METALLICSPECGLOSSMAP");
            material.DisableKeyword("_EMISSION");
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            return material;
        }

        private enum MaterialRegion
        {
            Chitin,
            Tissue,
            Blade,
            Eye
        }

        private readonly struct SurfaceSample
        {
            public readonly Color32 Color;
            public readonly MaterialRegion Region;
            public readonly Color32 MetallicSmoothness;

            public SurfaceSample(Color32 color, MaterialRegion region)
            {
                Color = color;
                Region = region;
                MetallicSmoothness = region switch
                {
                    MaterialRegion.Tissue => new Color32(0, 0, 0, 71),
                    MaterialRegion.Blade => new Color32(224, 224, 224, 189),
                    MaterialRegion.Eye => new Color32(13, 13, 13, 214),
                    _ => new Color32(0, 0, 0, 133)
                };
            }
        }

        private sealed class BakeMetrics
        {
            public int RasterizedPixelCount;
            public float RasterizedCoverage;
            public int ChitinPixelCount;
            public int TissuePixelCount;
            public int BladePixelCount;
            public int EyePixelCount;

            public void Add(MaterialRegion region)
            {
                switch (region)
                {
                    case MaterialRegion.Tissue:
                        TissuePixelCount++;
                        break;
                    case MaterialRegion.Blade:
                        BladePixelCount++;
                        break;
                    case MaterialRegion.Eye:
                        EyePixelCount++;
                        break;
                    default:
                        ChitinPixelCount++;
                        break;
                }
            }
        }

        private sealed class PreviewMetrics
        {
            public int Total;
            public int Foreground;
            public int Green;
            public int Rust;
            public int Steel;
            public double LuminanceSum;

            public void RequireMaterialDistribution(string view)
            {
                if (Total <= 0 || Foreground < Total * 0.025f)
                {
                    throw new InvalidOperationException(
                        view + " preview does not contain a visible Ostinato model. " + ToString());
                }
                if (Green < Foreground * 0.04f || Rust < Foreground * 0.07f || Steel < Foreground * 0.008f)
                {
                    throw new InvalidOperationException(
                        view + " preview is missing the approved green chitin, rust tissue, or steel distribution. " +
                        ToString());
                }
            }

            public void RequireLuminanceNear(PreviewMetrics approved, string view)
            {
                var approvedLuminance = approved.MeanLuminance;
                var ratio = MeanLuminance / Math.Max(approvedLuminance, 0.0001);
                if (ratio < 0.78 || ratio > 1.22)
                {
                    throw new InvalidOperationException(
                        $"{view} preview luminance ratio {ratio:0.######} is outside the approved sample range. " +
                        $"Approved={approvedLuminance:0.######}, Current={MeanLuminance:0.######}");
                }
            }

            public void RequireAppearanceNear(PreviewMetrics approved, string view)
            {
                RequireMaterialDistribution(view);
                var foreground = Mathf.Max(Foreground, 1);
                var approvedForeground = Mathf.Max(approved.Foreground, 1);
                var greenDifference = Math.Abs(Green / (double)foreground - approved.Green / (double)approvedForeground);
                var rustDifference = Math.Abs(Rust / (double)foreground - approved.Rust / (double)approvedForeground);
                var steelDifference = Math.Abs(Steel / (double)foreground - approved.Steel / (double)approvedForeground);
                if (greenDifference > 0.16 || rustDifference > 0.20 || steelDifference > 0.16)
                {
                    throw new InvalidOperationException(
                        $"{view} preview material distribution differs from the approved sample. " +
                        $"GreenDiff={greenDifference:0.######}, RustDiff={rustDifference:0.######}, " +
                        $"SteelDiff={steelDifference:0.######}, Approved={approved}, Current={this}");
                }
                RequireLuminanceNear(approved, view);
            }

            public double MeanLuminance => LuminanceSum / Math.Max(Foreground, 1);

            public override string ToString()
            {
                return
                    $"Foreground={Foreground}," +
                    $"GreenRatio={Green / (float)Mathf.Max(Foreground, 1):0.######}," +
                    $"RustRatio={Rust / (float)Mathf.Max(Foreground, 1):0.######}," +
                    $"SteelRatio={Steel / (float)Mathf.Max(Foreground, 1):0.######}," +
                    $"MeanLuminance={MeanLuminance:0.######}";
            }
        }

        private sealed class ReferenceProjection : IDisposable
        {
            private readonly Texture2D texture;
            private readonly Color32[] pixels;
            private readonly int width;
            private readonly int height;
            private readonly int minX;
            private readonly int maxX;
            private readonly int minY;
            private readonly int maxY;
            private readonly Vector4 densePatch;

            private ReferenceProjection(Texture2D texture, string assetPath)
            {
                this.texture = texture;
                pixels = texture.GetPixels32();
                width = texture.width;
                height = texture.height;
                minX = width - 1;
                maxX = 0;
                minY = height - 1;
                maxY = 0;
                var found = false;
                for (var y = 0; y < height; y++)
                {
                    for (var x = 0; x < width; x++)
                    {
                        if (!IsForeground(pixels[y * width + x]))
                        {
                            continue;
                        }
                        found = true;
                        minX = Mathf.Min(minX, x);
                        maxX = Mathf.Max(maxX, x);
                        minY = Mathf.Min(minY, y);
                        maxY = Mathf.Max(maxY, y);
                    }
                }
                if (!found)
                {
                    throw new InvalidOperationException(assetPath + " contains no reference foreground.");
                }
                densePatch = assetPath == FrontReferencePath || assetPath == ApprovedSampleFrontRender
                    ? new Vector4(0.468f, 0.202f, 0.607f, 0.422f)
                    : assetPath == SideReferencePath || assetPath == ApprovedSampleSideRender
                        ? new Vector4(0.599f, 0.241f, 0.698f, 0.381f)
                        : new Vector4(0.453f, 0.099f, 0.593f, 0.319f);
            }

            public static ReferenceProjection Load(string assetPath)
            {
                var bytes = File.ReadAllBytes(ProjectAbsolutePath(assetPath));
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, false)
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                if (!ImageConversion.LoadImage(texture, bytes, false))
                {
                    UnityEngine.Object.DestroyImmediate(texture);
                    throw new InvalidOperationException(assetPath + " could not be decoded.");
                }
                return new ReferenceProjection(texture, assetPath);
            }

            public Color32 Sample(float u, float vTop, out bool foreground)
            {
                var color = SampleRaw(u, vTop);
                foreground = IsForeground(color);
                return color;
            }

            public Color32 SampleDensePatch(float u, float vTop)
            {
                var tiledU = u * 3f;
                var tiledV = vTop * 3f;
                var tileX = Mathf.FloorToInt(tiledU);
                var tileY = Mathf.FloorToInt(tiledV);
                var localU = tiledU - Mathf.Floor(tiledU);
                var localV = tiledV - Mathf.Floor(tiledV);
                if ((tileX & 1) != 0)
                {
                    localU = 1f - localU;
                }
                if ((tileY & 1) != 0)
                {
                    localV = 1f - localV;
                }
                var patchU = Mathf.Lerp(densePatch.x, densePatch.z, localU);
                var patchV = Mathf.Lerp(densePatch.y, densePatch.w, localV);
                return SampleRaw(patchU, patchV);
            }

            public Vector4 UvRect => new Vector4(
                minX / (float)Mathf.Max(width - 1, 1),
                minY / (float)Mathf.Max(height - 1, 1),
                (maxX - minX) / (float)Mathf.Max(width - 1, 1),
                (maxY - minY) / (float)Mathf.Max(height - 1, 1));

            public void Dispose()
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }

            private Color32 SampleRaw(float u, float vTop)
            {
                var x = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(minX, maxX, Mathf.Clamp01(u))), minX, maxX);
                var y = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(maxY, minY, Mathf.Clamp01(vTop))), minY, maxY);
                return pixels[y * width + x];
            }

            private static bool IsForeground(Color32 color)
            {
                var converted = (Color)color;
                Color.RGBToHSV(converted, out _, out var saturation, out var value);
                var backgroundDistance = Mathf.Max(255 - color.r, Mathf.Max(255 - color.g, 255 - color.b));
                return backgroundDistance > 18 && (saturation > 0.075f || value < 0.38f);
            }
        }

        private readonly struct RootSnapshot
        {
            private readonly GameObject target;
            private readonly Vector3 position;
            private readonly Quaternion rotation;
            private readonly Vector3 scale;
            private readonly bool activeSelf;
            private readonly int childCount;

            public RootSnapshot(GameObject target)
            {
                this.target = target;
                position = target.transform.position;
                rotation = target.transform.rotation;
                scale = target.transform.localScale;
                activeSelf = target.activeSelf;
                childCount = target.transform.childCount;
            }

            public void AssertUnchanged()
            {
                if (target == null || target.transform.position != position || target.transform.rotation != rotation ||
                    target.transform.localScale != scale || target.activeSelf != activeSelf ||
                    target.transform.childCount != childCount)
                {
                    throw new InvalidOperationException("Ostinato material application changed an unapproved scene root.");
                }
            }
        }

        private readonly struct TransformSnapshot
        {
            private readonly Transform target;
            private readonly Vector3 localPosition;
            private readonly Quaternion localRotation;
            private readonly Vector3 localScale;
            private readonly bool activeSelf;

            public TransformSnapshot(Transform target)
            {
                this.target = target;
                localPosition = target.localPosition;
                localRotation = target.localRotation;
                localScale = target.localScale;
                activeSelf = target.gameObject.activeSelf;
            }

            public void AssertUnchanged()
            {
                if (target == null || target.localPosition != localPosition || target.localRotation != localRotation ||
                    target.localScale != localScale || target.gameObject.activeSelf != activeSelf)
                {
                    throw new InvalidOperationException("Ostinato material application changed an approved Transform.");
                }
            }
        }

        private readonly struct AnimatorSnapshot
        {
            private readonly Animator target;
            private readonly RuntimeAnimatorController controller;
            private readonly bool applyRootMotion;
            private readonly bool enabled;

            public AnimatorSnapshot(Animator target)
            {
                this.target = target;
                controller = target.runtimeAnimatorController;
                applyRootMotion = target.applyRootMotion;
                enabled = target.enabled;
            }

            public void AssertUnchanged()
            {
                if (target == null || target.runtimeAnimatorController != controller ||
                    target.applyRootMotion != applyRootMotion || target.enabled != enabled)
                {
                    throw new InvalidOperationException("Ostinato material application changed Animator state.");
                }
            }
        }

        private static string ComputeSha256(string path)
        {
            using var stream = File.OpenRead(path);
            using var sha256 = SHA256.Create();
            return BitConverter.ToString(sha256.ComputeHash(stream)).Replace("-", string.Empty);
        }

        private static string ProjectAbsolutePath(string assetOrRelativePath)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetOrRelativePath));
        }

        private static Scene RequireOpenCargoRunScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || scene.path != CargoRunScenePath)
            {
                throw new InvalidOperationException("CargoRunMvp must already be the active scene.");
            }
            return scene;
        }

        private static GameObject RequireRoot(Scene scene, string name)
        {
            return scene.GetRootGameObjects().FirstOrDefault(root => root.name == name) ??
                throw new InvalidOperationException(name + " root is missing from CargoRunMvp.");
        }

        private static void WriteReport(string fileName, string contents)
        {
            var folder = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ValidationFolder));
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, fileName), contents, new UTF8Encoding(false));
        }

        private static string FormatVector2(Vector2 value) => $"({value.x:0.######},{value.y:0.######})";
        private static string FormatVector3(Vector3 value) => $"({value.x:0.######},{value.y:0.######},{value.z:0.######})";
    }
}
