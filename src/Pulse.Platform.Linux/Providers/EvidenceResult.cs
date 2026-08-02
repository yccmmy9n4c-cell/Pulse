namespace Pulse.Platform.Linux.Providers;

public sealed record EvidenceResult(string ProviderId, string Title, string Summary, string Guidance);

public interface ILinuxEvidenceProvider
{
    string Id { get; }
    Task<EvidenceResult> CollectAsync(CancellationToken cancellationToken = default);
}
