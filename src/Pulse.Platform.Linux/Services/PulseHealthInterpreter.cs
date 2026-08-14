using Pulse.Platform.Linux.Providers;

namespace Pulse.Platform.Linux.Services;

public sealed record PulseHealthSummary(
    int Score,
    string State,
    string Detail,
    int AttentionCount,
    int UnavailableCount);

public static class PulseHealthInterpreter
{
    public static PulseHealthSummary Interpret(IReadOnlyList<EvidenceResult> evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        if (evidence.Count == 0)
        {
            return new(0, "Assessment Ready", "Run a read-only assessment to establish the current state.", 0, 0);
        }

        var attention = evidence.Count(item => item.State == EvidenceState.Attention);
        var unavailable = evidence.Count(item => item.State == EvidenceState.Unavailable);
        // Unavailable evidence represents coverage, not negative health evidence.
        // Keep it visible in the state/detail while reserving score deductions for actual review findings.
        var score = Math.Clamp(100 - (attention * 12), 0, 100);

        if (attention > 0 && score > 79)
        {
            score = 79;
        }

        var state = score switch
        {
            >= 95 when unavailable == 0 => "Optimized",
            >= 80 => "Healthy",
            >= 65 => "Attention Recommended",
            >= 40 => "Degraded",
            _ => "Critical"
        };
        var detail = state switch
        {
            "Optimized" => "No review items were found in the available Linux evidence.",
            "Healthy" => "The available evidence looks healthy, with some coverage unavailable.",
            "Attention Recommended" => "Pulse found evidence that deserves review.",
            "Degraded" => "Several findings may affect system health or reliability.",
            _ => "The available evidence contains multiple serious review items."
        };

        return new(score, state, detail, attention, unavailable);
    }
}
