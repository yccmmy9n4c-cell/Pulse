namespace Pulse.Platform.Linux.Providers;

public enum EvidenceState
{
    Healthy,
    Attention,
    Informational,
    Unavailable
}

public sealed record EvidenceResult(
    string ProviderId,
    string Title,
    EvidenceState State,
    string Summary,
    string Guidance,
    string Source)
{
    public static EvidenceResult Unavailable(string providerId, string title, string source, string detail) =>
        new(providerId, title, EvidenceState.Unavailable,
            "Pulse could not read this evidence on the current system.",
            $"No system changes were made. {detail}", source);
}

public interface ILinuxEvidenceProvider
{
    string Id { get; }
    Task<EvidenceResult> CollectAsync(CancellationToken cancellationToken = default);
}
