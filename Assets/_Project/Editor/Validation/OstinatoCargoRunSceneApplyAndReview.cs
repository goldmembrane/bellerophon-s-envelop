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

namespace Bellerophon.Editor.OstinatoCargoRunScene
{
    internal static class OstinatoCargoRunSceneApplyAndReview
    {
        private const string CargoRunScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string OstinatoSourceRelativePath = "enemies model/ostinato.fbx";
        private const string OstinatoModelAssetPath =
            "Assets/_Project/Art/Enemies/Ostinato/Models/Ostinato.fbx";
        private const string LongaRootName = "Approved Longa Arma Enemy Placement";
        private const string TergoRootName = "Approved Tergo Enemy Placement";
        private const string SmorzandoRootName = "Approved Smorzando Enemy Placement";
        private const string SmorzandoStaticSlotName = "Smorzando_Installed_01";
        private const string SmorzandoPersonFirstSlotName = "Smorzando_Person_01";
        private const string SmorzandoPersonSecondSlotName = "Smorzando_Person_02";
        private const string PlayerRootName = "Player";
        private const string PlacementRootName = "Approved Ostinato Enemy Placement";
        private const string SlotPrefix = "Ostinato_";
        private const string ModelChildName = "Ostinato_Model";
        private const string ValidationRelativeFolder =
            "docs/validation/ostinato_placement_2026-07-18";
        private const string InspectionReportRelativePath =
            ValidationRelativeFolder + "/Ostinato_PlacementTargetInspection.txt";
        private const string OrientationRelativeFolder =
            ValidationRelativeFolder + "/source_orientation";
        private const string ApplyReportRelativePath =
            ValidationRelativeFolder + "/Ostinato_PlacementApply.txt";
        private const string CaptureRelativeFolder =
            ValidationRelativeFolder + "/automated_visual_capture";
        private const int PlacementCount = 9;
        private const int CaptureLayer = 31;
        private const float PlacementFacingYawDegrees = 180f;

