using System;

namespace Bellerophon.Core.Session
{
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
            float personalCargoSaleMultiplier = 1f)
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
                PersonalCargoSaleMultiplier);
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
        public SettlementResult(
            int grossRevenue,
            int expenses,
            int netChange,
            int finalBalance,
            bool isTransportFailed,
            bool requiresTowing,
            bool isGameOver,
            float cargoHoldScore,
            float personalCargoSaleMultiplier)
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
    }
}
