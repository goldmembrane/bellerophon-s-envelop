using System;
using Bellerophon.Core.Player;
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
        Armory,
        SupplyRoom
    }

    public enum ShipControlRoomScreenMode
    {
        MainCctv,
        VerticalRoomList,
        HorizontalShipLayout
    }

    public enum ShipWeaponOperationMode
    {
        AutoTurret,
        ManualTurret
    }

    public readonly struct ControlRoomPurificationState
    {
        public const int DurationSeconds = 30;
        public const int TotalFireDamage = 500;

        private ControlRoomPurificationState(
            bool isActive,
            ShipRoomId targetRoom,
            float elapsedSeconds,
            int appliedFireDamage)
        {
            IsActive = isActive;
            TargetRoom = targetRoom;
            ElapsedSeconds = Mathf.Clamp(elapsedSeconds, 0f, DurationSeconds);
            AppliedFireDamage = Mathf.Clamp(appliedFireDamage, 0, TotalFireDamage);
        }

        public bool IsActive { get; }

        public ShipRoomId TargetRoom { get; }

        public float ElapsedSeconds { get; }

        public float RemainingSeconds => IsActive
            ? Mathf.Max(0f, DurationSeconds - ElapsedSeconds)
            : 0f;

        public int AppliedFireDamage { get; }

        public static ControlRoomPurificationState Inactive => new ControlRoomPurificationState(
            false,
            ShipRoomId.Cockpit,
            0f,
            0);

        public static ControlRoomPurificationState Start(ShipRoomId targetRoom)
        {
            return new ControlRoomPurificationState(true, targetRoom, 0f, 0);
        }

        public ControlRoomPurificationTickResult Tick(float deltaSeconds)
        {
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), "Delta seconds cannot be negative.");
            }

            if (!IsActive || deltaSeconds <= 0f)
            {
                return new ControlRoomPurificationTickResult(this, TargetRoom, 0, false);
            }

            var nextElapsed = Mathf.Min(DurationSeconds, ElapsedSeconds + deltaSeconds);
            var nextApplied = nextElapsed >= DurationSeconds
                ? TotalFireDamage
                : Mathf.RoundToInt(TotalFireDamage * (nextElapsed / DurationSeconds));
            var fireDamageThisTick = Mathf.Max(0, nextApplied - AppliedFireDamage);
            var completed = nextElapsed >= DurationSeconds;
            var next = new ControlRoomPurificationState(
                !completed,
                TargetRoom,
                nextElapsed,
                nextApplied);
            return new ControlRoomPurificationTickResult(
                next,
                TargetRoom,
                fireDamageThisTick,
                completed);
        }
    }

    public readonly struct ControlRoomPurificationTickResult
    {
        public ControlRoomPurificationTickResult(
            ControlRoomPurificationState state,
            ShipRoomId targetRoom,
            int fireDamageThisTick,
            bool completedThisTick)
        {
            State = state;
            TargetRoom = targetRoom;
            FireDamageThisTick = Mathf.Max(0, fireDamageThisTick);
            CompletedThisTick = completedThisTick;
        }

        public ControlRoomPurificationState State { get; }

        public ShipRoomId TargetRoom { get; }

        public int FireDamageThisTick { get; }

        public bool CompletedThisTick { get; }
    }

    public sealed class ShipDeviceInteractionState : MonoBehaviour
    {
        private ShipState shipState;
        private CargoState cargoState;
        private PlayerEquipmentState equipmentState;
        private ShipUpgradeState shipUpgradeState;
        private bool isInitialized;
        private ShipDevicePanelMode activePanelMode;
        private ShipCctvTarget currentCctvTarget;
        private ShipControlRoomScreenMode controlRoomScreenMode;
        private ControlRoomPurificationState controlRoomPurificationState;
        private bool manualFlightModeActive;
        private bool turretManualModeActive;
        private bool engineOverclockActive;
        private bool engineOverclockUsedThisRun;
        private int engineOverclockActivationCount;
        private bool hasTransportRun;
        private TransportRunState transportRunState;
        private TransportHazardState transportHazardState;
        private TransportHazardResult lastTransportHazardResult;
        private ExternalTargetState externalTargetState;
        private ManualTurretState manualTurretState;
        private SeedIntruderState seedIntruderState;
        private EquipmentUseResult lastEquipmentUseResult;
        private FirstPersonPlayerStatus playerStatus;
        private float seedIntruderCheckAccumulatorSeconds;
        private int seedIntruderCheckCount;
        private float asteroidHazardCheckAccumulatorSeconds;
        private int asteroidHazardCheckCount;
        private float cargoFreedomHazardCheckAccumulatorSeconds;
        private int cargoFreedomHazardCheckCount;
        private float spacePirateHazardCheckAccumulatorSeconds;
        private int spacePirateHazardCheckCount;
        private float alienLifeHazardCheckAccumulatorSeconds;
        private int alienLifeHazardCheckCount;
        private int lastPurificationPlayerDamage;
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

        public PlayerEquipmentState CurrentEquipmentState
        {
            get
            {
                EnsureInitialized();
                return equipmentState;
            }
        }

        public ShipUpgradeState CurrentShipUpgradeState
        {
            get
            {
                EnsureInitialized();
                return shipUpgradeState;
            }
        }

        public ShipDevicePanelMode ActivePanelMode => activePanelMode;

        public ShipCctvTarget CurrentCctvTarget => currentCctvTarget;

        public ShipControlRoomScreenMode CurrentControlRoomScreenMode => controlRoomScreenMode;

        public ControlRoomPurificationState CurrentControlRoomPurification
        {
            get
            {
                EnsureInitialized();
                return controlRoomPurificationState;
            }
        }

        public bool ManualFlightModeActive => manualFlightModeActive;

        public bool TurretManualModeActive => turretManualModeActive;

        public ShipWeaponOperationMode CurrentWeaponOperationMode => turretManualModeActive
            ? ShipWeaponOperationMode.ManualTurret
            : ShipWeaponOperationMode.AutoTurret;

        public ManualTurretState CurrentManualTurret
        {
            get
            {
                EnsureInitialized();
                return manualTurretState;
            }
        }

        public ExternalTargetState CurrentExternalTarget
        {
            get
            {
                EnsureInitialized();
                return externalTargetState;
            }
        }

        public bool EngineOverclockActive => engineOverclockActive;

        public bool EngineOverclockUsedThisRun => engineOverclockUsedThisRun;

        public int EngineOverclockActivationCount => engineOverclockActivationCount;

        public int HandSlotCount
        {
            get
            {
                EnsureInitialized();
                return equipmentState.UnlockedHandSlotCount;
            }
        }

        public int SupplySlotCount
        {
            get
            {
                EnsureInitialized();
                return ShipStateRules.CalculateSupplyStorageSlotCount(
                    shipState,
                    equipmentState.UnlockedSupplySlotCount);
            }
        }

        public EquipmentUseResult LastEquipmentUseResult => lastEquipmentUseResult;

        public FirstPersonPlayerStatus PlayerStatus => playerStatus;

        public string LastInteractionSummary => lastInteractionSummary;

        public bool HasActiveTransportRun => hasTransportRun;

        public bool HasActiveTransportHazard
        {
            get
            {
                EnsureInitialized();
                return transportHazardState.HasActiveHazard;
            }
        }

        public TransportHazardState CurrentTransportHazard
        {
            get
            {
                EnsureInitialized();
                return transportHazardState;
            }
        }

        public TransportHazardResult LastTransportHazardResult
        {
            get
            {
                EnsureInitialized();
                return lastTransportHazardResult;
            }
        }

        public TransportRunState CurrentTransportRun
        {
            get
            {
                EnsureInitialized();
                if (!hasTransportRun)
                {
                    throw new InvalidOperationException("No active transport run is registered.");
                }

                return transportRunState;
            }
        }

        public SeedIntruderState CurrentSeedIntruder
        {
            get
            {
                EnsureInitialized();
                return seedIntruderState;
            }
        }

        public bool HasActiveSeedIntruder
        {
            get
            {
                EnsureInitialized();
                return seedIntruderState.IsActive;
            }
        }

        public int SeedIntruderCheckCount => seedIntruderCheckCount;

        public int LastPurificationPlayerDamage => lastPurificationPlayerDamage;

        public ShipFlightMode CurrentFlightMode => hasTransportRun
            ? transportRunState.FlightMode
            : manualFlightModeActive ? ShipFlightMode.ManualFlight : ShipFlightMode.AutoPilot;

        public bool IsAutoPilotAvailable
        {
            get
            {
                EnsureInitialized();
                return hasTransportRun
                    ? transportRunState.IsAutoPilotAvailable
                    : ShipStateRules.CanUseAutoPilot(shipState);
            }
        }

        public float TransportProgressPercent => hasTransportRun ? transportRunState.ProgressPercent : 0f;

        public float TransportRemainingSeconds => hasTransportRun ? transportRunState.RemainingSeconds : 0f;

        public float ManualFlightOffsetX => hasTransportRun ? transportRunState.ManualOffsetX : 0f;

        public float ManualFlightOffsetY => hasTransportRun ? transportRunState.ManualOffsetY : 0f;

        private void Awake()
        {
            EnsureInitialized();
        }

        private void Update()
        {
            TickEquipmentState(Time.deltaTime);
            if (hasTransportRun)
            {
                TickTransportRun(Time.deltaTime);
            }
            else
            {
                TickControlRoomOperations(Time.deltaTime);
            }
        }

        public void EnsureInitialized()
        {
            if (isInitialized)
            {
                return;
            }

            shipState = ShipState.CreateDefault();
            cargoState = new CargoState(CargoGrade.Common, 50, 100, 1f, false);
            equipmentState = PlayerEquipmentState.Empty;
            shipUpgradeState = ShipUpgradeState.Empty;
            activePanelMode = ShipDevicePanelMode.None;
            currentCctvTarget = ShipCctvTarget.Cockpit;
            controlRoomScreenMode = ShipControlRoomScreenMode.MainCctv;
            controlRoomPurificationState = ControlRoomPurificationState.Inactive;
            hasTransportRun = false;
            transportHazardState = TransportHazardState.None;
            lastTransportHazardResult = TransportHazardResult.None;
            externalTargetState = ExternalTargetState.None;
            manualTurretState = ManualTurretState.Inactive;
            seedIntruderState = SeedIntruderState.None;
            lastEquipmentUseResult = default;
            ResolvePlayerStatus();
            seedIntruderCheckAccumulatorSeconds = 0f;
            seedIntruderCheckCount = 0;
            ResetTransportHazardOccurrenceChecks();
            lastPurificationPlayerDamage = 0;
            isInitialized = true;
        }

        public string ActivateDevice(ShipDeviceType deviceType)
        {
            EnsureInitialized();

            switch (deviceType)
            {
                case ShipDeviceType.CockpitHelm:
                    lastInteractionSummary = ActivateCockpitHelm();
                    break;
                case ShipDeviceType.EngineRoomPowerScreen:
                    activePanelMode = ShipDevicePanelMode.EngineStatus;
                    lastInteractionSummary = ActivateEngineScreen();
                    break;
                case ShipDeviceType.ControlRoomMainScreen:
                    activePanelMode = ShipDevicePanelMode.ControlRoom;
                    controlRoomScreenMode = ShipControlRoomScreenMode.MainCctv;
                    lastInteractionSummary = "Control room main CCTV screen opened.";
                    break;
                case ShipDeviceType.ArmoryTurretHandle:
                    lastInteractionSummary = ActivateArmoryTurretHandle();
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
            if (activePanelMode != ShipDevicePanelMode.ControlRoom ||
                controlRoomScreenMode != ShipControlRoomScreenMode.MainCctv ||
                direction == 0)
            {
                return;
            }

            var availableCount = Math.Min(
                ShipStateRules.CalculateControlRoomAvailableCctvCount(shipState),
                CctvOrder.Length);
            if (availableCount <= 0)
            {
                currentCctvTarget = CctvOrder[0];
                lastInteractionSummary = "CCTV is offline because control room damage is critical.";
                return;
            }

            var currentIndex = GetCctvIndex(currentCctvTarget);
            if (currentIndex >= availableCount)
            {
                currentIndex = 0;
            }

            var nextIndex = currentIndex + (direction > 0 ? 1 : -1);
            if (nextIndex < 0)
            {
                nextIndex = availableCount - 1;
            }
            else if (nextIndex >= availableCount)
            {
                nextIndex = 0;
            }

            currentCctvTarget = CctvOrder[nextIndex];
            lastInteractionSummary = "CCTV target changed to " + GetCctvDisplayName(currentCctvTarget) + ".";
        }

        public void SwitchControlRoomScreenByRightClick()
        {
            EnsureInitialized();
            if (activePanelMode != ShipDevicePanelMode.ControlRoom)
            {
                return;
            }

            switch (controlRoomScreenMode)
            {
                case ShipControlRoomScreenMode.MainCctv:
                    SetControlRoomScreenMode(ShipControlRoomScreenMode.VerticalRoomList);
                    break;
                case ShipControlRoomScreenMode.VerticalRoomList:
                    SetControlRoomScreenMode(ShipControlRoomScreenMode.HorizontalShipLayout);
                    break;
                case ShipControlRoomScreenMode.HorizontalShipLayout:
                    SetControlRoomScreenMode(ShipControlRoomScreenMode.MainCctv);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void SetControlRoomScreenMode(ShipControlRoomScreenMode screenMode)
        {
            EnsureInitialized();
            if (activePanelMode != ShipDevicePanelMode.ControlRoom)
            {
                activePanelMode = ShipDevicePanelMode.ControlRoom;
            }

            controlRoomScreenMode = screenMode;
            lastInteractionSummary = "Control room screen switched to " + FormatControlRoomScreenMode(screenMode) + ".";
        }

        public bool SelectControlRoomVerticalRoomByDisplayIndex(int displayIndex)
        {
            EnsureInitialized();
            if (displayIndex < 1 || displayIndex > ControlRoomVerticalRoomOrder.Length)
            {
                lastInteractionSummary = "Select a listed control room target from 1 to " +
                                         ControlRoomVerticalRoomOrder.Length + ".";
                return false;
            }

            return SelectControlRoomPurificationTarget(ControlRoomVerticalRoomOrder[displayIndex - 1]);
        }

        public bool SelectControlRoomPurificationTarget(ShipRoomId roomId)
        {
            EnsureInitialized();
            if (activePanelMode != ShipDevicePanelMode.ControlRoom ||
                controlRoomScreenMode != ShipControlRoomScreenMode.VerticalRoomList)
            {
                lastInteractionSummary = "Vertical control room screen is required for room purification.";
                return false;
            }

            if (!ShipStateRules.CanUseControlRoomRoomOperation(shipState))
            {
                OpenAllSealedRooms();
                controlRoomPurificationState = ControlRoomPurificationState.Inactive;
                lastInteractionSummary = "Control room damage prevents room closure and internal purification.";
                return false;
            }

            if (controlRoomPurificationState.IsActive)
            {
                lastInteractionSummary = "Internal purification is already running in " +
                                         FormatRoomName(controlRoomPurificationState.TargetRoom) + ".";
                return false;
            }

            var room = shipState.GetRoom(roomId);
            shipState = shipState.WithRoom(roomId, room.WithSealed(true));
            if (hasTransportRun)
            {
                transportRunState = transportRunState.WithShipState(shipState);
            }

            controlRoomPurificationState = ControlRoomPurificationState.Start(roomId);
            lastPurificationPlayerDamage = 0;
            lastInteractionSummary = "Internal purification started in " + FormatRoomName(roomId) + ".";
            return true;
        }

        public ControlRoomPurificationTickResult TickControlRoomOperations(float deltaSeconds)
        {
            return TickControlRoomOperations(deltaSeconds, ResolveCurrentPlayerRoom());
        }

        public ControlRoomPurificationTickResult TickControlRoomOperations(
            float deltaSeconds,
            ShipRoomId? playerRoom)
        {
            EnsureInitialized();
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), "Delta seconds cannot be negative.");
            }

            lastPurificationPlayerDamage = 0;
            if (!controlRoomPurificationState.IsActive || deltaSeconds <= 0f)
            {
                return new ControlRoomPurificationTickResult(
                    controlRoomPurificationState,
                    controlRoomPurificationState.TargetRoom,
                    0,
                    false);
            }

            var targetRoom = controlRoomPurificationState.TargetRoom;
            if (!ShipStateRules.CanUseControlRoomRoomOperation(shipState))
            {
                OpenAllSealedRooms();
                controlRoomPurificationState = ControlRoomPurificationState.Inactive;
                lastInteractionSummary = "Control room damage opened sealed rooms and stopped internal purification.";
                return new ControlRoomPurificationTickResult(
                    controlRoomPurificationState,
                    targetRoom,
                    0,
                    true);
            }

            var result = controlRoomPurificationState.Tick(deltaSeconds);
            controlRoomPurificationState = result.State;
            if (result.FireDamageThisTick > 0 &&
                playerRoom.HasValue &&
                playerRoom.Value == targetRoom)
            {
                var damageResult = ApplyPlayerDamage(new PlayerDamageProfile(
                    result.FireDamageThisTick,
                    CombatDamageSourceKind.Fire));
                lastPurificationPlayerDamage = damageResult.HealthDamage + damageResult.ShieldDamage;
            }

            if (result.CompletedThisTick)
            {
                var room = shipState.GetRoom(targetRoom);
                shipState = shipState.WithRoom(targetRoom, room.WithSealed(false));
                if (hasTransportRun)
                {
                    transportRunState = transportRunState.WithShipState(shipState);
                }

                lastInteractionSummary = "Internal purification completed in " + FormatRoomName(targetRoom) + ".";
            }
            else if (result.FireDamageThisTick > 0)
            {
                lastInteractionSummary = "Internal purification is burning " + FormatRoomName(targetRoom) + ".";
            }

            return result;
        }

        public bool ExitActiveDevicePanel()
        {
            EnsureInitialized();
            switch (activePanelMode)
            {
                case ShipDevicePanelMode.None:
                    return false;
                case ShipDevicePanelMode.ManualFlight:
                    return ExitManualFlightToAutoPilot();
                case ShipDevicePanelMode.TurretManual:
                    return ExitManualTurretMode();
                case ShipDevicePanelMode.ControlRoom:
                    activePanelMode = ShipDevicePanelMode.None;
                    controlRoomScreenMode = ShipControlRoomScreenMode.MainCctv;
                    lastInteractionSummary = "Control room screen closed.";
                    return true;
                case ShipDevicePanelMode.EngineStatus:
                case ShipDevicePanelMode.SupplyStorage:
                case ShipDevicePanelMode.CargoStatus:
                    activePanelMode = ShipDevicePanelMode.None;
                    lastInteractionSummary = "Ship device panel closed.";
                    return true;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public string GetSupplySlotLabel(int index)
        {
            if (index < 0 || index >= SupplySlotCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, null);
            }

            return FormatEquipmentSlot(equipmentState.GetSupplySlot(index));
        }

        public string GetHandSlotLabel(int index)
        {
            if (index < 0 || index >= HandSlotCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, null);
            }

            var label = FormatEquipmentSlot(equipmentState.GetHandSlot(index));
            return index == equipmentState.ActiveHandSlotIndex ? label + " <active>" : label;
        }

        public void SetShipStateForValidation(ShipState nextShipState)
        {
            SetShipState(nextShipState);
        }

        public void SetShipState(ShipState nextShipState)
        {
            EnsureInitialized();
            shipState = nextShipState ?? throw new ArgumentNullException(nameof(nextShipState));
            if (hasTransportRun)
            {
                transportRunState = transportRunState.WithShipState(shipState);
                manualFlightModeActive = transportRunState.FlightMode == ShipFlightMode.ManualFlight;
            }

            if (!ShipStateRules.CanUseEngineOverclock(shipState))
            {
                engineOverclockActive = false;
            }

            if (!ShipStateRules.CanUseManualTurret(shipState))
            {
                turretManualModeActive = false;
                manualTurretState = ManualTurretState.Inactive;
            }
            else
            {
                manualTurretState = manualTurretState.WithMagazineCapacity(
                    ShipStateRules.CalculateManualTurretMagazineCapacity(shipUpgradeState),
                    ShipStateRules.IsPlasmaCannonInstalled(shipUpgradeState));
            }

            if (!ShipStateRules.CanUseControlRoomRoomOperation(shipState))
            {
                OpenAllSealedRooms();
                controlRoomPurificationState = ControlRoomPurificationState.Inactive;
                lastPurificationPlayerDamage = 0;
            }

            var availableCctvCount = ShipStateRules.CalculateControlRoomAvailableCctvCount(shipState);
            if (availableCctvCount <= 0 || GetCctvIndex(currentCctvTarget) >= availableCctvCount)
            {
                currentCctvTarget = ShipCctvTarget.Cockpit;
            }
        }

        public void SetCargoState(CargoState nextCargoState)
        {
            EnsureInitialized();
            cargoState = nextCargoState;
        }

        public void SetCargoStateForValidation(CargoState nextCargoState)
        {
            SetCargoState(nextCargoState);
        }

        public void SetEquipmentState(PlayerEquipmentState nextEquipmentState)
        {
            EnsureInitialized();
            equipmentState = nextEquipmentState;
        }

        public void SetEquipmentStateForValidation(PlayerEquipmentState nextEquipmentState)
        {
            SetEquipmentState(nextEquipmentState);
        }

        public void SetShipUpgradeState(ShipUpgradeState nextShipUpgradeState)
        {
            EnsureInitialized();
            shipUpgradeState = nextShipUpgradeState;
            manualTurretState = manualTurretState.WithMagazineCapacity(
                ShipStateRules.CalculateManualTurretMagazineCapacity(shipUpgradeState),
                ShipStateRules.IsPlasmaCannonInstalled(shipUpgradeState));
        }

        public void SetShipUpgradeStateForValidation(ShipUpgradeState nextShipUpgradeState)
        {
            SetShipUpgradeState(nextShipUpgradeState);
        }

        public void SetPlayerStatusForValidation(FirstPersonPlayerStatus status)
        {
            playerStatus = status;
        }

        public void SelectEquipmentHandSlot(int handSlotIndex)
        {
            EnsureInitialized();
            equipmentState = equipmentState.WithActiveHandSlot(handSlotIndex);
            lastInteractionSummary = "Active hand slot changed to " + (handSlotIndex + 1) + ".";
        }

        public EquipmentUseResult UseActiveEquipment(bool alternateMode)
        {
            EnsureInitialized();
            ResolvePlayerStatus();
            lastEquipmentUseResult = EquipmentRules.UseActiveEquipment(
                equipmentState,
                alternateMode,
                seedIntruderState.IsActive,
                playerStatus == null ? null : playerStatus.ActiveStatusEffects);
            equipmentState = lastEquipmentUseResult.State;

            if (seedIntruderState.IsActive &&
                (lastEquipmentUseResult.AppliesIntruderDamage ||
                 lastEquipmentUseResult.StatusEffectToApply.HasEffect))
            {
                if (lastEquipmentUseResult.AppliesIntruderDamage)
                {
                    seedIntruderState = SeedIntruderRules.ApplyDamage(
                        seedIntruderState,
                        lastEquipmentUseResult.Damage);
                }

                if (lastEquipmentUseResult.StatusEffectToApply.HasEffect && seedIntruderState.IsActive)
                {
                    seedIntruderState = SeedIntruderRules.ApplyStatusEffect(
                        seedIntruderState,
                        lastEquipmentUseResult.StatusEffectToApply);
                }

                lastInteractionSummary = seedIntruderState.IsResolved
                    ? EquipmentRules.FormatItemName(lastEquipmentUseResult.ItemKind) + " neutralized " +
                      SeedIntruderRules.FormatSeedIntruderKind(seedIntruderState.Kind) + "."
                    : lastEquipmentUseResult.Summary;
                return lastEquipmentUseResult;
            }

            lastInteractionSummary = lastEquipmentUseResult.Summary;
            return lastEquipmentUseResult;
        }

        public EquipmentUseResult UseSupplyItem(int supplySlotIndex)
        {
            EnsureInitialized();
            lastEquipmentUseResult = EquipmentRules.UseSupplyItem(equipmentState, supplySlotIndex);
            equipmentState = lastEquipmentUseResult.State;
            ApplyPlayerUseEffects(lastEquipmentUseResult);
            lastInteractionSummary = lastEquipmentUseResult.Summary;
            return lastEquipmentUseResult;
        }

        public int CalculateIncomingDamageAfterProtection(int rawDamage)
        {
            EnsureInitialized();
            return EquipmentRules.CalculateDamageAfterProtection(rawDamage, equipmentState);
        }

        public PlayerDamageResult ApplyPlayerDamage(
            PlayerDamageProfile profile,
            int statusRollPercent = 0)
        {
            EnsureInitialized();
            ResolvePlayerStatus();
            if (playerStatus == null)
            {
                return default;
            }

            var result = playerStatus.ApplyIncomingDamage(profile, equipmentState, statusRollPercent);
            lastInteractionSummary = result.WasKilled
                ? "Player was killed by incoming damage."
                : "Player damage applied: shield -" + result.ShieldDamage + ", health -" + result.HealthDamage + ".";
            return result;
        }

        public EquipmentUseResult ReloadActiveEquipment()
        {
            EnsureInitialized();
            lastEquipmentUseResult = EquipmentRules.ReloadActiveEquipment(equipmentState);
            equipmentState = lastEquipmentUseResult.State;
            lastInteractionSummary = lastEquipmentUseResult.Summary;
            return lastEquipmentUseResult;
        }

        public EquipmentUseResult DropActiveEquipment()
        {
            EnsureInitialized();
            lastEquipmentUseResult = EquipmentRules.DropActiveHandItem(equipmentState);
            equipmentState = lastEquipmentUseResult.State;
            lastInteractionSummary = lastEquipmentUseResult.Summary;
            return lastEquipmentUseResult;
        }

        public void TickEquipmentState(float deltaSeconds)
        {
            EnsureInitialized();
            equipmentState = EquipmentRules.Tick(equipmentState, deltaSeconds);
        }

        private void ApplyPlayerUseEffects(EquipmentUseResult result)
        {
            if (result.HealthDelta <= 0 &&
                result.ShieldDelta <= 0 &&
                result.StatusEffectToClear == CombatStatusEffectKind.None &&
                !result.StatusEffectToApply.HasEffect &&
                !result.DelayedStatusEffectToApply.HasEffect)
            {
                return;
            }

            ResolvePlayerStatus();
            if (playerStatus != null)
            {
                playerStatus.ApplyRecovery(result.HealthDelta, result.ShieldDelta);
                if (result.StatusEffectToClear != CombatStatusEffectKind.None)
                {
                    playerStatus.ClearStatusEffect(result.StatusEffectToClear);
                }

                if (result.StatusEffectToApply.HasEffect)
                {
                    playerStatus.ApplyStatusEffect(result.StatusEffectToApply);
                }

                if (result.DelayedStatusEffectToApply.HasEffect)
                {
                    playerStatus.ScheduleStatusEffect(
                        result.DelayedStatusEffectToApply,
                        result.DelayedStatusEffectDelaySeconds);
                }
            }
        }

        public void StartTransportRun(int baseDurationSeconds)
        {
            EnsureInitialized();
            transportRunState = TransportRunState.Start(baseDurationSeconds, shipState);
            hasTransportRun = true;
            transportHazardState = TransportHazardState.None;
            lastTransportHazardResult = TransportHazardResult.None;
            externalTargetState = ExternalTargetState.None;
            manualTurretState = ManualTurretState.Inactive;
            seedIntruderState = SeedIntruderState.None;
            seedIntruderCheckAccumulatorSeconds = 0f;
            seedIntruderCheckCount = 0;
            ResetTransportHazardOccurrenceChecks();
            turretManualModeActive = false;
            engineOverclockActive = false;
            engineOverclockUsedThisRun = false;
            engineOverclockActivationCount = 0;
            manualFlightModeActive = transportRunState.FlightMode == ShipFlightMode.ManualFlight;
            if (manualFlightModeActive)
            {
                activePanelMode = ShipDevicePanelMode.ManualFlight;
            }
            else
            {
                activePanelMode = ShipDevicePanelMode.None;
            }

            lastInteractionSummary = manualFlightModeActive
                ? "Auto pilot unavailable; manual flight required."
                : "Auto pilot transport started.";
        }

        public void TickTransportRun(float deltaSeconds)
        {
            EnsureInitialized();
            if (!hasTransportRun || deltaSeconds <= 0f)
            {
                return;
            }

            var activeTransportSeconds = Mathf.Min(deltaSeconds, transportRunState.RemainingSeconds);
            transportRunState = transportRunState.Tick(deltaSeconds);
            TickManualTurretState(deltaSeconds);
            TickControlRoomOperations(deltaSeconds);
            TickTransportHazard(deltaSeconds);
            TickSeedIntruderDamage(activeTransportSeconds);
            manualFlightModeActive = transportRunState.FlightMode == ShipFlightMode.ManualFlight;
            ResolveSeedIntruderOnCompletedTransport();
        }

        public bool TickSeedIntruderOccurrenceForCurrentRun(float deltaSeconds, GameSessionState session)
        {
            EnsureInitialized();
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), "Delta seconds cannot be negative.");
            }

            if (!hasTransportRun ||
                transportRunState.IsComplete ||
                seedIntruderState.IsActive ||
                !SeedIntruderRules.CanCheckSeedIntruder(session))
            {
                return false;
            }

            seedIntruderCheckAccumulatorSeconds += deltaSeconds;
            while (seedIntruderCheckAccumulatorSeconds + 0.0001f >= SeedIntruderRules.OccurrenceCheckIntervalSeconds)
            {
                seedIntruderCheckAccumulatorSeconds -= SeedIntruderRules.OccurrenceCheckIntervalSeconds;
                seedIntruderCheckCount++;
                if (!SeedIntruderRules.ShouldStartSeedIntruder(session, seedIntruderCheckCount, shipState))
                {
                    continue;
                }

                StartSeedIntruder(SeedIntruderRules.CreateParvumIntrusion(session, seedIntruderCheckCount));
                return true;
            }

            return false;
        }

        public void StartSeedIntruderForValidation(SeedIntruderState intruder)
        {
            StartSeedIntruder(intruder);
        }

        public SeedIntruderState DamageActiveSeedIntruderForValidation(int damage)
        {
            EnsureInitialized();
            seedIntruderState = SeedIntruderRules.ApplyDamage(seedIntruderState, damage);
            lastInteractionSummary = seedIntruderState.IsResolved
                ? "Parvum intruder neutralized."
                : "Parvum intruder damaged.";
            return seedIntruderState;
        }

        public SeedIntruderState NeutralizeActiveSeedIntruderForValidation()
        {
            EnsureInitialized();
            if (!seedIntruderState.IsActive)
            {
                return seedIntruderState;
            }

            seedIntruderState = SeedIntruderRules.ApplyDamage(
                seedIntruderState,
                seedIntruderState.Intruder.CurrentHealth);
            lastInteractionSummary = SeedIntruderRules.FormatSeedIntruderKind(seedIntruderState.Kind) +
                                     " intruder neutralized.";
            return seedIntruderState;
        }

        public bool TickTransportHazardOccurrenceForCurrentRun(float deltaSeconds, GameSessionState session)
        {
            EnsureInitialized();
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), "Delta seconds cannot be negative.");
            }

            if (!hasTransportRun ||
                transportRunState.IsComplete ||
                transportHazardState.HasActiveHazard ||
                !TransportHazardRules.CanCheckTransportHazard(session))
            {
                return false;
            }

            if (TickTransportHazardOccurrence(
                    ref asteroidHazardCheckAccumulatorSeconds,
                    ref asteroidHazardCheckCount,
                    TransportHazardType.AsteroidFieldSmall,
                    deltaSeconds,
                    session))
            {
                return true;
            }

            if (TickTransportHazardOccurrence(
                    ref cargoFreedomHazardCheckAccumulatorSeconds,
                    ref cargoFreedomHazardCheckCount,
                    TransportHazardType.CargoFreedomLeagueRegion,
                    deltaSeconds,
                    session))
            {
                return true;
            }

            if (TickTransportHazardOccurrence(
                    ref spacePirateHazardCheckAccumulatorSeconds,
                    ref spacePirateHazardCheckCount,
                    TransportHazardType.SpacePirateRegion,
                    deltaSeconds,
                    session))
            {
                return true;
            }

            return TickTransportHazardOccurrence(
                ref alienLifeHazardCheckAccumulatorSeconds,
                ref alienLifeHazardCheckCount,
                TransportHazardType.AlienLifeRegion,
                deltaSeconds,
                session);
        }

        public bool TryStartAsteroidFieldForCurrentRun(GameSessionState session)
        {
            EnsureInitialized();
            if (!hasTransportRun ||
                transportHazardState.HasActiveHazard ||
                !TransportHazardRules.ShouldStartAsteroidField(session))
            {
                return false;
            }

            StartTransportHazard(TransportHazardRules.CreateAsteroidField(session));
            return true;
        }

        public bool TryStartTransportHazardForCurrentRun(
            GameSessionState session,
            TransportHazardType hazardType,
            int checkIndex)
        {
            EnsureInitialized();
            if (!hasTransportRun ||
                transportHazardState.HasActiveHazard ||
                !TransportHazardRules.ShouldStartHazard(session, hazardType, checkIndex))
            {
                return false;
            }

            StartTransportHazard(TransportHazardRules.CreateHazard(session, hazardType, checkIndex));
            return true;
        }

        public void StartTransportHazardForValidation(TransportHazardState hazard)
        {
            StartTransportHazard(hazard);
        }

        public void StartTransportHazard(TransportHazardState hazard)
        {
            EnsureInitialized();
            if (!hasTransportRun)
            {
                throw new InvalidOperationException("A transport run must be active before starting a transport hazard.");
            }

            transportHazardState = hazard;
            lastTransportHazardResult = TransportHazardResult.None;
            externalTargetState = TransportHazardRules.CreateExternalTarget(hazard);
            if (hazard.HazardType != TransportHazardType.None)
            {
                lastInteractionSummary = TransportHazardRules.FormatHazardType(hazard.HazardType) + " detected.";
            }
        }

        public void ApplyManualTurretAimInput(float horizontalDelta, float verticalDelta)
        {
            EnsureInitialized();
            if (!ShipStateRules.CanUseManualTurret(shipState))
            {
                manualTurretState = ManualTurretState.Inactive;
                turretManualModeActive = false;
                lastInteractionSummary = "Manual turret is offline because armory damage is severe.";
                return;
            }

            var aimMultiplier = ShipStateRules.CalculateManualTurretAimMultiplier(shipState);
            manualTurretState = manualTurretState.ApplyAimInput(
                horizontalDelta * aimMultiplier,
                verticalDelta * aimMultiplier);
        }

        public void SetManualTurretAimForValidation(float aimX, float aimY)
        {
            EnsureInitialized();
            EnsureManualTurretStarted();
            manualTurretState = manualTurretState.SetAim(aimX, aimY);
        }

        public void BeginManualTurretReload()
        {
            EnsureInitialized();
            if (!ShipStateRules.CanUseManualTurret(shipState))
            {
                manualTurretState = ManualTurretState.Inactive;
                turretManualModeActive = false;
                lastInteractionSummary = "Manual turret is offline because armory damage is severe.";
                return;
            }

            EnsureManualTurretStarted();
            manualTurretState = manualTurretState.BeginReload();
            if (manualTurretState.IsReloading)
            {
                lastInteractionSummary = "Manual turret reload started.";
            }
        }

        public ManualTurretFireResult FireManualTurret()
        {
            EnsureInitialized();
            if (!ShipStateRules.CanUseManualTurret(shipState))
            {
                manualTurretState = ManualTurretState.Inactive;
                turretManualModeActive = false;
                var inactiveResult = manualTurretState.FireAt(externalTargetState);
                lastInteractionSummary = FormatManualTurretFireResult(inactiveResult);
                return inactiveResult;
            }

            EnsureManualTurretStarted();
            var fireResult = manualTurretState.FireAt(externalTargetState);
            manualTurretState = fireResult.Turret;
            externalTargetState = fireResult.Target;
            lastInteractionSummary = FormatManualTurretFireResult(fireResult);
            if (fireResult.DestroyedTarget)
            {
                ResolveActiveHazardFromTurret();
            }

            return fireResult;
        }

        public ManualTurretPlasmaResult FireManualTurretPlasma()
        {
            EnsureInitialized();
            if (!ShipStateRules.CanUseManualTurret(shipState))
            {
                manualTurretState = ManualTurretState.Inactive;
                turretManualModeActive = false;
                var inactiveResult = new ManualTurretPlasmaResult(
                    ManualTurretPlasmaOutcome.Inactive,
                    manualTurretState,
                    externalTargetState,
                    0);
                lastInteractionSummary = FormatManualTurretPlasmaResult(inactiveResult);
                return inactiveResult;
            }

            if (!ShipStateRules.IsPlasmaCannonAvailable(shipState, shipUpgradeState))
            {
                EnsureManualTurretStarted();
                var unavailableResult = new ManualTurretPlasmaResult(
                    ManualTurretPlasmaOutcome.Unavailable,
                    manualTurretState,
                    externalTargetState,
                    0);
                lastInteractionSummary = FormatManualTurretPlasmaResult(unavailableResult);
                return unavailableResult;
            }

            EnsureManualTurretStarted();
            var result = manualTurretState.FirePlasmaCannon(externalTargetState);
            manualTurretState = result.Turret;
            externalTargetState = result.Target;
            lastInteractionSummary = FormatManualTurretPlasmaResult(result);
            return result;
        }

        public bool ExitManualTurretMode()
        {
            EnsureInitialized();
            turretManualModeActive = false;
            manualTurretState = manualTurretState.Stop();
            activePanelMode = hasTransportRun && transportRunState.FlightMode == ShipFlightMode.ManualFlight
                ? ShipDevicePanelMode.ManualFlight
                : ShipDevicePanelMode.None;
            lastInteractionSummary = "Manual turret mode frame exited.";
            return true;
        }

        public bool UseManualFlightBooster()
        {
            EnsureInitialized();
            if (!hasTransportRun || !transportHazardState.HasActiveHazard)
            {
                lastInteractionSummary = "Manual flight booster has no active hazard target.";
                return false;
            }

            if (transportRunState.FlightMode != ShipFlightMode.ManualFlight)
            {
                lastInteractionSummary = "Manual flight booster requires manual flight mode.";
                return false;
            }

            if (!ShipStateRules.CanUseBooster(shipState))
            {
                lastInteractionSummary = "Engine room damage prevents booster use.";
                return false;
            }

            var reductionSeconds = TransportHazardRules.GetManualFlightBoosterReductionSeconds(
                transportHazardState.HazardType);
            if (reductionSeconds <= 0)
            {
                lastInteractionSummary = "Manual flight booster has no effect on this hazard.";
                return false;
            }

            transportHazardState = TransportHazardRules.ApplyManualFlightBooster(transportHazardState);
            lastInteractionSummary = "Manual flight booster reduced hazard duration by " +
                                     reductionSeconds + " seconds.";
            if (transportHazardState.IsComplete)
            {
                ResolveCompletedTransportHazard();
            }

            return true;
        }

        public void ApplyManualFlightInput(float horizontal, float vertical, float deltaSeconds)
        {
            EnsureInitialized();
            if (!hasTransportRun)
            {
                return;
            }

            var beforeX = transportRunState.ManualOffsetX;
            var beforeY = transportRunState.ManualOffsetY;
            transportRunState = transportRunState.ApplyManualFlightInput(horizontal, vertical, deltaSeconds);
            if (Mathf.Abs(beforeX - transportRunState.ManualOffsetX) > 0.0001f ||
                Mathf.Abs(beforeY - transportRunState.ManualOffsetY) > 0.0001f)
            {
                lastInteractionSummary = "Manual flight evasive input applied.";
            }
        }

        public bool ExitManualFlightToAutoPilot()
        {
            EnsureInitialized();
            if (!hasTransportRun)
            {
                manualFlightModeActive = false;
                activePanelMode = ShipDevicePanelMode.None;
                lastInteractionSummary = "Manual flight mode frame exited.";
                return true;
            }

            transportRunState = transportRunState.ReturnToAutoPilot();
            manualFlightModeActive = transportRunState.FlightMode == ShipFlightMode.ManualFlight;
            if (manualFlightModeActive)
            {
                activePanelMode = ShipDevicePanelMode.ManualFlight;
                lastInteractionSummary = "Auto pilot unavailable; manual flight remains active.";
                return false;
            }

            if (activePanelMode == ShipDevicePanelMode.ManualFlight)
            {
                activePanelMode = ShipDevicePanelMode.None;
            }

            lastInteractionSummary = "Auto pilot mode restored.";
            return true;
        }

        private void TickTransportHazard(float deltaSeconds)
        {
            if (!transportHazardState.HasActiveHazard)
            {
                return;
            }

            transportHazardState = transportHazardState.Tick(deltaSeconds, IsManualHazardAvoidanceActive());
            if (!transportHazardState.IsComplete)
            {
                return;
            }

            ResolveCompletedTransportHazard();
        }

        private void TickSeedIntruderDamage(float deltaSeconds)
        {
            if (!seedIntruderState.IsActive || deltaSeconds <= 0f)
            {
                return;
            }

            var profile = SeedIntruderRules.GetProfile(seedIntruderState.Kind);
            var roomDamage = ShipStateRules.CalculateInternalIntruderRoomDamage(
                profile.ShipFacilityDamage,
                shipState);
            var result = SeedIntruderRules.TickSeedIntruder(
                seedIntruderState,
                shipState,
                cargoState,
                deltaSeconds,
                roomDamage,
                CargoMaterial.Unspecified);
            seedIntruderState = result.State;
            shipState = result.Ship;
            cargoState = result.Cargo;
            transportRunState = transportRunState.WithShipState(shipState);
            manualFlightModeActive = transportRunState.FlightMode == ShipFlightMode.ManualFlight;
            if (result.AttackCount > 0)
            {
                lastInteractionSummary = SeedIntruderRules.FormatSeedIntruderKind(seedIntruderState.Kind) +
                                         " is pressuring " +
                                         FormatRoomName(seedIntruderState.Intruder.CurrentRoom) + ".";
            }
        }

        private void ResolveSeedIntruderOnCompletedTransport()
        {
            if (!seedIntruderState.IsActive || !transportRunState.IsComplete)
            {
                return;
            }

            seedIntruderState = SeedIntruderRules.ResolveActiveIntruder(
                seedIntruderState,
                IntruderResolution.ObjectiveApplied);
            lastInteractionSummary = SeedIntruderRules.FormatSeedIntruderKind(seedIntruderState.Kind) +
                                     " intrusion ended at arrival; damage remains for settlement.";
        }

        private bool IsManualHazardAvoidanceActive()
        {
            return transportRunState.FlightMode == ShipFlightMode.ManualFlight &&
                   (Mathf.Abs(transportRunState.ManualOffsetX) > 0.05f ||
                    Mathf.Abs(transportRunState.ManualOffsetY) > 0.05f);
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
                case ShipCctvTarget.SupplyRoom:
                    return "Supply Room";
                default:
                    throw new ArgumentOutOfRangeException(nameof(target), target, null);
            }
        }

        public static ShipRoomId GetRoomForCctvTarget(ShipCctvTarget target)
        {
            switch (target)
            {
                case ShipCctvTarget.Cockpit:
                    return ShipRoomId.Cockpit;
                case ShipCctvTarget.CargoHold:
                    return ShipRoomId.CargoHold;
                case ShipCctvTarget.EngineRoom:
                    return ShipRoomId.EngineRoom;
                case ShipCctvTarget.Armory:
                    return ShipRoomId.Armory;
                case ShipCctvTarget.SupplyRoom:
                    return ShipRoomId.SupplyRoom;
                default:
                    throw new ArgumentOutOfRangeException(nameof(target), target, null);
            }
        }

        public static ShipRoomId[] GetControlRoomVerticalRoomOrder()
        {
            return (ShipRoomId[])ControlRoomVerticalRoomOrder.Clone();
        }

        private string ActivateEngineScreen()
        {
            if (!ShipStateRules.CanUseEngineOverclock(shipState))
            {
                engineOverclockActive = false;
                return "Engine power is too damaged for overclock.";
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

        private string ActivateArmoryTurretHandle()
        {
            if (hasTransportRun && transportRunState.FlightMode == ShipFlightMode.ManualFlight)
            {
                turretManualModeActive = false;
                manualTurretState = manualTurretState.Stop();
                activePanelMode = ShipDevicePanelMode.None;
                return "Manual flight forces weapon room auto turret mode.";
            }

            activePanelMode = ShipDevicePanelMode.TurretManual;
            if (!ShipStateRules.CanUseManualTurret(shipState))
            {
                turretManualModeActive = false;
                manualTurretState = ManualTurretState.Inactive;
                return "Manual turret is offline because armory damage is severe.";
            }

            turretManualModeActive = true;
            EnsureManualTurretStarted();
            return externalTargetState.IsActive
                ? "Manual turret mode entered; external target acquired."
                : "Manual turret mode entered; no external target.";
        }

        private string ActivateCockpitHelm()
        {
            activePanelMode = ShipDevicePanelMode.ManualFlight;
            turretManualModeActive = false;
            manualTurretState = manualTurretState.Stop();

            if (!hasTransportRun)
            {
                manualFlightModeActive = true;
                return "Manual flight mode frame entered.";
            }

            if (transportRunState.FlightMode == ShipFlightMode.ManualFlight)
            {
                ExitManualFlightToAutoPilot();
                return lastInteractionSummary;
            }

            transportRunState = transportRunState.EnterManualFlight();
            manualFlightModeActive = true;
            return "Manual flight mode entered.";
        }

        private void EnsureManualTurretStarted()
        {
            if (!manualTurretState.IsActive)
            {
                manualTurretState = ManualTurretState.Start(
                    true,
                    ShipStateRules.CalculateManualTurretMagazineCapacity(shipUpgradeState),
                    ShipStateRules.IsPlasmaCannonInstalled(shipUpgradeState));
            }
        }

        private void TickManualTurretState(float deltaSeconds)
        {
            var result = manualTurretState.TickWithPlasma(deltaSeconds, externalTargetState);
            manualTurretState = result.Turret;
            externalTargetState = result.Target;
            if (result.DamageApplied > 0)
            {
                lastInteractionSummary = "Plasma cannon burned external target for " +
                                         result.DamageApplied + " damage.";
            }

            if (result.DestroyedTarget)
            {
                ResolveActiveHazardFromTurret();
            }
        }

        private void ResolveCompletedTransportHazard()
        {
            lastTransportHazardResult = TransportHazardRules.ResolveTransportHazard(transportHazardState);
            shipState = TransportHazardRules.ApplyHazardResult(shipState, lastTransportHazardResult);
            transportRunState = transportRunState.WithShipState(shipState);
            transportHazardState = TransportHazardState.None;
            externalTargetState = ExternalTargetState.None;
            manualFlightModeActive = transportRunState.FlightMode == ShipFlightMode.ManualFlight;
            lastInteractionSummary = FormatHazardResult(lastTransportHazardResult);
        }

        private void ResolveActiveHazardFromTurret()
        {
            if (!transportHazardState.HasActiveHazard)
            {
                return;
            }

            lastTransportHazardResult = TransportHazardRules.ResolveTransportHazard(transportHazardState, true);
            shipState = TransportHazardRules.ApplyHazardResult(shipState, lastTransportHazardResult);
            transportRunState = transportRunState.WithShipState(shipState);
            transportHazardState = TransportHazardState.None;
            externalTargetState = ExternalTargetState.None;
            lastInteractionSummary = "External target destroyed; " +
                                     TransportHazardRules.FormatHazardType(lastTransportHazardResult.HazardType) +
                                     " neutralized.";
        }

        private bool TickTransportHazardOccurrence(
            ref float accumulatorSeconds,
            ref int checkCount,
            TransportHazardType hazardType,
            float deltaSeconds,
            GameSessionState session)
        {
            var intervalSeconds = TransportHazardRules.GetOccurrenceCheckIntervalSeconds(hazardType);
            if (intervalSeconds <= 0 || deltaSeconds <= 0f)
            {
                return false;
            }

            accumulatorSeconds += deltaSeconds;
            while (accumulatorSeconds + 0.0001f >= intervalSeconds)
            {
                accumulatorSeconds -= intervalSeconds;
                checkCount++;
                if (!TransportHazardRules.ShouldStartHazard(session, hazardType, checkCount))
                {
                    continue;
                }

                StartTransportHazard(TransportHazardRules.CreateHazard(session, hazardType, checkCount));
                return true;
            }

            return false;
        }

        private void ResetTransportHazardOccurrenceChecks()
        {
            asteroidHazardCheckAccumulatorSeconds = 0f;
            asteroidHazardCheckCount = 0;
            cargoFreedomHazardCheckAccumulatorSeconds = 0f;
            cargoFreedomHazardCheckCount = 0;
            spacePirateHazardCheckAccumulatorSeconds = 0f;
            spacePirateHazardCheckCount = 0;
            alienLifeHazardCheckAccumulatorSeconds = 0f;
            alienLifeHazardCheckCount = 0;
        }

        private void ResolvePlayerStatus()
        {
            if (playerStatus == null)
            {
                playerStatus = UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayerStatus>();
            }
        }

        private ShipRoomId? ResolveCurrentPlayerRoom()
        {
            ResolvePlayerStatus();
            if (playerStatus == null)
            {
                return null;
            }

            return ShipInteriorMapRules.FindCurrentRoom(playerStatus.transform.position);
        }

        private void StartSeedIntruder(SeedIntruderState intruder)
        {
            EnsureInitialized();
            if (!hasTransportRun)
            {
                throw new InvalidOperationException("A transport run must be active before starting a seed intruder.");
            }

            if (!intruder.IsActive)
            {
                throw new ArgumentException("Seed intruder must be active.", nameof(intruder));
            }

            seedIntruderState = intruder;
            lastInteractionSummary = "Parvum seed intruder formed inside " + FormatRoomName(intruder.Intruder.CurrentRoom) + ".";
        }

        private static string FormatHazardResult(TransportHazardResult result)
        {
            if (result.HazardType == TransportHazardType.None)
            {
                return "Transport hazard resolved.";
            }

            var hazardName = TransportHazardRules.FormatHazardType(result.HazardType);
            switch (result.Resolution)
            {
                case TransportHazardResolution.Neutralized:
                    return hazardName + " neutralized by turret.";
                case TransportHazardResolution.Avoided:
                    return hazardName + " passed without effect.";
                case TransportHazardResolution.GlancingHit:
                    return hazardName + " partially affected the ship.";
                case TransportHazardResolution.DirectHit:
                    if (result.HazardType == TransportHazardType.SpacePirateRegion)
                    {
                        return hazardName + " caused " + result.BoardingEventCount +
                               " boarding event(s) and " + result.BombardmentHitCount + " bombardment hit(s).";
                    }

                    if (result.BoardingEventCount > 0)
                    {
                        return hazardName + " caused " + result.BoardingEventCount + " boarding event(s).";
                    }

                    return hazardName + " damaged the ship.";
                default:
                    return hazardName + " resolved.";
            }
        }

        private static string FormatManualTurretFireResult(ManualTurretFireResult result)
        {
            switch (result.Outcome)
            {
                case ManualTurretFireOutcome.Hit:
                    return "Manual turret hit external target.";
                case ManualTurretFireOutcome.Destroyed:
                    return "Manual turret destroyed external target.";
                case ManualTurretFireOutcome.Miss:
                    return "Manual turret fired and missed.";
                case ManualTurretFireOutcome.EmptyMagazine:
                    return "Manual turret magazine empty.";
                case ManualTurretFireOutcome.Reloading:
                    return "Manual turret is reloading.";
                case ManualTurretFireOutcome.Inactive:
                    return "Manual turret is inactive.";
                default:
                    return "Manual turret fired.";
            }
        }

        private static string FormatManualTurretPlasmaResult(ManualTurretPlasmaResult result)
        {
            switch (result.Outcome)
            {
                case ManualTurretPlasmaOutcome.Activated:
                    return "Plasma cannon fired.";
                case ManualTurretPlasmaOutcome.Unavailable:
                    return "Plasma cannon is unavailable.";
                case ManualTurretPlasmaOutcome.Cooldown:
                    return "Plasma cannon is cooling down.";
                case ManualTurretPlasmaOutcome.AlreadyActive:
                    return "Plasma cannon is already firing.";
                case ManualTurretPlasmaOutcome.Inactive:
                    return "Manual turret is inactive.";
                default:
                    return "Plasma cannon input received.";
            }
        }

        private static string FormatControlRoomScreenMode(ShipControlRoomScreenMode screenMode)
        {
            switch (screenMode)
            {
                case ShipControlRoomScreenMode.MainCctv:
                    return "main CCTV";
                case ShipControlRoomScreenMode.VerticalRoomList:
                    return "vertical room list";
                case ShipControlRoomScreenMode.HorizontalShipLayout:
                    return "horizontal ship layout";
                default:
                    throw new ArgumentOutOfRangeException(nameof(screenMode), screenMode, null);
            }
        }

        private void OpenAllSealedRooms()
        {
            for (var i = 0; i < AllRoomOrder.Length; i++)
            {
                var roomId = AllRoomOrder[i];
                var room = shipState.GetRoom(roomId);
                if (room.IsSealed)
                {
                    shipState = shipState.WithRoom(roomId, room.WithSealed(false));
                }
            }

            if (hasTransportRun)
            {
                transportRunState = transportRunState.WithShipState(shipState);
            }
        }

        private static string FormatRoomName(ShipRoomId roomId)
        {
            switch (roomId)
            {
                case ShipRoomId.Cockpit:
                    return "Cockpit";
                case ShipRoomId.CargoHold:
                    return "Cargo Hold";
                case ShipRoomId.EngineRoom:
                    return "Engine Room";
                case ShipRoomId.ControlRoom:
                    return "Control Room";
                case ShipRoomId.Armory:
                    return "Armory";
                case ShipRoomId.SupplyRoom:
                    return "Supply Room";
                default:
                    throw new ArgumentOutOfRangeException(nameof(roomId), roomId, null);
            }
        }

        private static string FormatEquipmentSlot(EquipmentSlotState slot)
        {
            if (slot.IsEmpty)
            {
                return "Empty";
            }

            var label = EquipmentRules.FormatItemName(slot.ItemKind);
            if (slot.Count > 1)
            {
                label += " x" + slot.Count;
            }

            return label + " " + slot.DurabilityPercent + "%";
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
            ShipCctvTarget.Armory,
            ShipCctvTarget.SupplyRoom
        };

        private static readonly ShipRoomId[] ControlRoomVerticalRoomOrder =
        {
            ShipRoomId.Cockpit,
            ShipRoomId.CargoHold,
            ShipRoomId.SupplyRoom,
            ShipRoomId.EngineRoom,
            ShipRoomId.ControlRoom,
            ShipRoomId.Armory
        };

        private static readonly ShipRoomId[] AllRoomOrder =
        {
            ShipRoomId.Cockpit,
            ShipRoomId.CargoHold,
            ShipRoomId.Armory,
            ShipRoomId.SupplyRoom,
            ShipRoomId.EngineRoom,
            ShipRoomId.ControlRoom
        };
    }
}