        [MenuItem("Bellerophon/Enemies/Ostinato/Inspect Placement Target")]
        public static void InspectOstinatoPlacementTarget()
        {
            var scene = RequireOpenCargoRunScene();
            var sceneWasDirty = scene.isDirty;
            var longaRoot = RequireRoot(scene, LongaRootName);
            var tergoRoot = RequireRoot(scene, TergoRootName);
            var smorzandoRoot = RequireRoot(scene, SmorzandoRootName);
            var smorzandoStatic = smorzandoRoot.transform.Find(SmorzandoStaticSlotName) ??
                throw new InvalidOperationException("Smorzando installed static review object is missing.");
            var player = RequireRoot(scene, PlayerRootName);
            var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(OstinatoModelAssetPath) ??
                throw new InvalidOperationException("Ostinato FBX has not been imported.");
            var importer = AssetImporter.GetAtPath(OstinatoModelAssetPath) as ModelImporter ??
                throw new InvalidOperationException("Ostinato FBX importer is missing.");
            var assetRenderers = modelAsset.GetComponentsInChildren<Renderer>(true);
            if (assetRenderers.Length == 0)
            {
                throw new InvalidOperationException("Ostinato FBX contains no renderers.");
            }

            var zSpacing = CalculateLongaTergoSpacing(longaRoot.transform, tergoRoot.transform);
            var xSpacing = CalculateSmorzandoPersonXSpacing(smorzandoRoot.transform);
            var smorzandoBounds = CalculateRendererBounds(
                smorzandoStatic,
                new Bounds(smorzandoStatic.position, Vector3.one));
            var existingOstinato = scene.GetRootGameObjects()
                .FirstOrDefault(root => root.name == "Approved Ostinato Enemy Placement");
            var report = new StringBuilder();
            report.AppendLine("Scene=" + scene.path);
            report.AppendLine("SourceAsset=" + OstinatoModelAssetPath);
            report.AppendLine("SourceSha256=" + ComputeSha256(ProjectAbsolutePath(OstinatoSourceRelativePath)));
            report.AppendLine("ImportedSha256=" + ComputeSha256(ProjectAbsolutePath(OstinatoModelAssetPath)));
            report.AppendLine("AnimationType=" + importer.animationType);
            report.AppendLine("ImportAnimation=" + importer.importAnimation);
            report.AppendLine("RendererCount=" + assetRenderers.Length);
            report.AppendLine("LongaRootPosition=" + FormatVector(longaRoot.transform.position));
            report.AppendLine("TergoRootPosition=" + FormatVector(tergoRoot.transform.position));
            report.AppendLine("LongaTergoZSpacing=" + zSpacing.ToString("0.######"));
            report.AppendLine("SmorzandoPersonXSpacing=" + xSpacing.ToString("0.######"));
            report.AppendLine("SmorzandoStaticPosition=" + FormatVector(smorzandoStatic.position));
            report.AppendLine("SmorzandoStaticBoundsCenter=" + FormatVector(smorzandoBounds.center));
            report.AppendLine("SmorzandoStaticBoundsSize=" + FormatVector(smorzandoBounds.size));
            report.AppendLine("ExpectedOstinatoFirstPosition=" + FormatVector(
                smorzandoStatic.position + Vector3.back * zSpacing));
            report.AppendLine("ExistingOstinatoRoot=" + (existingOstinato != null));
            report.AppendLine("PlayerPosition=" + FormatVector(player.transform.position));
            report.AppendLine("PlayerEuler=" + FormatVector(player.transform.eulerAngles));
            var mainCamera = player.GetComponentInChildren<Camera>(true);
            report.AppendLine("PlayerMainCamera=" + (mainCamera != null ? RelativePath(player.transform, mainCamera.transform) : "None"));
            if (mainCamera != null)
            {
                report.AppendLine("MainCameraWorldPosition=" + FormatVector(mainCamera.transform.position));
                report.AppendLine("MainCameraWorldForward=" + FormatVector(mainCamera.transform.forward));
                report.AppendLine("MainCameraFieldOfView=" + mainCamera.fieldOfView.ToString("0.######"));
            }
            for (var index = 0; index < assetRenderers.Length; index++)
            {
                var renderer = assetRenderers[index];
                var mesh = RendererMesh(renderer);
                report.AppendLine(
                    $"Renderer[{index}]={RelativePath(modelAsset.transform, renderer.transform)}," +
                    $"Type={renderer.GetType().Name},Mesh={(mesh != null ? mesh.name : "None")}," +
                    $"Vertices={(mesh != null ? mesh.vertexCount : 0)}," +
                    $"SubMeshes={(mesh != null ? mesh.subMeshCount : 0)}," +
                    $"MeshBoundsCenter={(mesh != null ? FormatVector(mesh.bounds.center) : "None")}," +
                    $"MeshBoundsSize={(mesh != null ? FormatVector(mesh.bounds.size) : "None")}," +
                    $"Materials={string.Join("|", renderer.sharedMaterials.Select(MaterialName))}");
            }
            var clips = AssetDatabase.LoadAllAssetsAtPath(OstinatoModelAssetPath)
                .OfType<AnimationClip>()
                .Where(clip => !clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            report.AppendLine("ClipCount=" + clips.Length);
            foreach (var clip in clips)
            {
                report.AppendLine(
                    $"Clip={clip.name},Length={clip.length:0.######},FrameRate={clip.frameRate:0.######},Loop={clip.isLooping}");
            }
            report.AppendLine("SceneChanged=False");
            report.AppendLine("SelectionCleared=True");
            WriteTextReport(InspectionReportRelativePath, report.ToString());
            CaptureSourceOrientations(modelAsset, scene, sceneWasDirty);
            Selection.activeObject = null;
            if (scene.isDirty != sceneWasDirty)
            {
                throw new InvalidOperationException("Ostinato placement inspection changed scene dirty state.");
            }
            Debug.Log(
                $"OstinatoPlacementTargetInspected ZSpacing={zSpacing:0.###}, XSpacing={xSpacing:0.###}, " +
                $"Renderers={assetRenderers.Length}, ExistingRoot={existingOstinato != null}, " +
                "SceneChanged=False, SelectionCleared=True");
        }

        [MenuItem("Bellerophon/Enemies/Ostinato/Apply Placement")]
        public static void ApplyOstinatoPlacement()
        {
            var scene = RequireOpenCargoRunScene();
            var longaRoot = RequireRoot(scene, LongaRootName);
            var tergoRoot = RequireRoot(scene, TergoRootName);
            var smorzandoRoot = RequireRoot(scene, SmorzandoRootName);
            var smorzandoStatic = smorzandoRoot.transform.Find(SmorzandoStaticSlotName) ??
                throw new InvalidOperationException("Smorzando installed static review object is missing.");
            var player = RequireRoot(scene, PlayerRootName);
            var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(OstinatoModelAssetPath) ??
                throw new InvalidOperationException("Ostinato FBX has not been imported.");
            if (modelAsset.GetComponentsInChildren<Renderer>(true).Length == 0)
            {
                throw new InvalidOperationException("Ostinato FBX contains no renderers.");
            }

            var zSpacing = CalculateLongaTergoSpacing(longaRoot.transform, tergoRoot.transform);
            if (zSpacing <= 0.1f)
            {
                throw new InvalidOperationException("Longa Arma-Tergo spacing is not usable.");
            }
            var xSpacing = CalculateSmorzandoPersonXSpacing(smorzandoRoot.transform);
            var firstPosition = smorzandoStatic.position + Vector3.back * zSpacing;
            var existingRoot = scene.GetRootGameObjects()
                .FirstOrDefault(root => root.name == PlacementRootName);
            var preservedRoots = scene.GetRootGameObjects()
                .Where(root => root != existingRoot && root != player)
                .Select(root => new RootSnapshot(root))
                .ToArray();
            var preservedPlayerChildren = player.GetComponentsInChildren<Transform>(true)
                .Where(target => target != player.transform)
                .Select(target => new LocalTransformSnapshot(target))
                .ToArray();
            if (existingRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(existingRoot);
            }

            var placementRoot = new GameObject(PlacementRootName);
            SceneManager.MoveGameObjectToScene(placementRoot, scene);
            placementRoot.transform.SetPositionAndRotation(firstPosition, Quaternion.identity);
            placementRoot.transform.localScale = Vector3.one;
            for (var index = 0; index < PlacementCount; index++)
            {
                var slot = new GameObject(SlotPrefix + (index + 1).ToString("00") + "_Static_Review");
                slot.transform.SetParent(placementRoot.transform, false);
                slot.transform.localPosition = Vector3.right * (xSpacing * index);
                slot.transform.localRotation = Quaternion.Euler(0f, PlacementFacingYawDegrees, 0f);
                slot.transform.localScale = Vector3.one;
                var model = PrefabUtility.InstantiatePrefab(modelAsset, scene) as GameObject ??
                    UnityEngine.Object.Instantiate(modelAsset);
                model.name = ModelChildName;
                model.transform.SetParent(slot.transform, false);
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;
                model.transform.localScale = Vector3.one;
                foreach (var skinned in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    skinned.updateWhenOffscreen = true;
                }
                var animator = model.GetComponent<Animator>();
                if (animator != null)
                {
                    animator.applyRootMotion = false;
                }
                var initialBounds = CalculateRendererBounds(
                    model.transform,
                    new Bounds(model.transform.position, Vector3.zero));
                model.transform.position += Vector3.up * (slot.transform.position.y - initialBounds.min.y);
            }

            var rowBounds = CalculateRendererBounds(
                placementRoot.transform,
                new Bounds(placementRoot.transform.position, Vector3.one));
            var mainCamera = player.GetComponentInChildren<Camera>(true) ??
                throw new InvalidOperationException("Player Main Camera is missing.");
            var distance = CalculatePlayerFrontDistance(rowBounds, mainCamera.fieldOfView, 16f / 9f);
            var rowCenter = new Vector3(rowBounds.center.x, 0f, rowBounds.center.z);
            var playerPosition = new Vector3(rowBounds.center.x, 0f, rowBounds.min.z - distance);
            var horizontalLook = rowCenter - playerPosition;
            horizontalLook.y = 0f;
            player.transform.SetPositionAndRotation(
                playerPosition,
                Quaternion.LookRotation(horizontalLook.normalized, Vector3.up));

            EditorUtility.SetDirty(placementRoot);
            EditorUtility.SetDirty(player.transform);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException("CargoRunMvp could not be saved after Ostinato placement.");
            }

            foreach (var snapshot in preservedRoots)
            {
                snapshot.AssertUnchanged();
            }
            foreach (var snapshot in preservedPlayerChildren)
            {
                snapshot.AssertUnchanged();
            }
            AssertAppliedPlacement(placementRoot.transform, player.transform, xSpacing, firstPosition);
            var report = new StringBuilder();
            report.AppendLine("TargetRoot=" + PlacementRootName);
            report.AppendLine("Anchor=Approved Smorzando Enemy Placement/Smorzando_Installed_01");
            report.AppendLine("SourceAsset=" + OstinatoModelAssetPath);
            report.AppendLine("SourceSha256=" + ComputeSha256(ProjectAbsolutePath(OstinatoSourceRelativePath)));
            report.AppendLine("ImportedSha256=" + ComputeSha256(ProjectAbsolutePath(OstinatoModelAssetPath)));
            report.AppendLine("LongaTergoZSpacing=" + zSpacing.ToString("0.######"));
            report.AppendLine("SmorzandoPersonXSpacing=" + xSpacing.ToString("0.######"));
            report.AppendLine("PlacementCount=" + placementRoot.transform.childCount);
            report.AppendLine("FirstPosition=" + FormatVector(placementRoot.transform.GetChild(0).position));
            report.AppendLine("LastPosition=" + FormatVector(placementRoot.transform.GetChild(PlacementCount - 1).position));
            report.AppendLine("RowBoundsCenter=" + FormatVector(rowBounds.center));
            report.AppendLine("RowBoundsSize=" + FormatVector(rowBounds.size));
            report.AppendLine("SourceModelFrontAxis=+Z");
            report.AppendLine("PlacedModelFrontAxis=-Z");
            report.AppendLine("PlayerPosition=" + FormatVector(player.transform.position));
            report.AppendLine("PlayerEuler=" + FormatVector(player.transform.eulerAngles));
            report.AppendLine("PlayerFrontDistance=" + distance.ToString("0.######"));
            report.AppendLine("PlayerFacesRowCenter=True");
            report.AppendLine("PositiveXOrder=True");
            report.AppendLine("UniformXSpacing=True");
            report.AppendLine("UniformGroundAlignment=True");
            report.AppendLine("OtherSceneRootsChanged=False");
            report.AppendLine("PlayerChildLocalTransformsChanged=False");
            report.AppendLine("SelectionCleared=True");
            WriteTextReport(ApplyReportRelativePath, report.ToString());
            Selection.activeObject = null;
            Debug.Log(
                $"OstinatoPlacementApplied Count={PlacementCount}, XSpacing={xSpacing:0.###}, " +
                $"First={FormatVector(firstPosition)}, Player={FormatVector(playerPosition)}, " +
                "OtherSceneRootsChanged=False, SelectionCleared=True");
        }

