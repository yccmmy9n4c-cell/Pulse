using Pulse.Platform.Linux.Platform;

namespace Pulse.Platform.Linux.Providers;

public sealed class RpmDatabaseEvidenceProvider(IReadOnlyCommandRunner commandRunner) : ILinuxEvidenceProvider
{
    public string Id => "linux.rpm-verifydb";

    public async Task<EvidenceResult> CollectAsync(CancellationToken cancellationToken = default)
    {
        const string source = "rpm --verifydb";
        var result = await commandRunner.RunAsync("rpm", ["--verifydb"], cancellationToken: cancellationToken);
        if (!result.Started || result.TimedOut)
        {
            return EvidenceResult.Unavailable(Id, "RPM database health", source,
                result.TimedOut ? "The read-only RPM database check timed out." : "The rpm command is unavailable.");
        }

        if (result.ExitCode == 0)
        {
            return new(Id, "RPM database health", EvidenceState.Healthy,
                "RPM completed its local database verification without reporting an error.",
                "No package-database repair is currently indicated. Pulse did not modify RPM state.", source);
        }

        var detail = string.Join(' ', new[] { result.StandardOutput, result.StandardError }
            .Where(value => !string.IsNullOrWhiteSpace(value))).Trim();
        return new(Id, "RPM database health", EvidenceState.Attention,
            string.IsNullOrWhiteSpace(detail) ? $"rpm --verifydb exited with code {result.ExitCode}." : detail,
            "Review the RPM database with Fedora's supported package tools. Pulse did not run a repair command.", source);
    }
}

public sealed class RpmInventoryEvidenceProvider(IReadOnlyCommandRunner commandRunner) : ILinuxEvidenceProvider
{
    public string Id => "linux.rpm-inventory";

    public async Task<EvidenceResult> CollectAsync(CancellationToken cancellationToken = default)
    {
        const string source = "rpm -qa --qf %{NAME}\\n";
        var result = await commandRunner.RunAsync("rpm", ["-qa", "--qf", "%{NAME}\\n"],
            TimeSpan.FromSeconds(20), cancellationToken);
        if (!result.Started || result.TimedOut || result.ExitCode != 0)
        {
            return EvidenceResult.Unavailable(Id, "Installed package inventory", source,
                result.TimedOut ? "The local RPM inventory query timed out." : "RPM could not provide its installed-package inventory.");
        }

        var count = result.StandardOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Length;
        return new(Id, "Installed package inventory", EvidenceState.Informational,
            $"The local RPM database contains {count:N0} installed package(s).",
            "This count is inventory context, not a health score. Pulse does not retain package names in the assessment summary.", source);
    }
}

public sealed class DnfCachedUpdateEvidenceProvider(IReadOnlyCommandRunner commandRunner) : ILinuxEvidenceProvider
{
    public string Id => "linux.dnf-cached-updates";

    public async Task<EvidenceResult> CollectAsync(CancellationToken cancellationToken = default)
    {
        const string source = "dnf --cacheonly --quiet check-upgrade";
        var result = await commandRunner.RunAsync("dnf", ["--cacheonly", "--quiet", "check-upgrade"],
            TimeSpan.FromSeconds(25), cancellationToken);
        if (!result.Started || result.TimedOut || result.ExitCode is not (0 or 100))
        {
            return EvidenceResult.Unavailable(Id, "Cached available updates", source,
                result.TimedOut ? "The cache-only DNF check timed out." : "DNF could not evaluate its existing local cache.");
        }

        var updates = result.StandardOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Count(line => !line.StartsWith("Last metadata expiration check", StringComparison.OrdinalIgnoreCase));
        if (result.ExitCode == 0 || updates == 0)
        {
            return new(Id, "Cached available updates", EvidenceState.Healthy,
                "DNF's existing local metadata does not currently list an available package update.",
                "This is a cache-only result and can be older than Fedora's repositories. Pulse did not refresh metadata.", source);
        }

        return new(Id, "Cached available updates", EvidenceState.Attention,
            $"DNF's existing local metadata lists approximately {updates:N0} update record(s).",
            "Open Fedora's graphical software updater to review and approve updates. Pulse did not download or install anything.", source);
    }
}

public sealed class DnfSecurityUpdateEvidenceProvider(IReadOnlyCommandRunner commandRunner) : ILinuxEvidenceProvider
{
    public string Id => "linux.dnf-security-updates";

