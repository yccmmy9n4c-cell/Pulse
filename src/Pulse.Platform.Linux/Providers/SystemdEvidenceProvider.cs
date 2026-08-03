namespace Pulse.Platform.Linux.Providers;

public sealed class SystemdEvidenceProvider : ILinuxEvidenceProvider
{
    public string Id => "linux.systemd";

    public Task<EvidenceResult> CollectAsync(CancellationToken cancellationToken = default)
    {
        var available = Directory.Exists("/run/systemd/system");
        var summary = available
            ? "systemd is active. A future Pulse schedule can use systemd --user after explicit approval."
            : "Pulse did not detect an active systemd system instance.";
        return Task.FromResult(new EvidenceResult(Id, "Service and scheduling foundation",
            available ? EvidenceState.Healthy : EvidenceState.Informational, summary,
            "No service or timer was created, enabled, or started.", "/run/systemd/system"));
    }
}
