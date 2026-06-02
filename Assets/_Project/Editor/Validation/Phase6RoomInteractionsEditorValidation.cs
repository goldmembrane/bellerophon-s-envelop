using System;
using Bellerophon.Core.Ship;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.Validation
{
    public static class Phase6RoomInteractionsEditorValidation
    {
        private static readonly (string ObjectName, ShipDeviceType DeviceType)[] RequiredDevices =
        {
            ("Interactable - Cockpit Helm", ShipDeviceType.CockpitHelm),
            ("Interactable - Engine Room Power Screen", ShipDeviceType.EngineRoomPowerScreen),
            ("Interactable - Control Room Main Screen", ShipDeviceType.ControlRoomMainScreen),
            ("Interactable - Armory Turret Handle", ShipDeviceType.ArmoryTurretHandle),
            ("Interactable - Supply Room Storage Cabinet", ShipDeviceType.SupplyRoomStorageCabinet),
            ("Interactable - Cargo Hold Cargo Status", ShipDeviceType.CargoHoldCargoStatus)
        };

        public static void Run()
        {
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(Phase6RoomInteractionsBootstrap.CargoRunScenePath);
            if (sceneAsset == null)
            {
                throw new InvalidOperationException("Missing CargoRunMvp scene.");
            }

            if (SceneManager.GetActiveScene().path != Phase6RoomInteractionsBootstrap.CargoRunScenePath)
            {
                EditorSceneManager.OpenScene(Phase6RoomInteractionsBootstrap.CargoRunScenePath, OpenSceneMode.Single);
            }

            var root = GameObject.Find(Phase6RoomInteractionsBootstrap.Phase6RootName);
            if (root == null)
            {
                throw new InvalidOperationException("Missing Phase 6 room interactions root.");
            }

            var state = root.GetComponent<ShipDeviceInteractionState>();
            if (state == null)
            {
                throw new InvalidOperationException("Missing Phase 6 ship device interaction state.");
            }

            foreach (var requiredDevice in RequiredDevices)
            {
                var deviceObject = GameObject.Find(requiredDevice.ObjectName);
                if (deviceObject == null)
                {
                    throw new InvalidOperationException("Missing Phase 6 device object: " + requiredDevice.ObjectName);
                }

                var device = deviceObject.GetComponent<ShipDeviceInteractable>();
                if (device == null)
                {
                    throw new InvalidOperationException("Missing ShipDeviceInteractable on " + requiredDevice.ObjectName);
                }

                if (device.DeviceType != requiredDevice.DeviceType)
                {
                    throw new InvalidOperationException(
                        $"{requiredDevice.ObjectName} has wrong device type. Expected={requiredDevice.DeviceType}, Actual={device.DeviceType}");
                }

                if (device.InteractionState != state)
                {
                    throw new InvalidOperationException(requiredDevice.ObjectName + " is not wired to the Phase 6 state.");
                }
            }

            var deviceHud = UnityEngine.Object.FindFirstObjectByType<ShipDeviceHud>();
            if (deviceHud == null || deviceHud.PanelText == null)
            {
                throw new InvalidOperationException("Missing Phase 6 ship device HUD panel.");
            }

            if (deviceHud.PanelText.name != Phase6RoomInteractionsBootstrap.DevicePanelTextName)
            {
                throw new InvalidOperationException("Phase 6 ship device HUD panel has the wrong text object.");
            }

            Debug.Log($"Phase 6 room interactions editor validation passed. Devices={RequiredDevices.Length}");
        }
    }
}
