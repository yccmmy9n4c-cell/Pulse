namespace Pulse.Platform.Linux.Providers;

public sealed class AppArmorEvidenceProvider : ILinuxEvidenceProvider
{
    public string Id => "linux.apparmor";

    public async Task<EvidenceResult> CollectAsync(CancellationToken cancellationToken = default)
    {
        const string parameterPath = "/sys/module/apparmor/parameters/enabled";
        if (!File.Exists(parameterPath))
        {
            return new(Id, "AppArmor posture", EvidenceState.Informational,
                "The AppArmor kernel parameter was not found.",
                "AppArmor may be unavailable or disabled. Pulse did not change security policy.", parameterPath);
        }

        var value = (await File.ReadAllTextAsync(parameterPath, cancellationToken)).Trim();
        var enabled = value.Equals("Y", StringComparison.OrdinalIgnoreCase) || value == "1";
        return new(Id, "AppArmor posture", enabled ? EvidenceState.Healthy : EvidenceState.Attention,
            enabled ? "The kernel reports AppArmor enabled." : $"The kernel reports AppArmor disabled ({value}).",
            enabled
                ? "AppArmor is available as a system security layer. Profile coverage will be evaluated in a later provider."
                : "Review whether AppArmor should be enabled for this system. Pulse did not alter kernel or profile settings.",
            parameterPath);
    }
}
