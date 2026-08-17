using System.Text.RegularExpressions;
using Pulse.Platform.Linux.Platform;

namespace Pulse.Platform.Linux.Providers;

public sealed class SystemdCriticalChainEvidenceProvider(IReadOnlyCommandRunner commandRunner) : ILinuxEvidenceProvider
{
    public string Id => "linux.startup-critical-chain";

    public async Task<EvidenceResult> CollectAsync(CancellationToken cancellationToken = default)
    {
        const string source = "systemd-analyze critical-chain --no-pager";
        var result = await commandRunner.RunAsync("systemd-analyze", ["critical-chain", "--no-pager"],
            TimeSpan.FromSeconds(10), cancellationToken);
        if (!result.Started || result.TimedOut || result.ExitCode != 0 || string.IsNullOrWhiteSpace(result.StandardOutput))
        {
            return EvidenceResult.Unavailable(Id, "Critical startup chain", source,
                result.TimedOut ? "The startup-chain query timed out." : "systemd did not expose a readable critical startup chain.");
        }

        var units = result.StandardOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(StripAnsi)
            .Select(line => line.TrimStart('└', '─', '├', '●', ' ', '\t'))
            .Where(line => line.Contains(".service", StringComparison.Ordinal) ||
                           line.Contains(".target", StringComparison.Ordinal) ||
                           line.Contains(".mount", StringComparison.Ordinal) ||
                           line.Contains(".device", StringComparison.Ordinal))
            .Take(5)
            .ToArray();
        if (units.Length == 0)
        {
            return EvidenceResult.Unavailable(Id, "Critical startup chain", source,
                "The startup-chain output did not contain recognizable systemd units.");
        }

        return new(Id, "Critical startup chain", EvidenceState.Informational,
            $"The current boot's leading critical-chain entries are: {string.Join("; ", units)}.",
            "Critical-chain timing is a diagnostic baseline, not proof that a service is faulty. Compare repeated boots and investigate only when startup is visibly delayed.", source);
    }

    private static string StripAnsi(string value) => Regex.Replace(value, "\\x1B\\[[0-?]*[ -/]*[@-~]", string.Empty);
}

public sealed class DesktopAutostartEvidenceProvider(
    string systemAutostartRoot = "/etc/xdg/autostart",
    string? userAutostartRoot = null) : ILinuxEvidenceProvider
{
    public string Id => "linux.startup-desktop-autostart";

    public async Task<EvidenceResult> CollectAsync(CancellationToken cancellationToken = default)
    {
        var userRoot = userAutostartRoot ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "autostart");
        var entries = new Dictionary<string, (string Name, bool Disabled)>(StringComparer.Ordinal);
        await ReadRootAsync(systemAutostartRoot, entries, overrideExisting: false, cancellationToken);
        await ReadRootAsync(userRoot, entries, overrideExisting: true, cancellationToken);

        if (entries.Count == 0)
        {
            return new(Id, "Desktop autostart", EvidenceState.Informational,
                "No standard XDG desktop-autostart entries were detected.",
                "This is normal when the desktop or installed applications do not use XDG autostart files. Pulse does not create or remove startup entries.",
                $"{systemAutostartRoot} and {userRoot}");
        }

        var enabled = entries.Values.Where(entry => !entry.Disabled).ToArray();
        var disabled = entries.Count - enabled.Length;
        var examples = enabled.Select(entry => entry.Name).Where(name => !string.IsNullOrWhiteSpace(name)).Take(4).ToArray();
        var exampleText = examples.Length == 0 ? string.Empty : $" Examples: {string.Join(", ", examples)}.";
        return new(Id, "Desktop autostart", EvidenceState.Informational,
            $"Detected {enabled.Length} enabled and {disabled} disabled XDG desktop-autostart entry or override(s).{exampleText}",
            "Autostart entries are context, not faults. Review only unfamiliar or unwanted items through the installed desktop's Startup Applications settings.",
            $"{systemAutostartRoot} and {userRoot}");
    }

    private static async Task ReadRootAsync(
        string root,
        IDictionary<string, (string Name, bool Disabled)> entries,
        bool overrideExisting,
        CancellationToken cancellationToken)
    {
        if (!Directory.Exists(root))
        {
            return;
        }

        foreach (var path in Directory.EnumerateFiles(root, "*.desktop", SearchOption.TopDirectoryOnly))
        {
            var key = Path.GetFileName(path);
            if (!overrideExisting && entries.ContainsKey(key))
            {
                continue;
            }

            var lines = await File.ReadAllLinesAsync(path, cancellationToken);
            var nameLine = lines.FirstOrDefault(line => line.StartsWith("Name=", StringComparison.Ordinal));
            var name = nameLine is null ? null : nameLine[5..].Trim();
            var hidden = lines.Any(line => line.Trim().Equals("Hidden=true", StringComparison.OrdinalIgnoreCase));
            entries[key] = (string.IsNullOrWhiteSpace(name) ? Path.GetFileNameWithoutExtension(path) : name!, hidden);
        }
    }
}

public sealed class EnabledUserUnitsEvidenceProvider(IReadOnlyCommandRunner commandRunner) : ILinuxEvidenceProvider
{
    public string Id => "linux.startup-enabled-user-units";

    public async Task<EvidenceResult> CollectAsync(CancellationToken cancellationToken = default)
    {
        const string source = "systemctl --user list-unit-files --state=enabled --no-legend --no-pager";
        var result = await commandRunner.RunAsync("systemctl",
            ["--user", "list-unit-files", "--state=enabled", "--no-legend", "--no-pager"],
            TimeSpan.FromSeconds(10), cancellationToken);
        if (!result.Started || result.TimedOut || result.ExitCode != 0)
        {
            return EvidenceResult.Unavailable(Id, "Enabled user services and timers", source,
                result.TimedOut ? "The signed-in user's enabled-unit query timed out." : "The user systemd manager was not readable in this session.");
        }

        var names = result.StandardOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(line => line.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault())
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!)
            .ToArray();
        var services = names.Count(name => name.EndsWith(".service", StringComparison.Ordinal));
        var timers = names.Count(name => name.EndsWith(".timer", StringComparison.Ordinal));
        var other = names.Length - services - timers;
        return new(Id, "Enabled user services and timers", EvidenceState.Informational,
            $"The signed-in user has {services} enabled service(s), {timers} enabled timer(s), and {other} other enabled user unit(s).",
            "Enabled user units are startup context. Pulse does not enable, disable, start, stop, or reset them.", source);
    }
}
