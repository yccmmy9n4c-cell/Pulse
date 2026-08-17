using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Pulse.Platform.Linux.Platform;
using Pulse.Platform.Linux.Providers;

namespace Pulse.Platform.Linux.Services;

public sealed class AssessmentArchiveService
{
    private static readonly SemaphoreSlim ActivityLogGate = new(1, 1);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _dataDirectory;

    public AssessmentArchiveService(string? dataDirectory = null)
    {
        _dataDirectory = dataDirectory ?? LinuxUserPaths.DataDirectory;
    }

    public string ReportsDirectoryPath => Path.Combine(_dataDirectory, "Reports");
    public string LogsDirectoryPath => Path.Combine(_dataDirectory, "Logs");
    public string ActivityLogPath => Path.Combine(LogsDirectoryPath, "activity.jsonl");

    public string? FindLatestReportPath()
    {
        var reportsDirectory = ReportsDirectoryPath;
        return Directory.Exists(reportsDirectory)
            ? Directory.EnumerateFiles(reportsDirectory, "pulse-assessment-*.html")
                .OrderByDescending(path => path, StringComparer.Ordinal)
                .FirstOrDefault()
            : null;
    }

    public IReadOnlyList<string> FindRecentReportPaths(int maximum = 10)
    {
        if (maximum <= 0 || !Directory.Exists(ReportsDirectoryPath))
        {
            return [];
        }

        return Directory.EnumerateFiles(ReportsDirectoryPath, "pulse-assessment-*.html")
            .OrderByDescending(path => path, StringComparer.Ordinal)
            .Take(maximum)
            .ToArray();
    }

    public IReadOnlyList<AssessmentSnapshot> LoadRecentSnapshots(int maximum = 10)
    {
        var snapshotsDirectory = Path.Combine(_dataDirectory, "Assessments");
        if (maximum <= 0 || !Directory.Exists(snapshotsDirectory))
        {
            return [];
        }

        var snapshots = new List<AssessmentSnapshot>();
        foreach (var path in Directory.EnumerateFiles(snapshotsDirectory, "pulse-assessment-*.json")
                     .OrderByDescending(path => path, StringComparer.Ordinal)
                     .Take(maximum))
        {
            try
            {
                var snapshot = JsonSerializer.Deserialize<AssessmentSnapshot>(File.ReadAllText(path), JsonOptions);
                if (snapshot is not null)
                {
                    snapshots.Add(snapshot);
                }
            }
            catch (JsonException)
            {
                // A damaged historical snapshot is skipped; newer records remain available.
            }
            catch (IOException)
            {
                // A temporarily unreadable snapshot does not block the dashboard.
            }
        }

        return snapshots;
    }

    public IReadOnlyList<string> ReadRecentActivityLines(int maximum = 50)
    {
        if (maximum <= 0 || !File.Exists(ActivityLogPath))
        {
            return [];
        }

        try
        {
            return File.ReadLines(ActivityLogPath).TakeLast(maximum).Reverse().ToArray();
        }
        catch (IOException)
        {
            return [];
        }
    }

    public async Task ClearActivityLogAsync(CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(LogsDirectoryPath);
        SecureDirectory(_dataDirectory);
        SecureDirectory(LogsDirectoryPath);
        await ActivityLogGate.WaitAsync(cancellationToken);
        try
        {
            await WriteAtomicallyAsync(ActivityLogPath, string.Empty, cancellationToken);
        }
        finally
        {
            ActivityLogGate.Release();
        }
    }

