using System;
using System.IO;
using Bellerophon.Core.Session;
using UnityEngine;

namespace Bellerophon.Editor.Validation
{
    public static class DetailedStep21BalancePlaytestHardeningEditorValidation
    {
        public static void Run()
        {
            var summary = BuildValidationSummary();
            Debug.Log("Detailed step 21 balance playtest hardening editor validation passed.");
            Debug.Log("Detailed step 21 balance playtest hardening validation details: " + summary);
        }

        public static string BuildValidationSummary()
        {
            ValidateSourceValuedEconomyPins();
            ValidateRiskAndFailurePins();
            var smokeScriptCount = ValidateSmokeSuiteScripts();

            var postTutorial = TransportContractDefinition.CreatePostTutorialContracts();
            return "RepairRate=" + ShipStateRules.SettlementSummaryRepairRatePerPercent +
                   "; Towing=" + ShipStateRules.CalculateTowingCost(1) + "/" +
                   ShipStateRules.CalculateTowingCost(2) + "/" +
                   ShipStateRules.CalculateTowingCost(3) +
                   "; PostTutorialRewards=" + postTutorial[0].RewardCredits + "/" + postTutorial[1].RewardCredits +
                   "; AlienGate=" + TransportHazardRules.AlienLifeFameThreshold +
                   "; CorridorDuration=" + SpecialContractRules.CorridorPurifierRouteDurationSeconds +
                   "; SmokeScripts=" + smokeScriptCount;
        }

        private static void ValidateSourceValuedEconomyPins()
        {
            RequireEqual(ShipStateRules.SettlementSummaryRepairRatePerPercent, 5, "repair rate");
            RequireEqual(ShipStateRules.MaxNormalRepairMissingPercent, 599, "normal repair cap");
            RequireEqual(ShipStateRules.TotalLossClaimCost, 5000, "total loss claim");
            RequireEqual(ShipStateRules.CalculateTowingCost(1), 2000, "first towing cost");
            RequireEqual(ShipStateRules.CalculateTowingCost(2), 3000, "second towing cost");
            RequireEqual(ShipStateRules.CalculateTowingCost(3), 5000, "third towing cost");
            RequireEqual(ShipStateRules.CalculateTowingCost(4), 7500, "fourth towing cost");

            var tutorial = TransportContractDefinition.CreateTutorial();
            var postTutorial = TransportContractDefinition.CreatePostTutorialContracts();
            RequireEqual(tutorial.RewardCredits, 1000, "tutorial reward");
            RequireEqual(tutorial.DurationSeconds, 60, "tutorial duration");
            RequireEqual(postTutorial[0].RewardCredits, 900, "association follow-up reward");
            RequireEqual(postTutorial[1].RewardCredits, 1800, "private follow-up reward");

            RequireEqual(EquipmentRules.GetDefinition(EquipmentItemKind.Stick).PriceCredits, 200, "stick price");
            RequireEqual(EquipmentRules.GetDefinition(EquipmentItemKind.Musket).PriceCredits, 450, "musket price");
            RequireEqual(EquipmentRules.GetDefinition(EquipmentItemKind.Shotgun).PriceCredits, 600, "shotgun price");
            RequireEqual(EquipmentRules.GetDefinition(EquipmentItemKind.LightBlade).PriceCredits, 1000, "light blade price");
            RequireEqual(EquipmentRules.GetDefinition(EquipmentItemKind.ElectricMine).PriceCredits, 1000, "electric mine price");
            RequireEqual(EquipmentRules.GetDefinition(EquipmentItemKind.CorridorPurifier).PriceCredits, 600, "corridor purifier price");
        }

        private static void ValidateRiskAndFailurePins()
        {
            RequireEqual(TransportHazardRules.AsteroidFieldOccurrencePercent, 30, "asteroid occurrence");
            RequireEqual(TransportHazardRules.CargoFreedomLeagueOccurrencePercent, 15, "cargo freedom occurrence");
            RequireEqual(TransportHazardRules.SpacePirateOccurrencePercent, 5, "space pirate occurrence");
            RequireEqual(TransportHazardRules.AlienLifeOccurrencePercent, 10, "alien life occurrence");

            var corridorState = SpecialContractProgressState.Empty
                .WithActiveContract(SpecialContractKind.CorridorPurifierUnlock);
            var modifier = SpecialContractRules.CreateRouteModifier(corridorState);
            if (!modifier.ForcesAllIntrusionHazards)
            {
                throw new InvalidOperationException("Corridor purifier route must force intrusion hazards.");
            }

            RequireEqual(modifier.IntrusionOccurrenceMultiplier, 3, "corridor intrusion multiplier");
            RequireEqual(modifier.FixedDurationSeconds, 284, "corridor route duration");

            var firstNegative = SettlementCalculator.Calculate(CreateSettlementInput(
                new WalletState(10, false),
                cargoLossPenalty: 150));
            var secondNegative = SettlementCalculator.Calculate(CreateSettlementInput(
                new WalletState(-40, false, true),
                cargoLossPenalty: 150));
            if (firstNegative.DebtStatus != SettlementDebtStatus.GraceActive || firstNegative.IsGameOver)
            {
                throw new InvalidOperationException("First negative settlement must use debt grace.");
            }

            if (secondNegative.DebtStatus != SettlementDebtStatus.FinalGameOver || !secondNegative.IsGameOver)
            {
                throw new InvalidOperationException("Second negative settlement must trigger final game over.");
            }
        }

        private static int ValidateSmokeSuiteScripts()
        {
            var scripts = new[]
            {
                "Run-Phase1To18Smokes.ps1",
                "Run-DetailedStep13SeedEntitySmoke.ps1",
                "Run-DetailedStep14AlienLifeformSmoke.ps1",
                "Run-DetailedStep15CargoFreedomLeagueSmoke.ps1",
                "Run-DetailedStep16SpacePirateSmoke.ps1",
                "Run-DetailedStep17SpecialContractsSmoke.ps1",
                "Run-DetailedStep18PlanetUxSmoke.ps1",
                "Run-DetailedStep19SaveSettingsPlatformSmoke.ps1",
                "Run-DetailedStep20PresentationSmoke.ps1",
                "Run-DetailedStep21BalancePlaytestHardeningSmoke.ps1",
                "Run-DetailedStep21FullSmokeSuite.ps1"
            };
            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            for (var i = 0; i < scripts.Length; i++)
            {
                var scriptPath = Path.Combine(projectRoot, "scripts", scripts[i]);
                if (!File.Exists(scriptPath))
                {
                    throw new FileNotFoundException("Detailed step 21 smoke-suite script is missing.", scriptPath);
                }
            }

            return scripts.Length;
        }

        private static SettlementInput CreateSettlementInput(
            WalletState wallet,
            int cargoLossPenalty)
        {
            return new SettlementInput(
                ContractType.Association,
                ContractDifficulty.Normal,
                new CargoState(CargoGrade.Common, 1, 100, 1f, false),
                ShipState.CreateDefault(),
                new CrewState(1, 0),
                wallet,
                contractBasePay: 100,
                cargoLossPenalty: cargoLossPenalty);
        }

        private static void RequireEqual(int actual, int expected, string label)
        {
            if (actual != expected)
            {
                throw new InvalidOperationException(
                    "Detailed step 21 source pin changed for " + label + ": expected " + expected + ", got " + actual + ".");
            }
        }
    }
}
