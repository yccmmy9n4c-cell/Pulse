namespace Pulse.Platform.Linux.Providers;

public sealed class UnattendedUpgradesEvidenceProvider : ILinuxEvidenceProvider
{
    private static readonly string[] CandidateFiles =
    [
        "/etc/apt/apt.conf.d/20auto-upgrades",
        "/etc/apt/apt.conf.d/10periodic"
    ];

    public string Id => "linux.unattended-upgrades";

    public async Task<EvidenceResult> CollectAsync(CancellationToken cancellationToken = default)
    {
        var existing = CandidateFiles.Where(File.Exists).ToArray();
        if (existing.Length == 0)
        {
            return new(Id, "Automatic security updates", EvidenceState.Informational,
                "Pulse did not find a standard APT periodic-update configuration file.",
                "Your distribution may manage updates another way. Pulse did not change update settings.",
                string.Join(", ", CandidateFiles));
        }

        var contents = new List<string>();
        foreach (var path in existing)
        {
            contents.Add(await File.ReadAllTextAsync(path, cancellationToken));
        }

        var combined = string.Join('\n', contents);
        var enabled = combined.Contains("APT::Periodic::Unattended-Upgrade \"1\"", StringComparison.Ordinal);
        return new(Id, "Automatic security updates", enabled ? EvidenceState.Healthy : EvidenceState.Attention,
            enabled
                ? "APT periodic unattended upgrades are configured as enabled."
                : "APT configuration was found, but unattended upgrades are not clearly enabled.",
            enabled
                ? "Pulse confirmed configuration presence only; successful update history will be evaluated separately."
                : "Review automatic security-update preferences in the distribution's update settings. Pulse made no changes.",
            string.Join(", ", existing));
    }
}
