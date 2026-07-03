using Bellerophon.Core.Session;
using NUnit.Framework;

namespace Bellerophon.Tests.EditMode
{
    public sealed class SpecialContractRulesTests
    {
        [Test]
        public void OfferConditions_UseFamePlanetVisitsKillsAndDifficultyCompletions()
        {
            var progress = SpecialContractProgressState.Empty;
            progress = SpecialContractRules.RecordPlanetVisit(progress, PlanetTrait.OrganicRich);
            progress = SpecialContractRules.RecordPlanetVisit(progress, PlanetTrait.VolcanicActive);
            progress = SpecialContractRules.RecordPlanetVisit(progress, PlanetTrait.VolcanicActive);
            progress = SpecialContractRules.RecordPlanetVisit(progress, PlanetTrait.VolcanicActive);
            progress = SpecialContractRules.RecordPlanetVisit(progress, PlanetTrait.CommonMineralRich);
            progress = SpecialContractRules.RecordPlanetVisit(progress, PlanetTrait.RareMineralRich);
            progress = SpecialContractRules.RecordContractCompletion(progress, ContractDifficulty.VeryHard);
            progress = SpecialContractRules.RecordContractCompletion(progress, ContractDifficulty.VeryHard);
            progress = Record(progress, SpecialContractEnemyKind.Revolution, 3);
            progress = Record(progress, SpecialContractEnemyKind.Monstrum, 1);
            progress = Record(progress, SpecialContractEnemyKind.Dolore, 1);
            progress = Record(progress, SpecialContractEnemyKind.Ata, 1);

            Assert.That(SpecialContractRules.CanOfferContract(
                progress,
                new ReputationState(500, 0, false),
                PlanetTrait.OrganicRich,
                SpecialContractKind.PresenceDetectorUnlock), Is.True);
            Assert.That(SpecialContractRules.CanOfferContract(
                progress,
                new ReputationState(1000, 0, false),
                PlanetTrait.RareMineralRich,
                SpecialContractKind.LightBladeUnlock), Is.True);
            Assert.That(SpecialContractRules.CanOfferContract(
                progress,
                new ReputationState(3000, 0, false),
                PlanetTrait.CommonMineralRich,
                SpecialContractKind.ElectricMineUnlock), Is.True);
            Assert.That(SpecialContractRules.CanOfferContract(
                progress,
                new ReputationState(5000, 0, false),
                PlanetTrait.WaterRich,
                SpecialContractKind.CorridorPurifierUnlock), Is.True);
        }

        [Test]
        public void PresenceDetectorContract_DropsChipsOnlyWhenAcceptedAndUnlocksShopPurchase()
        {
            var reputation = new ReputationState(500, 0, false);
            var progress = SpecialContractRules.RecordPlanetVisit(
                SpecialContractProgressState.Empty,
                PlanetTrait.OrganicRich);
            var inactiveKill = SpecialContractRules.RecordEnemyNeutralized(
                progress,
                SpecialContractEnemyKind.Resistance);
            var accepted = SpecialContractRules.AcceptContract(
                progress,
                reputation,
                PlanetTrait.OrganicRich,
                SpecialContractKind.PresenceDetectorUnlock);

            var state = accepted.State;
            state = SpecialContractRules.RecordEnemyNeutralized(state, SpecialContractEnemyKind.Resistance).State;
            state = SpecialContractRules.RecordEnemyNeutralized(state, SpecialContractEnemyKind.Resistance).State;
            state = SpecialContractRules.RecordEnemyNeutralized(state, SpecialContractEnemyKind.Revolution).State;
            var cargoDrop = new CargoFreedomLeagueDropResult(
                CargoFreedomLeagueDropKind.None,
                0f,
                EquipmentItemKind.None,
                true,
                false);
            var recordedDrop = SpecialContractRules.RecordCargoFreedomLeagueDrop(accepted.State, cargoDrop);
            var settlement = SpecialContractRules.ResolveTransportArrival(state, CreateCargo(), true);
            var lockedPurchase = EquipmentRules.PurchaseItem(PlayerEquipmentState.Empty, EquipmentItemKind.PresenceDetector);
            var unlockedPurchase = EquipmentRules.PurchaseItem(
                PlayerEquipmentState.Empty,
                EquipmentItemKind.PresenceDetector,
                settlement.State.EquipmentUnlocks);

            Assert.That(inactiveKill.ObjectiveItemDropped, Is.False);
            Assert.That(accepted.Accepted, Is.True);
            Assert.That(SpecialContractRules.ShouldRequestCargoFreedomLeagueSpecialChipDrop(
                accepted.State,
                CargoFreedomLeagueKind.Resistance), Is.True);
            Assert.That(recordedDrop.ResistanceChipCount, Is.EqualTo(1));
            Assert.That(settlement.Completed, Is.True);
            Assert.That(settlement.BonusCredits, Is.EqualTo(2000));
            Assert.That(settlement.State.EquipmentUnlocks.PresenceDetectorUnlocked, Is.True);
            Assert.That(lockedPurchase.Purchased, Is.False);
            Assert.That(unlockedPurchase.Purchased, Is.True);
            Assert.That(unlockedPurchase.SpentCredits, Is.EqualTo(EquipmentRules.GetDefinition(EquipmentItemKind.PresenceDetector).PriceCredits));
        }

