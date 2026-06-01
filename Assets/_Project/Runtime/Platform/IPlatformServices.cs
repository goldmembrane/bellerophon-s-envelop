namespace Bellerophon.Platform
{
    public interface IPlatformServices
    {
        string PlatformName { get; }

        bool IsAvailable { get; }

        void UnlockAchievement(string achievementId);
    }
}

