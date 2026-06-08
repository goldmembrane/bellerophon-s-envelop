namespace Bellerophon.Platform
{
    public interface IPlatformStatsServices
    {
        string ProviderName { get; }

        bool IsAvailable { get; }

        void SetIntStat(string statId, int value);

        bool TryGetIntStat(string statId, out int value);
    }
}
