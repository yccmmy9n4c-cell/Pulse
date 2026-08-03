using Pulse.Platform.Linux.Providers;
using Pulse.Platform.Linux.Platform;

namespace Pulse.Platform.Linux.Services;

public sealed class LinuxAssessmentService
{
    private readonly IReadOnlyList<ILinuxEvidenceProvider> _providers;

    public LinuxAssessmentService() : this(BuildDefaultProviders(new ReadOnlyCommandRunner()))
    {
    }

    public LinuxAssessmentService(IEnumerable<ILinuxEvidenceProvider> providers) => _providers = providers.ToArray();

    public async Task<IReadOnlyList<EvidenceResult>> RunAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<EvidenceResult>(_providers.Count);
        foreach (var provider in _providers)
        {
            try
            {
                results.Add(await provider.CollectAsync(cancellationToken));
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                results.Add(EvidenceResult.Unavailable(provider.Id, "Evidence provider unavailable", provider.Id, ex.Message));
            }
        }

        return results;
    }

    private static ILinuxEvidenceProvider[] BuildDefaultProviders(IReadOnlyCommandRunner commandRunner) =>
    [
        new OsReleaseEvidenceProvider(),
        new ProcEvidenceProvider(),
        new StorageEvidenceProvider(),
        new RootMountEvidenceProvider(commandRunner),
        new InodeCapacityEvidenceProvider(commandRunner),
        new PackageHealthEvidenceProvider(commandRunner),
        new InstalledPackageInventoryEvidenceProvider(commandRunner),
        new CachedUpdateEvidenceProvider(commandRunner),
        new SecurityUpdateEvidenceProvider(commandRunner),
        new AppArmorEvidenceProvider(),
        new FirewallEvidenceProvider(commandRunner),
        new UnattendedUpgradesEvidenceProvider(),
        new EncryptionEvidenceProvider(commandRunner),
        new SystemdEvidenceProvider(),
        new NetworkPostureEvidenceProvider(commandRunner),
        new JournalReliabilityEvidenceProvider(commandRunner),
        new DriveHealthEvidenceProvider(commandRunner),
        new BackupPostureEvidenceProvider(),
        new RestartRequirementEvidenceProvider()
    ];
}
