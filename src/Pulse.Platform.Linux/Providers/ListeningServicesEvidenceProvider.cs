using Pulse.Platform.Linux.Platform;

namespace Pulse.Platform.Linux.Providers;

public sealed class ListeningServicesEvidenceProvider(IReadOnlyCommandRunner commandRunner) : ILinuxEvidenceProvider
{
    public string Id => "linux.listening-services";

    public async Task<EvidenceResult> CollectAsync(CancellationToken cancellationToken = default)
    {
        var result = await commandRunner.RunAsync("ss", ["-H", "-lntu"], cancellationToken: cancellationToken);
        if (!result.Started)
        {
            return new(Id, "Listening services", EvidenceState.Informational,
                "The optional ss tool is not available, so Pulse did not inventory listening network sockets.",
                "No failure is implied. Install the standard iproute2 tools only if this additional read-only coverage is wanted.",
                "ss -H -lntu");
        }

        if (result.TimedOut || result.ExitCode != 0)
        {
            return EvidenceResult.Unavailable(Id, "Listening services", "ss -H -lntu",
                "The local listening-socket inventory did not complete. Pulse requested no process names or payload data.");
        }

        var localAddresses = result.StandardOutput
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(line => line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            .Where(parts => parts.Length >= 2)
            .Select(parts => parts[^2])
            .ToArray();
        if (localAddresses.Length == 0)
        {
            return new(Id, "Listening services", EvidenceState.Healthy,
                "No TCP or UDP listening sockets were reported to Pulse.",
                "No action is required. Pulse made no network connection and requested no process details.",
                "ss -H -lntu");
        }

        var wildcardCount = localAddresses.Count(IsWildcardAddress);
        if (wildcardCount > 0)
        {
            return new(Id, "Listening services", EvidenceState.Informational,
                $"Pulse counted {localAddresses.Length} TCP/UDP listening socket(s); {wildcardCount} listen on all local addresses.",
                "Listening services are not automatically unhealthy. If any are unexpected, review installed server, sharing, remote-access, and firewall settings. Pulse does not retain endpoint or process details.",
                "ss -H -lntu");
        }

        return new(Id, "Listening services", EvidenceState.Healthy,
            $"Pulse counted {localAddresses.Length} TCP/UDP listening socket(s), all bound to specific local addresses.",
            "No action is required unless a local service is unexpected. Pulse made no connection and retained no endpoint or process details.",
            "ss -H -lntu");
    }

    private static bool IsWildcardAddress(string address) =>
        address.StartsWith("0.0.0.0:", StringComparison.Ordinal) ||
        address.StartsWith("[::]:", StringComparison.Ordinal) ||
        address.StartsWith("*:", StringComparison.Ordinal) ||
        address.StartsWith(":::", StringComparison.Ordinal);
}
