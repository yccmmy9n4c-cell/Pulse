using System.Text.Json;
using Pulse.Platform.Linux.Platform;

namespace Pulse.Platform.Linux.Providers;

public sealed class JournalReliabilityEvidenceProvider(IReadOnlyCommandRunner commandRunner) : ILinuxEvidenceProvider
{
    public string Id => "linux.journal-reliability";

    public async Task<EvidenceResult> CollectAsync(CancellationToken cancellationToken = default)
    {
        var arguments = new[]
        {
            "--boot=0", "--priority=0..3", "--no-pager", "--quiet", "--output=json",
            "--output-fields=PRIORITY,_SYSTEMD_UNIT,SYSLOG_IDENTIFIER,_COMM", "--lines=100"
        };
        var result = await commandRunner.RunAsync("journalctl", arguments, TimeSpan.FromSeconds(12), cancellationToken);
        if (!result.Started || result.TimedOut || result.ExitCode != 0)
        {
            return EvidenceResult.Unavailable(Id, "Current-boot reliability", "journalctl current boot priority 0..3",
                result.TimedOut
                    ? "The journal query timed out."
                    : "The current user could not read the requested journal evidence.");
        }

        var entries = new List<JournalSignal>();
        foreach (var line in result.StandardOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            try
            {
                using var document = JsonDocument.Parse(line);
                entries.Add(ReadSignal(document.RootElement));
            }
            catch (JsonException)
            {
                // One malformed journal row does not invalidate other readable reliability evidence.
            }
        }

        if (entries.Count == 0)
        {
            return new(Id, "Current-boot reliability", EvidenceState.Healthy,
                "No error-or-higher journal entries were readable for the current boot.",
                "This is a current-boot, user-readable view. It is not proof that every service log is accessible.",
                "journalctl --boot=0 --priority=0..3 --output=json --output-fields=metadata-only --lines=100");
        }

        var severeCount = entries.Count(entry => entry.Priority <= 2);
        var sources = entries.Select(entry => entry.Source)
            .Where(source => !string.IsNullOrWhiteSpace(source))
            .GroupBy(source => source, StringComparer.Ordinal)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key, StringComparer.Ordinal)
            .Take(4)
            .Select(group => $"{group.Key} ({group.Count()})")
            .ToArray();
        var sourceSummary = sources.Length == 0 ? "source unavailable" : string.Join(", ", sources);
        var needsReview = severeCount > 0 || entries.Count >= 5;

        return new(Id, "Current-boot reliability",
            needsReview ? EvidenceState.Attention : EvidenceState.Informational,
            $"The readable current-boot journal contains {entries.Count} error-or-higher event(s); {severeCount} are critical-or-higher. Leading sources: {sourceSummary}.",
            needsReview
                ? "Review the named service or application logs before taking corrective action. Pulse reports counts and sources without copying potentially sensitive journal messages, and it made no changes."
                : "A small number of isolated errors can occur during a normal boot. Reassess later and review the source if the count grows or the same issue repeats.",
            "journalctl --boot=0 --priority=0..3 --output=json --output-fields=metadata-only --lines=100");
    }

    private static JournalSignal ReadSignal(JsonElement entry)
    {
        var priorityText = Value(entry, "PRIORITY");
        var priority = int.TryParse(priorityText, out var parsed) ? parsed : 3;
        var source = Value(entry, "_SYSTEMD_UNIT") ?? Value(entry, "SYSLOG_IDENTIFIER") ??
                     Value(entry, "_COMM") ?? "unknown";
        return new(priority, source);
    }

    private static string? Value(JsonElement entry, string propertyName) =>
        entry.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private sealed record JournalSignal(int Priority, string Source);
}