        [Test]
        public void LightBladeContract_RequiresPiratePartsAndGrantsReward()
        {
            var progress = CreateLightBladeOfferProgress();
            var accepted = SpecialContractRules.AcceptContract(
                progress,
                new ReputationState(1000, 0, false),
                PlanetTrait.RareMineralRich,
                SpecialContractKind.LightBladeUnlock);
            var istante = SpecialContractRules.RecordEnemyNeutralized(
                accepted.State,
                SpecialContractEnemyKind.Istante);
            var ata = SpecialContractRules.RecordEnemyNeutralized(
                istante.State,
                SpecialContractEnemyKind.Ata);
            var settlement = SpecialContractRules.ResolveTransportArrival(ata.State, CreateCargo(), true);
            var granted = EquipmentRules.GrantItem(PlayerEquipmentState.Empty, settlement.GrantedItemKind);
            var hit = EquipmentRules.UseActiveEquipment(granted.State, false, true);

            Assert.That(accepted.Accepted, Is.True);
            Assert.That(istante.ObjectiveItemKind, Is.EqualTo(SpecialContractObjectiveItemKind.IstantePowerCore));
            Assert.That(ata.ObjectiveItemKind, Is.EqualTo(SpecialContractObjectiveItemKind.AtaControlModule));
            Assert.That(settlement.Completed, Is.True);
            Assert.That(settlement.BonusCredits, Is.EqualTo(2500));
            Assert.That(settlement.State.EquipmentUnlocks.LightBladeUnlocked, Is.True);
            Assert.That(granted.State.HasAnyItem(EquipmentItemKind.LightBlade), Is.True);
            Assert.That(hit.Damage, Is.EqualTo(EquipmentRules.LightBladeDamage));
        }

        [Test]
        public void ElectricMineContract_UsesHalfCargoAndFailsBelowDurability()
        {
            var active = CreateElectricMineOfferProgress()
                .WithActiveContract(SpecialContractKind.ElectricMineUnlock);
            var weakCargo = CreateCargo(
                SpecialContractRules.ElectricMineSpecialCargoMinimumSizeUnits,
                SpecialContractRules.ElectricMineSpecialCargoMinimumDurability - 0.01f);
            var intactCargo = CreateCargo(
                SpecialContractRules.ElectricMineSpecialCargoMinimumSizeUnits,
                SpecialContractRules.ElectricMineSpecialCargoMinimumDurability);

            var failure = SpecialContractRules.ResolveTransportArrival(active, weakCargo, true);
            var success = SpecialContractRules.ResolveTransportArrival(active, intactCargo, true);

            Assert.That(SpecialContractRules.ShouldForceSpecialCargoPriority(active), Is.True);
            Assert.That(SpecialContractRules.ElectricMineSpecialCargoMinimumSizeUnits, Is.EqualTo(PersonalCargoRules.FullCargoHoldCapacityUnits / 2));
            Assert.That(failure.Failed, Is.True);
            Assert.That(failure.State.HasActiveContract, Is.False);
            Assert.That(failure.State.RevolutionNeutralizedCount, Is.Zero);
            Assert.That(success.Completed, Is.True);
            Assert.That(success.State.EquipmentUnlocks.ElectricMineUnlocked, Is.True);
            Assert.That(success.BonusCredits, Is.EqualTo(3000));
        }

        [Test]
        public void CorridorPurifierContract_UsesFourMinuteFortyFourRouteAndInstallsReward()
        {
            var active = CreateCorridorPurifierOfferProgress()
                .WithActiveContract(SpecialContractKind.CorridorPurifierUnlock);
            var route = SpecialContractRules.CreateRouteModifier(active);
            var settlement = SpecialContractRules.ResolveTransportArrival(active, CreateCargo(), true);
            var granted = EquipmentRules.GrantItem(PlayerEquipmentState.Empty, settlement.GrantedItemKind);

            Assert.That(route.ForcesAllIntrusionHazards, Is.True);
            Assert.That(route.IntrusionOccurrenceMultiplier, Is.EqualTo(3));
            Assert.That(route.FixedDurationSeconds, Is.EqualTo(284));
            Assert.That(settlement.Completed, Is.True);
            Assert.That(settlement.BonusCredits, Is.EqualTo(7500));
            Assert.That(settlement.State.CorridorPurifierInstalled, Is.True);
            Assert.That(settlement.State.CorridorPurifierChargeCount, Is.EqualTo(1));
            Assert.That(settlement.State.EquipmentUnlocks.CorridorPurifierUnlocked, Is.True);
            Assert.That(granted.State.HasAnyItem(EquipmentItemKind.CorridorPurifier), Is.True);
        }

