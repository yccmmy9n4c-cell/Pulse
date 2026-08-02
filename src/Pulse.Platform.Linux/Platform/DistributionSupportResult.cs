namespace Pulse.Platform.Linux.Platform;

public enum DistributionSupportLevel
{
    Supported,
    UnverifiedDerivative,
    Unsupported
}

public sealed record DistributionSupportResult(
    DistributionSupportLevel Level,
    string Id,
    string VersionId,
    string DisplayName,
    string Architecture,
    string Message);
