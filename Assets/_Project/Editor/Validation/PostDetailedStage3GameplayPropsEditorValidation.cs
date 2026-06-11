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

            var stageRoot = RequireObject(PostDetailedStage3GameplayPropsBootstrap.Stage3RootName);
            var worldRendererCount = AssertRendererCount(stageRoot, 60, "stage 3 world prop root");
            AssertMaterials(stageRoot, "stage 3 world prop root");

            AssertPresentationObject(PostDetailedStage3GameplayPropsBootstrap.CockpitHelmPropName, 10);
            AssertPresentationObject(PostDetailedStage3GameplayPropsBootstrap.CockpitStatusScreensName, 8);
            AssertPresentationObject(PostDetailedStage3GameplayPropsBootstrap.ControlRoomCctvTerminalName, 12);
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
            AssertApprovedArtSampleAlignment();

            Debug.Log("Post-detailed stage 3 gameplay props editor validation passed.");
            Debug.Log(
                "Post-detailed stage 3 gameplay props details: WorldRenderers=" +
                worldRendererCount +
                "; FirstPersonRenderers=" +
                firstPersonRendererCount +
                "; CargoStraps=2; DeviceSurfaces=7; SampleOnlyLooseProps=0; ArtSampleMatch=True; RuntimeIntegration=True");
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
            if (Vector3.Distance(statusPanel.transform.position, statusInteractable.transform.position) > 0.32f)
            {
                throw new InvalidOperationException("Stage 3 cargo status panel must be attached to the existing cargo status interactable block.");
            }

            if (statusPanel.transform.position.z >= statusInteractable.transform.position.z)
            {
                throw new InvalidOperationException("Stage 3 cargo status panel must sit on the visible front side of the cargo status interactable.");
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
            if (renderers.Length != 1)
            {
                throw new InvalidOperationException("Stage 3 crowbar must be one continuous mesh renderer. Actual=" + renderers.Length);
            }

            if (renderers[0].name != PostDetailedStage3GameplayPropsBootstrap.CrowbarContinuousBodyName)
            {
                throw new InvalidOperationException("Stage 3 crowbar renderer must be the continuous body mesh.");
            }

            var meshFilter = renderers[0].GetComponent<MeshFilter>();
            var mesh = meshFilter == null ? null : meshFilter.sharedMesh;
            if (mesh == null || mesh.vertexCount < 250 || mesh.triangles.Length < 1500)
            {
                throw new InvalidOperationException("Stage 3 crowbar continuous mesh is too coarse to read as a smooth crowbar.");
            }

            var bounds = mesh.bounds;
            if (bounds.min.y > -0.64f || bounds.max.x < 0.11f)
            {
                throw new InvalidOperationException("Stage 3 crowbar must have an extended pointed pry tip instead of a blunt rounded end.");
            }

            var lowerTipMin = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            var lowerTipMax = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
            var lowerTipVertexCount = 0;
            var vertices = mesh.vertices;
            for (var i = 0; i < vertices.Length; i++)
            {
                if (vertices[i].y > bounds.min.y + 0.025f)
                {
                    continue;
                }

                lowerTipMin = Vector3.Min(lowerTipMin, vertices[i]);
                lowerTipMax = Vector3.Max(lowerTipMax, vertices[i]);
                lowerTipVertexCount++;
            }

            if (lowerTipVertexCount == 0 ||
                lowerTipMax.x - lowerTipMin.x > 0.035f ||
                lowerTipMax.z - lowerTipMin.z > 0.035f)
            {
                throw new InvalidOperationException("Stage 3 crowbar lower tip must taper to a sharp weapon-like point.");
            }

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
            var renderer = stickVisual.GetComponentInChildren<MeshRenderer>(false);
            if (renderer == null)
            {
                throw new InvalidOperationException("Stage 3 stick/crowbar visual is active but has no active renderer.");
            }

            var viewportBounds = CalculateViewportBounds(camera, renderer, renderer.name);
            var center = viewportBounds.center;
            if (viewportBounds.width < 0.035f || viewportBounds.height < 0.16f)
            {
                throw new InvalidOperationException(
                    "Stage 3 crowbar is too small or edge-on in the first-person camera. ViewportSize=" +
                    viewportBounds.width.ToString("0.00") +
                    "x" +
                    viewportBounds.height.ToString("0.00"));
            }

            if (center.x < 0.5f || center.x > 0.88f || center.y < 0.16f || center.y > 0.62f)
            {
                throw new InvalidOperationException(
                    "Stage 3 crowbar must sit in the lower-right first-person equipment band. ViewportCenter=" +
                    center.x.ToString("0.00") +
                    "," +
                    center.y.ToString("0.00"));
            }

            if (viewportBounds.xMax > 0.9f && viewportBounds.yMin < 0.32f)
            {
                throw new InvalidOperationException(
                    "Stage 3 crowbar overlaps the minimap-safe corner too strongly. ViewportBounds=" +
                    viewportBounds.xMin.ToString("0.00") +
                    "," +
                    viewportBounds.yMin.ToString("0.00") +
                    "-" +
                    viewportBounds.xMax.ToString("0.00") +
                    "," +
                    viewportBounds.yMax.ToString("0.00"));
            }
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
        }

        private static void AssertApprovedArtSampleAlignment()
        {
            var projectRoot = Directory.GetParent(Application.dataPath);
            if (projectRoot == null)
            {
                throw new InvalidOperationException("Could not resolve project root for Stage 3 art sample validation.");
            }

            var samplePath = Path.Combine(projectRoot.FullName, "artSample", "gameplay_props_equipment_sample.html");
            if (!File.Exists(samplePath))
            {
                throw new InvalidOperationException("Missing approved Stage 3 art sample: " + samplePath);
            }

            var sample = File.ReadAllText(samplePath);
            RequireSampleText(sample, "First-Person Equipment", samplePath);
            RequireSampleText(sample, "Ship Device Prop Family", samplePath);
            RequireSampleText(sample, "Cargo Kit Preview", samplePath);
            RequireSampleText(sample, "Special Equipment Silhouette Direction", samplePath);
            RequireSampleText(sample, "Contract Cargo Container", samplePath);
            RequireSampleText(sample, "Personal Cargo Container", samplePath);
            RequireSampleText(sample, "Strap And Bracket Set", samplePath);
            RequireSampleText(sample, "Presence Detector", samplePath);
            RequireSampleText(sample, "Light Blade", samplePath);
            RequireSampleText(sample, "Electric Mine", samplePath);
            RequireSampleText(sample, "Corridor Purifier", samplePath);
            RequireSampleText(sample, "Approval Checklist", samplePath);

            RequireReferenceFile(projectRoot.FullName, "crowbar_reference.jfif");
            RequireReferenceFile(projectRoot.FullName, "musket_reference.png");

            RequireObject(PostDetailedStage3GameplayPropsBootstrap.CrowbarModelName);
            RequireObject(PostDetailedStage3GameplayPropsBootstrap.MusketModelName);
            RequireObject(PostDetailedStage3GameplayPropsBootstrap.CockpitHelmPropName);
            RequireObject(PostDetailedStage3GameplayPropsBootstrap.CockpitStatusScreensName);
            RequireObject(PostDetailedStage3GameplayPropsBootstrap.ControlRoomCctvTerminalName);
            RequireObject(PostDetailedStage3GameplayPropsBootstrap.EngineRoomPowerTerminalName);
            RequireObject(PostDetailedStage3GameplayPropsBootstrap.CargoHoldStatusPanelName);
            RequireObject(PostDetailedStage3GameplayPropsBootstrap.SupplyRoomStorageCabinetName);
            RequireObject(PostDetailedStage3GameplayPropsBootstrap.ArmoryTurretGripMountName);
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

        private static void RequireReferenceFile(string projectRoot, string fileName)
        {
            var path = Path.Combine(projectRoot, "artSample", "refs", fileName);
            if (!File.Exists(path))
            {
                throw new InvalidOperationException("Missing Stage 3 art sample reference file: " + path);
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
