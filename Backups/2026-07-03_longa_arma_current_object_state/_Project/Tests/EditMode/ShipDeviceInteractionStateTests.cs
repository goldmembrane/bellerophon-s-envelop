using Bellerophon.Core.Player;
using Bellerophon.Core.Ship;
using Bellerophon.Core.Session;
using NUnit.Framework;
using UnityEngine;

namespace Bellerophon.Tests.EditMode
{
    public sealed class ShipDeviceInteractionStateTests
    {
        [Test]
        public void EngineScreen_ActivatesOverclockOnlyOncePerRun()
        {
            var stateObject = new GameObject("Ship Device State Test");
            try
            {
                var state = stateObject.AddComponent<ShipDeviceInteractionState>();

                state.ActivateDevice(ShipDeviceType.EngineRoomPowerScreen);
                state.ActivateDevice(ShipDeviceType.EngineRoomPowerScreen);

                Assert.That(state.ActivePanelMode, Is.EqualTo(ShipDevicePanelMode.EngineStatus));
                Assert.That(state.EngineOverclockUsedThisRun, Is.True);
                Assert.That(state.EngineOverclockActivationCount, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(stateObject);
            }
        }

        [Test]
        public void ControlRoomCctv_CyclesInOriginalOrderWithDirections()
        {
            var stateObject = new GameObject("Ship Device State Test");
            try
            {
                var state = stateObject.AddComponent<ShipDeviceInteractionState>();

                state.ActivateDevice(ShipDeviceType.ControlRoomMainScreen);
                state.CycleCctv(1);
                Assert.That(state.CurrentCctvTarget, Is.EqualTo(ShipCctvTarget.CargoHold));

                state.CycleCctv(1);
                Assert.That(state.CurrentCctvTarget, Is.EqualTo(ShipCctvTarget.EngineRoom));

                state.CycleCctv(1);
                Assert.That(state.CurrentCctvTarget, Is.EqualTo(ShipCctvTarget.Armory));

                state.CycleCctv(-1);
                Assert.That(state.CurrentCctvTarget, Is.EqualTo(ShipCctvTarget.EngineRoom));
            }
            finally
            {
                Object.DestroyImmediate(stateObject);
            }
        }

        [Test]
        public void RoomDamageEffects_LimitDeviceOperationsAndHudStatus()
        {
            var stateObject = new GameObject("Ship Device State Test");
            var hudObject = new GameObject("Ship Device HUD Test");
            try
            {
                var state = stateObject.AddComponent<ShipDeviceInteractionState>();
                var label = new GameObject("Panel Text").AddComponent<UnityEngine.UI.Text>();
                label.transform.SetParent(hudObject.transform);
                var hud = hudObject.AddComponent<ShipDeviceHud>();
                hud.Configure(state, label, null);

                state.SetShipState(ShipState.CreateDefault()
                    .WithRoom(ShipRoomId.ControlRoom, new ShipRoomState(25, 100)));
                state.ActivateDevice(ShipDeviceType.ControlRoomMainScreen);
                state.CycleCctv(1);
                hud.RefreshPanel();

                Assert.That(state.CurrentCctvTarget, Is.EqualTo(ShipCctvTarget.Cockpit));
                Assert.That(label.text, Does.Contain("CCTV Channels: 0/5"));

                state.SetShipState(ShipState.CreateDefault()
                    .WithRoom(ShipRoomId.SupplyRoom, new ShipRoomState(75, 100)));
                state.ActivateDevice(ShipDeviceType.SupplyRoomStorageCabinet);
                hud.RefreshPanel();

                Assert.That(state.SupplySlotCount, Is.Zero);
                Assert.That(label.text, Does.Contain("Usable Slots: 0/3"));

                state.SetShipState(ShipState.CreateDefault()
                    .WithRoom(ShipRoomId.EngineRoom, new ShipRoomState(50, 100)));
                state.ActivateDevice(ShipDeviceType.EngineRoomPowerScreen);
                hud.RefreshPanel();

                Assert.That(state.EngineOverclockUsedThisRun, Is.False);
                Assert.That(label.text, Does.Contain("Overclock: Offline"));

                state.SetShipState(ShipState.CreateDefault()
                    .WithRoom(ShipRoomId.Armory, new ShipRoomState(0, 100)));
                state.ActivateDevice(ShipDeviceType.ArmoryTurretHandle);
                var fireResult = state.FireManualTurret();
                hud.RefreshPanel();

                Assert.That(state.TurretManualModeActive, Is.False);
                Assert.That(fireResult.Outcome, Is.EqualTo(ManualTurretFireOutcome.Inactive));
                Assert.That(label.text, Does.Contain("Manual Turret: Offline"));
            }
            finally
            {
                Object.DestroyImmediate(hudObject);
                Object.DestroyImmediate(stateObject);
            }
        }

        [Test]
        public void CockpitHelm_TogglesManualFlightDuringActiveTransportRun()
        {
            var stateObject = new GameObject("Ship Device State Test");
            try
            {
                var state = stateObject.AddComponent<ShipDeviceInteractionState>();
                state.StartTransportRun(60);

                Assert.That(state.CurrentFlightMode, Is.EqualTo(ShipFlightMode.AutoPilot));

                state.ActivateDevice(ShipDeviceType.CockpitHelm);
                Assert.That(state.ManualFlightModeActive, Is.True);
                Assert.That(state.CurrentFlightMode, Is.EqualTo(ShipFlightMode.ManualFlight));

                Assert.That(state.ExitManualFlightToAutoPilot(), Is.True);
                Assert.That(state.ManualFlightModeActive, Is.False);
                Assert.That(state.CurrentFlightMode, Is.EqualTo(ShipFlightMode.AutoPilot));
            }
            finally
            {
                Object.DestroyImmediate(stateObject);
            }
        }

        [Test]
        public void StartTransportRun_ClearsPreviousManualTurretPanel()
        {
            var stateObject = new GameObject("Ship Device State Test");
            try
            {
                var state = stateObject.AddComponent<ShipDeviceInteractionState>();

                state.ActivateDevice(ShipDeviceType.ArmoryTurretHandle);
                Assert.That(state.ActivePanelMode, Is.EqualTo(ShipDevicePanelMode.TurretManual));
                Assert.That(state.TurretManualModeActive, Is.True);

                state.StartTransportRun(60);

                Assert.That(state.HasActiveTransportRun, Is.True);
                Assert.That(state.ActivePanelMode, Is.EqualTo(ShipDevicePanelMode.None));
                Assert.That(state.TurretManualModeActive, Is.False);
                Assert.That(state.CurrentManualTurret.IsActive, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(stateObject);
            }
        }

        [Test]
        public void ControlRoomCctv_IncludesSupplyRoomAndSwitchesOriginalScreenModes()
        {
            var stateObject = new GameObject("Ship Device State Test");
            try
            {
                var state = stateObject.AddComponent<ShipDeviceInteractionState>();

                state.ActivateDevice(ShipDeviceType.ControlRoomMainScreen);
                state.CycleCctv(1);
                state.CycleCctv(1);
                state.CycleCctv(1);
                state.CycleCctv(1);

                Assert.That(state.CurrentCctvTarget, Is.EqualTo(ShipCctvTarget.SupplyRoom));

                state.SwitchControlRoomScreenByRightClick();
                Assert.That(state.CurrentControlRoomScreenMode, Is.EqualTo(ShipControlRoomScreenMode.VerticalRoomList));

                state.SwitchControlRoomScreenByRightClick();
                Assert.That(state.CurrentControlRoomScreenMode, Is.EqualTo(ShipControlRoomScreenMode.HorizontalShipLayout));
            }
            finally
            {
                Object.DestroyImmediate(stateObject);
            }
        }

        [Test]
        public void ControlRoomDisplayIndexSelection_StartsPurificationFromVerticalList()
        {
            var stateObject = new GameObject("Ship Device State Test");
            try
            {
                var state = stateObject.AddComponent<ShipDeviceInteractionState>();
                state.ActivateDevice(ShipDeviceType.ControlRoomMainScreen);
                state.SetControlRoomScreenMode(ShipControlRoomScreenMode.VerticalRoomList);

                var selected = state.SelectControlRoomVerticalRoomByDisplayIndex(6);

                Assert.That(selected, Is.True);
                Assert.That(state.CurrentControlRoomPurification.IsActive, Is.True);
                Assert.That(state.CurrentControlRoomPurification.TargetRoom, Is.EqualTo(ShipRoomId.Armory));
                Assert.That(state.CurrentShipState.GetRoom(ShipRoomId.Armory).IsSealed, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(stateObject);
            }
        }

        [Test]
        public void ControlRoomPanelExit_ClosesPanelWithoutStoppingActivePurification()
        {
            var stateObject = new GameObject("Ship Device State Test");
            try
            {
                var state = stateObject.AddComponent<ShipDeviceInteractionState>();
                state.ActivateDevice(ShipDeviceType.ControlRoomMainScreen);
                state.SetControlRoomScreenMode(ShipControlRoomScreenMode.VerticalRoomList);
                state.SelectControlRoomPurificationTarget(ShipRoomId.Armory);

                var exited = state.ExitActiveDevicePanel();

                Assert.That(exited, Is.True);
                Assert.That(state.ActivePanelMode, Is.EqualTo(ShipDevicePanelMode.None));
                Assert.That(state.CurrentControlRoomScreenMode, Is.EqualTo(ShipControlRoomScreenMode.MainCctv));
                Assert.That(state.CurrentControlRoomPurification.IsActive, Is.True);
                Assert.That(state.CurrentShipState.GetRoom(ShipRoomId.Armory).IsSealed, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(stateObject);
            }
        }

        [Test]
        public void ControlRoomPurification_SealsSelectedRoomAndDamagesPlayerInsideWithoutRoomDamage()
        {
            var stateObject = new GameObject("Ship Device State Test");
            var statusObject = new GameObject("Player Status Test");
            var settings = ScriptableObject.CreateInstance<FirstPersonPlayerSettings>();
            try
            {
                var state = stateObject.AddComponent<ShipDeviceInteractionState>();
                var status = statusObject.AddComponent<FirstPersonPlayerStatus>();
                status.Configure(settings);
                state.SetPlayerStatusForValidation(status);
                state.ActivateDevice(ShipDeviceType.ControlRoomMainScreen);
                state.SetControlRoomScreenMode(ShipControlRoomScreenMode.VerticalRoomList);

                var started = state.SelectControlRoomPurificationTarget(ShipRoomId.Armory);
                var firstTick = state.TickControlRoomOperations(3f, ShipRoomId.Armory);

                Assert.That(started, Is.True);
                Assert.That(state.CurrentShipState.GetRoom(ShipRoomId.Armory).IsSealed, Is.True);
                Assert.That(firstTick.FireDamageThisTick, Is.EqualTo(50));
                Assert.That(status.CurrentShield, Is.Zero);
                Assert.That(status.CurrentHealth, Is.EqualTo(100));
                Assert.That(state.LastPurificationPlayerDamage, Is.EqualTo(50));
                Assert.That(state.CurrentShipState.GetRoom(ShipRoomId.Armory).CurrentDurability, Is.EqualTo(100));

                state.TickControlRoomOperations(27f, ShipRoomId.Armory);

                Assert.That(state.CurrentControlRoomPurification.IsActive, Is.False);
                Assert.That(state.CurrentShipState.GetRoom(ShipRoomId.Armory).IsSealed, Is.False);
                Assert.That(state.CurrentShipState.GetRoom(ShipRoomId.Armory).CurrentDurability, Is.EqualTo(100));
            }
            finally
            {
                Object.DestroyImmediate(settings);
                Object.DestroyImmediate(statusObject);
                Object.DestroyImmediate(stateObject);
            }
        }

        [Test]
        public void ControlRoomPurification_DoesNotDamagePlayerOutsideSelectedRoom()
        {
            var stateObject = new GameObject("Ship Device State Test");
            var statusObject = new GameObject("Player Status Test");
            var settings = ScriptableObject.CreateInstance<FirstPersonPlayerSettings>();
            try
            {
                var state = stateObject.AddComponent<ShipDeviceInteractionState>();
                var status = statusObject.AddComponent<FirstPersonPlayerStatus>();
                status.Configure(settings);
                state.SetPlayerStatusForValidation(status);
                state.ActivateDevice(ShipDeviceType.ControlRoomMainScreen);
                state.SetControlRoomScreenMode(ShipControlRoomScreenMode.VerticalRoomList);
                state.SelectControlRoomPurificationTarget(ShipRoomId.Armory);

                state.TickControlRoomOperations(3f, ShipRoomId.Cockpit);

                Assert.That(status.CurrentShield, Is.EqualTo(50));
                Assert.That(status.CurrentHealth, Is.EqualTo(100));
                Assert.That(state.LastPurificationPlayerDamage, Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(settings);
                Object.DestroyImmediate(statusObject);
                Object.DestroyImmediate(stateObject);
            }
        }

        [Test]
        public void ControlRoomDestroyed_OpensSealedRoomAndBlocksPurification()
        {
            var stateObject = new GameObject("Ship Device State Test");
            try
            {
                var state = stateObject.AddComponent<ShipDeviceInteractionState>();
                state.ActivateDevice(ShipDeviceType.ControlRoomMainScreen);
                state.SetControlRoomScreenMode(ShipControlRoomScreenMode.VerticalRoomList);
                Assert.That(state.SelectControlRoomPurificationTarget(ShipRoomId.CargoHold), Is.True);

                state.SetShipState(ShipState.CreateDefault()
                    .WithRoom(ShipRoomId.ControlRoom, new ShipRoomState(0, 100))
                    .WithRoom(ShipRoomId.CargoHold, new ShipRoomState(100, 100, false, false, true)));

                Assert.That(state.CurrentControlRoomPurification.IsActive, Is.False);
                Assert.That(state.CurrentShipState.GetRoom(ShipRoomId.CargoHold).IsSealed, Is.False);
                Assert.That(state.SelectControlRoomPurificationTarget(ShipRoomId.Cockpit), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(stateObject);
            }
        }

        [Test]
        public void SupplyItemUse_AppliesTreatmentRecoveryToPlayerStatus()
        {
            var stateObject = new GameObject("Ship Device State Test");
            var statusObject = new GameObject("Player Status Test");
            var settings = ScriptableObject.CreateInstance<FirstPersonPlayerSettings>();
            try
            {
                var state = stateObject.AddComponent<ShipDeviceInteractionState>();
                var status = statusObject.AddComponent<FirstPersonPlayerStatus>();
                status.Configure(settings);
                status.SetVitalsForValidation(60, 20);
                state.SetPlayerStatusForValidation(status);
                state.SetEquipmentStateForValidation(PlayerEquipmentState.CreateDefaultAssociationIssue()
                    .WithSupplySlot(0, EquipmentSlotState.Purchased(EquipmentItemKind.InjuryReliever, EquipmentRules.InjuryRelieverPriceCredits)));

                var result = state.UseSupplyItem(0);

                Assert.That(result.Outcome, Is.EqualTo(EquipmentUseOutcome.TreatmentApplied));
                Assert.That(status.CurrentHealth, Is.EqualTo(85));
                Assert.That(status.CurrentShield, Is.EqualTo(20));
                Assert.That(state.CurrentEquipmentState.GetSupplySlot(0).IsEmpty, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(settings);
                Object.DestroyImmediate(statusObject);
                Object.DestroyImmediate(stateObject);
            }
        }

        [Test]
        public void CockpitDamage_AtFiftyPercentPreventsAutoPilotReturn()
        {
            var stateObject = new GameObject("Ship Device State Test");
            try
            {
                var state = stateObject.AddComponent<ShipDeviceInteractionState>();
                state.SetShipState(ShipState.CreateDefault()
                    .WithRoom(ShipRoomId.Cockpit, new ShipRoomState(50, 100)));
                state.StartTransportRun(60);

                Assert.That(state.IsAutoPilotAvailable, Is.False);
                Assert.That(state.CurrentFlightMode, Is.EqualTo(ShipFlightMode.ManualFlight));
                Assert.That(state.ExitManualFlightToAutoPilot(), Is.False);
                Assert.That(state.CurrentFlightMode, Is.EqualTo(ShipFlightMode.ManualFlight));
            }
            finally
            {
                Object.DestroyImmediate(stateObject);
            }
        }

        [Test]
        public void TransportHazard_AutoPilotAppliesDamageWhenAsteroidFieldCompletes()
        {
            var stateObject = new GameObject("Ship Device State Test");
            try
            {
                var state = stateObject.AddComponent<ShipDeviceInteractionState>();
                state.StartTransportRun(60);
                state.StartTransportHazardForValidation(TransportHazardState.StartAsteroidField(991, 10));

                state.TickTransportRun(10f);

                Assert.That(state.HasActiveTransportHazard, Is.False);
                Assert.That(state.LastTransportHazardResult.Resolution, Is.EqualTo(TransportHazardResolution.DirectHit));
                Assert.That(ShipStateRules.CalculateRepairCost(state.CurrentShipState), Is.GreaterThan(0));
                Assert.That(state.CurrentTransportRun.Ship, Is.SameAs(state.CurrentShipState));
            }
            finally
            {
                Object.DestroyImmediate(stateObject);
            }
        }

        [Test]
        public void TransportHazard_ManualFlightInputAvoidsAsteroidDamage()
        {
            var stateObject = new GameObject("Ship Device State Test");
            try
            {
                var state = stateObject.AddComponent<ShipDeviceInteractionState>();
                state.StartTransportRun(60);
                state.StartTransportHazardForValidation(TransportHazardState.StartAsteroidField(991, 10));

                state.ActivateDevice(ShipDeviceType.CockpitHelm);
                state.ApplyManualFlightInput(1f, 0f, 1f);
                state.TickTransportRun(10f);

                Assert.That(state.HasActiveTransportHazard, Is.False);
                Assert.That(state.LastTransportHazardResult.Resolution, Is.EqualTo(TransportHazardResolution.Avoided));
                Assert.That(ShipStateRules.CalculateRepairCost(state.CurrentShipState), Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(stateObject);
            }
        }

        [Test]
        public void TransportHazardOccurrence_ChecksScheduledAsteroidAndFameUnlocks()
        {
            var stateObject = new GameObject("Ship Device State Test");
            try
            {
                var state = stateObject.AddComponent<ShipDeviceInteractionState>();
                var session = CreatePostTutorialTransport();
                var asteroidCheck = FindStartingCheck(session, TransportHazardType.AsteroidFieldSmall);
                var asteroidInterval = TransportHazardRules.AsteroidFieldOccurrenceCheckIntervalSeconds;
                state.StartTransportRun(60);

                var startedBeforeInterval = state.TickTransportHazardOccurrenceForCurrentRun(
                    asteroidInterval * asteroidCheck - 0.1f,
                    session);
                var startedAtInterval = state.TickTransportHazardOccurrenceForCurrentRun(0.1f, session);

                Assert.That(startedBeforeInterval, Is.False);
                Assert.That(startedAtInterval, Is.True);
                Assert.That(state.HasActiveTransportHazard, Is.True);
                Assert.That(
                    state.CurrentTransportHazard.HazardType == TransportHazardType.AsteroidFieldSmall ||
                    state.CurrentTransportHazard.HazardType == TransportHazardType.AsteroidFieldLarge,
                    Is.True);

                var cargoSession = session
                    .WithReputation(new ReputationState(1800, 0, false))
                    .WithReputation(new ReputationState(100, 0, false));
                var cargoCheck = FindStartingCheck(cargoSession, TransportHazardType.CargoFreedomLeagueRegion);
                state.TickTransportRun(state.CurrentTransportHazard.DurationSeconds);
                var cargoStarted = state.TryStartTransportHazardForCurrentRun(
                    cargoSession,
                    TransportHazardType.CargoFreedomLeagueRegion,
                    cargoCheck);

                Assert.That(cargoSession.TransportHazardUnlocks.CargoFreedomLeagueUnlocked, Is.True);
                Assert.That(cargoStarted, Is.True);
                Assert.That(state.CurrentTransportHazard.HazardType, Is.EqualTo(TransportHazardType.CargoFreedomLeagueRegion));
            }
            finally
            {
                Object.DestroyImmediate(stateObject);
            }
        }

        [Test]
        public void ManualFlightBooster_ReducesAsteroidDurationAndRequiresEngineRoom()
        {
            var stateObject = new GameObject("Ship Device State Test");
            try
            {
                var state = stateObject.AddComponent<ShipDeviceInteractionState>();
                state.StartTransportRun(60);
                state.StartTransportHazardForValidation(TransportHazardState.StartAsteroidField(991, 12));
                state.ActivateDevice(ShipDeviceType.CockpitHelm);

                var used = state.UseManualFlightBooster();

                Assert.That(used, Is.True);
                Assert.That(state.CurrentTransportHazard.ManualAvoidanceSeconds, Is.EqualTo(10f).Within(0.0001f));

                state.TickTransportRun(2f);

                Assert.That(state.HasActiveTransportHazard, Is.False);
                Assert.That(state.LastTransportHazardResult.Resolution, Is.EqualTo(TransportHazardResolution.Avoided));
                Assert.That(ShipStateRules.CalculateRepairCost(state.CurrentShipState), Is.Zero);

                state.SetShipState(ShipState.CreateDefault()
                    .WithRoom(ShipRoomId.EngineRoom, new ShipRoomState(50, 100)));
                state.StartTransportRun(60);
                state.StartTransportHazardForValidation(TransportHazardState.StartAsteroidField(991, 12));
                state.ActivateDevice(ShipDeviceType.CockpitHelm);

                Assert.That(state.UseManualFlightBooster(), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(stateObject);
            }
        }

        private static GameSessionState CreatePostTutorialTransport()
        {
            var tutorialRun = GameSessionState.StartAssociationSession()
                .StartTransport(TransportContractDefinition.CreateTutorial());
            var completed = tutorialRun.CompleteTransport(new SettlementInput(
                ContractType.Association,
                ContractDifficulty.Intro,
                TransportContractDefinition.CreateTutorial().Cargo,
                ShipState.CreateDefault(),
                new CrewState(1, 0),
                tutorialRun.Wallet,
                contractBasePay: 1000,
                repairSupportAmount: 100));
            return completed.StartTransport(TransportContractDefinition.CreateAssociationFollowUp());
        }

        private static int FindStartingCheck(GameSessionState session, TransportHazardType hazardType)
        {
            for (var i = 1; i <= 1000; i++)
            {
                if (TransportHazardRules.ShouldStartHazard(session, hazardType, i))
                {
                    return i;
                }
            }

            throw new AssertionException("No deterministic hazard check found for " + hazardType + ".");
        }

        [Test]
        public void ManualFlight_ForcesArmoryIntoAutoTurretMode()
        {
            var stateObject = new GameObject("Ship Device State Test");
            try
            {
                var state = stateObject.AddComponent<ShipDeviceInteractionState>();
                state.StartTransportRun(60);
                state.ActivateDevice(ShipDeviceType.CockpitHelm);

                state.ActivateDevice(ShipDeviceType.ArmoryTurretHandle);

                Assert.That(state.CurrentWeaponOperationMode, Is.EqualTo(ShipWeaponOperationMode.AutoTurret));
                Assert.That(state.TurretManualModeActive, Is.False);
                Assert.That(state.ActivePanelMode, Is.EqualTo(ShipDevicePanelMode.None));
            }
            finally
            {
                Object.DestroyImmediate(stateObject);
            }
        }

        [Test]
        public void ManualTurret_DestroysExternalTargetAndNeutralizesAsteroidHazard()
        {
            var stateObject = new GameObject("Ship Device State Test");
            try
            {
                var state = stateObject.AddComponent<ShipDeviceInteractionState>();
                state.StartTransportRun(60);
                state.StartTransportHazardForValidation(TransportHazardState.StartAsteroidField(991, 10));

                state.ActivateDevice(ShipDeviceType.ArmoryTurretHandle);
                var target = state.CurrentExternalTarget;
                state.SetManualTurretAimForValidation(target.PositionX, target.PositionY);
                ManualTurretFireResult finalShot = default;
                for (var i = 0; i < 20 && state.CurrentExternalTarget.IsActive; i++)
                {
                    finalShot = state.FireManualTurret();
                }

                Assert.That(finalShot.Outcome, Is.EqualTo(ManualTurretFireOutcome.Destroyed));
                Assert.That(state.HasActiveTransportHazard, Is.False);
                Assert.That(state.CurrentExternalTarget.IsActive, Is.False);
                Assert.That(state.LastTransportHazardResult.Resolution, Is.EqualTo(TransportHazardResolution.Neutralized));
                Assert.That(ShipStateRules.CalculateRepairCost(state.CurrentShipState), Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(stateObject);
            }
        }

        [Test]
        public void ManualTurret_DestroysAlienLifeformExternalTargetAndNeutralizesHazard()
        {
            var stateObject = new GameObject("Ship Device State Test");
            try
            {
                var state = stateObject.AddComponent<ShipDeviceInteractionState>();
                state.StartTransportRun(60);
                state.StartTransportHazardForValidation(
                    TransportHazardState.Start(TransportHazardType.AlienLifeRegion, 991, 30));

                Assert.That(state.CurrentExternalTarget.TargetType, Is.EqualTo(ExternalTargetType.AlienLifeform));
                Assert.That(state.CurrentExternalTarget.MaxHealth, Is.EqualTo(350));

                state.ActivateDevice(ShipDeviceType.ArmoryTurretHandle);
                var target = state.CurrentExternalTarget;
                state.SetManualTurretAimForValidation(target.PositionX, target.PositionY);
                ManualTurretFireResult finalShot = default;
                for (var i = 0; i < 7 && state.CurrentExternalTarget.IsActive; i++)
                {
                    finalShot = state.FireManualTurret();
                }

                Assert.That(finalShot.Outcome, Is.EqualTo(ManualTurretFireOutcome.Destroyed));
                Assert.That(state.HasActiveTransportHazard, Is.False);
                Assert.That(state.CurrentExternalTarget.IsActive, Is.False);
                Assert.That(state.LastTransportHazardResult.Resolution, Is.EqualTo(TransportHazardResolution.Neutralized));
                Assert.That(state.LastTransportHazardResult.RoomDamages, Is.Empty);
                Assert.That(ShipStateRules.CalculateRepairCost(state.CurrentShipState), Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(stateObject);
            }
        }

        [Test]
        public void ManualTurret_DestroysCargoFreedomLeagueCraftAndNeutralizesHazard()
        {
            var stateObject = new GameObject("Ship Device State Test");
            try
            {
                var state = stateObject.AddComponent<ShipDeviceInteractionState>();
                state.StartTransportRun(60);
                state.StartTransportHazardForValidation(
                    TransportHazardState.Start(TransportHazardType.CargoFreedomLeagueRegion, 3, 30));

                Assert.That(state.CurrentExternalTarget.TargetType, Is.EqualTo(ExternalTargetType.CargoFreedomLeagueBoardingCraft));
                Assert.That(state.CurrentExternalTarget.MaxHealth, Is.EqualTo(CargoFreedomLeagueRules.RevolutionBoardingCraftHealth));

                state.ActivateDevice(ShipDeviceType.ArmoryTurretHandle);
                var target = state.CurrentExternalTarget;
                state.SetManualTurretAimForValidation(target.PositionX, target.PositionY);
                ManualTurretFireResult finalShot = default;
                for (var i = 0; i < 20 && state.CurrentExternalTarget.IsActive; i++)
                {
                    finalShot = state.FireManualTurret();
                }

                Assert.That(finalShot.Outcome, Is.EqualTo(ManualTurretFireOutcome.Destroyed));
                Assert.That(state.HasActiveTransportHazard, Is.False);
                Assert.That(state.CurrentExternalTarget.IsActive, Is.False);
                Assert.That(state.LastTransportHazardResult.Resolution, Is.EqualTo(TransportHazardResolution.Neutralized));
                Assert.That(state.LastTransportHazardResult.RoomDamages, Is.Empty);
                Assert.That(ShipStateRules.CalculateRepairCost(state.CurrentShipState), Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(stateObject);
            }
        }

        [Test]
        public void ManualTurret_DestroysSpacePirateAtaCraftAndNeutralizesHazard()
        {
            var stateObject = new GameObject("Ship Device State Test");
            try
            {
                var state = stateObject.AddComponent<ShipDeviceInteractionState>();
                state.StartTransportRun(60);
                state.StartTransportHazardForValidation(
                    TransportHazardState.Start(TransportHazardType.SpacePirateRegion, 3, 60));

                Assert.That(state.CurrentExternalTarget.TargetType, Is.EqualTo(ExternalTargetType.SpacePirateBoardingCraft));
                Assert.That(state.CurrentExternalTarget.MaxHealth, Is.EqualTo(SpacePirateRules.AtaBoardingCraftHealth));

                state.ActivateDevice(ShipDeviceType.ArmoryTurretHandle);
                var target = state.CurrentExternalTarget;
                state.SetManualTurretAimForValidation(target.PositionX, target.PositionY);
                ManualTurretFireResult finalShot = default;
                for (var i = 0; i < 24 && state.CurrentExternalTarget.IsActive; i++)
                {
                    finalShot = state.FireManualTurret();
                }

                Assert.That(finalShot.Outcome, Is.EqualTo(ManualTurretFireOutcome.Destroyed));
                Assert.That(state.HasActiveTransportHazard, Is.False);
                Assert.That(state.CurrentExternalTarget.IsActive, Is.False);
                Assert.That(state.LastTransportHazardResult.Resolution, Is.EqualTo(TransportHazardResolution.Neutralized));
                Assert.That(state.LastTransportHazardResult.RoomDamages, Is.Empty);
                Assert.That(ShipStateRules.CalculateRepairCost(state.CurrentShipState), Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(stateObject);
            }
        }

        [Test]
        public void ManualTurret_UpgradeMagazineAndPlasmaNeutralizeExternalTarget()
        {
            var stateObject = new GameObject("Ship Device State Test");
            try
            {
                var upgrades = ShipUpgradeState.Empty
                    .WithPurchasedTier(ShipUpgradeCategory.WeaponSystems, 2)
                    .WithEquippedTier(ShipUpgradeCategory.WeaponSystems, 2);
                var state = stateObject.AddComponent<ShipDeviceInteractionState>();
                state.SetShipUpgradeStateForValidation(upgrades);
                state.StartTransportRun(60);
                state.StartTransportHazardForValidation(TransportHazardState.StartAsteroidField(991, 10));
                state.ActivateDevice(ShipDeviceType.ArmoryTurretHandle);
                var target = state.CurrentExternalTarget;
                state.SetManualTurretAimForValidation(target.PositionX, target.PositionY);

                var plasma = state.FireManualTurretPlasma();
                state.TickTransportRun(0.9f);

                Assert.That(state.CurrentManualTurret.MagazineCapacity, Is.EqualTo(75));
                Assert.That(plasma.Outcome, Is.EqualTo(ManualTurretPlasmaOutcome.Activated));
                Assert.That(state.HasActiveTransportHazard, Is.False);
                Assert.That(state.LastTransportHazardResult.Resolution, Is.EqualTo(TransportHazardResolution.Neutralized));
            }
            finally
            {
                Object.DestroyImmediate(stateObject);
            }
        }

        [Test]
        public void ManualTurret_ArmoryDamageBlocksUpgradedPlasma()
        {
            var stateObject = new GameObject("Ship Device State Test");
            try
            {
                var upgrades = ShipUpgradeState.Empty
                    .WithPurchasedTier(ShipUpgradeCategory.WeaponSystems, 2)
                    .WithEquippedTier(ShipUpgradeCategory.WeaponSystems, 2);
                var state = stateObject.AddComponent<ShipDeviceInteractionState>();
                state.SetShipUpgradeStateForValidation(upgrades);
                state.SetShipState(ShipState.CreateDefault()
                    .WithRoom(ShipRoomId.Armory, new ShipRoomState(75, 100)));
                state.StartTransportRun(60);
                state.StartTransportHazardForValidation(TransportHazardState.StartAsteroidField(991, 10));
                state.ActivateDevice(ShipDeviceType.ArmoryTurretHandle);

                var plasma = state.FireManualTurretPlasma();

                Assert.That(plasma.Outcome, Is.EqualTo(ManualTurretPlasmaOutcome.Unavailable));
                Assert.That(state.HasActiveTransportHazard, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(stateObject);
            }
        }

        [Test]
        public void ManualTurret_ReloadCompletesAfterTwoTransportSeconds()
        {
            var stateObject = new GameObject("Ship Device State Test");
            try
            {
                var state = stateObject.AddComponent<ShipDeviceInteractionState>();
                state.StartTransportRun(60);

                state.ActivateDevice(ShipDeviceType.ArmoryTurretHandle);
                state.FireManualTurret();
                state.BeginManualTurretReload();
                state.TickTransportRun(1f);

                Assert.That(state.CurrentManualTurret.IsReloading, Is.True);
                Assert.That(state.CurrentManualTurret.AmmoInMagazine, Is.EqualTo(ManualTurretState.MagazineSize - 1));

                state.TickTransportRun(1f);

                Assert.That(state.CurrentManualTurret.IsReloading, Is.False);
                Assert.That(state.CurrentManualTurret.AmmoInMagazine, Is.EqualTo(ManualTurretState.MagazineSize));
            }
            finally
            {
                Object.DestroyImmediate(stateObject);
            }
        }

        [Test]
        public void ManualTurretView_HeldLeftClickRepeatsFireAfterInterval()
        {
            var stateObject = new GameObject("Ship Device State Test");
            var viewObject = new GameObject("Manual Turret View Test");
            try
            {
                var state = stateObject.AddComponent<ShipDeviceInteractionState>();
                state.StartTransportRun(60);
                state.StartTransportHazardForValidation(TransportHazardState.StartAsteroidField(991, 10));
                state.ActivateDevice(ShipDeviceType.ArmoryTurretHandle);
                var target = state.CurrentExternalTarget;
                state.SetManualTurretAimForValidation(target.PositionX, target.PositionY);

                var view = viewObject.AddComponent<ManualTurretView>();
                view.Configure(state, null, null, null, null, null);

                var first = view.ProcessHeldFireForValidation(0f, true, true);
                var blocked = view.ProcessHeldFireForValidation(ManualTurretState.HeldFireIntervalSeconds * 0.5f, true, false);
                var repeated = view.ProcessHeldFireForValidation(ManualTurretState.HeldFireIntervalSeconds, true, false);

                Assert.That(first.Outcome, Is.EqualTo(ManualTurretFireOutcome.Hit));
                Assert.That(blocked.Outcome, Is.EqualTo(ManualTurretFireOutcome.None));
                Assert.That(repeated.Outcome, Is.EqualTo(ManualTurretFireOutcome.Hit));
                Assert.That(state.CurrentManualTurret.AmmoInMagazine, Is.EqualTo(ManualTurretState.MagazineSize - 2));
            }
            finally
            {
                Object.DestroyImmediate(viewObject);
                Object.DestroyImmediate(stateObject);
            }
        }

        [Test]
        public void SeedIntruderOccurrence_TutorialIsExcludedAndFollowUpCanStartAfterCheckInterval()
        {
            var stateObject = new GameObject("Ship Device State Test");
            try
            {
                var state = stateObject.AddComponent<ShipDeviceInteractionState>();
                var tutorialSession = GameSessionState.StartAssociationSession()
                    .StartTransport(TransportContractDefinition.CreateTutorial());
                state.StartTransportRun(60);

                var tutorialStarted = state.TickSeedIntruderOccurrenceForCurrentRun(
                    SeedIntruderRules.OccurrenceCheckIntervalSeconds * 10f,
                    tutorialSession);

                Assert.That(tutorialStarted, Is.False);
                Assert.That(state.HasActiveSeedIntruder, Is.False);
                Assert.That(state.SeedIntruderCheckCount, Is.Zero);

                var followUpSession = CreateFollowUpTransportSession();
                state.StartTransportRun(followUpSession.ActiveTransportContract.Value.DurationSeconds);
                var started = false;
                for (var i = 0; i < 200 && !started; i++)
                {
                    started = state.TickSeedIntruderOccurrenceForCurrentRun(
                        SeedIntruderRules.OccurrenceCheckIntervalSeconds,
                        followUpSession);
                }

                Assert.That(started, Is.True);
                Assert.That(state.HasActiveSeedIntruder, Is.True);
                Assert.That(state.CurrentSeedIntruder.Kind, Is.EqualTo(SeedIntruderKind.Parvum));
            }
            finally
            {
                Object.DestroyImmediate(stateObject);
            }
        }

        [Test]
        public void SeedIntruderDamage_AppliesToShipStateAndHudStatus()
        {
            var stateObject = new GameObject("Ship Device State Test");
            var hudObject = new GameObject("Ship Device HUD Test");
            try
            {
                var state = stateObject.AddComponent<ShipDeviceInteractionState>();
                state.StartTransportRun(60);
                state.StartSeedIntruderForValidation(
                    SeedIntruderRules.CreateParvumIntrusionForSeed(47, ShipRoomId.Cockpit));

                state.TickTransportRun(SeedIntruderRules.ParvumAttackDelaySeconds);

                Assert.That(state.CurrentSeedIntruder.TotalRoomDamageApplied, Is.EqualTo(SeedIntruderRules.ParvumShipFacilityDamage));
                Assert.That(ShipStateRules.CalculateRepairCost(state.CurrentShipState), Is.GreaterThan(0));
                Assert.That(state.CurrentTransportRun.Ship, Is.SameAs(state.CurrentShipState));

                var statusObject = new GameObject("Transport Status");
                statusObject.transform.SetParent(hudObject.transform);
                var statusText = statusObject.AddComponent<UnityEngine.UI.Text>();
                var hud = hudObject.AddComponent<ShipDeviceHud>();
                hud.Configure(state, null, statusText);
                hud.RefreshTransportStatus();

                Assert.That(statusText.text, Does.Contain("Intruder: Parvum"));
                Assert.That(statusText.text, Does.Contain("Intruder Damage: 3"));
            }
            finally
            {
                Object.DestroyImmediate(hudObject);
                Object.DestroyImmediate(stateObject);
            }
        }

        [Test]
        public void SeedIntruderDamage_ControlRoomDestroyedIncreasesFacilityDamage()
        {
            var stateObject = new GameObject("Ship Device State Test");
            try
            {
                var state = stateObject.AddComponent<ShipDeviceInteractionState>();
                state.SetShipState(ShipState.CreateDefault()
                    .WithRoom(ShipRoomId.ControlRoom, new ShipRoomState(0, 100)));
                state.StartTransportRun(60);
                state.StartSeedIntruderForValidation(
                    SeedIntruderRules.CreateParvumIntrusionForSeed(47, ShipRoomId.Cockpit));

                state.TickTransportRun(SeedIntruderRules.ParvumAttackDelaySeconds);

                Assert.That(state.CurrentSeedIntruder.TotalRoomDamageApplied, Is.EqualTo(9));
            }
            finally
            {
                Object.DestroyImmediate(stateObject);
            }
        }

        [Test]
        public void SeedIntruderVisualView_ShowsActiveParvumAtCurrentRoomAndHidesWhenResolved()
        {
            var stateObject = new GameObject("Ship Device State Test");
            var viewObject = new GameObject("Seed Intruder View Test");
            var anchorsObject = new GameObject("Seed Intruder Anchors Test");
            try
            {
                var state = stateObject.AddComponent<ShipDeviceInteractionState>();
                state.StartTransportRun(60);

                var visualRoot = new GameObject("Parvum Visual Test");
                visualRoot.transform.SetParent(viewObject.transform);

                var cockpit = CreateAnchor(anchorsObject.transform, ShipRoomId.Cockpit, new Vector3(1f, 0f, 0f));
                var cargoHold = CreateAnchor(anchorsObject.transform, ShipRoomId.CargoHold, new Vector3(2f, 0f, 0f));
                var engineRoom = CreateAnchor(anchorsObject.transform, ShipRoomId.EngineRoom, new Vector3(3f, 0f, 0f));
                var controlRoom = CreateAnchor(anchorsObject.transform, ShipRoomId.ControlRoom, new Vector3(4f, 0f, 0f));
                var armory = CreateAnchor(anchorsObject.transform, ShipRoomId.Armory, new Vector3(5f, 0f, 0f));
                var supplyRoom = CreateAnchor(anchorsObject.transform, ShipRoomId.SupplyRoom, new Vector3(6f, 0f, 0f));

                var view = viewObject.AddComponent<SeedIntruderVisualView>();
                view.Configure(state, visualRoot, cockpit, cargoHold, engineRoom, controlRoom, armory, supplyRoom);

                Assert.That(view.IsViewActive, Is.False);

                var intruder = SeedIntruderRules.CreateParvumIntrusionForSeed(47, ShipRoomId.Cockpit);
                state.StartSeedIntruderForValidation(intruder);
                view.RefreshView();

                var initialAnchor = view.GetAnchorForValidation(intruder.Intruder.CurrentRoom);
                Assert.That(view.IsViewActive, Is.True);
                Assert.That(view.LastDisplayedRoom, Is.EqualTo(intruder.Intruder.CurrentRoom));
                Assert.That(Vector3.Distance(visualRoot.transform.position, initialAnchor.position), Is.LessThan(0.001f));

                state.TickTransportRun(SeedIntruderRules.ParvumAttackDelaySeconds);
                view.RefreshView();

                var targetAnchor = view.GetAnchorForValidation(state.CurrentSeedIntruder.Intruder.CurrentRoom);
                Assert.That(view.LastDisplayedRoom, Is.EqualTo(state.CurrentSeedIntruder.Intruder.CurrentRoom));
                Assert.That(Vector3.Distance(visualRoot.transform.position, targetAnchor.position), Is.LessThan(0.001f));

                state.NeutralizeActiveSeedIntruderForValidation();
                view.RefreshView();

                Assert.That(view.IsViewActive, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(anchorsObject);
                Object.DestroyImmediate(viewObject);
                Object.DestroyImmediate(stateObject);
            }
        }

        private static GameSessionState CreateFollowUpTransportSession()
        {
            var tutorialContract = TransportContractDefinition.CreateTutorial();
            var tutorialSession = GameSessionState.StartAssociationSession().StartTransport(tutorialContract);
            var completedSession = tutorialSession.CompleteTransport(new SettlementInput(
                tutorialContract.ContractType,
                tutorialContract.Difficulty,
                tutorialContract.Cargo,
                tutorialSession.Ship,
                new CrewState(1, 0),
                tutorialSession.Wallet,
                contractBasePay: tutorialContract.RewardCredits,
                repairSupportAmount: 100));

            return completedSession.StartTransport(TransportContractDefinition.CreateAssociationFollowUp());
        }

        private static Transform CreateAnchor(Transform parent, ShipRoomId roomId, Vector3 position)
        {
            var anchor = new GameObject("Anchor - " + roomId);
            anchor.transform.SetParent(parent);
            anchor.transform.position = position;
            return anchor.transform;
        }
    }
}