        [MenuItem("Bellerophon/Enemies/Ostinato/Inspect Applied Grounding")]
        public static void InspectOstinatoAppliedGrounding()
        {
            var scene = RequireOpenCargoRunScene();
            var sceneWasDirty = scene.isDirty;
            var placementRoot = RequireRoot(scene, PlacementRootName);
            var report = new StringBuilder();
            report.AppendLine("PlacementRootPosition=" + FormatVector(placementRoot.transform.position));
            report.AppendLine("SlotCount=" + placementRoot.transform.childCount);
            for (var index = 0; index < placementRoot.transform.childCount; index++)
            {
                var slot = placementRoot.transform.GetChild(index);
                var model = slot.Find(ModelChildName) ??
                    throw new InvalidOperationException(slot.name + " is missing its Ostinato model.");
                var bounds = CalculateRendererBounds(model, new Bounds(model.position, Vector3.zero));
                report.AppendLine(
                    $"Slot[{index}]={slot.name},Position={FormatVector(slot.position)}," +
                    $"ModelLocalPosition={FormatVector(model.localPosition)}," +
                    $"BoundsMinY={bounds.min.y:0.######},GroundGap={bounds.min.y - slot.position.y:0.######}," +
                    $"BoundsCenter={FormatVector(bounds.center)},BoundsSize={FormatVector(bounds.size)}");
            }
            report.AppendLine("SceneChanged=False");
            report.AppendLine("SelectionCleared=True");
            WriteTextReport(
                ValidationRelativeFolder + "/Ostinato_AppliedGroundingInspection.txt",
                report.ToString());
            Selection.activeObject = null;
            if (scene.isDirty != sceneWasDirty)
            {
                throw new InvalidOperationException("Ostinato grounding inspection changed scene dirty state.");
            }
            Debug.Log("OstinatoAppliedGroundingInspected SceneChanged=False, SelectionCleared=True");
        }

