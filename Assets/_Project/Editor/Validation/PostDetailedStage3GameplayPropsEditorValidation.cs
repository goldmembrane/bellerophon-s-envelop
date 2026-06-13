using System;
using System.IO;
using Bellerophon.Core.Player;
using Bellerophon.Core.Session;
using Bellerophon.Core.Ship;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.Validation
{
    public static class PostDetailedStage3GameplayPropsEditorValidation
    {
        private const float CargoHoldDeckY = -3f;

        [MenuItem("Bellerophon/Validation/Run Post-Detailed Stage 3 Gameplay Props Validation")]
        public static void Run()
        {
            PostDetailedStage3GameplayPropsBootstrap.EnsureStage3AssetsWithoutValidation();
            ValidateScene();
        }

        [MenuItem("Bellerophon/Validation/Capture Stage 3 Art Sample Unity Snapshots")]
        public static void CaptureUnityComparisonSnapshots()
        {
            PostDetailedStage3GameplayPropsBootstrap.EnsureStage3AssetsWithoutValidation();
            ValidateScene();

            var projectRoot = Directory.GetParent(Application.dataPath);
            if (projectRoot == null)
            {
                throw new InvalidOperationException("Could not resolve project root for Stage 3 Unity comparison snapshots.");
            }

            var outputRoot = Path.Combine(projectRoot.FullName, "artSample", "stage3_rework_review", "unity_current_pass");
            Directory.CreateDirectory(outputRoot);

            CaptureTemporaryView(
                Path.Combine(outputRoot, "unity_01_cockpit_view.png"),
                new Vector3(0f, 1.2f, 15.95f),
                new Vector3(0f, 1.18f, 21.4f));
            CaptureTemporaryView(
                Path.Combine(outputRoot, "unity_02_control_room_view.png"),
                new Vector3(14f, 1.22f, 17.0f),
                new Vector3(14.1f, 1.35f, 21.65f));
            CaptureTemporaryView(
                Path.Combine(outputRoot, "unity_03_engine_room_view.png"),
                new Vector3(-12.35f, 1.18f, 15.7f),
                new Vector3(-15.1f, 1.1f, 18f));
            CaptureTemporaryView(
                Path.Combine(outputRoot, "unity_04_supply_room_view.png"),
                new Vector3(13.1f, 1.22f, -16.75f),
                new Vector3(17.55f, 1.22f, -14.1f));
            CaptureTemporaryView(
                Path.Combine(outputRoot, "unity_05_cargo_hold_view.png"),
                new Vector3(0f, -1.62f, -5.2f),
                new Vector3(0f, -1.45f, 1.2f));
            CaptureTemporaryView(
                Path.Combine(outputRoot, "unity_06_armory_view.png"),
                new Vector3(-14f, 1.22f, -13.1f),
                new Vector3(-14f, 1.38f, -10.25f));
            CaptureFirstPersonView(Path.Combine(outputRoot, "unity_07_first_person_equipment_view.png"));

            Debug.Log("Stage 3 art sample Unity comparison snapshots saved: " + outputRoot);
        }

        public static void ValidateScene()
        {
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(PostDetailedStage3GameplayPropsBootstrap.CargoRunScenePath);
            if (sceneAsset == null)
            {
                throw new InvalidOperationException("Missing CargoRunMvp scene for post-detailed stage 3 gameplay props validation.");
            }

            if (SceneManager.GetActiveScene().path != PostDetailedStage3GameplayPropsBootstrap.CargoRunScenePath)
            {
                EditorSceneManager.OpenScene(PostDetailedStage3GameplayPropsBootstrap.CargoRunScenePath, OpenSceneMode.Single);
            }

            Phase20PresentationEditorValidation.Run();
            RequireStage2Anchors();
            AssertGeneratedLabelsAreSubtle();
            AssertLegacyGrayboxPresentationHidden();

            var stageRoot = RequireObject(PostDetailedStage3GameplayPropsBootstrap.Stage3RootName);
            var worldRendererCount = AssertRendererCount(stageRoot, 220, "stage 3 world prop root");
            AssertMaterials(stageRoot, "stage 3 world prop root");
            AssertPresentationObject(PostDetailedStage3GameplayPropsBootstrap.RoomDressingRootName, 180);
            AssertArtSampleRoomDressingSet();

            AssertPresentationObject(PostDetailedStage3GameplayPropsBootstrap.CockpitHelmPropName, 10);
            AssertPresentationObject(PostDetailedStage3GameplayPropsBootstrap.CockpitStatusScreensName, 8);
            AssertPresentationObject(PostDetailedStage3GameplayPropsBootstrap.ControlRoomCctvTerminalName, 12);
            AssertControlRoomSingleLargeScreenSet();
            AssertPresentationObject(PostDetailedStage3GameplayPropsBootstrap.EngineRoomPowerTerminalName, 5);
            AssertPresentationObject(PostDetailedStage3GameplayPropsBootstrap.SupplyRoomStorageCabinetName, 13);
            AssertPresentationObject(PostDetailedStage3GameplayPropsBootstrap.CargoHoldStatusPanelName, 4);
            AssertPresentationObject(PostDetailedStage3GameplayPropsBootstrap.ArmoryTurretGripMountName, 6);

            AssertPresentationObject(PostDetailedStage3GameplayPropsBootstrap.ContractCargoContainerName, 12);
            AssertContractCargoStraps();
            AssertCargoHoldPropGroundingAndInteractionConnection();
            AssertPresentationObject(PostDetailedStage3GameplayPropsBootstrap.PersonalCargoContainerName, 4);
            AssertPresentationObject(PostDetailedStage3GameplayPropsBootstrap.WarningLabelSetName, 4);
            AssertNoSampleOnlyLooseProps();

            RequireMissingObject(PostDetailedStage3GameplayPropsBootstrap.SpecialEquipmentRootName);
            RequireMissingObject(PostDetailedStage3GameplayPropsBootstrap.PresenceDetectorPropName);
            RequireMissingObject(PostDetailedStage3GameplayPropsBootstrap.LightBladePropName);
            RequireMissingObject(PostDetailedStage3GameplayPropsBootstrap.ElectricMinePropName);
            RequireMissingObject(PostDetailedStage3GameplayPropsBootstrap.CorridorPurifierIconName);

            AssertPresentationObject(PostDetailedStage3GameplayPropsBootstrap.DiegeticTerminalShellName, 9);
            RequireObject(PostDetailedStage3GameplayPropsBootstrap.DiegeticTerminalScreenBackingName);
            RequireObject(PostDetailedStage3GameplayPropsBootstrap.DiegeticTerminalButtonMeshName + " 1");

            var firstPersonRendererCount = AssertFirstPersonEquipmentPreview();
            AssertEquipmentDefinitions();
            AssertStage3MaterialAssets();
            AssertStage3BlenderReworkAssets();
            AssertApprovedArtSampleAlignment();

            Debug.Log("Post-detailed stage 3 gameplay props editor validation passed.");
            Debug.Log(
                "Post-detailed stage 3 gameplay props details: WorldRenderers=" +
                worldRendererCount +
                "; FirstPersonRenderers=" +
                firstPersonRendererCount +
                "; CargoStraps=2; DeviceSurfaces=7; SampleOnlyLooseProps=0; ArtSampleMatch=True; RuntimeIntegration=True");
        }

        private static void CaptureTemporaryView(string path, Vector3 position, Vector3 lookAt)
        {
            var cameraObject = new GameObject("Stage 3 Unity Comparison Camera");
            cameraObject.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                var camera = cameraObject.AddComponent<Camera>();
                camera.transform.position = position;
                camera.transform.rotation = Quaternion.LookRotation((lookAt - position).normalized, Vector3.up);
                camera.fieldOfView = 62f;
                camera.aspect = 16f / 9f;
                camera.nearClipPlane = 0.05f;
                camera.farClipPlane = ShipInteriorAtmosphereController.TargetCameraFarClip;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.004f, 0.005f, 0.006f, 1f);
                CaptureCamera(camera, path);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static void CaptureFirstPersonView(string path)
        {
            var cameraObject = RequireObject("Player Camera");
            var camera = cameraObject.GetComponent<Camera>();
            var visualController = UnityEngine.Object.FindFirstObjectByType<FirstPersonEquipmentVisualController>();
            var deviceState = UnityEngine.Object.FindFirstObjectByType<ShipDeviceInteractionState>();
            if (camera == null || visualController == null || deviceState == null)
            {
                throw new InvalidOperationException("Stage 3 first-person comparison snapshot requires player camera, equipment visual controller, and device state.");
            }

            var original = deviceState.CurrentEquipmentState;
            var originalPosition = camera.transform.position;
            var originalRotation = camera.transform.rotation;
            var originalFieldOfView = camera.fieldOfView;
            try
            {
                deviceState.SetEquipmentState(PlayerEquipmentState.CreateDefaultAssociationIssue());
                visualController.RefreshForValidation();
                camera.fieldOfView = 62f;
                camera.transform.position = new Vector3(2.05f, -1.56f, -4.85f);
                camera.transform.rotation = Quaternion.LookRotation((new Vector3(2.05f, -1.44f, 3.85f) - camera.transform.position).normalized, Vector3.up);
                CaptureCamera(camera, path);
            }
            finally
            {
                camera.transform.position = originalPosition;
                camera.transform.rotation = originalRotation;
                camera.fieldOfView = originalFieldOfView;
                deviceState.SetEquipmentState(original);
                visualController.RefreshForValidation();
            }
        }

        private static void CaptureCamera(Camera camera, string path)
        {
            CaptureCamera(camera, path, 1280, 720);
        }

        private static void CaptureCamera(Camera camera, string path, int width, int height)
        {
            var previousTargetTexture = camera.targetTexture;
            var previousActiveTexture = RenderTexture.active;
            var renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var readableTexture = new Texture2D(width, height, TextureFormat.RGB24, false);

            try
            {
                camera.targetTexture = renderTexture;
                camera.Render();
                RenderTexture.active = renderTexture;
                readableTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                readableTexture.Apply();
                File.WriteAllBytes(path, readableTexture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTargetTexture;
                RenderTexture.active = previousActiveTexture;
                UnityEngine.Object.DestroyImmediate(renderTexture);
                UnityEngine.Object.DestroyImmediate(readableTexture);
            }
        }

        private static void RequireStage2Anchors()
        {
            RequireObject("Cargo Hold Central Cargo");
            RequireObject("Corridor - Control Room to Armory Segment 1 Floor");
            RequireObject("Corridor - Cargo Hold to Armory Segment 1 Floor");
            RequireObject("Armory Turret Station Support Frame");
            RequireObject("Door Frame - Cargo Hold - North Cockpit Lintel");
            Phase4CargoShipGrayboxEditorValidation.RequireCargoHoldOutboundCorridorUniformWallLighting();
        }

        private static void AssertContractCargoStraps()
        {
            var body = RequireObject(PostDetailedStage3GameplayPropsBootstrap.ContractCargoBodyName);
            var horizontal = RequireObject(PostDetailedStage3GameplayPropsBootstrap.ContractCargoStrapHorizontalName);
            var vertical = RequireObject(PostDetailedStage3GameplayPropsBootstrap.ContractCargoStrapVerticalName);
            RequireMissingObject(PostDetailedStage3GameplayPropsBootstrap.ContractCargoLowerStrapName);

            if (horizontal.transform.lossyScale.x < body.transform.lossyScale.x * 1.05f)
            {
                throw new InvalidOperationException("Stage 3 contract cargo horizontal strap must visibly wrap across the cargo width.");
            }

            if (vertical.transform.lossyScale.y < body.transform.lossyScale.y * 0.88f)
            {
                throw new InvalidOperationException("Stage 3 contract cargo vertical strap must visibly secure the cargo height.");
            }

            if (Mathf.Abs(horizontal.transform.position.z - vertical.transform.position.z) > 0.08f)
            {
                throw new InvalidOperationException("Stage 3 contract cargo straps must sit on the same front face.");
            }
        }

        private static void AssertCargoHoldPropGroundingAndInteractionConnection()
        {
            var centralCargo = RequireObject("Cargo Hold Central Cargo");
            var body = RequireObject(PostDetailedStage3GameplayPropsBootstrap.ContractCargoBodyName);
            if (Vector3.Distance(body.transform.position, centralCargo.transform.position) > 0.16f)
            {
                throw new InvalidOperationException("Stage 3 contract cargo must remain aligned with the original cargo-hold central cargo target.");
            }

            if (Mathf.Abs(body.transform.lossyScale.y - centralCargo.transform.lossyScale.y) > 0.12f ||
                Mathf.Abs(body.transform.lossyScale.z - centralCargo.transform.lossyScale.z) > 0.16f)
            {
                throw new InvalidOperationException("Stage 3 contract cargo must read as a fitted upgrade to the original beige cargo brick.");
            }

            var statusInteractable = RequireObject("Interactable - Cargo Hold Cargo Status");
            var statusPanel = RequireObject(PostDetailedStage3GameplayPropsBootstrap.CargoHoldStatusPanelName + " Panel Frame");
            _ = statusInteractable;
            if (statusPanel.transform.position.x > -5.1f ||
                statusPanel.transform.position.y < -1.8f ||
                statusPanel.transform.position.y > -0.6f)
            {
                throw new InvalidOperationException("Stage 3 cargo status panel must be installed on the cargo-hold side wall instead of blocking the first-person corridor view.");
            }

            if (statusPanel.transform.lossyScale.z < 1.2f ||
                statusPanel.transform.lossyScale.y < 0.65f ||
                statusPanel.transform.lossyScale.x > 0.22f)
            {
                throw new InvalidOperationException("Stage 3 cargo status panel must read as a broad wall-mounted terminal rather than a front-facing blocker.");
            }

            AssertBottomNearCargoHoldDeck(RequireObject(PostDetailedStage3GameplayPropsBootstrap.PersonalCargoContainerName + " Body"), "personal cargo container");
            AssertBottomNearCargoHoldDeck(RequireObject(PostDetailedStage3GameplayPropsBootstrap.DiegeticTerminalShellName + " Pedestal"), "diegetic terminal pedestal");
        }

        private static void AssertBottomNearCargoHoldDeck(GameObject gameObject, string label)
        {
            var bottom = gameObject.transform.position.y - (gameObject.transform.lossyScale.y * 0.5f);
            if (Mathf.Abs(bottom - CargoHoldDeckY) > 0.08f)
            {
                throw new InvalidOperationException("Stage 3 " + label + " must rest on the cargo hold deck. BottomY=" + bottom.ToString("0.00"));
            }
        }

        private static void AssertNoSampleOnlyLooseProps()
        {
            RequireMissingObject(PostDetailedStage3GameplayPropsBootstrap.RepairPanelKitName);
            RequireMissingObject(PostDetailedStage3GameplayPropsBootstrap.DamagedPanelKitName);
            RequireMissingObject(PostDetailedStage3GameplayPropsBootstrap.EscapePodVisualName);
            RequireMissingObject(PostDetailedStage3GameplayPropsBootstrap.NormalMaterialVariantPanelName);
            RequireMissingObject(PostDetailedStage3GameplayPropsBootstrap.DamagedMaterialVariantPanelName);
        }

        private static void AssertLegacyGrayboxPresentationHidden()
        {
            var rendererOnlyObjects = new[]
            {
                "Cargo Hold Central Cargo",
                "Interactable - Cargo Hold Cargo Status",
                "Interactable - Cockpit Helm",
                "Interactable - Engine Room Power Screen",
                "Interactable - Control Room Main Screen",
                "Interactable - Armory Turret Handle",
                "Interactable - Supply Room Storage Cabinet",
                "Control Room Horizontal Screen Placeholder",
                "Control Room Vertical Screen Placeholder",
                "Control Room Screen Partition",
                "Cockpit Console Base",
                "Cockpit Worn Button Strip",
                "Engine Room Central Power Cylinder",
                "Armory Central Pillar",
                "Armory Forward Screen Placeholder",
                "Supply Room Ejection Pad Placeholder",
                "Supply Room Ejection Terminal Placeholder",
            };

            for (var i = 0; i < rendererOnlyObjects.Length; i++)
            {
                AssertRenderersDisabled(rendererOnlyObjects[i]);
            }
        }

        private static void AssertRenderersDisabled(string objectName)
        {
            var target = RequireObject(objectName);
            var renderers = target.GetComponentsInChildren<MeshRenderer>(true);
            for (var i = 0; i < renderers.Length; i++)
            {
                if (renderers[i].enabled)
                {
                    throw new InvalidOperationException("Legacy graybox renderer must be hidden behind Stage 3 art-sample dressing: " + objectName);
                }
            }
        }

        private static void AssertArtSampleRoomDressingSet()
        {
            var root = RequireObject(PostDetailedStage3GameplayPropsBootstrap.RoomDressingRootName);
            AssertPresentationObject(PostDetailedStage3GameplayPropsBootstrap.CockpitDressingName, 35);
            AssertPresentationObject(PostDetailedStage3GameplayPropsBootstrap.ControlRoomDressingName, 25);
            AssertPresentationObject(PostDetailedStage3GameplayPropsBootstrap.EngineRoomDressingName, 22);
            AssertPresentationObject(PostDetailedStage3GameplayPropsBootstrap.SupplyRoomDressingName, 28);
            AssertPresentationObject(PostDetailedStage3GameplayPropsBootstrap.CargoHoldDressingName, 38);
            AssertPresentationObject(PostDetailedStage3GameplayPropsBootstrap.ArmoryDressingName, 25);
            AssertPresentationObject(PostDetailedStage3GameplayPropsBootstrap.CargoStartCorridorDressingName, 38);
            AssertDressingMaterialMix(root);
            AssertViewpointRendererCoverage(
                "cargo start corridor",
                new Vector3(0f, -1.62f, -5.2f),
                new Vector3(0f, -1.45f, 1.2f),
                18);
            AssertViewpointRendererCoverage(
                "control room CCTV wall",
                new Vector3(14f, 1.22f, 17.0f),
                new Vector3(14.1f, 1.35f, 21.65f),
                16);
            AssertViewpointRendererCoverage(
                "cockpit helm wall",
                new Vector3(0f, 1.2f, 16.0f),
                new Vector3(0f, 1.18f, 21.4f),
                14);
        }

        private static void AssertDressingMaterialMix(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<MeshRenderer>(true);
            var darkMetal = 0;
            var screenOrLight = 0;
            var warningOrYellow = 0;
            var rubber = 0;
            for (var i = 0; i < renderers.Length; i++)
            {
                var material = renderers[i].sharedMaterial;
                var path = material == null ? string.Empty : AssetDatabase.GetAssetPath(material).Replace('\\', '/');
                if (path == PostDetailedStage3GameplayPropsBootstrap.MetalMaterialPath ||
                    path == PostDetailedStage3GameplayPropsBootstrap.DamagedMaterialPath ||
                    path == PostDetailedStage3GameplayPropsBootstrap.CargoMaterialPath)
                {
                    darkMetal++;
                }
                else if (path == PostDetailedStage3GameplayPropsBootstrap.ScreenMaterialPath ||
                         path == PostDetailedStage3GameplayPropsBootstrap.LightMaterialPath ||
                         path == PostDetailedStage3GameplayPropsBootstrap.WarmLightMaterialPath)
                {
                    screenOrLight++;
                }
                else if (path == PostDetailedStage3GameplayPropsBootstrap.WarningMaterialPath ||
                         path == PostDetailedStage3GameplayPropsBootstrap.YellowMaterialPath)
                {
                    warningOrYellow++;
                }
                else if (path == PostDetailedStage3GameplayPropsBootstrap.DarkRubberMaterialPath)
                {
                    rubber++;
                }
            }

            if (darkMetal < 90 || screenOrLight < 14 || warningOrYellow < 10 || rubber < 55)
            {
                throw new InvalidOperationException(
                    "Stage 3 art-sample dressing must carry the approved industrial mix of worn metal, CRT/light, hazard paint, and rubber cabling. Metal=" +
                    darkMetal +
                    ", ScreenLight=" +
                    screenOrLight +
                    ", WarningYellow=" +
                    warningOrYellow +
                    ", Rubber=" +
                    rubber);
            }
        }

        private static void AssertViewpointRendererCoverage(string label, Vector3 position, Vector3 lookAt, int minimumVisibleRenderers)
        {
            var root = RequireObject(PostDetailedStage3GameplayPropsBootstrap.RoomDressingRootName);
            var cameraObject = new GameObject("Stage 3 Art Sample Coverage Camera");
            cameraObject.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                var camera = cameraObject.AddComponent<Camera>();
                camera.transform.position = position;
                camera.transform.rotation = Quaternion.LookRotation((lookAt - position).normalized, Vector3.up);
                camera.fieldOfView = 72f;
                camera.aspect = 16f / 9f;
                camera.nearClipPlane = 0.05f;
                camera.farClipPlane = 14f;

                var planes = GeometryUtility.CalculateFrustumPlanes(camera);
                var renderers = root.GetComponentsInChildren<MeshRenderer>(true);
                var visible = 0;
                for (var i = 0; i < renderers.Length; i++)
                {
                    if (renderers[i].gameObject.activeInHierarchy &&
                        GeometryUtility.TestPlanesAABB(planes, renderers[i].bounds))
                    {
                        visible++;
                    }
                }

                if (visible < minimumVisibleRenderers)
                {
                    throw new InvalidOperationException(
                        "Stage 3 art-sample dressing is too sparse from the " +
                        label +
                        " viewpoint. VisibleRenderers=" +
                        visible +
                        ", Minimum=" +
                        minimumVisibleRenderers);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static void AssertControlRoomSingleLargeScreenSet()
        {
            var mainFrame = RequireObject(PostDetailedStage3GameplayPropsBootstrap.ControlRoomCctvMainScreenFrameName);
            var mainGlow = RequireObject(PostDetailedStage3GameplayPropsBootstrap.ControlRoomCctvMainScreenGlowName);
            var horizontal = RequireObject(PostDetailedStage3GameplayPropsBootstrap.ControlRoomCctvHorizontalScreenName + " Frame");
            var vertical = RequireObject(PostDetailedStage3GameplayPropsBootstrap.ControlRoomCctvVerticalScreenName + " Frame");
            var aButton = RequireObject(PostDetailedStage3GameplayPropsBootstrap.ControlRoomCctvButtonAName);
            var dButton = RequireObject(PostDetailedStage3GameplayPropsBootstrap.ControlRoomCctvButtonDName);
            var mainFrameBounds = RequireRendererBounds(mainFrame, "control-room CCTV main frame");
            var verticalBounds = RequireRendererBounds(vertical, "control-room CCTV vertical helper screen");

            var mainFrameWidth = Mathf.Max(mainFrameBounds.size.x, mainFrame.transform.lossyScale.x);
            var mainFrameHeight = Mathf.Max(mainFrameBounds.size.y, mainFrame.transform.lossyScale.y);
            var verticalHeight = Mathf.Max(verticalBounds.size.y, vertical.transform.lossyScale.y);

            if (mainFrameWidth < 1.6f || mainFrameHeight < 0.78f)
            {
                throw new InvalidOperationException("Stage 3 control room CCTV must use one dominant large main screen.");
            }

            if (mainGlow.transform.position.z >= mainFrame.transform.position.z)
            {
                throw new InvalidOperationException("Stage 3 control room main CCTV glow must sit on the visible front face of the large screen frame.");
            }

            if (horizontal.transform.position.x > mainFrame.transform.position.x ||
                horizontal.transform.position.y < mainFrame.transform.position.y + 0.45f)
            {
                throw new InvalidOperationException("Stage 3 control room horizontal helper screen must sit above the upper-left of the main screen.");
            }

            if (vertical.transform.position.x < mainFrame.transform.position.x + 1.1f ||
                verticalHeight < mainFrameHeight)
            {
                throw new InvalidOperationException("Stage 3 control room vertical screen must sit to the right of the large CCTV screen.");
            }

            if (aButton.transform.position.y < 0.86f ||
                dButton.transform.position.x <= aButton.transform.position.x)
            {
                throw new InvalidOperationException("Stage 3 control room A/D controls must be readable on the console lip below the large screen.");
            }

            RequireMissingObject(PostDetailedStage3GameplayPropsBootstrap.ControlRoomCctvTerminalName + " Monitor Frame 1");
            RequireMissingObject(PostDetailedStage3GameplayPropsBootstrap.ControlRoomCctvTerminalName + " Monitor Frame 2");
            RequireMissingObject(PostDetailedStage3GameplayPropsBootstrap.ControlRoomCctvTerminalName + " Monitor Frame 3");
            RequireMissingObject(PostDetailedStage3GameplayPropsBootstrap.ControlRoomCctvTerminalName + " Monitor Glow 1");
            RequireMissingObject(PostDetailedStage3GameplayPropsBootstrap.ControlRoomCctvTerminalName + " Monitor Glow 2");
            RequireMissingObject(PostDetailedStage3GameplayPropsBootstrap.ControlRoomCctvTerminalName + " Monitor Glow 3");
        }

        private static Bounds RequireRendererBounds(GameObject target, string label)
        {
            var renderer = target.GetComponent<MeshRenderer>();
            if (renderer == null)
            {
                throw new InvalidOperationException("Stage 3 " + label + " must have a renderer.");
            }

            return renderer.bounds;
        }

        private static int AssertFirstPersonEquipmentPreview()
        {
            var cameraObject = RequireObject("Player Camera");
            var preview = RequireObject(PostDetailedStage3GameplayPropsBootstrap.FirstPersonPreviewRootName);
            if (preview.transform.parent != cameraObject.transform)
            {
                throw new InvalidOperationException("Stage 3 first-person equipment preview must be parented to Player Camera.");
            }

            var visualController = preview.GetComponent<FirstPersonEquipmentVisualController>();
            var deviceState = UnityEngine.Object.FindFirstObjectByType<ShipDeviceInteractionState>();
            if (visualController == null || deviceState == null)
            {
                throw new InvalidOperationException("Stage 3 first-person equipment preview requires a runtime visual controller and ship device state.");
            }

            visualController.RefreshForValidation();

            AssertContinuousCrowbarModel();
            AssertPresentationObject(PostDetailedStage3GameplayPropsBootstrap.MusketModelName, 12);
            AssertPresentationObject(PostDetailedStage3GameplayPropsBootstrap.ProtectiveSuitReadoutName, 3);
            AssertActiveFirstPersonWeaponVisuals(visualController, deviceState, cameraObject.GetComponent<Camera>());
            AssertMaterials(preview, "stage 3 first-person preview");
            AssertFirstPersonBounds(preview, cameraObject.transform);
            return preview.GetComponentsInChildren<MeshRenderer>(true).Length;
        }

        private static void AssertContinuousCrowbarModel()
        {
            var crowbar = RequireObject(PostDetailedStage3GameplayPropsBootstrap.CrowbarModelName);
            var renderers = crowbar.GetComponentsInChildren<MeshRenderer>(true);
            MeshRenderer bodyRenderer = null;
            for (var i = 0; i < renderers.Length; i++)
            {
                if (renderers[i].name == PostDetailedStage3GameplayPropsBootstrap.CrowbarContinuousBodyName)
                {
                    bodyRenderer = renderers[i];
                    break;
                }
            }

            if (bodyRenderer == null)
            {
                throw new InvalidOperationException("Stage 3 stick/crowbar must include one continuous hooked body mesh.");
            }

            var meshFilter = bodyRenderer.GetComponent<MeshFilter>();
            var mesh = meshFilter == null ? null : meshFilter.sharedMesh;
            if (mesh == null || mesh.vertexCount < 250 || mesh.triangles.Length < 1500)
            {
                throw new InvalidOperationException("Stage 3 stick/crowbar continuous mesh is too coarse to read as a smooth hooked two-handed weapon.");
            }

            var bounds = mesh.bounds;
            if (Stage3BlenderReviewAssetBuilder.IsBlenderFbxMesh(mesh))
            {
                var longestAxis = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
                if (longestAxis < 1.85f)
                {
                    throw new InvalidOperationException("Stage 3 stick/crowbar Blender FBX mesh must keep the long art-sample two-handed shaft length.");
                }

                RequireObject(PostDetailedStage3GameplayPropsBootstrap.CrowbarGripWrapName + " Lower 1");
                RequireObject(PostDetailedStage3GameplayPropsBootstrap.CrowbarGripWrapName + " Upper 1");
                RequireObject(PostDetailedStage3GameplayPropsBootstrap.CrowbarLowerGloveName + " Palm");
                RequireObject(PostDetailedStage3GameplayPropsBootstrap.CrowbarUpperGloveName + " Palm");
                RequireMissingObject(PostDetailedStage3GameplayPropsBootstrap.CrowbarModelName + " Round Main Shaft");
                RequireMissingObject(PostDetailedStage3GameplayPropsBootstrap.CrowbarModelName + " Upper Hook Neck");
                RequireMissingObject(PostDetailedStage3GameplayPropsBootstrap.CrowbarModelName + " Hook Flattened Claw");
                RequireMissingObject(PostDetailedStage3GameplayPropsBootstrap.CrowbarModelName + " Single Flattened Pry End");
                AssertMaterials(crowbar, PostDetailedStage3GameplayPropsBootstrap.CrowbarModelName);
                return;
            }

            if (bounds.min.y > -0.68f || bounds.max.y < 0.62f || bounds.max.x < 0.24f)
            {
                throw new InvalidOperationException("Stage 3 stick/crowbar must keep a long two-handed shaft and a visible hooked pry end.");
            }

            const int radialSegments = 24;
            var vertices = mesh.vertices;
            var finalRingStart = vertices.Length - radialSegments - 2;
            if (finalRingStart < 0)
            {
                throw new InvalidOperationException("Stage 3 stick/crowbar mesh does not contain enough rings for hooked tip validation.");
            }

            var hookTipMin = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            var hookTipMax = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
            for (var i = 0; i < radialSegments; i++)
            {
                var vertex = vertices[finalRingStart + i];
                hookTipMin = Vector3.Min(hookTipMin, vertex);
                hookTipMax = Vector3.Max(hookTipMax, vertex);
            }

            var hookTipCenter = (hookTipMin + hookTipMax) * 0.5f;
            if (hookTipCenter.x < 0.23f ||
                hookTipCenter.y < 0.36f ||
                hookTipCenter.y > 0.46f ||
                hookTipMax.y - hookTipMin.y > 0.035f ||
                hookTipMax.z - hookTipMin.z > 0.025f)
            {
                throw new InvalidOperationException("Stage 3 stick/crowbar hooked pry tip must taper into a flattened curved end.");
            }

            RequireObject(PostDetailedStage3GameplayPropsBootstrap.CrowbarGripWrapName + " Lower 1");
            RequireObject(PostDetailedStage3GameplayPropsBootstrap.CrowbarGripWrapName + " Upper 1");
            RequireObject(PostDetailedStage3GameplayPropsBootstrap.CrowbarLowerGloveName + " Palm");
            RequireObject(PostDetailedStage3GameplayPropsBootstrap.CrowbarUpperGloveName + " Palm");
            RequireMissingObject(PostDetailedStage3GameplayPropsBootstrap.CrowbarModelName + " Round Main Shaft");
            RequireMissingObject(PostDetailedStage3GameplayPropsBootstrap.CrowbarModelName + " Upper Hook Neck");
            RequireMissingObject(PostDetailedStage3GameplayPropsBootstrap.CrowbarModelName + " Hook Flattened Claw");
            RequireMissingObject(PostDetailedStage3GameplayPropsBootstrap.CrowbarModelName + " Single Flattened Pry End");
            AssertMaterials(crowbar, PostDetailedStage3GameplayPropsBootstrap.CrowbarModelName);
        }

        private static void AssertActiveFirstPersonWeaponVisuals(
            FirstPersonEquipmentVisualController visualController,
            ShipDeviceInteractionState deviceState,
            Camera camera)
        {
            if (visualController.StickVisual == null ||
                visualController.MusketVisual == null ||
                visualController.ProtectiveSuitReadout == null)
            {
                throw new InvalidOperationException("Stage 3 first-person visual controller is missing one or more configured visual roots.");
            }

            if (camera == null)
            {
                throw new InvalidOperationException("Stage 3 first-person crowbar visibility validation requires a Camera component on Player Camera.");
            }

            var original = deviceState.CurrentEquipmentState;
            var stickState = PlayerEquipmentState.CreateDefaultAssociationIssue();
            var musketState = stickState
                .WithHandSlot(1, EquipmentSlotState.One(EquipmentItemKind.Musket))
                .WithActiveHandSlot(1);

            try
            {
                deviceState.SetEquipmentState(stickState);
                visualController.RefreshForValidation();
                AssertWeaponVisualState(visualController, true, false, false, "Stick default state");
                AssertActiveStickViewportVisibility(camera, visualController.StickVisual);

                deviceState.SetEquipmentState(musketState);
                visualController.RefreshForValidation();
                AssertWeaponVisualState(visualController, false, true, false, "Musket active state");
            }
            finally
            {
                deviceState.SetEquipmentState(original);
                visualController.RefreshForValidation();
            }
        }

        private static void AssertActiveStickViewportVisibility(Camera camera, GameObject stickVisual)
        {
            var renderers = stickVisual.GetComponentsInChildren<MeshRenderer>(false);
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException("Stage 3 stick/crowbar visual is active but has no active renderer.");
            }

            var viewportBounds = CalculateCombinedViewportBounds(camera, renderers);
            var center = viewportBounds.center;
            if (viewportBounds.width < 0.085f || viewportBounds.height < 0.54f)
            {
                throw new InvalidOperationException(
                    "Stage 3 crowbar is too small or too short compared with the approved long first-person art sample. ViewportSize=" +
                    viewportBounds.width.ToString("0.00") +
                    "x" +
                    viewportBounds.height.ToString("0.00"));
            }

            if (viewportBounds.width > 0.72f || viewportBounds.height > 1.90f)
            {
                throw new InvalidOperationException(
                    "Stage 3 crowbar takes too much of the first-person camera and no longer matches the approved lower-right art sample. ViewportSize=" +
                    viewportBounds.width.ToString("0.00") +
                    "x" +
                    viewportBounds.height.ToString("0.00"));
            }

            if (center.x < 0.44f || center.x > 0.92f || center.y < 0.05f || center.y > 0.78f)
            {
                throw new InvalidOperationException(
                    "Stage 3 crowbar must sit in the lower-right first-person equipment band. ViewportCenter=" +
                    center.x.ToString("0.00") +
                    "," +
                    center.y.ToString("0.00"));
            }

            if (viewportBounds.xMax > 1.04f || viewportBounds.yMin < -0.75f)
            {
                throw new InvalidOperationException(
                    "Stage 3 crowbar is clipped or overlaps the minimap-safe corner too strongly. ViewportBounds=" +
                    viewportBounds.xMin.ToString("0.00") +
                    "," +
                    viewportBounds.yMin.ToString("0.00") +
                    "-" +
                    viewportBounds.xMax.ToString("0.00") +
                    "," +
                    viewportBounds.yMax.ToString("0.00"));
            }
        }

        private static Rect CalculateCombinedViewportBounds(Camera camera, MeshRenderer[] renderers)
        {
            var min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            var max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            var included = 0;
            for (var i = 0; i < renderers.Length; i++)
            {
                if (!renderers[i].gameObject.activeInHierarchy)
                {
                    continue;
                }

                var bounds = CalculateViewportBounds(camera, renderers[i], renderers[i].name);
                min = Vector2.Min(min, new Vector2(bounds.xMin, bounds.yMin));
                max = Vector2.Max(max, new Vector2(bounds.xMax, bounds.yMax));
                included++;
            }

            if (included == 0)
            {
                throw new InvalidOperationException("Stage 3 first-person stick/crowbar has no active renderers for viewport validation.");
            }

            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private static Rect CalculateViewportBounds(Camera camera, MeshRenderer renderer, string label)
        {
            var meshFilter = renderer.GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null && meshFilter.sharedMesh.vertexCount > 0)
            {
                return CalculateViewportBounds(camera, meshFilter.sharedMesh.vertices, renderer.transform, label);
            }

            return CalculateViewportBounds(camera, renderer.bounds, label);
        }

        private static Rect CalculateViewportBounds(Camera camera, Vector3[] localVertices, Transform vertexTransform, string label)
        {
            var min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            var max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            var visibleVertexCount = 0;
            var step = Mathf.Max(1, localVertices.Length / 512);

            for (var i = 0; i < localVertices.Length; i += step)
            {
                var worldPoint = vertexTransform.TransformPoint(localVertices[i]);
                var viewportPoint = camera.WorldToViewportPoint(worldPoint);
                if (viewportPoint.z <= 0f)
                {
                    continue;
                }

                var viewportPosition = new Vector2(viewportPoint.x, viewportPoint.y);
                min = Vector2.Min(min, viewportPosition);
                max = Vector2.Max(max, viewportPosition);
                visibleVertexCount++;
            }

            if (visibleVertexCount == 0)
            {
                throw new InvalidOperationException("Stage 3 first-person mesh is behind the camera: " + label);
            }

            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private static Rect CalculateViewportBounds(Camera camera, Bounds bounds, string label)
        {
            var extents = bounds.extents;
            var min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            var max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            var visibleCornerCount = 0;

            for (var x = -1; x <= 1; x += 2)
            {
                for (var y = -1; y <= 1; y += 2)
                {
                    for (var z = -1; z <= 1; z += 2)
                    {
                        var corner = bounds.center + new Vector3(extents.x * x, extents.y * y, extents.z * z);
                        var viewportPoint = camera.WorldToViewportPoint(corner);
                        if (viewportPoint.z <= 0f)
                        {
                            continue;
                        }

                        var viewportPosition = new Vector2(viewportPoint.x, viewportPoint.y);
                        min = Vector2.Min(min, viewportPosition);
                        max = Vector2.Max(max, viewportPosition);
                        visibleCornerCount++;
                    }
                }
            }

            if (visibleCornerCount == 0)
            {
                throw new InvalidOperationException("Stage 3 first-person renderer is behind the camera: " + label);
            }

            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private static void AssertWeaponVisualState(
            FirstPersonEquipmentVisualController visualController,
            bool expectStick,
            bool expectMusket,
            bool expectSuitReadout,
            string label)
        {
            if (visualController.StickVisual.activeSelf != expectStick)
            {
                throw new InvalidOperationException("Stage 3 stick/crowbar visual active state is wrong for " + label + ".");
            }

            if (visualController.MusketVisual.activeSelf != expectMusket)
            {
                throw new InvalidOperationException("Stage 3 musket visual active state is wrong for " + label + ".");
            }

            if (!expectMusket && visualController.MusketVisual.activeInHierarchy)
            {
                throw new InvalidOperationException("Stage 3 musket visual is visible before Musket is the active hand item.");
            }

            if (visualController.ProtectiveSuitReadout.activeSelf != expectSuitReadout)
            {
                throw new InvalidOperationException("Stage 3 protective suit readout active state is wrong for " + label + ".");
            }
        }

        private static void AssertFirstPersonBounds(GameObject preview, Transform cameraTransform)
        {
            var renderers = preview.GetComponentsInChildren<MeshRenderer>(true);
            for (var i = 0; i < renderers.Length; i++)
            {
                if (!renderers[i].gameObject.activeInHierarchy)
                {
                    continue;
                }

                var center = cameraTransform.InverseTransformPoint(renderers[i].bounds.center);
                if (center.z < 0.45f || center.z > 1.8f)
                {
                    throw new InvalidOperationException(
                        "Stage 3 first-person equipment preview must stay inside a readable camera-space depth. Object=" +
                        renderers[i].name +
                        ", LocalZ=" +
                        center.z.ToString("0.00"));
                }

                if (Mathf.Abs(center.x) < 0.18f && center.y > -0.22f)
                {
                    throw new InvalidOperationException(
                        "Stage 3 first-person equipment preview must not block the central HUD sightline. Object=" +
                        renderers[i].name +
                        ", LocalX=" +
                        center.x.ToString("0.00") +
                        ", LocalY=" +
                        center.y.ToString("0.00"));
                }
            }
        }

        private static void AssertGeneratedLabelsAreSubtle()
        {
            var activeScene = SceneManager.GetActiveScene();
            var textMeshes = Resources.FindObjectsOfTypeAll<TextMesh>();
            for (var i = 0; i < textMeshes.Length; i++)
            {
                var textMesh = textMeshes[i];
                if (textMesh == null ||
                    textMesh.gameObject.scene != activeScene ||
                    (!textMesh.name.StartsWith("Label - ", StringComparison.Ordinal) &&
                     !textMesh.name.StartsWith("Sign - ", StringComparison.Ordinal)))
                {
                    continue;
                }

                if (textMesh.characterSize > 0.03f || textMesh.fontSize > 32 || textMesh.color.a > 0.6f)
                {
                    throw new InvalidOperationException(
                        "Production room labels must stay subtle after Stage 3 art integration. Label=" +
                        textMesh.name +
                        ", CharacterSize=" +
                        textMesh.characterSize.ToString("0.000") +
                        ", FontSize=" +
                        textMesh.fontSize +
                        ", Alpha=" +
                        textMesh.color.a.ToString("0.00"));
                }
            }
        }

        private static void AssertEquipmentDefinitions()
        {
            AssertEquipmentDefinition(EquipmentItemKind.Stick, "Stick", EquipmentStorageTarget.HandFirst);
            AssertEquipmentDefinition(EquipmentItemKind.Musket, "Musket", EquipmentStorageTarget.HandFirst);
            AssertEquipmentDefinition(EquipmentItemKind.BasicProtectiveSuit, "Basic Protective Suit", EquipmentStorageTarget.SupplyOnly);
            AssertEquipmentDefinition(EquipmentItemKind.PresenceDetector, "Presence Detector", EquipmentStorageTarget.SupplyOnly);
            AssertEquipmentDefinition(EquipmentItemKind.LightBlade, "Light Blade", EquipmentStorageTarget.HandFirst);
            AssertEquipmentDefinition(EquipmentItemKind.ElectricMine, "Electric Mine", EquipmentStorageTarget.HandFirst);
            AssertEquipmentDefinition(EquipmentItemKind.CorridorPurifier, "Corridor Purifier", EquipmentStorageTarget.SupplyOnly);
        }

        private static void AssertEquipmentDefinition(
            EquipmentItemKind itemKind,
            string displayName,
            EquipmentStorageTarget storageTarget)
        {
            var definition = EquipmentRules.GetDefinition(itemKind);
            if (definition.DisplayName != displayName ||
                definition.StorageTarget != storageTarget ||
                definition.PriceCredits < 0)
            {
                throw new InvalidOperationException("Stage 3 equipment prop validation found drift in equipment definition: " + itemKind);
            }
        }

        private static void AssertPresentationObject(string objectName, int minChildRenderers)
        {
            var target = RequireObject(objectName);
            AssertRendererCount(target, minChildRenderers, objectName);
            AssertMaterials(target, objectName);
        }

        private static int AssertRendererCount(GameObject target, int minChildRenderers, string label)
        {
            var renderers = target.GetComponentsInChildren<MeshRenderer>(true);
            if (renderers.Length < minChildRenderers)
            {
                throw new InvalidOperationException(label + " must contain at least " + minChildRenderers + " renderers. Actual=" + renderers.Length);
            }

            return renderers.Length;
        }

        private static void AssertMaterials(GameObject target, string label)
        {
            var renderers = target.GetComponentsInChildren<MeshRenderer>(true);
            for (var i = 0; i < renderers.Length; i++)
            {
                if (renderers[i].sharedMaterial == null)
                {
                    throw new InvalidOperationException(label + " renderer is missing a material: " + renderers[i].name);
                }

                AssertSupportedMaterialShader(renderers[i].sharedMaterial, label + " renderer " + renderers[i].name);
            }
        }

        private static GameObject RequireObject(string objectName)
        {
            var target = GameObject.Find(objectName);
            if (target == null)
            {
                target = FindObjectInLoadedScene(objectName);
            }

            if (target == null)
            {
                throw new InvalidOperationException("Missing post-detailed stage 3 gameplay prop object: " + objectName);
            }

            return target;
        }

        private static void RequireMissingObject(string objectName)
        {
            if (GameObject.Find(objectName) != null || FindObjectInLoadedScene(objectName) != null)
            {
                throw new InvalidOperationException("Post-detailed stage 3 must not contain removed sample element: " + objectName);
            }
        }

        private static GameObject FindObjectInLoadedScene(string objectName)
        {
            var scene = SceneManager.GetActiveScene();
            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                var found = FindChildRecursive(roots[i].transform, objectName);
                if (found != null)
                {
                    return found.gameObject;
                }
            }

            return null;
        }

        private static Transform FindChildRecursive(Transform parent, string objectName)
        {
            if (parent.name == objectName)
            {
                return parent;
            }

            for (var i = 0; i < parent.childCount; i++)
            {
                var found = FindChildRecursive(parent.GetChild(i), objectName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static void AssertStage3MaterialAssets()
        {
            AssertMaterialAsset(PostDetailedStage3GameplayPropsBootstrap.MetalMaterialPath);
            AssertMaterialAsset(PostDetailedStage3GameplayPropsBootstrap.DarkRubberMaterialPath);
            AssertMaterialAsset(PostDetailedStage3GameplayPropsBootstrap.ScreenMaterialPath);
            AssertMaterialAsset(PostDetailedStage3GameplayPropsBootstrap.WarningMaterialPath);
            AssertMaterialAsset(PostDetailedStage3GameplayPropsBootstrap.YellowMaterialPath);
            AssertMaterialAsset(PostDetailedStage3GameplayPropsBootstrap.WoodMaterialPath);
            AssertMaterialAsset(PostDetailedStage3GameplayPropsBootstrap.CrowbarSteelMaterialPath);
            AssertMaterialAsset(PostDetailedStage3GameplayPropsBootstrap.CargoMaterialPath);
            AssertMaterialAsset(PostDetailedStage3GameplayPropsBootstrap.DamagedMaterialPath);
            AssertMaterialAsset(PostDetailedStage3GameplayPropsBootstrap.LightMaterialPath);
            AssertMaterialAsset(PostDetailedStage3GameplayPropsBootstrap.WarmLightMaterialPath);
        }

        private static void AssertStage3BlenderReworkAssets()
        {
            AssertGeneratedAssetFile(Stage3BlenderReviewAssetBuilder.BlenderSourcePath);

            var fbxPaths = Stage3BlenderReviewAssetBuilder.GetRequiredFbxPaths();
            for (var i = 0; i < fbxPaths.Length; i++)
            {
                AssertGeneratedAssetFile(fbxPaths[i]);
                if (AssetDatabase.LoadAssetAtPath<GameObject>(fbxPaths[i]) == null)
                {
                    throw new InvalidOperationException("Stage 3 Blender FBX must import as a Unity model asset: " + fbxPaths[i]);
                }
            }

            var texturePaths = Stage3BlenderReviewAssetBuilder.GetRequiredTexturePaths();
            for (var i = 0; i < texturePaths.Length; i++)
            {
                AssertTextureAsset(texturePaths[i]);
            }

            AssertBlenderMeshSources(RequireObject(PostDetailedStage3GameplayPropsBootstrap.CockpitHelmPropName), "cockpit helm");
            AssertBlenderMeshSources(RequireObject(PostDetailedStage3GameplayPropsBootstrap.CockpitStatusScreensName), "cockpit status screens");
            AssertBlenderMeshSources(RequireObject(PostDetailedStage3GameplayPropsBootstrap.ControlRoomCctvTerminalName), "control-room CCTV set");
            AssertBlenderMeshSources(RequireObject(PostDetailedStage3GameplayPropsBootstrap.EngineRoomPowerTerminalName), "engine-room power terminal");
            AssertBlenderMeshSources(RequireObject(PostDetailedStage3GameplayPropsBootstrap.SupplyRoomStorageCabinetName), "supply-room storage cabinet");
            AssertBlenderMeshSources(RequireObject(PostDetailedStage3GameplayPropsBootstrap.CargoHoldStatusPanelName), "cargo-hold status panel");
            AssertBlenderMeshSources(RequireObject(PostDetailedStage3GameplayPropsBootstrap.ContractCargoContainerName), "contract cargo container");
            AssertBlenderMeshSources(RequireObject(PostDetailedStage3GameplayPropsBootstrap.PersonalCargoContainerName), "personal cargo container");
            AssertBlenderMeshSources(RequireObject(PostDetailedStage3GameplayPropsBootstrap.ArmoryTurretGripMountName), "armory turret grip mount");
            AssertBlenderMeshSources(RequireObject(PostDetailedStage3GameplayPropsBootstrap.DiegeticTerminalShellName), "diegetic cargo terminal");
            AssertBlenderMeshSources(RequireObject(PostDetailedStage3GameplayPropsBootstrap.CrowbarModelName), "first-person hooked stick");
            AssertBlenderMeshSources(RequireObject(PostDetailedStage3GameplayPropsBootstrap.RoomDressingRootName), "art-sample room dressing");

            AssertTexturedMaterialAsset(PostDetailedStage3GameplayPropsBootstrap.MetalMaterialPath);
            AssertTexturedMaterialAsset(PostDetailedStage3GameplayPropsBootstrap.DarkRubberMaterialPath);
            AssertTexturedMaterialAsset(PostDetailedStage3GameplayPropsBootstrap.ScreenMaterialPath);
            AssertTexturedMaterialAsset(PostDetailedStage3GameplayPropsBootstrap.WarningMaterialPath);
            AssertTexturedMaterialAsset(PostDetailedStage3GameplayPropsBootstrap.YellowMaterialPath);
            AssertTexturedMaterialAsset(PostDetailedStage3GameplayPropsBootstrap.WoodMaterialPath);
            AssertTexturedMaterialAsset(PostDetailedStage3GameplayPropsBootstrap.CrowbarSteelMaterialPath);
            AssertTexturedMaterialAsset(PostDetailedStage3GameplayPropsBootstrap.CargoMaterialPath);
        }

        private static void AssertGeneratedAssetFile(string path)
        {
            var projectRoot = Directory.GetParent(Application.dataPath);
            if (projectRoot == null)
            {
                throw new InvalidOperationException("Could not resolve project root for Stage 3 Blender asset validation.");
            }

            var fullPath = Path.Combine(projectRoot.FullName, path.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(fullPath))
            {
                throw new InvalidOperationException("Missing Stage 3 Blender-generated asset file: " + path);
            }
        }

        private static void AssertBlenderMeshSources(GameObject root, string label)
        {
            var meshFilters = root.GetComponentsInChildren<MeshFilter>(true);
            if (meshFilters.Length == 0)
            {
                throw new InvalidOperationException("Stage 3 " + label + " must contain Blender-generated mesh filters.");
            }

            for (var i = 0; i < meshFilters.Length; i++)
            {
                var mesh = meshFilters[i].sharedMesh;
                var meshPath = mesh == null ? string.Empty : AssetDatabase.GetAssetPath(mesh).Replace('\\', '/');
                if (string.IsNullOrEmpty(meshPath) ||
                    !meshPath.StartsWith(Stage3BlenderReviewAssetBuilder.FbxDirectory + "/", StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "Stage 3 " +
                        label +
                        " must use Blender-authored FBX mesh assets, not Unity primitive or scene-only mesh assets: " +
                        meshFilters[i].name);
                }
            }
        }

        private static void AssertMeshAsset(string path)
        {
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null)
            {
                throw new InvalidOperationException("Missing Stage 3 rework mesh asset: " + path);
            }

            if (path.IndexOf("HookedStick_Body", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                if (mesh.vertexCount < 250 || mesh.triangles.Length < 1500)
                {
                    throw new InvalidOperationException("Stage 3 hooked stick body mesh asset is too coarse: " + path);
                }
            }
            else if (mesh.vertexCount < 8 || mesh.triangles.Length < 12)
            {
                throw new InvalidOperationException("Stage 3 rework mesh asset is empty or too coarse: " + path);
            }
        }

        private static void AssertTextureAsset(string path)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null)
            {
                throw new InvalidOperationException("Missing Stage 3 rework texture asset: " + path);
            }

            if (texture.width < 64 || texture.height < 64)
            {
                throw new InvalidOperationException("Stage 3 rework texture asset is below the minimum readable review size: " + path);
            }
        }

        private static void AssertTexturedMaterialAsset(string path)
        {
            AssertMaterialAsset(path);
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            var texture = material == null ? null : material.mainTexture;
            if (texture == null && material != null && material.HasProperty("_BaseMap"))
            {
                texture = material.GetTexture("_BaseMap");
            }

            if (texture == null)
            {
                throw new InvalidOperationException("Stage 3 rework material must reference a generated texture: " + path);
            }
        }

        private static void AssertApprovedArtSampleAlignment()
        {
            var projectRoot = Directory.GetParent(Application.dataPath);
            if (projectRoot == null)
            {
                throw new InvalidOperationException("Could not resolve project root for Stage 3 art sample validation.");
            }

            var sampleRoot = Path.Combine(projectRoot.FullName, "artSample", "stage3_rework_review");
            var samplePath = Path.Combine(sampleRoot, "index.html");
            if (!File.Exists(samplePath))
            {
                throw new InvalidOperationException("Missing approved Stage 3 art sample: " + samplePath);
            }

            var sample = File.ReadAllText(samplePath);
            RequireSampleText(sample, "Stage 3", samplePath);
            RequireSampleText(sample, "01_cockpit_helm_and_status_review.png", samplePath);
            RequireSampleText(sample, "02_control_room_cctv_terminal_review.png", samplePath);
            RequireSampleText(sample, "03_engine_room_power_terminal_review.png", samplePath);
            RequireSampleText(sample, "04_supply_room_storage_cabinet_review.png", samplePath);
            RequireSampleText(sample, "05_cargo_hold_props_and_terminal_review.png", samplePath);
            RequireSampleText(sample, "06_armory_turret_grip_mount_review.png", samplePath);
            RequireSampleText(sample, "07_first_person_equipment_review.png", samplePath);
            RequireSampleText(sample, "통제실 단일 대형 CCTV 스크린", samplePath);
            RequireSampleText(sample, "빠루처럼 굽은 갈고리형 프라이 팁", samplePath);
            RequireSampleText(sample, "양손 막대기", samplePath);

            RequireArtSampleFile(sampleRoot, "01_cockpit_helm_and_status_review.png");
            RequireArtSampleFile(sampleRoot, "02_control_room_cctv_terminal_review.png");
            RequireArtSampleFile(sampleRoot, "03_engine_room_power_terminal_review.png");
            RequireArtSampleFile(sampleRoot, "04_supply_room_storage_cabinet_review.png");
            RequireArtSampleFile(sampleRoot, "05_cargo_hold_props_and_terminal_review.png");
            RequireArtSampleFile(sampleRoot, "06_armory_turret_grip_mount_review.png");
            RequireArtSampleFile(sampleRoot, "07_first_person_equipment_review.png");

            RequireObject(PostDetailedStage3GameplayPropsBootstrap.CrowbarModelName);
            RequireObject(PostDetailedStage3GameplayPropsBootstrap.MusketModelName);
            RequireObject(PostDetailedStage3GameplayPropsBootstrap.CockpitHelmPropName);
            RequireObject(PostDetailedStage3GameplayPropsBootstrap.CockpitStatusScreensName);
            RequireObject(PostDetailedStage3GameplayPropsBootstrap.ControlRoomCctvTerminalName);
            RequireObject(PostDetailedStage3GameplayPropsBootstrap.EngineRoomPowerTerminalName);
            RequireObject(PostDetailedStage3GameplayPropsBootstrap.CargoHoldStatusPanelName);
            RequireObject(PostDetailedStage3GameplayPropsBootstrap.SupplyRoomStorageCabinetName);
            RequireObject(PostDetailedStage3GameplayPropsBootstrap.ArmoryTurretGripMountName);
            RequireObject(PostDetailedStage3GameplayPropsBootstrap.RoomDressingRootName);
            RequireObject(PostDetailedStage3GameplayPropsBootstrap.CockpitDressingName);
            RequireObject(PostDetailedStage3GameplayPropsBootstrap.ControlRoomDressingName);
            RequireObject(PostDetailedStage3GameplayPropsBootstrap.EngineRoomDressingName);
            RequireObject(PostDetailedStage3GameplayPropsBootstrap.SupplyRoomDressingName);
            RequireObject(PostDetailedStage3GameplayPropsBootstrap.CargoHoldDressingName);
            RequireObject(PostDetailedStage3GameplayPropsBootstrap.ArmoryDressingName);
            RequireObject(PostDetailedStage3GameplayPropsBootstrap.CargoStartCorridorDressingName);
            RequireObject(PostDetailedStage3GameplayPropsBootstrap.ContractCargoContainerName);
            RequireObject(PostDetailedStage3GameplayPropsBootstrap.PersonalCargoContainerName);
            RequireObject(PostDetailedStage3GameplayPropsBootstrap.ContractCargoStrapHorizontalName);
            RequireObject(PostDetailedStage3GameplayPropsBootstrap.ContractCargoStrapVerticalName);
            RequireObject(PostDetailedStage3GameplayPropsBootstrap.DiegeticTerminalShellName);

            RequireMissingObject(PostDetailedStage3GameplayPropsBootstrap.RepairPanelKitName);
            RequireMissingObject(PostDetailedStage3GameplayPropsBootstrap.DamagedPanelKitName);
            RequireMissingObject(PostDetailedStage3GameplayPropsBootstrap.EscapePodVisualName);
            RequireMissingObject(PostDetailedStage3GameplayPropsBootstrap.SpecialEquipmentRootName);
            RequireMissingObject(PostDetailedStage3GameplayPropsBootstrap.PresenceDetectorPropName);
            RequireMissingObject(PostDetailedStage3GameplayPropsBootstrap.LightBladePropName);
            RequireMissingObject(PostDetailedStage3GameplayPropsBootstrap.ElectricMinePropName);
            RequireMissingObject(PostDetailedStage3GameplayPropsBootstrap.CorridorPurifierIconName);
            RequireMissingObject(PostDetailedStage3GameplayPropsBootstrap.NormalMaterialVariantPanelName);
            RequireMissingObject(PostDetailedStage3GameplayPropsBootstrap.DamagedMaterialVariantPanelName);
        }

        private static void RequireSampleText(string sample, string requiredText, string samplePath)
        {
            if (sample.IndexOf(requiredText, StringComparison.OrdinalIgnoreCase) < 0)
            {
                throw new InvalidOperationException(
                    "Approved Stage 3 art sample is missing required comparison anchor '" +
                    requiredText +
                    "': " +
                    samplePath);
            }
        }

        private static void RequireArtSampleFile(string sampleRoot, string fileName)
        {
            var path = Path.Combine(sampleRoot, fileName);
            if (!File.Exists(path))
            {
                throw new InvalidOperationException("Missing Stage 3 approved rework sample file: " + path);
            }
        }

        private static void AssertMaterialAsset(string path)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                throw new InvalidOperationException("Missing post-detailed stage 3 material asset: " + path);
            }

            AssertSupportedMaterialShader(material, path);
        }

        private static void AssertSupportedMaterialShader(Material material, string label)
        {
            if (material.shader == null)
            {
                throw new InvalidOperationException("Stage 3 material has no shader: " + label);
            }

            var shaderName = material.shader.name;
            if (shaderName == "Standard" || shaderName == "Hidden/InternalErrorShader")
            {
                throw new InvalidOperationException(
                    "Stage 3 material must use a URP-compatible shader, not " + shaderName + ": " + label);
            }
        }
    }
}
