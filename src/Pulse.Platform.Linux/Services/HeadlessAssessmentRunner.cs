using Pulse.Platform.Linux.Platform;
using Pulse.Platform.Linux.Providers;

namespace Pulse.Platform.Linux.Services;

public sealed class HeadlessAssessmentRunner
{
    private readonly DistributionSupportDetector _detector;
    private readonly LinuxAssessmentService _assessment;
    private readonly AssessmentArchiveService _archive;

    public HeadlessAssessmentRunner(
        DistributionSupportDetector? detector = null,
        LinuxAssessmentService? assessment = null,
        AssessmentArchiveService? archive = null)
    {
        _detector = detector ?? new DistributionSupportDetector();
        _assessment = assessment ?? new LinuxAssessmentService();
        _archive = archive ?? new AssessmentArchiveService();
    }

    public async Task<int> RunAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var support = _detector.Detect();
            if (support.Level != DistributionSupportLevel.Supported)
            {
                Console.Error.WriteLine($"Pulse assessment refused: {support.Message}");
                return 2;
            }

            var evidence = await _assessment.RunAsync(cancellationToken);
            var version = typeof(HeadlessAssessmentRunner).Assembly.GetName().Version?.ToString(4) ?? "0.0.0.7";
            var artifacts = await _archive.SaveAsync(support, evidence, version,
                cancellationToken: cancellationToken);
            Console.WriteLine($"Pulse assessment saved: {artifacts.ReportPath}");
            Console.WriteLine($"Evidence: {evidence.Count}; review: {evidence.Count(item => item.State == EvidenceState.Attention)}; unavailable: {evidence.Count(item => item.State == EvidenceState.Unavailable)}");
            return 0;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            Console.Error.WriteLine("Pulse assessment was cancelled.");
            return 130;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Pulse assessment failed without changing the system: {ex.Message}");
            return 1;
        }
    }
}
