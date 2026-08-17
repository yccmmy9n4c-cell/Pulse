using Pulse.Platform.Linux.Providers;
using Pulse.Platform.Linux.Platform;

namespace Pulse.Platform.Linux.Services;

public sealed class LinuxAssessmentService
{
    private readonly IReadOnlyList<ILinuxEvidenceProvider> _providers;
    private readonly PulseUserPreferencesService? _preferences;

    public LinuxAssessmentService() : this(
        BuildDefaultProviders(new ReadOnlyCommandRunner()),
        new PulseUserPreferencesService())
    {
    }

    public LinuxAssessmentService(IEnumerable<ILinuxEvidenceProvider> providers) : this(providers, null)
    {
    }

    public LinuxAssessmentService(
        IEnumerable<ILinuxEvidenceProvider> providers,
        PulseUserPreferencesService? preferences)
    {
        _providers = providers.ToArray();
        _preferences = preferences;
    }

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

        if (_preferences is null)
        {
            return results;
        }

        return EvidencePreferencePolicy.Apply(results, _preferences.Load());
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
        new SecureBootEvidenceProvider(),
        new AppArmorEvidenceProvider(),
        new FirewallEvidenceProvider(commandRunner),
        new UnattendedUpgradesEvidenceProvider(),
        new EncryptionEvidenceProvider(commandRunner),
        new SystemdEvidenceProvider(),
        new NetworkPostureEvidenceProvider(commandRunner),
        new DefaultRouteEvidenceProvider(commandRunner),
        new NetworkManagerEvidenceProvider(commandRunner),
        new DnsConfigurationEvidenceProvider(),
        new ListeningServicesEvidenceProvider(commandRunner),
        new JournalReliabilityEvidenceProvider(commandRunner),
        new SystemdFailedUnitsEvidenceProvider(commandRunner),
        new SystemdFailedUnitsEvidenceProvider(commandRunner, userScope: true),
        new SystemdBootTimingEvidenceProvider(commandRunner),
        new SystemdCriticalChainEvidenceProvider(commandRunner),
        new DesktopAutostartEvidenceProvider(),
        new EnabledUserUnitsEvidenceProvider(commandRunner),
        new UptimeEvidenceProvider(),
        new LoadAverageEvidenceProvider(),
        new MemoryAvailabilityEvidenceProvider(),
        new PressureStallEvidenceProvider(PressureResource.Cpu),
        new PressureStallEvidenceProvider(PressureResource.Memory),
        new PressureStallEvidenceProvider(PressureResource.Io),
        new ThermalPostureEvidenceProvider(),
        new ProcessorIdentityEvidenceProvider(),
        new PhysicalMemoryEvidenceProvider(),
        new FirmwareIdentityEvidenceProvider(),
        new BatteryConditionEvidenceProvider(),
        new GraphicsHardwareEvidenceProvider(),
        new VirtualizationPostureEvidenceProvider(commandRunner),
        new DriveHealthEvidenceProvider(commandRunner),
        new BackupPostureEvidenceProvider(),
        new BackupScheduleEvidenceProvider(commandRunner),
        new BackupActivityEvidenceProvider(commandRunner),
        new BackupDestinationMountEvidenceProvider(commandRunner),
        new SystemSnapshotEvidenceProvider(),
        new BackupRestoreReadinessEvidenceProvider(),
        new RestartRequirementEvidenceProvider()
    ];
}
