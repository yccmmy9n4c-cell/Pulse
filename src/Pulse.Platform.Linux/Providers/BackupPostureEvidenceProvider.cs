namespace Pulse.Platform.Linux.Providers;

public sealed class BackupPostureEvidenceProvider : ILinuxEvidenceProvider
{
    private static readonly BackupCandidate[] Candidates =
    [
        new("Déjà Dup", ["deja-dup"], [".config/deja-dup", ".var/app/org.gnome.DejaDup"]),
        new("Pika Backup", ["pika-backup"], [".config/pika-backup", ".var/app/org.gnome.World.PikaBackup"]),
        new("Back In Time", ["backintime", "backintime-qt"], [".config/backintime"]),
        new("BorgBackup", ["borg"], [".config/borg"]),
        new("Restic", ["restic"], [".config/restic"]),
        new("Duplicity", ["duplicity"], [".cache/duplicity"]),
        new("Timeshift", ["timeshift"], [])
    ];

    private readonly string _homeDirectory;
    private readonly IReadOnlyList<string> _executableDirectories;
    private readonly string _timeshiftConfiguration;

    public BackupPostureEvidenceProvider(
        string? homeDirectory = null,
        IReadOnlyList<string>? executableDirectories = null,
        string? timeshiftConfiguration = null)
    {
        _homeDirectory = homeDirectory ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        _executableDirectories = executableDirectories ?? ["/usr/bin", "/usr/sbin", "/bin", "/sbin"];
        _timeshiftConfiguration = timeshiftConfiguration ?? "/etc/timeshift/timeshift.json";
    }

    public string Id => "linux.backup-posture";

    public Task<EvidenceResult> CollectAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var configured = new List<string>();
        var installed = new List<string>();

        foreach (var candidate in Candidates)
        {
            var hasExecutable = candidate.Executables.Any(executable =>
                _executableDirectories.Any(directory => File.Exists(Path.Combine(directory, executable))));
            var hasConfiguration = candidate.ConfigurationPaths.Any(relativePath =>
                Directory.Exists(Path.Combine(_homeDirectory, relativePath)) ||
                File.Exists(Path.Combine(_homeDirectory, relativePath)));

            if (candidate.Name == "Timeshift" && File.Exists(_timeshiftConfiguration))
            {
                hasConfiguration = true;
            }

            if (hasConfiguration)
            {
                configured.Add(candidate.Name);
            }
            else if (hasExecutable)
            {
                installed.Add(candidate.Name);
            }
        }

        if (configured.Count > 0)
        {
            return Task.FromResult(new EvidenceResult(Id, "Backup posture", EvidenceState.Informational,
                $"Pulse detected backup configuration evidence for {string.Join(", ", configured)}.",
                "Configuration presence does not prove that a recent backup succeeded or can be restored. Verify the tool's last successful run and periodically test recovery. Pulse did not open repositories or run a backup.",
                "Known user configuration paths and /etc/timeshift/timeshift.json when readable"));
        }

        if (installed.Count > 0)
        {
            return Task.FromResult(new EvidenceResult(Id, "Backup posture", EvidenceState.Informational,
                $"Pulse detected installed backup tooling ({string.Join(", ", installed)}) but no known configuration evidence for this user.",
                "The tool may be configured elsewhere or may not yet be in use. Review its settings and confirm a recent recoverable backup. Pulse made no changes.",
                "Known executable and user configuration paths"));
        }

        return Task.FromResult(new EvidenceResult(Id, "Backup posture", EvidenceState.Informational,
            "Pulse did not detect the supported backup tools or configuration paths in its current read-only coverage.",
            "This is not proof that the computer has no backup. Review your chosen backup method and confirm that recovery has been tested.",
            "Known Déjà Dup, Pika Backup, Back In Time, Borg, Restic, Duplicity, and Timeshift paths"));
    }

    private sealed record BackupCandidate(
        string Name,
        IReadOnlyList<string> Executables,
        IReadOnlyList<string> ConfigurationPaths);
}
