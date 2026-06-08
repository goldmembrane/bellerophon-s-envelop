using System;
using Bellerophon.Core.Session;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Bellerophon.Editor.Validation
{
    public static class Phase10PlanetMaintenanceEditorValidation
    {
        public static void Run()
        {
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(Phase10PlanetMaintenanceBootstrap.CargoRunScenePath);
            if (sceneAsset == null)
            {
                throw new InvalidOperationException("Missing CargoRunMvp scene for Phase 10 maintenance validation.");
            }

            if (SceneManager.GetActiveScene().path != Phase10PlanetMaintenanceBootstrap.CargoRunScenePath)
            {
                EditorSceneManager.OpenScene(Phase10PlanetMaintenanceBootstrap.CargoRunScenePath, OpenSceneMode.Single);
            }

            var settlementController = UnityEngine.Object.FindFirstObjectByType<TransportSettlementController>();
            var planetController = UnityEngine.Object.FindFirstObjectByType<PlanetStayController>();
            var maintenanceController = UnityEngine.Object.FindFirstObjectByType<PlanetMaintenanceController>();
            var contractBoardController = UnityEngine.Object.FindFirstObjectByType<ContractBoardController>();
            var shopController = UnityEngine.Object.FindFirstObjectByType<EquipmentShopController>();
            var personalCargoController = UnityEngine.Object.FindFirstObjectByType<PersonalCargoController>();
            var shipUpgradeController = UnityEngine.Object.FindFirstObjectByType<ShipUpgradeController>();
            if (settlementController == null ||
                planetController == null ||
                maintenanceController == null ||
                contractBoardController == null ||
                personalCargoController == null ||
                shipUpgradeController == null)
            {
                throw new InvalidOperationException("Phase 10 requires settlement, planet stay, maintenance, contract board, personal cargo, and ship upgrade controllers.");
            }

            if (settlementController.ContinueToMaintenanceButton == null ||
                settlementController.ContinueToMaintenanceButton.name != Phase10PlanetMaintenanceBootstrap.ContinueButtonName)
            {
                throw new InvalidOperationException("Phase 10 settlement continuation button is not configured.");
            }

            if (settlementController.PlanetStayController != planetController)
            {
                throw new InvalidOperationException("Phase 20 settlement continuation must target the planet stay screen before maintenance.");
            }

            if (planetController.PlanetRoot == null ||
                planetController.PlanetRoot.name != Phase10PlanetMaintenanceBootstrap.PlanetStayRootName ||
                planetController.TitleText == null ||
                planetController.BodyText == null ||
                planetController.StatusText == null ||
                planetController.RepairShopButton == null ||
                planetController.ContractOfficeButton == null ||
                planetController.ShopButton == null ||
                planetController.CargoDepotButton == null ||
                planetController.ShipButton == null)
            {
                throw new InvalidOperationException("Phase 20 planet stay screen references are missing.");
            }

            if (maintenanceController.MaintenanceRoot == null ||
                maintenanceController.MaintenanceRoot.name != Phase10PlanetMaintenanceBootstrap.MaintenanceRootName ||
                maintenanceController.RoomStatusText == null ||
                maintenanceController.ContractListText == null ||
                maintenanceController.StatusText == null)
            {
                throw new InvalidOperationException("Phase 10 maintenance screen text references are missing.");
            }

            if (maintenanceController.RepairButton == null ||
                maintenanceController.ContractBoardButton == null ||
                maintenanceController.ShopButton == null ||
                maintenanceController.PersonalCargoButton == null ||
                maintenanceController.UpgradesButton == null ||
                maintenanceController.PlanetBackButton == null)
            {
                throw new InvalidOperationException("Phase 10 maintenance action buttons are missing.");
            }

            if (maintenanceController.PlanetStayController != planetController)
            {
                throw new InvalidOperationException("Phase 10 repair screen must be able to return to the planet stay screen.");
            }

            if (shopController != null &&
                (planetController.ShopController != shopController ||
                 shopController.PlanetStayController != planetController ||
                 shopController.MaintenanceController != maintenanceController))
            {
                throw new InvalidOperationException("Existing Phase 15 equipment shop must stay linked to the Phase 20 planet stay hub.");
            }

            if (contractBoardController.BoardRoot == null ||
                contractBoardController.BoardRoot.name != Phase10PlanetMaintenanceBootstrap.ContractBoardRootName ||
                contractBoardController.SummaryText == null ||
                contractBoardController.ContractListText == null ||
                contractBoardController.StatusText == null ||
                contractBoardController.ContractSlotButtons == null ||
                contractBoardController.ContractSlotButtons.Length != Phase10PlanetMaintenanceBootstrap.ContractSlotButtonCount ||
                contractBoardController.AssociationContractButton == null ||
                contractBoardController.PrivateContractButton == null ||
                contractBoardController.SpecialContractButton == null ||
                contractBoardController.PreviousContractButton == null ||
                contractBoardController.NextContractButton == null ||
                contractBoardController.AcceptContractButton == null ||
                contractBoardController.StartRunButton == null ||
                contractBoardController.BackButton == null)
            {
                throw new InvalidOperationException("Phase 10 contract board screen references are missing.");
            }

            if (personalCargoController.CargoRoot == null ||
                personalCargoController.CargoRoot.name != Phase10PlanetMaintenanceBootstrap.PersonalCargoRootName ||
                personalCargoController.BodyText == null ||
                personalCargoController.StatusText == null ||
                personalCargoController.CollectButton == null ||
                personalCargoController.CloseButton == null)
            {
                throw new InvalidOperationException("Phase 10 personal cargo screen references are missing.");
            }

            if (shipUpgradeController.UpgradeRoot == null ||
                shipUpgradeController.UpgradeRoot.name != Phase10PlanetMaintenanceBootstrap.ShipUpgradeRootName ||
                shipUpgradeController.BodyText == null ||
                shipUpgradeController.StatusText == null ||
                shipUpgradeController.PurchaseButtons == null ||
                shipUpgradeController.PurchaseButtons.Length != Phase10PlanetMaintenanceBootstrap.ShipUpgradeCategoryButtonCount ||
                shipUpgradeController.EquipButtons == null ||
                shipUpgradeController.EquipButtons.Length != Phase10PlanetMaintenanceBootstrap.ShipUpgradeCategoryButtonCount ||
                shipUpgradeController.CloseButton == null)
            {
                throw new InvalidOperationException("Phase 10 ship upgrade screen references are missing.");
            }

            for (var i = 0; i < contractBoardController.ContractSlotButtons.Length; i++)
            {
                var slotButton = contractBoardController.ContractSlotButtons[i];
                if (slotButton == null ||
                    slotButton.name != Phase10PlanetMaintenanceBootstrap.ContractSlotButtonNamePrefix + (i + 1))
                {
                    throw new InvalidOperationException("Phase 10 contract board slot buttons are missing.");
                }
            }

            var maintenanceRect = maintenanceController.MaintenanceRoot.GetComponent<RectTransform>();
            if (maintenanceRect == null ||
                maintenanceRect.anchorMin != Vector2.zero ||
                maintenanceRect.anchorMax != Vector2.one ||
                maintenanceRect.sizeDelta != Vector2.zero)
            {
                throw new InvalidOperationException("Phase 10 maintenance screen must cover the full screen.");
            }

            var background = maintenanceController.MaintenanceRoot.GetComponent<Image>();
            if (background == null || background.color.a < 1f)
            {
                throw new InvalidOperationException("Phase 10 maintenance screen background must be fully opaque.");
            }

            var planetRect = planetController.PlanetRoot.GetComponent<RectTransform>();
            if (planetRect == null ||
                planetRect.anchorMin != Vector2.zero ||
                planetRect.anchorMax != Vector2.one ||
                planetRect.sizeDelta != Vector2.zero)
            {
                throw new InvalidOperationException("Phase 20 planet stay screen must cover the full screen.");
            }

            var planetBackground = planetController.PlanetRoot.GetComponent<Image>();
            if (planetBackground == null || planetBackground.color.a < 1f)
            {
                throw new InvalidOperationException("Phase 20 planet stay screen background must be fully opaque.");
            }

            var boardRect = contractBoardController.BoardRoot.GetComponent<RectTransform>();
            if (boardRect == null ||
                boardRect.anchorMin != Vector2.zero ||
                boardRect.anchorMax != Vector2.one ||
                boardRect.sizeDelta != Vector2.zero)
            {
                throw new InvalidOperationException("Phase 10 contract board screen must cover the full screen.");
            }

            var boardBackground = contractBoardController.BoardRoot.GetComponent<Image>();
            if (boardBackground == null || boardBackground.color.a < 1f)
            {
                throw new InvalidOperationException("Phase 10 contract board screen background must be fully opaque.");
            }

            var cargoRect = personalCargoController.CargoRoot.GetComponent<RectTransform>();
            if (cargoRect == null ||
                cargoRect.anchorMin != Vector2.zero ||
                cargoRect.anchorMax != Vector2.one ||
                cargoRect.sizeDelta != Vector2.zero)
            {
                throw new InvalidOperationException("Phase 10 personal cargo screen must cover the full screen.");
            }

            var cargoBackground = personalCargoController.CargoRoot.GetComponent<Image>();
            if (cargoBackground == null || cargoBackground.color.a < 1f)
            {
                throw new InvalidOperationException("Phase 10 personal cargo screen background must be fully opaque.");
            }

            var upgradeRect = shipUpgradeController.UpgradeRoot.GetComponent<RectTransform>();
            if (upgradeRect == null ||
                upgradeRect.anchorMin != Vector2.zero ||
                upgradeRect.anchorMax != Vector2.one ||
                upgradeRect.sizeDelta != Vector2.zero)
            {
                throw new InvalidOperationException("Phase 10 ship upgrade screen must cover the full screen.");
            }

            var upgradeBackground = shipUpgradeController.UpgradeRoot.GetComponent<Image>();
            if (upgradeBackground == null || upgradeBackground.color.a < 1f)
            {
                throw new InvalidOperationException("Phase 10 ship upgrade screen background must be fully opaque.");
            }

            Debug.Log("Phase 10 planet maintenance editor validation passed.");
        }
    }
}
