using System.Collections.Generic;

namespace Bellerophon.Platform
{
    public sealed class NullPlatformServices : IPlatformServices
    {
        private readonly NullPlatformAchievementServices achievements = new NullPlatformAchievementServices();
        private readonly NullPlatformCloudSaveServices cloud = new NullPlatformCloudSaveServices();
        private readonly NullPlatformStatsServices stats = new NullPlatformStatsServices();
        private readonly NullPlatformMultiplayerServices multiplayer = new NullPlatformMultiplayerServices();

        public string PlatformName => "Null";

        public bool IsAvailable => true;

        public IPlatformMultiplayerServices Multiplayer => multiplayer;

        public IPlatformAchievementServices Achievements => achievements;

        public IPlatformCloudSaveServices Cloud => cloud;

        public IPlatformStatsServices Stats => stats;

        public IReadOnlyCollection<string> UnlockedAchievements => achievements.UnlockedAchievementIds;

        public void UnlockAchievement(string achievementId)
        {
            achievements.UnlockAchievement(achievementId);
        }
    }

    public sealed class NullPlatformAchievementServices : IPlatformAchievementServices
    {
        private readonly HashSet<string> unlockedAchievements = new HashSet<string>();

        public string ProviderName => "Null";

        public bool IsAvailable => true;

        public IReadOnlyCollection<string> UnlockedAchievementIds => unlockedAchievements;

        public void UnlockAchievement(string achievementId)
        {
            if (!string.IsNullOrWhiteSpace(achievementId))
            {
                unlockedAchievements.Add(achievementId);
            }
        }

        public bool IsAchievementUnlocked(string achievementId)
        {
            return !string.IsNullOrWhiteSpace(achievementId) &&
                   unlockedAchievements.Contains(achievementId);
        }
    }

    public sealed class NullPlatformCloudSaveServices : IPlatformCloudSaveServices
    {
        private readonly Dictionary<string, byte[]> files = new Dictionary<string, byte[]>();

        public string ProviderName => "Null";

        public bool IsAvailable => true;

        public bool SupportsRemoteSync => false;

        public void WriteFile(string path, byte[] bytes)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            files[path] = bytes == null ? new byte[0] : (byte[])bytes.Clone();
        }

        public bool TryReadFile(string path, out byte[] bytes)
        {
            byte[] stored;
            if (string.IsNullOrWhiteSpace(path) || !files.TryGetValue(path, out stored))
            {
                bytes = null;
                return false;
            }

            bytes = (byte[])stored.Clone();
            return true;
        }

        public bool Exists(string path)
        {
            return !string.IsNullOrWhiteSpace(path) && files.ContainsKey(path);
        }

        public void DeleteFile(string path)
        {
            if (!string.IsNullOrWhiteSpace(path))
            {
                files.Remove(path);
            }
        }
    }

    public sealed class NullPlatformStatsServices : IPlatformStatsServices
    {
        private readonly Dictionary<string, int> intStats = new Dictionary<string, int>();

        public string ProviderName => "Null";

        public bool IsAvailable => true;

        public void SetIntStat(string statId, int value)
        {
            if (!string.IsNullOrWhiteSpace(statId))
            {
                intStats[statId] = value;
            }
        }

        public bool TryGetIntStat(string statId, out int value)
        {
            if (string.IsNullOrWhiteSpace(statId))
            {
                value = 0;
                return false;
            }

            return intStats.TryGetValue(statId, out value);
        }
    }
}
