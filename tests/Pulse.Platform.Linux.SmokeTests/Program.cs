using Pulse.Platform.Linux;
using Pulse.Platform.Linux.Platform;
using Pulse.Platform.Linux.Providers;
using Pulse.Platform.Linux.Services;
using System.Net;
using System.Security.Cryptography;
using System.Text;

var failures = new List<string>();
var detector = new DistributionSupportDetector();

var preferenceRoot = Path.Combine(Path.GetTempPath(), $"pulse-preferences-{Guid.NewGuid():N}");
var preferencePath = Path.Combine(preferenceRoot, "settings.json");
try
{
    var preferenceStore = new PulseUserPreferencesService(preferencePath);
    if (preferenceStore.Load().IgnoreInactiveFirewall)
    {
        failures.Add("The inactive-firewall preference must default to review, not ignored.");
    }

    Directory.CreateDirectory(preferenceRoot);
    await File.WriteAllTextAsync(preferencePath, "{not-valid-json");
    if (preferenceStore.Load().IgnoreInactiveFirewall)
    {
        failures.Add("Malformed user settings must fail safe by restoring firewall review.");
    }

    var acknowledged = preferenceStore.SetInactiveFirewallAcknowledged(true);
    var reloaded = preferenceStore.Load();
    var inactiveFirewall = new EvidenceResult(
        "linux.firewall-indicator",
        "Firewall indicator",
        EvidenceState.Informational,
        FirewallEvidenceProvider.InactiveSummary,
        FirewallEvidenceProvider.InactiveGuidance,
        FirewallEvidenceProvider.InactiveSource);
    var acceptedEvidence = EvidencePreferencePolicy.Apply([inactiveFirewall], reloaded).Single();
    var restoredEvidence = EvidencePreferencePolicy.Apply([acceptedEvidence], new PulseUserPreferences()).Single();
    if (!acknowledged.IgnoreInactiveFirewall || acknowledged.InactiveFirewallAcknowledgedAtUtc is null ||
        !reloaded.IgnoreInactiveFirewall || !File.Exists(preferencePath) ||
        acceptedEvidence.State != EvidenceState.Healthy ||
        !acceptedEvidence.Summary.Contains("off intentionally", StringComparison.Ordinal) ||
        restoredEvidence.State != EvidenceState.Informational ||
        restoredEvidence.Summary != FirewallEvidenceProvider.InactiveSummary)
    {
        failures.Add("The user-approved firewall-off choice must persist, suppress review only for the inactive indicator, and remain reversible.");
    }

    var activeFirewall = inactiveFirewall with
    {
        State = EvidenceState.Healthy,
        Summary = "UFW's systemd service is active.",
        Guidance = "Active firewall indicator.",
        Source = "systemctl is-active ufw.service"
    };
    if (EvidencePreferencePolicy.Apply([activeFirewall], reloaded).Single() != activeFirewall)
    {
        failures.Add("The firewall-off preference must never rewrite directly observed active firewall evidence.");
    }

    preferenceStore.SetInactiveFirewallAcknowledged(false);
    if (preferenceStore.Load().IgnoreInactiveFirewall)
    {
        failures.Add("Restore Firewall Review must remove the persisted intentional-off choice.");
    }
}
finally
{
    if (Directory.Exists(preferenceRoot))
    {
        Directory.Delete(preferenceRoot, true);
    }
}

if (AppInfo.ProductName != "Pulse Supernova Linux" || AppInfo.Version != "0.0.0.22")
{
    failures.Add("Pulse Supernova Linux identity and version must come from AppInfo.");
}