    public async Task<AssessmentArtifacts> SaveAsync(
        DistributionSupportResult platform,
        IReadOnlyList<EvidenceResult> evidence,
        string pulseVersion,
        DateTimeOffset? assessedAt = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(platform);
        ArgumentNullException.ThrowIfNull(evidence);
        ArgumentException.ThrowIfNullOrWhiteSpace(pulseVersion);

        var timestamp = (assessedAt ?? DateTimeOffset.UtcNow).ToUniversalTime();
        var snapshot = new AssessmentSnapshot(timestamp, pulseVersion, platform, evidence);
        var stem = $"pulse-assessment-{timestamp:yyyyMMdd-HHmmss-fff}";
        var snapshotsDirectory = Path.Combine(_dataDirectory, "Assessments");
        var reportsDirectory = ReportsDirectoryPath;
        var logsDirectory = LogsDirectoryPath;
        Directory.CreateDirectory(snapshotsDirectory);
        Directory.CreateDirectory(reportsDirectory);
        Directory.CreateDirectory(logsDirectory);
        SecureDirectory(_dataDirectory);
        SecureDirectory(snapshotsDirectory);
        SecureDirectory(reportsDirectory);
        SecureDirectory(logsDirectory);

        var snapshotPath = Path.Combine(snapshotsDirectory, $"{stem}.json");
        var reportPath = Path.Combine(reportsDirectory, $"{stem}.html");
        var activityLogPath = ActivityLogPath;

        await WriteAtomicallyAsync(snapshotPath, JsonSerializer.Serialize(snapshot, JsonOptions), cancellationToken);
        await WriteAtomicallyAsync(reportPath, BuildHtml(snapshot), cancellationToken);
        await AppendActivityAsync(activityLogPath, snapshot, snapshotPath, reportPath, cancellationToken);

        return new AssessmentArtifacts(snapshotPath, reportPath, activityLogPath);
    }

