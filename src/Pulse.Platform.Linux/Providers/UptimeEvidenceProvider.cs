namespace Pulse.Platform.Linux.Providers;

public sealed class UptimeEvidenceProvider(string uptimePath = "/proc/uptime") : ILinuxEvidenceProvider
{
    public string Id => "linux.uptime";

    public async Task<EvidenceResult> CollectAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(uptimePath))
        {
            return EvidenceResult.Unavailable(Id, "System uptime", uptimePath,
                "The standard Linux uptime source is not present.");
        }

        var text = await File.ReadAllTextAsync(uptimePath, cancellationToken);
        var first = text.Split([' ', '\t', '\n'], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        if (!double.TryParse(first, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var seconds) || seconds < 0)
        {
            return EvidenceResult.Unavailable(Id, "System uptime", uptimePath,
                "The uptime value was not in the expected Linux format.");
        }

        var uptime = TimeSpan.FromSeconds(seconds);
        var summary = uptime.TotalDays >= 1
            ? $"The system has been running for {uptime.Days} day(s), {uptime.Hours} hour(s), and {uptime.Minutes} minute(s)."
            : $"The system has been running for {uptime.Hours} hour(s) and {uptime.Minutes} minute(s).";
        return new(Id, "System uptime", EvidenceState.Informational, summary,
            "Uptime is context for reliability evidence, not a recommendation to restart. Pulse will advise separately when DNF reports a restart hint.",
            uptimePath);
    }
}
