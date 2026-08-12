using System.Text.Json;
using Pulse.Platform.Linux.Platform;

namespace Pulse.Platform.Linux.Providers;

public sealed class DriveHealthEvidenceProvider : ILinuxEvidenceProvider
{
    private readonly IReadOnlyCommandRunner _commandRunner;
    private readonly Func<string, string?> _toolLocator;

    public DriveHealthEvidenceProvider(
        IReadOnlyCommandRunner commandRunner,
        Func<string, string?>? toolLocator = null)
    {
        _commandRunner = commandRunner;
        _toolLocator = toolLocator ?? LocateTool;
    }

    public string Id => "linux.drive-health";

    public async Task<EvidenceResult> CollectAsync(CancellationToken cancellationToken = default)
    {
        var devicesResult = await _commandRunner.RunAsync("lsblk",
            ["--json", "--nodeps", "--output", "NAME,PATH,TYPE,MODEL,TRAN"],
            cancellationToken: cancellationToken);
        if (!Succeeded(devicesResult))
        {
            return EvidenceResult.Unavailable(Id, "Physical drive health", "lsblk --json --nodeps",
                devicesResult.TimedOut ? "The physical-drive query timed out." : "Pulse could not enumerate physical drives.");
        }

        IReadOnlyList<DriveDevice> devices;
        try
        {
            devices = ReadDevices(devicesResult.StandardOutput).Take(6).ToArray();
        }
        catch (JsonException ex)
        {
            return EvidenceResult.Unavailable(Id, "Physical drive health", "lsblk --json --nodeps", ex.Message);
        }

        if (devices.Count == 0)
        {
            return new(Id, "Physical drive health", EvidenceState.Informational,
                "No physical disk devices were visible in readable block metadata.",
                "This can be normal in a container, restricted virtual machine, or unusual storage layout. Pulse made no device changes.",
                "lsblk --json --nodeps --output NAME,PATH,TYPE,MODEL,TRAN");
        }

        var smartctl = _toolLocator("smartctl");
        var nvme = _toolLocator("nvme");
        if (smartctl is null && nvme is null)
        {
            return EvidenceResult.Unavailable(Id, "Physical drive health", "smartctl or nvme smart-log",
                $"Pulse found {devices.Count} physical drive(s), but optional SMART/NVMe tooling is not installed or discoverable.");
        }

        var observations = new List<DriveObservation>();
        foreach (var device in devices)
        {
            DriveObservation observation;
            if (smartctl is not null)
            {
                observation = await ReadSmartctlAsync(smartctl, device, cancellationToken);
                if (observation.State == DriveObservationState.Unavailable && nvme is not null && device.IsNvme)
                {
                    observation = await ReadNvmeAsync(nvme, device, cancellationToken);
                }
            }
            else if (nvme is not null && device.IsNvme)
            {
                observation = await ReadNvmeAsync(nvme, device, cancellationToken);
            }
            else
            {
                observation = new(device, DriveObservationState.Unavailable, "no compatible tool");
            }

            observations.Add(observation);
        }

        var attention = observations.Where(item => item.State == DriveObservationState.Attention).ToArray();
        var healthy = observations.Count(item => item.State == DriveObservationState.Healthy);
        var historical = observations.Where(item => item.State == DriveObservationState.Historical).ToArray();
        var unavailableItems = observations.Where(item => item.State == DriveObservationState.Unavailable).ToArray();
        var unavailable = unavailableItems.Length;
        var sleeping = observations.Count(item => item.State == DriveObservationState.Sleeping);
        var deviceNames = string.Join(", ", observations.Select(item => item.Device.DisplayName));

        if (attention.Length > 0)
        {
            return new(Id, "Physical drive health", EvidenceState.Attention,
                $"Current drive-health indicators request review for {string.Join(", ", attention.Select(item => item.Device.DisplayName))}. Checked: {deviceNames}.",
                "Back up important data before running vendor diagnostics or repair tools. Pulse issued only read-only health queries and made no device changes.",
                "lsblk metadata; smartctl --nocheck=standby,3 --health or nvme smart-log when available");
        }

        if (historical.Length > 0)
        {
            return new(Id, "Physical drive health", EvidenceState.Informational,
                $"The current health check did not report an active failure, but historical SMART/NVMe records exist for {string.Join(", ", historical.Select(item => item.Device.DisplayName))}.",
                "Historical error, attribute, or self-test records do not by themselves mean the drive is currently failing, and desktop tools may not display them. Keep a verified backup and use the drive manufacturer's diagnostic tool if errors recur or symptoms appear.",
                "lsblk metadata; standby-safe SMART/NVMe current health and historical status bits");
        }

        if (healthy > 0 && unavailable == 0)
        {
            return new(Id, "Physical drive health", EvidenceState.Healthy,
                $"Readable health indicators passed for {healthy} drive(s). Sleeping and not awakened: {sleeping}. Devices: {deviceNames}.",
                "A passing SMART/NVMe indicator does not replace backups or predict every failure. Pulse did not wake sleeping drives or start a self-test.",
                "lsblk metadata; smartctl --nocheck=standby,3 --health or nvme smart-log when available");
        }

        var unavailableReasons = string.Join("; ", unavailableItems.Select(item => item.Detail).Distinct(StringComparer.Ordinal));
        return new(Id, "Physical drive health", EvidenceState.Informational,
            $"Pulse read health indicators for {healthy} drive(s); {unavailable} were inaccessible or unsupported and {sleeping} were left asleep. Devices: {deviceNames}.{(string.IsNullOrWhiteSpace(unavailableReasons) ? string.Empty : $" Coverage detail: {unavailableReasons}.")}",
            "Incomplete access is not a detected drive failure. Use the distribution's disk utility to review SMART/NVMe health when available. Pulse did not elevate privileges, wake sleeping drives, or start a self-test.",
            "lsblk metadata; optional smartctl/nvme tooling");
    }

