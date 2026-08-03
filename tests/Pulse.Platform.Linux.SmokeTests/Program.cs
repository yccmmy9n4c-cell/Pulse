using Pulse.Platform.Linux;
using Pulse.Platform.Linux.Platform;
using Pulse.Platform.Linux.Providers;
using Pulse.Platform.Linux.Services;

var failures = new List<string>();
var detector = new DistributionSupportDetector();

if (AppInfo.ProductName != "Pulse Supernova Linux" || AppInfo.Version != "0.0.0.11")
{
    failures.Add("Pulse Supernova Linux identity and version must come from AppInfo.");
}

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
if (liveResults.Count != 14)
{
    failures.Add($"The default Piece 9 assessment must return 14 provider results; received {liveResults.Count}.");
}

var providerCommands = new List<(string Executable, IReadOnlyList<string> Arguments)>();
var providerRunner = new ScriptedReadOnlyCommandRunner((executable, arguments) =>
{
    providerCommands.Add((executable, arguments.ToArray()));
    if (executable == "ip" && arguments.SequenceEqual(["-json", "link", "show", "up"]))
    {
        return Success("[{\"ifname\":\"lo\"},{\"ifname\":\"enp1s0\"}]");
    }

    if (executable == "ip" && arguments.SequenceEqual(["-json", "-4", "route", "show", "default"]))
    {
        return Success("[{\"dst\":\"default\",\"gateway\":\"192.0.2.1\"}]");
    }

    if (executable == "ip" && arguments.SequenceEqual(["-json", "-6", "route", "show", "default"]))
    {
        return Success("[]");
    }

    if (executable == "nmcli")
    {
        return Success("connected:full\n");
    }

    if (executable == "journalctl")
    {
        return Success("""
            {"PRIORITY":"3","_SYSTEMD_UNIT":"example.service","MESSAGE":"private diagnostic text"}
            {"PRIORITY":"2","SYSLOG_IDENTIFIER":"kernel","MESSAGE":"another private message"}
            """);
    }

    if (executable == "lsblk" && arguments.Contains("--nodeps", StringComparer.Ordinal))
    {
        return Success("{\"blockdevices\":[{\"name\":\"sda\",\"path\":\"/dev/sda\",\"type\":\"disk\",\"model\":\"Pulse Test Disk\",\"tran\":\"sata\"}]}");
    }

    if (Path.GetFileName(executable) == "smartctl")
    {
        return Success("SMART overall-health self-assessment test result: PASSED\n");
    }

    return new ReadOnlyCommandResult(false, false, -1, string.Empty, "Unexpected test command.");
});

var networkEvidence = await new NetworkPostureEvidenceProvider(providerRunner).CollectAsync();
var journalEvidence = await new JournalReliabilityEvidenceProvider(providerRunner).CollectAsync();
var driveEvidence = await new DriveHealthEvidenceProvider(providerRunner,
    name => name == "smartctl" ? "smartctl" : null).CollectAsync();
if (networkEvidence.State != EvidenceState.Healthy ||
    !networkEvidence.Summary.Contains("enp1s0", StringComparison.Ordinal) ||
    !networkEvidence.Summary.Contains("default route", StringComparison.OrdinalIgnoreCase))
{
    failures.Add("Piece 7 network intelligence must recognize an active interface and local default route.");
}

if (journalEvidence.State != EvidenceState.Attention ||
    !journalEvidence.Summary.Contains("critical-or-higher", StringComparison.Ordinal) ||
    journalEvidence.Summary.Contains("private", StringComparison.OrdinalIgnoreCase))
{
    failures.Add("Piece 7 journal intelligence must summarize severity and sources without copying message contents.");
}

if (providerCommands.Any(command => command.Executable is "ping" or "curl" or "wget"))
{
    failures.Add("Piece 7 must not perform an active internet reachability test.");
}

var journalCommand = providerCommands.FirstOrDefault(command => command.Executable == "journalctl");
if (journalCommand.Arguments is null ||
    !journalCommand.Arguments.Contains("--output-fields=PRIORITY,_SYSTEMD_UNIT,SYSLOG_IDENTIFIER,_COMM", StringComparer.Ordinal))
{
    failures.Add("Piece 7 journal collection must request metadata fields only.");
}

