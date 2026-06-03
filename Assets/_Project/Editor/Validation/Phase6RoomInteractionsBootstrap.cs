using System;
using System.IO;
using Bellerophon.Core.Player;
using Bellerophon.Core.Ship;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Bellerophon.Editor.Validation
{
    public static class Phase6RoomInteractionsBootstrap
    {
        public const string CargoRunScenePath = Phase4CargoShipGrayboxBootstrap.CargoRunScenePath;
        public const string Phase6RootName = "Phase 6 Room Interactions";
        public const string DevicePanelTextName = "Ship Device Panel Text";

        [MenuItem("Bellerophon/Bootstrap/Ensure Phase 6 Room Interactions")]
        public static void EnsurePhase6Assets()
        {
            Phase4CargoShipGrayboxBootstrap.EnsurePhase4Assets();

            var scene = EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            DeleteGeneratedObject(Phase6RootName);

            var stateRoot = new GameObject(Phase6RootName);
            var interactionState = stateRoot.AddComponent<ShipDeviceInteractionState>();
            interactionState.EnsureInitialized();

            ConfigureDevice(
                "Interactable - Cockpit Helm",
                ShipDeviceType.CockpitHelm,
                interactionState,
                "Cockpit Helm",
                "Manual Flight");
            ConfigureDevice(
                "Interactable - Engine Room Power Screen",
                ShipDeviceType.EngineRoomPowerScreen,
                interactionState,
                "Engine Room Power Screen",
                "Overclock");
            ConfigureDevice(
                "Interactable - Control Room Main Screen",
                ShipDeviceType.ControlRoomMainScreen,
                interactionState,
                "Control Room Main Screen",
                "Open Control Screen");
            ConfigureDevice(
                "Interactable - Armory Turret Handle",
                ShipDeviceType.ArmoryTurretHandle,
                interactionState,
                "Armory Turret Handle",
                "Manual Turret");
            ConfigureDevice(
                "Interactable - Supply Room Storage Cabinet",
                ShipDeviceType.SupplyRoomStorageCabinet,
                interactionState,
                "Supply Room Storage Cabinet",
                "Open Storage");
            ConfigureDevice(
                "Interactable - Cargo Hold Cargo Status",
                ShipDeviceType.CargoHoldCargoStatus,
                interactionState,
                "Cargo Hold Cargo Status",
                "Inspect Cargo");

            ConfigureDeviceHud(interactionState);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, CargoRunScenePath);
            Phase6RoomInteractionsEditorValidation.Run();

            if (!Application.isBatchMode)
            {
                EditorSceneManager.OpenScene(CargoRunScenePath, OpenSceneMode.Single);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Phase 6 room interaction assets are ready.");
        }

        private static void ConfigureDevice(
            string objectName,
            ShipDeviceType deviceType,
            ShipDeviceInteractionState interactionState,
            string displayName,
            string prompt)
        {
            var target = GameObject.Find(objectName);
            if (target == null)
            {
                throw new InvalidOperationException("Missing phase 6 device target: " + objectName);
            }

            var device = target.GetComponent<ShipDeviceInteractable>();
            if (device == null)
            {
                device = target.AddComponent<ShipDeviceInteractable>();
            }

            device.Configure(deviceType, interactionState, displayName, prompt);
            EditorUtility.SetDirty(target);
        }

        private static void ConfigureDeviceHud(ShipDeviceInteractionState interactionState)
        {
            var hud = UnityEngine.Object.FindFirstObjectByType<FirstPersonHud>();
            if (hud == null)
            {
                throw new InvalidOperationException("Phase 6 room interactions require the first-person HUD.");
            }

            var hudTransform = hud.transform;
            var existingPanel = hudTransform.Find(DevicePanelTextName);
            if (existingPanel != null)
            {
                UnityEngine.Object.DestroyImmediate(existingPanel.gameObject);
            }

            var panelText = CreateDevicePanelText(hudTransform);
            var deviceHud = hud.GetComponent<ShipDeviceHud>();
            if (deviceHud == null)
            {
                deviceHud = hud.gameObject.AddComponent<ShipDeviceHud>();
            }

            deviceHud.Configure(interactionState, panelText);
            EditorUtility.SetDirty(hud.gameObject);
        }

        private static Text CreateDevicePanelText(Transform parent)
        {
            var textObject = new GameObject(DevicePanelTextName);
            textObject.transform.SetParent(parent, false);

            var rectTransform = textObject.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(1f, 0f);
            rectTransform.anchorMax = new Vector2(1f, 0f);
            rectTransform.pivot = new Vector2(1f, 0f);
            rectTransform.anchoredPosition = new Vector2(-24f, 24f);
            rectTransform.sizeDelta = new Vector2(620f, 300f);

            var label = textObject.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 20;
            label.alignment = TextAnchor.LowerRight;
            label.color = new Color(0.86f, 0.94f, 0.9f, 1f);
            label.supportRichText = true;
            label.raycastTarget = false;
            label.text = string.Empty;
            label.enabled = false;
            return label;
        }

        private static void DeleteGeneratedObject(string objectName)
        {
            var scene = SceneManager.GetActiveScene();
            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                if (roots[i].name == objectName)
                {
                    UnityEngine.Object.DestroyImmediate(roots[i]);
                    return;
                }
            }
        }
    }
}