    private async Task<DriveObservation> ReadSmartctlAsync(
        string executable,
        DriveDevice device,
        CancellationToken cancellationToken)
    {
        var result = await _commandRunner.RunAsync(executable,
            ["--nocheck=standby,3", "--health", device.Path], TimeSpan.FromSeconds(12), cancellationToken);
        var combined = $"{result.StandardOutput}\n{result.StandardError}";
        if (!result.Started || result.TimedOut)
        {
            return new(device, DriveObservationState.Unavailable, "query unavailable");
        }

        if (combined.Contains("STANDBY", StringComparison.OrdinalIgnoreCase) ||
            combined.Contains("SLEEP", StringComparison.OrdinalIgnoreCase))
        {
            return new(device, DriveObservationState.Sleeping, "drive left asleep");
        }

        var currentFailureMask = (1 << 3) | (1 << 4);
        var historicalMask = (1 << 5) | (1 << 6) | (1 << 7);
        var queryFailureMask = (1 << 0) | (1 << 1) | (1 << 2);
        if ((result.ExitCode & currentFailureMask) != 0)
        {
            return new(device, DriveObservationState.Attention,
                $"smartctl reported a current health-failure status bit (exit code {result.ExitCode})");
        }

        if ((result.ExitCode & queryFailureMask) != 0)
        {
            var reason = combined.Contains("Permission denied", StringComparison.OrdinalIgnoreCase)
                ? "smartctl could not open the device with the current user's permissions"
                : $"smartctl could not complete the query (exit code {result.ExitCode})";
            return new(device, DriveObservationState.Unavailable, reason);
        }

        if (ReportsExplicitSmartFailure(combined))
        {
            return new(device, DriveObservationState.Attention,
                "smartctl explicitly reported a failed overall-health result");
        }

        if ((result.ExitCode & historicalMask) != 0)
        {
            return new(device, DriveObservationState.Historical,
                $"historical SMART status bit present (exit code {result.ExitCode})");
        }

        if (result.ExitCode == 0 &&
            ReportsExplicitSmartPass(combined))
        {
            return new(device, DriveObservationState.Healthy, "health indicator passed");
        }

        return new(device, DriveObservationState.Unavailable, "health result not readable");
    }

