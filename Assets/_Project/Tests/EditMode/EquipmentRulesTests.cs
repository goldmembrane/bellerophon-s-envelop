using Bellerophon.Core.Session;
using Bellerophon.Core.Player;
using Bellerophon.Core.Ship;
using NUnit.Framework;
using UnityEngine;

namespace Bellerophon.Tests.EditMode
{
    public sealed class EquipmentRulesTests
    {
        [Test]
        public void DefaultAssociationEquipment_HasBasicSuitStickAndThreeSupplySlots()
        {
            var equipment = PlayerEquipmentState.CreateDefaultAssociationIssue();

            Assert.That(equipment.HasBasicProtectiveSuit, Is.True);
            Assert.That(equipment.GetHandSlot(0).ItemKind, Is.EqualTo(EquipmentItemKind.Stick));
            Assert.That(equipment.GetHandSlot(1).IsEmpty, Is.True);
            Assert.That(equipment.GetSupplySlot(0).IsEmpty, Is.True);
            Assert.That(equipment.GetSupplySlot(1).IsEmpty, Is.True);
            Assert.That(equipment.GetSupplySlot(2).IsEmpty, Is.True);
        }

        [Test]
        public void WeaponDefinitions_UseConfirmedStickAndMusketStats()
        {
            var stick = EquipmentRules.GetDefinition(EquipmentItemKind.Stick);
            var musket = EquipmentRules.GetDefinition(EquipmentItemKind.Musket);

            Assert.That(stick.Damage, Is.EqualTo(30));
            Assert.That(stick.MinRange, Is.EqualTo(2f));
            Assert.That(stick.MaxRange, Is.EqualTo(3f));
            Assert.That(stick.UseDelaySeconds, Is.EqualTo(2.5f));
            Assert.That(stick.HasThrowMode, Is.True);

            Assert.That(musket.Damage, Is.EqualTo(50));
            Assert.That(musket.MinRange, Is.EqualTo(5f));
            Assert.That(musket.MaxRange, Is.EqualTo(7f));
            Assert.That(musket.UseDelaySeconds, Is.EqualTo(3.5f));
            Assert.That(musket.HasPrecisionAimMode, Is.True);
            Assert.That(musket.HasReloadInputSkeleton, Is.True);
            Assert.That(musket.HasConfirmedMagazineSpec, Is.False);
        }

        [Test]
        public void UseActiveEquipment_StickDamagesAndCanNeutralizeParvumAfterCooldown()
        {
            var stateObject = new GameObject("Ship Device State Test");
            try
            {
                var state = stateObject.AddComponent<ShipDeviceInteractionState>();
                state.SetEquipmentState(PlayerEquipmentState.CreateDefaultAssociationIssue());
                state.StartTransportRun(60);
                state.StartSeedIntruderForValidation(
                    SeedIntruderRules.CreateParvumIntrusionForSeed(47, ShipRoomId.Cockpit));

                var firstHit = state.UseActiveEquipment(false);
                Assert.That(firstHit.Outcome, Is.EqualTo(EquipmentUseOutcome.MeleeHit));
                Assert.That(firstHit.Damage, Is.EqualTo(EquipmentRules.StickDamage));
                Assert.That(state.CurrentSeedIntruder.Intruder.CurrentHealth, Is.EqualTo(25));

                var blocked = state.UseActiveEquipment(false);
                Assert.That(blocked.Outcome, Is.EqualTo(EquipmentUseOutcome.CooldownBlocked));

                state.TickEquipmentState(EquipmentRules.StickUseDelaySeconds);
                var secondHit = state.UseActiveEquipment(false);

                Assert.That(secondHit.Outcome, Is.EqualTo(EquipmentUseOutcome.MeleeHit));
                Assert.That(state.CurrentSeedIntruder.IsResolved, Is.True);
                Assert.That(state.CurrentSeedIntruder.Intruder.Resolution, Is.EqualTo(IntruderResolution.Neutralized));
            }
            finally
            {
                Object.DestroyImmediate(stateObject);
            }
        }

        [Test]
        public void PurchaseMusket_AddsToSecondHandSlotAndReloadRemainsSkeleton()
        {
            var tutorial = TransportContractDefinition.CreateTutorial();
            var tutorialSession = GameSessionState.StartAssociationSession().StartTransport(tutorial);
            var completed = tutorialSession.CompleteTransport(new SettlementInput(
                tutorial.ContractType,
                tutorial.Difficulty,
                tutorial.Cargo,
                tutorialSession.Ship,
                new CrewState(1, 0),
                tutorialSession.Wallet,
                contractBasePay: tutorial.RewardCredits,
                repairSupportAmount: 100));

            var purchased = completed.PurchaseEquipment(EquipmentItemKind.Musket);
            var reload = EquipmentRules.ReloadActiveEquipment(purchased.Equipment);

            Assert.That(purchased.Wallet.Credits, Is.EqualTo(650));
            Assert.That(purchased.Equipment.GetHandSlot(0).ItemKind, Is.EqualTo(EquipmentItemKind.Stick));
            Assert.That(purchased.Equipment.GetHandSlot(1).ItemKind, Is.EqualTo(EquipmentItemKind.Musket));
            Assert.That(purchased.Equipment.ActiveHandSlotIndex, Is.EqualTo(1));
            Assert.That(reload.Outcome, Is.EqualTo(EquipmentUseOutcome.ReloadSkeleton));
            Assert.That(EquipmentRules.GetDefinition(EquipmentItemKind.Musket).HasConfirmedMagazineSpec, Is.False);
        }

        [Test]
        public void EquipmentController_RightClickAlternateModeTogglesInsteadOfHolding()
        {
            var stateObject = new GameObject("Ship Device State Test");
            var controllerObject = new GameObject("Equipment Controller Test");
            try
            {
                var state = stateObject.AddComponent<ShipDeviceInteractionState>();
                var equipment = PlayerEquipmentState.CreateDefaultAssociationIssue()
                    .WithHandSlot(1, EquipmentSlotState.One(EquipmentItemKind.Musket))
                    .WithActiveHandSlot(1);
                state.SetEquipmentStateForValidation(equipment);

                var controller = controllerObject.AddComponent<PlayerEquipmentController>();
                controller.Configure(null, null, state, null, null);

                controller.ToggleAlternateModeForValidation();
                Assert.That(controller.AlternateModeActive, Is.True);
                Assert.That(state.CurrentEquipmentState.ActiveMode, Is.EqualTo(EquipmentUseMode.PrecisionAim));

                controller.ToggleAlternateModeForValidation();
                Assert.That(controller.AlternateModeActive, Is.False);
                Assert.That(state.CurrentEquipmentState.ActiveMode, Is.EqualTo(EquipmentUseMode.Primary));

                state.SetEquipmentStateForValidation(state.CurrentEquipmentState.WithActiveHandSlot(0));
                controller.ToggleAlternateModeForValidation();
                Assert.That(controller.AlternateModeActive, Is.True);
                Assert.That(state.CurrentEquipmentState.ActiveMode, Is.EqualTo(EquipmentUseMode.Throwing));
            }
            finally
            {
                Object.DestroyImmediate(controllerObject);
                Object.DestroyImmediate(stateObject);
            }
        }
    }
}
