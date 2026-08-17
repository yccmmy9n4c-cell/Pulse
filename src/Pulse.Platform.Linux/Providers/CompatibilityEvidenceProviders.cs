using System.Runtime.InteropServices;
using Pulse.Platform.Linux.Platform;

namespace Pulse.Platform.Linux.Providers;

public sealed class DistributionCompatibilityEvidenceProvider : ILinuxEvidenceProvider
{
    private readonly DistributionSupportDetector _detector;
    private readonly string _osReleasePath;

    public DistributionCompatibilityEvidenceProvider(
        DistributionSupportDetector? detector = null,
        string osReleasePath = "/etc/os-release")
    {
        _detector = detector ?? new DistributionSupportDetector();
        _osReleasePath = osReleasePath;
    }

    public string Id => "linux.compatibility-distribution";

    public Task<EvidenceResult> CollectAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var support = _detector.Detect(_osReleasePath);
        var state = support.Level == DistributionSupportLevel.Supported
            ? EvidenceState.Healthy
            : EvidenceState.Informational;
        var summary = support.Level switch
        {
            DistributionSupportLevel.Supported => $"{support.DisplayName} is inside the verified Pulse Debian-family boundary.",
            DistributionSupportLevel.UnverifiedDerivative => $"{support.DisplayName} reports Debian compatibility but is not yet verified by Pulse.",
            _ => $"{support.DisplayName} is outside the verified Pulse Linux distribution boundary."
        };
        return Task.FromResult(new EvidenceResult(Id, "Distribution compatibility", state, summary,
            support.Message, _osReleasePath));
    }
}

public sealed class ArchitectureCompatibilityEvidenceProvider(string? architecture = null) : ILinuxEvidenceProvider
{
    public string Id => "linux.compatibility-architecture";

    public Task<EvidenceResult> CollectAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var current = architecture ?? RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant();
        return Task.FromResult(current switch
        {
            "x64" => new EvidenceResult(Id, "Architecture compatibility", EvidenceState.Healthy,
                "This system is linux-x64, the currently validated primary Pulse Linux architecture.",
                "Use the architecture-specific amd64 Debian package. Pulse never substitutes a package built for another architecture.",
                "RuntimeInformation.ProcessArchitecture"),
            "arm64" => new EvidenceResult(Id, "Architecture compatibility", EvidenceState.Informational,
                "This system is linux-arm64. Pulse source support is prepared, but physical arm64 acceptance remains deferred until the x64 gate is complete.",
                "Use only a verified arm64 package when it becomes available; do not install the amd64 package on this system.",
                "RuntimeInformation.ProcessArchitecture"),
            _ => new EvidenceResult(Id, "Architecture compatibility", EvidenceState.Informational,
                $"This system reports the {current} process architecture, which does not yet have a Pulse Linux package target.",
                "Pulse currently targets linux-x64 first and linux-arm64 afterward. No cross-architecture package will be selected automatically.",
                "RuntimeInformation.ProcessArchitecture")
        });
    }
}

public sealed class DesktopEnvironmentEvidenceProvider(Func<string, string?>? environmentReader = null) : ILinuxEvidenceProvider
{
    private readonly Func<string, string?> _environmentReader = environmentReader ?? Environment.GetEnvironmentVariable;

    public string Id => "linux.compatibility-desktop";

    public Task<EvidenceResult> CollectAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var desktop = First("XDG_CURRENT_DESKTOP", "DESKTOP_SESSION", "GDMSESSION");
        if (string.IsNullOrWhiteSpace(desktop))
        {
            return Task.FromResult(new EvidenceResult(Id, "Desktop environment", EvidenceState.Informational,
                "No desktop-environment identity was exposed to this Pulse session.",
                "This is normal for headless scheduled assessments. In an interactive session, Pulse supports conventional Debian-family desktops and falls back to in-app guidance when a desktop utility is unavailable.",
                "XDG_CURRENT_DESKTOP, DESKTOP_SESSION, and GDMSESSION"));
        }

        var recognized = new[] { "gnome", "cinnamon", "mate", "kde", "plasma", "xfce", "lxqt" }
            .Any(value => desktop.Contains(value, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(new EvidenceResult(Id, "Desktop environment",
            recognized ? EvidenceState.Healthy : EvidenceState.Informational,
            recognized
                ? $"Pulse detected the {desktop} desktop session."
                : $"Pulse detected the {desktop} desktop session, which has not yet completed dedicated Pulse validation.",
            recognized
                ? "Pulse will use an installed native settings utility when a safe match exists and otherwise retain in-app guidance."
                : "Core Pulse pages remain available, but native settings-launch actions may fall back to in-app guidance until this desktop is verified.",
            "Desktop session environment variables"));
    }

    private string? First(params string[] names) => names
        .Select(_environmentReader)
        .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
}

public sealed class DisplaySessionEvidenceProvider(Func<string, string?>? environmentReader = null) : ILinuxEvidenceProvider
{
    private readonly Func<string, string?> _environmentReader = environmentReader ?? Environment.GetEnvironmentVariable;

