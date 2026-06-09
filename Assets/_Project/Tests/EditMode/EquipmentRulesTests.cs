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
            Assert.That(equipment.UnlockedHandSlotCount, Is.EqualTo(3));
            Assert.That(equipment.UnlockedSupplySlotCount, Is.EqualTo(3));
            Assert.That(equipment.GetHandSlot(0).ItemKind, Is.EqualTo(EquipmentItemKind.Stick));
            Assert.That(equipment.GetHandSlot(1).IsEmpty, Is.True);
            Assert.That(equipment.GetHandSlot(2).IsEmpty, Is.True);
            Assert.That(equipment.GetSupplySlot(0).IsEmpty, Is.True);
            Assert.That(equipment.GetSupplySlot(1).IsEmpty, Is.True);
            Assert.That(equipment.GetSupplySlot(2).IsEmpty, Is.True);
        }

        [Test]
        public void PouchUpgrade_IncreasesHandSlotsFromThreeToFour()
        {
            var equipment = PlayerEquipmentState.CreateDefaultAssociationIssue();
            var upgraded = equipment.WithPouchUpgrade(true);

            Assert.That(equipment.UnlockedHandSlotCount, Is.EqualTo(PlayerEquipmentState.DefaultHandSlotCount));
            Assert.That(upgraded.UnlockedHandSlotCount, Is.EqualTo(PlayerEquipmentState.UpgradedHandSlotCount));
            Assert.That(upgraded.GetHandSlot(3).IsEmpty, Is.True);
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
        public void Step8WeaponDefinitions_FollowSourceDamageRangeDelayAndCost()
        {
            var shotgun = EquipmentRules.GetDefinition(EquipmentItemKind.Shotgun);
            var miniFlamethrower = EquipmentRules.GetDefinition(EquipmentItemKind.MiniFlamethrower);
            var electricBaton = EquipmentRules.GetDefinition(EquipmentItemKind.ElectricBaton);
            var dagger = EquipmentRules.GetDefinition(EquipmentItemKind.Dagger);

            Assert.That(shotgun.Damage, Is.EqualTo(70));
            Assert.That(shotgun.MinRange, Is.EqualTo(1.5f));
            Assert.That(shotgun.MaxRange, Is.EqualTo(4f));
            Assert.That(shotgun.UseDelaySeconds, Is.EqualTo(3f));
            Assert.That(shotgun.PriceCredits, Is.EqualTo(600));
            Assert.That(shotgun.HasReloadInputSkeleton, Is.True);
            Assert.That(shotgun.HasConfirmedMagazineSpec, Is.True);

            Assert.That(miniFlamethrower.Damage, Is.EqualTo(4));
            Assert.That(miniFlamethrower.MinRange, Is.EqualTo(1f));
            Assert.That(miniFlamethrower.MaxRange, Is.EqualTo(3f));
            Assert.That(miniFlamethrower.PriceCredits, Is.EqualTo(800));

            Assert.That(electricBaton.Damage, Is.EqualTo(25));
            Assert.That(electricBaton.MinRange, Is.EqualTo(1f));
            Assert.That(electricBaton.MaxRange, Is.EqualTo(1.5f));
            Assert.That(electricBaton.UseDelaySeconds, Is.EqualTo(2.5f));
            Assert.That(electricBaton.PriceCredits, Is.EqualTo(500));

            Assert.That(dagger.Damage, Is.EqualTo(15));
            Assert.That(dagger.MinRange, Is.EqualTo(1f));
            Assert.That(dagger.MaxRange, Is.EqualTo(1f));
            Assert.That(dagger.UseDelaySeconds, Is.EqualTo(2f));
            Assert.That(dagger.PriceCredits, Is.EqualTo(150));
            Assert.That(dagger.HasThrowMode, Is.True);
        }

        [Test]
        public void Step8SupplyItems_ApplyProtectionTreatmentAndMeleeEnhancement()
        {
            var equipment = PlayerEquipmentState.CreateDefaultAssociationIssue()
                .WithSupplySlot(0, EquipmentSlotState.Purchased(EquipmentItemKind.ProtectiveSuit, 400))
                .WithSupplySlot(1, EquipmentSlotState.Purchased(EquipmentItemKind.InjuryReliever, EquipmentRules.InjuryRelieverPriceCredits))
                .WithSupplySlot(2, EquipmentSlotState.Purchased(EquipmentItemKind.StrengthEnhancer, 100));

            var protection = EquipmentRules.UseSupplyItem(equipment, 0);
            var treatment = EquipmentRules.UseSupplyItem(protection.State, 1);
            var strength = EquipmentRules.UseSupplyItem(treatment.State, 2);
            var stickHit = EquipmentRules.UseActiveEquipment(strength.State.WithActiveHandSlot(0), false, true);
            var musketState = strength.State
                .WithHandSlot(1, EquipmentSlotState.One(EquipmentItemKind.Musket))
                .WithActiveHandSlot(1);
            var musketHit = EquipmentRules.UseActiveEquipment(musketState, false, true);
            var expired = EquipmentRules.Tick(strength.State, EquipmentRules.StrengthEnhancerDurationSeconds);

            Assert.That(protection.Outcome, Is.EqualTo(EquipmentUseOutcome.ProtectiveEquipped));
            Assert.That(protection.DamageReductionPercent, Is.EqualTo(EquipmentRules.ProtectiveSuitReductionPercent));
            Assert.That(protection.State.ActiveProtectiveItemKind, Is.EqualTo(EquipmentItemKind.ProtectiveSuit));
            Assert.That(protection.State.GetSupplySlot(0).DurabilityPercent, Is.EqualTo(95));
            Assert.That(EquipmentRules.CalculateDamageAfterProtection(50, protection.State), Is.EqualTo(35));

            Assert.That(treatment.Outcome, Is.EqualTo(EquipmentUseOutcome.TreatmentApplied));
            Assert.That(treatment.HealthDelta, Is.EqualTo(EquipmentRules.InjuryRelieverHealAmount));
            Assert.That(treatment.ConsumedItem, Is.True);
            Assert.That(treatment.State.GetSupplySlot(1).IsEmpty, Is.True);

            Assert.That(strength.Outcome, Is.EqualTo(EquipmentUseOutcome.EnhancementApplied));
            Assert.That(strength.State.HasActiveStrengthEnhancer, Is.True);
            Assert.That(strength.State.StrengthDamageBonusPercent, Is.EqualTo(EquipmentRules.StrengthEnhancerDamageBonusPercent));
            Assert.That(strength.State.GetSupplySlot(2).IsEmpty, Is.True);
            Assert.That(stickHit.Damage, Is.EqualTo(42));
            Assert.That(musketHit.Damage, Is.EqualTo(EquipmentRules.MusketDamage));
            Assert.That(expired.HasActiveStrengthEnhancer, Is.False);
        }

        [Test]
        public void Step8Flashlight_ActivatesTimedUtilityState()
        {
            var equipment = PlayerEquipmentState.CreateDefaultAssociationIssue()
                .WithHandSlot(1, EquipmentSlotState.Purchased(EquipmentItemKind.Flashlight, EquipmentRules.FlashlightPriceCredits))
                .WithActiveHandSlot(1);

            var activated = EquipmentRules.UseActiveEquipment(equipment, false, false);
            var expired = EquipmentRules.Tick(activated.State, EquipmentRules.FlashlightDurationSeconds);

            Assert.That(activated.Outcome, Is.EqualTo(EquipmentUseOutcome.UtilityActivated));
            Assert.That(activated.State.HasActiveFlashlight, Is.True);
            Assert.That(activated.State.GetHandSlot(1).DurabilityPercent, Is.EqualTo(99));
            Assert.That(expired.HasActiveFlashlight, Is.False);
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
        public void PurchaseRules_UseThirdHandSlotSupplyStorageAndSelectedDisposal()
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

            var musket = completed.PurchaseEquipment(EquipmentItemKind.Musket);
            var flashlight = musket.PurchaseEquipment(EquipmentItemKind.Flashlight);
            var aid = flashlight.PurchaseEquipment(EquipmentItemKind.InjuryReliever);
            var disposal = aid.DisposePurchasedSupplyEquipment(0);

            Assert.That(flashlight.Equipment.GetHandSlot(2).ItemKind, Is.EqualTo(EquipmentItemKind.Flashlight));
            Assert.That(aid.Equipment.GetSupplySlot(0).ItemKind, Is.EqualTo(EquipmentItemKind.InjuryReliever));
            Assert.That(aid.Equipment.GetSupplySlot(0).PurchasePriceCredits, Is.EqualTo(EquipmentRules.InjuryRelieverPriceCredits));
            Assert.That(disposal.Disposed, Is.True);
            Assert.That(disposal.ItemKind, Is.EqualTo(EquipmentItemKind.InjuryReliever));
            Assert.That(disposal.ReceivedCredits, Is.EqualTo(1));
            Assert.That(disposal.State.Wallet.Credits, Is.EqualTo(501));
            Assert.That(disposal.State.Equipment.GetHandSlot(1).ItemKind, Is.EqualTo(EquipmentItemKind.Musket));
            Assert.That(disposal.State.Equipment.GetSupplySlot(0).IsEmpty, Is.True);
        }

        [Test]
        public void SupplySlotUpgrade_ExpandsEquipmentStorageCapacity()
        {
            var wallet = new WalletState(3000, false);
            var completed = GameSessionState.StartSession(wallet)
                .StartTransport()
                .CompleteTransport(new SettlementInput(
                    ContractType.Association,
                    ContractDifficulty.Normal,
                    new CargoState(CargoGrade.Common, 1, 100, 1f, false),
                    ShipState.CreateDefault(),
                    new CrewState(1, 0),
                    wallet));

            var purchased = completed.PurchaseShipUpgrade(ShipUpgradeCategory.SupplySlots);
            var equipped = purchased.State.EquipShipUpgrade(ShipUpgradeCategory.SupplySlots);

            Assert.That(purchased.State.Equipment.UnlockedSupplySlotCount, Is.EqualTo(3));
            Assert.That(equipped.State.Equipment.UnlockedSupplySlotCount, Is.EqualTo(5));
        }

        [Test]
        public void Catalogs_FilterCommonFameAndSpecialShopProducts()
        {
            var catalog = EquipmentRules.CreatePhase15BuyCatalog();

            Assert.That(EquipmentRules.FilterCatalogByAvailability(catalog, EquipmentAvailability.CommonShop).Length, Is.GreaterThan(10));
            Assert.That(EquipmentRules.FilterCatalogByAvailability(catalog, EquipmentAvailability.FameRestrictedShop).Length, Is.GreaterThan(0));
            Assert.That(EquipmentRules.FilterCatalogByAvailability(catalog, EquipmentAvailability.SpecialUnlock).Length, Is.EqualTo(4));
            Assert.That(EquipmentRules.FilterCatalogByCategory(catalog, EquipmentItemCategory.Treatment).Length, Is.GreaterThan(0));
        }

        [Test]
        public void SpecialUnlockEquipment_RequiresContractUnlockBeforePurchase()
        {
            var lightBlade = EquipmentRules.GetDefinition(EquipmentItemKind.LightBlade);
            var electricMine = EquipmentRules.GetDefinition(EquipmentItemKind.ElectricMine);
            var corridorPurifier = EquipmentRules.GetDefinition(EquipmentItemKind.CorridorPurifier);
            var locked = EquipmentRules.PurchaseItem(PlayerEquipmentState.Empty, EquipmentItemKind.LightBlade);
            var unlocks = SpecialEquipmentUnlockState.None.WithUnlocked(EquipmentItemKind.LightBlade);
            var unlocked = EquipmentRules.PurchaseItem(
                PlayerEquipmentState.Empty,
                EquipmentItemKind.LightBlade,
                unlocks);

            Assert.That(lightBlade.Availability, Is.EqualTo(EquipmentAvailability.SpecialUnlock));
            Assert.That(lightBlade.Damage, Is.EqualTo(EquipmentRules.LightBladeDamage));
            Assert.That(lightBlade.PriceCredits, Is.EqualTo(1000));
            Assert.That(electricMine.Availability, Is.EqualTo(EquipmentAvailability.SpecialUnlock));
            Assert.That(electricMine.MaxStackCount, Is.EqualTo(2));
            Assert.That(corridorPurifier.Availability, Is.EqualTo(EquipmentAvailability.SpecialUnlock));
            Assert.That(corridorPurifier.MaxStackCount, Is.EqualTo(2));
            Assert.That(locked.Purchased, Is.False);
            Assert.That(unlocked.Purchased, Is.True);
            Assert.That(unlocked.State.HasAnyItem(EquipmentItemKind.LightBlade), Is.True);
        }

        [Test]
        public void EquipmentDurabilityDamage_ReducesDurabilityAndBreaksAtZero()
        {
            var slot = EquipmentSlotState.Purchased(EquipmentItemKind.Flashlight, EquipmentRules.FlashlightPriceCredits);

            var damaged = EquipmentRules.ApplyDurabilityDamage(slot, 35);
            var broken = EquipmentRules.ApplyDurabilityDamage(damaged, 65);

            Assert.That(damaged.DurabilityPercent, Is.EqualTo(65));
            Assert.That(broken.IsEmpty, Is.True);
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

        [Test]
        public void EquipmentController_HandInventorySelectionStaysSyncedWithPurchasedWeapon()
        {
            var stateObject = new GameObject("Ship Device State Test");
            var inventoryObject = new GameObject("Hand Inventory Test");
            var controllerObject = new GameObject("Equipment Controller Test");
            try
            {
                var state = stateObject.AddComponent<ShipDeviceInteractionState>();
                var equipment = PlayerEquipmentState.CreateDefaultAssociationIssue()
                    .WithHandSlot(1, EquipmentSlotState.Purchased(EquipmentItemKind.Musket, EquipmentRules.MusketPriceCredits))
                    .WithActiveHandSlot(1);
                state.SetEquipmentStateForValidation(equipment);

                var inventory = inventoryObject.AddComponent<FirstPersonHandInventory>();
                var controller = controllerObject.AddComponent<PlayerEquipmentController>();
                controller.Configure(inventory, null, state, null, null);

                Assert.That(inventory.ActiveSlotIndex, Is.EqualTo(1));
                Assert.That(state.CurrentEquipmentState.ActiveHandSlotIndex, Is.EqualTo(1));

                inventory.SelectSlotForValidation(0);
                Assert.That(state.CurrentEquipmentState.ActiveHandSlotIndex, Is.EqualTo(0));

                inventory.SelectSlotForValidation(1);
                Assert.That(state.CurrentEquipmentState.ActiveHandSlotIndex, Is.EqualTo(1));
                Assert.That(state.UseActiveEquipment(false).ItemKind, Is.EqualTo(EquipmentItemKind.Musket));
            }
            finally
            {
                Object.DestroyImmediate(controllerObject);
                Object.DestroyImmediate(inventoryObject);
                Object.DestroyImmediate(stateObject);
            }
        }
    }
}
