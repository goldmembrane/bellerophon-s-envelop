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
    public static class Phase9SettlementGameOverEditorValidation
    {
        public static void Run()
        {
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(Phase9SettlementGameOverBootstrap.CargoRunScenePath);
            if (sceneAsset == null)
            {
                throw new InvalidOperationException("Missing CargoRunMvp scene for Phase 9 settlement validation.");
            }

            if (SceneManager.GetActiveScene().path != Phase9SettlementGameOverBootstrap.CargoRunScenePath)
            {
                EditorSceneManager.OpenScene(Phase9SettlementGameOverBootstrap.CargoRunScenePath, OpenSceneMode.Single);
            }

            var startController = UnityEngine.Object.FindFirstObjectByType<NewGameStartFlowController>();
            var deviceState = UnityEngine.Object.FindFirstObjectByType<ShipDeviceInteractionState>();
            var playerInput = UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayerInput>();
            var settlementController = UnityEngine.Object.FindFirstObjectByType<TransportSettlementController>();
            if (startController == null || deviceState == null || playerInput == null || settlementController == null)
            {
                throw new InvalidOperationException("Phase 9 requires start flow, ship device state, player input, and settlement controller.");
            }

            if (settlementController.SettlementRoot == null ||
                settlementController.SettlementRoot.name != Phase9SettlementGameOverBootstrap.SettlementRootName ||
                settlementController.SettlementBodyText == null)
            {
                throw new InvalidOperationException("Phase 9 settlement panel is not configured.");
            }

            if (settlementController.GameOverRoot == null ||
                settlementController.GameOverRoot.name != Phase9SettlementGameOverBootstrap.GameOverRootName ||
                settlementController.CargoShipVisual == null ||
                settlementController.PodVisual == null ||
                settlementController.GameOverTitleText == null)
            {
                throw new InvalidOperationException("Phase 9 game over cutscene is missing root, ship, pod, or title.");
            }

            var gameOverRect = settlementController.GameOverRoot.GetComponent<RectTransform>();
            if (gameOverRect == null ||
                gameOverRect.anchorMin != Vector2.zero ||
                gameOverRect.anchorMax != Vector2.one ||
                gameOverRect.sizeDelta != Vector2.zero)
            {
                throw new InvalidOperationException("Phase 9 game over cutscene must be full-screen.");
            }

            var background = settlementController.GameOverRoot.GetComponent<Image>();
            if (background == null || background.color.a < 1f)
            {
                throw new InvalidOperationException("Phase 9 game over cutscene background must be fully opaque.");
            }

            Debug.Log("Phase 9 settlement game over editor validation passed.");
        }
    }
}
