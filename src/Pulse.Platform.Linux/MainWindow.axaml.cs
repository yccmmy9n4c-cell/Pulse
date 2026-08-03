using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Pulse.Platform.Linux.Platform;
using Pulse.Platform.Linux.Providers;
using Pulse.Platform.Linux.Services;

namespace Pulse.Platform.Linux;

public sealed partial class MainWindow : Window
{
    private readonly DistributionSupportDetector _detector = new();
    private readonly LinuxAssessmentService _assessment = new();
    private readonly AssessmentArchiveService _archive = new();
    private DistributionSupportResult _support;
    private string? _latestReportPath;

    public MainWindow()
    {
        InitializeComponent();
        _support = _detector.Detect();
        _latestReportPath = _archive.FindLatestReportPath();
        OpenReportButton.IsEnabled = _latestReportPath is not null;
        RenderSupport();
    }

    private void RenderSupport()
    {
        SupportStatus.Text = _support.Level switch
        {
            DistributionSupportLevel.Supported => "SUPPORTED",
            DistributionSupportLevel.UnverifiedDerivative => "NOT YET VERIFIED",
            _ => "UNSUPPORTED"
        };
        SupportStatus.Foreground = new SolidColorBrush(Color.Parse(
            _support.Level == DistributionSupportLevel.Supported ? "#34D399" : "#FFD13D"));
        PlatformSummary.Text = $"{_support.DisplayName} • {_support.Architecture}";
        SupportDetail.Text = _support.Message;
        AssessButton.IsEnabled = _support.Level == DistributionSupportLevel.Supported;
    }

    private async void AssessButton_OnClick(object? sender, RoutedEventArgs e)
    {
        AssessButton.IsEnabled = false;
        AssessButton.Content = "Assessing…";

        try
        {
            var results = await _assessment.RunAsync();
            FoundationResults.Text = string.Join("\n\n", results.Select(result =>
                $"[{StateLabel(result.State)}] {result.Title}\n{result.Summary}\nSource: {result.Source}"));

            var attentionCount = results.Count(result => result.State == EvidenceState.Attention);
            var unavailableCount = results.Count(result => result.State == EvidenceState.Unavailable);
            var overview = attentionCount == 0
                ? "Pulse found no attention items in the available evidence."
                : $"Pulse found {attentionCount} item(s) that deserve review.";
            if (unavailableCount > 0)
            {
                overview += $" {unavailableCount} provider(s) were unavailable and did not stop the assessment.";
            }

            var prioritizedGuidance = results
                .OrderBy(result => result.State switch
                {
                    EvidenceState.Attention => 0,
                    EvidenceState.Unavailable => 1,
                    EvidenceState.Informational => 2,
                    _ => 3
                })
                .Select(result => $"{result.Title}: {result.Guidance}")
                .Distinct();
            var guidance = $"{overview}\n\n{string.Join("\n\n", prioritizedGuidance)}";
            try
            {
                var version = typeof(MainWindow).Assembly.GetName().Version?.ToString(4) ?? "0.0.0.4";
                var artifacts = await _archive.SaveAsync(_support, results, version);
                _latestReportPath = artifacts.ReportPath;
                OpenReportButton.IsEnabled = true;
                guidance += "\n\nAssessment saved. Use Open Latest Report to view the full Pulse report.";
            }
            catch (Exception archiveError)
            {
                guidance += $"\n\nThe assessment completed, but Pulse could not save its report. Technical detail: {archiveError.Message}";
            }

            GuidanceText.Text = guidance;
        }
        catch (Exception ex)
        {
            FoundationResults.Text = "Pulse could not complete the read-only assessment.";
            GuidanceText.Text = $"No system changes were made. Technical detail: {ex.Message}";
        }
        finally
        {
            AssessButton.Content = "Run Read-Only Assessment";
            AssessButton.IsEnabled = _support.Level == DistributionSupportLevel.Supported;
        }
    }

    private void OpenReportButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_latestReportPath) || !File.Exists(_latestReportPath))
        {
            OpenReportButton.IsEnabled = false;
            GuidanceText.Text = "The latest report is no longer available. Run a new assessment to create another one.";
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = _latestReportPath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            GuidanceText.Text = $"Pulse saved the report but could not open the default browser. Report: {_latestReportPath}\nTechnical detail: {ex.Message}";
        }
    }

    private static string StateLabel(EvidenceState state) => state switch
    {
        EvidenceState.Healthy => "HEALTHY",
        EvidenceState.Attention => "REVIEW",
        EvidenceState.Informational => "INFO",
        _ => "UNAVAILABLE"
    };
}
