namespace Bellerophon.Platform
{
    public interface IPlatformMultiplayerServices
    {
        string ProviderName { get; }

        bool IsAvailable { get; }

        int MaxSupportedPlayers { get; }

        bool SupportsOnlineTransport { get; }
    }
}
