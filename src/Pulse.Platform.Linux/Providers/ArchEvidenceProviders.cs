using Pulse.Platform.Linux.Platform;

namespace Pulse.Platform.Linux.Providers;

public sealed class PacmanDatabaseEvidenceProvider(IReadOnlyCommandRunner commandRunner) : ILinuxEvidenceProvider
{
    public string Id => "linux.pacman-database";

    public async Task<EvidenceResult> CollectAsync(CancellationToken cancellationToken = default)
    {
        const string source = "pacman -Dk";
        var result = await commandRunner.RunAsync("pacman", ["-Dk"], TimeSpan.FromSeconds(20), cancellationToken);
        if (!result.Started || result.TimedOut)
        {
            return EvidenceResult.Unavailable(Id, "Pacman database health", source,
                result.TimedOut ? "The local pacman database check timed out." : "The pacman command is unavailable.");
        }

        return result.ExitCode == 0
            ? new(Id, "Pacman database health", EvidenceState.Healthy,
                "Pacman completed its local database consistency check without reporting an error.",
                "Pulse did not modify or synchronize the package database.", source)
            : new(Id, "Pacman database health", EvidenceState.Attention,
                "Pacman reported a local package-database consistency problem.",
                "Review the complete pacman output with Arch-supported tools. Pulse did not run a repair command.", source);
    }
}

public sealed class PacmanInventoryEvidenceProvider(IReadOnlyCommandRunner commandRunner) : ILinuxEvidenceProvider
{
    public string Id => "linux.pacman-inventory";

    public async Task<EvidenceResult> CollectAsync(CancellationToken cancellationToken = default)
    {
        const string source = "pacman -Qq";
        var result = await commandRunner.RunAsync("pacman", ["-Qq"], TimeSpan.FromSeconds(20), cancellationToken);
        if (!result.Started || result.TimedOut || result.ExitCode != 0)
        {
            return EvidenceResult.Unavailable(Id, "Installed package inventory", source,
                result.TimedOut ? "The local pacman inventory timed out." : "Pacman could not provide the installed-package inventory.");
        }

        var count = result.StandardOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Length;
        return new(Id, "Installed package inventory", EvidenceState.Informational,
            $"The local pacman database contains {count:N0} installed package(s).",
            "This count is inventory context, not a health score. Pulse does not retain package names in the assessment summary.", source);
    }
}

public sealed class PacmanCachedUpdateEvidenceProvider(IReadOnlyCommandRunner commandRunner) : ILinuxEvidenceProvider
{
    public string Id => "linux.pacman-cached-updates";

    public async Task<EvidenceResult> CollectAsync(CancellationToken cancellationToken = default)
    {
        const string source = "pacman -Qu";
        var result = await commandRunner.RunAsync("pacman", ["-Qu"], TimeSpan.FromSeconds(20), cancellationToken);
        if (!result.Started || result.TimedOut || result.ExitCode is not (0 or 1))
        {
            return EvidenceResult.Unavailable(Id, "Cached available updates", source,
                result.TimedOut ? "The local pacman update query timed out." : "Pacman could not compare installed packages with its current sync database.");
        }

        var count = result.StandardOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Length;
        return count == 0
            ? new(Id, "Cached available updates", EvidenceState.Healthy,
                "Pacman's current local sync database does not list an available package update.",
                "Pulse did not run pacman -Sy. This local result can be older than the configured mirrors.", source)
            : new(Id, "Cached available updates", EvidenceState.Attention,
                $"Pacman's current local sync database lists {count:N0} available package update(s).",
                "Use a supported full-system upgrade workflow such as pacman -Syu when you choose. Pulse did not synchronize or install anything.", source);
    }
}

public sealed class ArchSecurityCoverageEvidenceProvider : ILinuxEvidenceProvider
{
    public string Id => "linux.arch-security-coverage";

