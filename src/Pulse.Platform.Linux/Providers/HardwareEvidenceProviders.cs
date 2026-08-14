using System.Globalization;
using Pulse.Platform.Linux.Platform;

namespace Pulse.Platform.Linux.Providers;

public sealed class ProcessorIdentityEvidenceProvider(string cpuInfoPath = "/proc/cpuinfo") : ILinuxEvidenceProvider
{
    public string Id => "linux.hardware-processor";

    public async Task<EvidenceResult> CollectAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(cpuInfoPath))
        {
            return EvidenceResult.Unavailable(Id, "Processor", cpuInfoPath,
                "Linux did not expose the standard processor information file.");
        }

        var lines = await File.ReadAllLinesAsync(cpuInfoPath, cancellationToken);
        var model = ReadFirst(lines, "model name") ?? ReadFirst(lines, "Processor") ?? "Processor model not exposed";
        var logical = lines.Count(line => line.StartsWith("processor", StringComparison.OrdinalIgnoreCase) && line.Contains(':'));
        var coreCount = ReadFirst(lines, "cpu cores");
        var detail = logical > 0 ? $"{logical} logical processor(s)" : "logical processor count not exposed";
        if (int.TryParse(coreCount, NumberStyles.Integer, CultureInfo.InvariantCulture, out var cores) && cores > 0)
        {
            detail += $"; {cores} core(s) reported per physical package";
        }

        return new(Id, "Processor", EvidenceState.Informational,
            $"{model.Trim()} • {detail}.",
            "Processor identity and topology are system context. Pulse does not change CPU governors, clocks, affinity, or firmware settings.",
            cpuInfoPath);
    }

    private static string? ReadFirst(IEnumerable<string> lines, string key) =>
        lines.FirstOrDefault(line => line.StartsWith(key, StringComparison.OrdinalIgnoreCase))
            ?.Split(':', 2).ElementAtOrDefault(1)?.Trim();
}

public sealed class PhysicalMemoryEvidenceProvider(string memInfoPath = "/proc/meminfo") : ILinuxEvidenceProvider
{
    public string Id => "linux.hardware-memory";

    public async Task<EvidenceResult> CollectAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(memInfoPath))
        {
            return EvidenceResult.Unavailable(Id, "Installed memory", memInfoPath,
                "Linux did not expose the standard memory information file.");
        }

        var line = (await File.ReadAllLinesAsync(memInfoPath, cancellationToken))
            .FirstOrDefault(value => value.StartsWith("MemTotal:", StringComparison.Ordinal));
        var token = line?.Split(' ', StringSplitOptions.RemoveEmptyEntries).ElementAtOrDefault(1);
        if (!long.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out var kib) || kib <= 0)
        {
            return EvidenceResult.Unavailable(Id, "Installed memory", memInfoPath,
                "The installed-memory value was not in the expected Linux format.");
        }

        var gib = kib / 1024d / 1024d;
        return new(Id, "Installed memory", EvidenceState.Informational,
            $"Linux reports {gib:F1} GiB of physical memory.",
            "Installed capacity is hardware context. Current available-memory health is assessed separately by Performance Intelligence.",
            memInfoPath);
    }
}

public sealed class FirmwareIdentityEvidenceProvider(string dmiRoot = "/sys/class/dmi/id") : ILinuxEvidenceProvider
{
    public string Id => "linux.hardware-firmware";

    public async Task<EvidenceResult> CollectAsync(CancellationToken cancellationToken = default)
    {
        var vendor = await ReadOptionalAsync("sys_vendor", cancellationToken);
        var product = await ReadOptionalAsync("product_name", cancellationToken);
        var biosVendor = await ReadOptionalAsync("bios_vendor", cancellationToken);
        var biosVersion = await ReadOptionalAsync("bios_version", cancellationToken);
        if (new[] { vendor, product, biosVendor, biosVersion }.All(string.IsNullOrWhiteSpace))
        {
            return EvidenceResult.Unavailable(Id, "Firmware and system identity", dmiRoot,
                "No readable DMI system or firmware identity was exposed to the current user.");
        }

        var system = string.Join(" ", new[] { vendor, product }.Where(value => !string.IsNullOrWhiteSpace(value)));
        var firmware = string.Join(" ", new[] { biosVendor, biosVersion }.Where(value => !string.IsNullOrWhiteSpace(value)));
        return new(Id, "Firmware and system identity", EvidenceState.Informational,
            $"System: {ValueOrUnknown(system)}. Firmware: {ValueOrUnknown(firmware)}.",
            "Pulse records readable DMI identity as context only. It does not flash firmware or change UEFI settings.",
            $"{dmiRoot}/sys_vendor, product_name, bios_vendor, bios_version");
    }

