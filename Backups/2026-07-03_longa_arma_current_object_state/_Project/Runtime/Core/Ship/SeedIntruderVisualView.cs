using System;
using Bellerophon.Core.Session;
using UnityEngine;

namespace Bellerophon.Core.Ship
{
    public sealed class SeedIntruderVisualView : MonoBehaviour
    {
        [SerializeField] private ShipDeviceInteractionState interactionState;
        [SerializeField] private GameObject parvumVisualRoot;
        [SerializeField] private Transform cockpitAnchor;
        [SerializeField] private Transform cargoHoldAnchor;
        [SerializeField] private Transform engineRoomAnchor;
        [SerializeField] private Transform controlRoomAnchor;
        [SerializeField] private Transform armoryAnchor;
        [SerializeField] private Transform supplyRoomAnchor;

        private ShipRoomId lastDisplayedRoom;

        public GameObject ParvumVisualRoot => parvumVisualRoot;

        public bool IsViewActive => parvumVisualRoot != null && parvumVisualRoot.activeSelf;

        public ShipRoomId LastDisplayedRoom => lastDisplayedRoom;

        public bool HasAllRoomAnchorsForValidation =>
            cockpitAnchor != null &&
            cargoHoldAnchor != null &&
            engineRoomAnchor != null &&
            controlRoomAnchor != null &&
            armoryAnchor != null &&
            supplyRoomAnchor != null;

        private void Awake()
        {
            if (interactionState == null)
            {
                interactionState = FindFirstObjectByType<ShipDeviceInteractionState>();
            }

            RefreshView();
        }

        private void Update()
        {
            RefreshView();
        }

        public void Configure(
            ShipDeviceInteractionState state,
            GameObject visualRoot,
            Transform cockpit,
            Transform cargoHold,
            Transform engineRoom,
            Transform controlRoom,
            Transform armory,
            Transform supplyRoom)
        {
            interactionState = state;
            parvumVisualRoot = visualRoot;
            cockpitAnchor = cockpit;
            cargoHoldAnchor = cargoHold;
            engineRoomAnchor = engineRoom;
            controlRoomAnchor = controlRoom;
            armoryAnchor = armory;
            supplyRoomAnchor = supplyRoom;
            RefreshView();
        }

        public void RefreshView()
        {
            if (parvumVisualRoot == null)
            {
                return;
            }

            if (interactionState == null)
            {
                parvumVisualRoot.SetActive(false);
                return;
            }

            var intruder = interactionState.CurrentSeedIntruder;
            if (intruder.Kind != SeedIntruderKind.Parvum || !intruder.IsActive)
            {
                parvumVisualRoot.SetActive(false);
                return;
            }

            var displayRoom = intruder.Intruder.CurrentRoom;
            var anchor = GetAnchor(displayRoom) ?? GetAnchor(intruder.TargetRoom);
            if (anchor == null)
            {
                parvumVisualRoot.SetActive(false);
                return;
            }

            lastDisplayedRoom = displayRoom;
            parvumVisualRoot.transform.SetPositionAndRotation(anchor.position, anchor.rotation);
            parvumVisualRoot.SetActive(true);
        }

        public Transform GetAnchorForValidation(ShipRoomId roomId)
        {
            return GetAnchor(roomId);
        }

        private Transform GetAnchor(ShipRoomId roomId)
        {
            switch (roomId)
            {
                case ShipRoomId.Cockpit:
                    return cockpitAnchor;
                case ShipRoomId.CargoHold:
                    return cargoHoldAnchor;
                case ShipRoomId.EngineRoom:
                    return engineRoomAnchor;
                case ShipRoomId.ControlRoom:
                    return controlRoomAnchor;
                case ShipRoomId.Armory:
                    return armoryAnchor;
                case ShipRoomId.SupplyRoom:
                    return supplyRoomAnchor;
                default:
                    throw new ArgumentOutOfRangeException(nameof(roomId), roomId, null);
            }
        }
    }
}
