namespace Bellerophon.Core.Session
{
    public readonly struct WalletState
    {
        public WalletState(int credits, bool allowsDebt)
            : this(credits, allowsDebt, false)
        {
        }

        public WalletState(int credits, bool allowsDebt, bool hasUnpaidDebtGrace)
        {
            Credits = credits;
            AllowsDebt = allowsDebt;
            HasUnpaidDebtGrace = hasUnpaidDebtGrace && credits < 0;
        }

        public int Credits { get; }

        public bool AllowsDebt { get; }

        public bool HasUnpaidDebtGrace { get; }

        public bool IsInDebt => Credits < 0;
    }
}
