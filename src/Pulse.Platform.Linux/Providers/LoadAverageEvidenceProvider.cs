using System.Globalization;

namespace Pulse.Platform.Linux.Providers;

public sealed class LoadAverageEvidenceProvider(
    string loadAveragePath = "/proc/loadavg",
    int? logicalProcessorCount = null) : ILinuxEvidenceProvider
{
    public string Id => "linux.performance-load";

    public async Task<EvidenceResult> CollectAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(loadAveragePath))
        {
            return EvidenceResult.Unavailable(Id, "Sustained system load", loadAveragePath,
                "The standard Linux load-average source is not present.");
        }

        var fields = (await File.ReadAllTextAsync(loadAveragePath, cancellationToken))
            .Split([' ', '\t', '\n'], StringSplitOptions.RemoveEmptyEntries);
        if (fields.Length < 3 ||
            !double.TryParse(fields[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var oneMinute) ||
            !double.TryParse(fields[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var fiveMinute) ||
            !double.TryParse(fields[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var fifteenMinute))
        {
            return EvidenceResult.Unavailable(Id, "Sustained system load", loadAveragePath,
                "The load-average values were not in the expected Linux format.");
        }

        var processors = Math.Max(1, logicalProcessorCount ?? Environment.ProcessorCount);
        var sustainedRatio = fifteenMinute / processors;
        var state = sustainedRatio >= 1.25
            ? EvidenceState.Attention
            : sustainedRatio >= 0.80
                ? EvidenceState.Informational
                : EvidenceState.Healthy;
        var guidance = state == EvidenceState.Attention
            ? "The 15-minute load is above available logical-processor capacity. Review running work before ending a task; background updates, compilation, or other intentional workloads may explain it."
            : "Load averages are workload context, not proof that an application is faulty. Compare repeated assessments if the computer feels persistently slow.";
        return new(Id, "Sustained system load", state,
            $"Load averages are {oneMinute:F2}, {fiveMinute:F2}, and {fifteenMinute:F2} across {processors} logical processor(s).",
            guidance, loadAveragePath);
    }
}
