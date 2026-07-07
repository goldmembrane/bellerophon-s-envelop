using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.SocietasCargoRunScene
{
    internal static class SocietasCargoRunSceneApplyAndReview
    {
        private const string CargoRunScenePath = "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string TergoPlacementRootName = "Approved Tergo Enemy Placement";
        private const string LongaArmaPlacementRootName = "Approved Longa Arma Enemy Placement";
        private const string UrzerePlacementRootName = "Approved Urzere Enemy Placement";
        private const string PlacementRootName = "Approved Societas Enemy Placement";
        private const string PlacementObjectName = "Societas_00_Static_Review";
        private const string ModelChildName = "SocietasPrepared_Model";
        private const string ReviewCameraName = "Model Cam";
        private const string PlayerRootName = "Player";

        private const string SourceModelAbsolutePath = "D:/Bellerophon2/Bellerophon/enemies model/societas.glb";
        private const string SocietasArtRoot = "Assets/_Project/Art/Enemies/Societas";
        private const string UnityModelFolder = SocietasArtRoot + "/Models";
        private const string UnityMaterialFolder = SocietasArtRoot + "/Materials";
        private const string UnityModelAssetPath = UnityModelFolder + "/societas.glb";
        private const string UnityMaterialAssetPath = UnityMaterialFolder + "/M_Societas_Glossy_Green_Body.mat";
        private const string ValidationFolder = "docs/validation/societas_static";

        private const float SocietasTargetHeightMeters = 0.30f;
        private const float SocietasFacingYawDegrees = 180f;
        private const float FallbackTergoLongaSpacing = 4.00f;
        private const float ReviewCameraMinimumFrontDistance = 1.85f;
        private const float ReviewCameraMaximumFrontDistance = 4.25f;
        private const float ReviewPlayerFrontDistance = 2.50f;
        private static readonly Color SocietasGlossyGreenColor = new(0.03f, 0.32f, 0.17f, 1f);

        [MenuItem("Bellerophon/Enemies/Societas/Apply Prepared Model To CargoRunMvp")]
        public static void ApplyPreparedModelToCurrentCargoRunScene()
        {
            RequirePreparedModelFile();
            EnsureUnityFolders();
            CopyPreparedModelAsset();
            ConfigureImportedModelAsset();

            var modelAsset = LoadPreparedModelAsset();
            var material = EnsureReferenceMaterial();
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = PlacePreparedModel(modelAsset, material, scene);
            ConfigureReviewCamera(placementRoot.transform);
            ConfigurePlayerStart(placementRoot.transform);
            InspectSceneState(placementRoot.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            Debug.Log("Prepared Societas model applied to CargoRunMvp scene.");
        }

        public static void InspectAppliedSceneState()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing.");
            }

            InspectSceneState(placementRoot.transform);
            Debug.Log("Prepared Societas CargoRunMvp scene state inspected.");
        }

        public static void MovePlayerStartToOppositeSide()
        {
            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing.");
            }

            MoveExistingPlayerStartToOppositeSide(placementRoot.transform);
            InspectSceneState(placementRoot.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Prepared Societas player start moved to the opposite side.");
        }

        public static void CaptureReview()
        {
            EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            var placementRoot = GameObject.Find(PlacementRootName);
            if (placementRoot == null)
            {
                throw new InvalidOperationException($"{PlacementRootName} root is missing.");
            }

            InspectSceneState(placementRoot.transform);
            var outputDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ValidationFolder));
            Directory.CreateDirectory(outputDirectory);

            var focus = FindSocietasCameraFocus(placementRoot.transform);
            var bounds = CalculateRendererBounds(focus, new Bounds(focus.position, Vector3.one));
            var cameraObject = new GameObject("SocietasStatic_CaptureCamera");
            var lightObject = new GameObject("SocietasStatic_CaptureLight");
            Texture2D texture = null;
            var outputPath = Path.Combine(outputDirectory, "Societas_00_Static_Review.png");

            try
            {
                var camera = cameraObject.AddComponent<Camera>();
                ConfigureCaptureCamera(camera, focus, bounds);

                var light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.25f;
                light.transform.rotation = Quaternion.Euler(44f, focus.eulerAngles.y - 32f, 0f);

                texture = CaptureCameraTexture(camera, 1400, 900);
                File.WriteAllBytes(outputPath, texture.EncodeToPNG());
            }
            finally
            {
                if (texture != null)
                {
                    UnityEngine.Object.DestroyImmediate(texture);
                }

                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(lightObject);
            }

            Debug.Log("SocietasStaticCapture Path=" + outputPath);
        }

        private static void RequirePreparedModelFile()
        {
            if (!File.Exists(SourceModelAbsolutePath))
            {
                throw new FileNotFoundException("Prepared Societas GLB model is missing.", SourceModelAbsolutePath);
            }
        }

        private static void EnsureUnityFolders()
        {
            EnsureUnityFolder(SocietasArtRoot);
            EnsureUnityFolder(UnityModelFolder);
            EnsureUnityFolder(UnityMaterialFolder);
        }

        private static void CopyPreparedModelAsset()
        {
            CopyFileToAsset(SourceModelAbsolutePath, UnityModelAssetPath);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(UnityModelAssetPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        }

        private static void ConfigureImportedModelAsset()
        {
            var modelImporter = AssetImporter.GetAtPath(UnityModelAssetPath) as ModelImporter;
            if (modelImporter == null)
            {
                return;
            }

            modelImporter.importCameras = false;
            modelImporter.importLights = false;
            modelImporter.importBlendShapes = true;
            modelImporter.importAnimation = true;
            modelImporter.importVisibility = false;
            modelImporter.animationType = ModelImporterAnimationType.Generic;
            modelImporter.animationCompression = ModelImporterAnimationCompression.Off;
            modelImporter.importNormals = ModelImporterNormals.Import;
            modelImporter.importTangents = ModelImporterTangents.CalculateMikk;
            modelImporter.globalScale = 1f;
            modelImporter.SaveAndReimport();
        }

        private static GameObject LoadPreparedModelAsset()
        {
            var glbAsset = AssetDatabase.LoadAssetAtPath<GameObject>(UnityModelAssetPath);
            if (glbAsset != null)
            {
                return glbAsset;
            }

            throw new InvalidOperationException(
                $"Could not load Societas GLB as a Unity model asset. GLB path={UnityModelAssetPath}.");
        }

        private static Material EnsureReferenceMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(UnityMaterialAssetPath);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                material = new Material(shader)
                {
                    name = "M_Societas_Glossy_Green_Body"
                };
                AssetDatabase.CreateAsset(material, UnityMaterialAssetPath);
            }

            SetMaterialColor(material, SocietasGlossyGreenColor);
            SetMaterialFloat(material, "_Smoothness", 0.88f);
            SetMaterialFloat(material, "_Glossiness", 0.88f);
            SetMaterialFloat(material, "_Metallic", 0f);
            if (material.HasProperty("_SpecColor"))
            {
                material.SetColor("_SpecColor", new Color(0.22f, 0.55f, 0.30f, 1f));
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject PlacePreparedModel(GameObject modelAsset, Material material, Scene scene)
        {
            var tergoRoot = RequireSceneRoot(TergoPlacementRootName);
            var longaRoot = RequireSceneRoot(LongaArmaPlacementRootName);
            var urzereRoot = RequireSceneRoot(UrzerePlacementRootName);
            var spacing = CalculateTergoLongaSpacing(tergoRoot.transform, longaRoot.transform);
            var placementPosition = new Vector3(
                urzereRoot.transform.position.x,
                urzereRoot.transform.position.y,
                urzereRoot.transform.position.z - spacing);

            var existingRoot = GameObject.Find(PlacementRootName);
            if (existingRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(existingRoot);
            }

            var placementRoot = new GameObject(PlacementRootName);
            SceneManager.MoveGameObjectToScene(placementRoot, scene);
            placementRoot.transform.position = placementPosition;
            placementRoot.transform.rotation = Quaternion.identity;
            placementRoot.transform.localScale = Vector3.one;

            var reviewRoot = new GameObject(PlacementObjectName);
            reviewRoot.transform.SetParent(placementRoot.transform, false);
            reviewRoot.transform.localPosition = Vector3.zero;
            reviewRoot.transform.localRotation = Quaternion.Euler(0f, SocietasFacingYawDegrees, 0f);
            reviewRoot.transform.localScale = Vector3.one;

            var modelInstance = PrefabUtility.InstantiatePrefab(modelAsset) as GameObject;
            if (modelInstance == null)
            {
                modelInstance = UnityEngine.Object.Instantiate(modelAsset);
            }

            modelInstance.name = ModelChildName;
            modelInstance.transform.SetParent(reviewRoot.transform, false);
            modelInstance.transform.localPosition = Vector3.zero;
            modelInstance.transform.localRotation = Quaternion.identity;
            modelInstance.transform.localScale = Vector3.one;

            DisableImportedAnimationPlayback(reviewRoot.transform);
            AssignMaterial(reviewRoot.transform, material);
            ScaleToTargetHeightAndAlignToGround(reviewRoot.transform, placementRoot.transform.position.y);

            EditorUtility.SetDirty(placementRoot);
            EditorUtility.SetDirty(reviewRoot);
            return placementRoot;
        }

        private static void DisableImportedAnimationPlayback(Transform root)
        {
            foreach (var animator in root.GetComponentsInChildren<Animator>(true))
            {
                animator.enabled = false;
                EditorUtility.SetDirty(animator);
            }

            foreach (var animation in root.GetComponentsInChildren<Animation>(true))
            {
                animation.enabled = false;
                EditorUtility.SetDirty(animation);
            }
        }

        private static void AssignMaterial(Transform root, Material material)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException("Societas prepared model contains no renderers.");
            }

            foreach (var renderer in renderers)
            {
                var materials = renderer.sharedMaterials;
                if (materials == null || materials.Length == 0)
                {
                    renderer.sharedMaterial = material;
                }
                else
                {
                    for (var i = 0; i < materials.Length; i++)
                    {
                        materials[i] = material;
                    }

                    renderer.sharedMaterials = materials;
                }

                EditorUtility.SetDirty(renderer);
            }
        }

        private static void ScaleToTargetHeightAndAlignToGround(Transform root, float groundY)
        {
            var bounds = CalculateRendererBounds(root, new Bounds(root.position, Vector3.one));
            if (bounds.size.y > 0.0001f)
            {
                var scaleFactor = Mathf.Clamp(SocietasTargetHeightMeters / bounds.size.y, 0.001f, 100f);
                root.localScale = Vector3.one * scaleFactor;
            }

            bounds = CalculateRendererBounds(root, new Bounds(root.position, Vector3.one));
            root.position += Vector3.up * (groundY - bounds.min.y);
        }

        private static void ConfigureReviewCamera(Transform placementRoot)
        {
            var focus = FindSocietasCameraFocus(placementRoot);
            var bounds = CalculateRendererBounds(focus, new Bounds(focus.position, Vector3.one));
            var camera = FindOrCreateReviewCamera();
            var frontDirection = CalculateSocietasVisualFrontDirection(focus);
            var lookAt = bounds.center + Vector3.up * Mathf.Clamp(bounds.extents.y * 0.12f, 0.03f, 0.12f);
            var distance = Mathf.Clamp(bounds.extents.magnitude * 4.25f, ReviewCameraMinimumFrontDistance, ReviewCameraMaximumFrontDistance);
            var verticalOffset = Mathf.Clamp(bounds.extents.y * 0.22f, 0.05f, 0.18f);
            var position = lookAt + frontDirection * distance + Vector3.up * verticalOffset;

            camera.transform.SetPositionAndRotation(position, Quaternion.LookRotation((lookAt - position).normalized, Vector3.up));
            camera.nearClipPlane = 0.02f;
            camera.farClipPlane = distance + Mathf.Max(bounds.extents.x, bounds.extents.z) + 12.00f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.11f, 0.12f, 0.12f, 1f);
            camera.orthographic = false;
            camera.fieldOfView = 32f;
            EditorUtility.SetDirty(camera);
            EditorUtility.SetDirty(camera.transform);

            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.LookAt(lookAt, camera.transform.rotation, distance, false, true);
            }
        }

        private static void ConfigurePlayerStart(Transform placementRoot)
        {
            var player = FindPlayerStartTransform();
            if (player == null)
            {
                throw new InvalidOperationException("Could not find Player start transform in CargoRunMvp scene.");
            }

            var focus = FindSocietasCameraFocus(placementRoot);
            var bounds = CalculateRendererBounds(focus, new Bounds(focus.position, Vector3.one));
            var lookAt = bounds.center + Vector3.up * Mathf.Clamp(bounds.extents.y * 0.10f, 0.03f, 0.12f);
            var frontDirection = CalculateSocietasVisualFrontDirection(focus);
            var startPosition = new Vector3(
                lookAt.x - frontDirection.x * ReviewPlayerFrontDistance,
                0f,
                lookAt.z - frontDirection.z * ReviewPlayerFrontDistance);

            player.SetPositionAndRotation(startPosition, CalculateYawRotationToward(startPosition, lookAt));
            EditorUtility.SetDirty(player);
        }

        private static void MoveExistingPlayerStartToOppositeSide(Transform placementRoot)
        {
            var player = FindPlayerStartTransform();
            if (player == null)
            {
                throw new InvalidOperationException("Could not find Player start transform in CargoRunMvp scene.");
            }

            var focus = FindSocietasCameraFocus(placementRoot);
            var bounds = CalculateRendererBounds(focus, new Bounds(focus.position, Vector3.one));
            var lookAt = bounds.center + Vector3.up * Mathf.Clamp(bounds.extents.y * 0.10f, 0.03f, 0.12f);
            var frontDirection = CalculateSocietasVisualFrontDirection(focus);
            var previousPosition = player.position;
            var offset = previousPosition - lookAt;
            offset.y = 0f;
            if (offset.sqrMagnitude < 0.001f)
            {
                offset = frontDirection * ReviewPlayerFrontDistance;
            }

            if (Vector3.Dot(offset.normalized, frontDirection.normalized) > -0.70f)
            {
                offset = -offset;
            }

            var startPosition = new Vector3(
                lookAt.x + offset.x,
                0f,
                lookAt.z + offset.z);

            player.SetPositionAndRotation(startPosition, CalculateYawRotationToward(startPosition, lookAt));
            EditorUtility.SetDirty(player);
            Debug.Log($"Societas player start opposite side update. Previous={previousPosition}, New={startPosition}, Center={lookAt}.");
        }

        private static void InspectSceneState(Transform placementRoot)
        {
            var tergoRoot = RequireSceneRoot(TergoPlacementRootName);
            var longaRoot = RequireSceneRoot(LongaArmaPlacementRootName);
            var urzereRoot = RequireSceneRoot(UrzerePlacementRootName);
            var spacing = CalculateTergoLongaSpacing(tergoRoot.transform, longaRoot.transform);
            var expectedPosition = new Vector3(
                urzereRoot.transform.position.x,
                urzereRoot.transform.position.y,
                urzereRoot.transform.position.z - spacing);

            if (Vector3.Distance(placementRoot.position, expectedPosition) > 0.05f)
            {
                throw new InvalidOperationException($"Societas placement root is not at the approved position. Expected={expectedPosition}, Actual={placementRoot.position}.");
            }

            var staticObject = placementRoot.Find(PlacementObjectName);
            if (staticObject == null)
            {
                throw new InvalidOperationException($"{PlacementObjectName} is missing under {PlacementRootName}.");
            }

            var model = staticObject.Find(ModelChildName);
            if (model == null)
            {
                throw new InvalidOperationException($"{ModelChildName} is missing under {PlacementObjectName}.");
            }

            var renderers = staticObject.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException("Societas prepared model contains no renderers.");
            }

            var bounds = CalculateRendererBounds(staticObject, new Bounds(staticObject.position, Vector3.one));
            if (Mathf.Abs(bounds.size.y - SocietasTargetHeightMeters) > 0.035f)
            {
                throw new InvalidOperationException($"Societas height must be close to {SocietasTargetHeightMeters:0.##}m. Actual={bounds.size.y:0.###}m.");
            }

            var camera = FindReviewCamera();
            if (camera == null)
            {
                throw new InvalidOperationException($"{ReviewCameraName} is missing.");
            }

            var lookAt = bounds.center + Vector3.up * Mathf.Clamp(bounds.extents.y * 0.10f, 0.03f, 0.12f);
            var frontDirection = CalculateSocietasVisualFrontDirection(staticObject);
            RequireFrontSideView(camera.transform.position, lookAt, frontDirection, ReviewCameraName);

            var player = FindPlayerStartTransform();
            if (player == null)
            {
                throw new InvalidOperationException("Could not find Player start transform in CargoRunMvp scene.");
            }

            RequireBackSideView(player.position, lookAt, frontDirection, PlayerRootName);
            RequireFacingTarget(player, lookAt, PlayerRootName);

            Debug.Log(
                $"SocietasSceneState Root={PlacementRootName}, Object={PlacementObjectName}, Model={UnityModelAssetPath}, Position={placementRoot.position}, TergoLongaSpacing={spacing:0.###}, BoundsSize={bounds.size}, RendererCount={renderers.Length}.");
        }

        private static void RequireFrontSideView(Vector3 viewerPosition, Vector3 lookAt, Vector3 frontDirection, string label)
        {
            var viewOffset = viewerPosition - lookAt;
            viewOffset.y = 0f;
            if (viewOffset.sqrMagnitude < 0.001f)
            {
                throw new InvalidOperationException($"{label} is too close to Societas center for front view validation.");
            }

            var dot = Vector3.Dot(viewOffset.normalized, frontDirection.normalized);
            if (dot < 0.70f)
            {
                throw new InvalidOperationException($"{label} must be on the Societas front side. Dot={dot:0.###}.");
            }
        }

        private static void RequireBackSideView(Vector3 viewerPosition, Vector3 lookAt, Vector3 frontDirection, string label)
        {
            var viewOffset = viewerPosition - lookAt;
            viewOffset.y = 0f;
            if (viewOffset.sqrMagnitude < 0.001f)
            {
                throw new InvalidOperationException($"{label} is too close to Societas center for opposite-side validation.");
            }

            var dot = Vector3.Dot(viewOffset.normalized, frontDirection.normalized);
            if (dot > -0.70f)
            {
                throw new InvalidOperationException($"{label} must be on the Societas opposite side. Dot={dot:0.###}.");
            }
        }

        private static void RequireFacingTarget(Transform viewer, Vector3 target, string label)
        {
            var toTarget = target - viewer.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.001f)
            {
                throw new InvalidOperationException($"{label} is too close to Societas center for facing validation.");
            }

            var forward = viewer.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.001f)
            {
                throw new InvalidOperationException($"{label} has no horizontal forward vector.");
            }

            var dot = Vector3.Dot(forward.normalized, toTarget.normalized);
            if (dot < 0.70f)
            {
                throw new InvalidOperationException($"{label} must face Societas after moving to the opposite side. Dot={dot:0.###}.");
            }
        }

        private static void ConfigureCaptureCamera(Camera camera, Transform focus, Bounds bounds)
        {
            var frontDirection = CalculateSocietasVisualFrontDirection(focus);
            var lookAt = bounds.center + Vector3.up * Mathf.Clamp(bounds.extents.y * 0.10f, 0.03f, 0.12f);
            var distance = Mathf.Clamp(bounds.extents.magnitude * 4.50f, ReviewCameraMinimumFrontDistance, ReviewCameraMaximumFrontDistance);
            var position = lookAt + frontDirection * distance + Vector3.up * Mathf.Clamp(bounds.extents.y * 0.18f, 0.05f, 0.18f);

            camera.transform.SetPositionAndRotation(position, Quaternion.LookRotation((lookAt - position).normalized, Vector3.up));
            camera.orthographic = true;
            camera.orthographicSize = Mathf.Max(bounds.extents.y * 2.85f, 0.48f);
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 30f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.075f, 0.08f, 0.085f, 1f);
        }

        private static Texture2D CaptureCameraTexture(Camera camera, int width, int height)
        {
            var renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            try
            {
                camera.targetTexture = renderTexture;
                RenderTexture.active = renderTexture;
                camera.Render();
                var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply();
                return texture;
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                renderTexture.Release();
                UnityEngine.Object.DestroyImmediate(renderTexture);
            }
        }

        private static Transform FindSocietasCameraFocus(Transform placementRoot)
        {
            return placementRoot.Find(PlacementObjectName) ?? placementRoot;
        }

        private static Vector3 CalculateSocietasVisualFrontDirection(Transform focus)
        {
            var yawRotation = Quaternion.Euler(0f, focus.eulerAngles.y, 0f);
            var frontDirection = yawRotation * Vector3.back;
            frontDirection.y = 0f;
            return frontDirection.sqrMagnitude > 0.001f ? frontDirection.normalized : Vector3.back;
        }

        private static Quaternion CalculateYawRotationToward(Vector3 position, Vector3 target)
        {
            var facing = target - position;
            facing.y = 0f;
            return facing.sqrMagnitude > 0.001f ? Quaternion.LookRotation(facing.normalized, Vector3.up) : Quaternion.identity;
        }

        private static Transform FindPlayerStartTransform()
        {
            var player = GameObject.Find(PlayerRootName);
            if (player != null)
            {
                return player.transform;
            }

            var characterController = UnityEngine.Object.FindFirstObjectByType<CharacterController>();
            return characterController != null ? characterController.transform : null;
        }

        private static Camera FindOrCreateReviewCamera()
        {
            var cameraObject = GameObject.Find(ReviewCameraName);
            if (cameraObject == null)
            {
                cameraObject = new GameObject(ReviewCameraName);
            }

            var camera = cameraObject.GetComponent<Camera>();
            if (camera == null)
            {
                camera = cameraObject.AddComponent<Camera>();
            }

            return camera;
        }

        private static Camera FindReviewCamera()
        {
            var cameraObject = GameObject.Find(ReviewCameraName);
            return cameraObject != null ? cameraObject.GetComponent<Camera>() : null;
        }

        private static GameObject RequireSceneRoot(string objectName)
        {
            var root = GameObject.Find(objectName);
            if (root == null)
            {
                throw new InvalidOperationException($"{objectName} is missing in CargoRunMvp scene.");
            }

            return root;
        }

        private static float CalculateTergoLongaSpacing(Transform tergoRoot, Transform longaRoot)
        {
            var zSpacing = Mathf.Abs(tergoRoot.position.z - longaRoot.position.z);
            if (zSpacing > 0.10f)
            {
                return zSpacing;
            }

            return Mathf.Max(Vector3.Distance(tergoRoot.position, longaRoot.position), FallbackTergoLongaSpacing);
        }

        private static Bounds CalculateRendererBounds(Transform root, Bounds fallback)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return fallback;
            }

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds;
        }

        private static void EnsureUnityFolder(string folderPath)
        {
            var parts = folderPath.Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }

        private static void CopyFileToAsset(string sourceAbsolutePath, string destinationAssetPath)
        {
            var destinationAbsolutePath = AssetPathToAbsolutePath(destinationAssetPath);
            var destinationDirectory = Path.GetDirectoryName(destinationAbsolutePath);
            if (!string.IsNullOrEmpty(destinationDirectory))
            {
                Directory.CreateDirectory(destinationDirectory);
            }

            File.Copy(sourceAbsolutePath, destinationAbsolutePath, true);
        }

        private static string AssetPathToAbsolutePath(string assetPath)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
        }

        private static void SetMaterialColor(Material material, Color color)
        {
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }
        }

        private static void SetMaterialFloat(Material material, string propertyName, float value)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetFloat(propertyName, value);
            }
        }
    }
}
