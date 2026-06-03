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
            if (settlementController == null || maintenanceController == null)
            {
                throw new InvalidOperationException("Phase 10 requires settlement and maintenance controllers.");
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
                maintenanceController.AssociationContractButton == null ||
                maintenanceController.PrivateContractButton == null ||
                maintenanceController.ShopButton == null ||
                maintenanceController.PersonalCargoButton == null ||
                maintenanceController.UpgradesButton == null)
            {
                throw new InvalidOperationException("Phase 10 maintenance action buttons are missing.");
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

            Debug.Log("Phase 10 planet maintenance editor validation passed.");
        }
    }
}
