namespace Bellerophon.Platform
{
    public interface IPlatformServices
    {
        string PlatformName { get; }

        bool IsAvailable { get; }

        IPlatformMultiplayerServices Multiplayer { get; }

        void UnlockAchievement(string achievementId);
    }
}