var updateReleaseJson = """
    [
      {
        "tag_name":"windows-v9.0.0.0","name":"Pulse Windows 9.0.0.0","body":"Windows release", "html_url":"https://example.invalid/windows", "draft":false,"prerelease":false,
        "assets":[{"name":"pulse-windows.exe","browser_download_url":"https://example.invalid/windows.exe","size":10}]
      },
      {
        "tag_name":"linux-v0.0.0.22","name":"Pulse Linux Beta 0.0.0.22","body":"Updater release notes", "html_url":"https://example.invalid/linux-22", "draft":false,"prerelease":true,
        "assets":[
          {"name":"pulse-platform_0.0.0.22_amd64.deb","browser_download_url":"https://example.invalid/pulse.deb","size":10},
          {"name":"SHA256SUMS","browser_download_url":"https://example.invalid/SHA256SUMS","size":100}
        ]
      },
      {
        "tag_name":"linux-v0.0.0.20","name":"Pulse Linux Beta 0.0.0.20","body":"Previous", "html_url":"https://example.invalid/linux-20", "draft":false,"prerelease":true,
        "assets":[
          {"name":"pulse-platform_0.0.0.20_amd64.deb","browser_download_url":"https://example.invalid/old.deb","size":10},
          {"name":"SHA256SUMS","browser_download_url":"https://example.invalid/old-sha","size":100}
        ]
      }
    ]
    """;
var availableUpdate = GitHubUpdateService.EvaluateReleaseList(updateReleaseJson, "0.0.0.20", "amd64");
var currentUpdate = GitHubUpdateService.EvaluateReleaseList(updateReleaseJson, "0.0.0.22", "amd64");
if (availableUpdate.Availability != UpdateAvailability.Available ||
    availableUpdate.LatestVersion != "0.0.0.22" ||
    availableUpdate.PackageAssetName != "pulse-platform_0.0.0.22_amd64.deb" ||
    currentUpdate.Availability != UpdateAvailability.Current)
{
    failures.Add("Updates must select the highest compatible Linux release asset, including published Beta prereleases, while ignoring unrelated Windows releases.");
}

