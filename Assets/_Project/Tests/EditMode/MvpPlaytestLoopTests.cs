using Bellerophon.Core.Coop;
using Bellerophon.Core.Session;
using Bellerophon.Core.Ship;
using NUnit.Framework;
using UnityEngine;

namespace Bellerophon.Tests.EditMode
{
    public sealed class MvpPlaytestLoopTests
    {
        [Test]
        public void MvpLoop_StartsFromAssociationContractAndReachesTutorialTransport()
        {
            var flow = NewGameStartFlowState.CreateNewGame();

            var association = flow.AcceptAssociationContract();
            var tutorial = association.AcceptTutorialContract();

            Assert.That(flow.Phase, Is.EqualTo(NewGameStartFlowPhase.ContractPrompt));
            Assert.That(association.Phase, Is.EqualTo(NewGameStartFlowPhase.AssociationPlanet));
            Assert.That(association.Session.IsAssociationMember, Is.True);
            Assert.That(association.Session.Equipment.HasBasicProtectiveSuit, Is.True);
            Assert.That(association.Session.Equipment.GetHandSlot(0).ItemKind, Is.EqualTo(EquipmentItemKind.Stick));
            Assert.That(association.AvailableContractCount, Is.EqualTo(1));
            Assert.That(tutorial.Phase, Is.EqualTo(NewGameStartFlowPhase.TutorialContractAccepted));
            Assert.That(tutorial.Session.Phase, Is.EqualTo(GameSessionPhase.Transporting));
            Assert.That(tutorial.Session.ActiveTransportContract.Value.IsTutorial, Is.True);
            Assert.That(tutorial.Session.ActiveCargo.HasValue, Is.True);
        }

        [Test]
        public void MvpLoop_TutorialSettlementRepairAndFollowUpContractStayConnected()
        {
            var tutorialFlow = NewGameStartFlowState.CreateNewGame()
                .AcceptAssociationContract()
                .AcceptTutorialContract();
            var damagedShip = ShipState.CreateDefault()
                .WithRoom(ShipRoomId.CargoHold, new ShipRoomState(20, 100));
            var repairCost = ShipStateRules.CalculateRepairCost(damagedShip);

            var completed = tutorialFlow.Session.CompleteTransport(new SettlementInput(
                ContractType.Association,
                ContractDifficulty.Intro,
                tutorialFlow.Session.ActiveCargo.Value,
                damagedShip,
                new CrewState(1, 0),
                tutorialFlow.Session.Wallet,
                repairCost: repairCost,
                contractBasePay: tutorialFlow.Session.ActiveTransportContract.Value.RewardCredits,
                repairSupportAmount: 100));
            var repaired = completed.ApplyMaintenanceRepair(completed.SettlementResult.PendingRepairCost);
            var postTransportFlow = tutorialFlow
                .WithSession(repaired)
                .PreparePostTransportContracts();
            var followUp = postTransportFlow.GetAvailableContract(0);
            var nextRun = repaired.StartTransport(followUp);

            Assert.That(completed.Phase, Is.EqualTo(GameSessionPhase.Completed));
            Assert.That(completed.CompletedTransportCount, Is.EqualTo(1));
            Assert.That(completed.Wallet.Credits, Is.EqualTo(1100));
            Assert.That(completed.SettlementResult.PendingRepairCost, Is.EqualTo(repairCost));
            Assert.That(repaired.Wallet.Credits, Is.EqualTo(1100 - repairCost));
            Assert.That(repaired.SettlementResult.PendingRepairCost, Is.Zero);
            Assert.That(repaired.Ship.GetRoom(ShipRoomId.CargoHold).CurrentDurability, Is.EqualTo(100));
            Assert.That(postTransportFlow.AvailableContractCount, Is.EqualTo(2));
            Assert.That(nextRun.Phase, Is.EqualTo(GameSessionPhase.Transporting));
            Assert.That(nextRun.ActiveTransportContract.Value.Id, Is.EqualTo("association-local-001"));
        }

