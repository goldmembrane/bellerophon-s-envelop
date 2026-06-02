namespace Bellerophon.Core.Session
{
    public readonly struct WalletState
    {
        public WalletState(int credits, bool allowsDebt)
        {
            Credits = credits;
            AllowsDebt = allowsDebt;
        }

        public int Credits { get; }

        public bool AllowsDebt { get; }
    }
}