var updateDownloadRoot = Path.Combine(Path.GetTempPath(), $"pulse-update-{Guid.NewGuid():N}");
try
{
    var packageBytes = Encoding.UTF8.GetBytes("verified Pulse Debian package bytes");
    var packageHash = Convert.ToHexString(SHA256.HashData(packageBytes)).ToLowerInvariant();
    var updateHttpClient = new HttpClient(new StaticHttpMessageHandler(request =>
        request.RequestUri?.AbsolutePath.EndsWith("SHA256SUMS", StringComparison.Ordinal) == true
            ? new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent($"{packageHash}  {availableUpdate.PackageAssetName}\n")
            }
            : new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(packageBytes) }));
    var downloadResult = await new GitHubUpdateService(updateHttpClient)
        .DownloadAndVerifyAsync(availableUpdate, updateDownloadRoot);
    if (!downloadResult.Succeeded || !File.Exists(downloadResult.PackagePath) ||
        !File.ReadAllBytes(downloadResult.PackagePath).SequenceEqual(packageBytes))
    {
        failures.Add("Updates must save an architecture-specific Debian package only after SHA-256 verification.");
    }

    var rejectedRoot = Path.Combine(updateDownloadRoot, "rejected");
    var rejectedClient = new HttpClient(new StaticHttpMessageHandler(request =>
        request.RequestUri?.AbsolutePath.EndsWith("SHA256SUMS", StringComparison.Ordinal) == true
            ? new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent($"{new string('0', 64)}  {availableUpdate.PackageAssetName}\n")
            }
            : new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(packageBytes) }));
    var rejectedResult = await new GitHubUpdateService(rejectedClient)
        .DownloadAndVerifyAsync(availableUpdate, rejectedRoot);
    if (rejectedResult.Succeeded || File.Exists(Path.Combine(rejectedRoot, availableUpdate.PackageAssetName!)) ||
        File.Exists(Path.Combine(rejectedRoot, availableUpdate.PackageAssetName! + ".part")))
    {
        failures.Add("Updates must reject and remove a Debian package whose SHA-256 checksum does not match.");
    }
}
finally
{
    if (Directory.Exists(updateDownloadRoot))
    {
        Directory.Delete(updateDownloadRoot, true);
    }
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
if (liveResults.Count != 24)
{
    failures.Add($"The default Network Intelligence assessment must return 24 provider results; received {liveResults.Count}.");
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

    if (executable == "ss")
    {
        return Success("tcp LISTEN 0 4096 0.0.0.0:22 0.0.0.0:*\nudp UNCONN 0 0 127.0.0.53:53 0.0.0.0:*\n");
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
var routeEvidence = await new DefaultRouteEvidenceProvider(providerRunner).CollectAsync();
var networkManagerEvidence = await new NetworkManagerEvidenceProvider(providerRunner).CollectAsync();
var listeningEvidence = await new ListeningServicesEvidenceProvider(providerRunner).CollectAsync();
var dnsTestPath = Path.Combine(Path.GetTempPath(), $"pulse-resolv-{Guid.NewGuid():N}.conf");
EvidenceResult dnsEvidence;
try
{
    await File.WriteAllTextAsync(dnsTestPath, "# test resolver\nnameserver 127.0.0.53\nnameserver 192.0.2.53\n");
    dnsEvidence = await new DnsConfigurationEvidenceProvider(dnsTestPath).CollectAsync();
}
finally
{
    File.Delete(dnsTestPath);
}
var journalEvidence = await new JournalReliabilityEvidenceProvider(providerRunner).CollectAsync();
var driveEvidence = await new DriveHealthEvidenceProvider(providerRunner,
    name => name == "smartctl" ? "smartctl" : null).CollectAsync();
if (networkEvidence.State != EvidenceState.Healthy ||
    !networkEvidence.Summary.Contains("enp1s0", StringComparison.Ordinal) ||
    networkEvidence.Summary.Contains("default route", StringComparison.OrdinalIgnoreCase))
{
    failures.Add("Network Intelligence must report active non-loopback interfaces separately from route state.");
}

if (routeEvidence.State != EvidenceState.Healthy ||
    !routeEvidence.Summary.Contains("IPv4", StringComparison.Ordinal) ||
    networkManagerEvidence.State != EvidenceState.Healthy ||
    !networkManagerEvidence.Summary.Contains("connected/full", StringComparison.Ordinal) ||
    dnsEvidence.State != EvidenceState.Healthy ||
    !dnsEvidence.Summary.Contains("2 nameserver entries", StringComparison.Ordinal) ||
    dnsEvidence.Summary.Contains("192.0.2.53", StringComparison.Ordinal) ||
    listeningEvidence.State != EvidenceState.Informational ||
    !listeningEvidence.Summary.Contains("2 TCP/UDP", StringComparison.Ordinal) ||
    listeningEvidence.Summary.Contains("0.0.0.0", StringComparison.Ordinal) ||
    listeningEvidence.Summary.Contains(":22", StringComparison.Ordinal))
{
    failures.Add("Network Intelligence must separate route, NetworkManager, and DNS posture and summarize listening exposure without retaining private endpoint details.");
}

if (journalEvidence.State != EvidenceState.Attention ||
    !journalEvidence.Summary.Contains("critical-or-higher", StringComparison.Ordinal) ||
    journalEvidence.Summary.Contains("private", StringComparison.OrdinalIgnoreCase))
{
    failures.Add("Piece 7 journal intelligence must summarize severity and sources without copying message contents.");
}

if (providerCommands.Any(command => command.Executable is "ping" or "curl" or "wget" or "dig" or "host" or "nslookup" or "getent"))
{
    failures.Add("Network Intelligence must not perform an active internet reachability or DNS test.");
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

var historicalSmartRunner = new ScriptedReadOnlyCommandRunner((executable, arguments) =>
{
    if (executable == "lsblk")
    {
        return Success("{\"blockdevices\":[{\"name\":\"sdb\",\"path\":\"/dev/sdb\",\"type\":\"disk\",\"model\":\"History Disk\",\"tran\":\"sata\"}]}");
    }

    return Path.GetFileName(executable) == "smartctl"
        ? new ReadOnlyCommandResult(true, false, 64, "SMART overall-health self-assessment test result: PASSED\n", string.Empty)
        : new ReadOnlyCommandResult(false, false, -1, string.Empty, "Unexpected historical SMART test command.");
});
var historicalSmartEvidence = await new DriveHealthEvidenceProvider(historicalSmartRunner,
    name => name == "smartctl" ? "smartctl" : null).CollectAsync();
if (historicalSmartEvidence.State != EvidenceState.Informational ||
    !historicalSmartEvidence.Summary.Contains("historical", StringComparison.OrdinalIgnoreCase) ||
    !historicalSmartEvidence.Summary.Contains("did not report an active failure", StringComparison.OrdinalIgnoreCase))
{
    failures.Add("Storage Intelligence must describe historical SMART records as informational rather than a current drive failure.");
}

var currentSmartFailureRunner = new ScriptedReadOnlyCommandRunner((executable, arguments) =>
{
    if (executable == "lsblk")
    {
        return Success("{\"blockdevices\":[{\"name\":\"sdc\",\"path\":\"/dev/sdc\",\"type\":\"disk\",\"model\":\"Current Failure Disk\",\"tran\":\"sata\"}]}");
    }

    return Path.GetFileName(executable) == "smartctl"
        ? new ReadOnlyCommandResult(true, false, 8, "SMART overall-health self-assessment test result: FAILED!\n", string.Empty)
        : new ReadOnlyCommandResult(false, false, -1, string.Empty, "Unexpected current SMART test command.");
});
var currentSmartFailureEvidence = await new DriveHealthEvidenceProvider(currentSmartFailureRunner,
    name => name == "smartctl" ? "smartctl" : null).CollectAsync();
if (currentSmartFailureEvidence.State != EvidenceState.Attention ||
    !currentSmartFailureEvidence.Summary.Contains("Current drive-health indicators", StringComparison.Ordinal))
{
    failures.Add("Storage Intelligence must continue to flag an active SMART failure for attention.");
}

var deniedSmartRunner = new ScriptedReadOnlyCommandRunner((executable, arguments) =>
{
    if (executable == "lsblk")
    {
        return Success("{\"blockdevices\":[{\"name\":\"nvme0n1\",\"path\":\"/dev/nvme0n1\",\"type\":\"disk\",\"model\":\"Permission Test NVMe\",\"tran\":\"nvme\"}]}");
    }

    return Path.GetFileName(executable) == "smartctl"
        ? new ReadOnlyCommandResult(true, false, 2, string.Empty,
            "smartctl open device: /dev/nvme0n1 failed: Permission denied")
        : new ReadOnlyCommandResult(false, false, -1, string.Empty, "Unexpected permission test command.");
});
var deniedSmartEvidence = await new DriveHealthEvidenceProvider(deniedSmartRunner,
    name => name == "smartctl" ? "smartctl" : null).CollectAsync();
if (deniedSmartEvidence.State != EvidenceState.Informational ||
    deniedSmartEvidence.State == EvidenceState.Attention ||
    !deniedSmartEvidence.Summary.Contains("current user's permissions", StringComparison.OrdinalIgnoreCase) ||
    !deniedSmartEvidence.Guidance.Contains("not a detected drive failure", StringComparison.OrdinalIgnoreCase))
{
    failures.Add("A smartctl permission-denied message must be incomplete coverage, never a drive-health failure.");
}

var storageCommands = new List<(string Executable, IReadOnlyList<string> Arguments)>();
var storageRunner = new ScriptedReadOnlyCommandRunner((executable, arguments) =>
{
    storageCommands.Add((executable, arguments.ToArray()));
    if (executable == "findmnt")
    {
        return Success("{\"filesystems\":[{\"source\":\"/dev/mapper/system-root\",\"fstype\":\"ext4\",\"options\":\"ro,relatime\"}]}");
    }

    if (executable == "df")
    {
        return Success("Filesystem Inodes IUsed IFree IUse% Mounted on\n/dev/mapper/system-root 1000000 910000 90000 91% /\n");
    }

    return new ReadOnlyCommandResult(false, false, -1, string.Empty, "Unexpected storage test command.");
});
var rootMountEvidence = await new RootMountEvidenceProvider(storageRunner).CollectAsync();
var inodeEvidence = await new InodeCapacityEvidenceProvider(storageRunner).CollectAsync();
if (rootMountEvidence.State != EvidenceState.Attention ||
    !rootMountEvidence.Summary.Contains("read-only", StringComparison.OrdinalIgnoreCase) ||
    !rootMountEvidence.Summary.Contains("ext4", StringComparison.Ordinal) ||
    inodeEvidence.State != EvidenceState.Attention ||
    !inodeEvidence.Summary.Contains("91%", StringComparison.Ordinal) ||
    storageCommands.Any(command => command.Arguments.Any(argument =>
        argument.Contains("repair", StringComparison.OrdinalIgnoreCase) ||
        argument.Contains("remount", StringComparison.OrdinalIgnoreCase))))
{
    failures.Add("Storage Intelligence must identify read-only root mounts and inode pressure using read-only metadata commands.");
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

var secureBootRoot = Path.Combine(Path.GetTempPath(), $"pulse-secure-boot-{Guid.NewGuid():N}");
try
{
    var efiPath = Path.Combine(secureBootRoot, "efi");
    var efivarsPath = Path.Combine(efiPath, "efivars");
    Directory.CreateDirectory(efivarsPath);
    var variablePath = Path.Combine(efivarsPath, "SecureBoot-test-guid");
    await File.WriteAllBytesAsync(variablePath, [7, 0, 0, 0, 1]);

    var enabledSecureBoot = await new SecureBootEvidenceProvider(efiPath, efivarsPath).CollectAsync();
    await File.WriteAllBytesAsync(variablePath, [7, 0, 0, 0, 0]);
    var disabledSecureBoot = await new SecureBootEvidenceProvider(efiPath, efivarsPath).CollectAsync();
    if (enabledSecureBoot.State != EvidenceState.Healthy ||
        !enabledSecureBoot.Summary.Contains("enabled", StringComparison.OrdinalIgnoreCase) ||
        disabledSecureBoot.State != EvidenceState.Informational ||
        !disabledSecureBoot.Summary.Contains("disabled", StringComparison.OrdinalIgnoreCase) ||
        !disabledSecureBoot.Guidance.Contains("made no firmware", StringComparison.OrdinalIgnoreCase))
    {
        failures.Add("Security Intelligence must read UEFI Secure Boot state, treat disabled posture as optional hardening, and never change firmware or boot policy.");
    }
}
finally
{
    if (Directory.Exists(secureBootRoot))
    {
        Directory.Delete(secureBootRoot, true);
    }
}

var packageCommands = new List<(string Executable, IReadOnlyList<string> Arguments)>();
var packageRunner = new ScriptedReadOnlyCommandRunner((executable, arguments) =>
{
    packageCommands.Add((executable, arguments.ToArray()));
    if (executable == "dpkg-query")
    {
        return Success("base-files\tinstall ok installed\nopenssl:amd64\tinstall ok installed\nold-package\tdeinstall ok config-files\n");
    }

    if (executable == "apt-get")
    {
        return Success("Inst openssl [3.0.0] (3.0.1 Ubuntu:24.04/noble-security [amd64])\nInst example [1.0] (1.1 Ubuntu:24.04/noble-updates [amd64])\n");
    }

    return new ReadOnlyCommandResult(false, false, -1, string.Empty, "Unexpected package test command.");
});
var inventoryEvidence = await new InstalledPackageInventoryEvidenceProvider(packageRunner).CollectAsync();
var securityUpdateEvidence = await new SecurityUpdateEvidenceProvider(packageRunner).CollectAsync();
if (inventoryEvidence.State != EvidenceState.Healthy ||
    !inventoryEvidence.Summary.Contains("2 installed", StringComparison.Ordinal) ||
    securityUpdateEvidence.State != EvidenceState.Attention ||
    !securityUpdateEvidence.Summary.Contains("1 security update", StringComparison.Ordinal) ||
    !securityUpdateEvidence.Summary.Contains("openssl", StringComparison.Ordinal))
{
    failures.Add("Package Intelligence must count installed packages and identify cached security-update candidates.");
}

if (packageCommands.Any(command =>
        command.Arguments.Contains("update", StringComparer.Ordinal) ||
        command.Arguments.Contains("install", StringComparer.Ordinal) ||
        command.Arguments.Contains("dist-upgrade", StringComparer.Ordinal)))
{
    failures.Add("Package Intelligence must never refresh repositories, install packages, or simulate a distribution upgrade.");
}

var restartRoot = Path.Combine(Path.GetTempPath(), $"pulse-restart-{Guid.NewGuid():N}");
try
{
    Directory.CreateDirectory(restartRoot);
    var restartRequired = Path.Combine(restartRoot, "reboot-required");
    var restartPackages = Path.Combine(restartRoot, "reboot-required.pkgs");
    await File.WriteAllTextAsync(restartRequired, "System restart required\n");
    await File.WriteAllTextAsync(restartPackages, "linux-image-test\nlibc6\n");
    var restartEvidence = await new RestartRequirementEvidenceProvider(restartRequired, restartPackages).CollectAsync();
    if (restartEvidence.State != EvidenceState.Attention ||
        !restartEvidence.Summary.Contains("linux-image-test", StringComparison.Ordinal) ||
        !restartEvidence.Guidance.Contains("will not restart", StringComparison.OrdinalIgnoreCase))
    {
        failures.Add("Package Intelligence must explain a Debian restart marker without restarting automatically.");
    }
}
finally
{
    if (Directory.Exists(restartRoot))
    {
        Directory.Delete(restartRoot, true);
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
var hardeningInformationHealth = PulseHealthInterpreter.Interpret(
[
    new EvidenceResult("test.hardening", "Optional hardening", EvidenceState.Informational,
        "An optional hardening control is disabled.", "Review it if desired.", "test")
]);
if (optimizedHealth.State != "Optimized" || optimizedHealth.Score != 100 ||
    attentionHealth.State != "Attention Recommended" || attentionHealth.Score > 79 ||
    hardeningInformationHealth.State != "Optimized" || hardeningInformationHealth.Score != 100)
{
    failures.Add("Pulse Standard health language must remain status-first, attention-aware, and must not penalize optional hardening information.");
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
    var artifacts = await archive.SaveAsync(platform, evidence, "0.0.0.22",
        new DateTimeOffset(2026, 8, 3, 12, 34, 56, TimeSpan.Zero));

    if (!File.Exists(artifacts.SnapshotPath) || !File.Exists(artifacts.ReportPath) || !File.Exists(artifacts.ActivityLogPath))
    {
        failures.Add("Piece 4 must save a JSON snapshot, HTML report, and activity log.");
    }
    else
    {
        using var document = System.Text.Json.JsonDocument.Parse(await File.ReadAllTextAsync(artifacts.SnapshotPath));
        if (document.RootElement.GetProperty("PulseVersion").GetString() != "0.0.0.22")
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

sealed class StaticHttpMessageHandler(
    Func<HttpRequestMessage, HttpResponseMessage> handler) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken) =>
        Task.FromResult(handler(request));
}
