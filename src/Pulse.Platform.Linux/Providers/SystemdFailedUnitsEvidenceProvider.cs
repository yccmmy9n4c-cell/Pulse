using Pulse.Platform.Linux.Platform;

namespace Pulse.Platform.Linux.Providers;

public sealed class SystemdFailedUnitsEvidenceProvider(
    IReadOnlyCommandRunner commandRunner,
    bool userScope = false) : ILinuxEvidenceProvider
{
    public string Id => userScope ? "linux.systemd-user-failed" : "linux.systemd-system-failed";

    public async Task<EvidenceResult> CollectAsync(CancellationToken cancellationToken = default)
    {
        var arguments = new List<string>();
        if (userScope)
        {
            arguments.Add("--user");
        }

        arguments.AddRange(["--failed", "--type=service", "--no-legend", "--plain"]);
        var result = await commandRunner.RunAsync("systemctl", arguments, TimeSpan.FromSeconds(10), cancellationToken);
        var title = userScope ? "Failed user services" : "Failed system services";
        var source = $"systemctl {(userScope ? "--user " : string.Empty)}--failed --type=service --no-legend --plain";
        if (!result.Started || result.TimedOut || result.ExitCode != 0)
        {
            return EvidenceResult.Unavailable(Id, title, source,
                result.TimedOut
                    ? "The service-state query timed out."
                    : "The current user could not read this systemd service scope.");
        }

        var units = result.StandardOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(line => line.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault(token => token.EndsWith(".service", StringComparison.Ordinal)))
            .Where(unit => !string.IsNullOrWhiteSpace(unit))
            .Distinct(StringComparer.Ordinal)
            .Cast<string>()
            .ToArray();
        if (units.Length == 0)
        {
            return new(Id, title, EvidenceState.Healthy,
                userScope
                    ? "No failed user services are reported by systemd."
                    : "No failed system services are reported by systemd.",
                "No service was started, stopped, enabled, disabled, or reset by Pulse.", source);
        }

        var visibleUnits = string.Join(", ", units.Take(4));
        var remainder = units.Length > 4 ? $" and {units.Length - 4} more" : string.Empty;
        return new(Id, title, EvidenceState.Attention,
            $"systemd reports {units.Length} failed {(userScope ? "user" : "system")} service(s): {visibleUnits}{remainder}.",
            "Open the system logs and review the named services before restarting or changing them. A failed optional service may not affect normal use.",
            source);
    }
}