        [MenuItem("Bellerophon/Enemies/Ostinato/Capture Placement Frames")]
        public static void CaptureOstinatoPlacementFrames()
        {
            var scene = RequireOpenCargoRunScene();
            var sceneWasDirty = scene.isDirty;
            var placementRoot = RequireRoot(scene, PlacementRootName);
            var player = RequireRoot(scene, PlayerRootName);
            var mainCamera = player.GetComponentInChildren<Camera>(true) ??
                throw new InvalidOperationException("Player Main Camera is missing.");
            var zSpacing = CalculateLongaTergoSpacing(
                RequireRoot(scene, LongaRootName).transform,
                RequireRoot(scene, TergoRootName).transform);
            var smorzandoRoot = RequireRoot(scene, SmorzandoRootName).transform;
            var xSpacing = CalculateSmorzandoPersonXSpacing(smorzandoRoot);
            var firstPosition = smorzandoRoot
                .Find(SmorzandoStaticSlotName)?.position + Vector3.back * zSpacing ??
                throw new InvalidOperationException("Smorzando installed static review object is missing.");
            AssertAppliedPlacement(placementRoot.transform, player.transform, xSpacing, firstPosition);
            var folder = ProjectAbsolutePath(CaptureRelativeFolder);
            Directory.CreateDirectory(folder);
            var playerViewPath = Path.Combine(folder, "Ostinato_PlayerStart_View.png");
            var isolatedViewPath = Path.Combine(folder, "Ostinato_Row_Front.png");
            CaptureExistingCamera(mainCamera, 1280, 720, playerViewPath);

            var clone = UnityEngine.Object.Instantiate(placementRoot);
            clone.name = "Ostinato_Placement_CaptureClone";
            clone.hideFlags = HideFlags.HideAndDontSave;
            var cameraObject = new GameObject("Ostinato_Placement_CaptureCamera")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            var lightObject = new GameObject("Ostinato_Placement_CaptureLight")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            GameObject floor = null;
            Material floorMaterial = null;
            try
            {
                clone.transform.position = Vector3.zero;
                clone.transform.rotation = Quaternion.identity;
                SetCaptureOnly(clone);
                foreach (var skinned in clone.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    skinned.updateWhenOffscreen = true;
                }
                var bounds = CalculateRendererBounds(clone.transform, new Bounds(Vector3.zero, Vector3.one));
                var camera = cameraObject.AddComponent<Camera>();
                camera.cullingMask = 1 << CaptureLayer;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.018f, 0.022f, 0.018f, 1f);
                camera.fieldOfView = 34f;
                camera.nearClipPlane = 0.03f;
                camera.farClipPlane = 1000f;
                var light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 4.8f;
                light.color = new Color(1f, 0.90f, 0.75f, 1f);
                light.cullingMask = 1 << CaptureLayer;
                light.shadows = LightShadows.None;
                lightObject.transform.rotation = Quaternion.Euler(35f, 12f, 0f);
                floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
                floor.name = "Ostinato_Placement_CaptureFloor";
                floor.hideFlags = HideFlags.HideAndDontSave;
                floor.layer = CaptureLayer;
                floor.transform.position = new Vector3(bounds.center.x, bounds.min.y - 0.025f, bounds.center.z);
                floor.transform.localScale = new Vector3(bounds.size.x + 4f, 0.05f, 5f);
                var collider = floor.GetComponent<Collider>();
                if (collider != null)
                {
                    UnityEngine.Object.DestroyImmediate(collider);
                }
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                floorMaterial = new Material(shader)
                {
                    hideFlags = HideFlags.HideAndDontSave,
                    color = new Color(0.09f, 0.11f, 0.09f, 1f)
                };
                floor.GetComponent<MeshRenderer>().sharedMaterial = floorMaterial;
                var distance = CalculatePlayerFrontDistance(bounds, camera.fieldOfView, 16f / 9f);
                CapturePng(
                    camera,
                    new Vector3(bounds.center.x, bounds.center.y + 0.12f, bounds.min.z - distance),
                    bounds.center,
                    1280,
                    720,
                    isolatedViewPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(clone);
                UnityEngine.Object.DestroyImmediate(floor);
                UnityEngine.Object.DestroyImmediate(floorMaterial);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(lightObject);
                Selection.activeObject = null;
            }

            File.WriteAllLines(
                Path.Combine(folder, "Ostinato_PlacementCaptureManifest.txt"),
                new[]
                {
                    "PlacementCount=" + PlacementCount,
                    "LongaTergoZSpacing=" + zSpacing.ToString("0.######"),
                    "SmorzandoPersonXSpacing=" + xSpacing.ToString("0.######"),
                    "FirstPosition=" + FormatVector(firstPosition),
                    "PlayerPosition=" + FormatVector(player.transform.position),
                    "PlayerForward=" + FormatVector(player.transform.forward),
                    "PlayerFacesRowCenter=True",
                    "SourceModelFrontAxis=+Z",
                    "PlacedModelFrontAxis=-Z",
                    "PositiveXOrder=True",
                    "Views=PlayerStart|IsolatedFrontRow",
                    "SceneViewFocused=False",
                    "SceneSaved=False",
                    "SelectionCleared=True"
                });
            if (scene.isDirty != sceneWasDirty)
            {
                throw new InvalidOperationException("Ostinato placement capture changed scene dirty state.");
            }
            Debug.Log(
                $"OstinatoPlacementFramesCaptured Count={PlacementCount}, Views=PlayerStart|IsolatedFrontRow, " +
                "SceneViewFocused=False, SceneSaved=False, SelectionCleared=True");
        }

