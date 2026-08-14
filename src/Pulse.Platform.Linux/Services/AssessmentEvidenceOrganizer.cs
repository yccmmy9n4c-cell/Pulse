using Pulse.Platform.Linux.Providers;

namespace Pulse.Platform.Linux.Services;

public sealed record AssessmentEvidenceSections(
    IReadOnlyList<EvidenceResult> Information,
    IReadOnlyList<EvidenceResult> Healthy,
    IReadOnlyList<EvidenceResult> Guidance)
{
    public int TotalCount => Information.Count + Healthy.Count + Guidance.Count;
}

public static class AssessmentEvidenceOrganizer
{
    public static AssessmentEvidenceSections Organize(IReadOnlyList<EvidenceResult> results)
    {
        ArgumentNullException.ThrowIfNull(results);
        var information = results.Where(item => item.State == EvidenceState.Informational)
            .OrderBy(item => item.Title, StringComparer.Ordinal).ToArray();
        var healthy = results.Where(item => item.State == EvidenceState.Healthy)
            .OrderBy(item => item.Title, StringComparer.Ordinal).ToArray();
        var guidance = results.Where(item => item.State is EvidenceState.Attention or EvidenceState.Unavailable)
            .OrderBy(item => item.State == EvidenceState.Attention ? 0 : 1)
            .ThenBy(item => item.Title, StringComparer.Ordinal).ToArray();
        return new(information, healthy, guidance);
    }
}
