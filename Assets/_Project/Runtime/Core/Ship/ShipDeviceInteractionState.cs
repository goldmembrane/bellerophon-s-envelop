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
        Armory
    }

    public sealed class ShipDeviceInteractionState : MonoBehaviour
    {
        private ShipState shipState;
        private CargoState cargoState;
        private PlayerEquipmentState equipmentState;
        private bool isInitialized;
        private ShipDevicePanelMode activePanelMode;
        private ShipCctvTarget currentCctvTarget;
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

        public ShipDevicePanelMode ActivePanelMode => activePanelMode;

        public ShipCctvTarget CurrentCctvTarget => currentCctvTarget;

        public bool ManualFlightModeActive => manualFlightModeActive;

        public bool TurretManualModeActive => turretManualModeActive;

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
            TickTransportRun(Time.deltaTime);
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
            activePanelMode = ShipDevicePanelMode.None;
            currentCctvTarget = ShipCctvTarget.Cockpit;
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
                    lastInteractionSummary = "Control room screen opened.";
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
            if (activePanelMode != ShipDevicePanelMode.ControlRoom || direction == 0)
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
            lastEquipmentUseResult = EquipmentRules.UseActiveEquipment(
                equipmentState,
                alternateMode,
                seedIntruderState.IsActive);
            equipmentState = lastEquipmentUseResult.State;

            if (lastEquipmentUseResult.AppliesIntruderDamage && seedIntruderState.IsActive)
            {
                seedIntruderState = SeedIntruderRules.ApplyDamage(
                    seedIntruderState,
                    lastEquipmentUseResult.Damage);
                lastInteractionSummary = seedIntruderState.IsResolved
                    ? EquipmentRules.FormatItemName(lastEquipmentUseResult.ItemKind) + " neutralized Parvum."
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
            ApplyPlayerRecovery(lastEquipmentUseResult);
            lastInteractionSummary = lastEquipmentUseResult.Summary;
            return lastEquipmentUseResult;
        }

        public int CalculateIncomingDamageAfterProtection(int rawDamage)
        {
            EnsureInitialized();
            return EquipmentRules.CalculateDamageAfterProtection(rawDamage, equipmentState);
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

        private void ApplyPlayerRecovery(EquipmentUseResult result)
        {
            if (result.HealthDelta <= 0 && result.ShieldDelta <= 0)
            {
                return;
            }

            ResolvePlayerStatus();
            if (playerStatus != null)
            {
                playerStatus.ApplyRecovery(result.HealthDelta, result.ShieldDelta);
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
            manualTurretState = manualTurretState.Tick(deltaSeconds);
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
            lastInteractionSummary = "Parvum intruder neutralized.";
            return seedIntruderState;
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
            if (hazard.HazardType == TransportHazardType.AsteroidField)
            {
                lastInteractionSummary = "Asteroid field detected.";
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

            lastTransportHazardResult = TransportHazardRules.ResolveAsteroidField(transportHazardState);
            shipState = TransportHazardRules.ApplyHazardResult(shipState, lastTransportHazardResult);
            transportRunState = transportRunState.WithShipState(shipState);
            transportHazardState = TransportHazardState.None;
            externalTargetState = ExternalTargetState.None;
            manualFlightModeActive = transportRunState.FlightMode == ShipFlightMode.ManualFlight;
            lastInteractionSummary = FormatHazardResult(lastTransportHazardResult);
        }

        private void TickSeedIntruderDamage(float deltaSeconds)
        {
            if (!seedIntruderState.IsActive || deltaSeconds <= 0f)
            {
                return;
            }

            var roomDamage = ShipStateRules.CalculateInternalIntruderRoomDamage(
                SeedIntruderRules.ParvumShipFacilityDamage,
                shipState);
            var result = SeedIntruderRules.TickParvum(
                seedIntruderState,
                shipState,
                cargoState,
                deltaSeconds,
                roomDamage);
            seedIntruderState = result.State;
            shipState = result.Ship;
            cargoState = result.Cargo;
            transportRunState = transportRunState.WithShipState(shipState);
            manualFlightModeActive = transportRunState.FlightMode == ShipFlightMode.ManualFlight;
            if (result.AttackCount > 0)
            {
                lastInteractionSummary = "Parvum is chewing through " + FormatRoomName(seedIntruderState.TargetRoom) + ".";
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
            lastInteractionSummary = "Parvum intrusion ended at arrival; damage remains for settlement.";
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
                default:
                    throw new ArgumentOutOfRangeException(nameof(target), target, null);
            }
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
                manualTurretState = ManualTurretState.Start(true);
            }
        }

        private void ResolveActiveHazardFromTurret()
        {
            if (!transportHazardState.HasActiveHazard)
            {
                return;
            }

            lastTransportHazardResult = TransportHazardRules.ResolveAsteroidField(transportHazardState, true);
            shipState = TransportHazardRules.ApplyHazardResult(shipState, lastTransportHazardResult);
            transportRunState = transportRunState.WithShipState(shipState);
            transportHazardState = TransportHazardState.None;
            externalTargetState = ExternalTargetState.None;
            lastInteractionSummary = "External target destroyed; asteroid hazard neutralized.";
        }

        private void ResolvePlayerStatus()
        {
            if (playerStatus == null)
            {
                playerStatus = UnityEngine.Object.FindFirstObjectByType<FirstPersonPlayerStatus>();
            }
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
            if (result.HazardType != TransportHazardType.AsteroidField)
            {
                return "Transport hazard resolved.";
            }

            switch (result.Resolution)
            {
                case TransportHazardResolution.Neutralized:
                    return "Asteroid field neutralized by turret.";
                case TransportHazardResolution.Avoided:
                    return "Asteroid field avoided.";
                case TransportHazardResolution.GlancingHit:
                    return "Asteroid field grazed the ship.";
                case TransportHazardResolution.DirectHit:
                    return "Asteroid field damaged the ship.";
                default:
                    return "Asteroid field resolved.";
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
            ShipCctvTarget.Armory
        };
    }
}
