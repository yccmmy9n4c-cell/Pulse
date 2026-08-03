namespace Pulse.Platform.Linux.Providers;

public sealed class RestartRequirementEvidenceProvider(
    string requiredPath = "/var/run/reboot-required",
    string packagesPath = "/var/run/reboot-required.pkgs") : ILinuxEvidenceProvider
{
    public string Id => "linux.reboot-required";

    public async Task<EvidenceResult> CollectAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(requiredPath))
        {
            return new(Id, "Restart requirement", EvidenceState.Healthy,
                "The standard Debian restart-required marker is not present.",
                "No package-triggered restart is currently indicated by this marker.", requiredPath);
        }

        var packageNames = Array.Empty<string>();
        if (File.Exists(packagesPath))
        {
            packageNames = (await File.ReadAllLinesAsync(packagesPath, cancellationToken))
                .Select(line => line.Trim())
                .Where(line => line.Length > 0)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
        }

        var detail = packageNames.Length == 0
            ? "Debian's restart-required marker is present."
            : $"A restart is requested after package changes involving {string.Join(", ", packageNames.Take(4))}{(packageNames.Length > 4 ? $" and {packageNames.Length - 4} more" : string.Empty)}.";
        return new(Id, "Restart requirement", EvidenceState.Attention, detail,
            "Save your work and restart at a convenient time. Pulse will not restart the computer automatically.",
            File.Exists(packagesPath) ? $"{requiredPath}, {packagesPath}" : requiredPath);
    }
}
