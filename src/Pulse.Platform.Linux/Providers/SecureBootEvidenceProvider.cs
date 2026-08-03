namespace Pulse.Platform.Linux.Providers;

public sealed class SecureBootEvidenceProvider(
    string efiPath = "/sys/firmware/efi",
    string efivarsPath = "/sys/firmware/efi/efivars") : ILinuxEvidenceProvider
{
    public string Id => "linux.secure-boot";

    public async Task<EvidenceResult> CollectAsync(CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(efiPath))
        {
            return new(Id, "Secure Boot posture", EvidenceState.Informational,
                "UEFI firmware state is not visible, so Pulse cannot determine Secure Boot posture.",
                "The system may use legacy boot, a virtualized firmware path, or restricted sysfs access. This is incomplete coverage, not proof that Secure Boot is disabled.",
                efiPath);
        }

        string? variablePath;
        try
        {
            variablePath = Directory.Exists(efivarsPath)
                ? Directory.EnumerateFiles(efivarsPath, "SecureBoot-*", SearchOption.TopDirectoryOnly).FirstOrDefault()
                : null;
        }
        catch (UnauthorizedAccessException ex)
        {
            return EvidenceResult.Unavailable(Id, "Secure Boot posture", efivarsPath,
                $"The Secure Boot firmware variable is not readable: {ex.Message}");
        }
        catch (IOException ex)
        {
            return EvidenceResult.Unavailable(Id, "Secure Boot posture", efivarsPath,
                $"Pulse could not enumerate Secure Boot firmware variables: {ex.Message}");
        }

        if (variablePath is null)
        {
            return new(Id, "Secure Boot posture", EvidenceState.Informational,
                "UEFI is present, but the Secure Boot firmware variable was not visible.",
                "This does not prove Secure Boot is disabled. Confirm firmware security settings if Secure Boot assurance is required.",
                efivarsPath);
        }

        try
        {
            var data = await File.ReadAllBytesAsync(variablePath, cancellationToken);
            if (data.Length < 5)
            {
                return EvidenceResult.Unavailable(Id, "Secure Boot posture", variablePath,
                    "The Secure Boot firmware variable did not contain a readable state byte.");
            }

            var enabled = data[4] == 1;
            return new(Id, "Secure Boot posture", enabled ? EvidenceState.Healthy : EvidenceState.Attention,
                enabled
                    ? "UEFI firmware reports Secure Boot enabled."
                    : "UEFI firmware reports Secure Boot disabled.",
                enabled
                    ? "Secure Boot helps verify the early boot chain. Pulse read the firmware state only and made no changes."
                    : "Review whether Secure Boot is appropriate for this computer before changing firmware settings. Pulse made no firmware or boot-policy changes.",
                variablePath);
        }
        catch (UnauthorizedAccessException ex)
        {
            return EvidenceResult.Unavailable(Id, "Secure Boot posture", variablePath,
                $"The Secure Boot firmware variable is not readable: {ex.Message}");
        }
        catch (IOException ex)
        {
            return EvidenceResult.Unavailable(Id, "Secure Boot posture", variablePath,
                $"Pulse could not read Secure Boot firmware state: {ex.Message}");
        }
    }
}
