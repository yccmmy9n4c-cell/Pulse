using Pulse.Platform.Linux.Providers;

namespace Pulse.Platform.Linux.Services;

public sealed class LinuxAssessmentService
{
    private readonly IReadOnlyList<ILinuxEvidenceProvider> _providers;

    public LinuxAssessmentService() : this(new ILinuxEvidenceProvider[]
    {
        new OsReleaseEvidenceProvider(),
        new ProcEvidenceProvider(),
        new SystemdEvidenceProvider()
    })
    {
    }

    internal LinuxAssessmentService(IReadOnlyList<ILinuxEvidenceProvider> providers) => _providers = providers;

    public async Task<IReadOnlyList<EvidenceResult>> RunAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<EvidenceResult>(_providers.Count);
        foreach (var provider in _providers)
        {
            results.Add(await provider.CollectAsync(cancellationToken));
        }

        return results;
    }
}