    private async Task<string?> ReadOptionalAsync(string name, CancellationToken cancellationToken)
    {
        var path = System.IO.Path.Combine(dmiRoot, name);
        return File.Exists(path) ? (await File.ReadAllTextAsync(path, cancellationToken)).Trim() : null;
    }

    private static string ValueOrUnknown(string value) => string.IsNullOrWhiteSpace(value) ? "not exposed" : value;
}

public sealed class BatteryConditionEvidenceProvider(string powerSupplyRoot = "/sys/class/power_supply") : ILinuxEvidenceProvider
{
    public string Id => "linux.hardware-battery";

    public async Task<EvidenceResult> CollectAsync(CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(powerSupplyRoot))
        {
            return new(Id, "Battery condition", EvidenceState.Informational,
                "No Linux power-supply class was exposed; this is normal on many desktop systems.",
                "No battery action is required. Pulse does not alter charging thresholds or power policy.", powerSupplyRoot);
        }

        var batteries = new List<string>();
        foreach (var directory in Directory.EnumerateDirectories(powerSupplyRoot))
        {
            var type = await ReadOptionalAsync(directory, "type", cancellationToken);
            if (type.Equals("Battery", StringComparison.OrdinalIgnoreCase))
            {
                batteries.Add(directory);
            }
        }

        if (batteries.Count == 0)
        {
            return new(Id, "Battery condition", EvidenceState.Informational,
                "No system battery was detected; this is normal for a desktop computer.",
                "No battery action is required.", powerSupplyRoot);
        }

        var battery = batteries[0];
        var name = System.IO.Path.GetFileName(battery);
        var status = await ReadOptionalAsync(battery, "status", cancellationToken);
        var charge = await ReadLongAsync(battery, "capacity", cancellationToken);
        var full = await ReadLongAsync(battery, "energy_full", cancellationToken)
            ?? await ReadLongAsync(battery, "charge_full", cancellationToken);
        var design = await ReadLongAsync(battery, "energy_full_design", cancellationToken)
            ?? await ReadLongAsync(battery, "charge_full_design", cancellationToken);
        var health = full is > 0 && design is > 0 ? Math.Clamp(full.Value * 100d / design.Value, 0, 150) : null;
        var state = health is null
            ? EvidenceState.Informational
            : health.Value < 60
                ? EvidenceState.Attention
                : health.Value < 80
                    ? EvidenceState.Informational
                    : EvidenceState.Healthy;
        var chargeText = charge is null ? "current charge not exposed" : $"{charge}% charged";
        var healthText = health is null ? "design-capacity retention not exposed" : $"approximately {health:F0}% of design capacity";
        return new(Id, "Battery condition", state,
            $"{name}: {ValueOrUnknown(status)}, {chargeText}, {healthText}.",
            state == EvidenceState.Attention
                ? "The reported full-charge capacity is materially below its design value. Confirm behavior over several charge cycles and use the computer manufacturer's battery guidance before considering replacement."
                : "Battery capacity estimates vary with calibration, temperature, and firmware. Pulse records the exposed values and does not change charging or power settings.",
            $"{battery}/status, capacity, energy_* or charge_*");
    }

    private static async Task<string> ReadOptionalAsync(string directory, string name, CancellationToken cancellationToken)
    {
        var path = System.IO.Path.Combine(directory, name);
        return File.Exists(path) ? (await File.ReadAllTextAsync(path, cancellationToken)).Trim() : string.Empty;
    }

    private static async Task<long?> ReadLongAsync(string directory, string name, CancellationToken cancellationToken)
    {
        var text = await ReadOptionalAsync(directory, name, cancellationToken);
        return long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : null;
    }

    private static string ValueOrUnknown(string value) => string.IsNullOrWhiteSpace(value) ? "status not exposed" : value;
}

