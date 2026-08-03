using System.Text.Json;
using Pulse.Platform.Linux.Platform;

namespace Pulse.Platform.Linux.Providers;

public sealed class EncryptionEvidenceProvider(IReadOnlyCommandRunner commandRunner) : ILinuxEvidenceProvider
{
    public string Id => "linux.luks-indicator";

    public async Task<EvidenceResult> CollectAsync(CancellationToken cancellationToken = default)
    {
        var result = await commandRunner.RunAsync("lsblk", ["--json", "--output", "NAME,TYPE,FSTYPE,MOUNTPOINTS"],
            cancellationToken: cancellationToken);
        if (!result.Started || result.TimedOut || result.ExitCode != 0)
        {
            return EvidenceResult.Unavailable(Id, "Disk encryption indicator", "lsblk --json",
                result.TimedOut ? "The block-device query timed out." : "lsblk could not provide block-device metadata.");
        }

        try
        {
            using var document = JsonDocument.Parse(result.StandardOutput);
            var luksDetected = ContainsLuks(document.RootElement);
            return new(Id, "Disk encryption indicator",
                luksDetected ? EvidenceState.Healthy : EvidenceState.Informational,
                luksDetected
                    ? "Pulse detected a LUKS encrypted block-device layer."
                    : "Pulse did not detect a LUKS layer in readable block-device metadata.",
                luksDetected
                    ? "LUKS presence is a positive indicator; confirmation that user data is covered will be added later."
                    : "This is not definitive proof that data is unencrypted. Review the installation's disk-encryption configuration if assurance is required.",
                "lsblk --json --output NAME,TYPE,FSTYPE,MOUNTPOINTS");
        }
        catch (JsonException ex)
        {
            return EvidenceResult.Unavailable(Id, "Disk encryption indicator", "lsblk --json", ex.Message);
        }
    }

    private static bool ContainsLuks(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (property.NameEquals("fstype") && property.Value.ValueKind == JsonValueKind.String &&
                    property.Value.GetString()?.Contains("crypto_LUKS", StringComparison.OrdinalIgnoreCase) == true)
                {
                    return true;
                }

                if (ContainsLuks(property.Value))
                {
                    return true;
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                if (ContainsLuks(item))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