        [Test]
        public void MvpLoop_PostTutorialHazardsCanBeAvoidedAndNeutralized()
        {
            var hazard = TransportHazardState.StartAsteroidFieldSmall(0, 12);

            var avoidedHazard = hazard
                .Tick(hazard.DurationSeconds * 0.5f, true)
                .Tick(hazard.DurationSeconds * 0.5f, false);
            var avoided = TransportHazardRules.ResolveAsteroidField(avoidedHazard);
            var target = TransportHazardRules.CreateExternalTarget(hazard);
            var turret = ManualTurretState.Start(true).SetAim(target.PositionX, target.PositionY);
            ManualTurretFireResult shot = default;
            for (var i = 0; i < 20 && !target.IsDestroyed; i++)
            {
                shot = turret.FireAt(target);
                turret = shot.Turret;
                target = shot.Target;
            }

            var neutralized = TransportHazardRules.ResolveAsteroidField(hazard, target.IsDestroyed);

            Assert.That(avoided.Resolution, Is.EqualTo(TransportHazardResolution.Avoided));
            Assert.That(avoided.RoomDamages, Is.Empty);
            Assert.That(shot.Outcome, Is.EqualTo(ManualTurretFireOutcome.Destroyed));
            Assert.That(neutralized.Resolution, Is.EqualTo(TransportHazardResolution.Neutralized));
            Assert.That(TransportHazardRules.ApplyHazardResult(ShipState.CreateDefault(), neutralized).AverageDurabilityPercent, Is.EqualTo(1f));
        }

        [Test]
        public void MvpLoop_ParvumIntruderCanBeResolvedWithIssuedAndPurchasedWeapons()
        {
            var intruder = SeedIntruderRules.CreateParvumIntrusionForSeed(
                7,
                ShipRoomId.Cockpit,
                "mvp-loop-parvum");
            var equipment = PlayerEquipmentState.CreateDefaultAssociationIssue()
                .WithHandSlot(1, EquipmentSlotState.One(EquipmentItemKind.Musket));

            var stickHit = EquipmentRules.UseActiveEquipment(equipment, false, intruder.IsActive);
            intruder = SeedIntruderRules.ApplyDamage(intruder, stickHit.Damage);
            var musketReady = EquipmentRules.Tick(stickHit.State, EquipmentRules.StickUseDelaySeconds)
                .WithActiveHandSlot(1);
            var musketHit = EquipmentRules.UseActiveEquipment(musketReady, false, intruder.IsActive);
            intruder = SeedIntruderRules.ApplyDamage(intruder, musketHit.Damage);

            Assert.That(stickHit.Outcome, Is.EqualTo(EquipmentUseOutcome.MeleeHit));
            Assert.That(musketHit.Outcome, Is.EqualTo(EquipmentUseOutcome.RangedHit));
            Assert.That(intruder.IsResolved, Is.True);
            Assert.That(intruder.Intruder.Resolution, Is.EqualTo(IntruderResolution.Neutralized));
        }

        [Test]
        public void MvpLoop_LocalCoopSnapshotCanObserveFollowUpRunState()
        {
            var authority = LocalCoopSessionAuthority.CreateLocalSimulation(
                GameSessionState.StartAssociationSession());
            var helm = new CoopParticipantId("mvp-loop-helm");
            var remote = new CoopParticipantId("mvp-loop-remote");
            authority.Join(helm);
            authority.Join(remote);

            authority.UpdatePlayerPose(new CoopPlayerPoseState(
                helm,
                0f,
                0f,
                18f,
                35f,
                5f,
                ShipRoomId.Cockpit));
            authority.SubmitInteraction(CoopInteractionRequest.BeginDevice(helm, ShipDeviceType.CockpitHelm));
            authority.SubmitInteraction(CoopInteractionRequest.StartTransportRun(helm, 60));
            authority.ApplyAuthoritativeHazardResult(new TransportHazardResult(
                TransportHazardType.AsteroidField,
                TransportHazardResolution.GlancingHit,
                new[]
                {
                    new ShipRoomHazardDamage(ShipRoomId.Armory, 10)
                }));
            var snapshot = authority.CreateSnapshot(remote);

            Assert.That(snapshot.ParticipantCount, Is.EqualTo(2));
            Assert.That(snapshot.Session.Phase, Is.EqualTo(GameSessionPhase.Transporting));
            Assert.That(snapshot.HasTransportRun, Is.True);
            Assert.That(snapshot.TransportRun.BaseDurationSeconds, Is.EqualTo(60));
            Assert.That(snapshot.Ship.GetRoom(ShipRoomId.Armory).CurrentDurability, Is.EqualTo(90));
            Assert.That(snapshot.TryGetPlayerPose(helm, out var pose), Is.True);
            Assert.That(pose.CurrentRoom, Is.EqualTo(ShipRoomId.Cockpit));
        }

