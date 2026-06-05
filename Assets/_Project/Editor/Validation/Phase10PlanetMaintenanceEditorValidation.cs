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
            var maintenanceController = UnityEngine.Object.FindFirstObjectByType<PlanetMaintenanceController>();
            var contractBoardController = UnityEngine.Object.FindFirstObjectByType<ContractBoardController>();
            if (settlementController == null || maintenanceController == null || contractBoardController == null)
            {
                throw new InvalidOperationException("Phase 10 requires settlement, maintenance, and contract board controllers.");
            }

            if (settlementController.ContinueToMaintenanceButton == null ||
                settlementController.ContinueToMaintenanceButton.name != Phase10PlanetMaintenanceBootstrap.ContinueButtonName)
            {
                throw new InvalidOperationException("Phase 10 settlement continuation button is not configured.");
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
                maintenanceController.UpgradesButton == null)
            {
                throw new InvalidOperationException("Phase 10 maintenance action buttons are missing.");
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
                contractBoardController.BackButton == null)
            {
                throw new InvalidOperationException("Phase 10 contract board screen references are missing.");
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

            Debug.Log("Phase 10 planet maintenance editor validation passed.");
        }
    }
}
