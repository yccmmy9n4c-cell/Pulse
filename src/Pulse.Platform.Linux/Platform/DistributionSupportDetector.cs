using System.Runtime.InteropServices;

namespace Pulse.Platform.Linux.Platform;

public sealed class DistributionSupportDetector
{
    private static readonly HashSet<string> VerifiedIds = new(StringComparer.OrdinalIgnoreCase) { "fedora" };

    public DistributionSupportResult Detect(string path = "/etc/os-release")
    {
        var architecture = RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant();
        if (!OperatingSystem.IsLinux())
        {
            return new(DistributionSupportLevel.Unsupported, "unknown", "unknown", RuntimeInformation.OSDescription,
                architecture, "Pulse Supernova Linux FE runs only on verified Fedora-family desktop systems.");
        }

        if (!File.Exists(path))
        {
            return new(DistributionSupportLevel.Unsupported, "unknown", "unknown", "Unknown Linux distribution",
                architecture, "Pulse could not read /etc/os-release, so it cannot verify this system safely.");
        }

        var values = Parse(File.ReadAllLines(path));
        var id = Value(values, "ID", "unknown");
        var version = Value(values, "VERSION_ID", "unknown");
        var display = Value(values, "PRETTY_NAME", $"{id} {version}");
        var idLike = Value(values, "ID_LIKE", string.Empty);

        if (VerifiedIds.Contains(id))
        {
            return new(DistributionSupportLevel.Supported, id, version, display, architecture,
                "This distribution is inside the FE support boundary. Assessment is read-only; physical Fedora acceptance is tracked separately.");
        }

        var looksFedoraCompatible = idLike.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Any(value => value.Equals("fedora", StringComparison.OrdinalIgnoreCase));

        if (looksFedoraCompatible)
        {
            return new(DistributionSupportLevel.UnverifiedDerivative, id, version, display, architecture,
                "This system reports Fedora compatibility, but Pulse has not verified this derivative. Assessment is disabled until it is added to the FE compatibility matrix.");
        }

        return new(DistributionSupportLevel.Unsupported, id, version, display, architecture,
            "This distribution is outside the FE scope. Debian-family, RHEL, Arch, BSD, and unrelated systems require their own verified Pulse edition.");
    }

    private static Dictionary<string, string> Parse(IEnumerable<string> lines) => lines
        .Where(line => !string.IsNullOrWhiteSpace(line) && !line.TrimStart().StartsWith('#') && line.Contains('='))
        .Select(line => line.Split('=', 2))
        .GroupBy(parts => parts[0].Trim(), StringComparer.OrdinalIgnoreCase)
        .ToDictionary(group => group.Key, group => group.Last()[1].Trim().Trim('"', '\''), StringComparer.OrdinalIgnoreCase);

    private static string Value(IReadOnlyDictionary<string, string> values, string key, string fallback) =>
        values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : fallback;
}