        [Test]
        public void MvpLoop_SettlementArrivalGateAllowsImmediateSecondTransportCompletion()
        {
            var objects = new[]
            {
                new GameObject("mvp-loop-device"),
                new GameObject("mvp-loop-start"),
                new GameObject("mvp-loop-settlement"),
                new GameObject("mvp-loop-settlement-root"),
                new GameObject("mvp-loop-game-over-root")
            };

            try
            {
                var device = objects[0].AddComponent<ShipDeviceInteractionState>();
                var start = objects[1].AddComponent<NewGameStartFlowController>();
                var settlement = objects[2].AddComponent<TransportSettlementController>();
                device.EnsureInitialized();
                start.Configure(null, null, null, null, null, device);
                settlement.Configure(
                    start,
                    device,
                    null,
                    objects[3],
                    null,
                    null,
                    null,
                    objects[4],
                    null,
                    null,
                    null,
                    null);

                start.AcceptAssociationContract();
                start.AcceptTutorialContract();
                device.TickTransportRun(60f);
                settlement.ProcessTransportArrival();

                var firstCompleted = settlement.CurrentSession;
                var nextContract = TransportContractDefinition.CreateAssociationFollowUp();
                var nextSession = firstCompleted.StartTransport(nextContract);
                start.ApplySessionState(nextSession);
                device.SetShipState(nextSession.Ship);
                device.SetCargoState(nextContract.Cargo);
                device.SetEquipmentState(nextSession.Equipment);
                device.StartTransportRun(nextContract.DurationSeconds);
                device.TickTransportRun(nextContract.DurationSeconds);
                settlement.ProcessTransportArrival();

                Assert.That(firstCompleted.Phase, Is.EqualTo(GameSessionPhase.Completed));
                Assert.That(firstCompleted.CompletedTransportCount, Is.EqualTo(1));
                Assert.That(settlement.CurrentSession.Phase, Is.EqualTo(GameSessionPhase.Completed));
                Assert.That(settlement.CurrentSession.CompletedTransportCount, Is.EqualTo(2));
                Assert.That(settlement.SettlementShownCompletedTransportCountForValidation, Is.EqualTo(2));
            }
            finally
            {
                for (var i = 0; i < objects.Length; i++)
                {
                    Object.DestroyImmediate(objects[i]);
                }
            }
        }

        private static GameSessionState CreatePostTutorialTransport()
        {
            var tutorial = NewGameStartFlowState.CreateNewGame()
                .AcceptAssociationContract()
                .AcceptTutorialContract();
            var completed = tutorial.Session.CompleteTransport(new SettlementInput(
                ContractType.Association,
                ContractDifficulty.Intro,
                tutorial.Session.ActiveCargo.Value,
                ShipState.CreateDefault(),
                new CrewState(1, 0),
                tutorial.Session.Wallet,
                contractBasePay: tutorial.Session.ActiveTransportContract.Value.RewardCredits,
                repairSupportAmount: 100));
            return completed.StartTransport(TransportContractDefinition.CreateAssociationFollowUp());
        }
    }
}
