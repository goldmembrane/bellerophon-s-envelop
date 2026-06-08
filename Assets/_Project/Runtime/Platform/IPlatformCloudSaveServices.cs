namespace Bellerophon.Platform
{
    public interface IPlatformCloudSaveServices
    {
        string ProviderName { get; }

        bool IsAvailable { get; }

        bool SupportsRemoteSync { get; }

        void WriteFile(string path, byte[] bytes);

        bool TryReadFile(string path, out byte[] bytes);

        bool Exists(string path);

        void DeleteFile(string path);
    }
}
