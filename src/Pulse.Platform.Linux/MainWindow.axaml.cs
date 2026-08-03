using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
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
    private readonly SystemdUserScheduleService _schedule = new();
    private DistributionSupportResult _support;
    private string? _latestReportPath;
    private UserScheduleState _scheduleState = UserScheduleState.Unavailable;
    private bool _scheduleConfirmationPending;
    private bool _clearLogConfirmationPending;

    public MainWindow()
    {
        InitializeComponent();
        _support = _detector.Detect();
        _latestReportPath = _archive.FindLatestReportPath();
        Title = AppInfo.ProductName;
        BuildIdText.Text = $"Build ID: {AppInfo.BuildId}";
        VersionNameText.Text = AppInfo.VersionLine;
        MissionVersionText.Text = AppInfo.Version;
        MissionBuildIdText.Text = AppInfo.BuildId;
        MissionComputerText.Text = Environment.MachineName;
        MissionReportsFolderText.Text = _archive.ReportsDirectoryPath;
        MissionSettingsFolderText.Text = LinuxUserPaths.SettingsDirectory;
        RenderSupportBoundary();
        RefreshDashboardFromHistory();
        RefreshReportsPage();
        RefreshLogsPage();
        ShowPage("Dashboard");
        _ = RefreshScheduleStatusAsync();
    }

    private void NavigateButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string page })
        {
            ShowPage(page);
        }
    }

    private void ShowPage(string page)
    {
        DashboardPage.IsVisible = page == "Dashboard";
        AssessmentPage.IsVisible = page == "Assessment";
        StoragePage.IsVisible = page == "Storage";
        ReportsPage.IsVisible = page == "Reports";
        SchedulerPage.IsVisible = page == "Scheduler";
        LogsPage.IsVisible = page == "Logs";
        MissionControlPage.IsVisible = page == "MissionControl";
        PageTitle.Text = page switch
        {
            "Assessment" => "Linux Assessment",
            "Storage" => "Storage Intelligence",
            "Scheduler" => "Scheduler",
            "Logs" => "Logs",
            "MissionControl" => "Mission Control",
            _ => page
        };

        foreach (var item in NavigationButtons())
        {
            if (item.Page == page)
            {
                if (!item.Button.Classes.Contains("selected"))
                {
                    item.Button.Classes.Add("selected");
                }
            }
            else
            {
                item.Button.Classes.Remove("selected");
            }
        }

        if (page == "Reports")
        {
            RefreshReportsPage();
        }
        else if (page == "Logs")
        {
            RefreshLogsPage();
        }
        else if (page == "Scheduler")
        {
            _ = RefreshScheduleStatusAsync();
        }
    }

    private IEnumerable<(Button Button, string Page)> NavigationButtons()
    {
        yield return (DashboardNavButton, "Dashboard");
        yield return (AssessmentNavButton, "Assessment");
        yield return (StorageNavButton, "Storage");
        yield return (ReportsNavButton, "Reports");
        yield return (SchedulerNavButton, "Scheduler");
        yield return (LogsNavButton, "Logs");
        yield return (MissionControlNavButton, "MissionControl");
    }

    private void RenderSupportBoundary()
    {
        var supported = _support.Level == DistributionSupportLevel.Supported;
        var status = _support.Level switch
        {
            DistributionSupportLevel.Supported => "SUPPORTED",
            DistributionSupportLevel.UnverifiedDerivative => "NOT YET VERIFIED",
            _ => "UNSUPPORTED"
        };
        var statusColor = BrushForSupport(_support.Level);

        AssessmentSupportStatus.Text = status;
        AssessmentSupportStatus.Foreground = statusColor;
        AssessmentPlatformSummary.Text = $"{_support.DisplayName} • {_support.Architecture}";
        AssessmentSupportDetail.Text = _support.Message;
        DashboardPlatformText.Text = $"{_support.DisplayName} • {_support.Architecture} • {status}";
        MissionDistributionText.Text = $"{_support.DisplayName} • {_support.Architecture}";
        AssessmentRunButton.IsEnabled = supported;
        DashboardAssessButton.IsEnabled = supported;
        StorageAssessButton.IsEnabled = supported;
    }

    private void RefreshDashboardFromHistory()
    {
        var snapshots = _archive.LoadRecentSnapshots(20);
        _latestReportPath = _archive.FindLatestReportPath();
        SetReportButtonsEnabled(_latestReportPath is not null);

        if (snapshots.Count == 0)
        {
            ApplyDashboardHealth(PulseHealthInterpreter.Interpret([]));
            DashboardLastAssessmentText.Text = "No saved assessment yet.";
            TopRiskText.Text = "Assessment data has not been collected.";
            RecentChangesText.Text = "No assessment history yet.";
            RecommendationsText.Text = "Run an assessment for plain-language guidance.";
            SystemTrendText.Text = "Trend data will appear after additional assessments.";
            DashboardEvidenceCountText.Text = "0 evidence sources";
            RenderStorageIntelligence([]);
            return;
        }

        var latest = snapshots[0];
        var health = PulseHealthInterpreter.Interpret(latest.Evidence);
        ApplyDashboardHealth(health);
        DashboardLastAssessmentText.Text = $"Last assessment: {latest.AssessedAtUtc.ToLocalTime():g}";
        DashboardEvidenceCountText.Text = $"{latest.Evidence.Count} evidence sources";

        var topRisk = latest.Evidence.FirstOrDefault(item => item.State == EvidenceState.Attention)
            ?? latest.Evidence.FirstOrDefault(item => item.State == EvidenceState.Unavailable);
        TopRiskText.Text = topRisk is null
            ? "No significant risks detected in the available evidence."
            : $"{topRisk.Title}\n{topRisk.Summary}";

        RecentChangesText.Text = snapshots.Count == 1
            ? "The first Linux assessment baseline has been recorded."
            : DescribeRecentChanges(latest, snapshots[1]);
        RecommendationsText.Text = topRisk?.Guidance ?? "No immediate action is required.";

        if (snapshots.Count < 2)
        {
            SystemTrendText.Text = "Trend data will appear after an additional assessment.";
        }
        else
        {
            var previous = PulseHealthInterpreter.Interpret(snapshots[1].Evidence);
            var delta = health.Score - previous.Score;
            var direction = delta switch
            {
                > 0 => "improved",
                < 0 => "declined",
                _ => "remained stable"
            };
            SystemTrendText.Text = $"Executive health {direction} since the previous assessment ({delta:+#;-#;0}).";
        }

        RenderAssessmentEvidence(latest.Evidence);
        RenderStorageIntelligence(latest.Evidence);
        AssessmentGuidanceText.Text = BuildGuidance(latest.Evidence);
    }

    private void ApplyDashboardHealth(PulseHealthSummary health)
    {
        var brush = BrushForHealth(health.State);
        DashboardHealthDot.Background = brush;
        DashboardStateText.Foreground = brush;
        DashboardStateText.Text = health.State;
        DashboardSummaryText.Text = health.Detail;
    }

    private async void RunAssessmentButton_OnClick(object? sender, RoutedEventArgs e)
    {
        DashboardAssessButton.IsEnabled = false;
        AssessmentRunButton.IsEnabled = false;
        StorageAssessButton.IsEnabled = false;
        DashboardAssessButton.Content = "Assessing…";
        AssessmentRunButton.Content = "Assessing…";
        StorageAssessButton.Content = "Assessing…";
        SetActivity("Read-only Linux assessment started.");

        try
        {
            var results = await _assessment.RunAsync();
            RenderAssessmentEvidence(results);
            AssessmentGuidanceText.Text = BuildGuidance(results);

            try
            {
                var artifacts = await _archive.SaveAsync(_support, results, AppInfo.Version);
                _latestReportPath = artifacts.ReportPath;
                SetActivity("Assessment completed and the Aurora HTML report was saved.");
            }
            catch (Exception archiveError)
            {
                SetActivity($"Assessment completed, but its report could not be saved: {archiveError.Message}");
            }

            RefreshDashboardFromHistory();
            RefreshReportsPage();
        }
        catch (Exception ex)
        {
            AssessmentResultsPanel.Children.Clear();
            AssessmentResultsPanel.Children.Add(new TextBlock
            {
                Text = "Pulse could not complete the read-only assessment.",
                Foreground = BrushForHealth("Critical")
            });
            AssessmentGuidanceText.Text = $"No system changes were made. Technical detail: {ex.Message}";
            SetActivity("Assessment failed without changing the system.");
        }
        finally
        {
            DashboardAssessButton.Content = "Run Assessment";
            AssessmentRunButton.Content = "Run Read-Only Assessment";
            StorageAssessButton.Content = "Run Assessment";
            var supported = _support.Level == DistributionSupportLevel.Supported;
            DashboardAssessButton.IsEnabled = supported;
            AssessmentRunButton.IsEnabled = supported;
            StorageAssessButton.IsEnabled = supported;
        }
    }

    private void RenderAssessmentEvidence(IReadOnlyList<EvidenceResult> results)
    {
        AssessmentResultsPanel.Children.Clear();
        foreach (var result in results.OrderBy(item => EvidencePriority(item.State)))
        {
            var heading = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*") };
            heading.Children.Add(new TextBlock
            {
                Text = StateLabel(result.State),
                Foreground = BrushForEvidence(result.State),
                FontSize = 10,
                FontWeight = FontWeight.Bold,
                Margin = new Thickness(0, 1, 12, 0)
            });
            var title = new TextBlock
            {
                Text = result.Title,
                Foreground = Brushes.White,
                FontSize = 14,
                FontWeight = FontWeight.SemiBold
            };
            Grid.SetColumn(title, 1);
            heading.Children.Add(title);

            var content = new StackPanel { Spacing = 7 };
            content.Children.Add(heading);
            content.Children.Add(new TextBlock
            {
                Text = result.Summary,
                Foreground = new SolidColorBrush(Color.Parse("#DDE7F0")),
                TextWrapping = TextWrapping.Wrap
            });
            content.Children.Add(new TextBlock
            {
                Text = $"Evidence source: {result.Source}",
                Foreground = new SolidColorBrush(Color.Parse("#718497")),
                FontSize = 10,
                TextWrapping = TextWrapping.Wrap
            });

            var card = new Border { Child = content };
            card.Classes.Add("evidenceCard");
            AssessmentResultsPanel.Children.Add(card);
        }
    }

    private static string BuildGuidance(IReadOnlyList<EvidenceResult> results)
    {
        var health = PulseHealthInterpreter.Interpret(results);
        var guidance = results
            .OrderBy(item => EvidencePriority(item.State))
            .Select(item => $"{item.Title}: {item.Guidance}")
            .Distinct()
            .Take(6);
        return $"{health.Detail}\n\n{string.Join("\n\n", guidance)}";
    }

    private void RenderStorageIntelligence(IReadOnlyList<EvidenceResult> results)
    {
        var storageItems = new[]
        {
            FindEvidence(results, "linux.storage-root"),
            FindEvidence(results, "linux.drive-health"),
            FindEvidence(results, "linux.luks-indicator"),
            FindEvidence(results, "linux.backup-posture")
        };

        ApplyStorageCard(storageItems[0], StorageCapacityStateText, StorageCapacityDetailText);
        ApplyStorageCard(storageItems[1], StorageDriveStateText, StorageDriveDetailText);
        ApplyStorageCard(storageItems[2], StorageEncryptionStateText, StorageEncryptionDetailText);
        ApplyStorageCard(storageItems[3], StorageBackupStateText, StorageBackupDetailText);

        var available = storageItems.Where(item => item is not null).Select(item => item!).ToArray();
        if (available.Length == 0)
        {
            StorageExecutiveStateText.Text = "Pending Assessment";
            StorageExecutiveStateText.Foreground = BrushForHealth("Attention Recommended");
            StorageExecutiveDetailText.Text = "Run an assessment to evaluate capacity, physical-drive indicators, encryption, and detectable backup posture.";
            StorageRecommendationText.Text = "Run an assessment to establish storage evidence.";
            return;
        }

        var health = PulseHealthInterpreter.Interpret(available);
        StorageExecutiveStateText.Text = health.State;
        StorageExecutiveStateText.Foreground = BrushForHealth(health.State);
        StorageExecutiveDetailText.Text = health.Detail;
        StorageRecommendationText.Text = available
            .OrderBy(item => EvidencePriority(item.State))
            .Select(item => item.Guidance)
            .FirstOrDefault() ?? "No storage recommendation is available.";
    }

    private static EvidenceResult? FindEvidence(IReadOnlyList<EvidenceResult> results, string providerId) =>
        results.FirstOrDefault(item => item.ProviderId.Equals(providerId, StringComparison.Ordinal));

    private static void ApplyStorageCard(EvidenceResult? evidence, TextBlock stateText, TextBlock detailText)
    {
        if (evidence is null)
        {
            stateText.Text = "Pending Assessment";
            stateText.Foreground = BrushForHealth("Attention Recommended");
            detailText.Text = "Evidence has not been collected.";
            return;
        }

        stateText.Text = StateLabel(evidence.State);
        stateText.Foreground = BrushForEvidence(evidence.State);
        detailText.Text = evidence.Summary;
    }

    private static string DescribeRecentChanges(AssessmentSnapshot latest, AssessmentSnapshot previous)
    {
        var previousStates = previous.Evidence.ToDictionary(
            item => item.ProviderId, item => item.State, StringComparer.Ordinal);
        var changes = latest.Evidence
            .Where(item => previousStates.TryGetValue(item.ProviderId, out var oldState) && oldState != item.State)
            .Select(item => $"{item.Title}: {previousStates[item.ProviderId]} → {item.State}")
            .Take(3)
            .ToArray();

        return changes.Length == 0
            ? "No evidence-state changes were detected since the previous assessment."
            : string.Join("\n", changes);
    }

    private void RefreshReportsPage()
    {
        var reports = _archive.FindRecentReportPaths(8);
        _latestReportPath = reports.FirstOrDefault();
        SetReportButtonsEnabled(_latestReportPath is not null);
        LatestReportText.Text = _latestReportPath is null
            ? "No report has been generated yet."
            : $"Latest: {Path.GetFileName(_latestReportPath)}";

        RecentReportsPanel.Children.Clear();
        if (reports.Count == 0)
        {
            RecentReportsPanel.Children.Add(new TextBlock
            {
                Text = "No saved reports.",
                Foreground = new SolidColorBrush(Color.Parse("#DDE7F0"))
            });
            return;
        }

        foreach (var reportPath in reports)
        {
            var capturedPath = reportPath;
            var button = new Button
            {
                Content = Path.GetFileNameWithoutExtension(reportPath),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Left
            };
            button.Classes.Add("secondary");
            button.Click += (_, _) => OpenPath(capturedPath, "HTML report");
            RecentReportsPanel.Children.Add(button);
        }
    }

    private void SetReportButtonsEnabled(bool enabled)
    {
        DashboardOpenReportButton.IsEnabled = enabled;
        ReportsOpenLatestButton.IsEnabled = enabled;
        StorageOpenReportButton.IsEnabled = enabled;
    }

    private void OpenLatestReportButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_latestReportPath) || !File.Exists(_latestReportPath))
        {
            SetReportButtonsEnabled(false);
            SetActivity("The latest report is unavailable. Run a new assessment to create one.");
            return;
        }

        OpenPath(_latestReportPath, "latest HTML report");
    }

    private void OpenReportsFolderButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Directory.CreateDirectory(_archive.ReportsDirectoryPath);
        OpenPath(_archive.ReportsDirectoryPath, "HTML reports folder");
    }

    private void OpenLogsFolderButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Directory.CreateDirectory(_archive.LogsDirectoryPath);
        OpenPath(_archive.LogsDirectoryPath, "logs folder");
    }

    private void OpenPath(string path, string description)
    {
        try
        {
            Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
            SetActivity($"Opened {description}.");
        }
        catch (Exception ex)
        {
            SetActivity($"Pulse could not open the {description}: {ex.Message}");
        }
    }

    private async void ScheduleButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_scheduleState == UserScheduleState.Enabled)
        {
            ScheduleButton.IsEnabled = false;
            ScheduleButton.Content = "Disabling…";
            var disabled = await _schedule.DisableAsync();
            SetActivity(disabled.Message);
            _scheduleConfirmationPending = false;
            await RefreshScheduleStatusAsync();
            return;
        }

        if (!_scheduleConfirmationPending)
        {
            _scheduleConfirmationPending = true;
            ScheduleButton.Content = "Confirm Weekly Schedule";
            SchedulerDetailText.Text = "Pulse will run one read-only assessment each week through systemd --user. Reports remain in your user-data folder. Click Confirm Weekly Schedule to approve.";
            SetActivity("Weekly scheduling is awaiting explicit confirmation.");
            return;
        }

        ScheduleButton.IsEnabled = false;
        ScheduleButton.Content = "Enabling…";
        var executablePath = Environment.ProcessPath;
        var enabled = executablePath is null
            ? new UserScheduleOperationResult(false, "Pulse could not identify its executable, so no schedule was created.")
            : await _schedule.EnableAsync(executablePath);
        SetActivity(enabled.Message);
        _scheduleConfirmationPending = false;
        await RefreshScheduleStatusAsync();
    }

    private async Task RefreshScheduleStatusAsync()
    {
        if (_support.Level != DistributionSupportLevel.Supported)
        {
            ApplyScheduleStatus(new UserScheduleStatus(UserScheduleState.Unavailable,
                "Scheduling is unavailable on unsupported systems."));
            return;
        }

        try
        {
            ApplyScheduleStatus(await _schedule.GetStatusAsync());
        }
        catch (Exception ex)
        {
            ApplyScheduleStatus(new UserScheduleStatus(UserScheduleState.Unavailable,
                $"Pulse could not read the weekly schedule status. {ex.Message}"));
        }
    }

    private void ApplyScheduleStatus(UserScheduleStatus status)
    {
        _scheduleState = status.State;
        SchedulerStateText.Text = status.State switch
        {
            UserScheduleState.Enabled => "Running",
            UserScheduleState.Disabled => "Stopped",
            _ => "Unavailable"
        };
        SchedulerStateText.Foreground = status.State == UserScheduleState.Enabled
            ? BrushForHealth("Healthy")
            : status.State == UserScheduleState.Disabled
                ? BrushForHealth("Attention Recommended")
                : BrushForHealth("Degraded");
        SchedulerDetailText.Text = status.Message;
        ScheduleButton.Content = status.State switch
        {
            UserScheduleState.Enabled => "Stop Scheduler",
            UserScheduleState.Disabled => "Start Scheduler",
            _ => "Scheduler Unavailable"
        };
        ScheduleButton.IsEnabled = status.State != UserScheduleState.Unavailable;
    }

    private void RefreshLogsButton_OnClick(object? sender, RoutedEventArgs e)
    {
        RefreshLogsPage();
        SetActivity("Pulse event log refreshed.");
    }

    private void RefreshLogsPage()
    {
        LogsLocationText.Text = _archive.ActivityLogPath;
        var lines = _archive.ReadRecentActivityLines(60);
        LogsText.Text = lines.Count == 0
            ? "No Pulse activity has been recorded."
            : string.Join("\n\n", lines);
    }

    private async void ClearLogButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (!_clearLogConfirmationPending)
        {
            _clearLogConfirmationPending = true;
            ClearLogButton.Content = "Confirm Clear Event Log";
            SetActivity("Clearing the Pulse event log is awaiting confirmation.");
            return;
        }

        try
        {
            await _archive.ClearActivityLogAsync();
            SetActivity("Pulse event log cleared.");
            RefreshLogsPage();
        }
        catch (Exception ex)
        {
            SetActivity($"Pulse could not clear the event log: {ex.Message}");
        }
        finally
        {
            _clearLogConfirmationPending = false;
            ClearLogButton.Content = "Clear Event Log";
        }
    }

    private void RefreshPulseButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _support = _detector.Detect();
        RenderSupportBoundary();
        RefreshDashboardFromHistory();
        RefreshReportsPage();
        RefreshLogsPage();
        _ = RefreshScheduleStatusAsync();
        SetActivity("Pulse refreshed.");
    }

    private void ExitButton_OnClick(object? sender, RoutedEventArgs e) => Close();

    private void SetActivity(string message) =>
        ActivityText.Text = $"[{DateTime.Now:HH:mm:ss}] {message}";

    private static int EvidencePriority(EvidenceState state) => state switch
    {
        EvidenceState.Attention => 0,
        EvidenceState.Unavailable => 1,
        EvidenceState.Informational => 2,
        _ => 3
    };

    private static string StateLabel(EvidenceState state) => state switch
    {
        EvidenceState.Healthy => "HEALTHY",
        EvidenceState.Attention => "REVIEW",
        EvidenceState.Informational => "INFORMATION",
        _ => "UNAVAILABLE"
    };

    private static IBrush BrushForSupport(DistributionSupportLevel level) => level switch
    {
        DistributionSupportLevel.Supported => new SolidColorBrush(Color.Parse("#5CFF88")),
        DistributionSupportLevel.UnverifiedDerivative => new SolidColorBrush(Color.Parse("#FFD13D")),
        _ => new SolidColorBrush(Color.Parse("#FF5B6E"))
    };

    private static IBrush BrushForEvidence(EvidenceState state) => state switch
    {
        EvidenceState.Healthy => new SolidColorBrush(Color.Parse("#5CFF88")),
        EvidenceState.Attention => new SolidColorBrush(Color.Parse("#FFD13D")),
        EvidenceState.Informational => new SolidColorBrush(Color.Parse("#00A6FF")),
        _ => new SolidColorBrush(Color.Parse("#FF9F1C"))
    };

    private static IBrush BrushForHealth(string state) => state switch
    {
        "Optimized" or "Healthy" => new SolidColorBrush(Color.Parse("#5CFF88")),
        "Attention Recommended" => new SolidColorBrush(Color.Parse("#FFD13D")),
        "Degraded" => new SolidColorBrush(Color.Parse("#FF9F1C")),
        "Critical" => new SolidColorBrush(Color.Parse("#FF5B6E")),
        _ => new SolidColorBrush(Color.Parse("#00A6FF"))
    };
}
