using System.Text.Json;
using Pulse.Platform.Linux.Platform;

namespace Pulse.Platform.Linux.Providers;

public sealed class NetworkPostureEvidenceProvider(IReadOnlyCommandRunner commandRunner) : ILinuxEvidenceProvider
{
    public string Id => "linux.network-posture";

    public async Task<EvidenceResult> CollectAsync(CancellationToken cancellationToken = default)
    {
        var links = await commandRunner.RunAsync("ip", ["-json", "link", "show", "up"],
            cancellationToken: cancellationToken);
        if (!Succeeded(links))
        {
            return EvidenceResult.Unavailable(Id, "Network posture", "ip -json link show up",
                links.TimedOut ? "The local interface query timed out." : "The ip command could not provide local interface state.");
        }

        IReadOnlyList<string> activeInterfaces;
        try
        {
            activeInterfaces = ReadActiveInterfaces(links.StandardOutput);
        }
        catch (JsonException ex)
        {
            return EvidenceResult.Unavailable(Id, "Network posture", "ip -json link show up", ex.Message);
        }

        var ipv4Route = await commandRunner.RunAsync("ip", ["-json", "-4", "route", "show", "default"],
            cancellationToken: cancellationToken);
        var ipv6Route = await commandRunner.RunAsync("ip", ["-json", "-6", "route", "show", "default"],
            cancellationToken: cancellationToken);
        var hasDefaultRoute = HasRoute(ipv4Route) || HasRoute(ipv6Route);

        var networkManager = await commandRunner.RunAsync("nmcli", ["--terse", "--fields", "STATE,CONNECTIVITY", "general", "status"],
            cancellationToken: cancellationToken);
        var managerSummary = Succeeded(networkManager) && !string.IsNullOrWhiteSpace(networkManager.StandardOutput)
            ? $" NetworkManager reports {networkManager.StandardOutput.Trim().Replace(':', '/').ToLowerInvariant()}."
            : string.Empty;

        if (activeInterfaces.Count == 0)
        {
            return new(Id, "Network posture", EvidenceState.Attention,
                "Pulse found no active non-loopback network interface.",
                "If this computer is intentionally offline, no action is needed. Otherwise review Wi-Fi, Ethernet, airplane-mode, or NetworkManager settings. Pulse performed no connection test and made no changes.",
                "ip -json link show up");
        }

        var interfaceSummary = string.Join(", ", activeInterfaces.Take(4));
        if (!hasDefaultRoute)
        {
            return new(Id, "Network posture", EvidenceState.Attention,
                $"Active interface(s): {interfaceSummary}, but no readable IPv4 or IPv6 default route was found.{managerSummary}",
                "The system may be intentionally isolated or may lack a route beyond its local network. Review connection settings if broader access is expected. Pulse did not ping, probe, or contact the internet.",
                "ip -json link show up; ip -json -4/-6 route show default");
        }

        return new(Id, "Network posture", EvidenceState.Healthy,
            $"Active interface(s): {interfaceSummary}; a default route is present.{managerSummary}",
            "Local connectivity structure looks available. Pulse did not perform an internet reachability or speed test.",
            "ip -json link show up; ip -json -4/-6 route show default; nmcli general when available");
    }

    private static bool Succeeded(ReadOnlyCommandResult result) =>
        result.Started && !result.TimedOut && result.ExitCode == 0;

    private static bool HasRoute(ReadOnlyCommandResult result)
    {
        if (!Succeeded(result))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(result.StandardOutput);
            return document.RootElement.ValueKind == JsonValueKind.Array &&
                   document.RootElement.GetArrayLength() > 0;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static IReadOnlyList<string> ReadActiveInterfaces(string json)
    {
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var interfaces = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var item in document.RootElement.EnumerateArray())
        {
            if (!item.TryGetProperty("ifname", out var nameElement) ||
                nameElement.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            var name = nameElement.GetString();
            if (!string.IsNullOrWhiteSpace(name) && !name.Equals("lo", StringComparison.OrdinalIgnoreCase))
            {
                interfaces.Add(name);
            }
        }

        return interfaces.ToArray();
    }
}
