using System;
using System.IO;
using System.Linq;
using Bellerophon.Core.Player;
using Bellerophon.Core.Session;
using Bellerophon.Core.Ship;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Bellerophon.Editor.Validation
{
    public static class Phase16HudMapAtmosphereBootstrap
    {
        public const string CargoRunScenePath = Phase15EquipmentLoopBootstrap.CargoRunScenePath;
        public const string Phase16RootName = "Phase 16 HUD Map Atmosphere";
        public const string Phase16UiRootName = "Phase 16 HUD Root";
        public const string VitalRootName = "Phase 16 Vitals";
        public const string HealthBarFillName = "Phase 16 Health Fill";
        public const string ShieldBarFillName = "Phase 16 Shield Fill";
        public const string StatusEffectsTextName = "Phase 16 Status Effects";
        public const string MapRootName = "Phase 16 Ship Map";
        public const string MapCurrentRoomTextName = "Phase 16 Map Current Room";
        public const string MapCurrentRoomMarkerName = "Phase 16 Current Room Marker";
        public const string AtmosphereControllerName = "Phase 16 Atmosphere Controller";
        public const string AudioHooksName = "Phase 16 Signal Audio Hooks";

        private const string ShipSettingsDirectory = "Assets/_Project/Settings/Ship";
        private const string ShipArtMaterialsDirectory = "Assets/_Project/Art/Ship/Materials";
        private const string GrayboxFloorMaterialPath = ShipSettingsDirectory + "/GrayboxFloorMaterial.mat";
        private const string GrayboxCorridorMaterialPath = ShipSettingsDirectory + "/GrayboxCorridorMaterial.mat";
        private const string GrayboxWallMaterialPath = ShipSettingsDirectory + "/GrayboxWallMaterial.mat";
        private const string GrayboxConsoleMaterialPath = ShipSettingsDirectory + "/GrayboxConsoleMaterial.mat";
        private const string GrayboxCargoMaterialPath = ShipSettingsDirectory + "/GrayboxCargoMaterial.mat";
        private const string GrayboxInteractableMaterialPath = ShipSettingsDirectory + "/GrayboxInteractableMaterial.mat";
        private const string ProductionFloorMaterialPath = ShipArtMaterialsDirectory + "/ShipInteriorFloor_Rough.mat";
        private const string ProductionCorridorMaterialPath = ShipArtMaterialsDirectory + "/ShipInteriorCorridorFloor_Rough.mat";
        private const string ProductionWallMaterialPath = ShipArtMaterialsDirectory + "/ShipInteriorWall_Rough.mat";
        private const string ProductionCeilingMaterialPath = ShipArtMaterialsDirectory + "/ShipInteriorCeiling_Rough.mat";
        private const string ProductionDoorFrameMaterialPath = ShipArtMaterialsDirectory + "/ShipInteriorDoorFrame_Worn.mat";
        private const string ProductionCableMaterialPath = ShipArtMaterialsDirectory + "/ShipInteriorCableTray_Dark.mat";
        private const string ProductionDamageMaterialPath = ShipArtMaterialsDirectory + "/ShipInteriorDamageState_Warning.mat";
        private const string ProductionGlassMaterialPath = ShipArtMaterialsDirectory + "/CockpitGlass_Dirty.mat";
        private const string ProductionConsoleMaterialPath = ShipArtMaterialsDirectory + "/ShipInteriorConsole_Aged.mat";
        private const string ProductionCargoMaterialPath = ShipArtMaterialsDirectory + "/ShipInteriorCargo_Worn.mat";
        private const string ProductionInteractableMaterialPath = ShipArtMaterialsDirectory + "/ShipInteriorInteractable_WornYellow.mat";

        [MenuItem("Bellerophon/Bootstrap/Ensure Phase 16 HUD Map Atmosphere")]
        public static void EnsurePhase16Assets()
        {
            Phase15EquipmentLoopBootstrap.EnsurePhase15Assets();

            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            DeleteGeneratedObject(Phase16RootName);
            DeleteGeneratedObject(Phase16UiRootName);

            var hud = UnityEngine.Object.FindFirstObjectByType<FirstPersonHud>();
            var player = UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayerMotor>();
            var playerStatus = UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayerStatus>();
            var interaction = UnityEngine.Object.FindFirstObjectByType<FirstPersonInteractionController>();
            var deviceState = UnityEngine.Object.FindFirstObjectByType<ShipDeviceInteractionState>();
            if (hud == null || player == null || playerStatus == null || interaction == null || deviceState == null)
            {
                throw new InvalidOperationException("Phase 16 requires Phase 15 HUD, player, interaction, and ship device state.");
            }

            ApplyLowSaturationMaterials();
            DisableDefaultCrosshair(hud.transform);

            var root = new GameObject(Phase16RootName);
            var uiRoot = CreateUiRoot(hud.transform);
            var vitals = CreateVitals(uiRoot.transform, hud, playerStatus, interaction);
            var map = CreateMap(uiRoot.transform, player.transform, deviceState);
            var atmosphere = CreateAtmosphere(root.transform);
            var audioHooks = CreateAudioHooks(root.transform);

            hud.Configure(
                playerStatus,
                vitals.HealthText,
                vitals.ShieldText,
                interaction,
                FindText(hud.transform, "Interaction Prompt Text"),
                vitals.HealthFill,
                vitals.ShieldFill,
                vitals.StatusText);
            map.RefreshForValidation();
            atmosphere.ApplyAtmosphere();
            audioHooks.TriggerShipInteriorHook();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, CargoRunScenePath);
            Phase16HudMapAtmosphereEditorValidation.Run();

            if (!Application.isBatchMode)
            {
                EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Phase 16 HUD map atmosphere assets are ready.");
        }

        private static GameObject CreateUiRoot(Transform hudTransform)
        {
            var root = new GameObject(Phase16UiRootName, typeof(RectTransform));
            root.transform.SetParent(hudTransform, false);

            var rectTransform = root.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = Vector2.zero;
            return root;
        }

        private static VitalHudRefs CreateVitals(
            Transform parent,
            FirstPersonHud hud,
            FirstPersonPlayerStatus status,
            FirstPersonInteractionController interaction)
        {
            var root = new GameObject(VitalRootName, typeof(RectTransform));
            root.transform.SetParent(parent, false);

            var rectTransform = root.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.zero;
            rectTransform.pivot = Vector2.zero;
            rectTransform.anchoredPosition = new Vector2(24f, 26f);
            rectTransform.sizeDelta = new Vector2(350f, 94f);

            var shieldBackground = CreateImage(
                "Phase 16 Shield Background",
                root.transform,
                new Vector2(84f, 58f),
                new Vector2(252f, 24f),
                new Color(0.025f, 0.036f, 0.036f, 0.96f));
            AnchorRectToBottomLeftCenter(shieldBackground.GetComponent<RectTransform>());
            var shieldFill = CreateImage(
                ShieldBarFillName,
                shieldBackground.transform,
                Vector2.zero,
                new Vector2(252f, 24f),
                new Color(0.05f, 0.78f, 0.58f, 0.86f));
            ConfigureFill(shieldFill);

            var healthBackground = CreateImage(
                "Phase 16 Health Background",
                root.transform,
                new Vector2(84f, 24f),
                new Vector2(252f, 24f),
                new Color(0.04f, 0.024f, 0.024f, 0.96f));
            AnchorRectToBottomLeftCenter(healthBackground.GetComponent<RectTransform>());
            var healthFill = CreateImage(
                HealthBarFillName,
                healthBackground.transform,
                Vector2.zero,
                new Vector2(252f, 24f),
                new Color(0.75f, 0.08f, 0.07f, 0.88f));
            ConfigureFill(healthFill);

            DisableLegacyText(hud.ShieldText);
            DisableLegacyText(hud.HealthText);

            var shieldText = ReparentOrCreateText(
                null,
                "Phase 16 Shield Percent",
                root.transform,
                new Vector2(4f, 58f),
                new Vector2(72f, 26f),
                18,
                TextAnchor.MiddleRight);
            var healthText = ReparentOrCreateText(
                null,
                "Phase 16 Health Percent",
                root.transform,
                new Vector2(4f, 24f),
                new Vector2(72f, 26f),
                18,
                TextAnchor.MiddleRight);
            var statusText = ReparentOrCreateText(
                null,
                StatusEffectsTextName,
                root.transform,
                new Vector2(84f, 90f),
                new Vector2(252f, 26f),
                15,
                TextAnchor.MiddleLeft);

            hud.Configure(
                status,
                healthText,
                shieldText,
                interaction,
                FindText(hud.transform, "Interaction Prompt Text"),
                healthFill,
                shieldFill,
                statusText);

            return new VitalHudRefs(healthText, shieldText, statusText, healthFill, shieldFill);
        }

        private static ShipInteriorMapHud CreateMap(
            Transform parent,
            Transform player,
            ShipDeviceInteractionState deviceState)
        {
            var rootImage = CreateImage(
                MapRootName,
                parent,
                new Vector2(-24f, 26f),
                new Vector2(374f, 300f),
                new Color(0.018f, 0.025f, 0.026f, 0.94f));
            var mapRoot = rootImage.GetComponent<RectTransform>();
            mapRoot.anchorMin = new Vector2(1f, 0f);
            mapRoot.anchorMax = new Vector2(1f, 0f);
            mapRoot.pivot = new Vector2(1f, 0f);
            mapRoot.localScale = new Vector3(
                ShipInteriorMapRules.ShipInteriorMapScale,
                ShipInteriorMapRules.ShipInteriorMapScale,
                1f);

            var title = CreateText(
                "Phase 16 Map Title",
                mapRoot,
                new Vector2(0f, 132f),
                new Vector2(330f, 28f),
                20,
                TextAnchor.MiddleCenter);
            var currentRoomText = CreateText(
                MapCurrentRoomTextName,
                mapRoot,
                new Vector2(0f, -132f),
                new Vector2(330f, 28f),
                17,
                TextAnchor.MiddleCenter);

            CreateMapLine(mapRoot, ShipRoomId.CargoHold, ShipRoomId.Cockpit);
            CreateMapLine(mapRoot, ShipRoomId.CargoHold, ShipRoomId.EngineRoom);
            CreateMapLine(mapRoot, ShipRoomId.CargoHold, ShipRoomId.ControlRoom);
            CreateMapLine(mapRoot, ShipRoomId.CargoHold, ShipRoomId.Armory);
            CreateMapLine(mapRoot, ShipRoomId.CargoHold, ShipRoomId.SupplyRoom);
            CreateMapLine(mapRoot, ShipRoomId.SupplyRoom, ShipRoomId.Armory);
            CreateMapLine(mapRoot, ShipRoomId.Cockpit, ShipRoomId.EngineRoom);
            CreateMapLine(mapRoot, ShipRoomId.Cockpit, ShipRoomId.ControlRoom);
            CreateMapLine(mapRoot, ShipRoomId.EngineRoom, ShipRoomId.ControlRoom);

            var cockpit = CreateMapRoom(mapRoot, ShipRoomId.Cockpit);
            var cargoHold = CreateMapRoom(mapRoot, ShipRoomId.CargoHold);
            var armory = CreateMapRoom(mapRoot, ShipRoomId.Armory);
            var supplyRoom = CreateMapRoom(mapRoot, ShipRoomId.SupplyRoom);
            var engineRoom = CreateMapRoom(mapRoot, ShipRoomId.EngineRoom);
            var controlRoom = CreateMapRoom(mapRoot, ShipRoomId.ControlRoom);

            var markerImage = CreateImage(
                MapCurrentRoomMarkerName,
                mapRoot,
                Vector2.zero,
                new Vector2(40f, 40f),
                new Color(0.1f, 0.9f, 0.68f, 0.22f));
            markerImage.transform.SetAsLastSibling();

            var mapHud = rootImage.gameObject.AddComponent<ShipInteriorMapHud>();
            mapHud.Configure(
                player,
                deviceState,
                mapRoot,
                title,
                currentRoomText,
                markerImage.GetComponent<RectTransform>(),
                cockpit,
                cargoHold,
                armory,
                supplyRoom,
                engineRoom,
                controlRoom);
            return mapHud;
        }

        private static ShipInteriorAtmosphereController CreateAtmosphere(Transform parent)
        {
            var atmosphereObject = new GameObject(AtmosphereControllerName);
            atmosphereObject.transform.SetParent(parent, false);
            var atmosphere = atmosphereObject.AddComponent<ShipInteriorAtmosphereController>();
            var camera = Camera.main;
            var lights = UnityEngine.Object.FindObjectsByType<Light>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            atmosphere.Configure(camera, lights);
            return atmosphere;
        }

        private static ShipSignalAudioHooks CreateAudioHooks(Transform parent)
        {
            var audioObject = new GameObject(AudioHooksName);
            audioObject.transform.SetParent(parent, false);

            var interior = audioObject.AddComponent<AudioSource>();
            interior.playOnAwake = false;
            interior.loop = true;
            var external = audioObject.AddComponent<AudioSource>();
            external.playOnAwake = false;
            var intruder = audioObject.AddComponent<AudioSource>();
            intruder.playOnAwake = false;

            var hooks = audioObject.AddComponent<ShipSignalAudioHooks>();
            hooks.Configure(interior, external, intruder);
            return hooks;
        }

        private static Image CreateMapRoom(RectTransform parent, ShipRoomId roomId)
        {
            var room = ShipInteriorMapRules.GetRoom(roomId);
            var image = CreateImage(
                "Phase 16 Map Room - " + room.DisplayName,
                parent,
                room.MapPosition,
                room.MapSize,
                new Color(0.18f, 0.25f, 0.24f, 0.82f));
            CreateText(
                "Phase 16 Map Label - " + room.DisplayName,
                image.transform,
                Vector2.zero,
                room.MapSize,
                12,
                TextAnchor.MiddleCenter).text = room.DisplayName;
            return image;
        }

        private static void CreateMapLine(RectTransform parent, ShipRoomId fromRoom, ShipRoomId toRoom)
        {
            var from = ShipInteriorMapRules.GetRoom(fromRoom).MapPosition;
            var to = ShipInteriorMapRules.GetRoom(toRoom).MapPosition;
            var delta = to - from;
            var center = (from + to) * 0.5f;
            var length = delta.magnitude;

            var line = CreateImage(
                "Phase 16 Map Corridor - " + fromRoom + " to " + toRoom,
                parent,
                center,
                new Vector2(length, 7f),
                new Color(0.08f, 0.13f, 0.13f, 0.84f));
            line.transform.SetAsFirstSibling();
            line.GetComponent<RectTransform>().localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        }

        private static Image CreateImage(
            string name,
            Transform parent,
            Vector2 anchoredPosition,
            Vector2 size,
            Color color)
        {
            var imageObject = new GameObject(name, typeof(RectTransform));
            imageObject.transform.SetParent(parent, false);

            var rectTransform = imageObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = size;

            var image = imageObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static void AnchorRectToBottomLeftCenter(RectTransform rectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.zero;
            rectTransform.pivot = new Vector2(0f, 0.5f);
        }

        private static Text CreateText(
            string name,
            Transform parent,
            Vector2 anchoredPosition,
            Vector2 size,
            int fontSize,
            TextAnchor alignment)
        {
            var textObject = new GameObject(name, typeof(RectTransform));
            textObject.transform.SetParent(parent, false);

            var rectTransform = textObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = size;

            var label = textObject.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = fontSize;
            label.alignment = alignment;
            label.color = new Color(0.78f, 0.9f, 0.84f, 1f);
            label.raycastTarget = false;
            label.text = string.Empty;
            return label;
        }

        private static void DisableLegacyText(Text text)
        {
            if (text == null)
            {
                return;
            }

            text.text = string.Empty;
            text.enabled = false;
            text.raycastTarget = false;
        }

        private static Text ReparentOrCreateText(
            Text existing,
            string name,
            Transform parent,
            Vector2 anchoredPosition,
            Vector2 size,
            int fontSize,
            TextAnchor alignment)
        {
            var label = existing != null ? existing : CreateText(name, parent, anchoredPosition, size, fontSize, alignment);
            label.transform.SetParent(parent, false);

            var rectTransform = label.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.zero;
            rectTransform.pivot = new Vector2(0f, 0.5f);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = size;

            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = fontSize;
            label.alignment = alignment;
            label.color = new Color(0.9f, 0.95f, 0.9f, 1f);
            label.raycastTarget = false;
            label.enabled = true;
            return label;
        }

        private static void ConfigureFill(Image fill)
        {
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 1f;
            fill.raycastTarget = false;

            var rectTransform = fill.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = Vector2.zero;
        }

        private static Text FindText(Transform root, string name)
        {
            var labels = root.GetComponentsInChildren<Text>(true);
            for (var i = 0; i < labels.Length; i++)
            {
                if (labels[i].name == name)
                {
                    return labels[i];
                }
            }

            return null;
        }

        private static void DisableDefaultCrosshair(Transform hudRoot)
        {
            var labels = hudRoot.GetComponentsInChildren<Text>(true);
            for (var i = 0; i < labels.Length; i++)
            {
                if (labels[i].name != "Crosshair Text")
                {
                    continue;
                }

                labels[i].text = string.Empty;
                labels[i].enabled = false;
                labels[i].raycastTarget = false;
            }
        }

        private static void ApplyLowSaturationMaterials()
        {
            Directory.CreateDirectory(ShipSettingsDirectory);
            Directory.CreateDirectory(ShipArtMaterialsDirectory);
            SetMaterialColor(GrayboxFloorMaterialPath, new Color(0.105f, 0.122f, 0.12f, 1f));
            SetMaterialColor(GrayboxCorridorMaterialPath, new Color(0.07f, 0.086f, 0.084f, 1f));
            SetMaterialColor(GrayboxWallMaterialPath, new Color(0.16f, 0.18f, 0.18f, 1f));
            SetMaterialColor(GrayboxConsoleMaterialPath, new Color(0.035f, 0.052f, 0.052f, 1f));
            SetMaterialColor(GrayboxCargoMaterialPath, new Color(0.31f, 0.25f, 0.18f, 1f));
            SetMaterialColor(GrayboxInteractableMaterialPath, new Color(0.55f, 0.48f, 0.28f, 1f));
            SetMaterialColor(ProductionFloorMaterialPath, new Color(0.105f, 0.116f, 0.105f, 1f));
            SetMaterialColor(ProductionCorridorMaterialPath, new Color(0.075f, 0.084f, 0.078f, 1f));
            SetMaterialColor(ProductionWallMaterialPath, new Color(0.145f, 0.158f, 0.148f, 1f));
            SetMaterialColor(ProductionCeilingMaterialPath, new Color(0.075f, 0.082f, 0.075f, 1f));
            SetMaterialColor(ProductionDoorFrameMaterialPath, new Color(0.22f, 0.215f, 0.18f, 1f));
            SetMaterialColor(ProductionCableMaterialPath, new Color(0.032f, 0.034f, 0.03f, 1f));
            SetMaterialColor(ProductionDamageMaterialPath, new Color(0.5f, 0.19f, 0.06f, 1f));
            SetMaterialColor(ProductionGlassMaterialPath, new Color(0.075f, 0.16f, 0.18f, 0.55f));
            SetMaterialColor(ProductionConsoleMaterialPath, new Color(0.028f, 0.043f, 0.04f, 1f));
            SetMaterialColor(ProductionCargoMaterialPath, new Color(0.26f, 0.21f, 0.15f, 1f));
            SetMaterialColor(ProductionInteractableMaterialPath, new Color(0.48f, 0.4f, 0.2f, 1f));
        }

        private static void SetMaterialColor(string path, Color color)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                return;
            }

            material.color = color;
            EditorUtility.SetDirty(material);
        }

        private static void DeleteGeneratedObject(string objectName)
        {
            var existing = GameObject.Find(objectName);
            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing);
                return;
            }

            var scene = SceneManager.GetActiveScene();
            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                var child = FindChildRecursive(roots[i].transform, objectName);
                if (child == null)
                {
                    continue;
                }

                UnityEngine.Object.DestroyImmediate(child.gameObject);
                return;
            }
        }

        private static Transform FindChildRecursive(Transform parent, string objectName)
        {
            for (var i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (child.name == objectName)
                {
                    return child;
                }

                var nested = FindChildRecursive(child, objectName);
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }

        private readonly struct VitalHudRefs
        {
            public VitalHudRefs(Text healthText, Text shieldText, Text statusText, Image healthFill, Image shieldFill)
            {
                HealthText = healthText;
                ShieldText = shieldText;
                StatusText = statusText;
                HealthFill = healthFill;
                ShieldFill = shieldFill;
            }

            public Text HealthText { get; }

            public Text ShieldText { get; }

            public Text StatusText { get; }

            public Image HealthFill { get; }

            public Image ShieldFill { get; }
        }
    }
}