    public string Id => "linux.compatibility-display";

    public Task<EvidenceResult> CollectAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var sessionType = _environmentReader("XDG_SESSION_TYPE")?.Trim().ToLowerInvariant();
        var hasWayland = !string.IsNullOrWhiteSpace(_environmentReader("WAYLAND_DISPLAY"));
        var hasX11 = !string.IsNullOrWhiteSpace(_environmentReader("DISPLAY"));
        var display = sessionType switch
        {
            "wayland" => "Wayland",
            "x11" => "X11",
            _ when hasWayland => "Wayland",
            _ when hasX11 => "X11",
            _ => null
        };

        return Task.FromResult(display is null
            ? new EvidenceResult(Id, "Display session", EvidenceState.Informational,
                "No interactive X11 or Wayland display was exposed to this Pulse session.",
                "This is expected for the headless scheduled-assessment path. Launch the graphical application from the signed-in desktop session for UI validation.",
                "XDG_SESSION_TYPE, WAYLAND_DISPLAY, and DISPLAY")
            : new EvidenceResult(Id, "Display session", EvidenceState.Healthy,
                $"Pulse is running in an interactive {display} display session.",
                "Avalonia supplies the shared Pulse interface. Desktop-specific window behavior still requires physical validation on each supported distribution.",
                "XDG_SESSION_TYPE, WAYLAND_DISPLAY, and DISPLAY"));
    }
}

public sealed class UserServiceCompatibilityEvidenceProvider(IReadOnlyCommandRunner commandRunner) : ILinuxEvidenceProvider
{
    public string Id => "linux.compatibility-user-services";

    public async Task<EvidenceResult> CollectAsync(CancellationToken cancellationToken = default)
    {
        const string source = "systemctl --user is-system-running";
        var result = await commandRunner.RunAsync("systemctl", ["--user", "is-system-running"],
            TimeSpan.FromSeconds(8), cancellationToken);
        var status = result.StandardOutput.Trim().ToLowerInvariant();
        if (result.Started && !result.TimedOut && (status is "running" or "degraded"))
        {
            return new(Id, "User-service readiness", EvidenceState.Healthy,
                $"The signed-in systemd user manager is reachable and reports {status}.",
                status == "degraded"
                    ? "User scheduling remains available, but one or more user units may require separate Reliability review. Pulse does not reset or change them."
                    : "The user manager is available for the explicitly approved Pulse assessment timer. Pulse does not enable that timer automatically.", source);
        }

        return new(Id, "User-service readiness", EvidenceState.Informational,
            "The systemd user manager was not confirmed as running in this session.",
            "This can occur in a headless, remote, or incomplete login session. Pulse scheduling remains disabled unless the signed-in user explicitly approves it from a compatible session.", source);
    }
}

public sealed class IntelligenceToolCoverageEvidenceProvider(
    IReadOnlyList<string>? executableDirectories = null) : ILinuxEvidenceProvider
{
    private static readonly string[] CoreTools = ["systemctl", "journalctl", "dpkg", "apt-get", "ip", "findmnt", "lsblk"];
    private static readonly string[] OptionalTools = ["nmcli", "smartctl", "ufw", "nft", "systemd-analyze"];
    private readonly IReadOnlyList<string> _executableDirectories = executableDirectories ?? ["/usr/bin", "/usr/sbin", "/bin", "/sbin"];

    public string Id => "linux.compatibility-tool-coverage";

    public Task<EvidenceResult> CollectAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var availableCore = CoreTools.Where(Exists).ToArray();
        var missingCore = CoreTools.Except(availableCore, StringComparer.Ordinal).ToArray();
        var availableOptional = OptionalTools.Count(Exists);
        var state = missingCore.Length == 0 ? EvidenceState.Healthy : EvidenceState.Informational;
        var summary = missingCore.Length == 0
            ? $"All {CoreTools.Length} core Linux evidence tools are available; {availableOptional} of {OptionalTools.Length} optional tools were detected."
            : $"Detected {availableCore.Length} of {CoreTools.Length} core Linux evidence tools and {availableOptional} of {OptionalTools.Length} optional tools. Missing core coverage: {string.Join(", ", missingCore)}.";
        return Task.FromResult(new EvidenceResult(Id, "Intelligence tool coverage", state, summary,
            "Missing optional tools reduce evidence coverage but do not by themselves indicate poor system health. Pulse never installs a package automatically.",
            "Conventional executable paths; no package installation or repository refresh"));
    }

    private bool Exists(string executable) => _executableDirectories
        .Any(directory => File.Exists(Path.Combine(directory, executable)));
}
