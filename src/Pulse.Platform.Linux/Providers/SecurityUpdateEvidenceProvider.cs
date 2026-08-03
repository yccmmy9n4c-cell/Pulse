using Pulse.Platform.Linux.Platform;

namespace Pulse.Platform.Linux.Providers;

public sealed class SecurityUpdateEvidenceProvider(IReadOnlyCommandRunner commandRunner) : ILinuxEvidenceProvider
{
    public string Id => "linux.apt-security-updates";

    public async Task<EvidenceResult> CollectAsync(CancellationToken cancellationToken = default)
    {
        var result = await commandRunner.RunAsync("apt-get", ["--simulate", "--no-download", "upgrade"],
            TimeSpan.FromSeconds(20), cancellationToken);
        if (!result.Started || result.TimedOut || result.ExitCode != 0)
        {
            return EvidenceResult.Unavailable(Id, "Cached security updates", "apt-get --simulate --no-download upgrade",
                result.TimedOut
                    ? "The cached security-update simulation timed out."
                    : "APT could not simulate an upgrade from its current local cache.");
        }

        var candidates = result.StandardOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(line => line.StartsWith("Inst ", StringComparison.Ordinal))
            .ToArray();
        var security = candidates.Where(IsSecurityCandidate).ToArray();
        if (security.Length == 0)
        {
            return new(Id, "Cached security updates", EvidenceState.Healthy,
                candidates.Length == 0
                    ? "APT's current local cache identifies no pending package upgrades."
                    : $"APT's current local cache identifies {candidates.Length} upgrade(s), with none clearly marked as security updates.",
                "This is a no-download simulation using existing cache data. Pulse did not contact repositories or install updates.",
                "apt-get --simulate --no-download upgrade (cached)");
        }

        var names = security.Select(ParsePackageName).Where(name => name.Length > 0).Take(4).ToArray();
        var preview = names.Length == 0 ? string.Empty : $": {string.Join(", ", names)}";
        var suffix = security.Length > names.Length ? $" and {security.Length - names.Length} more" : string.Empty;
        return new(Id, "Cached security updates", EvidenceState.Attention,
            $"APT's local cache identifies {security.Length} security update(s){preview}{suffix}.",
            "Review and install security updates through your distribution's normal update tool. Pulse did not refresh or install anything.",
            "apt-get --simulate --no-download upgrade (cached)");
    }

    private static bool IsSecurityCandidate(string line) =>
        line.Contains("-security", StringComparison.OrdinalIgnoreCase) ||
        line.Contains("/security", StringComparison.OrdinalIgnoreCase) ||
        line.Contains(" security ", StringComparison.OrdinalIgnoreCase);

    private static string ParsePackageName(string line)
    {
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 1 ? parts[1].Split(':', 2)[0] : string.Empty;
    }
}