    public Task<EvidenceResult> CollectAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new EvidenceResult(Id, "Security update classification", EvidenceState.Informational,
            "Pacman's standard local databases do not classify available packages as security-only updates.",
            "Review all available updates through the normal full-system upgrade process and consult Arch security advisories when needed. Pulse will not invent a security score or contact a third-party service silently.",
            "Pacman local metadata boundary"));
    }
}

public sealed class ArchUpdatePolicyEvidenceProvider : ILinuxEvidenceProvider
{
    public string Id => "linux.arch-update-policy";

    public Task<EvidenceResult> CollectAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new EvidenceResult(Id, "Update policy", EvidenceState.Informational,
            "Arch updates are expected to be applied as user-approved full-system upgrades.",
            "Pulse does not enable an automatic partial-upgrade timer. Review and approve pacman -Syu through your normal maintenance workflow.",
            "Arch full-system upgrade policy"));
    }
}

public sealed class ArchMandatoryAccessControlEvidenceProvider : ILinuxEvidenceProvider
{
    public string Id => "linux.arch-mac";

    public async Task<EvidenceResult> CollectAsync(CancellationToken cancellationToken = default)
    {
        const string selinuxPath = "/sys/fs/selinux/enforce";
        const string appArmorPath = "/sys/module/apparmor/parameters/enabled";
        if (File.Exists(selinuxPath))
        {
            var value = (await File.ReadAllTextAsync(selinuxPath, cancellationToken)).Trim();
            return new(Id, "Mandatory access control", value == "1" ? EvidenceState.Healthy : EvidenceState.Informational,
                value == "1" ? "SELinux is present and enforcing." : "SELinux is present but not enforcing.",
                "Mandatory access control is an optional hardening choice on Arch. Pulse did not change policy.", selinuxPath);
        }

        if (File.Exists(appArmorPath))
        {
            var value = (await File.ReadAllTextAsync(appArmorPath, cancellationToken)).Trim();
            var enabled = value.StartsWith("Y", StringComparison.OrdinalIgnoreCase);
            return new(Id, "Mandatory access control", enabled ? EvidenceState.Healthy : EvidenceState.Informational,
                enabled ? "AppArmor is present and enabled." : "AppArmor is present but not enabled.",
                "Mandatory access control is an optional hardening choice on Arch. Pulse did not change policy.", appArmorPath);
        }

        return new(Id, "Mandatory access control", EvidenceState.Informational,
            "Pulse did not detect active SELinux or AppArmor kernel indicators.",
            "This is an optional hardening posture note, not proof of a system error. Pulse made no changes.",
            $"{selinuxPath}; {appArmorPath}");
    }
}

public sealed class ArchRestartRequirementEvidenceProvider(
    IReadOnlyCommandRunner commandRunner,
    string modulesRoot = "/usr/lib/modules") : ILinuxEvidenceProvider
{
    public string Id => "linux.reboot-required";

    public async Task<EvidenceResult> CollectAsync(CancellationToken cancellationToken = default)
    {
        const string source = "uname -r; /usr/lib/modules/<running-kernel>";
        var result = await commandRunner.RunAsync("uname", ["-r"], cancellationToken: cancellationToken);
        if (!result.Started || result.TimedOut || result.ExitCode != 0 || string.IsNullOrWhiteSpace(result.StandardOutput))
        {
            return EvidenceResult.Unavailable(Id, "Restart requirement", source, "The running kernel release could not be read.");
        }

        var release = result.StandardOutput.Trim();
        var modules = Path.Combine(modulesRoot, release);
        return Directory.Exists(modules)
            ? new(Id, "Restart requirement", EvidenceState.Healthy,
                "The module tree for the running kernel is still installed.",
                "Arch does not provide a universal restart-required marker. Pulse confirmed only this narrow running-kernel check.", source)
            : new(Id, "Restart requirement", EvidenceState.Attention,
                "The module tree for the running kernel is no longer present, which can occur after a kernel upgrade.",
                "Save your work and restart when convenient. Pulse will never restart the computer automatically.", source);
    }
}
