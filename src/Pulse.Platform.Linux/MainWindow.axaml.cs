using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Pulse.Platform.Linux.Platform;
using Pulse.Platform.Linux.Services;

namespace Pulse.Platform.Linux;

public sealed partial class MainWindow : Window
{
    private readonly DistributionSupportDetector _detector = new();
    private readonly LinuxAssessmentService _assessment = new();
    private DistributionSupportResult _support;

    public MainWindow()
    {
        InitializeComponent();
        _support = _detector.Detect();
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
                $"{result.Title}\n{result.Summary}"));
            GuidanceText.Text = string.Join("\n\n", results.Select(result => result.Guidance).Distinct());
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
}
