using System;
using Bellerophon.Core.Session;
using UnityEngine;

namespace Bellerophon.Core.Ship
{
    public readonly struct ShipInteriorMapRoom
    {
        public ShipInteriorMapRoom(
            ShipRoomId roomId,
            string displayName,
            Vector3 worldCenter,
            Vector2 worldSize,
            Vector2 mapPosition,
            Vector2 mapSize)
        {
            RoomId = roomId;
            DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
            WorldCenter = worldCenter;
            WorldSize = worldSize;
            MapPosition = mapPosition;
            MapSize = mapSize;
        }

        public ShipRoomId RoomId { get; }

        public string DisplayName { get; }

        public Vector3 WorldCenter { get; }

        public Vector2 WorldSize { get; }

        public Vector2 MapPosition { get; }

        public Vector2 MapSize { get; }

        public bool ContainsWorldPosition(Vector3 position)
        {
            var half = WorldSize * 0.5f;
            return position.x >= WorldCenter.x - half.x &&
                   position.x <= WorldCenter.x + half.x &&
                   position.z >= WorldCenter.z - half.y &&
                   position.z <= WorldCenter.z + half.y;
        }
    }

    public static class ShipInteriorMapRules
    {
        public const float ShipInteriorMapScale = 0.8f;

        private static readonly ShipInteriorMapRoom[] Rooms =
        {
            new ShipInteriorMapRoom(
                ShipRoomId.CargoHold,
                "Cargo Hold",
                new Vector3(0f, -3f, 0f),
                new Vector2(12f, 12f),
                new Vector2(0f, -12f),
                new Vector2(96f, 96f)),
            new ShipInteriorMapRoom(
                ShipRoomId.Cockpit,
                "Cockpit",
                new Vector3(0f, 0f, 18f),
                new Vector2(10f, 8f),
                new Vector2(0f, 94f),
                new Vector2(82f, 56f)),
            new ShipInteriorMapRoom(
                ShipRoomId.EngineRoom,
                "Engine Room",
                new Vector3(-14f, 0f, 18f),
                new Vector2(8f, 8f),
                new Vector2(-106f, 94f),
                new Vector2(66f, 56f)),
            new ShipInteriorMapRoom(
                ShipRoomId.ControlRoom,
                "Control Room",
                new Vector3(14f, 0f, 18f),
                new Vector2(8f, 8f),
                new Vector2(106f, 94f),
                new Vector2(66f, 56f)),
            new ShipInteriorMapRoom(
                ShipRoomId.Armory,
                "Armory",
                new Vector3(-14f, 0f, -14f),
                new Vector2(8f, 8f),
                new Vector2(-106f, -110f),
                new Vector2(66f, 56f)),
            new ShipInteriorMapRoom(
                ShipRoomId.SupplyRoom,
                "Supply Room",
                new Vector3(14f, 0f, -14f),
                new Vector2(8f, 8f),
                new Vector2(106f, -110f),
                new Vector2(66f, 56f))
        };

        public static ShipInteriorMapRoom[] GetRooms()
        {
            var clone = new ShipInteriorMapRoom[Rooms.Length];
            Array.Copy(Rooms, clone, Rooms.Length);
            return clone;
        }

        public static ShipRoomId FindCurrentRoom(Vector3 worldPosition)
        {
            for (var i = 0; i < Rooms.Length; i++)
            {
                if (Rooms[i].ContainsWorldPosition(worldPosition))
                {
                    return Rooms[i].RoomId;
                }
            }

            return FindNearestRoom(worldPosition).RoomId;
        }

        public static ShipInteriorMapRoom GetRoom(ShipRoomId roomId)
        {
            for (var i = 0; i < Rooms.Length; i++)
            {
                if (Rooms[i].RoomId == roomId)
                {
                    return Rooms[i];
                }
            }

            throw new ArgumentOutOfRangeException(nameof(roomId), roomId, null);
        }

        public static string FormatRoomName(ShipRoomId roomId)
        {
            return GetRoom(roomId).DisplayName;
        }

        private static ShipInteriorMapRoom FindNearestRoom(Vector3 worldPosition)
        {
            var nearest = Rooms[0];
            var nearestDistance = float.MaxValue;
            for (var i = 0; i < Rooms.Length; i++)
            {
                var deltaX = worldPosition.x - Rooms[i].WorldCenter.x;
                var deltaZ = worldPosition.z - Rooms[i].WorldCenter.z;
                var distance = (deltaX * deltaX) + (deltaZ * deltaZ);
                if (distance >= nearestDistance)
                {
                    continue;
                }

                nearest = Rooms[i];
                nearestDistance = distance;
            }

            return nearest;
        }
    }
}
