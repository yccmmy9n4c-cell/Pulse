using Pulse.Platform.Linux.Platform;

namespace Pulse.Platform.Linux.Providers;

public sealed class PackageHealthEvidenceProvider(IReadOnlyCommandRunner commandRunner) : ILinuxEvidenceProvider
{
    public string Id => "linux.dpkg-audit";

    public async Task<EvidenceResult> CollectAsync(CancellationToken cancellationToken = default)
    {
        var result = await commandRunner.RunAsync("dpkg", ["--audit"], cancellationToken: cancellationToken);
        if (!result.Started || result.TimedOut)
        {
            return EvidenceResult.Unavailable(Id, "Package database health", "dpkg --audit",
                result.TimedOut ? "The read-only package audit timed out." : "The dpkg command is unavailable.");
        }

        var detail = string.Join('\n', new[] { result.StandardOutput, result.StandardError }
            .Where(value => !string.IsNullOrWhiteSpace(value))).Trim();
        if (result.ExitCode == 0 && string.IsNullOrWhiteSpace(detail))
        {
            return new(Id, "Package database health", EvidenceState.Healthy,
                "dpkg reports no incomplete or inconsistent package operations.",
                "No package repair is currently indicated by the local package database.", "dpkg --audit");
        }

        return new(Id, "Package database health", EvidenceState.Attention,
            string.IsNullOrWhiteSpace(detail) ? $"dpkg audit exited with code {result.ExitCode}." : detail,
            "Review the reported package state before installing additional software. Pulse did not run a repair.",
            "dpkg --audit");
    }
}
