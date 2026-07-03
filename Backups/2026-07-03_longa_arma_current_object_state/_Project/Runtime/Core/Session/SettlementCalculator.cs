using System;
using System.Collections.Generic;

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
            var configuredContractPay = input.ContractBasePay +
                                        input.DistancePay +
                                        input.RepairSupportAmount +
                                        input.SafeStreakBonus;

            var contractRevenue = transportFailed
                ? 0
                : configuredContractPay > 0
                    ? configuredContractPay
                    : CalculateContractRevenue(input, cargoHoldScore, personalCargoSaleMultiplier);

            var shipLossInsurancePayout = transportFailed ? 0 : input.ShipLossInsurancePayout;
            var grossRevenue = contractRevenue + shipLossInsurancePayout;
            var towingExpense = requiresTowing ? input.TowingCost : 0;
            var revivalExpense = input.Crew.DeadCount * input.RevivalCostPerDeadCrew;
            var cargoLossPenalty = input.CargoLossPenalty > 0
                ? input.CargoLossPenalty
                : CalculateCargoLossPenalty(input);
            var cleaningExpense = input.Crew.SurvivorCount == 0 ? input.CleaningCostWhenNoSurvivors : 0;
            var associationExpense = input.AssociationBrokerageFee + input.AssociationMaintenanceFee;
            var pendingRepairCost = input.RepairCost;
            var expenses = input.InsuranceCost +
                           towingExpense +
                           revivalExpense +
                           cargoLossPenalty +
                           cleaningExpense +
                           associationExpense;
            var finalBalance = input.Wallet.Credits + grossRevenue - expenses;
            var debtStatus = GetDebtStatus(input.Wallet, finalBalance);
            var isGameOver = debtStatus == SettlementDebtStatus.FinalGameOver;
            var lineItems = BuildLineItems(
                input,
                contractRevenue,
                shipLossInsurancePayout,
                pendingRepairCost,
                input.InsuranceCost,
                towingExpense,
                revivalExpense,
                cargoLossPenalty,
                cleaningExpense,
                input.AssociationBrokerageFee,
                input.AssociationMaintenanceFee);

            return new SettlementResult(
                grossRevenue,
                expenses,
                grossRevenue - expenses,
                finalBalance,
                transportFailed,
                requiresTowing,
                isGameOver,
                cargoHoldScore,
                personalCargoSaleMultiplier,
                debtStatus,
                lineItems,
                pendingRepairCost);
        }

        public static float CalculateCargoHoldScore(ShipState ship)
        {
            return ShipStateRules.CalculateCargoHoldScore(ship);
        }

        public static int CalculateCargoLossPenalty(SettlementInput input)
        {
            if (input.Cargo.IsPersonalCargo || input.Cargo.LossPercent <= 0f)
            {
                return 0;
            }

            return RoundMoney(
                input.Cargo.BaseValue *
                GetGradeMultiplier(input.Cargo.Grade) *
                GetContractMultiplier(input.ContractType) *
                GetDifficultyMultiplier(input.Difficulty) *
                input.Cargo.LossPercent);
        }

        public static int CalculateAssociationSafeStreakBonus(int completedTransportNumber)
        {
            if (completedTransportNumber < 3)
            {
                return 0;
            }

            var cappedTransportNumber = Math.Min(completedTransportNumber, 10);
            return 50 + (cappedTransportNumber - 3) * 20;
        }

        private static int CalculateContractRevenue(
            SettlementInput input,
            float cargoHoldScore,
            float personalCargoSaleMultiplier)
        {
            var cargoDurabilityMultiplier = input.Cargo.IsPersonalCargo
                ? input.Cargo.DurabilityPercent
                : 1f;

            return RoundMoney(
                input.Cargo.BaseValue *
                GetGradeMultiplier(input.Cargo.Grade) *
                GetContractMultiplier(input.ContractType) *
                GetDifficultyMultiplier(input.Difficulty) *
                cargoHoldScore *
                personalCargoSaleMultiplier *
                cargoDurabilityMultiplier);
        }

        private static SettlementDebtStatus GetDebtStatus(WalletState wallet, int finalBalance)
        {
            if (finalBalance >= 0)
            {
                return SettlementDebtStatus.Clear;
            }

            return wallet.HasUnpaidDebtGrace
                ? SettlementDebtStatus.FinalGameOver
                : SettlementDebtStatus.GraceActive;
        }

        private static SettlementLineItem[] BuildLineItems(
            SettlementInput input,
            int contractRevenue,
            int shipLossInsurancePayout,
            int repairCost,
            int legacyInsuranceCost,
            int towingCost,
            int revivalCost,
            int cargoLossPenalty,
            int cleaningCost,
            int associationBrokerageFee,
            int associationMaintenanceFee)
        {
            var items = new List<SettlementLineItem>();
            if (contractRevenue > 0 &&
                (input.ContractBasePay > 0 ||
                 input.DistancePay > 0 ||
                 input.RepairSupportAmount > 0 ||
                 input.SafeStreakBonus > 0))
            {
                AddRevenue(items, "Contract reward", input.ContractBasePay);
                AddRevenue(items, "Distance pay", input.DistancePay);
                AddRevenue(items, "Association support bonus", input.RepairSupportAmount);
                AddRevenue(items, "Safe streak bonus", input.SafeStreakBonus);
            }
            else
            {
                AddRevenue(items, "Contract reward", contractRevenue);
            }

            AddRevenue(items, "Ship loss insurance payout", shipLossInsurancePayout);
            AddPendingExpense(items, "Ship repair cost", repairCost);
            AddExpense(items, "Insurance cost", legacyInsuranceCost);
            AddExpense(items, "Towing cost", towingCost);
            AddExpense(items, "Dead crew life insurance", revivalCost);
            AddExpense(items, "Cargo loss penalty", cargoLossPenalty);
            AddExpense(items, "No-survivor cleaning cost", cleaningCost);
            AddExpense(items, "Association brokerage fee", associationBrokerageFee);
            AddExpense(items, "Association maintenance fee", associationMaintenanceFee);
            return items.ToArray();
        }

        private static void AddRevenue(List<SettlementLineItem> items, string label, int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            items.Add(new SettlementLineItem(label, amount, true));
        }

        private static void AddPendingExpense(List<SettlementLineItem> items, string label, int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            items.Add(new SettlementLineItem(label, -amount, false, false));
        }

        private static void AddExpense(List<SettlementLineItem> items, string label, int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            items.Add(new SettlementLineItem(label, -amount, false));
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
