namespace Pulse.Platform.Linux.Providers;

public sealed class OsReleaseEvidenceProvider : ILinuxEvidenceProvider
{
    public string Id => "linux.os-release";

    public async Task<EvidenceResult> CollectAsync(CancellationToken cancellationToken = default)
    {
        var lines = await File.ReadAllLinesAsync("/etc/os-release", cancellationToken);
        var prettyName = lines.FirstOrDefault(line => line.StartsWith("PRETTY_NAME=", StringComparison.Ordinal))?
            .Split('=', 2)[1].Trim().Trim('"') ?? "Unknown distribution";
        return new(Id, "Operating system", EvidenceState.Healthy, prettyName,
            "Pulse verified the operating-system identity without changing the system.",
            "/etc/os-release");
    }
}
