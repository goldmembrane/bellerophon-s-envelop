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
            bool hasControlRoomDestroyedWarning,
            bool isPersonalCargoBlocked = false)
        {
            IsCockpitDestroyed = isCockpitDestroyed;
            IsCargoHoldBlocked = isCargoHoldBlocked;
            IsEngineRoomDestroyed = isEngineRoomDestroyed;
            HasControlRoomDestroyedWarning = hasControlRoomDestroyedWarning;
            IsPersonalCargoBlocked = isPersonalCargoBlocked;
        }

        public bool CanStartTransport =>
            !IsCockpitDestroyed &&
            !IsCargoHoldBlocked &&
            !IsEngineRoomDestroyed &&
            !IsPersonalCargoBlocked;

        public bool IsCockpitDestroyed { get; }

        public bool IsCargoHoldBlocked { get; }

        public bool IsEngineRoomDestroyed { get; }

        public bool HasControlRoomDestroyedWarning { get; }

        public bool IsPersonalCargoBlocked { get; }
    }

    public static class ShipStateRules
    {
        public const float StableThreshold = 0.75f;
        public const float DamagedThreshold = 0.5f;
        public const float CriticalThreshold = 0.25f;
        public const float CargoHoldBlockedThreshold = 0.25f;
        public const float PersonalCargoTransportOfflineThreshold = DamagedThreshold;
        public const float CargoHoldCriticalCargoLossPercent = 0.2f;
        public const float CargoHoldDestroyedCargoDamagePerSecond = 0.001f;
        public const float AutoPilotOfflineThreshold = DamagedThreshold;
        public const float CockpitStableManualInputMultiplier = 0.75f;
        public const float CockpitCriticalManualInputMultiplier = 0.5f;
        public const float ArmoryCriticalManualAimMultiplier = 0.5f;
        public const int DefaultControlRoomCctvCount = 4;
        public const int ControlRoomDamagedCctvCount = 2;
        public const int SupplyRoomStableSlotPenalty = 3;
        public const int SupplyRoomDamagedAdditionalSlotPenalty = 2;
        public const float SupplyRoomCriticalEquipmentDamagePercent = 0.1f;
        public const int SupplyRoomDestroyedEquipmentExplosionChancePercent = 25;
        public const float ControlRoomDestroyedIntruderStatMultiplier = 3f;
        public const int SettlementSummaryRepairRatePerPercent = 5;
        public const int MaxNormalRepairMissingPercent = 599;
        public const int TotalLossClaimCost = 5000;
        public const int FirstTowingCost = 2000;
        public const int SecondTowingCost = 3000;
        public const int ThirdTowingCost = 5000;
        public const int AdditionalTowingCostAfterThird = 2500;
        public const int ShipLossInsurancePayout90 = 500;
        public const int ShipLossInsurancePayout80 = 1000;
        public const int ShipLossInsurancePayout70 = 1400;
        public const int ShipLossInsurancePayout60 = 1450;
        public const int ShipLossInsurancePayout50 = 1500;

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
            if (ship == null)
            {
                throw new ArgumentNullException(nameof(ship));
            }

            if (ship.IsTotalLoss)
            {
                return 0;
            }

            var missingPercent = CalculateMissingDurabilityPercent(ship);
            return Math.Min(missingPercent, MaxNormalRepairMissingPercent) * SettlementSummaryRepairRatePerPercent;
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

        public static int CalculateMissingDurabilityPercent(ShipState ship)
        {
            if (ship == null)
            {
                throw new ArgumentNullException(nameof(ship));
            }

            var missingPercent = 0;
            foreach (var roomId in RepairRoomOrder)
            {
                var room = ship.GetRoom(roomId);
                missingPercent += CalculateRoomMissingDurabilityPercent(room);
            }

            return missingPercent;
        }

        public static int CalculateTowingCost(int towingIncidentNumber)
        {
            if (towingIncidentNumber <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(towingIncidentNumber), "Towing incident number must be positive.");
            }

            if (towingIncidentNumber == 1)
            {
                return FirstTowingCost;
            }

            if (towingIncidentNumber == 2)
            {
                return SecondTowingCost;
            }

            return ThirdTowingCost + ((towingIncidentNumber - 3) * AdditionalTowingCostAfterThird);
        }

        public static int CalculateTotalLossClaimCost(ShipState ship)
        {
            if (ship == null)
            {
                throw new ArgumentNullException(nameof(ship));
            }

            return ship.IsTotalLoss ? TotalLossClaimCost : 0;
        }

        public static ShipState RepairAllRooms(ShipState ship)
        {
            if (ship == null)
            {
                throw new ArgumentNullException(nameof(ship));
            }

            foreach (var roomId in RepairRoomOrder)
            {
                var room = ship.GetRoom(roomId);
                ship = ship.WithRoom(roomId, new ShipRoomState(room.MaxDurability, room.MaxDurability));
            }

            return ship.WithRunState(ShipRunState.Docked);
        }

        public static int CalculateShipLossInsurancePayout(ShipState ship)
        {
            if (ship == null)
            {
                throw new ArgumentNullException(nameof(ship));
            }

            var averageDurability = ship.AverageDurabilityPercent;
            if (averageDurability >= 1f || averageDurability < DamagedThreshold)
            {
                return 0;
            }

            if (averageDurability >= 0.9f)
            {
                return ShipLossInsurancePayout90;
            }

            if (averageDurability >= 0.8f)
            {
                return ShipLossInsurancePayout80;
            }

            if (averageDurability >= 0.7f)
            {
                return ShipLossInsurancePayout70;
            }

            if (averageDurability >= 0.6f)
            {
                return ShipLossInsurancePayout60;
            }

            return ShipLossInsurancePayout50;
        }

        public static float CalculateCargoHoldScore(ShipState ship)
        {
            if (ship == null)
            {
                throw new ArgumentNullException(nameof(ship));
            }

            return CalculateCargoHoldCapacityMultiplier(ship);
        }

        public static float CalculateCargoHoldCapacityMultiplier(ShipState ship)
        {
            if (ship == null)
            {
                throw new ArgumentNullException(nameof(ship));
            }

            return ship.GetRoom(ShipRoomId.CargoHold).DurabilityPercent;
        }

        public static bool CanTransportPersonalCargo(ShipState ship)
        {
            if (ship == null)
            {
                throw new ArgumentNullException(nameof(ship));
            }

            return ship.GetRoom(ShipRoomId.CargoHold).DurabilityPercent > PersonalCargoTransportOfflineThreshold;
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

        public static ShipStartAssessment EvaluateStartReadiness(ShipState ship, bool hasPersonalCargo = false)
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
                controlRoom.CurrentDurability <= 0,
                hasPersonalCargo && !CanTransportPersonalCargo(ship));
        }

        public static bool CanUseAutoPilot(ShipState ship)
        {
            if (ship == null)
            {
                throw new ArgumentNullException(nameof(ship));
            }

            return ship.GetRoom(ShipRoomId.Cockpit).DurabilityPercent > AutoPilotOfflineThreshold;
        }

        public static int CalculateEffectiveTransportDurationSeconds(int baseDurationSeconds, ShipState ship)
        {
            if (baseDurationSeconds <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(baseDurationSeconds), "Transport duration must be positive.");
            }

            if (ship == null)
            {
                throw new ArgumentNullException(nameof(ship));
            }

            return (int)Math.Ceiling(baseDurationSeconds * CalculateTransportDurationMultiplier(ship));
        }

        public static float CalculateTransportDurationMultiplier(ShipState ship)
        {
            if (ship == null)
            {
                throw new ArgumentNullException(nameof(ship));
            }

            var multiplier = 1f;
            if (ship.GetRoom(ShipRoomId.Cockpit).CurrentDurability <= 0)
            {
                multiplier += 1f;
            }

            var enginePercent = ship.GetRoom(ShipRoomId.EngineRoom).DurabilityPercent;
            if (enginePercent <= StableThreshold)
            {
                multiplier += 0.5f;
            }

            if (enginePercent <= DamagedThreshold)
            {
                multiplier += 1f;
            }

            if (enginePercent <= CriticalThreshold)
            {
                multiplier += 1f;
            }

            return multiplier;
        }

        public static float CalculateManualFlightInputMultiplier(ShipState ship)
        {
            if (ship == null)
            {
                throw new ArgumentNullException(nameof(ship));
            }

            var cockpit = ship.GetRoom(ShipRoomId.Cockpit);
            if (cockpit.CurrentDurability <= 0)
            {
                return 0f;
            }

            if (cockpit.DurabilityPercent <= CriticalThreshold)
            {
                return CockpitCriticalManualInputMultiplier;
            }

            return cockpit.DurabilityPercent <= StableThreshold
                ? CockpitStableManualInputMultiplier
                : 1f;
        }

        public static bool CanUseEngineOverclock(ShipState ship)
        {
            if (ship == null)
            {
                throw new ArgumentNullException(nameof(ship));
            }

            var engineRoom = ship.GetRoom(ShipRoomId.EngineRoom);
            return engineRoom.DurabilityPercent > DamagedThreshold && !engineRoom.IsFunctionOffline;
        }

        public static bool CanUseBooster(ShipState ship)
        {
            return CanUseEngineOverclock(ship);
        }

        public static int CalculateEngineBlackoutRoomCount(ShipState ship)
        {
            if (ship == null)
            {
                throw new ArgumentNullException(nameof(ship));
            }

            var enginePercent = ship.GetRoom(ShipRoomId.EngineRoom).DurabilityPercent;
            if (enginePercent <= CriticalThreshold)
            {
                return 6;
            }

            if (enginePercent <= DamagedThreshold)
            {
                return 5;
            }

            return enginePercent <= StableThreshold ? 2 : 0;
        }

        public static int CalculateEngineOfflineRoomCount(ShipState ship)
        {
            if (ship == null)
            {
                throw new ArgumentNullException(nameof(ship));
            }

            var enginePercent = ship.GetRoom(ShipRoomId.EngineRoom).DurabilityPercent;
            if (enginePercent <= CriticalThreshold)
            {
                return 5;
            }

            return enginePercent <= DamagedThreshold ? 1 : 0;
        }

        public static bool CanUseManualTurret(ShipState ship)
        {
            if (ship == null)
            {
                throw new ArgumentNullException(nameof(ship));
            }

            var armory = ship.GetRoom(ShipRoomId.Armory);
            return armory.CurrentDurability > 0 && !armory.IsFunctionOffline;
        }

        public static bool IsPlasmaCannonAvailable(ShipState ship)
        {
            if (ship == null)
            {
                throw new ArgumentNullException(nameof(ship));
            }

            var armory = ship.GetRoom(ShipRoomId.Armory);
            return armory.DurabilityPercent > StableThreshold && !armory.IsFunctionOffline;
        }

        public static bool IsAutoAimOnline(ShipState ship)
        {
            return IsPlasmaCannonAvailable(ship);
        }

        public static int CalculateActiveAutoTurretCount(ShipState ship, int baseTurretCount)
        {
            if (baseTurretCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(baseTurretCount), "Base turret count cannot be negative.");
            }

            if (ship == null)
            {
                throw new ArgumentNullException(nameof(ship));
            }

            if (baseTurretCount == 0 || ship.GetRoom(ShipRoomId.Armory).CurrentDurability <= 0)
            {
                return 0;
            }

            return ship.GetRoom(ShipRoomId.Armory).DurabilityPercent <= DamagedThreshold
                ? Math.Max(1, (baseTurretCount + 1) / 2)
                : baseTurretCount;
        }

        public static float CalculateManualTurretAimMultiplier(ShipState ship)
        {
            if (ship == null)
            {
                throw new ArgumentNullException(nameof(ship));
            }

            var armory = ship.GetRoom(ShipRoomId.Armory);
            if (armory.CurrentDurability <= 0)
            {
                return 0f;
            }

            return armory.DurabilityPercent <= CriticalThreshold
                ? ArmoryCriticalManualAimMultiplier
                : 1f;
        }

        public static int CalculateControlRoomClosedCorridorPercent(ShipState ship)
        {
            if (ship == null)
            {
                throw new ArgumentNullException(nameof(ship));
            }

            var controlRoom = ship.GetRoom(ShipRoomId.ControlRoom);
            if (controlRoom.CurrentDurability <= 0)
            {
                return 0;
            }

            if (controlRoom.DurabilityPercent <= CriticalThreshold)
            {
                return 90;
            }

            if (controlRoom.DurabilityPercent <= DamagedThreshold)
            {
                return 50;
            }

            return controlRoom.DurabilityPercent <= StableThreshold ? 20 : 0;
        }

        public static int CalculateControlRoomAvailableCctvCount(ShipState ship)
        {
            if (ship == null)
            {
                throw new ArgumentNullException(nameof(ship));
            }

            var controlRoom = ship.GetRoom(ShipRoomId.ControlRoom);
            if (controlRoom.DurabilityPercent <= CriticalThreshold || controlRoom.IsFunctionOffline)
            {
                return 0;
            }

            return controlRoom.DurabilityPercent <= DamagedThreshold
                ? ControlRoomDamagedCctvCount
                : DefaultControlRoomCctvCount;
        }

        public static bool CanUseControlRoomCctv(ShipState ship)
        {
            return CalculateControlRoomAvailableCctvCount(ship) > 0;
        }

        public static bool IsIntruderDetectionOnline(ShipState ship)
        {
            if (ship == null)
            {
                throw new ArgumentNullException(nameof(ship));
            }

            var controlRoom = ship.GetRoom(ShipRoomId.ControlRoom);
            return controlRoom.DurabilityPercent > DamagedThreshold && !controlRoom.IsFunctionOffline;
        }

        public static bool IsCargoDamageWarningOnline(ShipState ship)
        {
            if (ship == null)
            {
                throw new ArgumentNullException(nameof(ship));
            }

            var controlRoom = ship.GetRoom(ShipRoomId.ControlRoom);
            return controlRoom.DurabilityPercent > CriticalThreshold && !controlRoom.IsFunctionOffline;
        }

        public static bool IsIntruderSuppressionOnline(ShipState ship)
        {
            if (ship == null)
            {
                throw new ArgumentNullException(nameof(ship));
            }

            var controlRoom = ship.GetRoom(ShipRoomId.ControlRoom);
            return controlRoom.CurrentDurability > 0 && !controlRoom.IsFunctionOffline;
        }

        public static int CalculateSeedIntruderOccurrencePercent(int baseOccurrencePercent, ShipState ship)
        {
            if (baseOccurrencePercent < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(baseOccurrencePercent), "Occurrence percent cannot be negative.");
            }

            return IsIntruderSuppressionOnline(ship)
                ? baseOccurrencePercent
                : baseOccurrencePercent * 2;
        }

        public static int CalculateInternalIntruderRoomDamage(int baseRoomDamage, ShipState ship)
        {
            if (baseRoomDamage < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(baseRoomDamage), "Room damage cannot be negative.");
            }

            return IsIntruderSuppressionOnline(ship)
                ? baseRoomDamage
                : (int)Math.Ceiling(baseRoomDamage * ControlRoomDestroyedIntruderStatMultiplier);
        }

        public static int CalculateSupplyStorageSlotCount(ShipState ship, int baseSlotCount)
        {
            if (baseSlotCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(baseSlotCount), "Base slot count cannot be negative.");
            }

            if (ship == null)
            {
                throw new ArgumentNullException(nameof(ship));
            }

            var supplyRoom = ship.GetRoom(ShipRoomId.SupplyRoom);
            if (supplyRoom.DurabilityPercent <= CriticalThreshold || supplyRoom.IsFunctionOffline)
            {
                return 0;
            }

            var penalty = 0;
            if (supplyRoom.DurabilityPercent <= StableThreshold)
            {
                penalty += SupplyRoomStableSlotPenalty;
            }

            if (supplyRoom.DurabilityPercent <= DamagedThreshold)
            {
                penalty += SupplyRoomDamagedAdditionalSlotPenalty;
            }

            return Math.Max(0, baseSlotCount - penalty);
        }

        public static bool IsSupplyStorageSecurityOnline(ShipState ship)
        {
            if (ship == null)
            {
                throw new ArgumentNullException(nameof(ship));
            }

            var supplyRoom = ship.GetRoom(ShipRoomId.SupplyRoom);
            return supplyRoom.DurabilityPercent > DamagedThreshold && !supplyRoom.IsFunctionOffline;
        }

        public static float CalculateSupplyEquipmentDurabilityDamagePercent(ShipState ship)
        {
            if (ship == null)
            {
                throw new ArgumentNullException(nameof(ship));
            }

            return ship.GetRoom(ShipRoomId.SupplyRoom).DurabilityPercent <= CriticalThreshold
                ? SupplyRoomCriticalEquipmentDamagePercent
                : 0f;
        }

        public static int CalculateSupplyEquipmentExplosionChancePercent(ShipState ship)
        {
            if (ship == null)
            {
                throw new ArgumentNullException(nameof(ship));
            }

            return ship.GetRoom(ShipRoomId.SupplyRoom).CurrentDurability <= 0
                ? SupplyRoomDestroyedEquipmentExplosionChancePercent
                : 0;
        }

        public static string BuildRoomDamageEffectSummary(ShipState ship, ShipRoomId roomId)
        {
            if (ship == null)
            {
                throw new ArgumentNullException(nameof(ship));
            }

            switch (roomId)
            {
                case ShipRoomId.Cockpit:
                    return "Manual input " + FormatPercent(CalculateManualFlightInputMultiplier(ship)) +
                           "; auto pilot " + FormatOnline(CanUseAutoPilot(ship));
                case ShipRoomId.CargoHold:
                    return "Capacity " + FormatPercent(CalculateCargoHoldCapacityMultiplier(ship)) +
                           "; personal cargo " + FormatOnline(CanTransportPersonalCargo(ship));
                case ShipRoomId.EngineRoom:
                    return "Duration x" + CalculateTransportDurationMultiplier(ship).ToString("0.##") +
                           "; booster " + FormatOnline(CanUseBooster(ship)) +
                           "; blackout rooms " + CalculateEngineBlackoutRoomCount(ship);
                case ShipRoomId.ControlRoom:
                    return "Corridor seal " + CalculateControlRoomClosedCorridorPercent(ship) + "%" +
                           "; CCTV " + CalculateControlRoomAvailableCctvCount(ship) + "/" + DefaultControlRoomCctvCount +
                           "; suppression " + FormatOnline(IsIntruderSuppressionOnline(ship));
                case ShipRoomId.Armory:
                    return "Manual turret " + FormatOnline(CanUseManualTurret(ship)) +
                           "; auto aim " + FormatOnline(IsAutoAimOnline(ship)) +
                           "; plasma " + FormatOnline(IsPlasmaCannonAvailable(ship));
                case ShipRoomId.SupplyRoom:
                    return "Storage slots " + CalculateSupplyStorageSlotCount(ship, PlayerEquipmentState.DefaultSupplySlotCount) +
                           "/" + PlayerEquipmentState.DefaultSupplySlotCount +
                           "; security " + FormatOnline(IsSupplyStorageSecurityOnline(ship)) +
                           "; equipment damage " + FormatPercent(CalculateSupplyEquipmentDurabilityDamagePercent(ship));
                default:
                    throw new ArgumentOutOfRangeException(nameof(roomId), roomId, null);
            }
        }

        private static int CalculateRoomMissingDurabilityPercent(ShipRoomState room)
        {
            var currentPercent = (int)Math.Floor((double)room.CurrentDurability * 100d / room.MaxDurability);
            return Math.Max(0, 100 - currentPercent);
        }

        private static string FormatOnline(bool online)
        {
            return online ? "Online" : "Offline";
        }

        private static string FormatPercent(float value)
        {
            return (int)Math.Round(Math.Max(0f, value) * 100f) + "%";
        }
    }
}