        private static void AssertAppliedPlacement(
            Transform placementRoot,
            Transform player,
            float xSpacing,
            Vector3 expectedFirstPosition)
        {
            if (placementRoot.childCount != PlacementCount)
            {
                throw new InvalidOperationException(
                    $"Ostinato placement must contain exactly {PlacementCount} slots.");
            }
            for (var index = 0; index < PlacementCount; index++)
            {
                var slot = placementRoot.GetChild(index);
                var expected = expectedFirstPosition + Vector3.right * (xSpacing * index);
                if (Vector3.Distance(slot.position, expected) > 0.001f)
                {
                    throw new InvalidOperationException("Ostinato slot position does not match approved spacing.");
                }
                var model = slot.Find(ModelChildName) ??
                    throw new InvalidOperationException(slot.name + " is missing its Ostinato model.");
                var bounds = CalculateRendererBounds(model, new Bounds(model.position, Vector3.zero));
                if (model.GetComponentsInChildren<Renderer>(true).Length == 0 ||
                    Mathf.Abs(bounds.min.y - expectedFirstPosition.y) > 0.02f)
                {
                    throw new InvalidOperationException(slot.name + " is not visibly grounded.");
                }
            }
            var rowBounds = CalculateRendererBounds(
                placementRoot,
                new Bounds(placementRoot.position, Vector3.one));
            var expectedCenterX = rowBounds.center.x;
            if (Mathf.Abs(player.position.x - expectedCenterX) > 0.001f ||
                player.position.z >= rowBounds.min.z ||
                Vector3.Dot(player.forward, Vector3.forward) < 0.999f)
            {
                throw new InvalidOperationException("Player start is not centered in front of the Ostinato row.");
            }
        }