public sealed class GraphicsHardwareEvidenceProvider(string drmRoot = "/sys/class/drm") : ILinuxEvidenceProvider
{
    public string Id => "linux.hardware-graphics";

    public async Task<EvidenceResult> CollectAsync(CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(drmRoot))
        {
            return EvidenceResult.Unavailable(Id, "Graphics hardware", drmRoot,
                "The standard Linux DRM hardware class was not available.");
        }

        var adapters = new List<string>();
        foreach (var card in Directory.EnumerateDirectories(drmRoot, "card*"))
        {
            var name = System.IO.Path.GetFileName(card);
            if (name.Contains('-', StringComparison.Ordinal) || !Directory.Exists(System.IO.Path.Combine(card, "device")))
            {
                continue;
            }

            var vendor = await ReadOptionalAsync(card, "device/vendor", cancellationToken);
            var device = await ReadOptionalAsync(card, "device/device", cancellationToken);
            var driverLink = new DirectoryInfo(System.IO.Path.Combine(card, "device/driver"));
            var driver = driverLink.Exists ? driverLink.ResolveLinkTarget(false)?.Name : null;
            adapters.Add($"{name} {vendor}/{device}{(string.IsNullOrWhiteSpace(driver) ? string.Empty : $" using {driver}")}");
        }

        if (adapters.Count == 0)
        {
            return EvidenceResult.Unavailable(Id, "Graphics hardware", drmRoot,
                "No readable DRM graphics adapter was exposed to Pulse.");
        }

        return new(Id, "Graphics hardware", EvidenceState.Informational,
            $"Linux exposes {adapters.Count} graphics adapter(s): {string.Join("; ", adapters)}.",
            "Graphics identifiers and active kernel drivers are context. Pulse does not install drivers or change display settings.",
            $"{drmRoot}/card*/device");
    }

    private static async Task<string> ReadOptionalAsync(string root, string relativePath, CancellationToken cancellationToken)
    {
        var path = System.IO.Path.Combine(root, relativePath);
        return File.Exists(path) ? (await File.ReadAllTextAsync(path, cancellationToken)).Trim() : "unknown";
    }
}

public sealed class VirtualizationPostureEvidenceProvider(IReadOnlyCommandRunner commandRunner) : ILinuxEvidenceProvider
{
    public string Id => "linux.hardware-virtualization";

    public async Task<EvidenceResult> CollectAsync(CancellationToken cancellationToken = default)
    {
        const string source = "systemd-detect-virt --vm";
        var result = await commandRunner.RunAsync("systemd-detect-virt", ["--vm"], TimeSpan.FromSeconds(5), cancellationToken);
        if (!result.Started || result.TimedOut)
        {
            return EvidenceResult.Unavailable(Id, "Virtualization posture", source,
                result.TimedOut ? "The virtualization query timed out." : "systemd-detect-virt was not installed.");
        }

        var value = result.StandardOutput.Trim();
        if (result.ExitCode == 1 || value.Equals("none", StringComparison.OrdinalIgnoreCase))
        {
            return new(Id, "Virtualization posture", EvidenceState.Informational,
                "No virtual-machine environment was detected.",
                "This is system context, not a health judgment. Pulse does not enable virtualization features or change firmware settings.", source);
        }

        if (result.ExitCode != 0 || string.IsNullOrWhiteSpace(value))
        {
            return EvidenceResult.Unavailable(Id, "Virtualization posture", source,
                "The virtualization environment could not be identified.");
        }

        return new(Id, "Virtualization posture", EvidenceState.Informational,
            $"Pulse is running inside a {value} virtual-machine environment.",
            "Virtualization can affect which firmware, battery, thermal, storage, and hardware sensors are exposed. This is context and not a fault.", source);
    }
}
