using System;
using Bellerophon.Core.Session;
using UnityEngine;

namespace Bellerophon.Editor.Validation
{
    public static class DetailedStep17SpecialContractsEditorValidation
    {
        public static void Run()
        {
            var summary = BuildValidationSummary();
            Debug.Log("Detailed step 17 special contracts editor validation passed.");
            Debug.Log("Detailed step 17 special contracts validation details: " + summary);
        }

        public static string BuildValidationSummary()
        {
            var definitions = SpecialContractRules.CreateAllDefinitions();
            if (definitions.Length != 4)
            {
                throw new InvalidOperationException("Detailed step 17 must expose four special contract definitions.");
            }

            var presence = SpecialContractRules.AcceptContract(
                SpecialContractRules.RecordPlanetVisit(SpecialContractProgressState.Empty, PlanetTrait.OrganicRich),
                new ReputationState(500, 0, false),
                PlanetTrait.OrganicRich,
                SpecialContractKind.PresenceDetectorUnlock);
            if (!presence.Accepted)
            {
                throw new InvalidOperationException("Presence detector special contract must be offerable after fame and organic planet visit requirements.");
            }

            var presenceState = presence.State;
            presenceState = SpecialContractRules.RecordEnemyNeutralized(presenceState, SpecialContractEnemyKind.Resistance).State;
            presenceState = SpecialContractRules.RecordEnemyNeutralized(presenceState, SpecialContractEnemyKind.Resistance).State;
            presenceState = SpecialContractRules.RecordEnemyNeutralized(presenceState, SpecialContractEnemyKind.Revolution).State;
            var presenceSettlement = SpecialContractRules.ResolveTransportArrival(presenceState, CreateCargo(), true);
            var presencePurchase = EquipmentRules.PurchaseItem(
                PlayerEquipmentState.Empty,
                EquipmentItemKind.PresenceDetector,
                presenceSettlement.State.EquipmentUnlocks);
            if (!presenceSettlement.Completed ||
                presenceSettlement.BonusCredits != SpecialContractRules.PresenceDetectorBonusCredits ||
                !presenceSettlement.State.EquipmentUnlocks.PresenceDetectorUnlocked ||
                !presencePurchase.Purchased)
            {
                throw new InvalidOperationException("Presence detector special contract must unlock shop purchase and pay its bonus.");
            }

            var lightState = CreateLightBladeOfferProgress();
            lightState = SpecialContractRules.AcceptContract(
                lightState,
                new ReputationState(1000, 0, false),
                PlanetTrait.RareMineralRich,
                SpecialContractKind.LightBladeUnlock).State;
            lightState = SpecialContractRules.RecordEnemyNeutralized(lightState, SpecialContractEnemyKind.Istante).State;
            lightState = SpecialContractRules.RecordEnemyNeutralized(lightState, SpecialContractEnemyKind.Ata).State;
            var lightSettlement = SpecialContractRules.ResolveTransportArrival(lightState, CreateCargo(), true);
            var lightGrant = EquipmentRules.GrantItem(PlayerEquipmentState.Empty, lightSettlement.GrantedItemKind);
            if (!lightSettlement.Completed ||
                lightSettlement.BonusCredits != SpecialContractRules.LightBladeBonusCredits ||
                !lightSettlement.State.EquipmentUnlocks.LightBladeUnlocked ||
                !lightGrant.State.HasAnyItem(EquipmentItemKind.LightBlade))
            {
                throw new InvalidOperationException("Light blade special contract must require pirate parts, unlock, grant the item, and pay its bonus.");
            }

            var electricActive = CreateElectricMineOfferProgress().WithActiveContract(SpecialContractKind.ElectricMineUnlock);
            var electricFailure = SpecialContractRules.ResolveTransportArrival(
                electricActive,
                CreateCargo(
                    SpecialContractRules.ElectricMineSpecialCargoMinimumSizeUnits,
                    SpecialContractRules.ElectricMineSpecialCargoMinimumDurability - 0.01f),
                true);
            var electricSuccess = SpecialContractRules.ResolveTransportArrival(
                electricActive,
                CreateCargo(
                    SpecialContractRules.ElectricMineSpecialCargoMinimumSizeUnits,
                    SpecialContractRules.ElectricMineSpecialCargoMinimumDurability),
                true);
            if (!SpecialContractRules.ShouldForceSpecialCargoPriority(electricActive) ||
                !electricFailure.Failed ||
                !electricSuccess.Completed ||
                !electricSuccess.State.EquipmentUnlocks.ElectricMineUnlocked)
            {
                throw new InvalidOperationException("Electric mine special contract must force cargo priority and require half-hold cargo durability >=55%.");
            }

            var corridorActive = CreateCorridorPurifierOfferProgress().WithActiveContract(SpecialContractKind.CorridorPurifierUnlock);
            var route = SpecialContractRules.CreateRouteModifier(corridorActive);
            var corridorSettlement = SpecialContractRules.ResolveTransportArrival(corridorActive, CreateCargo(), true);
            if (!route.ForcesAllIntrusionHazards ||
                route.FixedDurationSeconds != SpecialContractRules.CorridorPurifierRouteDurationSeconds ||
                route.IntrusionOccurrenceMultiplier != SpecialContractRules.CorridorPurifierIntrusionOccurrenceMultiplier ||
                !corridorSettlement.State.CorridorPurifierInstalled ||
                corridorSettlement.State.CorridorPurifierChargeCount != SpecialContractRules.CorridorPurifierRewardChargeCount)
            {
                throw new InvalidOperationException("Corridor purifier special contract must set the fixed hostile route and install the reward.");
            }

            var sessionCompleted = CompleteLightBladeSession(lightState);
            if (sessionCompleted.Wallet.Credits != SpecialContractRules.LightBladeBonusCredits ||
                !sessionCompleted.SpecialContracts.EquipmentUnlocks.LightBladeUnlocked ||
                !sessionCompleted.Equipment.HasAnyItem(EquipmentItemKind.LightBlade))
            {
                throw new InvalidOperationException("Special contract settlement must auto-apply bonus, unlock, and reward item on arrival.");
            }

            return "Definitions=" + definitions.Length +
                   "; PresenceBonus=" + presenceSettlement.BonusCredits +
                   "; LightBlade=" + lightGrant.State.HasAnyItem(EquipmentItemKind.LightBlade) +
                   "; ElectricFail=" + electricFailure.Failed +
                   "; ElectricBonus=" + electricSuccess.BonusCredits +
                   "; CorridorRoute=" + route.FixedDurationSeconds + "s x" + route.IntrusionOccurrenceMultiplier +
                   "; SessionWallet=" + sessionCompleted.Wallet.Credits;
        }

