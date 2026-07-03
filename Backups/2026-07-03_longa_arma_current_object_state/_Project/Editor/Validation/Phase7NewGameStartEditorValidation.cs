using System;
using Bellerophon.Core.Player;
using Bellerophon.Core.Session;
using Bellerophon.Core.Ship;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.Validation
{
    public static class Phase7NewGameStartEditorValidation
    {
        public static void Run()
        {
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(Phase7NewGameStartBootstrap.CargoRunScenePath);
            if (sceneAsset == null)
            {
                throw new InvalidOperationException("Missing CargoRunMvp scene.");
            }

            if (SceneManager.GetActiveScene().path != Phase7NewGameStartBootstrap.CargoRunScenePath)
            {
                EditorSceneManager.OpenScene(Phase7NewGameStartBootstrap.CargoRunScenePath, OpenSceneMode.Single);
            }

            var controller = UnityEngine.Object.FindFirstObjectByType<NewGameStartFlowController>();
            if (controller == null)
            {
                throw new InvalidOperationException("Missing Phase 7 new game start flow controller.");
            }

            if (controller.FlowState.Phase != NewGameStartFlowPhase.ContractPrompt)
            {
                throw new InvalidOperationException("Phase 7 start flow must begin at the association contract prompt.");
            }

            if (controller.TitleText == null ||
                controller.BodyText == null ||
                controller.StatusText == null ||
                controller.YesButton == null ||
                controller.NoButton == null ||
                controller.TutorialContractButton == null ||
                controller.SkipTutorialButton == null)
            {
                throw new InvalidOperationException("Phase 7 start UI is not fully wired.");
            }

            if (controller.ShipDeviceState == null ||
                UnityEngine.Object.FindFirstObjectByType<ShipDeviceInteractionState>() == null)
            {
                throw new InvalidOperationException("Phase 7 start flow must be wired to the ship device state.");
            }

            if (controller.PlayerInput == null ||
                UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayerInput>() == null)
            {
                throw new InvalidOperationException("Phase 7 start flow must be wired to the player input for cursor unlock.");
            }

            if (!controller.YesButton.gameObject.activeSelf ||
                !controller.NoButton.gameObject.activeSelf ||
                !controller.TutorialContractButton.gameObject.activeSelf ||
                !controller.SkipTutorialButton.gameObject.activeSelf ||
                controller.YesButton.interactable ||
                controller.NoButton.interactable ||
                controller.TutorialContractButton.interactable ||
                controller.SkipTutorialButton.interactable ||
                controller.FlowState.AssociationContractScroll.ProgressPercent != 0)
            {
                throw new InvalidOperationException("Phase 7 initial UI must wait for the association contract scroll before enabling decisions.");
            }

            var yesPosition = controller.YesButton.GetComponent<RectTransform>().anchoredPosition.x;
            var noPosition = controller.NoButton.GetComponent<RectTransform>().anchoredPosition.x;
            if (yesPosition <= noPosition)
            {
                throw new InvalidOperationException("Phase 7 association Yes button must be placed to the right of the No button.");
            }

            if (GameObject.Find("Cargo Hold Central Cargo") == null)
            {
                throw new InvalidOperationException("Phase 7 requires the central cargo object in the cargo hold.");
            }

            if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() == null)
            {
                throw new InvalidOperationException("Phase 7 start UI requires an EventSystem for direct editor testing.");
            }

            Debug.Log("Phase 7 new game start editor validation passed.");
        }
    }
}
