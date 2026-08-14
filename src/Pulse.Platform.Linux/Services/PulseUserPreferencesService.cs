using System.Text.Json;
using Pulse.Platform.Linux.Platform;
using Pulse.Platform.Linux.Providers;

namespace Pulse.Platform.Linux.Services;

public sealed record PulseUserPreferences(
    bool IgnoreInactiveFirewall = false,
    DateTimeOffset? InactiveFirewallAcknowledgedAtUtc = null);

public sealed class PulseUserPreferencesService
{
    public const string SettingsFileName = "settings.json";
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _settingsFilePath;

    public PulseUserPreferencesService(string? settingsFilePath = null)
    {
        _settingsFilePath = settingsFilePath ?? Path.Combine(LinuxUserPaths.SettingsDirectory, SettingsFileName);
    }

    public string SettingsFilePath => _settingsFilePath;

    public PulseUserPreferences Load()
    {
        try
        {
            if (!File.Exists(_settingsFilePath))
            {
                return new();
            }

            return JsonSerializer.Deserialize<PulseUserPreferences>(File.ReadAllText(_settingsFilePath)) ?? new();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            // A missing, unreadable, or malformed preference must fail safe by restoring review.
            return new();
        }
    }

    public PulseUserPreferences SetInactiveFirewallAcknowledged(bool acknowledged)
    {
        var preferences = new PulseUserPreferences(
            acknowledged,
            acknowledged ? DateTimeOffset.UtcNow : null);
        var directory = Path.GetDirectoryName(_settingsFilePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var temporaryPath = $"{_settingsFilePath}.{Guid.NewGuid():N}.tmp";
        try
        {
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(preferences, JsonOptions));
            File.Move(temporaryPath, _settingsFilePath, true);
            return preferences;
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }
}

public static class EvidencePreferencePolicy
{
    public static IReadOnlyList<EvidenceResult> Apply(
        IReadOnlyList<EvidenceResult> evidence,
        PulseUserPreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        ArgumentNullException.ThrowIfNull(preferences);
        return evidence.Select(item => preferences.IgnoreInactiveFirewall &&
                                       (IsInactiveFirewall(item) || IsAcknowledgedFirewall(item))
            ? item with
            {
                State = EvidenceState.Healthy,
                Summary = "No active UFW or nftables service was detected. You confirmed that the firewall is off intentionally, so Pulse will not request review for this posture.",
                Guidance = "No action is required for the acknowledged configuration. Use Restore Firewall Review on Network Intelligence if this decision changes.",
                Source = $"{FirewallEvidenceProvider.InactiveSource}; user-approved Pulse preference"
            }
            : !preferences.IgnoreInactiveFirewall && IsAcknowledgedFirewall(item)
                ? item with
                {
                    State = EvidenceState.Informational,
                    Summary = FirewallEvidenceProvider.InactiveSummary,
                    Guidance = FirewallEvidenceProvider.InactiveGuidance,
                    Source = FirewallEvidenceProvider.InactiveSource
                }
            : item).ToArray();
    }

    public static bool ContainsInactiveFirewall(IReadOnlyList<EvidenceResult> evidence) =>
        evidence.Any(IsInactiveFirewall);

    private static bool IsInactiveFirewall(EvidenceResult item) =>
        item.ProviderId.Equals("linux.firewall-indicator", StringComparison.Ordinal) &&
        item.State == EvidenceState.Informational &&
        item.Summary.Equals(FirewallEvidenceProvider.InactiveSummary, StringComparison.Ordinal);

    private static bool IsAcknowledgedFirewall(EvidenceResult item) =>
        item.ProviderId.Equals("linux.firewall-indicator", StringComparison.Ordinal) &&
        item.Source.Contains("user-approved Pulse preference", StringComparison.Ordinal);
}
