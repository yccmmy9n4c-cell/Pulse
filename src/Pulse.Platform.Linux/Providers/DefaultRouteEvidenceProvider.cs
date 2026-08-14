using System.Text.Json;
using Pulse.Platform.Linux.Platform;

namespace Pulse.Platform.Linux.Providers;

public sealed class DefaultRouteEvidenceProvider(IReadOnlyCommandRunner commandRunner) : ILinuxEvidenceProvider
{
    public string Id => "linux.default-route";

    public async Task<EvidenceResult> CollectAsync(CancellationToken cancellationToken = default)
    {
        var ipv4 = await commandRunner.RunAsync("ip", ["-json", "-4", "route", "show", "default"],
            cancellationToken: cancellationToken);
        var ipv6 = await commandRunner.RunAsync("ip", ["-json", "-6", "route", "show", "default"],
            cancellationToken: cancellationToken);
        var ipv4Count = CountRoutes(ipv4);
        var ipv6Count = CountRoutes(ipv6);

        if (ipv4Count < 0 && ipv6Count < 0)
        {
            return EvidenceResult.Unavailable(Id, "Default route", "ip -json -4/-6 route show default",
                "The local route table could not be read. Pulse did not test an outside address.");
        }

        if (ipv4Count <= 0 && ipv6Count <= 0)
        {
            if (ipv4Count < 0 || ipv6Count < 0)
            {
                return EvidenceResult.Unavailable(Id, "Default route", "ip -json -4/-6 route show default",
                    "Pulse could read no default route and at least one address-family query was unavailable. Pulse did not test an outside address.");
            }

            return new(Id, "Default route", EvidenceState.Attention,
                "No readable IPv4 or IPv6 default route was found.",
                "The computer may be intentionally isolated. If broader network access is expected, review the active connection and gateway settings. Pulse did not ping or contact the internet.",
                "ip -json -4 route show default; ip -json -6 route show default");
        }

        var families = new List<string>();
        if (ipv4Count > 0)
        {
            families.Add($"IPv4 ({ipv4Count})");
        }

        if (ipv6Count > 0)
        {
            families.Add($"IPv6 ({ipv6Count})");
        }

        return new(Id, "Default route", EvidenceState.Healthy,
            $"Default route coverage is present for {string.Join(" and ", families)}.",
            "The local route structure looks available. This does not claim that the internet or any outside service is reachable.",
            "ip -json -4 route show default; ip -json -6 route show default");
    }

    private static int CountRoutes(ReadOnlyCommandResult result)
    {
        if (!result.Started || result.TimedOut || result.ExitCode != 0)
        {
            return -1;
        }

        try
        {
            using var document = JsonDocument.Parse(result.StandardOutput);
            return document.RootElement.ValueKind == JsonValueKind.Array
                ? document.RootElement.GetArrayLength()
                : -1;
        }
        catch (JsonException)
        {
            return -1;
        }
    }
}