    private static async Task WriteAtomicallyAsync(string path, string contents, CancellationToken cancellationToken)
    {
        var temporaryPath = $"{path}.{Guid.NewGuid():N}.tmp";
        try
        {
            await File.WriteAllTextAsync(temporaryPath, contents, new UTF8Encoding(false), cancellationToken);
            File.Move(temporaryPath, path, true);
            SecureFile(path);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private static async Task AppendActivityAsync(
        string activityLogPath,
        AssessmentSnapshot snapshot,
        string snapshotPath,
        string reportPath,
        CancellationToken cancellationToken)
    {
        var activity = new
        {
            timestampUtc = snapshot.AssessedAtUtc,
            eventName = "assessment.saved",
            snapshotPath,
            reportPath,
            evidenceCount = snapshot.Evidence.Count,
            attentionCount = snapshot.Evidence.Count(item => item.State == EvidenceState.Attention),
            unavailableCount = snapshot.Evidence.Count(item => item.State == EvidenceState.Unavailable)
        };
        var line = JsonSerializer.Serialize(activity) + Environment.NewLine;

        await ActivityLogGate.WaitAsync(cancellationToken);
        try
        {
            await File.AppendAllTextAsync(activityLogPath, line, new UTF8Encoding(false), cancellationToken);
            SecureFile(activityLogPath);
        }
        finally
        {
            ActivityLogGate.Release();
        }
    }

    private static void SecureDirectory(string path)
    {
        if (OperatingSystem.IsLinux())
        {
            File.SetUnixFileMode(path,
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
    }

    private static void SecureFile(string path)
    {
        if (OperatingSystem.IsLinux())
        {
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
    }

    private static string BuildHtml(AssessmentSnapshot snapshot)
    {
        static string Encode(string value) => WebUtility.HtmlEncode(value);
        static string StateClass(EvidenceState state) => state switch
        {
            EvidenceState.Healthy => "healthy",
            EvidenceState.Attention => "attention",
            EvidenceState.Informational => "information",
            _ => "unavailable"
        };
        static string StateLabel(EvidenceState state) => state switch
        {
            EvidenceState.Healthy => "Healthy",
            EvidenceState.Attention => "Review",
            EvidenceState.Informational => "Information",
            _ => "Unavailable"
        };

        var healthy = snapshot.Evidence.Count(item => item.State == EvidenceState.Healthy);
        var attention = snapshot.Evidence.Count(item => item.State == EvidenceState.Attention);
        var unavailable = snapshot.Evidence.Count(item => item.State == EvidenceState.Unavailable);
        var health = PulseHealthInterpreter.Interpret(snapshot.Evidence);
        var html = new StringBuilder();
        html.Append("""
            <!doctype html>
            <html lang="en">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width, initial-scale=1">
              <title>Pulse Supernova Linux Assessment</title>
              <style>
                :root { color-scheme: dark; font-family: Inter, system-ui, sans-serif; background: #080d16; color: #dde7f0; }
                body { max-width: 1040px; margin: 0 auto; padding: 40px 24px 64px; }
                header, .platform, article { background: #101b2a; border: 1px solid #24374a; border-radius: 18px; }
                header { padding: 28px; margin-bottom: 18px; }
                h1 { color: #00a6ff; letter-spacing: .08em; margin: 0; }
                h2 { margin: 6px 0 0; font-size: 1.1rem; font-weight: 500; color: #b7c4d1; }
                .meta { color: #9fb0c0; margin-top: 18px; }
                .platform { padding: 20px 24px; margin-bottom: 18px; }
                .counts { display: flex; flex-wrap: wrap; gap: 10px; margin: 18px 0; }
                .count, .state { border-radius: 999px; padding: 6px 11px; font-weight: 700; }
                .healthy { background: #12392f; color: #6ee7b7; }
                .attention { background: #443713; color: #ffd13d; }
                .information { background: #153854; color: #7dd3fc; }
                .unavailable { background: #3c2932; color: #fda4af; }
                article { padding: 20px 24px; margin: 14px 0; }
                article h3 { margin: 0 0 12px; }
                article p { line-height: 1.55; }
                .source { color: #8799aa; font-size: .9rem; word-break: break-word; }
                .executive { display: grid; grid-template-columns: 180px 1fr; gap: 24px; align-items: center; padding: 22px 24px; margin-bottom: 18px; background: #101b2a; border: 1px solid #36516b; border-radius: 18px; }
                .score { text-align: center; font-size: 2.4rem; font-weight: 800; color: #5cff88; }
                .gauge-state { text-align: center; color: #ffd13d; font-weight: 800; text-transform: uppercase; }
                .track { display: grid; grid-template-columns: repeat(4, 1fr); height: 14px; margin-top: 14px; overflow: hidden; border: 1px solid #00a6ff; border-radius: 9px; }
                .zone-red { background: #ff5b6e; } .zone-orange { background: #ff9f1c; } .zone-gold { background: #ffd13d; } .zone-green { background: #5cff88; }
                .pointer-line { position: relative; height: 18px; margin: 0 2px; }
                .pointer { position: absolute; transform: translateX(-50%); color: #00a6ff; text-shadow: 0 0 8px #00a6ff; }
                footer { color: #718497; text-align: center; margin-top: 30px; }
                @media (max-width: 640px) { .executive { grid-template-columns: 1fr; } }
              </style>
            </head>
            <body>
            """);
        html.Append($"<header><h1>PULSE SUPERNOVA LINUX</h1><h2>System Health. Optimized.</h2><div class=\"meta\">Release {Encode(snapshot.PulseVersion)} &bull; {Encode(snapshot.AssessedAtUtc.ToLocalTime().ToString("F"))}</div></header>");
        html.Append($"<section class=\"executive\"><div><div class=\"score\">{health.Score}</div><div class=\"gauge-state\">{Encode(health.State)}</div></div><div><strong>Current System State</strong><p>{Encode(health.Detail)}</p><div class=\"track\"><span class=\"zone-red\"></span><span class=\"zone-orange\"></span><span class=\"zone-gold\"></span><span class=\"zone-green\"></span></div><div class=\"pointer-line\"><span class=\"pointer\" style=\"left:{health.Score}%\">&#9650;</span></div></div></section>");
        html.Append($"<section class=\"platform\"><strong>{Encode(snapshot.Platform.DisplayName)}</strong> &bull; {Encode(snapshot.Platform.Architecture)}<p>{Encode(snapshot.Platform.Message)}</p></section>");
        html.Append($"<div class=\"counts\"><span class=\"count healthy\">{healthy} healthy</span><span class=\"count attention\">{attention} review</span><span class=\"count unavailable\">{unavailable} unavailable</span></div>");

        foreach (var item in snapshot.Evidence)
        {
            html.Append($"<article><span class=\"state {StateClass(item.State)}\">{StateLabel(item.State)}</span><h3>{Encode(item.Title)}</h3><p>{Encode(item.Summary)}</p><p><strong>Pulse guidance:</strong> {Encode(item.Guidance)}</p><div class=\"source\">Evidence source: {Encode(item.Source)}</div></article>");
        }

        html.Append("<footer>Pulse Supernova Linux &bull; Read-only assessment &bull; No elevated operations</footer></body></html>");
        return html.ToString();
    }
}
