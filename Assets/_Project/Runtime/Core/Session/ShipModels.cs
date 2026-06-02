using System;
using System.Collections.Generic;

namespace Bellerophon.Core.Session
{
    public enum ShipRoomId
    {
        Cockpit,
        CargoHold,
        Armory,
        SupplyRoom,
        EngineRoom,
        ControlRoom
    }

    public enum ShipRoomDurabilityTier
    {
        Optimal,
        Stable,
        Damaged,
        Critical,
        Destroyed
    }

    public enum ShipRunState
    {
        Docked,
        InTransit,
        Completed,
        Failed
    }

    public readonly struct ShipRoomState
    {
        public ShipRoomState(
            int currentDurability,
            int maxDurability,
            bool isFunctionOffline = false,
            bool isBlackout = false,
            bool isSealed = false)
        {
            if (maxDurability <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxDurability), "Max durability must be positive.");
            }

            MaxDurability = maxDurability;
            CurrentDurability = Clamp(currentDurability, 0, maxDurability);
            IsFunctionOffline = isFunctionOffline;
            IsBlackout = isBlackout;
            IsSealed = isSealed;
        }

        public int CurrentDurability { get; }

        public int MaxDurability { get; }

        public bool IsFunctionOffline { get; }

        public bool IsBlackout { get; }

        public bool IsSealed { get; }

        public float DurabilityPercent => MaxDurability == 0 ? 0f : (float)CurrentDurability / MaxDurability;

        public int MissingDurability => MaxDurability - CurrentDurability;

        public ShipRoomDurabilityTier DurabilityTier => ShipStateRules.GetDurabilityTier(this);

        public ShipRoomState WithDamage(int damage)
        {
            if (damage < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(damage), "Damage cannot be negative.");
            }

            return new ShipRoomState(
                CurrentDurability - damage,
                MaxDurability,
                IsFunctionOffline,
                IsBlackout,
                IsSealed);
        }

        public ShipRoomState WithFunctionOffline(bool isFunctionOffline)
        {
            return new ShipRoomState(CurrentDurability, MaxDurability, isFunctionOffline, IsBlackout, IsSealed);
        }

        public ShipRoomState WithBlackout(bool isBlackout)
        {
            return new ShipRoomState(CurrentDurability, MaxDurability, IsFunctionOffline, isBlackout, IsSealed);
        }

        public ShipRoomState WithSealed(bool isSealed)
        {
            return new ShipRoomState(CurrentDurability, MaxDurability, IsFunctionOffline, IsBlackout, isSealed);
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min)
            {
                return min;
            }

            return value > max ? max : value;
        }
    }

    public sealed class ShipState
    {
        private static readonly ShipRoomId[] RequiredRoomIds =
        {
            ShipRoomId.Cockpit,
            ShipRoomId.CargoHold,
            ShipRoomId.Armory,
            ShipRoomId.SupplyRoom,
            ShipRoomId.EngineRoom,
            ShipRoomId.ControlRoom
        };

        private readonly Dictionary<ShipRoomId, ShipRoomState> rooms;

        private ShipState(Dictionary<ShipRoomId, ShipRoomState> rooms, ShipRunState runState)
        {
            this.rooms = rooms;
            RunState = runState;
        }

        public ShipRunState RunState { get; }

        public float AverageDurabilityPercent
        {
            get
            {
                var total = 0f;
                foreach (var roomId in RequiredRoomIds)
                {
                    total += GetRoom(roomId).DurabilityPercent;
                }

                return total / RequiredRoomIds.Length;
            }
        }

        public bool RequiresTowing => GetRoom(ShipRoomId.EngineRoom).CurrentDurability <= 0;

        public bool IsTotalLoss
        {
            get
            {
                foreach (var roomId in RequiredRoomIds)
                {
                    if (GetRoom(roomId).CurrentDurability > 0)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public bool IsTransportFailed => RequiresTowing || RunState == ShipRunState.Failed;

        public static ShipState CreateDefault(int roomMaxDurability = 100)
        {
            var rooms = new Dictionary<ShipRoomId, ShipRoomState>();
            foreach (var roomId in RequiredRoomIds)
            {
                rooms.Add(roomId, new ShipRoomState(roomMaxDurability, roomMaxDurability));
            }

            return new ShipState(rooms, ShipRunState.Docked);
        }

        public ShipRoomState GetRoom(ShipRoomId roomId)
        {
            if (!rooms.TryGetValue(roomId, out var room))
            {
                throw new ArgumentOutOfRangeException(nameof(roomId), roomId, "Unknown ship room.");
            }

            return room;
        }

        public ShipState WithRoom(ShipRoomId roomId, ShipRoomState room)
        {
            var nextRooms = new Dictionary<ShipRoomId, ShipRoomState>(rooms);
            nextRooms[roomId] = room;
            return new ShipState(nextRooms, RunState);
        }

        public ShipState WithRunState(ShipRunState runState)
        {
            return new ShipState(new Dictionary<ShipRoomId, ShipRoomState>(rooms), runState);
        }
    }
}