        private static float CalculatePlayerFrontDistance(Bounds bounds, float verticalFieldOfView, float aspect)
        {
            var verticalRadians = verticalFieldOfView * Mathf.Deg2Rad;
            var horizontalRadians = 2f * Mathf.Atan(Mathf.Tan(verticalRadians * 0.5f) * aspect);
            var widthDistance = bounds.extents.x / Mathf.Max(Mathf.Tan(horizontalRadians * 0.5f), 0.01f);
            return Mathf.Max(widthDistance + 3f, 8f);
        }

        private static void CaptureExistingCamera(Camera camera, int width, int height, string path)
        {
            var renderTexture = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            try
            {
                camera.targetTexture = renderTexture;
                camera.Render();
                RenderTexture.active = renderTexture;
                var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                try
                {
                    texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                    texture.Apply();
                    File.WriteAllBytes(path, texture.EncodeToPNG());
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(texture);
                }
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(renderTexture);
            }
        }

        private static void CaptureSourceOrientations(GameObject modelAsset, Scene scene, bool sceneWasDirty)
        {
            var folder = ProjectAbsolutePath(OrientationRelativeFolder);
            Directory.CreateDirectory(folder);
            var clone = PrefabUtility.InstantiatePrefab(modelAsset, scene) as GameObject ??
                UnityEngine.Object.Instantiate(modelAsset);
            clone.name = "Ostinato_SourceOrientation_Clone";
            clone.hideFlags = HideFlags.HideAndDontSave;
            clone.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            clone.transform.localScale = Vector3.one;
            var cameraObject = new GameObject("Ostinato_SourceOrientation_Camera")
            {
                hideFlags = HideFlags.HideAndDontSave,
                layer = CaptureLayer
            };
            var lightObject = new GameObject("Ostinato_SourceOrientation_Light")
            {
                hideFlags = HideFlags.HideAndDontSave,
                layer = CaptureLayer
            };
            Material floorMaterial = null;
            GameObject floor = null;
            try
            {
                SetCaptureOnly(clone);
                foreach (var renderer in clone.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    renderer.updateWhenOffscreen = true;
                }
                var bounds = CalculateRendererBounds(clone.transform, new Bounds(Vector3.zero, Vector3.one));
                var camera = cameraObject.AddComponent<Camera>();
                camera.cullingMask = 1 << CaptureLayer;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.025f, 0.03f, 0.025f, 1f);
                camera.fieldOfView = 34f;
                camera.nearClipPlane = 0.03f;
                camera.farClipPlane = 1000f;
                var light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 3.2f;
                light.color = new Color(1f, 0.92f, 0.78f, 1f);
                light.cullingMask = 1 << CaptureLayer;
                light.shadows = LightShadows.None;
                lightObject.transform.rotation = Quaternion.Euler(38f, -32f, 0f);
                floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
                floor.name = "Ostinato_SourceOrientation_Floor";
                floor.hideFlags = HideFlags.HideAndDontSave;
                floor.layer = CaptureLayer;
                floor.transform.position = new Vector3(bounds.center.x, bounds.min.y - 0.025f, bounds.center.z);
                floor.transform.localScale = new Vector3(
                    Mathf.Max(bounds.size.x + 2f, 4f), 0.05f, Mathf.Max(bounds.size.z + 2f, 4f));
                var collider = floor.GetComponent<Collider>();
                if (collider != null)
                {
                    UnityEngine.Object.DestroyImmediate(collider);
                }
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                floorMaterial = new Material(shader)
                {
                    hideFlags = HideFlags.HideAndDontSave,
                    color = new Color(0.10f, 0.12f, 0.10f, 1f)
                };
                floor.GetComponent<MeshRenderer>().sharedMaterial = floorMaterial;
                var distance = Mathf.Max(bounds.extents.magnitude * 4.5f, 3f);
                var target = bounds.center;
                var views = new[]
                {
                    ("CameraPositiveZ", Vector3.forward),
                    ("CameraNegativeZ", Vector3.back),
                    ("CameraPositiveX", Vector3.right),
                    ("CameraNegativeX", Vector3.left)
                };
                foreach (var view in views)
                {
                    CapturePng(
                        camera,
                        target + view.Item2 * distance + Vector3.up * bounds.extents.y * 0.08f,
                        target,
                        640,
                        640,
                        Path.Combine(folder, "Ostinato_Source_" + view.Item1 + ".png"));
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(clone);
                UnityEngine.Object.DestroyImmediate(floor);
                UnityEngine.Object.DestroyImmediate(floorMaterial);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(lightObject);
                Selection.activeObject = null;
                if (scene.isDirty != sceneWasDirty)
                {
                    throw new InvalidOperationException("Ostinato source orientation capture changed scene dirty state.");
                }
            }
        }