    public async Task<EvidenceResult> CollectAsync(CancellationToken cancellationToken = default)
    {
        const string source = "dnf --cacheonly --quiet updateinfo list --security --available";
        var result = await commandRunner.RunAsync("dnf",
            ["--cacheonly", "--quiet", "updateinfo", "list", "--security", "--available"],
            TimeSpan.FromSeconds(25), cancellationToken);
        if (!result.Started || result.TimedOut || result.ExitCode != 0)
        {
            return EvidenceResult.Unavailable(Id, "Cached security updates", source,
                result.TimedOut ? "The cache-only DNF security query timed out." : "DNF update information was unavailable from the local cache.");
        }

        var count = result.StandardOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Count(line => !line.StartsWith("Last metadata expiration check", StringComparison.OrdinalIgnoreCase));
        return count == 0
            ? new(Id, "Cached security updates", EvidenceState.Healthy,
                "DNF's existing local metadata does not list an available security advisory.",
                "This is a cache-only result. Pulse did not contact repositories or install updates.", source)
            : new(Id, "Cached security updates", EvidenceState.Attention,
                $"DNF's existing local metadata lists {count:N0} available security advisory record(s).",
                "Open Fedora's graphical software updater to review and approve security updates.", source);
    }
}

public sealed class DnfAutomaticUpdatesEvidenceProvider(IReadOnlyCommandRunner commandRunner) : ILinuxEvidenceProvider
{
    public string Id => "linux.dnf-automatic";

    public async Task<EvidenceResult> CollectAsync(CancellationToken cancellationToken = default)
    {
        const string source = "systemctl is-enabled dnf-automatic.timer; /etc/dnf/automatic.conf";
        var timer = await commandRunner.RunAsync("systemctl", ["is-enabled", "dnf-automatic.timer"],
            cancellationToken: cancellationToken);
        var enabled = timer.Started && !timer.TimedOut && timer.ExitCode == 0 &&
            timer.StandardOutput.Trim().Equals("enabled", StringComparison.OrdinalIgnoreCase);
        var configExists = File.Exists("/etc/dnf/automatic.conf");
        return new(Id, "Automatic security updates", enabled ? EvidenceState.Healthy : EvidenceState.Informational,
            enabled
                ? "The dnf-automatic system timer is enabled."
                : configExists
                    ? "DNF automatic-update configuration exists, but the standard timer was not confirmed enabled."
                    : "The standard dnf-automatic timer and configuration were not confirmed.",
            enabled
                ? "Pulse confirmed timer enablement only; it did not prove recent update success."
                : "Automatic installation is a maintenance preference, not a current system error. Review Fedora update settings if desired.", source);
    }
}

public sealed class SelinuxEvidenceProvider : ILinuxEvidenceProvider
{
    public string Id => "linux.selinux";

    public async Task<EvidenceResult> CollectAsync(CancellationToken cancellationToken = default)
    {
        const string enforcePath = "/sys/fs/selinux/enforce";
        const string configPath = "/etc/selinux/config";
        if (!File.Exists(enforcePath))
        {
            return new(Id, "SELinux posture", EvidenceState.Informational,
                "The active SELinux enforcement indicator was not exposed to Pulse.",
                "SELinux may be disabled or unavailable. This is a hardening posture note, not proof of a system error.",
                $"{enforcePath}; {configPath}");
        }

        var value = (await File.ReadAllTextAsync(enforcePath, cancellationToken)).Trim();
        var enforcing = value == "1";
        return new(Id, "SELinux posture", enforcing ? EvidenceState.Healthy : EvidenceState.Informational,
            enforcing ? "The kernel reports SELinux enforcing." : "The kernel reports SELinux present but not enforcing.",
            enforcing
                ? "SELinux is active as a Fedora security layer. Pulse did not inspect or change policy."
                : "Permissive or disabled SELinux is a security choice, not a current reliability failure. Pulse made no changes.",
            $"{enforcePath}; {configPath}");
    }
}

public sealed class FedoraRestartRequirementEvidenceProvider(IReadOnlyCommandRunner commandRunner) : ILinuxEvidenceProvider
{
    public string Id => "linux.reboot-required";

    public async Task<EvidenceResult> CollectAsync(CancellationToken cancellationToken = default)
    {
        const string source = "dnf needs-restarting --reboothint";
        var result = await commandRunner.RunAsync("dnf", ["needs-restarting", "--reboothint"],
            TimeSpan.FromSeconds(15), cancellationToken);
        if (!result.Started || result.TimedOut || result.ExitCode is not (0 or 1))
        {
            return EvidenceResult.Unavailable(Id, "Restart requirement", source,
                result.TimedOut ? "The restart-hint query timed out." : "DNF restart-hint coverage is unavailable.");
        }

        return result.ExitCode == 0
            ? new(Id, "Restart requirement", EvidenceState.Healthy,
                "DNF does not currently report that a restart is required.",
                "This is a local package-state hint. Pulse did not restart the computer.", source)
            : new(Id, "Restart requirement", EvidenceState.Attention,
                "DNF reports that a restart should be reviewed after installed updates.",
                "Save your work and restart when convenient. Pulse will never restart the computer automatically.", source);
    }
}
