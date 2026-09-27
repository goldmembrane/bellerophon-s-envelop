using System;
using Bellerophon.Core.Session;
using UnityEngine;

namespace Bellerophon.Core.Ship
{
    /// <summary>Authored shutter parts, retracted inside their existing overhead housing.</summary>
    [DisallowMultipleComponent]
    public sealed class PegasusEntranceDoor : MonoBehaviour
    {
        [Serializable]
        public struct Part
        {
            public Transform transform;
            public Vector3 closedPosition, closedScale, openPosition, openScale;
            public Collider[] colliders;
            public bool[] closedColliderEnabled;
        }

        [SerializeField] private ShipDeviceInteractionState state;
        [SerializeField] private ShipRoomId roomA, roomB;
        [SerializeField] private int corridorIndex;
        [SerializeField] private Part[] parts = Array.Empty<Part>();
        [SerializeField] private bool isOpen = true;
        // The overclock release remains until a subsequent loss of control-room durability.
        private int lastOverclockCount;
        private int previousDurability;
        private bool releasedByOverclock;

        public bool IsOpen => isOpen;
        public int CorridorIndex => corridorIndex;
        public ShipRoomId RoomA => roomA;
        public ShipRoomId RoomB => roomB;

        public void Configure(ShipDeviceInteractionState source, ShipRoomId a, ShipRoomId b,
            int index, Part[] authoredParts)
        {
            state = source; roomA = a; roomB = b; corridorIndex = index; parts = authoredParts;
            ApplyOpen(true);
        }

        private void Start()
        {
            if (!state) return;
            lastOverclockCount = state.EngineOverclockActivationCount;
            previousDurability = state.CurrentShipState.GetRoom(ShipRoomId.ControlRoom).CurrentDurability;
            RefreshState();
        }

        private void Update() { RefreshState(); }

        private void RefreshState()
        {
            if (!state) return;
            var ship = state.CurrentShipState;
            var durability = ship.GetRoom(ShipRoomId.ControlRoom).CurrentDurability;
            var activation = state.EngineOverclockActivationCount;
            if (activation < lastOverclockCount) releasedByOverclock = false;
            if (durability < previousDurability) releasedByOverclock = false;
            if (activation > lastOverclockCount) releasedByOverclock = true;
            previousDurability = durability;
            lastOverclockCount = activation;
            // Match the existing IntruderRules corridor ordering and sealed-room restrictions.
            var closedByDamage = !releasedByOverclock &&
                corridorIndex < IntruderRules.CalculateClosedCorridorCount(ship);
            var closedByRoom = ship.GetRoom(roomA).IsSealed || ship.GetRoom(roomB).IsSealed;
            var open = durability == 0 || !(closedByDamage || closedByRoom);
            if (open != isOpen) ApplyOpen(open);
        }

        private void ApplyOpen(bool open)
        {
            foreach (var part in parts)
            {
                if (!part.transform) continue;
                part.transform.localPosition = open ? part.openPosition : part.closedPosition;
                part.transform.localScale = open ? part.openScale : part.closedScale;
                for (var i = 0; i < part.colliders.Length; i++)
                    if (part.colliders[i]) part.colliders[i].enabled = !open && part.closedColliderEnabled[i];
            }
            isOpen = open;
        }
    }
}
