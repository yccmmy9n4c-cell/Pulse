namespace Pulse.Platform.Linux.Providers;

public sealed class StorageEvidenceProvider : ILinuxEvidenceProvider
{
    public string Id => "linux.storage-root";

    public Task<EvidenceResult> CollectAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var root = DriveInfo.GetDrives().FirstOrDefault(drive => drive.IsReady && drive.Name == "/");
            if (root is null)
            {
                return Task.FromResult(EvidenceResult.Unavailable(Id, "System storage", "DriveInfo:/",
                    "Pulse could not identify the mounted root filesystem."));
            }

            var usedBytes = root.TotalSize - root.AvailableFreeSpace;
            var usedPercent = root.TotalSize > 0 ? usedBytes * 100d / root.TotalSize : 0;
            var state = usedPercent >= 85 ? EvidenceState.Attention : EvidenceState.Healthy;
            var summary = $"Root filesystem: {usedPercent:F1}% used • {ToGiB(root.AvailableFreeSpace):F1} GiB free of {ToGiB(root.TotalSize):F1} GiB";
            var guidance = state == EvidenceState.Healthy
                ? "The root filesystem has reasonable free space. No cleanup was performed."
                : "Root storage is above the 85% attention threshold. Review large files and applications before space becomes critical.";
            return Task.FromResult(new EvidenceResult(Id, "System storage", state, summary, guidance, "DriveInfo:/"));
        }
        catch (IOException ex)
        {
            return Task.FromResult(EvidenceResult.Unavailable(Id, "System storage", "DriveInfo:/", ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Task.FromResult(EvidenceResult.Unavailable(Id, "System storage", "DriveInfo:/", ex.Message));
        }
    }

    private static double ToGiB(long bytes) => bytes / 1024d / 1024d / 1024d;
}