        private static void CapturePng(
            Camera camera,
            Vector3 cameraPosition,
            Vector3 target,
            int width,
            int height,
            string path)
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
                var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                try
                {
                    texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                    texture.Apply();
                    File.WriteAllBytes(path, texture.EncodeToPNG());
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(texture);
                }
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(renderTexture);
            }
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

        private static float CalculateLongaTergoSpacing(Transform longa, Transform tergo)
        {
            var zSpacing = Mathf.Abs(longa.position.z - tergo.position.z);
            return zSpacing > 0.1f ? zSpacing : Vector3.Distance(longa.position, tergo.position);
        }

        private static float CalculateSmorzandoPersonXSpacing(Transform smorzandoRoot)
        {
            var first = smorzandoRoot.Find(SmorzandoPersonFirstSlotName) ??
                throw new InvalidOperationException("Smorzando person 01 review object is missing.");
            var second = smorzandoRoot.Find(SmorzandoPersonSecondSlotName) ??
                throw new InvalidOperationException("Smorzando person 02 review object is missing.");
            var xSpacing = Mathf.Abs(second.position.x - first.position.x);
            if (xSpacing <= 0.1f)
            {
                throw new InvalidOperationException("Smorzando person 01-02 X spacing is not usable.");
            }

            return xSpacing;
        }