if (driveEvidence.State != EvidenceState.Healthy ||
    !driveEvidence.Summary.Contains("Pulse Test Disk", StringComparison.Ordinal) ||
    !providerCommands.Any(command => command.Executable == "smartctl" &&
        command.Arguments.Contains("--nocheck=standby,3", StringComparer.Ordinal) &&
        command.Arguments.Contains("--health", StringComparer.Ordinal)) ||
    providerCommands.Any(command => command.Executable == "smartctl" &&
        command.Arguments.Any(argument => argument.Contains("selftest", StringComparison.OrdinalIgnoreCase) ||
                                          argument.Equals("--test", StringComparison.OrdinalIgnoreCase))))
{
    failures.Add("Piece 9 drive health must remain read-only, avoid waking standby drives, and never start a self-test.");
}

var backupRoot = Path.Combine(Path.GetTempPath(), $"pulse-backup-{Guid.NewGuid():N}");
try
{
    var backupBin = Path.Combine(backupRoot, "bin");
    Directory.CreateDirectory(backupBin);
    Directory.CreateDirectory(Path.Combine(backupRoot, ".config", "deja-dup"));
    await File.WriteAllTextAsync(Path.Combine(backupBin, "deja-dup"), string.Empty);
    var backupEvidence = await new BackupPostureEvidenceProvider(
        backupRoot, [backupBin], Path.Combine(backupRoot, "missing-timeshift.json")).CollectAsync();
    if (backupEvidence.State != EvidenceState.Informational ||
        !backupEvidence.Summary.Contains("Déjà Dup", StringComparison.Ordinal) ||
        !backupEvidence.Guidance.Contains("does not prove", StringComparison.OrdinalIgnoreCase))
    {
        failures.Add("Piece 9 backup posture must detect configuration without claiming recoverability.");
    }
}
finally
{
    if (Directory.Exists(backupRoot))
    {
        Directory.Delete(backupRoot, true);
    }
}

