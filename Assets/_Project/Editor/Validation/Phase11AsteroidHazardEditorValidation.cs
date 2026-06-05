using System;
using Bellerophon.Core.Session;
using Bellerophon.Core.Ship;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.Validation
{
    public static class Phase11AsteroidHazardEditorValidation
    {
        public static void Run()
        {
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(Phase11AsteroidHazardBootstrap.CargoRunScenePath);
            if (sceneAsset == null)
            {
                throw new InvalidOperationException("Missing CargoRunMvp scene for Phase 11 asteroid hazard validation.");
            }

            if (SceneManager.GetActiveScene().path != Phase11AsteroidHazardBootstrap.CargoRunScenePath)
            {
                EditorSceneManager.OpenScene(Phase11AsteroidHazardBootstrap.CargoRunScenePath, OpenSceneMode.Single);
            }

            var root = GameObject.Find(Phase11AsteroidHazardBootstrap.Phase11RootName);
            var deviceState = UnityEngine.Object.FindFirstObjectByType<ShipDeviceInteractionState>();
            var deviceHud = UnityEngine.Object.FindFirstObjectByType<ShipDeviceHud>();
            var manualView = UnityEngine.Object.FindFirstObjectByType<ManualFlightView>();
            var maintenanceController = UnityEngine.Object.FindFirstObjectByType<PlanetMaintenanceController>();
            var contractBoardController = UnityEngine.Object.FindFirstObjectByType<ContractBoardController>();
            if (root == null ||
                deviceState == null ||
                deviceHud == null ||
                manualView == null ||
                maintenanceController == null ||
                contractBoardController == null)
            {
                throw new InvalidOperationException("Phase 11 requires hazard root, device state, HUD, manual flight view, maintenance controller, and contract board.");
            }

            if (deviceHud.TransportStatusText == null)
            {
                throw new InvalidOperationException("Phase 11 transport HUD must be available for hazard status.");
            }

            if (manualView.ViewRoot == null ||
                manualView.ViewRoot.transform.Find(Phase8TransportRunBootstrap.AsteroidFieldName) == null)
            {
                throw new InvalidOperationException("Phase 11 must reuse the full-screen manual flight asteroid field view.");
            }

            if (maintenanceController.ContractBoardButton == null ||
                contractBoardController.AssociationContractButton == null ||
                contractBoardController.PrivateContractButton == null ||
                contractBoardController.AcceptContractButton == null)
            {
                throw new InvalidOperationException("Phase 11 requires the contract board as the post-tutorial hazard entry path.");
            }

            if (TransportHazardRules.AsteroidFieldOccurrencePercent <= 0 ||
                TransportHazardRules.MinimumAsteroidFieldDurationSeconds <= 0)
            {
                throw new InvalidOperationException("Phase 11 asteroid hazard occurrence and duration rules must be configured.");
            }

            Debug.Log("Phase 11 asteroid hazard editor validation passed.");
        }
    }
}
