using System;
using Bellerophon.Core.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bellerophon.Editor.Validation
{
    public static class Phase4CargoShipGrayboxEditorValidation
    {
        private static readonly string[] RequiredRooms =
        {
            "Cargo Hold",
            "Cockpit",
            "Engine Room",
            "Control Room",
            "Armory",
            "Supply Room"
        };

        private static readonly (string From, string To)[] RequiredCorridors =
        {
            ("Cargo Hold", "Cockpit"),
            ("Cargo Hold", "Engine Room"),
            ("Cargo Hold", "Control Room"),
            ("Cargo Hold", "Armory"),
            ("Cargo Hold", "Supply Room"),
            ("Supply Room", "Armory"),
            ("Cockpit", "Engine Room"),
            ("Cockpit", "Control Room"),
            ("Engine Room", "Control Room"),
            ("Control Room", "Armory")
        };

        public static void Run()
        {
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(Phase4CargoShipGrayboxBootstrap.CargoRunScenePath);
            if (sceneAsset == null)
            {
                throw new InvalidOperationException("Missing CargoRunMvp scene.");
            }

            if (SceneManager.GetActiveScene().path != Phase4CargoShipGrayboxBootstrap.CargoRunScenePath)
            {
                EditorSceneManager.OpenScene(Phase4CargoShipGrayboxBootstrap.CargoRunScenePath, OpenSceneMode.Single);
            }

            var root = GameObject.Find(Phase4CargoShipGrayboxBootstrap.GrayboxRootName);
            if (root == null)
            {
                throw new InvalidOperationException("Missing Phase 4 cargo ship graybox root.");
            }

            foreach (var room in RequiredRooms)
            {
                if (!Phase4CargoShipGrayboxBootstrap.HasRoom(room))
                {
                    throw new InvalidOperationException("Missing graybox room: " + room);
                }
            }

            foreach (var corridor in RequiredCorridors)
            {
                if (!Phase4CargoShipGrayboxBootstrap.HasCorridor(corridor.From, corridor.To))
                {
                    throw new InvalidOperationException($"Missing graybox corridor: {corridor.From} to {corridor.To}");
                }
            }

            if (Phase4CargoShipGrayboxBootstrap.RoomDeckY("Cargo Hold") >= Phase4CargoShipGrayboxBootstrap.RoomDeckY("Cockpit") - 2.0f)
            {
                throw new InvalidOperationException("Cargo Hold must be below the other ship rooms for the sloped corridor layout.");
            }

            var cargoCockpitCorridor = GameObject.Find("Corridor - Cargo Hold to Cockpit");
            if (cargoCockpitCorridor == null || Mathf.Abs(Mathf.DeltaAngle(0f, cargoCockpitCorridor.transform.eulerAngles.x)) < 1f)
            {
                throw new InvalidOperationException("Cargo Hold to Cockpit corridor must be sloped.");
            }

            RequireSeparatedCargoEntrance("Control Room", "Cargo Hold");
            RequireSeparatedCargoEntrance("Armory", "Cargo Hold");
            RequireSegmentedArmoryCargoCorridor();

            var interactables = UnityEngine.Object.FindObjectsByType<DebugInteractable>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            if (interactables.Length < 6)
            {
                throw new InvalidOperationException($"Phase 4 graybox must have at least 6 interaction points. Found: {interactables.Length}");
            }

            var player = UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayerMotor>();
            if (player == null)
            {
                throw new InvalidOperationException("CargoRunMvp must contain the first-person player.");
            }

            var camera = Camera.main;
            if (camera == null || !camera.isActiveAndEnabled)
            {
                throw new InvalidOperationException("CargoRunMvp must contain an active MainCamera.");
            }

            var visibleRenderers = CountVisibleRenderers(camera);
            if (visibleRenderers < 5)
            {
                throw new InvalidOperationException($"Phase 4 graybox must be visible from player start. VisibleRenderers={visibleRenderers}");
            }

            Debug.Log($"Phase 4 cargo ship graybox editor validation passed. Rooms={RequiredRooms.Length}, Corridors={RequiredCorridors.Length}, Interactables={interactables.Length}, VisibleRenderers={visibleRenderers}");
        }

        private static int CountVisibleRenderers(Camera camera)
        {
            var planes = GeometryUtility.CalculateFrustumPlanes(camera);
            var renderers = UnityEngine.Object.FindObjectsByType<MeshRenderer>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            var visibleRendererCount = 0;

            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (!renderer.enabled || !renderer.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (GeometryUtility.TestPlanesAABB(planes, renderer.bounds))
                {
                    visibleRendererCount++;
                }
            }

            return visibleRendererCount;
        }

        private static void RequireSeparatedCargoEntrance(string roomName, string cargoRoomName)
        {
            var roomCenter = Phase4CargoShipGrayboxBootstrap.CorridorEndpoint(roomName, roomName);
            var cargoEntrance = Phase4CargoShipGrayboxBootstrap.CorridorEndpoint(roomName, cargoRoomName);
            var distance = Vector3.Distance(
                new Vector3(roomCenter.x, 0f, roomCenter.z),
                new Vector3(cargoEntrance.x, 0f, cargoEntrance.z));

            if (distance < 2.5f)
            {
                throw new InvalidOperationException(
                    $"{roomName} cargo corridor entrance must be separated from the room center to avoid overlapping other corridor starts. Distance={distance:0.00}");
            }
        }

        private static void RequireSegmentedArmoryCargoCorridor()
        {
            var segmentCount = Phase4CargoShipGrayboxBootstrap.CorridorSegmentCount("Cargo Hold", "Armory");
            if (segmentCount < 12)
            {
                throw new InvalidOperationException($"Cargo Hold to Armory corridor must use a curved sampled route. SegmentCount={segmentCount}");
            }

            var armoryCenter = Phase4CargoShipGrayboxBootstrap.CorridorEndpoint("Armory", "Armory");
            var armoryCargoEntrance = Phase4CargoShipGrayboxBootstrap.CorridorEndpoint("Armory", "Cargo Hold");
            if (armoryCargoEntrance.x >= armoryCenter.x || armoryCargoEntrance.z >= armoryCenter.z)
            {
                throw new InvalidOperationException(
                    $"Armory cargo corridor entrance must be on the south-west side of the Armory. Center={armoryCenter}, Entrance={armoryCargoEntrance}");
            }
        }

    }
}