var optimizedHealth = PulseHealthInterpreter.Interpret(
[
    new EvidenceResult("test.healthy", "Healthy", EvidenceState.Healthy,
        "Healthy evidence.", "No action required.", "test")
]);
var attentionHealth = PulseHealthInterpreter.Interpret(
[
    new EvidenceResult("test.attention", "Attention", EvidenceState.Attention,
        "Review evidence.", "Review this item.", "test")
]);
if (optimizedHealth.State != "Optimized" || optimizedHealth.Score != 100 ||
    attentionHealth.State != "Attention Recommended" || attentionHealth.Score > 79)
{
    failures.Add("Pulse Standard health language must remain status-first and attention-aware.");
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
    var artifacts = await archive.SaveAsync(platform, evidence, "0.0.0.11",
        new DateTimeOffset(2026, 8, 3, 12, 34, 56, TimeSpan.Zero));

    if (!File.Exists(artifacts.SnapshotPath) || !File.Exists(artifacts.ReportPath) || !File.Exists(artifacts.ActivityLogPath))
    {
        failures.Add("Piece 4 must save a JSON snapshot, HTML report, and activity log.");
    }
    else
    {
        using var document = System.Text.Json.JsonDocument.Parse(await File.ReadAllTextAsync(artifacts.SnapshotPath));
        if (document.RootElement.GetProperty("PulseVersion").GetString() != "0.0.0.11")
        {
            failures.Add("The saved assessment snapshot must record the Pulse version.");
        }

        var report = await File.ReadAllTextAsync(artifacts.ReportPath);
        if (!report.Contains("&lt;script&gt;", StringComparison.Ordinal) ||
            !report.Contains("Current System State", StringComparison.Ordinal) ||
            !report.Contains("class=\"executive\"", StringComparison.Ordinal) ||
            !report.Contains("PULSE SUPERNOVA LINUX", StringComparison.Ordinal) ||
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

        if (archive.LoadRecentSnapshots(5).Count != 1 || archive.FindRecentReportPaths(5).Count != 1 ||
            archive.ReadRecentActivityLines(5).Count != 1)
        {
            failures.Add("The Pulse Standard dashboard must be able to reload report, snapshot, and activity history.");
        }

        await archive.ClearActivityLogAsync();
        if (archive.ReadRecentActivityLines(5).Count != 0)
        {
            failures.Add("The confirmed Clear Event Log action must leave an empty user activity log.");
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

var scheduleRoot = Path.Combine(Path.GetTempPath(), $"pulse-systemd-{Guid.NewGuid():N}");
var fakeExecutable = Path.Combine(scheduleRoot, "pulse-platform");
try
{
    Directory.CreateDirectory(scheduleRoot);
    await File.WriteAllTextAsync(fakeExecutable, "test executable");
    var systemdRunner = new FakeSystemdUserCommandRunner();
    var schedule = new SystemdUserScheduleService(scheduleRoot, systemdRunner);
    var enableResult = await schedule.EnableAsync(fakeExecutable);
    var servicePath = Path.Combine(scheduleRoot, SystemdUserScheduleService.ServiceUnitName);
    var timerPath = Path.Combine(scheduleRoot, SystemdUserScheduleService.TimerUnitName);

    if (!enableResult.Succeeded || !File.Exists(servicePath) || !File.Exists(timerPath))
    {
        failures.Add("Piece 5 must create and enable both systemd user units after approval.");
    }
    else
    {
        var serviceUnit = await File.ReadAllTextAsync(servicePath);
        var timerUnit = await File.ReadAllTextAsync(timerPath);
        if (!serviceUnit.Contains("--assess-once", StringComparison.Ordinal) ||
            !serviceUnit.Contains(Path.GetFullPath(fakeExecutable), StringComparison.Ordinal) ||
            !serviceUnit.Contains("NoNewPrivileges=true", StringComparison.Ordinal) ||
            !serviceUnit.Contains("UMask=0077", StringComparison.Ordinal) ||
            !timerUnit.Contains("OnCalendar=weekly", StringComparison.Ordinal) ||
            !timerUnit.Contains("Persistent=true", StringComparison.Ordinal))
        {
            failures.Add("The Piece 5 service/timer contract is incomplete.");
        }
    }

    var status = await schedule.GetStatusAsync();
    if (status.State != UserScheduleState.Enabled)
    {
        failures.Add("The weekly schedule must report enabled after systemd enables it.");
    }

    var disableResult = await schedule.DisableAsync();
    if (!disableResult.Succeeded || File.Exists(servicePath) || File.Exists(timerPath) ||
        systemdRunner.Commands.Any(command => command.Contains("sudo", StringComparer.Ordinal)))
    {
        failures.Add("Disabling must remove only the Pulse user units without invoking sudo.");
    }
}
finally
{
    if (Directory.Exists(scheduleRoot))
    {
        Directory.Delete(scheduleRoot, true);
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

Console.WriteLine("Pulse Linux boundary, Nebula providers, Aurora reporting, navigation data, and user-schedule smoke tests passed.");
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

static ReadOnlyCommandResult Success(string output) =>
    new(true, false, 0, output, string.Empty);

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

sealed class FakeSystemdUserCommandRunner : ISystemdUserCommandRunner
{
    public List<IReadOnlyList<string>> Commands { get; } = [];
    private bool _enabled;

    public Task<SystemdUserCommandResult> RunAsync(
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken = default)
    {
        Commands.Add(arguments.ToArray());
        if (arguments.SequenceEqual(["enable", "--now", SystemdUserScheduleService.TimerUnitName]))
        {
            _enabled = true;
        }
        else if (arguments.SequenceEqual(["disable", "--now", SystemdUserScheduleService.TimerUnitName]))
        {
            _enabled = false;
        }

        if (arguments.SequenceEqual(["is-enabled", SystemdUserScheduleService.TimerUnitName]))
        {
            return Task.FromResult(_enabled
                ? new SystemdUserCommandResult(true, false, 0, "enabled\n", string.Empty)
                : new SystemdUserCommandResult(true, false, 1, "disabled\n", string.Empty));
        }

        return Task.FromResult(new SystemdUserCommandResult(true, false, 0, string.Empty, string.Empty));
    }
}

sealed class ScriptedReadOnlyCommandRunner(
    Func<string, IReadOnlyList<string>, ReadOnlyCommandResult> handler) : IReadOnlyCommandRunner
{
    public Task<ReadOnlyCommandResult> RunAsync(
        string executable,
        IReadOnlyList<string> arguments,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(handler(executable, arguments));
}
