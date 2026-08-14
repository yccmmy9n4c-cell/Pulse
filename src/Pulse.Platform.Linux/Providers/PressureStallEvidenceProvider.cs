using System.Globalization;

namespace Pulse.Platform.Linux.Providers;

public enum PressureResource
{
    Cpu,
    Memory,
    Io
}

public sealed class PressureStallEvidenceProvider(
    PressureResource resource,
    string? pressurePath = null,
    string cgroupRoot = "/sys/fs/cgroup",
    string? bootConfigPath = null,
    string commandLinePath = "/proc/cmdline",
    string kernelReleasePath = "/proc/sys/kernel/osrelease") : ILinuxEvidenceProvider
{
    private string ProcPath => pressurePath ?? $"/proc/pressure/{ResourceName}";
    private string CgroupPath => System.IO.Path.Combine(cgroupRoot, $"{ResourceName}.pressure");
    private string ResourceName => resource.ToString().ToLowerInvariant();
    public string Id => $"linux.performance-{resource.ToString().ToLowerInvariant()}-pressure";

    public async Task<EvidenceResult> CollectAsync(CancellationToken cancellationToken = default)
    {
        var title = $"{(resource == PressureResource.Io ? "I/O" : resource.ToString())} pressure";
        var sourcePath = File.Exists(ProcPath)
            ? ProcPath
            : File.Exists(CgroupPath)
                ? CgroupPath
                : null;
        if (sourcePath is null)
        {
            var explanation = await ExplainUnavailableAsync(cancellationToken);
            return new(Id, title, EvidenceState.Unavailable,
                explanation.Summary,
                $"No system changes were made. {explanation.Guidance}",
                $"{ProcPath} or {CgroupPath}");
        }

        var lines = await File.ReadAllLinesAsync(sourcePath, cancellationToken);
        var some = ReadAverage(lines, "some");
        var full = ReadAverage(lines, "full");
        if (some is null)
        {
            return EvidenceResult.Unavailable(Id, title, sourcePath,
                "The pressure data was not in the expected Linux PSI format.");
        }

        var state = Classify(some.Value, full);
        var fullText = full is null ? "not exposed" : $"{full.Value:F2}%";
        return new(Id, title, state,
            $"Over the last 60 seconds, partial stall pressure averaged {some.Value:F2}% and full stall pressure was {fullText}.",
            state == EvidenceState.Attention
                ? "Pulse detected sustained resource waiting. Reassess after intentional heavy work completes; if responsiveness remains poor, review workload and storage or memory evidence before changing settings."
                : "PSI measures time tasks were delayed waiting for this resource. A single reading is context; repeated pressure alongside visible slowdown is more meaningful.",
            sourcePath);
    }

    private EvidenceState Classify(double some, double? full)
    {
        var fullValue = full ?? 0;
        return resource switch
        {
            PressureResource.Cpu when some >= 25 => EvidenceState.Attention,
            PressureResource.Cpu when some >= 10 => EvidenceState.Informational,
            PressureResource.Memory when some >= 10 || fullValue >= 2 => EvidenceState.Attention,
            PressureResource.Memory when some >= 3 || fullValue >= 0.5 => EvidenceState.Informational,
            PressureResource.Io when some >= 20 || fullValue >= 5 => EvidenceState.Attention,
            PressureResource.Io when some >= 5 || fullValue >= 1 => EvidenceState.Informational,
            _ => EvidenceState.Healthy
        };
    }

    private static double? ReadAverage(IEnumerable<string> lines, string category)
    {
        var line = lines.FirstOrDefault(value => value.StartsWith(category + " ", StringComparison.Ordinal));
        var token = line?.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault(value => value.StartsWith("avg60=", StringComparison.Ordinal));
        return token is not null && double.TryParse(token[6..], NumberStyles.Float,
            CultureInfo.InvariantCulture, out var value) ? value : null;
    }

    private async Task<(string Summary, string Guidance)> ExplainUnavailableAsync(CancellationToken cancellationToken)
    {
        var commandLine = File.Exists(commandLinePath)
            ? await File.ReadAllTextAsync(commandLinePath, cancellationToken)
            : string.Empty;
        if (commandLine.Split(' ', StringSplitOptions.RemoveEmptyEntries).Any(token => token.Equals("psi=0", StringComparison.Ordinal)))
        {
            return ("Linux PSI support is disabled by the current boot setting (psi=0).",
                "PSI is optional. If the user chooses to enable it, use the distribution's documented bootloader process to set psi=1 and reboot; Pulse will not change boot settings.");
        }

        var configPath = bootConfigPath;
        if (configPath is null && File.Exists(kernelReleasePath))
        {
            var release = (await File.ReadAllTextAsync(kernelReleasePath, cancellationToken)).Trim();
            if (!string.IsNullOrWhiteSpace(release))
            {
                configPath = $"/boot/config-{release}";
            }
        }

        var config = configPath is not null && File.Exists(configPath)
            ? await File.ReadAllTextAsync(configPath, cancellationToken)
            : string.Empty;
        var psiBuiltIn = config.Split('\n').Any(line => line.Trim().Equals("CONFIG_PSI=y", StringComparison.Ordinal));
        var defaultDisabled = config.Split('\n').Any(line => line.Trim().Equals("CONFIG_PSI_DEFAULT_DISABLED=y", StringComparison.Ordinal));
        var explicitlyEnabled = commandLine.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Any(token => token.Equals("psi=1", StringComparison.Ordinal));

        if (psiBuiltIn && defaultDisabled && !explicitlyEnabled)
        {
            return ("The running kernel includes PSI, but its configuration leaves PSI disabled unless the system starts with psi=1.",
                "This is a coverage limitation, not a performance fault. The user may enable psi=1 through the distribution's bootloader process and reboot, but Pulse will not make that system-level change.");
        }

        if (config.Contains("# CONFIG_PSI is not set", StringComparison.Ordinal))
        {
            return ("The running kernel was built without Linux Pressure Stall Information support.",
                "No repair is required. Pulse will use the other Performance Intelligence evidence and will not reduce health for this unavailable optional signal.");
        }

        return ("Linux did not expose system-wide or cgroup v2 Pressure Stall Information for this resource.",
            "This is incomplete performance coverage, not proof of a CPU, memory, or storage problem. Pulse will continue using available load, memory, and thermal evidence.");
    }
}
