namespace Pulse.Platform.Linux.Providers;

public sealed class DnsConfigurationEvidenceProvider(string resolvConfPath = "/etc/resolv.conf") : ILinuxEvidenceProvider
{
    public string Id => "linux.dns-configuration";

    public Task<EvidenceResult> CollectAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            if (!File.Exists(resolvConfPath))
            {
                return Task.FromResult(EvidenceResult.Unavailable(Id, "DNS configuration", resolvConfPath,
                    "The standard resolver configuration file is missing or its target is unavailable."));
            }

            var nameServers = File.ReadLines(resolvConfPath)
                .Select(line => line.Trim())
                .Where(line => !line.StartsWith('#') && line.StartsWith("nameserver", StringComparison.OrdinalIgnoreCase))
                .Select(line => line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
                .Where(parts => parts.Length >= 2)
                .Select(parts => parts[1])
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (nameServers.Length == 0)
            {
                return Task.FromResult(new EvidenceResult(Id, "DNS configuration", EvidenceState.Attention,
                    "No nameserver entry was found in the standard resolver configuration.",
                    "Name resolution may be managed elsewhere, but if host names are failing review the desktop connection's DNS settings. Pulse performed no DNS query.",
                    resolvConfPath));
            }

            var localStub = nameServers.Any(address => address.StartsWith("127.", StringComparison.Ordinal) ||
                                                       address.Equals("::1", StringComparison.Ordinal));
            var mode = localStub ? " through a local resolver stub" : string.Empty;
            return Task.FromResult(new EvidenceResult(Id, "DNS configuration", EvidenceState.Healthy,
                $"The resolver configuration contains {nameServers.Length} nameserver entr{(nameServers.Length == 1 ? "y" : "ies")}{mode}.",
                "Local DNS configuration is present. Pulse did not send a lookup or expose resolver addresses in its report.",
                resolvConfPath));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return Task.FromResult(EvidenceResult.Unavailable(Id, "DNS configuration", resolvConfPath,
                $"The resolver configuration could not be read. {ex.Message}"));
        }
    }
}
