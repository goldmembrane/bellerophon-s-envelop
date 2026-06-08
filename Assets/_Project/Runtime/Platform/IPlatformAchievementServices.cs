using System.Collections.Generic;

namespace Bellerophon.Platform
{
    public interface IPlatformAchievementServices
    {
        string ProviderName { get; }

        bool IsAvailable { get; }

        IReadOnlyCollection<string> UnlockedAchievementIds { get; }

        void UnlockAchievement(string achievementId);

        bool IsAchievementUnlocked(string achievementId);
    }
}
