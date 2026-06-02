using System;
using Bellerophon.Core.Session;
using UnityEngine;

namespace Bellerophon.Core.Ship
{
    public enum ShipDeviceType
    {
        CockpitHelm,
        EngineRoomPowerScreen,
        ControlRoomMainScreen,
        ArmoryTurretHandle,
        SupplyRoomStorageCabinet,
        CargoHoldCargoStatus
    }

    public enum ShipDevicePanelMode
    {
        None,
        ManualFlight,
        EngineStatus,
        ControlRoom,
        TurretManual,
        SupplyStorage,
        CargoStatus
    }

    public enum ShipCctvTarget
    {
        Cockpit,
        CargoHold,
        EngineRoom,
        Armory
    }

    public sealed class ShipDeviceInteractionState : MonoBehaviour
    {
        private const int DefaultSupplySlotCount = 3;

        private readonly string[] supplySlotLabels =
        {
            "Empty",
            "Empty",
            "Empty"
        };

        private ShipState shipState;
        private CargoState cargoState;
        private bool isInitialized;
        private ShipDevicePanelMode activePanelMode;
        private ShipCctvTarget currentCctvTarget;
        private bool manualFlightModeActive;
        private bool turretManualModeActive;
        private bool engineOverclockActive;
        private bool engineOverclockUsedThisRun;
        private int engineOverclockActivationCount;
        private string lastInteractionSummary = string.Empty;

        public ShipState CurrentShipState
        {
            get
            {
                EnsureInitialized();
                return shipState;
            }
        }

        public CargoState CurrentCargoState
        {
            get
            {
                EnsureInitialized();
                return cargoState;
            }
        }

        public ShipDevicePanelMode ActivePanelMode => activePanelMode;

        public ShipCctvTarget CurrentCctvTarget => currentCctvTarget;

        public bool ManualFlightModeActive => manualFlightModeActive;

        public bool TurretManualModeActive => turretManualModeActive;

        public bool EngineOverclockActive => engineOverclockActive;

        public bool EngineOverclockUsedThisRun => engineOverclockUsedThisRun;

        public int EngineOverclockActivationCount => engineOverclockActivationCount;

        public int SupplySlotCount => DefaultSupplySlotCount;

        public string LastInteractionSummary => lastInteractionSummary;

        private void Awake()
        {
            EnsureInitialized();
        }

        public void EnsureInitialized()
        {
            if (isInitialized)
            {
                return;
            }

            shipState = ShipState.CreateDefault();
            cargoState = new CargoState(CargoGrade.Common, 50, 100, 1f, false);
            activePanelMode = ShipDevicePanelMode.None;
            currentCctvTarget = ShipCctvTarget.Cockpit;
            isInitialized = true;
        }

        public string ActivateDevice(ShipDeviceType deviceType)
        {
            EnsureInitialized();

            switch (deviceType)
            {
                case ShipDeviceType.CockpitHelm:
                    activePanelMode = ShipDevicePanelMode.ManualFlight;
                    manualFlightModeActive = true;
                    lastInteractionSummary = "Manual flight mode frame entered.";
                    break;
                case ShipDeviceType.EngineRoomPowerScreen:
                    activePanelMode = ShipDevicePanelMode.EngineStatus;
                    lastInteractionSummary = ActivateEngineScreen();
                    break;
                case ShipDeviceType.ControlRoomMainScreen:
                    activePanelMode = ShipDevicePanelMode.ControlRoom;
                    lastInteractionSummary = "Control room screen opened.";
                    break;
                case ShipDeviceType.ArmoryTurretHandle:
                    activePanelMode = ShipDevicePanelMode.TurretManual;
                    turretManualModeActive = true;
                    lastInteractionSummary = "Manual turret mode frame entered.";
                    break;
                case ShipDeviceType.SupplyRoomStorageCabinet:
                    activePanelMode = ShipDevicePanelMode.SupplyStorage;
                    lastInteractionSummary = "Supply storage opened.";
                    break;
                case ShipDeviceType.CargoHoldCargoStatus:
                    activePanelMode = ShipDevicePanelMode.CargoStatus;
                    lastInteractionSummary = "Cargo status opened.";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(deviceType), deviceType, null);
            }

            return lastInteractionSummary;
        }

        public void CycleCctv(int direction)
        {
            EnsureInitialized();
            if (activePanelMode != ShipDevicePanelMode.ControlRoom || direction == 0)
            {
                return;
            }

            var nextIndex = GetCctvIndex(currentCctvTarget) + (direction > 0 ? 1 : -1);
            if (nextIndex < 0)
            {
                nextIndex = CctvOrder.Length - 1;
            }
            else if (nextIndex >= CctvOrder.Length)
            {
                nextIndex = 0;
            }

            currentCctvTarget = CctvOrder[nextIndex];
            lastInteractionSummary = "CCTV target changed to " + GetCctvDisplayName(currentCctvTarget) + ".";
        }

        public string GetSupplySlotLabel(int index)
        {
            if (index < 0 || index >= supplySlotLabels.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, null);
            }

            return supplySlotLabels[index];
        }

        public void SetShipStateForValidation(ShipState nextShipState)
        {
            shipState = nextShipState ?? throw new ArgumentNullException(nameof(nextShipState));
            isInitialized = true;
        }

        public void SetCargoState(CargoState nextCargoState)
        {
            cargoState = nextCargoState;
            isInitialized = true;
        }

        public void SetCargoStateForValidation(CargoState nextCargoState)
        {
            SetCargoState(nextCargoState);
        }

        public static string GetCctvDisplayName(ShipCctvTarget target)
        {
            switch (target)
            {
                case ShipCctvTarget.Cockpit:
                    return "Cockpit";
                case ShipCctvTarget.CargoHold:
                    return "Cargo Hold";
                case ShipCctvTarget.EngineRoom:
                    return "Engine Room";
                case ShipCctvTarget.Armory:
                    return "Armory";
                default:
                    throw new ArgumentOutOfRangeException(nameof(target), target, null);
            }
        }

        private string ActivateEngineScreen()
        {
            var engineRoom = shipState.GetRoom(ShipRoomId.EngineRoom);
            if (engineRoom.DurabilityPercent <= 0.2f)
            {
                engineOverclockActive = false;
                return "Engine power screen is damaged.";
            }

            if (engineOverclockUsedThisRun)
            {
                return "Engine overclock was already used this run.";
            }

            engineOverclockUsedThisRun = true;
            engineOverclockActive = true;
            engineOverclockActivationCount++;
            return "Engine overclock activated.";
        }

        private static int GetCctvIndex(ShipCctvTarget target)
        {
            for (var i = 0; i < CctvOrder.Length; i++)
            {
                if (CctvOrder[i] == target)
                {
                    return i;
                }
            }

            return 0;
        }

        private static readonly ShipCctvTarget[] CctvOrder =
        {
            ShipCctvTarget.Cockpit,
            ShipCctvTarget.CargoHold,
            ShipCctvTarget.EngineRoom,
            ShipCctvTarget.Armory
        };
    }
}