        [Test]
        public void GameSession_CompleteTransportAutoAppliesSpecialBonusUnlockAndReward()
        {
            var progress = CreateLightBladeOfferProgress();
            progress = SpecialContractRules.AcceptContract(
                progress,
                new ReputationState(1000, 0, false),
                PlanetTrait.RareMineralRich,
                SpecialContractKind.LightBladeUnlock).State;
            progress = SpecialContractRules.RecordEnemyNeutralized(progress, SpecialContractEnemyKind.Istante).State;
            progress = SpecialContractRules.RecordEnemyNeutralized(progress, SpecialContractEnemyKind.Ata).State;

            var contract = new TransportContractDefinition(
                "special-light-blade-test",
                "Special Light Blade Test",
                "Recovered Pirate Components",
                ContractType.Special,
                ContractDifficulty.Master,
                60,
                0,
                CreateCargo(),
                false,
                originTrait: PlanetTrait.RareMineralRich,
                destinationTrait: PlanetTrait.RareMineralRich);
            var started = GameSessionState.StartSession(new WalletState(0, false))
                .WithReputation(new ReputationState(1000, 0, false))
                .WithSpecialContracts(progress)
                .StartTransport(contract);

            var completed = started.CompleteTransport(new SettlementInput(
                contract.ContractType,
                contract.Difficulty,
                contract.Cargo,
                started.Ship,
                new CrewState(1, 0),
                started.Wallet,
                contractBasePay: contract.RewardCredits));

            Assert.That(completed.Wallet.Credits, Is.EqualTo(2500));
            Assert.That(completed.SpecialContracts.HasActiveContract, Is.False);
            Assert.That(completed.SpecialContracts.EquipmentUnlocks.LightBladeUnlocked, Is.True);
            Assert.That(completed.Equipment.HasAnyItem(EquipmentItemKind.LightBlade), Is.True);
            Assert.That(completed.Equipment.GetHandSlot(0).PurchasePriceCredits, Is.Zero);
        }

        private static SpecialContractProgressState CreateLightBladeOfferProgress()
        {
            var progress = SpecialContractProgressState.Empty;
            progress = SpecialContractRules.RecordPlanetVisit(progress, PlanetTrait.VolcanicActive);
            progress = SpecialContractRules.RecordPlanetVisit(progress, PlanetTrait.VolcanicActive);
            progress = SpecialContractRules.RecordPlanetVisit(progress, PlanetTrait.VolcanicActive);
            progress = SpecialContractRules.RecordContractCompletion(progress, ContractDifficulty.VeryHard);
            return SpecialContractRules.RecordContractCompletion(progress, ContractDifficulty.VeryHard);
        }

        private static SpecialContractProgressState CreateElectricMineOfferProgress()
        {
            var progress = SpecialContractProgressState.Empty;
            progress = SpecialContractRules.RecordPlanetVisit(progress, PlanetTrait.CommonMineralRich);
            progress = SpecialContractRules.RecordPlanetVisit(progress, PlanetTrait.RareMineralRich);
            return Record(progress, SpecialContractEnemyKind.Revolution, 3);
        }

        private static SpecialContractProgressState CreateCorridorPurifierOfferProgress()
        {
            var progress = SpecialContractProgressState.Empty;
            progress = Record(progress, SpecialContractEnemyKind.Monstrum, 1);
            progress = Record(progress, SpecialContractEnemyKind.Dolore, 1);
            progress = Record(progress, SpecialContractEnemyKind.Revolution, 1);
            return Record(progress, SpecialContractEnemyKind.Ata, 1);
        }

        private static SpecialContractProgressState Record(
            SpecialContractProgressState progress,
            SpecialContractEnemyKind enemyKind,
            int count)
        {
            var next = progress;
            for (var i = 0; i < count; i++)
            {
                next = SpecialContractRules.RecordEnemyNeutralized(next, enemyKind).State;
            }

            return next;
        }

        private static CargoState CreateCargo(
            int sizeUnits = 1,
            float durabilityPercent = 1f)
        {
            return new CargoState(CargoGrade.Premium, sizeUnits, 0, durabilityPercent, false);
        }
    }
}
