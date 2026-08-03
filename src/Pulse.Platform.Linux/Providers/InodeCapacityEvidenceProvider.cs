using Pulse.Platform.Linux.Platform;

namespace Pulse.Platform.Linux.Providers;

public sealed class InodeCapacityEvidenceProvider(IReadOnlyCommandRunner commandRunner) : ILinuxEvidenceProvider
{
    public string Id => "linux.inode-capacity";

    public async Task<EvidenceResult> CollectAsync(CancellationToken cancellationToken = default)
    {
        var result = await commandRunner.RunAsync("df", ["--portability", "--inodes", "/"],
            TimeSpan.FromSeconds(10), cancellationToken);
        if (!result.Started || result.TimedOut || result.ExitCode != 0)
        {
            return EvidenceResult.Unavailable(Id, "Filesystem inode capacity", "df --portability --inodes /",
                result.TimedOut ? "The inode-capacity query timed out." : "df could not provide root inode usage.");
        }

        var lines = result.StandardOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (lines.Length < 2)
        {
            return EvidenceResult.Unavailable(Id, "Filesystem inode capacity", "df --portability --inodes /",
                "The inode-capacity output did not contain a data row.");
        }

        var fields = lines[^1].Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (fields.Length < 6 || !TryPercent(fields[^2], out var usedPercent) ||
            !long.TryParse(fields[^3], out var freeInodes))
        {
            return EvidenceResult.Unavailable(Id, "Filesystem inode capacity", "df --portability --inodes /",
                "Pulse could not interpret the root inode-capacity row.");
        }

        var state = usedPercent >= 85 ? EvidenceState.Attention : EvidenceState.Healthy;
        return new(Id, "Filesystem inode capacity", state,
            $"Root filesystem inode usage is {usedPercent}% with {freeInodes:N0} inode(s) available.",
            state == EvidenceState.Healthy
                ? "The root filesystem has reasonable inode capacity. No files were removed."
                : "Inode usage is above the 85% attention threshold. Review directories containing very large numbers of small files before file creation begins to fail.",
            "df --portability --inodes /");
    }

    private static bool TryPercent(string value, out int percent) =>
        int.TryParse(value.TrimEnd('%'), out percent) && percent is >= 0 and <= 100;
}
