using System;

namespace Bellerophon.Core.Session
{
    public readonly struct ShipRepairCostProfile
    {
        public ShipRepairCostProfile(
            int cockpitRate,
            int cargoHoldRate,
            int armoryRate,
            int supplyRoomRate,
            int engineRoomRate,
            int controlRoomRate)
        {
            CockpitRate = RequireNonNegative(cockpitRate, nameof(cockpitRate));
            CargoHoldRate = RequireNonNegative(cargoHoldRate, nameof(cargoHoldRate));
            ArmoryRate = RequireNonNegative(armoryRate, nameof(armoryRate));
            SupplyRoomRate = RequireNonNegative(supplyRoomRate, nameof(supplyRoomRate));
            EngineRoomRate = RequireNonNegative(engineRoomRate, nameof(engineRoomRate));
            ControlRoomRate = RequireNonNegative(controlRoomRate, nameof(controlRoomRate));
        }

        public int CockpitRate { get; }

        public int CargoHoldRate { get; }

        public int ArmoryRate { get; }

        public int SupplyRoomRate { get; }

        public int EngineRoomRate { get; }

        public int ControlRoomRate { get; }

        public static ShipRepairCostProfile OriginalRoomRates =>
            new ShipRepairCostProfile(
                cockpitRate: 30,
                cargoHoldRate: 15,
                armoryRate: 40,
                supplyRoomRate: 5,
                engineRoomRate: 50,
                controlRoomRate: 20);

        public int GetRate(ShipRoomId roomId)
        {
            switch (roomId)
            {
                case ShipRoomId.Cockpit:
                    return CockpitRate;
                case ShipRoomId.CargoHold:
                    return CargoHoldRate;
                case ShipRoomId.Armory:
                    return ArmoryRate;
                case ShipRoomId.SupplyRoom:
                    return SupplyRoomRate;
                case ShipRoomId.EngineRoom:
                    return EngineRoomRate;
                case ShipRoomId.ControlRoom:
                    return ControlRoomRate;
                default:
                    throw new ArgumentOutOfRangeException(nameof(roomId), roomId, null);
            }
        }

        private static int RequireNonNegative(int value, string name)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(name, "Repair cost rates cannot be negative.");
            }

            return value;
        }
    }

    public readonly struct ShipStartAssessment
    {
        public ShipStartAssessment(
            bool isCockpitDestroyed,
            bool isCargoHoldBlocked,
            bool isEngineRoomDestroyed,
            bool hasControlRoomDestroyedWarning)
        {
            IsCockpitDestroyed = isCockpitDestroyed;
            IsCargoHoldBlocked = isCargoHoldBlocked;
            IsEngineRoomDestroyed = isEngineRoomDestroyed;
            HasControlRoomDestroyedWarning = hasControlRoomDestroyedWarning;
        }

        public bool CanStartTransport => !IsCockpitDestroyed && !IsCargoHoldBlocked && !IsEngineRoomDestroyed;

        public bool IsCockpitDestroyed { get; }

        public bool IsCargoHoldBlocked { get; }

        public bool IsEngineRoomDestroyed { get; }

        public bool HasControlRoomDestroyedWarning { get; }
    }

    public static class ShipStateRules
    {
        public const float StableThreshold = 0.75f;
        public const float DamagedThreshold = 0.5f;
        public const float CriticalThreshold = 0.25f;
        public const float CargoHoldBlockedThreshold = 0.25f;
        public const float CargoHoldCriticalCargoLossPercent = 0.2f;
        public const float CargoHoldDestroyedCargoDamagePerSecond = 0.001f;
        public const int TotalLossClaimCost = 5000;

        private static readonly ShipRoomId[] RepairRoomOrder =
        {
            ShipRoomId.Cockpit,
            ShipRoomId.CargoHold,
            ShipRoomId.Armory,
            ShipRoomId.SupplyRoom,
            ShipRoomId.EngineRoom,
            ShipRoomId.ControlRoom
        };

        public static ShipRoomDurabilityTier GetDurabilityTier(ShipRoomState room)
        {
            if (room.CurrentDurability <= 0)
            {
                return ShipRoomDurabilityTier.Destroyed;
            }

            var percent = room.DurabilityPercent;
            if (percent <= CriticalThreshold)
            {
                return ShipRoomDurabilityTier.Critical;
            }

            if (percent <= DamagedThreshold)
            {
                return ShipRoomDurabilityTier.Damaged;
            }

            return percent <= StableThreshold
                ? ShipRoomDurabilityTier.Stable
                : ShipRoomDurabilityTier.Optimal;
        }

        public static int CalculateRepairCost(ShipState ship)
        {
            return CalculateRepairCost(ship, ShipRepairCostProfile.OriginalRoomRates);
        }

        public static int CalculateRepairCost(ShipState ship, ShipRepairCostProfile repairCostProfile)
        {
            if (ship == null)
            {
                throw new ArgumentNullException(nameof(ship));
            }

            var repairCost = 0;
            foreach (var roomId in RepairRoomOrder)
            {
                var room = ship.GetRoom(roomId);
                repairCost += room.MissingDurability * repairCostProfile.GetRate(roomId);
            }

            return repairCost;
        }

        public static int CalculateTotalLossClaimCost(ShipState ship)
        {
            if (ship == null)
            {
                throw new ArgumentNullException(nameof(ship));
            }

            return ship.IsTotalLoss ? TotalLossClaimCost : 0;
        }

        public static float CalculateCargoHoldScore(ShipState ship)
        {
            if (ship == null)
            {
                throw new ArgumentNullException(nameof(ship));
            }

            return ship.GetRoom(ShipRoomId.CargoHold).DurabilityPercent;
        }

        public static float CalculateCargoLossPercentFromCargoHold(ShipState ship)
        {
            if (ship == null)
            {
                throw new ArgumentNullException(nameof(ship));
            }

            return ship.GetRoom(ShipRoomId.CargoHold).DurabilityPercent <= CriticalThreshold
                ? CargoHoldCriticalCargoLossPercent
                : 0f;
        }

        public static CargoState ApplyCargoHoldDamageOverTime(CargoState cargo, ShipState ship, float deltaSeconds)
        {
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), "Delta seconds cannot be negative.");
            }

            if (ship == null)
            {
                throw new ArgumentNullException(nameof(ship));
            }

            if (ship.GetRoom(ShipRoomId.CargoHold).CurrentDurability > 0)
            {
                return cargo;
            }

            return cargo.WithDamagePercent(deltaSeconds * CargoHoldDestroyedCargoDamagePerSecond);
        }

        public static ShipStartAssessment EvaluateStartReadiness(ShipState ship)
        {
            if (ship == null)
            {
                throw new ArgumentNullException(nameof(ship));
            }

            var cockpit = ship.GetRoom(ShipRoomId.Cockpit);
            var cargoHold = ship.GetRoom(ShipRoomId.CargoHold);
            var engineRoom = ship.GetRoom(ShipRoomId.EngineRoom);
            var controlRoom = ship.GetRoom(ShipRoomId.ControlRoom);

            return new ShipStartAssessment(
                cockpit.CurrentDurability <= 0,
                cargoHold.DurabilityPercent <= CargoHoldBlockedThreshold,
                engineRoom.CurrentDurability <= 0,
                controlRoom.CurrentDurability <= 0);
        }
    }
}
