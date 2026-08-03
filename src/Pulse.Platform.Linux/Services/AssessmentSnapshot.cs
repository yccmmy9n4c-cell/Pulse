using Pulse.Platform.Linux.Platform;
using Pulse.Platform.Linux.Providers;

namespace Pulse.Platform.Linux.Services;

public sealed record AssessmentSnapshot(
    DateTimeOffset AssessedAtUtc,
    string PulseVersion,
    DistributionSupportResult Platform,
    IReadOnlyList<EvidenceResult> Evidence);

public sealed record AssessmentArtifacts(
    string SnapshotPath,
    string ReportPath,
    string ActivityLogPath);
