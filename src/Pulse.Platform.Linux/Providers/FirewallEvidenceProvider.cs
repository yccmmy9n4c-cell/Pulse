using Pulse.Platform.Linux.Platform;

namespace Pulse.Platform.Linux.Providers;

public sealed class FirewallEvidenceProvider(IReadOnlyCommandRunner commandRunner) : ILinuxEvidenceProvider
{
    public const string InactiveSummary = "Pulse did not find an active UFW or nftables systemd service.";
    public const string InactiveGuidance = "This is not proof that the system has no firewall: rules may be managed by another service or directly. Deeper read-only rule inspection is deferred.";
    public const string InactiveSource = "systemctl is-active ufw.service; systemctl is-active nftables.service";
    public string Id => "linux.firewall-indicator";

    public async Task<EvidenceResult> CollectAsync(CancellationToken cancellationToken = default)
    {
        var ufw = await commandRunner.RunAsync("systemctl", ["is-active", "ufw.service"], cancellationToken: cancellationToken);
        if (IsActive(ufw))
        {
            return Active("UFW's systemd service is active.", "systemctl is-active ufw.service");
        }

        var nftables = await commandRunner.RunAsync("systemctl", ["is-active", "nftables.service"], cancellationToken: cancellationToken);
        if (IsActive(nftables))
        {
            return Active("The nftables systemd service is active.", "systemctl is-active nftables.service");
        }

        if (!ufw.Started && !nftables.Started)
        {
            return EvidenceResult.Unavailable(Id, "Firewall indicator", "systemctl is-active",
                "systemctl is unavailable, so Pulse could not read these service indicators.");
        }

        return new(Id, "Firewall indicator", EvidenceState.Informational,
            InactiveSummary,
            InactiveGuidance,
            InactiveSource);
    }

    private EvidenceResult Active(string summary, string source) => new(Id, "Firewall indicator", EvidenceState.Healthy,
        summary, "An active service is only a posture indicator; Pulse has not yet evaluated individual firewall rules.", source);

    private static bool IsActive(ReadOnlyCommandResult result) =>
        result.Started && !result.TimedOut && result.ExitCode == 0 &&
        result.StandardOutput.Trim().Equals("active", StringComparison.OrdinalIgnoreCase);
}
