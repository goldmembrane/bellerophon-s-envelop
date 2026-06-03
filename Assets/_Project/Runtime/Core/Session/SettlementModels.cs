using System;

namespace Bellerophon.Core.Session
{
    public enum SettlementDebtStatus
    {
        Clear,
        GraceActive,
        FinalGameOver
    }

    public readonly struct SettlementLineItem
    {
        public SettlementLineItem(string label, int amount, bool isRevenue, bool affectsBalance = true)
        {
            if (string.IsNullOrWhiteSpace(label))
            {
                throw new ArgumentException("Settlement line item label is required.", nameof(label));
            }

            Label = label;
            Amount = amount;
            IsRevenue = isRevenue;
            AffectsBalance = affectsBalance;
        }

        public string Label { get; }

        public int Amount { get; }

        public bool IsRevenue { get; }

        public bool AffectsBalance { get; }
    }

    public readonly struct SettlementInput
    {
        public SettlementInput(
            ContractType contractType,
            ContractDifficulty difficulty,
            CargoState cargo,
            ShipState ship,
            CrewState crew,
            WalletState wallet,
            int repairCost = 0,
            int insuranceCost = 0,
            int towingCost = 0,
            int revivalCostPerDeadCrew = 0,
            float personalCargoSaleMultiplier = 1f,
            int contractBasePay = 0,
            int distancePay = 0,
            int repairSupportAmount = 0,
            int safeStreakBonus = 0,
            int shipLossInsurancePayout = 0,
            int cargoLossPenalty = 0,
            int cleaningCostWhenNoSurvivors = 0,
            int associationBrokerageFee = 0,
            int associationMaintenanceFee = 0)
        {
            ContractType = contractType;
            Difficulty = difficulty;
            Cargo = cargo;
            Ship = ship ?? throw new ArgumentNullException(nameof(ship));
            Crew = crew;
            Wallet = wallet;
            RepairCost = RequireNonNegative(repairCost, nameof(repairCost));
            InsuranceCost = RequireNonNegative(insuranceCost, nameof(insuranceCost));
            TowingCost = RequireNonNegative(towingCost, nameof(towingCost));
            RevivalCostPerDeadCrew = RequireNonNegative(revivalCostPerDeadCrew, nameof(revivalCostPerDeadCrew));
            PersonalCargoSaleMultiplier = personalCargoSaleMultiplier < 0f ? 0f : personalCargoSaleMultiplier;
            ContractBasePay = RequireNonNegative(contractBasePay, nameof(contractBasePay));
            DistancePay = RequireNonNegative(distancePay, nameof(distancePay));
            RepairSupportAmount = RequireNonNegative(repairSupportAmount, nameof(repairSupportAmount));
            SafeStreakBonus = RequireNonNegative(safeStreakBonus, nameof(safeStreakBonus));
            ShipLossInsurancePayout = RequireNonNegative(shipLossInsurancePayout, nameof(shipLossInsurancePayout));
            CargoLossPenalty = RequireNonNegative(cargoLossPenalty, nameof(cargoLossPenalty));
            CleaningCostWhenNoSurvivors = RequireNonNegative(cleaningCostWhenNoSurvivors, nameof(cleaningCostWhenNoSurvivors));
            AssociationBrokerageFee = RequireNonNegative(associationBrokerageFee, nameof(associationBrokerageFee));
            AssociationMaintenanceFee = RequireNonNegative(associationMaintenanceFee, nameof(associationMaintenanceFee));
        }

        public ContractType ContractType { get; }

        public ContractDifficulty Difficulty { get; }

        public CargoState Cargo { get; }

        public ShipState Ship { get; }

        public CrewState Crew { get; }

        public WalletState Wallet { get; }

        public int RepairCost { get; }

        public int InsuranceCost { get; }

        public int TowingCost { get; }

        public int RevivalCostPerDeadCrew { get; }

        public float PersonalCargoSaleMultiplier { get; }

        public int ContractBasePay { get; }

        public int DistancePay { get; }

        public int RepairSupportAmount { get; }

        public int SafeStreakBonus { get; }

        public int ShipLossInsurancePayout { get; }

        public int CargoLossPenalty { get; }

        public int CleaningCostWhenNoSurvivors { get; }

        public int AssociationBrokerageFee { get; }

        public int AssociationMaintenanceFee { get; }

        public SettlementInput WithShip(ShipState ship)
        {
            return new SettlementInput(
                ContractType,
                Difficulty,
                Cargo,
                ship,
                Crew,
                Wallet,
                RepairCost,
                InsuranceCost,
                TowingCost,
                RevivalCostPerDeadCrew,
                PersonalCargoSaleMultiplier,
                ContractBasePay,
                DistancePay,
                RepairSupportAmount,
                SafeStreakBonus,
                ShipLossInsurancePayout,
                CargoLossPenalty,
                CleaningCostWhenNoSurvivors,
                AssociationBrokerageFee,
                AssociationMaintenanceFee);
        }

        public SettlementInput WithWallet(WalletState wallet)
        {
            return new SettlementInput(
                ContractType,
                Difficulty,
                Cargo,
                Ship,
                Crew,
                wallet,
                RepairCost,
                InsuranceCost,
                TowingCost,
                RevivalCostPerDeadCrew,
                PersonalCargoSaleMultiplier,
                ContractBasePay,
                DistancePay,
                RepairSupportAmount,
                SafeStreakBonus,
                ShipLossInsurancePayout,
                CargoLossPenalty,
                CleaningCostWhenNoSurvivors,
                AssociationBrokerageFee,
                AssociationMaintenanceFee);
        }

        private static int RequireNonNegative(int value, string name)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(name, "Settlement costs cannot be negative.");
            }

