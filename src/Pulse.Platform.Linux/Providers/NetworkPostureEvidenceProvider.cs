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

        if (activeInterfaces.Count == 0)
        {
            return new(Id, "Network posture", EvidenceState.Attention,
                "Pulse found no active non-loopback network interface.",
                "If this computer is intentionally offline, no action is needed. Otherwise review Wi-Fi, Ethernet, airplane-mode, or NetworkManager settings. Pulse performed no connection test and made no changes.",
                "ip -json link show up");
        }

        var interfaceSummary = string.Join(", ", activeInterfaces.Take(4));
        return new(Id, "Network posture", EvidenceState.Healthy,
            $"Active non-loopback interface(s): {interfaceSummary}.",
            "Interface link state looks available. Route, network-manager, and DNS posture are evaluated separately; Pulse performed no connection or speed test.",
            "ip -json link show up");
    }

    private static bool Succeeded(ReadOnlyCommandResult result) =>
        result.Started && !result.TimedOut && result.ExitCode == 0;

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
