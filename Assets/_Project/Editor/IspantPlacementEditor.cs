using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.IspantCargoRunScene
{
    internal static class IspantPlacementEditor
    {
        private const string ScenePath =
            "Assets/_Project/Scenes/CargoRunMvp.unity";
        private const string SourcePath =
            "D:/Bellerophon2/Bellerophon/enemies model/išpant-armed.fbx";
        private const string ArtRoot =
            "Assets/_Project/Art/Enemies/Ispant";
        private const string ModelFolder = ArtRoot + "/Models";
        private const string ModelPath = ModelFolder + "/Ispant_Armed.fbx";
        private const string PlacementRootName =
            "Approved Ispant Enemy Placement";
        private const string KursaRootName =
            "Approved Kursa Enemy Placement";
        private const string LongaRootName =
            "Approved Longa Arma Enemy Placement";
        private const string TergoRootName =
            "Approved Tergo Enemy Placement";
        private const string PlayerName = "Player";
        private const string ModelName = "Ispant_Model";
        private const string ValidationFolder =
            "docs/validation/ispant_armed_placement_2026-08-04";
        private const string DiagnosticPathFormat =
            ValidationFolder + "/Ispant_Armed_Placement_Diagnostic_{0:00}.png";
        private const string FinalReviewPath =
            ValidationFolder + "/Ispant_Armed_Placement_FinalReview.png";
        private const string PlayerStartValidationFolder =
            "docs/validation/ispant_player_start_2026-08-04";
        private const string PlayerStartDiagnosticPathFormat =
            PlayerStartValidationFolder +
            "/Ispant_PlayerStart_Diagnostic_{0:00}.png";
        private const string PlayerStartFinalReviewPath =
            PlayerStartValidationFolder +
            "/Ispant_PlayerStart_FinalReview.png";
        private const string ExpectedSourceSha256 =
            "62043F0A84221A74F0B106AEA90112E04205B4C53F043C9A3B4CD629606CA55B";
        private const int SlotCount = 12;
        private const float TargetHeight = 1.8f;
        private const float FacingYaw = 180f;
        private const float Tolerance = 0.03f;
        private const float MinimumPlayerDistance = 2.5f;
        private const float CameraMargin = 0.8f;

        private static readonly string[] SlotNames =
        {
            "Ispant_01", "Ispant_02", "Ispant_03", "Ispant_04",
            "Ispant_05", "Ispant_06", "Ispant_07", "Ispant_08",
            "Ispant_09", "Ispant_10", "Ispant_11", "Ispant_12"
        };

        [MenuItem("Bellerophon/Enemies/Ispant/Apply Armed Placement")]
        public static void ApplyIspantArmedPlacement()
        {
            RequireSource();
            var scene = RequireCurrentScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp has unsaved editor changes. Save or discard them before applying Ispant placement.");
            }

            var sourceHash = Sha256(SourcePath);
            RequireSameHash(ExpectedSourceSha256, sourceHash);
            CopyAndImportModel();
            var importedHash = Sha256(Absolute(ModelPath));
            RequireSameHash(sourceHash, importedHash);

            var modelAsset =
                AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath) ??
                throw new InvalidOperationException(
                    "The imported Ispant FBX is unavailable.");
            RequireVisibleGeometry(modelAsset.transform);

            var protectedBefore = ProtectedRootSignatures(scene);
            var kursa = RequireRoot(KursaRootName).transform;
            var longa = RequireRoot(LongaRootName).transform;
            var tergo = RequireRoot(TergoRootName).transform;
            var zSpacing = LongaTergoSpacing(longa, tergo);
            var xSpacing = KursaSlotSpacing(kursa);

            var oldRoot = GameObject.Find(PlacementRootName);
            if (oldRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(oldRoot);
            }

            var root = new GameObject(PlacementRootName);
            SceneManager.MoveGameObjectToScene(root, scene);
            root.transform.SetPositionAndRotation(
                new Vector3(
                    kursa.position.x,
                    kursa.position.y,
                    kursa.position.z - zSpacing),
                Quaternion.identity);

            for (var index = 0; index < SlotCount; index++)
            {
                var slot = new GameObject(SlotNames[index]);
                slot.transform.SetParent(root.transform, false);
                slot.transform.localPosition =
                    new Vector3(index * xSpacing, 0f, 0f);
                slot.transform.localRotation =
                    Quaternion.Euler(0f, FacingYaw, 0f);

                var model =
                    PrefabUtility.InstantiatePrefab(modelAsset, scene)
                    as GameObject ??
                    throw new InvalidOperationException(
                        "The supplied Ispant FBX could not be instantiated.");
                model.name = ModelName;
                model.transform.SetParent(slot.transform, false);
                model.transform.SetLocalPositionAndRotation(
                    Vector3.zero,
                    Quaternion.identity);
                model.transform.localScale = Vector3.one;
                ConfigureStaticModel(model.transform);
                ScaleAndGround(model.transform, root.transform.position.y);
                EditorUtility.SetDirty(slot);
                EditorUtility.SetDirty(model);
            }

            var metrics = InspectState(scene, root.transform);
            var protectedAfter = ProtectedRootSignatures(scene);
            if (!protectedBefore.SequenceEqual(
                    protectedAfter,
                    StringComparer.Ordinal))
            {
                throw new InvalidOperationException(
                    "A scene root outside Ispant changed during placement.");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved after Ispant placement.");
            }

            AssetDatabase.SaveAssets();
            RequireSameHash(sourceHash, Sha256(SourcePath));
            RequireSameHash(importedHash, Sha256(Absolute(ModelPath)));
            Debug.Log(
                "IspantArmedPlacementApplied Result=PASS" +
                ", Slots=" + SlotCount +
                ", Position=" + Vec(metrics.Ispant) +
                ", KursaPosition=" + Vec(metrics.Kursa) +
                ", LongaTergoZSpacing=" + Num(metrics.ZSpacing) +
                ", KursaXSpacing=" + Num(metrics.XSpacing) +
                ", TargetHeight=" + Num(TargetHeight) +
                ", SourceSha256=" + sourceHash +
                ", DirectFbxInstances=" + SlotCount +
                ", AnimationApplied=False" +
                ", OtherSceneRootsUnchanged=True" +
                ", SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Inspect Armed Placement")]
        public static void InspectIspantArmedPlacement()
        {
            var scene = RequireCurrentScene();
            var wasDirty = scene.isDirty;
            var root = GameObject.Find(PlacementRootName) ??
                       throw new InvalidOperationException(
                           "The Ispant placement root is missing.");
            var metrics = InspectState(scene, root.transform);
            if (EditorUtility.scriptCompilationFailed)
            {
                throw new InvalidOperationException(
                    "Unity reports script compilation errors.");
            }

            var sourceHash = Sha256(SourcePath);
            var importedHash = Sha256(Absolute(ModelPath));
            RequireSameHash(ExpectedSourceSha256, sourceHash);
            RequireSameHash(sourceHash, importedHash);
            if (scene.isDirty != wasDirty)
            {
                throw new InvalidOperationException(
                    "Ispant placement inspection changed the scene dirty state.");
            }

            Debug.Log(
                "IspantArmedPlacementInspected Result=PASS" +
                ", Slots=" + SlotCount +
                ", Position=" + Vec(metrics.Ispant) +
                ", KursaPosition=" + Vec(metrics.Kursa) +
                ", LongaTergoZSpacing=" + Num(metrics.ZSpacing) +
                ", KursaXSpacing=" + Num(metrics.XSpacing) +
                ", TargetHeight=" + Num(TargetHeight) +
                ", LineupBounds=" + Vec(metrics.Bounds.size) +
                ", SourceSha256=" + sourceHash +
                ", DirectFbxInstances=" + SlotCount +
                ", SceneChanged=False.");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Apply Player Start Framing")]
        public static void ApplyIspantPlayerStartFraming()
        {
            var scene = RequireCurrentScene();
            if (scene.isDirty)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp has unsaved editor changes. Save or discard them before moving the Player start.");
            }

            var root = RequireRoot(PlacementRootName).transform;
            var protectedBefore = PlayerMoveProtectedRootSignatures(scene);
            ConfigurePlayer(root);
            InspectPlayerStart(root);
            var protectedAfter = PlayerMoveProtectedRootSignatures(scene);
            if (!protectedBefore.SequenceEqual(
                    protectedAfter,
                    StringComparer.Ordinal))
            {
                throw new InvalidOperationException(
                    "A scene root outside Player changed while framing Ispant.");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException(
                    "CargoRunMvp could not be saved after moving the Player start.");
            }

            var player = RequirePlayer();
            Debug.Log(
                "IspantPlayerStartFramingApplied Result=PASS" +
                ", Player=" + Vec(player.position) +
                ", PlayerForward=" + Vec(player.forward) +
                ", FullLineupVisible=True" +
                ", ExistingSceneRootsUnchanged=True" +
                ", SceneSaved=True.");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Capture Player Start Diagnostic")]
        public static void CaptureIspantPlayerStartDiagnostic()
        {
            CapturePlayerStartReview(
                NextPlayerStartDiagnosticPath(),
                "Diagnostic");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Capture Player Start Final Review")]
        public static void CaptureIspantPlayerStartFinalReview()
        {
            var destination = Absolute(PlayerStartFinalReviewPath);
            if (File.Exists(destination))
            {
                throw new InvalidOperationException(
                    "The one-time Ispant Player-start final review already exists: " +
                    PlayerStartFinalReviewPath + ".");
            }

            CapturePlayerStartReview(destination, "FinalReview");
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Capture Armed Placement Diagnostic")]
        public static void CaptureIspantArmedPlacementDiagnostic()
        {
            var destination = NextDiagnosticPath();
            var yaw = destination.EndsWith("_02.png", StringComparison.Ordinal)
                ? 35f
                : 0f;
            CapturePlacementReview(destination, "Diagnostic", yaw);
        }

        [MenuItem("Bellerophon/Enemies/Ispant/Capture Armed Placement Final Review")]
        public static void CaptureIspantArmedPlacementFinalReview()
        {
            var destination = Absolute(FinalReviewPath);
            if (File.Exists(destination))
                throw new InvalidOperationException(
                    "The one-time Ispant final review already exists: " +
                    FinalReviewPath + ".");
            CapturePlacementReview(destination, "FinalReview", 20f);
        }

        private static void ConfigurePlayer(Transform root)
        {
            var player = RequirePlayer();
            var camera =
                player.GetComponentInChildren<Camera>(true) ??
                throw new InvalidOperationException(
                    "The Player camera is missing.");
            var bounds = BoundsOf(
                root,
                new Bounds(root.position, Vector3.one));
            var front = root.GetChild(0).forward;
            front.y = 0f;
            if (front.sqrMagnitude < 0.001f)
            {
                throw new InvalidOperationException(
                    "The Ispant front direction is unusable.");
            }

            front.Normalize();
            var desiredCamera =
                bounds.center + front * PlayerDistance(bounds, camera);
            var yaw = YawToward(desiredCamera, bounds.center);
            var cameraOffsetLocal =
                player.InverseTransformPoint(camera.transform.position);
            var desiredPlayer = desiredCamera - yaw * cameraOffsetLocal;
            desiredPlayer.y = player.position.y;
            player.SetPositionAndRotation(desiredPlayer, yaw);
            EditorUtility.SetDirty(player);
        }

        private static void InspectPlayerStart(Transform root)
        {
            var player = RequirePlayer();
            var camera =
                player.GetComponentInChildren<Camera>(true) ??
                throw new InvalidOperationException(
                    "The Player camera is missing.");
            var bounds = BoundsOf(
                root,
                new Bounds(root.position, Vector3.one));
            var fromFocus = camera.transform.position - bounds.center;
            fromFocus.y = 0f;
            var front = root.GetChild(0).forward;
            front.y = 0f;
            var toFocus = bounds.center - camera.transform.position;
            toFocus.y = 0f;
            var cameraForward = camera.transform.forward;
            cameraForward.y = 0f;
            if (fromFocus.sqrMagnitude < 0.001f ||
                front.sqrMagnitude < 0.001f ||
                Vector3.Dot(fromFocus.normalized, front.normalized) < 0.98f ||
                toFocus.sqrMagnitude < 0.001f ||
                cameraForward.sqrMagnitude < 0.001f ||
                Vector3.Dot(
                    toFocus.normalized,
                    cameraForward.normalized) < 0.98f)
            {
                throw new InvalidOperationException(
                    "The Player camera is not centered in front of Ispant.");
            }

            var fullLineupVisible = Corners(bounds).All(corner =>
            {
                var view = camera.WorldToViewportPoint(corner);
                return view.z > 0f &&
                       view.x >= -0.02f && view.x <= 1.02f &&
                       view.y >= -0.02f && view.y <= 1.02f;
            });
            if (!fullLineupVisible)
            {
                throw new InvalidOperationException(
                    "The Player camera does not contain the complete Ispant lineup.");
            }

            foreach (Transform slot in root)
            {
                var slotBounds = BoundsOf(
                    slot,
                    new Bounds(slot.position, Vector3.one));
                var view = camera.WorldToViewportPoint(slotBounds.center);
                if (view.z <= 0f ||
                    view.x < 0f || view.x > 1f ||
                    view.y < 0f || view.y > 1f)
                {
                    throw new InvalidOperationException(
                        slot.name +
                        " center is outside the Player camera.");
                }
            }

            var playerToCenter = bounds.center - player.position;
            playerToCenter.y = 0f;
            var playerForward = player.forward;
            playerForward.y = 0f;
            if (playerToCenter.sqrMagnitude < 0.001f ||
                playerForward.sqrMagnitude < 0.001f ||
                Vector3.Dot(
                    playerToCenter.normalized,
                    playerForward.normalized) < 0.98f)
            {
                throw new InvalidOperationException(
                    "The Player root does not face the Ispant lineup.");
            }
        }

        private static float PlayerDistance(Bounds bounds, Camera camera)
        {
            var vertical = Mathf.Max(1f, camera.fieldOfView * 0.5f) *
                           Mathf.Deg2Rad;
            const float captureAspect = 16f / 9f;
            var horizontal =
                Mathf.Atan(Mathf.Tan(vertical) * captureAspect);
            return Mathf.Max(
                MinimumPlayerDistance,
                bounds.extents.x /
                Mathf.Max(0.01f, Mathf.Tan(horizontal)) + CameraMargin,
                bounds.extents.y /
                Mathf.Max(0.01f, Mathf.Tan(vertical)) + CameraMargin);
        }

        private static void CapturePlayerStartReview(
            string destination,
            string kind)
        {
            var scene = RequireCurrentScene();
            var wasDirty = scene.isDirty;
            var root = RequireRoot(PlacementRootName).transform;
            InspectPlayerStart(root);
            var camera =
                RequirePlayer().GetComponentInChildren<Camera>(true) ??
                throw new InvalidOperationException(
                    "The Player camera is missing.");
            Capture(camera, destination, 1920, 1080);
            if (scene.isDirty != wasDirty)
            {
                throw new InvalidOperationException(
                    "Ispant Player-start capture changed the scene dirty state.");
            }

            Debug.Log(
                "IspantPlayerStartReviewCaptured Kind=" + kind +
                ", Image=" + destination +
                ", CompleteLineupVisible=True" +
                ", DirectVisualReviewRequired=True" +
                ", SceneChanged=False.");
        }

        private static void Capture(
            Camera camera,
            string destination,
            int width,
            int height)
        {
            Directory.CreateDirectory(
                Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException(
                    "Invalid Ispant Player-start capture folder."));
            var oldActive = RenderTexture.active;
            var target = new RenderTexture(
                width,
                height,
                24,
                RenderTextureFormat.ARGB32);
            var image = new Texture2D(
                width,
                height,
                TextureFormat.RGB24,
                false);
            var reviewCameraObject = new GameObject(
                "IspantPlayerStartReviewCamera",
                typeof(Camera))
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            Camera reviewCamera = null;
            try
            {
                reviewCamera = reviewCameraObject.GetComponent<Camera>();
                reviewCamera.CopyFrom(camera);
                reviewCamera.transform.SetPositionAndRotation(
                    camera.transform.position,
                    camera.transform.rotation);
                reviewCamera.allowHDR = false;
                reviewCamera.targetTexture = target;
                reviewCamera.Render();
                RenderTexture.active = target;
                image.ReadPixels(
                    new Rect(0f, 0f, width, height),
                    0,
                    0);
                image.Apply();
                File.WriteAllBytes(destination, image.EncodeToPNG());
            }
            finally
            {
                if (reviewCamera != null)
                    reviewCamera.targetTexture = null;
                RenderTexture.active = oldActive;
                UnityEngine.Object.DestroyImmediate(image);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(reviewCameraObject);
            }
        }

        private static string NextPlayerStartDiagnosticPath()
        {
            for (var index = 1; index <= 2; index++)
            {
                var destination = Absolute(string.Format(
                    CultureInfo.InvariantCulture,
                    PlayerStartDiagnosticPathFormat,
                    index));
                if (!File.Exists(destination)) return destination;
            }

            throw new InvalidOperationException(
                "The approved Ispant Player-start diagnostic captures already exist.");
        }

        private static void CapturePlacementReview(
            string destination,
            string kind,
            float lineupYaw)
        {
            var scene = RequireCurrentScene();
            var wasDirty = scene.isDirty;
            var root = RequireRoot(PlacementRootName).transform;
            var metrics = InspectState(scene, root);
            CaptureComposite(scene, root, destination, lineupYaw);
            if (scene.isDirty != wasDirty)
                throw new InvalidOperationException(
                    "Ispant placement capture changed the scene dirty state.");
            Debug.Log(
                "IspantArmedPlacementReviewCaptured Kind=" + kind +
                ", Image=" + destination +
                ", Slots=" + SlotCount +
                ", Position=" + Vec(metrics.Ispant) +
                ", KursaPosition=" + Vec(metrics.Kursa) +
                ", LongaTergoZSpacing=" + Num(metrics.ZSpacing) +
                ", KursaXSpacing=" + Num(metrics.XSpacing) +
                ", DirectVisualReviewRequired=True, SceneChanged=False.");
        }

        private static void CaptureComposite(
            Scene scene,
            Transform ispant,
            string destination,
            float lineupYaw)
        {
            const int width = 1920;
            const int panelHeight = 540;
            var referenceRoots = new[]
            {
                RequireRoot(LongaRootName).transform,
                RequireRoot(TergoRootName).transform,
                RequireRoot(KursaRootName).transform,
                ispant
            };
            var sceneRenderers = scene.GetRootGameObjects()
                .SelectMany(item => item.GetComponentsInChildren<Renderer>(true))
                .ToArray();
            var rendererStates = sceneRenderers
                .Select(item => new RendererState(item))
                .ToArray();
            var sceneLightStates = scene.GetRootGameObjects()
                .SelectMany(item => item.GetComponentsInChildren<Light>(true))
                .Select(item => new LightState(item))
                .ToArray();
            var cameraObject = new GameObject(
                "IspantPlacementReviewCamera",
                typeof(Camera))
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            var keyLightObject = new GameObject(
                "IspantPlacementReviewKeyLight",
                typeof(Light))
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            var fillLightObject = new GameObject(
                "IspantPlacementReviewFillLight",
                typeof(Light))
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            var target = new RenderTexture(
                width,
                panelHeight,
                24,
                RenderTextureFormat.ARGB32);
            var panel = new Texture2D(
                width,
                panelHeight,
                TextureFormat.RGB24,
                false);
            var composite = new Texture2D(
                width,
                panelHeight * 2,
                TextureFormat.RGB24,
                false);
            var oldActive = RenderTexture.active;
            try
            {
                foreach (var state in sceneLightStates) state.Disable();
                var camera = cameraObject.GetComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.30f, 0.32f, 0.36f, 1f);
                camera.orthographic = true;
                camera.cullingMask = ~0;
                camera.targetTexture = target;

                var keyLight = keyLightObject.GetComponent<Light>();
                keyLight.type = LightType.Directional;
                keyLight.color = new Color(1f, 0.94f, 0.86f, 1f);
                keyLight.intensity = 1.6f;
                keyLight.shadows = LightShadows.None;
                keyLightObject.transform.rotation =
                    Quaternion.Euler(38f, -32f, 0f);

                var fillLight = fillLightObject.GetComponent<Light>();
                fillLight.type = LightType.Directional;
                fillLight.color = new Color(0.72f, 0.84f, 1f, 1f);
                fillLight.intensity = 0.9f;
                fillLight.shadows = LightShadows.None;
                fillLightObject.transform.rotation =
                    Quaternion.Euler(24f, 148f, 0f);

                SetVisibleRoots(sceneRenderers, referenceRoots);
                var overviewBounds = BoundsOfRenderers(
                    referenceRoots.SelectMany(root =>
                        root.GetComponentsInChildren<Renderer>(true)));
                RenderPanel(
                    camera,
                    target,
                    panel,
                    overviewBounds,
                    new Vector3(0.22f, 0.72f, 1f));
                composite.SetPixels(0, panelHeight, width, panelHeight, panel.GetPixels());

                SetVisibleRoots(sceneRenderers, new[] { ispant });
                var lineupBounds = BoundsOf(
                    ispant,
                    new Bounds(ispant.position, Vector3.one));
                var front = ispant.GetChild(0).forward;
                front = Quaternion.AngleAxis(lineupYaw, Vector3.up) * front;
                front += Vector3.up * 0.08f;
                RenderPanel(
                    camera,
                    target,
                    panel,
                    lineupBounds,
                    front);
                composite.SetPixels(0, 0, width, panelHeight, panel.GetPixels());
                composite.Apply();
                Directory.CreateDirectory(
                    Path.GetDirectoryName(destination) ??
                    throw new InvalidOperationException(
                        "Invalid Ispant validation folder."));
                File.WriteAllBytes(destination, composite.EncodeToPNG());
            }
            finally
            {
                foreach (var state in rendererStates) state.Restore();
                foreach (var state in sceneLightStates) state.Restore();
                RenderTexture.active = oldActive;
                cameraObject.GetComponent<Camera>().targetTexture = null;
                UnityEngine.Object.DestroyImmediate(panel);
                UnityEngine.Object.DestroyImmediate(composite);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(keyLightObject);
                UnityEngine.Object.DestroyImmediate(fillLightObject);
            }
        }

        private static void SetVisibleRoots(
            IEnumerable<Renderer> renderers,
            IReadOnlyCollection<Transform> visibleRoots)
        {
            foreach (var renderer in renderers)
                renderer.enabled = visibleRoots.Any(root =>
                    renderer.transform.IsChildOf(root));
        }

        private static Bounds BoundsOfRenderers(IEnumerable<Renderer> source)
        {
            var renderers = source.Where(item => item != null && item.enabled).ToArray();
            if (renderers.Length == 0)
                throw new InvalidOperationException(
                    "Placement overview contains no visible renderers.");
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers.Skip(1))
                bounds.Encapsulate(renderer.bounds);
            return bounds;
        }

        private static void RenderPanel(
            Camera camera,
            RenderTexture target,
            Texture2D panel,
            Bounds bounds,
            Vector3 viewDirection)
        {
            var direction = viewDirection.normalized;
            var distance = Mathf.Max(10f, bounds.extents.magnitude * 3f);
            camera.transform.position = bounds.center + direction * distance;
            camera.transform.LookAt(bounds.center, Vector3.up);
            var horizontalExtent = ProjectedHalfExtent(
                bounds.extents,
                camera.transform.right);
            var verticalExtent = ProjectedHalfExtent(
                bounds.extents,
                camera.transform.up);
            camera.orthographicSize = Mathf.Max(
                verticalExtent * 1.12f,
                horizontalExtent / camera.aspect * 1.12f,
                1f);
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = distance + bounds.extents.magnitude * 4f + 10f;
            camera.Render();
            RenderTexture.active = target;
            panel.ReadPixels(
                new Rect(0f, 0f, target.width, target.height),
                0,
                0);
            panel.Apply();
        }

        private static float ProjectedHalfExtent(
            Vector3 extents,
            Vector3 axis)
        {
            return Mathf.Abs(axis.x) * extents.x +
                   Mathf.Abs(axis.y) * extents.y +
                   Mathf.Abs(axis.z) * extents.z;
        }

        private static string NextDiagnosticPath()
        {
            for (var index = 1; index <= 2; index++)
            {
                var destination = Absolute(string.Format(
                    CultureInfo.InvariantCulture,
                    DiagnosticPathFormat,
                    index));
                if (!File.Exists(destination)) return destination;
            }
            throw new InvalidOperationException(
                "The approved Ispant diagnostic captures already exist.");
        }

        private static Metrics InspectState(
            Scene scene,
            Transform root)
        {
            var kursa = RequireRoot(KursaRootName).transform;
            var longa = RequireRoot(LongaRootName).transform;
            var tergo = RequireRoot(TergoRootName).transform;
            var zSpacing = LongaTergoSpacing(longa, tergo);
            var xSpacing = KursaSlotSpacing(kursa);
            var expectedPosition = new Vector3(
                kursa.position.x,
                kursa.position.y,
                kursa.position.z - zSpacing);
            if (Vector3.Distance(root.position, expectedPosition) > Tolerance ||
                Quaternion.Angle(root.rotation, Quaternion.identity) > 0.1f ||
                Vector3.Distance(root.localScale, Vector3.one) > Tolerance ||
                root.childCount != SlotCount)
            {
                throw new InvalidOperationException(
                    "Ispant root position or twelve-slot contract differs.");
            }

            var rendererCount = -1;
            for (var index = 0; index < SlotCount; index++)
            {
                var slot = root.GetChild(index);
                if (slot.name != SlotNames[index] ||
                    Vector3.Distance(
                        slot.localPosition,
                        new Vector3(index * xSpacing, 0f, 0f)) > Tolerance ||
                    Quaternion.Angle(
                        slot.localRotation,
                        Quaternion.Euler(0f, FacingYaw, 0f)) > 0.1f ||
                    Vector3.Distance(slot.localScale, Vector3.one) > Tolerance ||
                    slot.childCount != 1)
                {
                    throw new InvalidOperationException(
                        "Ispant slot contract differs at index " + index + ".");
                }

                var model = slot.GetChild(0);
                var source = PrefabUtility.GetCorrespondingObjectFromSource(
                    model.gameObject);
                if (model.name != ModelName ||
                    source == null ||
                    AssetDatabase.GetAssetPath(source) != ModelPath)
                {
                    throw new InvalidOperationException(
                        slot.name +
                        " is not a direct instance of the supplied Ispant FBX.");
                }

                var renderers = RequireVisibleGeometry(model);
                if (rendererCount < 0)
                {
                    rendererCount = renderers.Length;
                }
                else if (rendererCount != renderers.Length)
                {
                    throw new InvalidOperationException(
                        "Ispant renderer count differs between slots.");
                }

                var modelBounds = BoundsOf(
                    model,
                    new Bounds(model.position, Vector3.one));
                if (Mathf.Abs(modelBounds.size.y - TargetHeight) > Tolerance ||
                    Mathf.Abs(modelBounds.min.y - root.position.y) > Tolerance)
                {
                    throw new InvalidOperationException(
                        slot.name + " height or ground alignment differs.");
                }

                if (model.GetComponentsInChildren<Animator>(true)
                        .Any(item => item.enabled) ||
                    model.GetComponentsInChildren<Animation>(true)
                        .Any(item => item.enabled))
                {
                    throw new InvalidOperationException(
                        "Ispant placement models must remain static.");
                }
            }

            var bounds = BoundsOf(
                root,
                new Bounds(root.position, Vector3.one));
            return new Metrics
            {
                Ispant = root.position,
                Kursa = kursa.position,
                ZSpacing = zSpacing,
                XSpacing = xSpacing,
                Bounds = bounds,
                RendererCount = rendererCount
            };
        }

        private static void CopyAndImportModel()
        {
            EnsureFolder(ArtRoot);
            EnsureFolder(ModelFolder);
            var destination = Absolute(ModelPath);
            Directory.CreateDirectory(
                Path.GetDirectoryName(destination) ??
                throw new InvalidOperationException(
                    "Invalid Ispant model folder."));
            File.Copy(SourcePath, destination, true);
            AssetDatabase.Refresh(
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(
                ModelPath,
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);
        }

        private static void ConfigureStaticModel(Transform model)
        {
            foreach (var animator in
                     model.GetComponentsInChildren<Animator>(true))
            {
                animator.enabled = false;
                animator.runtimeAnimatorController = null;
                EditorUtility.SetDirty(animator);
            }

            foreach (var animation in
                     model.GetComponentsInChildren<Animation>(true))
            {
                animation.enabled = false;
                EditorUtility.SetDirty(animation);
            }
        }

        private static void ScaleAndGround(Transform model, float groundY)
        {
            var bounds = BoundsOf(
                model,
                new Bounds(model.position, Vector3.one));
            if (bounds.size.y <= 0.00001f)
            {
                throw new InvalidOperationException(
                    "Ispant has no usable visible height.");
            }

            var scale = TargetHeight / bounds.size.y;
            if (float.IsNaN(scale) ||
                float.IsInfinity(scale) ||
                scale <= 0f ||
                scale > 1000f)
            {
                throw new InvalidOperationException(
                    "Ispant target-height scale is invalid.");
            }

            model.localScale = Vector3.one * scale;
            bounds = BoundsOf(
                model,
                new Bounds(model.position, Vector3.one));
            model.position += Vector3.up * (groundY - bounds.min.y);
        }

        private static float LongaTergoSpacing(
            Transform longa,
            Transform tergo)
        {
            var spacing = Mathf.Abs(longa.position.z - tergo.position.z);
            if (spacing <= 0.1f)
            {
                throw new InvalidOperationException(
                    "Longa Arma/Tergo Z spacing is unusable.");
            }

            return spacing;
        }

        private static float KursaSlotSpacing(Transform root)
        {
            if (root.childCount < 2)
            {
                throw new InvalidOperationException(
                    "Kursa needs at least two slots for X spacing.");
            }

            var spacing = Mathf.Abs(
                root.GetChild(1).position.x -
                root.GetChild(0).position.x);
            if (spacing <= 0.1f)
            {
                throw new InvalidOperationException(
                    "Kursa X spacing is unusable.");
            }

            return spacing;
        }

        private static Renderer[] RequireVisibleGeometry(Transform root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true)
                .Where(item => item.enabled)
                .ToArray();
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException(
                    "Ispant has no visible renderer.");
            }

            foreach (var renderer in renderers)
            {
                if (renderer is SkinnedMeshRenderer skinned &&
                    skinned.sharedMesh == null)
                {
                    throw new InvalidOperationException(
                        "Ispant has a SkinnedMeshRenderer without a mesh.");
                }

                if (renderer is MeshRenderer)
                {
                    var filter = renderer.GetComponent<MeshFilter>();
                    if (filter == null || filter.sharedMesh == null)
                    {
                        throw new InvalidOperationException(
                            "Ispant has a MeshRenderer without a mesh.");
                    }
                }
            }

            return renderers;
        }

        private static Bounds BoundsOf(Transform root, Bounds fallback)
        {
            var renderers = RequireVisibleGeometry(root);
            var bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            return bounds.size.sqrMagnitude > 0.000001f ? bounds : fallback;
        }

        private static IEnumerable<Vector3> Corners(Bounds bounds)
        {
            for (var x = -1; x <= 1; x += 2)
            {
                for (var y = -1; y <= 1; y += 2)
                {
                    for (var z = -1; z <= 1; z += 2)
                    {
                        yield return bounds.center + Vector3.Scale(
                            bounds.extents,
                            new Vector3(x, y, z));
                    }
                }
            }
        }

        private static Quaternion YawToward(Vector3 from, Vector3 to)
        {
            var direction = to - from;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f)
            {
                throw new InvalidOperationException(
                    "The Ispant Player view direction is unusable.");
            }

            return Quaternion.LookRotation(direction.normalized, Vector3.up);
        }

        private static Transform RequirePlayer()
        {
            var player = GameObject.Find(PlayerName) ??
                         throw new InvalidOperationException(
                             "The Player root is missing.");
            if (player.transform.parent != null)
            {
                throw new InvalidOperationException(
                    "The Player object is not a scene root.");
            }

            return player.transform;
        }

        private static GameObject RequireRoot(string name)
        {
            var root = GameObject.Find(name) ??
                       throw new InvalidOperationException(
                           name + " is missing.");
            if (root.transform.parent != null)
            {
                throw new InvalidOperationException(
                    name + " is not a scene root.");
            }

            return root;
        }

        private static Scene RequireCurrentScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
            {
                throw new InvalidOperationException(
                    "CargoRunMvp must already be the active scene. ActiveScene=" +
                    scene.path + ".");
            }

            return scene;
        }

        private static string[] ProtectedRootSignatures(Scene scene)
        {
            return scene.GetRootGameObjects()
                .Where(item => item.name != PlacementRootName)
                .Select(item => HierarchySignature(item.transform))
                .OrderBy(item => item, StringComparer.Ordinal)
                .ToArray();
        }

        private static string[] PlayerMoveProtectedRootSignatures(Scene scene)
        {
            return scene.GetRootGameObjects()
                .Where(item => item.name != PlayerName)
                .Select(item => HierarchySignature(item.transform))
                .OrderBy(item => item, StringComparer.Ordinal)
                .ToArray();
        }

        private static string HierarchySignature(Transform root)
        {
            var builder = new StringBuilder();
            foreach (var item in root.GetComponentsInChildren<Transform>(true)
                         .OrderBy(
                             item => RelativePath(root, item),
                             StringComparer.Ordinal))
            {
                builder.Append(RelativePath(root, item));
                builder.Append('|');
                builder.Append(item.gameObject.activeSelf);
                builder.Append('|');
                builder.Append(Vec(item.localPosition));
                builder.Append('|');
                builder.Append(Quat(item.localRotation));
                builder.Append('|');
                builder.Append(Vec(item.localScale));
                builder.Append(';');
            }

            return builder.ToString();
        }

        private static string RelativePath(Transform root, Transform item)
        {
            return item == root ?
                root.name :
                root.name + "/" +
                AnimationUtility.CalculateTransformPath(item, root);
        }

        private static void EnsureFolder(string assetPath)
        {
            var parts = assetPath.Split('/');
            var current = parts[0];
            for (var index = 1; index < parts.Length; index++)
            {
                var next = current + "/" + parts[index];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[index]);
                }

                current = next;
            }
        }

        private static void RequireSource()
        {
            if (!File.Exists(SourcePath))
            {
                throw new FileNotFoundException(
                    "The supplied Ispant FBX is missing.",
                    SourcePath);
            }
        }

        private static void RequireSameHash(string expected, string actual)
        {
            if (!string.Equals(
                    expected,
                    actual,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Ispant FBX SHA-256 differs. Expected=" + expected +
                    ", Actual=" + actual + ".");
            }
        }

        private static string Sha256(string path)
        {
            using var stream = File.OpenRead(path);
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(stream))
                .Replace("-", string.Empty);
        }

        private static string Absolute(string relativePath)
        {
            var projectRoot =
                Directory.GetParent(Application.dataPath)?.FullName ??
                throw new InvalidOperationException(
                    "Unity project root is unavailable.");
            return Path.GetFullPath(Path.Combine(
                projectRoot,
                relativePath.Replace('/', Path.DirectorySeparatorChar)));
        }

        private static string Num(float value)
        {
            return value.ToString("R", CultureInfo.InvariantCulture);
        }

        private static string Vec(Vector3 value)
        {
            return Num(value.x) + "," +
                   Num(value.y) + "," +
                   Num(value.z);
        }

        private static string Quat(Quaternion value)
        {
            return Num(value.x) + "," +
                   Num(value.y) + "," +
                   Num(value.z) + "," +
                   Num(value.w);
        }

        private readonly struct RendererState
        {
            private readonly Renderer renderer;
            private readonly bool enabled;

            public RendererState(Renderer value)
            {
                renderer = value;
                enabled = value.enabled;
            }

            public void Restore()
            {
                if (renderer != null) renderer.enabled = enabled;
            }
        }

        private readonly struct LightState
        {
            private readonly Light light;
            private readonly bool enabled;

            public LightState(Light value)
            {
                light = value;
                enabled = value.enabled;
            }

            public void Disable()
            {
                if (light != null) light.enabled = false;
            }

            public void Restore()
            {
                if (light != null) light.enabled = enabled;
            }
        }

        private sealed class Metrics
        {
            public Vector3 Ispant { get; set; }
            public Vector3 Kursa { get; set; }
            public float ZSpacing { get; set; }
            public float XSpacing { get; set; }
            public Bounds Bounds { get; set; }
            public int RendererCount { get; set; }
        }
    }
}
