using System.Text.Json;
using Pulse.Platform.Linux.Platform;

namespace Pulse.Platform.Linux.Providers;

public sealed class BackupScheduleEvidenceProvider(IReadOnlyCommandRunner commandRunner) : ILinuxEvidenceProvider
{
    private static readonly string[] BackupTerms =
        ["backup", "borg", "restic", "duplicity", "pika", "deja", "backintime", "timeshift"];

    public string Id => "linux.backup-schedule";

    public async Task<EvidenceResult> CollectAsync(CancellationToken cancellationToken = default)
    {
        const string source = "systemctl --user list-timers --all --no-legend --no-pager";
        var result = await commandRunner.RunAsync("systemctl",
            ["--user", "list-timers", "--all", "--no-legend", "--no-pager"],
            TimeSpan.FromSeconds(10), cancellationToken);
        if (!result.Started || result.TimedOut || result.ExitCode != 0)
        {
            return EvidenceResult.Unavailable(Id, "Backup schedules", source,
                result.TimedOut
                    ? "The signed-in user's timer query timed out."
                    : "The signed-in user's systemd timer list was not readable in this session.");
        }

        var timers = result.StandardOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .SelectMany(line => line.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Where(value => value.EndsWith(".timer", StringComparison.Ordinal))
            .Where(value => BackupTerms.Any(term => value.Contains(term, StringComparison.OrdinalIgnoreCase)))
            .Distinct(StringComparer.Ordinal)
            .Take(5)
            .ToArray();

        return timers.Length == 0
            ? new(Id, "Backup schedules", EvidenceState.Informational,
                "No recognized backup-related systemd user timers were detected.",
                "A backup may use an application scheduler, cron, a system timer, or an external service. This result does not prove that backups are unscheduled.", source)
            : new(Id, "Backup schedules", EvidenceState.Informational,
                $"Detected {timers.Length} recognized backup-related user timer(s): {string.Join(", ", timers)}.",
                "A configured timer does not prove that its most recent backup succeeded. Confirm the last successful run in the corresponding backup application.", source);
    }
}

public sealed class BackupActivityEvidenceProvider(IReadOnlyCommandRunner commandRunner) : ILinuxEvidenceProvider
{
    private static readonly string[] BackupTerms =
        ["backup", "borg", "restic", "duplicity", "pika", "deja", "backintime", "timeshift"];

    public string Id => "linux.backup-activity";

    public async Task<EvidenceResult> CollectAsync(CancellationToken cancellationToken = default)
    {
        var arguments = new[]
        {
            "--user", "--since", "30 days ago", "--no-pager", "--quiet", "--output=json",
            "--output-fields=_SYSTEMD_UNIT,SYSLOG_IDENTIFIER,_COMM", "--lines=500"
        };
        const string source = "journalctl --user --since '30 days ago' --output=json --output-fields=metadata-only --lines=500";
        var result = await commandRunner.RunAsync("journalctl", arguments, TimeSpan.FromSeconds(12), cancellationToken);
        if (!result.Started || result.TimedOut || result.ExitCode != 0)
        {
            return EvidenceResult.Unavailable(Id, "Recent backup activity", source,
                result.TimedOut
                    ? "The user-journal query timed out."
                    : "The signed-in user's journal metadata was not readable.");
        }

        var sources = new List<string>();
        foreach (var line in result.StandardOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            try
            {
                using var document = JsonDocument.Parse(line);
                var entry = document.RootElement;
                var name = Value(entry, "_SYSTEMD_UNIT") ?? Value(entry, "SYSLOG_IDENTIFIER") ?? Value(entry, "_COMM");
                if (!string.IsNullOrWhiteSpace(name) && BackupTerms.Any(term => name.Contains(term, StringComparison.OrdinalIgnoreCase)))
                {
                    sources.Add(name);
                }
            }
            catch (JsonException)
            {
                // One malformed journal row does not invalidate other metadata-only evidence.
            }
        }

        if (sources.Count == 0)
        {
            return new(Id, "Recent backup activity", EvidenceState.Informational,
                "No recognized backup-application activity was visible in the signed-in user's last 30 days of journal metadata.",
                "Many backup applications do not record activity in the user journal. Check the application's own history before deciding that no backup ran.", source);
        }

        var leadingSources = sources.GroupBy(value => value, StringComparer.Ordinal)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key, StringComparer.Ordinal)
            .Take(4)
            .Select(group => $"{group.Key} ({group.Count()})")
            .ToArray();
        return new(Id, "Recent backup activity", EvidenceState.Informational,
            $"Detected {sources.Count} backup-related journal metadata event(s) in the last 30 days. Sources: {string.Join(", ", leadingSources)}.",
            "Activity metadata is not a success or restore test. Confirm the latest successful backup in the corresponding application. Pulse does not retain journal message bodies.", source);
    }

    private static string? Value(JsonElement entry, string propertyName) =>
        entry.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}

public sealed class BackupDestinationMountEvidenceProvider(IReadOnlyCommandRunner commandRunner) : ILinuxEvidenceProvider
{
    private static readonly HashSet<string> NetworkFileSystems = new(StringComparer.OrdinalIgnoreCase)
        { "nfs", "nfs4", "cifs", "smb3", "sshfs", "fuse.sshfs", "davfs", "fuse.rclone" };

    public string Id => "linux.backup-destination-mounts";

