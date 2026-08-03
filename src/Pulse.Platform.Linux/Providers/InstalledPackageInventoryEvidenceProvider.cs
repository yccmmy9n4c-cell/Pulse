using Pulse.Platform.Linux.Platform;

namespace Pulse.Platform.Linux.Providers;

public sealed class InstalledPackageInventoryEvidenceProvider(IReadOnlyCommandRunner commandRunner) : ILinuxEvidenceProvider
{
    public string Id => "linux.dpkg-inventory";

    public async Task<EvidenceResult> CollectAsync(CancellationToken cancellationToken = default)
    {
        var result = await commandRunner.RunAsync("dpkg-query",
            ["-W", "-f=${binary:Package}\t${Status}\n"], TimeSpan.FromSeconds(12), cancellationToken);
        if (!result.Started || result.TimedOut || result.ExitCode != 0)
        {
            return EvidenceResult.Unavailable(Id, "Installed package inventory", "dpkg-query -W",
                result.TimedOut ? "The package inventory query timed out." : "dpkg-query could not provide the installed package inventory.");
        }

        var installed = result.StandardOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Count(line => line.EndsWith("\tinstall ok installed", StringComparison.Ordinal));
        if (installed == 0)
        {
            return new(Id, "Installed package inventory", EvidenceState.Unavailable,
                "dpkg-query returned no recognized installed-package records.",
                "Package inventory coverage is incomplete. Pulse did not change the package database.",
                "dpkg-query -W (local database)");
        }

        return new(Id, "Installed package inventory", EvidenceState.Healthy,
            $"The local dpkg database contains {installed:N0} installed package(s).",
            "This inventory count is informational and comes only from the local package database.",
            "dpkg-query -W (local database)");
    }
}
