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
        public const string PlanetStayRootName = "Phase 20 Planet Stay Screen";
        public const string MaintenanceRootName = "Phase 10 Maintenance Screen";
        public const string ContractBoardRootName = "Phase 10 Contract Board Screen";
        public const string PersonalCargoRootName = "Phase 10 Personal Cargo Screen";
        public const string ShipUpgradeRootName = "Phase 10 Ship Upgrade Screen";
        public const string ContinueButtonName = "Phase 10 Continue To Maintenance Button";
        public const string PlanetStayTitleTextName = "Phase 20 Planet Stay Title";
        public const string PlanetStayBodyTextName = "Phase 20 Planet Stay Body";
        public const string PlanetStayStatusTextName = "Phase 20 Planet Stay Status";
        public const string PlanetStayRepairButtonName = "Phase 20 Planet Repair Shop Button";
        public const string PlanetStayContractButtonName = "Phase 20 Planet Contract Office Button";
        public const string PlanetStayShopButtonName = "Phase 20 Planet Shop Button";
        public const string PlanetStayCargoButtonName = "Phase 20 Planet Cargo Depot Button";
        public const string PlanetStayShipButtonName = "Phase 20 Planet Ship Button";
        public const string MaintenanceTitleTextName = "Phase 10 Maintenance Title";
        public const string MaintenanceWalletTextName = "Phase 10 Maintenance Wallet";
        public const string MaintenanceRoomStatusTextName = "Phase 10 Room Status";
        public const string MaintenanceContractListTextName = "Phase 10 Contract List";
        public const string MaintenanceStatusTextName = "Phase 10 Maintenance Status";
        public const string RepairButtonName = "Phase 10 Repair Button";
        public const string ContractBoardButtonName = "Phase 10 Contract Board Entry Button";
        public const string ContractBoardTitleTextName = "Phase 10 Contract Board Title";
        public const string ContractBoardSummaryTextName = "Phase 10 Contract Board Summary";
        public const string ContractBoardListTextName = "Phase 10 Contract Board List";
        public const string ContractBoardStatusTextName = "Phase 10 Contract Board Status";
        public const string ContractSlotButtonNamePrefix = "Phase 10 Contract Slot Button ";
        public const string AssociationContractButtonName = "Phase 10 Association Contract Button";
        public const string PrivateContractButtonName = "Phase 10 Private Contract Button";
        public const string SpecialContractButtonName = "Phase 10 Special Contract Button";
        public const string PreviousContractButtonName = "Phase 10 Previous Contract Button";
        public const string NextContractButtonName = "Phase 10 Next Contract Button";
        public const string AcceptContractButtonName = "Phase 10 Accept Contract Button";
        public const string StartRunButtonName = "Phase 10 Start Run Button";
        public const string ContractBoardBackButtonName = "Phase 10 Contract Board Back Button";
        public const string ShopButtonName = "Phase 10 Shop Entry Button";
        public const string PersonalCargoButtonName = "Phase 10 Personal Cargo Entry Button";
        public const string UpgradesButtonName = "Phase 10 Upgrades Entry Button";
        public const string PlanetBackButtonName = "Phase 10 Back To Planet Button";
        public const string PersonalCargoTitleTextName = "Phase 10 Personal Cargo Title";
        public const string PersonalCargoBodyTextName = "Phase 10 Personal Cargo Body";
        public const string PersonalCargoStatusTextName = "Phase 10 Personal Cargo Status";
        public const string CollectPersonalCargoButtonName = "Phase 10 Collect Personal Cargo Button";
        public const string ClosePersonalCargoButtonName = "Phase 10 Close Personal Cargo Button";
        public const string ShipUpgradeTitleTextName = "Phase 10 Ship Upgrade Title";
        public const string ShipUpgradeBodyTextName = "Phase 10 Ship Upgrade Body";
        public const string ShipUpgradeStatusTextName = "Phase 10 Ship Upgrade Status";
        public const string ShipUpgradePurchaseButtonNamePrefix = "Phase 10 Upgrade Purchase Button ";
        public const string ShipUpgradeEquipButtonNamePrefix = "Phase 10 Upgrade Equip Button ";
        public const string CloseShipUpgradeButtonName = "Phase 10 Close Ship Upgrade Button";
        public const int ContractSlotButtonCount = 8;
        public const int ShipUpgradeCategoryButtonCount = 5;

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
            var planetRoot = CreatePlanetStayRoot(root.transform);
            var maintenanceRoot = CreateMaintenanceRoot(root.transform);
            var contractBoardRoot = CreateContractBoardRoot(root.transform);
            var personalCargoRoot = CreatePersonalCargoRoot(root.transform);
            var shipUpgradeRoot = CreateShipUpgradeRoot(root.transform);
            var planetTitle = CreateText(
                PlanetStayTitleTextName,
                planetRoot.transform,
                new Vector2(0f, 312f),
                new Vector2(980f, 48f),
                30,
                TextAnchor.MiddleCenter);
            var planetBody = CreateText(
                PlanetStayBodyTextName,
                planetRoot.transform,
                new Vector2(-350f, 42f),
                new Vector2(450f, 430f),
                18,
                TextAnchor.UpperLeft);
            var planetStatus = CreateText(
                PlanetStayStatusTextName,
                planetRoot.transform,
                new Vector2(0f, -284f),
                new Vector2(980f, 52f),
                18,
                TextAnchor.MiddleCenter);
            var planetRepairButton = CreateButton(PlanetStayRepairButtonName, planetRoot.transform, new Vector2(410f, 160f), "Repair", new Vector2(180f, 42f), 16, TextAnchor.MiddleCenter);
            var planetContractButton = CreateButton(PlanetStayContractButtonName, planetRoot.transform, new Vector2(410f, 104f), "Contracts", new Vector2(180f, 42f), 16, TextAnchor.MiddleCenter);
            var planetShopButton = CreateButton(PlanetStayShopButtonName, planetRoot.transform, new Vector2(410f, 48f), "Shop", new Vector2(180f, 42f), 16, TextAnchor.MiddleCenter);
            var planetCargoButton = CreateButton(PlanetStayCargoButtonName, planetRoot.transform, new Vector2(410f, -8f), "Cargo Depot", new Vector2(180f, 42f), 16, TextAnchor.MiddleCenter);
            var planetShipButton = CreateButton(PlanetStayShipButtonName, planetRoot.transform, new Vector2(410f, -64f), "Ship", new Vector2(180f, 42f), 16, TextAnchor.MiddleCenter);
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
            var contractBoardButton = CreateButton(ContractBoardButtonName, maintenanceRoot.transform, new Vector2(-170f, -218f), "Contracts");
            var shopButton = CreateButton(ShopButtonName, maintenanceRoot.transform, new Vector2(80f, -218f), "Shop");
            var personalButton = CreateButton(PersonalCargoButtonName, maintenanceRoot.transform, new Vector2(330f, -218f), "Cargo");
            var upgradesButton = CreateButton(UpgradesButtonName, maintenanceRoot.transform, new Vector2(580f, -218f), "Upgrades");
            var planetBackButton = CreateButton(PlanetBackButtonName, maintenanceRoot.transform, new Vector2(500f, 235f), "Planet");

            var boardTitle = CreateText(
                ContractBoardTitleTextName,
                contractBoardRoot.transform,
                new Vector2(0f, 310f),
                new Vector2(980f, 44f),
                30,
                TextAnchor.MiddleCenter);
            var boardSummary = CreateText(
                ContractBoardSummaryTextName,
                contractBoardRoot.transform,
                new Vector2(0f, 245f),
                new Vector2(1000f, 72f),
                18,
                TextAnchor.UpperLeft);
            var boardList = CreateText(
                ContractBoardListTextName,
                contractBoardRoot.transform,
                new Vector2(0f, 25f),
                new Vector2(1060f, 360f),
                16,
                TextAnchor.UpperLeft);
            var contractSlotButtons = CreateContractSlotButtons(contractBoardRoot.transform);
            var boardStatus = CreateText(
                ContractBoardStatusTextName,
                contractBoardRoot.transform,
                new Vector2(0f, -280f),
                new Vector2(1000f, 54f),
                18,
                TextAnchor.MiddleCenter);
            var associationButton = CreateButton(AssociationContractButtonName, contractBoardRoot.transform, new Vector2(-560f, -218f), "Association", new Vector2(132f, 38f), 15, TextAnchor.MiddleCenter);
            var privateButton = CreateButton(PrivateContractButtonName, contractBoardRoot.transform, new Vector2(-400f, -218f), "Private", new Vector2(132f, 38f), 15, TextAnchor.MiddleCenter);
            var specialButton = CreateButton(SpecialContractButtonName, contractBoardRoot.transform, new Vector2(-240f, -218f), "Special", new Vector2(132f, 38f), 15, TextAnchor.MiddleCenter);
            var previousButton = CreateButton(PreviousContractButtonName, contractBoardRoot.transform, new Vector2(-80f, -218f), "Previous", new Vector2(132f, 38f), 15, TextAnchor.MiddleCenter);
            var nextButton = CreateButton(NextContractButtonName, contractBoardRoot.transform, new Vector2(80f, -218f), "Next", new Vector2(132f, 38f), 15, TextAnchor.MiddleCenter);
            var acceptButton = CreateButton(AcceptContractButtonName, contractBoardRoot.transform, new Vector2(240f, -218f), "Accept", new Vector2(132f, 38f), 15, TextAnchor.MiddleCenter);
            var startRunButton = CreateButton(StartRunButtonName, contractBoardRoot.transform, new Vector2(400f, -218f), "Start Run", new Vector2(132f, 38f), 15, TextAnchor.MiddleCenter);
            var backButton = CreateButton(ContractBoardBackButtonName, contractBoardRoot.transform, new Vector2(560f, -218f), "Back", new Vector2(132f, 38f), 15, TextAnchor.MiddleCenter);

            var cargoTitle = CreateText(
                PersonalCargoTitleTextName,
                personalCargoRoot.transform,
                new Vector2(0f, 310f),
                new Vector2(980f, 44f),
                30,
                TextAnchor.MiddleCenter);
            var cargoBody = CreateText(
                PersonalCargoBodyTextName,
                personalCargoRoot.transform,
                new Vector2(0f, 35f),
                new Vector2(980f, 430f),
                18,
                TextAnchor.UpperLeft);
            var cargoStatus = CreateText(
                PersonalCargoStatusTextName,
                personalCargoRoot.transform,
                new Vector2(-210f, -280f),
                new Vector2(620f, 54f),
                18,
                TextAnchor.MiddleCenter);
            var collectCargoButton = CreateButton(CollectPersonalCargoButtonName, personalCargoRoot.transform, new Vector2(310f, -280f), "Collect");
            var closeCargoButton = CreateButton(ClosePersonalCargoButtonName, personalCargoRoot.transform, new Vector2(500f, -280f), "Back");

            var upgradeTitle = CreateText(
                ShipUpgradeTitleTextName,
                shipUpgradeRoot.transform,
                new Vector2(0f, 310f),
                new Vector2(980f, 44f),
                30,
                TextAnchor.MiddleCenter);
            var upgradeBody = CreateText(
                ShipUpgradeBodyTextName,
                shipUpgradeRoot.transform,
                new Vector2(130f, 35f),
                new Vector2(760f, 430f),
                17,
                TextAnchor.UpperLeft);
            var upgradeStatus = CreateText(
                ShipUpgradeStatusTextName,
                shipUpgradeRoot.transform,
                new Vector2(-140f, -280f),
                new Vector2(700f, 54f),
                18,
                TextAnchor.MiddleCenter);
            var upgradePurchaseButtons = CreateShipUpgradeButtons(
                shipUpgradeRoot.transform,
                ShipUpgradePurchaseButtonNamePrefix,
                "Buy T",
                new Vector2(-510f, 160f));
            var upgradeEquipButtons = CreateShipUpgradeButtons(
                shipUpgradeRoot.transform,
                ShipUpgradeEquipButtonNamePrefix,
                "Equip",
                new Vector2(-340f, 160f));
            var closeUpgradeButton = CreateButton(CloseShipUpgradeButtonName, shipUpgradeRoot.transform, new Vector2(500f, -280f), "Back");

            var continueButton = CreateButton(
                ContinueButtonName,
                root.transform,
                new Vector2(0f, -300f),
                "Planet");

            planetRoot.SetActive(false);
            maintenanceRoot.SetActive(false);
            contractBoardRoot.SetActive(false);
            personalCargoRoot.SetActive(false);
            shipUpgradeRoot.SetActive(false);

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
                null,
                null,
                shopButton,
                personalButton,
                upgradesButton);
            var boardController = root.AddComponent<ContractBoardController>();
            boardController.Configure(
                startController,
                deviceState,
                playerInput,
                controller,
                contractBoardRoot,
                boardTitle,
                boardSummary,
                boardList,
                boardStatus,
                contractSlotButtons,
                associationButton,
                privateButton,
                specialButton,
                previousButton,
                nextButton,
                acceptButton,
                startRunButton,
                backButton);
            controller.ConfigureContractBoard(boardController, contractBoardButton);
            var cargoController = root.AddComponent<PersonalCargoController>();
            cargoController.Configure(
                startController,
                controller,
                playerInput,
                personalCargoRoot,
                cargoTitle,
                cargoBody,
                cargoStatus,
                collectCargoButton,
                closeCargoButton);
            var upgradeController = root.AddComponent<ShipUpgradeController>();
            upgradeController.Configure(
                startController,
                controller,
                playerInput,
                shipUpgradeRoot,
                upgradeTitle,
                upgradeBody,
                upgradeStatus,
                upgradePurchaseButtons,
                upgradeEquipButtons,
                closeUpgradeButton);
            controller.ConfigureShipUpgrades(upgradeController);
            var planetController = root.AddComponent<PlanetStayController>();
            var equipmentShopController = UnityEngine.Object.FindFirstObjectByType<EquipmentShopController>();
            planetController.Configure(
                startController,
                playerInput,
                controller,
                boardController,
                equipmentShopController,
                cargoController,
                upgradeController,
                planetRoot,
                planetTitle,
                planetBody,
                planetStatus,
                planetRepairButton,
                planetContractButton,
                planetShopButton,
                planetCargoButton,
                planetShipButton);
            controller.ConfigurePlanetStay(planetController, planetBackButton);
            RelinkPlanetStayDestinations();
            settlementController.ConfigurePlanetContinuation(planetController, continueButton);

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

        public static void RelinkPlanetStayDestinations()
        {
            var planetController = UnityEngine.Object.FindFirstObjectByType<PlanetStayController>();
            if (planetController == null)
            {
                return;
            }

            var maintenanceController = UnityEngine.Object.FindFirstObjectByType<PlanetMaintenanceController>();
            var contractBoardController = UnityEngine.Object.FindFirstObjectByType<ContractBoardController>();
            var equipmentShopController = UnityEngine.Object.FindFirstObjectByType<EquipmentShopController>();
            var personalCargoController = UnityEngine.Object.FindFirstObjectByType<PersonalCargoController>();
            var upgradeController = UnityEngine.Object.FindFirstObjectByType<ShipUpgradeController>();
            var planetBackButton = FindButtonIncludingInactive(PlanetBackButtonName) ??
                maintenanceController?.PlanetBackButton;

            planetController.ConfigureDestinations(
                contractBoardController,
                equipmentShopController,
                personalCargoController,
                upgradeController);
            if (maintenanceController != null)
            {
                maintenanceController.ConfigurePlanetStay(planetController, planetBackButton);
            }
            contractBoardController?.ConfigurePlanetStay(planetController);
            personalCargoController?.ConfigurePlanetStay(planetController);
            upgradeController?.ConfigurePlanetStay(planetController);
            equipmentShopController?.ConfigureMaintenance(maintenanceController);
            equipmentShopController?.ConfigurePlanetStay(planetController);
        }

        private static Button FindButtonIncludingInactive(string buttonName)
        {
            var buttons = UnityEngine.Object.FindObjectsByType<Button>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (var i = 0; i < buttons.Length; i++)
            {
                if (buttons[i].name == buttonName)
                {
                    return buttons[i];
                }
            }

            return null;
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

        private static GameObject CreatePlanetStayRoot(Transform parent)
        {
            var root = new GameObject(PlanetStayRootName, typeof(RectTransform));
            root.transform.SetParent(parent, false);

            var rectTransform = root.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = Vector2.zero;

            var background = root.AddComponent<Image>();
            background.color = new Color(0.035f, 0.04f, 0.036f, 1f);

            CreatePlanetMapMarker(root.transform, "Phase 20 Planet Map Shop Marker", new Vector2(-22f, 156f), "SHOP");
            CreatePlanetMapMarker(root.transform, "Phase 20 Planet Map Repair Marker", new Vector2(-22f, -62f), "REPAIR");
            CreatePlanetMapMarker(root.transform, "Phase 20 Planet Map Ship Marker", new Vector2(144f, 18f), "SHIP");
            CreatePlanetMapMarker(root.transform, "Phase 20 Planet Map Cargo Marker", new Vector2(182f, 174f), "CARGO");
            return root;
        }

        private static void CreatePlanetMapMarker(
            Transform parent,
            string name,
            Vector2 anchoredPosition,
            string labelText)
        {
            var marker = new GameObject(name, typeof(RectTransform));
            marker.transform.SetParent(parent, false);

            var rectTransform = marker.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = new Vector2(118f, 42f);

            var image = marker.AddComponent<Image>();
            image.color = new Color(0.12f, 0.19f, 0.16f, 0.95f);
            image.raycastTarget = false;

            var label = CreateText(
                name + " Label",
                marker.transform,
                Vector2.zero,
                new Vector2(110f, 34f),
                14,
                TextAnchor.MiddleCenter);
            label.text = labelText;
        }

        private static GameObject CreateContractBoardRoot(Transform parent)
        {
            var root = new GameObject(ContractBoardRootName, typeof(RectTransform));
            root.transform.SetParent(parent, false);

            var rectTransform = root.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = Vector2.zero;

            var background = root.AddComponent<Image>();
            background.color = new Color(0.018f, 0.024f, 0.036f, 1f);
            return root;
        }

        private static GameObject CreatePersonalCargoRoot(Transform parent)
        {
            var root = new GameObject(PersonalCargoRootName, typeof(RectTransform));
            root.transform.SetParent(parent, false);

            var rectTransform = root.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = Vector2.zero;

            var background = root.AddComponent<Image>();
            background.color = new Color(0.024f, 0.03f, 0.028f, 1f);
            return root;
        }

        private static GameObject CreateShipUpgradeRoot(Transform parent)
        {
            var root = new GameObject(ShipUpgradeRootName, typeof(RectTransform));
            root.transform.SetParent(parent, false);

            var rectTransform = root.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = Vector2.zero;

            var background = root.AddComponent<Image>();
            background.color = new Color(0.026f, 0.026f, 0.038f, 1f);
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

        private static Button[] CreateContractSlotButtons(Transform parent)
        {
            var buttons = new Button[ContractSlotButtonCount];
            for (var i = 0; i < buttons.Length; i++)
            {
                buttons[i] = CreateButton(
                    ContractSlotButtonNamePrefix + (i + 1),
                    parent,
                    new Vector2(0f, 162f - (i * 42f)),
                    "Contract",
                    new Vector2(1060f, 34f),
                    15,
                    TextAnchor.MiddleLeft);
            }

            return buttons;
        }

        private static Button[] CreateShipUpgradeButtons(
            Transform parent,
            string namePrefix,
            string labelPrefix,
            Vector2 startPosition)
        {
            var labels = new[]
            {
                "Durability",
                "Weapons",
                "Auto Pilot",
                "Supply",
                "Control"
            };
            var buttons = new Button[ShipUpgradeCategoryButtonCount];
            for (var i = 0; i < buttons.Length; i++)
            {
                buttons[i] = CreateButton(
                    namePrefix + (i + 1),
                    parent,
                    new Vector2(startPosition.x, startPosition.y - (i * 54f)),
                    labelPrefix + " " + labels[i],
                    new Vector2(150f, 38f),
                    14,
                    TextAnchor.MiddleCenter);
            }

            return buttons;
        }

        private static Button CreateButton(string name, Transform parent, Vector2 anchoredPosition, string labelText)
        {
            return CreateButton(
                name,
                parent,
                anchoredPosition,
                labelText,
                new Vector2(150f, 38f),
                16,
                TextAnchor.MiddleCenter);
        }

        private static Button CreateButton(
            string name,
            Transform parent,
            Vector2 anchoredPosition,
            string labelText,
            Vector2 size,
            int fontSize,
            TextAnchor textAlignment)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform));
            buttonObject.transform.SetParent(parent, false);

            var rectTransform = buttonObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = size;

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
                new Vector2(Mathf.Max(10f, size.x - 12f), Mathf.Max(10f, size.y - 8f)),
                fontSize,
                textAlignment);
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
