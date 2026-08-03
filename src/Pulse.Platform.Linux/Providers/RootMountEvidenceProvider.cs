using System.Text.Json;
using Pulse.Platform.Linux.Platform;

namespace Pulse.Platform.Linux.Providers;

public sealed class RootMountEvidenceProvider(IReadOnlyCommandRunner commandRunner) : ILinuxEvidenceProvider
{
    public string Id => "linux.root-mount";

    public async Task<EvidenceResult> CollectAsync(CancellationToken cancellationToken = default)
    {
        var result = await commandRunner.RunAsync("findmnt",
            ["--json", "--target", "/", "--output", "SOURCE,FSTYPE,OPTIONS"],
            TimeSpan.FromSeconds(10), cancellationToken);
        if (!result.Started || result.TimedOut || result.ExitCode != 0)
        {
            return EvidenceResult.Unavailable(Id, "Root filesystem mount", "findmnt --json --target /",
                result.TimedOut ? "The root mount query timed out." : "findmnt could not provide root filesystem metadata.");
        }

        try
        {
            using var document = JsonDocument.Parse(result.StandardOutput);
            if (!document.RootElement.TryGetProperty("filesystems", out var filesystems) ||
                filesystems.ValueKind != JsonValueKind.Array || filesystems.GetArrayLength() == 0)
            {
                return EvidenceResult.Unavailable(Id, "Root filesystem mount", "findmnt --json --target /",
                    "The root filesystem was not present in readable findmnt output.");
            }

            var root = filesystems[0];
            var source = Text(root, "source", "unknown source");
            var fileSystem = Text(root, "fstype", "unknown filesystem");
            var options = Text(root, "options", string.Empty);
            var readOnly = options.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Contains("ro", StringComparer.Ordinal);
            return new(Id, "Root filesystem mount", readOnly ? EvidenceState.Attention : EvidenceState.Healthy,
                readOnly
                    ? $"The root filesystem is {fileSystem} from {source} and is currently mounted read-only."
                    : $"The root filesystem is {fileSystem} from {source} and is mounted read-write.",
                readOnly
                    ? "A read-only root mount can indicate recovery mode or filesystem trouble. Preserve important data and review system logs before attempting repair. Pulse made no mount changes."
                    : "The root mount mode appears normal. Pulse read mount metadata only.",
                "findmnt --json --target / --output SOURCE,FSTYPE,OPTIONS");
        }
        catch (JsonException ex)
        {
            return EvidenceResult.Unavailable(Id, "Root filesystem mount", "findmnt --json --target /", ex.Message);
        }
    }

    private static string Text(JsonElement element, string property, string fallback) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String &&
        !string.IsNullOrWhiteSpace(value.GetString())
            ? value.GetString()!.Trim()
            : fallback;
}
