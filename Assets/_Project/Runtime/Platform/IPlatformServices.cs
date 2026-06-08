namespace Bellerophon.Platform
{
    public interface IPlatformServices
    {
        string PlatformName { get; }

        bool IsAvailable { get; }

        IPlatformMultiplayerServices Multiplayer { get; }

        IPlatformAchievementServices Achievements { get; }

        IPlatformCloudSaveServices Cloud { get; }

        IPlatformStatsServices Stats { get; }

        void UnlockAchievement(string achievementId);
    }
}
