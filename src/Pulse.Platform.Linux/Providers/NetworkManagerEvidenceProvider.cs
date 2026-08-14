using Pulse.Platform.Linux.Platform;

namespace Pulse.Platform.Linux.Providers;

public sealed class NetworkManagerEvidenceProvider(IReadOnlyCommandRunner commandRunner) : ILinuxEvidenceProvider
{
    public string Id => "linux.network-manager";

    public async Task<EvidenceResult> CollectAsync(CancellationToken cancellationToken = default)
    {
        var result = await commandRunner.RunAsync("nmcli",
            ["--terse", "--fields", "STATE,CONNECTIVITY", "general", "status"],
            cancellationToken: cancellationToken);
        if (!result.Started)
        {
            return new(Id, "Network manager", EvidenceState.Informational,
                "NetworkManager command-line tools are not installed or not available to Pulse.",
                "This may be normal when the desktop uses another network stack. Interface, route, and DNS evidence remain available independently.",
                "nmcli --terse --fields STATE,CONNECTIVITY general status");
        }

        if (result.TimedOut || result.ExitCode != 0 || string.IsNullOrWhiteSpace(result.StandardOutput))
        {
            return new(Id, "Network manager", EvidenceState.Informational,
                "NetworkManager did not provide readable general connection state.",
                "This is incomplete management-layer coverage, not proof of a network problem. Review the desktop network panel only if connection behavior is unexpected.",
                "nmcli --terse --fields STATE,CONNECTIVITY general status");
        }

        var status = result.StandardOutput.Trim().Replace(':', '/').ToLowerInvariant();
        if (status.StartsWith("connected", StringComparison.Ordinal))
        {
            return new(Id, "Network manager", EvidenceState.Healthy,
                $"NetworkManager reports {status}.",
                "The desktop network-management layer reports a connected state. Pulse made no connection changes.",
                "nmcli --terse --fields STATE,CONNECTIVITY general status");
        }

        if (status.Contains("disconnected", StringComparison.Ordinal) ||
            status.Contains("asleep", StringComparison.Ordinal) ||
            status.Contains("disconnecting", StringComparison.Ordinal))
        {
            return new(Id, "Network manager", EvidenceState.Attention,
                $"NetworkManager reports {status}.",
                "If the computer is not intentionally offline, review Wi-Fi, Ethernet, airplane-mode, or VPN state in the desktop network panel.",
                "nmcli --terse --fields STATE,CONNECTIVITY general status");
        }

        return new(Id, "Network manager", EvidenceState.Informational,
            $"NetworkManager reports {status}.",
            "The management-layer state is available but not classified as a current failure. Review the desktop network panel if behavior is unexpected.",
            "nmcli --terse --fields STATE,CONNECTIVITY general status");
    }
}
