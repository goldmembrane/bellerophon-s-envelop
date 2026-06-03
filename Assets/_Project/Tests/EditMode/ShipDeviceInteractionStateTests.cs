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
                state.FireManualTurret();
                state.FireManualTurret();
                var finalShot = state.FireManualTurret();

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