        private static Bounds CalculateRendererBounds(Transform root, Bounds fallback)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.enabled && renderer.gameObject.activeInHierarchy)
                .ToArray();
            if (renderers.Length == 0)
            {
                return fallback;
            }
            var bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }
            return bounds;
        }

        private static Mesh RendererMesh(Renderer renderer)
        {
            if (renderer is SkinnedMeshRenderer skinned)
            {
                return skinned.sharedMesh;
            }
            var filter = renderer.GetComponent<MeshFilter>();
            return filter != null ? filter.sharedMesh : null;
        }

        private static void SetCaptureOnly(GameObject root)
        {
            foreach (var target in root.GetComponentsInChildren<Transform>(true))
            {
                target.gameObject.layer = CaptureLayer;
                target.gameObject.hideFlags = HideFlags.HideAndDontSave;
            }
        }

        private static string RelativePath(Transform root, Transform target)
        {
            if (target == root)
            {
                return string.Empty;
            }
            var parts = new List<string>();
            for (var current = target; current != null && current != root; current = current.parent)
            {
                parts.Add(current.name);
            }
            parts.Reverse();
            return string.Join("/", parts);
        }

        private static string ComputeSha256(string path)
        {
            using var stream = File.OpenRead(path);
            using var sha256 = SHA256.Create();
            return BitConverter.ToString(sha256.ComputeHash(stream)).Replace("-", string.Empty);
        }

        private static void WriteTextReport(string relativePath, string contents)
        {
            var path = ProjectAbsolutePath(relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ProjectAbsolutePath("docs/validation"));
            File.WriteAllText(path, contents, new UTF8Encoding(false));
        }

        private static string ProjectAbsolutePath(string relativePath)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", relativePath));
        }

        private static string FormatVector(Vector3 value)
        {
            return $"({value.x:0.######},{value.y:0.######},{value.z:0.######})";
        }

        private static string MaterialName(Material material)
        {
            return material != null ? material.name : "None";
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
                if (target == null || target.transform.position != position ||
                    target.transform.rotation != rotation || target.transform.localScale != scale ||
                    target.activeSelf != activeSelf || target.transform.childCount != childCount)
                {
                    throw new InvalidOperationException("Ostinato placement changed an unapproved scene root.");
                }
            }
        }

        private readonly struct LocalTransformSnapshot
        {
            private readonly Transform target;
            private readonly Vector3 position;
            private readonly Quaternion rotation;
            private readonly Vector3 scale;

            public LocalTransformSnapshot(Transform target)
            {
                this.target = target;
                position = target.localPosition;
                rotation = target.localRotation;
                scale = target.localScale;
            }

            public void AssertUnchanged()
            {
                if (target == null || target.localPosition != position ||
                    target.localRotation != rotation || target.localScale != scale)
                {
                    throw new InvalidOperationException("Ostinato placement changed a Player child Transform.");
                }
            }
        }
    }
}