    private static bool ReportsExplicitSmartFailure(string output) =>
        ReadOverallHealthValue(output) is { } value &&
        (value.StartsWith("FAILED", StringComparison.OrdinalIgnoreCase) ||
         value.StartsWith("BAD", StringComparison.OrdinalIgnoreCase));

    private static bool ReportsExplicitSmartPass(string output) =>
        ReadOverallHealthValue(output) is { } value &&
        (value.StartsWith("PASSED", StringComparison.OrdinalIgnoreCase) ||
         value.StartsWith("OK", StringComparison.OrdinalIgnoreCase));

    private static string? ReadOverallHealthValue(string output)
    {
        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!line.StartsWith("SMART overall-health self-assessment test result:", StringComparison.OrdinalIgnoreCase) &&
                !line.StartsWith("SMART Health Status:", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var separator = line.IndexOf(':');
            return separator >= 0 ? line[(separator + 1)..].Trim() : null;
        }

        return null;
    }

    private async Task<DriveObservation> ReadNvmeAsync(
        string executable,
        DriveDevice device,
        CancellationToken cancellationToken)
    {
        var result = await _commandRunner.RunAsync(executable,
            ["smart-log", "--output-format=json", device.Path], TimeSpan.FromSeconds(12), cancellationToken);
        if (!Succeeded(result))
        {
            return new(device, DriveObservationState.Unavailable, "NVMe health query unavailable");
        }

        try
        {
            using var document = JsonDocument.Parse(result.StandardOutput);
            var root = document.RootElement;
            var warning = Number(root, "critical_warning");
            var spare = Number(root, "available_spare");
            var used = Number(root, "percentage_used");
            var mediaErrors = Number(root, "media_errors");
            var currentFailure = warning > 0 || (spare >= 0 && spare < 10) || used >= 90;
            if (currentFailure)
            {
                return new(device, DriveObservationState.Attention, "current NVMe health indicator requested review");
            }

            return mediaErrors > 0
                ? new(device, DriveObservationState.Historical, "historical NVMe media-error count present")
                : new(device, DriveObservationState.Healthy, "NVMe health indicators passed");
        }
        catch (JsonException)
        {
            return new(device, DriveObservationState.Unavailable, "NVMe health output was unreadable");
        }
    }

    private static IReadOnlyList<DriveDevice> ReadDevices(string json)
    {
        using var document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("blockdevices", out var blockDevices) ||
            blockDevices.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var devices = new List<DriveDevice>();
        foreach (var item in blockDevices.EnumerateArray())
        {
            if (!string.Equals(Text(item, "type"), "disk", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var name = Text(item, "name");
            var path = Text(item, "path");
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(path) || !path.StartsWith("/dev/", StringComparison.Ordinal))
            {
                continue;
            }

            devices.Add(new(name, path, Text(item, "model"), Text(item, "tran")));
        }

        return devices;
    }

    private static string? Text(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()?.Trim()
            : null;

    private static long Number(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var value))
        {
            return -1;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var numericValue))
        {
            return numericValue;
        }

        return value.ValueKind == JsonValueKind.String && long.TryParse(value.GetString(), out var textValue)
            ? textValue
            : -1;
    }

    private static bool Succeeded(ReadOnlyCommandResult result) =>
        result.Started && !result.TimedOut && result.ExitCode == 0;

    private static string? LocateTool(string name)
    {
        var candidates = new[] { $"/usr/sbin/{name}", $"/usr/bin/{name}", $"/sbin/{name}", $"/bin/{name}" };
        return candidates.FirstOrDefault(File.Exists);
    }

    private sealed record DriveDevice(string Name, string Path, string? Model, string? Transport)
    {
        public bool IsNvme => Name.StartsWith("nvme", StringComparison.OrdinalIgnoreCase) ||
                              Transport?.Equals("nvme", StringComparison.OrdinalIgnoreCase) == true;

        public string DisplayName => string.IsNullOrWhiteSpace(Model) ? Name : $"{Name} ({Model})";
    }

    private enum DriveObservationState
    {
        Healthy,
        Attention,
        Historical,
        Sleeping,
        Unavailable
    }

    private sealed record DriveObservation(DriveDevice Device, DriveObservationState State, string Detail);
}
