using Pulse.Platform.Linux.Platform;
using Pulse.Platform.Linux.Providers;
using Pulse.Platform.Linux.Services;

var failures = new List<string>();
var detector = new DistributionSupportDetector();

Check("Ubuntu is supported", """
    ID=ubuntu
    VERSION_ID="24.04"
    PRETTY_NAME="Ubuntu 24.04 LTS"
    ID_LIKE=debian
    """, DistributionSupportLevel.Supported, "ubuntu");

Check("Debian is supported", """
    ID=debian
    VERSION_ID="13"
    PRETTY_NAME="Debian GNU/Linux 13"
    """, DistributionSupportLevel.Supported, "debian");

Check("Linux Mint is supported", """
    ID=linuxmint
    VERSION_ID="22.1"
    PRETTY_NAME="Linux Mint 22.1"
    ID_LIKE="ubuntu debian"
    """, DistributionSupportLevel.Supported, "linuxmint");

Check("Unverified derivative stays disabled", """
    ID=pop
    VERSION_ID="24.04"
    PRETTY_NAME="Pop!_OS 24.04"
    ID_LIKE="ubuntu debian"
    """, DistributionSupportLevel.UnverifiedDerivative, "pop");

Check("Fedora is unsupported", """
    ID=fedora
    VERSION_ID="42"
    PRETTY_NAME="Fedora Linux 42"
    ID_LIKE="rhel"
    """, DistributionSupportLevel.Unsupported, "fedora");

var missing = detector.Detect(Path.Combine(Path.GetTempPath(), $"pulse-missing-{Guid.NewGuid():N}"));
if (missing.Level != DistributionSupportLevel.Unsupported)
{
    failures.Add("Missing os-release file must be unsupported.");
}

var isolationService = new LinuxAssessmentService(
[
    new StaticProvider(),
    new ThrowingProvider()
]);
var isolatedResults = await isolationService.RunAsync();
if (isolatedResults.Count != 2 || isolatedResults[0].State != EvidenceState.Healthy ||
    isolatedResults[1].State != EvidenceState.Unavailable)
{
    failures.Add("A failed provider must be isolated and represented as unavailable.");
}

var liveResults = await new LinuxAssessmentService().RunAsync();
if (liveResults.Count != 10)
{
    failures.Add($"The default Piece 3 assessment must return 10 provider results; received {liveResults.Count}.");
}

if (liveResults.Select(result => result.ProviderId).Distinct(StringComparer.Ordinal).Count() != liveResults.Count)
{
    failures.Add("Default provider identifiers must be unique.");
}

var archiveRoot = Path.Combine(Path.GetTempPath(), $"pulse-archive-{Guid.NewGuid():N}");
try
{
    var archive = new AssessmentArchiveService(archiveRoot);
    var platform = new DistributionSupportResult(
        DistributionSupportLevel.Supported, "ubuntu", "24.04", "Ubuntu <Test>", "x86_64", "Supported & verified.");
    var evidence = new[]
    {
        new EvidenceResult("test.escape", "Title <script>alert(1)</script>", EvidenceState.Attention,
            "Summary & detail", "Review <carefully>.", "/proc/<test>")
    };
    var artifacts = await archive.SaveAsync(platform, evidence, "0.0.0.4",
        new DateTimeOffset(2026, 8, 3, 12, 34, 56, TimeSpan.Zero));

    if (!File.Exists(artifacts.SnapshotPath) || !File.Exists(artifacts.ReportPath) || !File.Exists(artifacts.ActivityLogPath))
    {
        failures.Add("Piece 4 must save a JSON snapshot, HTML report, and activity log.");
    }
    else
    {
        using var document = System.Text.Json.JsonDocument.Parse(await File.ReadAllTextAsync(artifacts.SnapshotPath));
        if (document.RootElement.GetProperty("PulseVersion").GetString() != "0.0.0.4")
        {
            failures.Add("The saved assessment snapshot must record the Pulse version.");
        }

        var report = await File.ReadAllTextAsync(artifacts.ReportPath);
        if (!report.Contains("&lt;script&gt;", StringComparison.Ordinal) ||
            report.Contains("<script>", StringComparison.Ordinal))
        {
            failures.Add("The HTML report must encode all evidence text.");
        }

        var activityLine = await File.ReadAllTextAsync(artifacts.ActivityLogPath);
        if (!activityLine.Contains("assessment.saved", StringComparison.Ordinal))
        {
            failures.Add("The activity log must record the saved assessment event.");
        }

        if (!string.Equals(archive.FindLatestReportPath(), artifacts.ReportPath, StringComparison.Ordinal))
        {
            failures.Add("Pulse must rediscover the latest saved report after restart.");
        }
    }
}
finally
{
    if (Directory.Exists(archiveRoot))
    {
        Directory.Delete(archiveRoot, true);
    }
}

foreach (var result in liveResults)
{
    Console.WriteLine($"{result.State,-13} {result.ProviderId}: {result.Summary.Replace('\n', ' ')}");
}

if (failures.Count > 0)
{
    Console.Error.WriteLine("Pulse Linux smoke tests failed:");
    foreach (var failure in failures)
    {
        Console.Error.WriteLine($"- {failure}");
    }

    return 1;
}

Console.WriteLine("Pulse Linux boundary, provider, and reporting smoke tests passed.");
return 0;

void Check(string name, string contents, DistributionSupportLevel expectedLevel, string expectedId)
{
    var path = Path.Combine(Path.GetTempPath(), $"pulse-os-release-{Guid.NewGuid():N}");
    try
    {
        File.WriteAllText(path, contents);
        var result = detector.Detect(path);
        if (result.Level != expectedLevel || !result.Id.Equals(expectedId, StringComparison.OrdinalIgnoreCase))
        {
            failures.Add($"{name}: expected {expectedLevel}/{expectedId}, received {result.Level}/{result.Id}.");
        }
    }
    finally
    {
        File.Delete(path);
    }
}

sealed class StaticProvider : ILinuxEvidenceProvider
{
    public string Id => "test.static";

    public Task<EvidenceResult> CollectAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(new EvidenceResult(Id, "Static test", EvidenceState.Healthy,
            "Static evidence succeeded.", "No action required.", "smoke-test"));
}

sealed class ThrowingProvider : ILinuxEvidenceProvider
{
    public string Id => "test.throwing";

    public Task<EvidenceResult> CollectAsync(CancellationToken cancellationToken = default) =>
        throw new IOException("Expected smoke-test failure.");
}
