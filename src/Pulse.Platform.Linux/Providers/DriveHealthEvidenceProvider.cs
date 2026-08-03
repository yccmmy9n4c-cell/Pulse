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
        var unavailable = observations.Count(item => item.State == DriveObservationState.Unavailable);
        var sleeping = observations.Count(item => item.State == DriveObservationState.Sleeping);
        var deviceNames = string.Join(", ", observations.Select(item => item.Device.DisplayName));

        if (attention.Length > 0)
        {
            return new(Id, "Physical drive health", EvidenceState.Attention,
                $"Drive-health indicators request review for {string.Join(", ", attention.Select(item => item.Device.DisplayName))}. Checked: {deviceNames}.",
                "Back up important data before running vendor diagnostics or repair tools. Pulse issued only read-only health queries and made no device changes.",
                "lsblk metadata; smartctl --nocheck=standby,3 --health or nvme smart-log when available");
        }

        if (healthy > 0 && unavailable == 0)
        {
            return new(Id, "Physical drive health", EvidenceState.Healthy,
                $"Readable health indicators passed for {healthy} drive(s). Sleeping and not awakened: {sleeping}. Devices: {deviceNames}.",
                "A passing SMART/NVMe indicator does not replace backups or predict every failure. Pulse did not wake sleeping drives or start a self-test.",
                "lsblk metadata; smartctl --nocheck=standby,3 --health or nvme smart-log when available");
        }

        return new(Id, "Physical drive health", EvidenceState.Informational,
            $"Pulse read health indicators for {healthy} drive(s); {unavailable} were inaccessible or unsupported and {sleeping} were left asleep. Devices: {deviceNames}.",
            "Coverage is incomplete, not a detected failure. Pulse did not elevate privileges, wake sleeping drives, or start a self-test.",
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

        var attentionMask = (1 << 3) | (1 << 4) | (1 << 5) | (1 << 6) | (1 << 7);
        if ((result.ExitCode & attentionMask) != 0 ||
            combined.Contains("FAILED", StringComparison.OrdinalIgnoreCase))
        {
            return new(device, DriveObservationState.Attention, "health indicator requested review");
        }

        if (result.ExitCode == 0 &&
            (combined.Contains("PASSED", StringComparison.OrdinalIgnoreCase) ||
             combined.Contains("OK", StringComparison.OrdinalIgnoreCase)))
        {
            return new(device, DriveObservationState.Healthy, "health indicator passed");
        }

        return new(device, DriveObservationState.Unavailable, "health result not readable");
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
            var needsReview = warning > 0 || (spare >= 0 && spare < 10) || used >= 90 || mediaErrors > 0;
            return new(device, needsReview ? DriveObservationState.Attention : DriveObservationState.Healthy,
                needsReview ? "NVMe health indicator requested review" : "NVMe health indicators passed");
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
        Sleeping,
        Unavailable
    }

    private sealed record DriveObservation(DriveDevice Device, DriveObservationState State, string Detail);
}
