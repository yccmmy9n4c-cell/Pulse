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
            "--output-fields=PRIORITY,_SYSTEMD_UNIT,_SYSTEMD_USER_UNIT,SYSLOG_IDENTIFIER,_COMM,MESSAGE", "--lines=100"
        };
        var result = await commandRunner.RunAsync("journalctl", arguments, TimeSpan.FromSeconds(12), cancellationToken);
        if (!result.Started || result.TimedOut || result.ExitCode != 0)
        {
            return EvidenceResult.Unavailable(Id, "Current-boot reliability", "journalctl current boot priority 0..3",
                result.TimedOut
                    ? "The journal query timed out."
                    : "The current user could not read the requested journal evidence.");
        }

        var readableEntries = new List<JournalSignal>();
        foreach (var line in result.StandardOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            try
            {
                using var document = JsonDocument.Parse(line);
                readableEntries.Add(ReadSignal(document.RootElement));
            }
            catch (JsonException)
            {
                // One malformed journal row does not invalidate other readable reliability evidence.
            }
        }

        if (readableEntries.Count == 0)
        {
            return new(Id, "Current-boot reliability", EvidenceState.Healthy,
                "No error-or-higher journal entries were readable for the current boot.",
                "This is a current-boot, user-readable view. It is not proof that every service log is accessible.",
                "journalctl --boot=0 --priority=0..3 --output=json --output-fields=metadata-only --lines=100");
        }

        // MESSAGE is read only long enough to classify exact known-benign diagnostics and
        // consolidate identical rows. It is never returned in EvidenceResult or persisted.
        var actionableEntries = readableEntries
            .Where(entry => !IsKnownBenignDiagnostic(entry))
            .GroupBy(entry => (entry.Priority, entry.Source, entry.Message))
            .Select(group => new ConsolidatedJournalSignal(
                group.Key.Priority,
                group.Key.Source,
                group.Count()))
            .ToArray();

        if (actionableEntries.Length == 0)
        {
            return new(Id, "Current-boot reliability", EvidenceState.Healthy,
                $"No actionable error-or-higher journal events remain after filtering {readableEntries.Count} known-benign diagnostic event(s).",
                "Pulse recognized exact non-failure diagnostic patterns. It retained no journal message bodies and made no system changes.",
                "journalctl --boot=0 --priority=0..3 --output=json --output-fields=classification-only --lines=100");
        }

        var severeCount = actionableEntries.Count(entry => entry.Priority <= 2);
        var actionableOccurrences = actionableEntries.Sum(entry => entry.Occurrences);
        var duplicateCount = actionableOccurrences - actionableEntries.Length;
        var filteredCount = readableEntries.Count - actionableOccurrences;
        var sources = actionableEntries
            .GroupBy(entry => entry.Source, StringComparer.Ordinal)
            .Select(group => new
            {
                Source = group.Key,
                Occurrences = group.Sum(entry => entry.Occurrences)
            })
            .OrderByDescending(item => item.Occurrences)
            .ThenBy(item => item.Source, StringComparer.Ordinal)
            .Take(4)
            .Select(item => $"{item.Source} ({item.Occurrences})")
            .ToArray();
        var sourceSummary = sources.Length == 0 ? "source unavailable" : string.Join(", ", sources);
        // A burst of one repeated real failure still deserves review, even though
        // duplicate rows are consolidated for presentation and scoring clarity.
        var needsReview = severeCount > 0 || actionableEntries.Length >= 5 || actionableOccurrences >= 10;
        var queryCount = readableEntries.Count >= 100 ? "at least 100" : readableEntries.Count.ToString();
        var consolidationSummary = duplicateCount > 0
            ? $" {duplicateCount} identical repeat(s) were consolidated."
            : string.Empty;
        var filteredSummary = filteredCount > 0
            ? $" {filteredCount} exact known-benign diagnostic event(s) were excluded."
            : string.Empty;

        return new(Id, "Current-boot reliability",
            needsReview ? EvidenceState.Attention : EvidenceState.Informational,
            $"The readable current-boot journal returned {queryCount} error-or-higher event(s), representing {actionableEntries.Length} distinct actionable signal(s); {severeCount} are critical-or-higher. Leading sources: {sourceSummary}.{consolidationSummary}{filteredSummary}",
            needsReview
                ? "Review the named service or application logs before taking corrective action. Pulse reports counts and sources without copying potentially sensitive journal messages, and it made no changes."
                : "A small number of isolated errors can occur during a normal boot. Reassess later and review the source if the count grows or the same issue repeats.",
            "journalctl --boot=0 --priority=0..3 --output=json --output-fields=classification-only --lines=100");
    }

    private static JournalSignal ReadSignal(JsonElement entry)
    {
        var priorityText = Value(entry, "PRIORITY");
        var priority = int.TryParse(priorityText, out var parsed) ? parsed : 3;
        var systemUnit = Value(entry, "_SYSTEMD_UNIT");
        var userUnit = Value(entry, "_SYSTEMD_USER_UNIT");
        var identifier = Value(entry, "SYSLOG_IDENTIFIER");
        var command = Value(entry, "_COMM");
        var source = IsGenericUserManager(systemUnit)
            ? identifier ?? command ?? userUnit ?? systemUnit ?? "unknown"
            : systemUnit ?? userUnit ?? identifier ?? command ?? "unknown";
        return new(priority, source, Value(entry, "MESSAGE") ?? string.Empty);
    }

    private static bool IsGenericUserManager(string? unit) =>
        unit is not null && unit.StartsWith("user@", StringComparison.Ordinal) &&
        unit.EndsWith(".service", StringComparison.Ordinal);

    private static bool IsKnownBenignDiagnostic(JournalSignal signal)
    {
        if (!signal.Source.Contains("powerdevil", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return signal.Message.Contains("Time since library initialized:", StringComparison.Ordinal) ||
               (signal.Message.Contains("Extra delay starting dw_start_watch_displays:", StringComparison.Ordinal) &&
                signal.Message.Contains("millisec", StringComparison.OrdinalIgnoreCase));
    }

    private static string? Value(JsonElement entry, string propertyName) =>
        entry.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private sealed record JournalSignal(int Priority, string Source, string Message);

    private sealed record ConsolidatedJournalSignal(int Priority, string Source, int Occurrences);
}
