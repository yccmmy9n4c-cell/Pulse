using Pulse.Platform.Linux.Platform;

namespace Pulse.Platform.Linux.Providers;

public sealed class SystemdBootTimingEvidenceProvider(IReadOnlyCommandRunner commandRunner) : ILinuxEvidenceProvider
{
    public string Id => "linux.systemd-boot-timing";

    public async Task<EvidenceResult> CollectAsync(CancellationToken cancellationToken = default)
    {
        const string source = "systemd-analyze time --no-pager";
        var result = await commandRunner.RunAsync("systemd-analyze", ["time", "--no-pager"],
            TimeSpan.FromSeconds(10), cancellationToken);
        if (!result.Started || result.TimedOut || result.ExitCode != 0 || string.IsNullOrWhiteSpace(result.StandardOutput))
        {
            return EvidenceResult.Unavailable(Id, "Boot timing", source,
                result.TimedOut
                    ? "The boot-timing query timed out."
                    : "systemd boot timing was not available to the current user.");
        }

        var summary = result.StandardOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault(line => line.StartsWith("Startup finished", StringComparison.OrdinalIgnoreCase))
            ?? result.StandardOutput.Trim();
        return new(Id, "Boot timing", EvidenceState.Informational, summary,
            "Boot duration is a baseline, not a fault by itself. Compare future assessments and investigate only if startup becomes materially slower or disrupts normal use.",
            source);
    }
}
