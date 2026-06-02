using System;

namespace Bellerophon.Core.Session
{
    public static class SettlementCalculator
    {
        // MVP baseline multipliers live here until phase-specific balance data assets exist.
        private const float PrivateContractMultiplier = 1.35f;
        private const float SpecialContractMultiplier = 1.75f;
        private const float RareCargoMultiplier = 1.8f;
        private const float PremiumCargoMultiplier = 3f;

        public static SettlementResult Calculate(SettlementInput input)
        {
            var requiresTowing = input.Ship.RequiresTowing;
            var transportFailed = input.Ship.IsTransportFailed;
            var cargoHoldScore = CalculateCargoHoldScore(input.Ship);
            var personalCargoSaleMultiplier = input.Cargo.IsPersonalCargo ? input.PersonalCargoSaleMultiplier : 1f;

            var grossRevenue = transportFailed
                ? 0
                : RoundMoney(
                    input.Cargo.BaseValue *
                    GetGradeMultiplier(input.Cargo.Grade) *
                    GetContractMultiplier(input.ContractType) *
                    GetDifficultyMultiplier(input.Difficulty) *
                    input.Cargo.DurabilityPercent *
                    cargoHoldScore *
                    personalCargoSaleMultiplier);

            var towingExpense = requiresTowing ? input.TowingCost : 0;
            var revivalExpense = input.Crew.DeadCount * input.RevivalCostPerDeadCrew;
            var expenses = input.RepairCost + input.InsuranceCost + towingExpense + revivalExpense;
            var finalBalance = input.Wallet.Credits + grossRevenue - expenses;
            var isGameOver = finalBalance < 0 && !input.Wallet.AllowsDebt;

            return new SettlementResult(
                grossRevenue,
                expenses,
                grossRevenue - expenses,
                finalBalance,
                transportFailed,
                requiresTowing,
                isGameOver,
                cargoHoldScore,
                personalCargoSaleMultiplier);
        }

        public static float CalculateCargoHoldScore(ShipState ship)
        {
            return ShipStateRules.CalculateCargoHoldScore(ship);
        }

        private static float GetContractMultiplier(ContractType contractType)
        {
            switch (contractType)
            {
                case ContractType.Association:
                    return 1f;
                case ContractType.Private:
                    return PrivateContractMultiplier;
                case ContractType.Special:
                    return SpecialContractMultiplier;
                default:
                    throw new ArgumentOutOfRangeException(nameof(contractType), contractType, null);
            }
        }

        private static float GetDifficultyMultiplier(ContractDifficulty difficulty)
        {
            switch (difficulty)
            {
                case ContractDifficulty.Intro:
                    return 0.6f;
                case ContractDifficulty.VeryEasy:
                    return 0.75f;
                case ContractDifficulty.Easy:
                    return 0.9f;
                case ContractDifficulty.Normal:
                    return 1f;
                case ContractDifficulty.Hard:
                    return 1.25f;
                case ContractDifficulty.VeryHard:
                    return 1.5f;
                case ContractDifficulty.Master:
                    return 2f;
                default:
                    throw new ArgumentOutOfRangeException(nameof(difficulty), difficulty, null);
            }
        }

        private static float GetGradeMultiplier(CargoGrade grade)
        {
            switch (grade)
            {
                case CargoGrade.Common:
                    return 1f;
                case CargoGrade.Rare:
                    return RareCargoMultiplier;
                case CargoGrade.Premium:
                    return PremiumCargoMultiplier;
                default:
                    throw new ArgumentOutOfRangeException(nameof(grade), grade, null);
            }
        }

        private static int RoundMoney(float value)
        {
            return (int)Math.Round(value, MidpointRounding.AwayFromZero);
        }
    }
}