    public async Task<EvidenceResult> CollectAsync(CancellationToken cancellationToken = default)
    {
        var findMount = await commandRunner.RunAsync("findmnt", ["--json", "--output", "FSTYPE,OPTIONS"],
            TimeSpan.FromSeconds(10), cancellationToken);
        var blockDevices = await commandRunner.RunAsync("lsblk", ["--json", "--output", "RM,TYPE,MOUNTPOINTS"],
            TimeSpan.FromSeconds(10), cancellationToken);
        if ((!findMount.Started || findMount.TimedOut || findMount.ExitCode != 0) &&
            (!blockDevices.Started || blockDevices.TimedOut || blockDevices.ExitCode != 0))
        {
            return EvidenceResult.Unavailable(Id, "Mounted backup destinations", "findmnt and lsblk",
                "Neither mounted-filesystem nor removable-device context was readable.");
        }

        var networkMounts = CountNetworkMounts(findMount.StandardOutput);
        var removableMounts = CountMountedRemovableDevices(blockDevices.StandardOutput);
        return new(Id, "Mounted backup destinations", EvidenceState.Informational,
            $"Detected {networkMounts} mounted network filesystem(s) and {removableMounts} mounted removable storage device(s).",
            "Mounted storage is only destination context. Pulse does not match mounts to private repository paths and cannot infer that any mounted device contains a current or recoverable backup.",
            "findmnt filesystem types and lsblk removable/mounted flags; mount paths excluded");
    }

    private static int CountNetworkMounts(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            return CountObjects(document.RootElement, element =>
                StringValue(element, "fstype") is { } fileSystem && NetworkFileSystems.Contains(fileSystem));
        }
        catch (JsonException)
        {
            return 0;
        }
    }

    private static int CountMountedRemovableDevices(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            return CountObjects(document.RootElement, element =>
                BooleanValue(element, "rm") && HasMountPoint(element));
        }
        catch (JsonException)
        {
            return 0;
        }
    }

    private static int CountObjects(JsonElement element, Func<JsonElement, bool> predicate)
    {
        var count = 0;
        if (element.ValueKind == JsonValueKind.Object)
        {
            if (predicate(element))
            {
                count++;
            }

            foreach (var property in element.EnumerateObject())
            {
                count += CountObjects(property.Value, predicate);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                count += CountObjects(item, predicate);
            }
        }

        return count;
    }

    private static string? StringValue(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static bool BooleanValue(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return false;
        }

        var text = value.ValueKind == JsonValueKind.String ? value.GetString() : null;
        return value.ValueKind == JsonValueKind.True ||
               value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number) && number != 0 ||
               text is not null && (text == "1" || text.Equals("true", StringComparison.OrdinalIgnoreCase));
    }

    private static bool HasMountPoint(JsonElement element, string propertyName = "mountpoints")
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return false;
        }

        return value.ValueKind switch
        {
            JsonValueKind.String => !string.IsNullOrWhiteSpace(value.GetString()),
            JsonValueKind.Array => value.EnumerateArray().Any(item =>
                item.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(item.GetString())),
            _ => false
        };
    }
}

public sealed class SystemSnapshotEvidenceProvider(
    string timeshiftConfiguration = "/etc/timeshift/timeshift.json",
    IReadOnlyList<string>? snapshotRoots = null) : ILinuxEvidenceProvider
{
    private readonly IReadOnlyList<string> _snapshotRoots = snapshotRoots ?? ["/.snapshots", "/timeshift/snapshots"];

    public string Id => "linux.backup-system-snapshots";

    public async Task<EvidenceResult> CollectAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var hasConfiguration = File.Exists(timeshiftConfiguration);
        var scheduledCadences = new List<string>();
        if (hasConfiguration)
        {
            try
            {
                using var document = JsonDocument.Parse(await File.ReadAllTextAsync(timeshiftConfiguration, cancellationToken));
                foreach (var cadence in new[] { "hourly", "boot", "daily", "weekly", "monthly" })
                {
                    if (IsEnabled(document.RootElement, $"schedule_{cadence}"))
                    {
                        scheduledCadences.Add(cadence);
                    }
                }
            }
            catch (JsonException)
            {
                // Configuration presence remains useful even when an unfamiliar format is encountered.
            }
            catch (UnauthorizedAccessException)
            {
                // Report readable path-level posture without requesting elevation.
            }
            catch (IOException)
            {
                // Report readable path-level posture without failing the full assessment.
            }
        }

        var visibleRoots = _snapshotRoots.Count(Directory.Exists);
        var scheduleText = scheduledCadences.Count == 0
            ? "no readable enabled cadence"
            : $"enabled {string.Join(", ", scheduledCadences)} cadence(s)";
        return new(Id, "System snapshot posture", EvidenceState.Informational,
            $"Timeshift configuration: {(hasConfiguration ? "detected" : "not detected")}; {scheduleText}; visible snapshot directories: {visibleRoots}.",
            "System snapshots can help reverse local system changes, but they are not a substitute for an independent backup of user data. Pulse did not create, delete, mount, or inspect a snapshot.",
            "Timeshift configuration keys and standard snapshot-directory presence");
    }

    private static bool IsEnabled(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value))
        {
            return false;
        }

        return value.ValueKind == JsonValueKind.True ||
               value.ValueKind == JsonValueKind.String && value.GetString()?.Equals("true", StringComparison.OrdinalIgnoreCase) == true ||
               value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number) && number != 0;
    }
}

public sealed class BackupRestoreReadinessEvidenceProvider : ILinuxEvidenceProvider
{
    public string Id => "linux.backup-restore-readiness";

    public Task<EvidenceResult> CollectAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new EvidenceResult(Id, "Restore readiness", EvidenceState.Informational,
            "Pulse has not performed a restore test and therefore does not claim that detected backup evidence is recoverable.",
            "Periodically restore a small, non-sensitive test file through your chosen backup application and document the result. Pulse will not initiate a restore or open a repository automatically.",
            "Pulse read-only recovery-verification boundary"));
    }
}
