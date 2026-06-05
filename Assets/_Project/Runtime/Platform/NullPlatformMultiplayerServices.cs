using Bellerophon.Core.Coop;

namespace Bellerophon.Platform
{
    public sealed class NullPlatformMultiplayerServices : IPlatformMultiplayerServices
    {
        public string ProviderName => "Null";

        public bool IsAvailable => true;

        public int MaxSupportedPlayers => CoopSessionLimits.FutureOnlineMaxPlayers;

        public bool SupportsOnlineTransport => false;
    }
}
