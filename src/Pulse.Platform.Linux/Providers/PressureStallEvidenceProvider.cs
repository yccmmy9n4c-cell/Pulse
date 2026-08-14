using System.Globalization;

namespace Pulse.Platform.Linux.Providers;

public enum PressureResource
{
    Cpu,
    Memory,
    Io
}

public sealed class PressureStallEvidenceProvider(
    PressureResource resource,
    string? pressurePath = null) : ILinuxEvidenceProvider
{
    private string Path => pressurePath ?? $"/proc/pressure/{resource.ToString().ToLowerInvariant()}";
    public string Id => $"linux.performance-{resource.ToString().ToLowerInvariant()}-pressure";

    public async Task<EvidenceResult> CollectAsync(CancellationToken cancellationToken = default)
    {
        var title = $"{(resource == PressureResource.Io ? "I/O" : resource.ToString())} pressure";
        if (!File.Exists(Path))
        {
            return EvidenceResult.Unavailable(Id, title, Path,
                "Linux Pressure Stall Information is not available for this resource.");
        }

        var lines = await File.ReadAllLinesAsync(Path, cancellationToken);
        var some = ReadAverage(lines, "some");
        var full = ReadAverage(lines, "full");
        if (some is null)
        {
            return EvidenceResult.Unavailable(Id, title, Path,
                "The pressure data was not in the expected Linux PSI format.");
        }

        var state = Classify(some.Value, full);
        var fullText = full is null ? "not exposed" : $"{full.Value:F2}%";
        return new(Id, title, state,
            $"Over the last 60 seconds, partial stall pressure averaged {some.Value:F2}% and full stall pressure was {fullText}.",
            state == EvidenceState.Attention
                ? "Pulse detected sustained resource waiting. Reassess after intentional heavy work completes; if responsiveness remains poor, review workload and storage or memory evidence before changing settings."
                : "PSI measures time tasks were delayed waiting for this resource. A single reading is context; repeated pressure alongside visible slowdown is more meaningful.",
            Path);
    }

    private EvidenceState Classify(double some, double? full)
    {
        var fullValue = full ?? 0;
        return resource switch
        {
            PressureResource.Cpu when some >= 25 => EvidenceState.Attention,
            PressureResource.Cpu when some >= 10 => EvidenceState.Informational,
            PressureResource.Memory when some >= 10 || fullValue >= 2 => EvidenceState.Attention,
            PressureResource.Memory when some >= 3 || fullValue >= 0.5 => EvidenceState.Informational,
            PressureResource.Io when some >= 20 || fullValue >= 5 => EvidenceState.Attention,
            PressureResource.Io when some >= 5 || fullValue >= 1 => EvidenceState.Informational,
            _ => EvidenceState.Healthy
        };
    }

    private static double? ReadAverage(IEnumerable<string> lines, string category)
    {
        var line = lines.FirstOrDefault(value => value.StartsWith(category + " ", StringComparison.Ordinal));
        var token = line?.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault(value => value.StartsWith("avg60=", StringComparison.Ordinal));
        return token is not null && double.TryParse(token[6..], NumberStyles.Float,
            CultureInfo.InvariantCulture, out var value) ? value : null;
    }
}
