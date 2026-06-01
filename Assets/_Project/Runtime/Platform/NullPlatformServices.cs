using System.Collections.Generic;

namespace Bellerophon.Platform
{
    public sealed class NullPlatformServices : IPlatformServices
    {
        private readonly HashSet<string> unlockedAchievements = new HashSet<string>();

        public string PlatformName => "Null";

        public bool IsAvailable => true;

        public IReadOnlyCollection<string> UnlockedAchievements => unlockedAchievements;

        public void UnlockAchievement(string achievementId)
        {
            if (!string.IsNullOrWhiteSpace(achievementId))
            {
                unlockedAchievements.Add(achievementId);
            }
        }
    }
}
