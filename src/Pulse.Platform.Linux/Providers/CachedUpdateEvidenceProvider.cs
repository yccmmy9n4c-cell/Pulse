using Pulse.Platform.Linux.Platform;

namespace Pulse.Platform.Linux.Providers;

public sealed class CachedUpdateEvidenceProvider(IReadOnlyCommandRunner commandRunner) : ILinuxEvidenceProvider
{
    public string Id => "linux.apt-cached-updates";

    public async Task<EvidenceResult> CollectAsync(CancellationToken cancellationToken = default)
    {
        var result = await commandRunner.RunAsync("apt", ["list", "--upgradable"],
            TimeSpan.FromSeconds(12), cancellationToken);
        if (!result.Started || result.TimedOut || result.ExitCode != 0)
        {
            return EvidenceResult.Unavailable(Id, "Cached update posture", "apt list --upgradable",
                result.TimedOut ? "The cached update query timed out." : "APT could not provide its cached update list.");
        }

        var packages = result.StandardOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(line => !line.StartsWith("Listing", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (packages.Length == 0)
        {
            return new(Id, "Cached update posture", EvidenceState.Healthy,
                "APT's current local cache lists no upgradable packages.",
                "This is cached evidence only; Pulse did not contact repositories or refresh package lists.",
                "apt list --upgradable (cached)");
        }

        var preview = string.Join(", ", packages.Take(4).Select(line => line.Split('/', 2)[0]));
        var suffix = packages.Length > 4 ? $" and {packages.Length - 4} more" : string.Empty;
        return new(Id, "Cached update posture", EvidenceState.Attention,
            $"APT's local cache lists {packages.Length} upgradable package(s): {preview}{suffix}.",
            "Review updates using your distribution's normal update tool. Pulse did not refresh or install anything.",
            "apt list --upgradable (cached)");
    }
}