            return value;
        }
    }

    public readonly struct SettlementResult
    {
        private static readonly SettlementLineItem[] EmptyLineItems = new SettlementLineItem[0];
        private readonly SettlementLineItem[] lineItems;

        public SettlementResult(
            int grossRevenue,
            int expenses,
            int netChange,
            int finalBalance,
            bool isTransportFailed,
            bool requiresTowing,
            bool isGameOver,
            float cargoHoldScore,
            float personalCargoSaleMultiplier,
            SettlementDebtStatus debtStatus = SettlementDebtStatus.Clear,
            SettlementLineItem[] lineItems = null,
            int pendingRepairCost = 0)
        {
            GrossRevenue = grossRevenue;
            Expenses = expenses;
            NetChange = netChange;
            FinalBalance = finalBalance;
            IsTransportFailed = isTransportFailed;
            RequiresTowing = requiresTowing;
            IsGameOver = isGameOver;
            CargoHoldScore = cargoHoldScore;
            PersonalCargoSaleMultiplier = personalCargoSaleMultiplier;
            DebtStatus = debtStatus;
            this.lineItems = lineItems ?? EmptyLineItems;
            PendingRepairCost = pendingRepairCost < 0 ? 0 : pendingRepairCost;
        }

        public int GrossRevenue { get; }

        public int Expenses { get; }

        public int NetChange { get; }

        public int FinalBalance { get; }

        public bool IsTransportFailed { get; }

        public bool RequiresTowing { get; }

        public bool IsGameOver { get; }

        public float CargoHoldScore { get; }

        public float PersonalCargoSaleMultiplier { get; }

        public SettlementDebtStatus DebtStatus { get; }

        public bool RequiresDebtGrace => DebtStatus == SettlementDebtStatus.GraceActive;

        public int PendingRepairCost { get; }

        public SettlementLineItem[] LineItems => lineItems ?? EmptyLineItems;

        public SettlementResult WithPendingRepairCost(int pendingRepairCost)
        {
            return new SettlementResult(
                GrossRevenue,
                Expenses,
                NetChange,
                FinalBalance,
                IsTransportFailed,
                RequiresTowing,
                IsGameOver,
                CargoHoldScore,
                PersonalCargoSaleMultiplier,
                DebtStatus,
                LineItems,
                pendingRepairCost);
        }
    }
}
