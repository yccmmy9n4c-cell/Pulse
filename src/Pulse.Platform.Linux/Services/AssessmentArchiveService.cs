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

    public string? FindLatestReportPath()
    {
        var reportsDirectory = Path.Combine(_dataDirectory, "Reports");
        return Directory.Exists(reportsDirectory)
            ? Directory.EnumerateFiles(reportsDirectory, "pulse-assessment-*.html")
                .OrderByDescending(path => path, StringComparer.Ordinal)
                .FirstOrDefault()
            : null;
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
        var reportsDirectory = Path.Combine(_dataDirectory, "Reports");
        var logsDirectory = Path.Combine(_dataDirectory, "Logs");
        Directory.CreateDirectory(snapshotsDirectory);
        Directory.CreateDirectory(reportsDirectory);
        Directory.CreateDirectory(logsDirectory);
        SecureDirectory(_dataDirectory);
        SecureDirectory(snapshotsDirectory);
        SecureDirectory(reportsDirectory);
        SecureDirectory(logsDirectory);

        var snapshotPath = Path.Combine(snapshotsDirectory, $"{stem}.json");
        var reportPath = Path.Combine(reportsDirectory, $"{stem}.html");
        var activityLogPath = Path.Combine(logsDirectory, "activity.jsonl");

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
        var html = new StringBuilder();
        html.Append("""
            <!doctype html>
            <html lang="en">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width, initial-scale=1">
              <title>Pulse Platform Assessment</title>
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
                footer { color: #718497; text-align: center; margin-top: 30px; }
              </style>
            </head>
            <body>
            """);
        html.Append($"<header><h1>PULSE</h1><h2>System Health. Optimized.</h2><div class=\"meta\">Linux Beta {Encode(snapshot.PulseVersion)} &bull; {Encode(snapshot.AssessedAtUtc.ToLocalTime().ToString("F"))}</div></header>");
        html.Append($"<section class=\"platform\"><strong>{Encode(snapshot.Platform.DisplayName)}</strong> &bull; {Encode(snapshot.Platform.Architecture)}<p>{Encode(snapshot.Platform.Message)}</p></section>");
        html.Append($"<div class=\"counts\"><span class=\"count healthy\">{healthy} healthy</span><span class=\"count attention\">{attention} review</span><span class=\"count unavailable\">{unavailable} unavailable</span></div>");

        foreach (var item in snapshot.Evidence)
        {
            html.Append($"<article><span class=\"state {StateClass(item.State)}\">{StateLabel(item.State)}</span><h3>{Encode(item.Title)}</h3><p>{Encode(item.Summary)}</p><p><strong>Pulse guidance:</strong> {Encode(item.Guidance)}</p><div class=\"source\">Evidence source: {Encode(item.Source)}</div></article>");
        }

        html.Append("<footer>Pulse Platform &bull; Read-only assessment &bull; No elevated operations</footer></body></html>");
        return html.ToString();
    }
}
