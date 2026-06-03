using System;
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
    public static class Phase10PlanetMaintenanceBootstrap
    {
        public const string CargoRunScenePath = Phase9SettlementGameOverBootstrap.CargoRunScenePath;
        public const string Phase10RootName = "Phase 10 Planet Maintenance";
        public const string MaintenanceRootName = "Phase 10 Maintenance Screen";
        public const string ContinueButtonName = "Phase 10 Continue To Maintenance Button";
        public const string MaintenanceTitleTextName = "Phase 10 Maintenance Title";
        public const string MaintenanceWalletTextName = "Phase 10 Maintenance Wallet";
        public const string MaintenanceRoomStatusTextName = "Phase 10 Room Status";
        public const string MaintenanceContractListTextName = "Phase 10 Contract List";
        public const string MaintenanceStatusTextName = "Phase 10 Maintenance Status";
        public const string RepairButtonName = "Phase 10 Repair Button";
        public const string AssociationContractButtonName = "Phase 10 Association Contract Button";
        public const string PrivateContractButtonName = "Phase 10 Private Contract Button";
        public const string ShopButtonName = "Phase 10 Shop Entry Button";
        public const string PersonalCargoButtonName = "Phase 10 Personal Cargo Entry Button";
        public const string UpgradesButtonName = "Phase 10 Upgrades Entry Button";

        [MenuItem("Bellerophon/Bootstrap/Ensure Phase 10 Planet Maintenance")]
        public static void EnsurePhase10Assets()
        {
            Phase9SettlementGameOverBootstrap.EnsurePhase9Assets();

            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            DeleteGeneratedObject(Phase10RootName);
            DeleteGeneratedObject(ContinueButtonName);

            var hud = UnityEngine.Object.FindFirstObjectByType<FirstPersonHud>();
            var playerInput = UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayerInput>();
            var deviceState = UnityEngine.Object.FindFirstObjectByType<ShipDeviceInteractionState>();
            var startController = UnityEngine.Object.FindFirstObjectByType<NewGameStartFlowController>();
            var settlementController = UnityEngine.Object.FindFirstObjectByType<TransportSettlementController>();
            if (hud == null ||
                playerInput == null ||
                deviceState == null ||
                startController == null ||
                settlementController == null ||
                settlementController.SettlementRoot == null)
            {
                throw new InvalidOperationException("Phase 10 requires Phase 9 settlement assets, HUD, player input, device state, and start flow controller.");
            }

            var root = CreateRoot(hud.transform);
            var maintenanceRoot = CreateMaintenanceRoot(root.transform);
            var title = CreateText(
                MaintenanceTitleTextName,
                maintenanceRoot.transform,
                new Vector2(0f, 310f),
                new Vector2(980f, 44f),
                30,
                TextAnchor.MiddleCenter);
            var wallet = CreateText(
                MaintenanceWalletTextName,
                maintenanceRoot.transform,
                new Vector2(-420f, 235f),
                new Vector2(360f, 72f),
                20,
                TextAnchor.UpperLeft);
            var roomStatus = CreateText(
                MaintenanceRoomStatusTextName,
                maintenanceRoot.transform,
                new Vector2(-300f, 35f),
                new Vector2(560f, 330f),
                18,
                TextAnchor.UpperLeft);
            var contractList = CreateText(
                MaintenanceContractListTextName,
                maintenanceRoot.transform,
                new Vector2(330f, 35f),
                new Vector2(560f, 330f),
                18,
                TextAnchor.UpperLeft);
            var status = CreateText(
                MaintenanceStatusTextName,
                maintenanceRoot.transform,
                new Vector2(0f, -280f),
                new Vector2(1000f, 54f),
                18,
                TextAnchor.MiddleCenter);

            var repairButton = CreateButton(RepairButtonName, maintenanceRoot.transform, new Vector2(-420f, -218f), "Repair Ship");
            var associationButton = CreateButton(AssociationContractButtonName, maintenanceRoot.transform, new Vector2(-170f, -218f), "Association Job");
            var privateButton = CreateButton(PrivateContractButtonName, maintenanceRoot.transform, new Vector2(80f, -218f), "Private Job");
            var shopButton = CreateButton(ShopButtonName, maintenanceRoot.transform, new Vector2(330f, -218f), "Shop");
            var personalButton = CreateButton(PersonalCargoButtonName, maintenanceRoot.transform, new Vector2(500f, -218f), "Cargo");
            var upgradesButton = CreateButton(UpgradesButtonName, maintenanceRoot.transform, new Vector2(670f, -218f), "Upgrades");

            var continueButton = CreateButton(
                ContinueButtonName,
                root.transform,
                new Vector2(0f, -300f),
                "Maintenance");

            maintenanceRoot.SetActive(false);

            var controller = root.AddComponent<PlanetMaintenanceController>();
            controller.Configure(
                startController,
                deviceState,
                playerInput,
                maintenanceRoot,
                title,
                wallet,
                roomStatus,
                contractList,
                status,
                repairButton,
                associationButton,
                privateButton,
                shopButton,
                personalButton,
                upgradesButton);
            settlementController.ConfigureMaintenanceContinuation(controller, continueButton);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, CargoRunScenePath);
            Phase10PlanetMaintenanceEditorValidation.Run();

            if (!Application.isBatchMode)
            {
                EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Phase 10 planet maintenance assets are ready.");
        }

        private static GameObject CreateRoot(Transform parent)
        {
            var root = new GameObject(Phase10RootName, typeof(RectTransform));
            root.transform.SetParent(parent, false);

            var rectTransform = root.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = Vector2.zero;

            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 20;
            root.AddComponent<GraphicRaycaster>();
            return root;
        }

        private static GameObject CreateMaintenanceRoot(Transform parent)
        {
            var root = new GameObject(MaintenanceRootName, typeof(RectTransform));
            root.transform.SetParent(parent, false);

            var rectTransform = root.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = Vector2.zero;

            var background = root.AddComponent<Image>();
            background.color = new Color(0.022f, 0.028f, 0.032f, 1f);
            return root;
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
            label.color = new Color(0.9f, 0.96f, 0.92f, 1f);
            label.supportRichText = true;
            label.raycastTarget = false;
            label.text = string.Empty;
            return label;
        }

        private static Button CreateButton(string name, Transform parent, Vector2 anchoredPosition, string labelText)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform));
            buttonObject.transform.SetParent(parent, false);

            var rectTransform = buttonObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = new Vector2(150f, 38f);

            var image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.18f, 0.28f, 0.24f, 1f);

            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            buttonObject.AddComponent<CanvasGroup>();
            var colors = button.colors;
            colors.normalColor = new Color(0.18f, 0.28f, 0.24f, 1f);
            colors.highlightedColor = new Color(0.24f, 0.36f, 0.31f, 1f);
            colors.pressedColor = new Color(0.12f, 0.2f, 0.18f, 1f);
            colors.disabledColor = new Color(0.09f, 0.11f, 0.11f, 0.85f);
            button.colors = colors;

            var text = CreateText(
                name + " Label",
                buttonObject.transform,
                Vector2.zero,
                new Vector2(138f, 30f),
                16,
                TextAnchor.MiddleCenter);
            text.text = labelText;
            text.color = new Color(0.94f, 0.98f, 0.94f, 1f);
            return button;
        }

        private static void DeleteGeneratedObject(string objectName)
        {
            var target = GameObject.Find(objectName);
            if (target != null)
            {
                UnityEngine.Object.DestroyImmediate(target);
                return;
            }

            var scene = SceneManager.GetActiveScene();
            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                if (roots[i].name == objectName)
                {
                    UnityEngine.Object.DestroyImmediate(roots[i]);
                    return;
                }

                var child = roots[i].transform.Find(objectName);
                if (child != null)
                {
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
                    return;
                }
            }
        }
    }
}