        private static GameSessionState CompleteLightBladeSession(SpecialContractProgressState progress)
        {
            var contract = new TransportContractDefinition(
                "detailed-step17-light-blade",
                "Detailed Step 17 Light Blade",
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
            return started.CompleteTransport(new SettlementInput(
                contract.ContractType,
                contract.Difficulty,
                contract.Cargo,
                started.Ship,
                new CrewState(1, 0),
                started.Wallet,
                contractBasePay: contract.RewardCredits));
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
            progress = SpecialContractRules.RecordEnemyNeutralized(progress, SpecialContractEnemyKind.Revolution).State;
            progress = SpecialContractRules.RecordEnemyNeutralized(progress, SpecialContractEnemyKind.Revolution).State;
            return SpecialContractRules.RecordEnemyNeutralized(progress, SpecialContractEnemyKind.Revolution).State;
        }

        private static SpecialContractProgressState CreateCorridorPurifierOfferProgress()
        {
            var progress = SpecialContractProgressState.Empty;
            progress = SpecialContractRules.RecordEnemyNeutralized(progress, SpecialContractEnemyKind.Monstrum).State;
            progress = SpecialContractRules.RecordEnemyNeutralized(progress, SpecialContractEnemyKind.Dolore).State;
            progress = SpecialContractRules.RecordEnemyNeutralized(progress, SpecialContractEnemyKind.Revolution).State;
            return SpecialContractRules.RecordEnemyNeutralized(progress, SpecialContractEnemyKind.Ata).State;
        }

        private static CargoState CreateCargo(int sizeUnits = 1, float durabilityPercent = 1f)
        {
            return new CargoState(CargoGrade.Premium, sizeUnits, 0, durabilityPercent, false);
        }
    }
}
