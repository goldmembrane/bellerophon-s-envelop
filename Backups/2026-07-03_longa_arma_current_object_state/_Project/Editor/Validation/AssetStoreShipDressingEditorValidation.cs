using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.Validation
{
    public static class AssetStoreShipDressingEditorValidation
    {
        private const string ApprovedSampleRootRelativePath = "artSample/asset_dressing_samples/step02_corridor_floor5_wall2_dense_floorbase_unifiedwall_fullwidthfloor_2026-06-14";
        private const string AppliedComparisonRootName = "unity_applied_comparison";
        private const string AppliedComparisonSuccessMarker = "Asset Store ship dressing step 2 Unity comparison snapshots saved:";

        private static readonly (string From, string To)[] RequiredCorridors =
        {
            ("Cargo Hold", "Cockpit"),
            ("Cargo Hold", "Engine Room"),
            ("Cargo Hold", "Control Room"),
            ("Cargo Hold", "Armory"),
            ("Cargo Hold", "Supply Room"),
            ("Control Room", "Armory"),
            ("Supply Room", "Armory"),
            ("Cockpit", "Engine Room"),
            ("Cockpit", "Control Room"),
            ("Engine Room", "Control Room")
        };

        private static readonly string[] RequiredRooms =
        {
            "Cargo Hold",
            "Cockpit",
            "Engine Room",
            "Control Room",
            "Armory",
            "Supply Room"
        };

        [MenuItem("Bellerophon/Validation/Run Asset Store Ship Dressing Step 1 Validation")]
        public static void Run()
        {
            AssetStoreShipDressingBootstrap.EnsureStep1RootsWithoutValidation();
            ValidateScene();
        }

        [MenuItem("Bellerophon/Validation/Run Asset Store Ship Dressing Step 2 Corridor Validation")]
        public static void RunStep2()
        {
            AssetStoreShipDressingBootstrap.EnsureStep2CorridorDressingWithoutValidation();
            ValidateStep2Scene();
        }

        [MenuItem("Bellerophon/Validation/Capture Asset Store Ship Dressing Step 2 Unity Comparison")]
        public static void CaptureApprovedStep2Comparison()
        {
            ValidateStep2Scene();

            var projectRoot = Directory.GetParent(Application.dataPath);
            if (projectRoot == null)
            {
                throw new InvalidOperationException("Could not resolve project root for Asset Store ship dressing comparison output.");
            }

            var outputRoot = Path.Combine(projectRoot.FullName, ApprovedSampleRootRelativePath, AppliedComparisonRootName);
            Directory.CreateDirectory(outputRoot);

            var corridorRoot = RequireAppliedCorridorRoot("Cargo Hold", "Cockpit");
            var route = Phase4CargoShipGrayboxBootstrap.CorridorRoute("Cargo Hold", "Cockpit");
            var start = route[0];
            var end = route[route.Length - 1];
            var forward = FlatDirection(end - start);
            var right = Vector3.Cross(Vector3.up, forward).normalized;
            var midpoint = (start + end) * 0.5f;

            CaptureAppliedView(
                Path.Combine(outputRoot, "unity_view_01_player_entry.png"),
                start - (forward * 0.82f) + new Vector3(0f, 1.35f, 0f),
                start + (forward * 5.2f) + new Vector3(0f, 1.12f, 0f),
                49f,
                false,
                5.2f);
            CaptureAppliedView(
                Path.Combine(outputRoot, "unity_view_02_floor_wall_diagonal.png"),
                PointAlongRoute(start, end, 2.7f) - (right * 3.05f) + new Vector3(0f, 1.55f, 0f),
                PointAlongRoute(start, end, 6.2f) + (right * 0.3f) + new Vector3(0f, 1.05f, 0f),
                56f,
                false,
                5.2f);
            CaptureAppliedView(
                Path.Combine(outputRoot, "unity_view_03_ceiling_and_wall_underlook.png"),
                PointAlongRoute(start, end, 4.0f) + new Vector3(0f, 0.82f, 0f),
                PointAlongRoute(start, end, 7.6f) + new Vector3(0f, 2.42f, 0f),
                63f,
                false,
                5.2f);

            SetRendererVisibilityByName(corridorRoot.transform, "Ceiling", false);
            CaptureAppliedView(
                Path.Combine(outputRoot, "unity_view_04_layout_topdown.png"),
                midpoint + new Vector3(0f, 12.8f, 0f),
                midpoint,
                45f,
                true,
                7.0f);
            SetRendererVisibilityByName(corridorRoot.transform, "Ceiling", true);

            CaptureAppliedView(
                Path.Combine(outputRoot, "unity_view_05_floor_stack_detail.png"),
                PointAlongRoute(start, end, 3.2f) + (right * 0.42f) + new Vector3(0f, 0.58f, 0f),
                PointAlongRoute(start, end, 5.1f) + new Vector3(0f, 0.16f, 0f),
                52f,
                false,
                4.2f);

            CaptureSlopeCorridorView(
                outputRoot,
                "unity_view_06_cargo_hold_engine_slope.png",
                "Cargo Hold",
                "Engine Room",
                -0.45f);
            CaptureSlopeCorridorView(
                outputRoot,
                "unity_view_07_cargo_hold_armory_slope.png",
                "Cargo Hold",
                "Armory",
                0.45f);
            CaptureRouteSegmentView(
                outputRoot,
                "unity_view_08_control_armory_dense_floor_wall.png",
                "Control Room",
                "Armory",
                1,
                0.2f);
            WriteAppliedComparisonIndex(outputRoot);
            AssetDatabase.Refresh();
            Debug.Log(AppliedComparisonSuccessMarker + " " + outputRoot);
        }

        public static void ValidateScene()
        {
            RequireSceneAndBaseShip();

            var root = RequireRootObject(AssetStoreShipDressingBootstrap.RootName);

            for (var i = 0; i < AssetStoreShipDressingBootstrap.TopLevelRoots.Length; i++)
            {
                RequireDirectChild(root.transform, AssetStoreShipDressingBootstrap.TopLevelRoots[i]);
            }

            var corridorRoot = RequireDirectChild(root.transform, AssetStoreShipDressingBootstrap.CorridorRootName);
            for (var i = 0; i < RequiredCorridors.Length; i++)
            {
                RequireDirectChild(
                    corridorRoot.transform,
                    AssetStoreShipDressingBootstrap.CorridorDressingRootName(RequiredCorridors[i].From, RequiredCorridors[i].To));
            }

            for (var i = 0; i < AssetStoreShipDressingBootstrap.ImportedAssetPaths.Length; i++)
            {
                RequireAssetFolderWithPrefabs(AssetStoreShipDressingBootstrap.ImportedAssetPaths[i]);
            }

            var renderers = root.GetComponentsInChildren<Renderer>(true).Length;
            var enabledColliders = CountEnabledColliders(root.transform);
            if (enabledColliders != 0)
            {
                throw new InvalidOperationException(
                    "Asset Store ship dressing roots must not introduce enabled colliders before traversal-specific validation. EnabledColliders=" +
                    enabledColliders);
            }

            Debug.Log("Asset Store ship dressing step 1 validation passed.");
            Debug.Log(
                "Asset Store ship dressing step 1 details: Root=True; TopRoots=" +
                AssetStoreShipDressingBootstrap.TopLevelRoots.Length +
                "; CorridorRoots=" +
                RequiredCorridors.Length +
                "; ImportedPacks=" +
                AssetStoreShipDressingBootstrap.ImportedAssetPaths.Length +
                "; Renderers=" +
                renderers +
                "; EnabledColliders=0");
        }

        public static void ValidateStep2Scene()
        {
            RequireSceneAndBaseShip();

            var root = RequireRootObject(AssetStoreShipDressingBootstrap.RootName);
            var corridorRoot = RequireDirectChild(root.transform, AssetStoreShipDressingBootstrap.CorridorRootName);
            var totalRenderers = 0;
            var enabledColliders = 0;
            var errorMaterialRenderers = 0;
            var corridorRootsWithDressing = 0;
            var opaqueWallBackings = 0;
            var wall2Panels = 0;
            var horizontalWallBandLiners = 0;
            var denseFloorOverlays = 0;
            var wallPillarSeams = 0;
            var cargoCeilingPanels = 0;
            var cargoHoldDenseFloorOverlays = 0;
            var cargoHoldWall2Panels = 0;
            var opaqueCeilingCaps = 0;
            var opaqueCeilingSideSkirts = 0;
            var corridorWallFillLights = 0;
            var controlArmoryDenseFloorOverlays = 0;
            var thresholdSidePosts = 0;
            var thresholdTopLintels = 0;
            var thresholdCenterBlockers = 0;

            for (var i = 0; i < RequiredCorridors.Length; i++)
            {
                if (!Phase4CargoShipGrayboxBootstrap.HasCorridor(RequiredCorridors[i].From, RequiredCorridors[i].To))
                {
                    throw new InvalidOperationException("Asset Store ship dressing step 2 must preserve corridor route: " + RequiredCorridors[i].From + " to " + RequiredCorridors[i].To);
                }

                var routeRoot = RequireDirectChild(
                    corridorRoot.transform,
                    AssetStoreShipDressingBootstrap.CorridorDressingRootName(RequiredCorridors[i].From, RequiredCorridors[i].To));
                var generated = RequireDirectChild(routeRoot.transform, AssetStoreShipDressingBootstrap.CorridorGeneratedRootName);
                var renderers = generated.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length < 10)
                {
                    throw new InvalidOperationException(
                        "Asset Store ship dressing step 2 corridor root has too few visual renderers: " +
                        routeRoot.name +
                        ", Renderers=" +
                        renderers.Length);
                }

                totalRenderers += renderers.Length;
                errorMaterialRenderers += CountErrorMaterialRenderers(renderers);
                opaqueWallBackings += CountObjectsContaining(generated.transform, "Project Opaque Wall Backing");
                var routeWall2Panels = CountObjectsContaining(generated.transform, "SOL Wall 2 Unified Panel");
                var routeHorizontalWallBandLiners = CountObjectsContaining(generated.transform, "Project Approved Horizontal Wall Band");
                var routeDenseFloorOverlays = CountObjectsContaining(generated.transform, "HSK Floor Base 1 F Dense Overlay");
                var routeCeilingCaps = CountObjectsContaining(generated.transform, "Project Opaque Ceiling Cap") +
                    CountObjectsContaining(generated.transform, "Project Opaque Ceiling Seam Cap") +
                    CountObjectsContaining(generated.transform, "Project Opaque Joint Ceiling Cap") +
                    CountObjectsContaining(generated.transform, "Project Opaque Threshold Ceiling Cap");
                var routeCeilingSideSkirts = CountObjectsContaining(generated.transform, "Project Opaque Ceiling Side Skirt") +
                    CountObjectsContaining(generated.transform, "Project Opaque Joint Ceiling Side Skirt");
                var routeCorridorWallFillLights = CountObjectsContaining(generated.transform, "Approved Corridor Wall Fill Light");
                wall2Panels += routeWall2Panels;
                horizontalWallBandLiners += routeHorizontalWallBandLiners;
                denseFloorOverlays += routeDenseFloorOverlays;
                opaqueCeilingCaps += routeCeilingCaps;
                opaqueCeilingSideSkirts += routeCeilingSideSkirts;
                corridorWallFillLights += routeCorridorWallFillLights;
                wallPillarSeams += CountObjectsContaining(generated.transform, "SOL Wall Pillar Unified Seam");
                cargoCeilingPanels += CountObjectsContaining(generated.transform, "HSK TB_2 Cargo Ceiling");
                thresholdSidePosts += CountObjectsContaining(generated.transform, "SOL Threshold Side Post");
                thresholdTopLintels += CountObjectsContaining(generated.transform, "Approved Threshold Top Lintel");
                thresholdCenterBlockers += CountObjectsContaining(generated.transform, "HSK Threshold Arch");
                thresholdCenterBlockers += CountObjectsContaining(generated.transform, "SMP Threshold Side Cap");
                corridorRootsWithDressing++;

                if (RequiredCorridors[i].From == "Cargo Hold" || RequiredCorridors[i].To == "Cargo Hold")
                {
                    cargoHoldDenseFloorOverlays += routeDenseFloorOverlays;
                    cargoHoldWall2Panels += routeWall2Panels;
                    if (routeDenseFloorOverlays < 24 || routeWall2Panels < 2 || routeCorridorWallFillLights < 2)
                    {
                        throw new InvalidOperationException(
                            "Cargo Hold connected corridor must use approved dense Floor Base 1 F overlays, Wall 2 panels, and warm wall fill lights: " +
                            RequiredCorridors[i].From +
                            " to " +
                            RequiredCorridors[i].To +
                            "; DenseFloorOverlays=" +
                            routeDenseFloorOverlays +
                            "; Wall2Panels=" +
                            routeWall2Panels +
                            "; WallFillLights=" +
                            routeCorridorWallFillLights);
                    }
                }

                if (Connects(RequiredCorridors[i].From, RequiredCorridors[i].To, "Control Room", "Armory"))
                {
                    controlArmoryDenseFloorOverlays += routeDenseFloorOverlays;
                    if (routeDenseFloorOverlays < 1200 || routeCeilingCaps < 12 || routeCeilingSideSkirts < 24 || routeCorridorWallFillLights < 4)
                    {
                        throw new InvalidOperationException(
                            "Control Room to Armory corridor must keep the approved dense floor, ceiling caps, and horizontal wall-band wall treatment. DenseFloorOverlays=" +
                            routeDenseFloorOverlays +
                            "; OpaqueCeilingCaps=" +
                            routeCeilingCaps +
                            "; OpaqueCeilingSideSkirts=" +
                            routeCeilingSideSkirts +
                            "; WallFillLights=" +
                            routeCorridorWallFillLights);
                    }
                }

                var colliders = generated.GetComponentsInChildren<Collider>(true);
                for (var colliderIndex = 0; colliderIndex < colliders.Length; colliderIndex++)
                {
                    if (colliders[colliderIndex].enabled)
                    {
                        enabledColliders++;
                    }
                }
            }

            if (enabledColliders > 0)
            {
                throw new InvalidOperationException("Asset Store ship dressing step 2 must keep imported dressing colliders disabled. EnabledColliders=" + enabledColliders);
            }

            if (errorMaterialRenderers > 0)
            {
                throw new InvalidOperationException("Asset Store ship dressing step 2 has magenta/error-shader corridor renderers. ErrorMaterialRenderers=" + errorMaterialRenderers);
            }

            if (opaqueWallBackings < RequiredCorridors.Length * 2)
            {
                throw new InvalidOperationException(
                    "Asset Store ship dressing step 2 must close hollow corridor wall gaps with hidden backing panels. OpaqueWallBackings=" +
                    opaqueWallBackings);
            }

            if (wall2Panels < RequiredCorridors.Length * 4 || wallPillarSeams < RequiredCorridors.Length * 4)
            {
                throw new InvalidOperationException(
                    "Asset Store ship dressing step 2 must use the approved Wall 2 panel pattern and pillar seams. Wall2Panels=" +
                    wall2Panels +
                    "; WallPillarSeams=" +
                    wallPillarSeams);
            }

            if (horizontalWallBandLiners < RequiredCorridors.Length * 4)
            {
                throw new InvalidOperationException(
                    "Asset Store ship dressing step 2 must keep the approved horizontal wall-band overlay from the ceiling/floor correction pass. HorizontalWallBandLiners=" +
                    horizontalWallBandLiners);
            }

            if (denseFloorOverlays < RequiredCorridors.Length * 24 || cargoCeilingPanels < RequiredCorridors.Length * 2)
            {
                throw new InvalidOperationException(
                    "Asset Store ship dressing step 2 must use the approved dense Floor Base 1 F overlay and TB_2 ceiling panels. DenseFloorOverlays=" +
                    denseFloorOverlays +
                    "; CargoCeilingPanels=" +
                    cargoCeilingPanels);
            }

            if (opaqueCeilingCaps < RequiredCorridors.Length * 4)
            {
                throw new InvalidOperationException(
                    "Asset Store ship dressing step 2 must close the top side of corridor ceiling assets with opaque caps. OpaqueCeilingCaps=" +
                    opaqueCeilingCaps);
            }

            if (opaqueCeilingSideSkirts < RequiredCorridors.Length * 4)
            {
                throw new InvalidOperationException(
                    "Asset Store ship dressing step 2 must close corridor ceiling side openings with opaque skirts. OpaqueCeilingSideSkirts=" +
                    opaqueCeilingSideSkirts);
            }

            if (corridorWallFillLights < RequiredCorridors.Length * 2)
            {
                throw new InvalidOperationException(
                    "Asset Store ship dressing step 2 must light every corridor so the approved Wall 2 pattern remains readable. CorridorWallFillLights=" +
                    corridorWallFillLights);
            }

            if (thresholdCenterBlockers > 0)
            {
                throw new InvalidOperationException("Asset Store ship dressing step 2 must not leave pass-through visual walls in doorway centers. ThresholdCenterBlockers=" + thresholdCenterBlockers);
            }

            if (thresholdSidePosts < RequiredCorridors.Length * 4 || thresholdTopLintels < RequiredCorridors.Length * 2)
            {
                throw new InvalidOperationException(
                    "Asset Store ship dressing step 2 must use open doorway frames instead of centered threshold blockers. ThresholdSidePosts=" +
                    thresholdSidePosts +
                    "; ThresholdTopLintels=" +
                    thresholdTopLintels);
            }

            var hskObjects = CountObjectsWithPrefix(root.transform, "HSK ");
            var solObjects = CountObjectsWithPrefix(root.transform, "SOL ");
            if (hskObjects < RequiredCorridors.Length * 12 || solObjects < RequiredCorridors.Length * 8)
            {
                throw new InvalidOperationException(
                    "Asset Store ship dressing step 2 must use the approved Heavy Station Kit and ScifiOfficeLite corridor assets. HSK=" +
                    hskObjects +
                    ", SOL=" +
                    solObjects);
            }

            var enabledLegacyCorridorRenderers = CountEnabledLegacyCorridorRenderers(root.transform);
            if (enabledLegacyCorridorRenderers > 0)
            {
                throw new InvalidOperationException("Asset Store ship dressing step 2 must hide legacy graybox corridor renderers after adding the dressing layer. EnabledLegacyCorridorRenderers=" + enabledLegacyCorridorRenderers);
            }

            var enabledStage3CargoStartRenderers = CountEnabledRenderers(PostDetailedStage3GameplayPropsBootstrap.CargoStartCorridorDressingName);
            if (enabledStage3CargoStartRenderers > 0)
            {
                throw new InvalidOperationException(
                    "Approved corridor dressing must hide the older Stage 3 cargo-start corridor dressing. EnabledStage3CargoStartRenderers=" +
                    enabledStage3CargoStartRenderers);
            }

            var stage3GameplayPropRoots = CountSceneObjectsNamed(PostDetailedStage3GameplayPropsBootstrap.Stage3RootName);
            if (stage3GameplayPropRoots > 0)
            {
                throw new InvalidOperationException(
                    "Approved corridor dressing must not leave generated Stage 3 room/background dressing in the scene. Stage3GameplayPropRoots=" +
                    stage3GameplayPropRoots);
            }

            var enabledLegacyClearanceColliders = CountEnabledLegacyClearanceColliders();
            if (enabledLegacyClearanceColliders > 0)
            {
                throw new InvalidOperationException(
                    "Approved corridor dressing must disable legacy doorway and joint protrusion colliders that no longer match the visible art pass. EnabledLegacyClearanceColliders=" +
                    enabledLegacyClearanceColliders);
            }

            if (totalRenderers < 120)
            {
                throw new InvalidOperationException("Asset Store ship dressing step 2 created too little corridor visual coverage. Renderers=" + totalRenderers);
            }

            Debug.Log("Asset Store ship dressing step 2 corridor validation passed.");
            Debug.Log(
                "Asset Store ship dressing step 2 details: CorridorRoots=" +
                corridorRootsWithDressing +
                "; Renderers=" +
                totalRenderers +
                "; EnabledColliders=" +
                enabledColliders +
                "; ErrorMaterialRenderers=" +
                errorMaterialRenderers +
                "; OpaqueWallBackings=" +
                opaqueWallBackings +
                "; Wall2Panels=" +
                wall2Panels +
                "; HorizontalWallBandLiners=" +
                horizontalWallBandLiners +
                "; DenseFloorOverlays=" +
                denseFloorOverlays +
                "; WallPillarSeams=" +
                wallPillarSeams +
                "; CargoCeilingPanels=" +
                cargoCeilingPanels +
                "; CargoHoldDenseFloorOverlays=" +
                cargoHoldDenseFloorOverlays +
                "; CargoHoldWall2Panels=" +
                cargoHoldWall2Panels +
                "; OpaqueCeilingCaps=" +
                opaqueCeilingCaps +
                "; OpaqueCeilingSideSkirts=" +
                opaqueCeilingSideSkirts +
                "; CorridorWallFillLights=" +
                corridorWallFillLights +
                "; ControlArmoryDenseFloorOverlays=" +
                controlArmoryDenseFloorOverlays +
                "; ThresholdCenterBlockers=" +
                thresholdCenterBlockers +
                "; ThresholdSidePosts=" +
                thresholdSidePosts +
                "; ThresholdTopLintels=" +
                thresholdTopLintels +
                "; HSK=" +
                hskObjects +
                "; SOL=" +
                solObjects +
                "; EnabledLegacyCorridorRenderers=" +
                enabledLegacyCorridorRenderers +
                "; Stage3GameplayPropRoots=" +
                stage3GameplayPropRoots +
                "; EnabledStage3CargoStartRenderers=" +
                enabledStage3CargoStartRenderers +
                "; EnabledLegacyClearanceColliders=" +
                enabledLegacyClearanceColliders);
        }

        private static GameObject RequireAppliedCorridorRoot(string from, string to)
        {
            var root = RequireRootObject(AssetStoreShipDressingBootstrap.RootName);
            var corridorRoot = RequireDirectChild(root.transform, AssetStoreShipDressingBootstrap.CorridorRootName);
            var routeRoot = RequireDirectChild(
                corridorRoot.transform,
                AssetStoreShipDressingBootstrap.CorridorDressingRootName(from, to));
            return RequireDirectChild(routeRoot.transform, AssetStoreShipDressingBootstrap.CorridorGeneratedRootName);
        }

        private static void CaptureAppliedView(
            string path,
            Vector3 cameraPosition,
            Vector3 lookAt,
            float fieldOfView,
            bool orthographic,
            float orthographicSize)
        {
            var cameraObject = new GameObject("Asset Store Corridor Applied Comparison Camera")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            var keyLightObject = new GameObject("Asset Store Corridor Applied Comparison Key Light")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            var rimLightObject = new GameObject("Asset Store Corridor Applied Comparison Rim Light")
            {
                hideFlags = HideFlags.HideAndDontSave
            };

            try
            {
                keyLightObject.transform.rotation = Quaternion.LookRotation(new Vector3(-0.35f, -0.7f, -0.62f).normalized, Vector3.up);
                var keyLight = keyLightObject.AddComponent<Light>();
                keyLight.type = LightType.Directional;
                keyLight.color = new Color(1f, 0.88f, 0.68f, 1f);
                keyLight.intensity = 0.85f;

                rimLightObject.transform.rotation = Quaternion.LookRotation(new Vector3(0.35f, -0.5f, 0.7f).normalized, Vector3.up);
                var rimLight = rimLightObject.AddComponent<Light>();
                rimLight.type = LightType.Directional;
                rimLight.color = new Color(0.44f, 0.64f, 0.76f, 1f);
                rimLight.intensity = 0.35f;

                var camera = cameraObject.AddComponent<Camera>();
                camera.transform.position = cameraPosition;
                camera.transform.LookAt(lookAt);
                camera.fieldOfView = fieldOfView;
                camera.orthographic = orthographic;
                camera.orthographicSize = orthographicSize;
                camera.nearClipPlane = 0.02f;
                camera.farClipPlane = 80f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.055f, 0.058f, 0.052f, 1f);
                camera.allowHDR = false;
                camera.allowMSAA = true;
                CaptureCamera(camera, path, 1600, 900);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(keyLightObject);
                UnityEngine.Object.DestroyImmediate(rimLightObject);
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

        private static void SetRendererVisibilityByName(Transform root, string namePart, bool visible)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            for (var i = 0; i < renderers.Length; i++)
            {
                if (HasNamePart(renderers[i].transform, root, namePart))
                {
                    renderers[i].enabled = visible;
                }
            }
        }

        private static bool HasNamePart(Transform transform, Transform stopAt, string namePart)
        {
            var current = transform;
            while (current != null)
            {
                if (current.name.IndexOf(namePart, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }

                if (current == stopAt)
                {
                    return false;
                }

                current = current.parent;
            }

            return false;
        }

        private static void WriteAppliedComparisonIndex(string outputRoot)
        {
            var builder = new StringBuilder();
            builder.AppendLine("<!doctype html>");
            builder.AppendLine("<html lang=\"ko\">");
            builder.AppendLine("<head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width, initial-scale=1\"><title>승인 복도 Unity 적용 비교</title>");
            builder.AppendLine("<style>body{margin:0;background:#151611;color:#e9e1cf;font-family:Georgia,'Times New Roman',serif}main{max-width:1400px;margin:0 auto;padding:24px}h1{font-size:27px;margin:0 0 8px}.meta{color:#b8ae96;margin:0 0 18px}.grid{display:grid;gap:18px}.pair{display:grid;grid-template-columns:1fr 1fr;gap:12px;border:1px solid #504b3c;background:#202219;border-radius:6px;padding:12px}.pair h2{grid-column:1/-1;font-size:18px;margin:0}.pair img{display:block;width:100%;height:auto;background:#0e0f0c}.label{font-size:13px;color:#d7ceb8;margin:6px 0 0}@media(max-width:900px){.pair{grid-template-columns:1fr}}</style>");
            builder.AppendLine("</head><body><main>");
            builder.AppendLine("<h1>승인 복도 Unity 적용 비교</h1>");
            builder.AppendLine("<p class=\"meta\">왼쪽은 승인된 artSample, 오른쪽은 `CargoRunMvp`에 실제 적용한 대표 복도 캡처입니다. 대표 복도는 `Cargo Hold`에서 `Cockpit`으로 이어지는 구간입니다.</p>");
            builder.AppendLine("<section class=\"grid\">");
            AddComparisonPair(builder, "01 플레이어 진입 시점", "../view_01_player_entry.png", "unity_view_01_player_entry.png");
            AddComparisonPair(builder, "02 바닥과 벽 연결 대각 구도", "../view_02_floor_wall_diagonal.png", "unity_view_02_floor_wall_diagonal.png");
            AddComparisonPair(builder, "03 천장과 상부 벽 구도", "../view_03_ceiling_and_wall_underlook.png", "unity_view_03_ceiling_and_wall_underlook.png");
            AddComparisonPair(builder, "04 배치/동선 확인 상단 구도", "../view_04_layout_topdown.png", "unity_view_04_layout_topdown.png");
            AddComparisonPair(builder, "05 Floor Base 1 F 전폭 고밀도 바닥 상세", "../view_05_floor_stack_detail.png", "unity_view_05_floor_stack_detail.png");
            AddAppliedOnlyCard(builder, "06 운송창고-동력실 경사 복도 적용 확인", "unity_view_06_cargo_hold_engine_slope.png");
            AddAppliedOnlyCard(builder, "07 운송창고-무기실 경사 복도 적용 확인", "unity_view_07_cargo_hold_armory_slope.png");
            AddAppliedOnlyCard(builder, "08 통제실-무기실 고밀도 바닥과 Wall 2 벽 확인", "unity_view_08_control_armory_dense_floor_wall.png");
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

        private static void AddAppliedOnlyCard(StringBuilder builder, string title, string appliedPath)
        {
            builder.AppendLine("<article class=\"pair\">");
            builder.Append("<h2>").Append(title).AppendLine("</h2>");
            builder.Append("<div><a href=\"").Append(appliedPath).Append("\"><img src=\"").Append(appliedPath).Append("\" alt=\"Unity 경사 복도 적용 결과\"></a><p class=\"label\">Unity 적용 결과</p></div>");
            builder.AppendLine();
            builder.AppendLine("</article>");
        }

        private static void CaptureSlopeCorridorView(
            string outputRoot,
            string fileName,
            string from,
            string to,
            float lateralOffset)
        {
            var route = Phase4CargoShipGrayboxBootstrap.CorridorRoute(from, to);
            var start = route[0];
            var end = route[route.Length - 1];
            var forward = FlatDirection(end - start);
            var right = Vector3.Cross(Vector3.up, forward).normalized;
            CaptureAppliedView(
                Path.Combine(outputRoot, fileName),
                PointAlongRoute(start, end, 1.1f) + (right * lateralOffset) + new Vector3(0f, 0.95f, 0f),
                PointAlongRoute(start, end, 5.2f) + new Vector3(0f, 1.05f, 0f),
                55f,
                false,
                4.8f);
        }

        private static void CaptureRouteSegmentView(
            string outputRoot,
            string fileName,
            string from,
            string to,
            int segmentIndex,
            float lateralOffset)
        {
            var route = Phase4CargoShipGrayboxBootstrap.CorridorRoute(from, to);
            var clampedSegmentIndex = Mathf.Clamp(segmentIndex, 0, route.Length - 2);
            var start = route[clampedSegmentIndex];
            var end = route[clampedSegmentIndex + 1];
            var forward = FlatDirection(end - start);
            var right = Vector3.Cross(Vector3.up, forward).normalized;
            var lookDistance = Mathf.Min(8.0f, Mathf.Max(0.5f, Vector3.Distance(start, end) - 0.3f));
            CaptureAppliedView(
                Path.Combine(outputRoot, fileName),
                start - (forward * 0.58f) + (right * lateralOffset) + new Vector3(0f, 1.08f, 0f),
                start + (forward * lookDistance) + new Vector3(0f, 0.98f, 0f),
                56f,
                false,
                5.6f);
        }

        private static Vector3 FlatDirection(Vector3 direction)
        {
            var flat = new Vector3(direction.x, 0f, direction.z);
            return flat.sqrMagnitude > 0.0001f ? flat.normalized : Vector3.forward;
        }

        private static Vector3 PointAlongRoute(Vector3 start, Vector3 end, float distance)
        {
            var delta = end - start;
            var length = delta.magnitude;
            if (length <= 0.001f)
            {
                return start;
            }

            return start + (delta.normalized * Mathf.Clamp(distance, 0f, length));
        }

        private static void RequireSceneAndBaseShip()
        {
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(AssetStoreShipDressingBootstrap.CargoRunScenePath);
            if (sceneAsset == null)
            {
                throw new InvalidOperationException("Missing CargoRunMvp scene for Asset Store ship dressing validation.");
            }

            if (SceneManager.GetActiveScene().path != AssetStoreShipDressingBootstrap.CargoRunScenePath)
            {
                EditorSceneManager.OpenScene(AssetStoreShipDressingBootstrap.CargoRunScenePath, OpenSceneMode.Single);
            }

            RequireRootObject(Phase4CargoShipGrayboxBootstrap.GrayboxRootName);
            for (var i = 0; i < RequiredRooms.Length; i++)
            {
                if (!Phase4CargoShipGrayboxBootstrap.HasProductionRoomShell(RequiredRooms[i]))
                {
                    throw new InvalidOperationException("Asset Store ship dressing requires the existing production room shell for: " + RequiredRooms[i]);
                }
            }
        }

        private static GameObject RequireRootObject(string objectName)
        {
            var scene = SceneManager.GetActiveScene();
            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                if (roots[i].name == objectName)
                {
                    return roots[i];
                }
            }

            throw new InvalidOperationException("Missing root object: " + objectName);
        }

        private static GameObject RequireDirectChild(Transform parent, string childName)
        {
            var child = parent.Find(childName);
            if (child == null)
            {
                throw new InvalidOperationException("Missing direct child '" + childName + "' under '" + parent.name + "'.");
            }

            return child.gameObject;
        }

        private static void RequireAssetFolderWithPrefabs(string assetFolder)
        {
            var folderAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetFolder);
            if (folderAsset == null)
            {
                throw new InvalidOperationException("Missing imported Asset Store folder: " + assetFolder);
            }

            var prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { assetFolder });
            if (prefabGuids.Length == 0)
            {
                throw new InvalidOperationException("Imported Asset Store folder has no prefabs: " + assetFolder);
            }
        }

        private static int CountErrorMaterialRenderers(Renderer[] renderers)
        {
            var count = 0;
            for (var i = 0; i < renderers.Length; i++)
            {
                if (RendererHasErrorMaterial(renderers[i]))
                {
                    count++;
                }
            }

            return count;
        }

        private static bool RendererHasErrorMaterial(Renderer renderer)
        {
            var materials = renderer.sharedMaterials;
            if (materials == null || materials.Length == 0)
            {
                return true;
            }

            for (var i = 0; i < materials.Length; i++)
            {
                var material = materials[i];
                if (material == null || material.shader == null)
                {
                    return true;
                }

                if (material.shader.name.IndexOf("InternalError", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }

                var color = material.HasProperty("_BaseColor") ? material.GetColor("_BaseColor") :
                    material.HasProperty("_Color") ? material.GetColor("_Color") : Color.black;
                if (color.r > 0.85f && color.b > 0.85f && color.g < 0.25f)
                {
                    return true;
                }
            }

            return false;
        }

        private static int CountObjectsWithPrefix(Transform root, string prefix)
        {
            var count = 0;
            var children = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < children.Length; i++)
            {
                if (children[i].name.StartsWith(prefix, StringComparison.Ordinal))
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountObjectsContaining(Transform root, string fragment)
        {
            var count = 0;
            var children = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < children.Length; i++)
            {
                if (children[i].name.IndexOf(fragment, StringComparison.Ordinal) >= 0)
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

        private static int CountEnabledRenderers(string objectName)
        {
            var target = GameObject.Find(objectName);
            if (target == null)
            {
                return 0;
            }

            var count = 0;
            var renderers = target.GetComponentsInChildren<Renderer>(true);
            for (var i = 0; i < renderers.Length; i++)
            {
                if (renderers[i].enabled)
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountSceneObjectsNamed(string objectName)
        {
            var count = 0;
            var transforms = UnityEngine.Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (var i = 0; i < transforms.Length; i++)
            {
                if (transforms[i] != null && transforms[i].gameObject.name == objectName)
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountEnabledLegacyCorridorRenderers(Transform dressingRoot)
        {
            var count = 0;
            var renderers = UnityEngine.Object.FindObjectsByType<Renderer>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null || IsChildOf(renderer.transform, dressingRoot))
                {
                    continue;
                }

                if (renderer.enabled && renderer.gameObject.name.StartsWith("Corridor - ", StringComparison.Ordinal))
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountEnabledLegacyClearanceColliders()
        {
            var grayboxRoot = GameObject.Find(Phase4CargoShipGrayboxBootstrap.GrayboxRootName);
            if (grayboxRoot == null)
            {
                return 0;
            }

            var count = 0;
            var colliders = grayboxRoot.GetComponentsInChildren<Collider>(true);
            for (var i = 0; i < colliders.Length; i++)
            {
                var collider = colliders[i];
                if (collider != null &&
                    collider.enabled &&
                    IsLegacyCorridorClearanceCollider(collider.gameObject.name))
                {
                    count++;
                }
            }

            return count;
        }

        private static bool IsLegacyCorridorClearanceCollider(string objectName)
        {
            if (!objectName.StartsWith("Corridor - ", StringComparison.Ordinal))
            {
                return false;
            }

            return objectName.Contains(" Mouth Closure Wall", StringComparison.Ordinal) ||
                   objectName.Contains(" Upper Bulkhead Wall", StringComparison.Ordinal) ||
                   (objectName.Contains(" Joint ", StringComparison.Ordinal) &&
                    objectName.Contains(" Closure Wall", StringComparison.Ordinal));
        }

        private static bool Connects(string from, string to, string expectedFrom, string expectedTo)
        {
            return (from == expectedFrom && to == expectedTo) ||
                   (from == expectedTo && to == expectedFrom);
        }

        private static bool IsChildOf(Transform candidate, Transform ancestor)
        {
            var current = candidate;
            while (current != null)
            {
                if (current == ancestor)
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }
    }
}
