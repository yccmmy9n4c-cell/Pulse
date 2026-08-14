namespace Pulse.Platform.Linux.Providers;

public sealed class MemoryAvailabilityEvidenceProvider(string memoryInfoPath = "/proc/meminfo") : ILinuxEvidenceProvider
{
    public string Id => "linux.performance-memory";

    public async Task<EvidenceResult> CollectAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(memoryInfoPath))
        {
            return EvidenceResult.Unavailable(Id, "Available memory", memoryInfoPath,
                "The standard Linux memory-information source is not present.");
        }

        var values = (await File.ReadAllLinesAsync(memoryInfoPath, cancellationToken))
            .Select(line => line.Split(':', 2, StringSplitOptions.TrimEntries))
            .Where(parts => parts.Length == 2)
            .ToDictionary(parts => parts[0], parts => ReadKilobytes(parts[1]), StringComparer.Ordinal);
        if (!values.TryGetValue("MemTotal", out var totalKb) || totalKb <= 0 ||
            !values.TryGetValue("MemAvailable", out var availableKb) || availableKb < 0)
        {
            return EvidenceResult.Unavailable(Id, "Available memory", memoryInfoPath,
                "MemTotal or MemAvailable was not readable in the expected Linux format.");
        }

        var availablePercent = availableKb * 100d / totalKb;
        var state = availablePercent < 8
            ? EvidenceState.Attention
            : availablePercent < 15
                ? EvidenceState.Informational
                : EvidenceState.Healthy;
        return new(Id, "Available memory", state,
            $"Linux reports {availableKb / 1024d / 1024d:F1} GiB available of {totalKb / 1024d / 1024d:F1} GiB ({availablePercent:F0}% available).",
            state == EvidenceState.Attention
                ? "Available memory is very low. Save work and review active applications before closing anything; Linux intentionally uses otherwise-idle memory for cache."
                : "Pulse uses MemAvailable rather than treating Linux filesystem cache as consumed memory. Repeated low availability matters more than one reading.",
            memoryInfoPath);
    }

    private static long ReadKilobytes(string value)
    {
        var token = value.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        return long.TryParse(token, out var parsed) ? parsed : -1;
    }
}
