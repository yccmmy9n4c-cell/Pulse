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
        PackagePage.IsVisible = page == "Package";
        StoragePage.IsVisible = page == "Storage";
        ReportsPage.IsVisible = page == "Reports";
        SchedulerPage.IsVisible = page == "Scheduler";
        LogsPage.IsVisible = page == "Logs";
        MissionControlPage.IsVisible = page == "MissionControl";
        PageTitle.Text = page switch
        {
            "Assessment" => "Linux Assessment",
            "Package" => "Package Intelligence",
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
        yield return (PackageNavButton, "Package");
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
        PackageAssessButton.IsEnabled = supported;
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
            RenderDashboardDomains([]);
            RenderSystemTrend([]);
            RenderPackageIntelligence([]);
            RenderStorageIntelligence([]);
            return;
        }

        var latest = snapshots[0];
        var health = PulseHealthInterpreter.Interpret(latest.Evidence);
        ApplyDashboardHealth(health);
        DashboardLastAssessmentText.Text = $"Last assessment: {latest.AssessedAtUtc.ToLocalTime():g}";
        DashboardEvidenceCountText.Text = $"{latest.Evidence.Count} evidence sources";
        RenderDashboardDomains(latest.Evidence);
        RenderSystemTrend(snapshots);

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
        RenderPackageIntelligence(latest.Evidence);
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
        DashboardExecutiveScoreText.Foreground = brush;
        DashboardExecutiveScoreText.Text = health.State == "Assessment Ready" ? "--" : health.Score.ToString();
        Canvas.SetLeft(DashboardGaugePointer, health.State == "Assessment Ready"
            ? 0
            : Math.Clamp((health.Score / 100d * 210d) - 4d, 0d, 210d));

        if (health.AttentionCount > 0)
        {
            RequiredActionBorder.Background = new SolidColorBrush(Color.Parse("#341923"));
            RequiredActionBorder.BorderBrush = new SolidColorBrush(Color.Parse("#FF5B6E"));
            RequiredActionText.Foreground = new SolidColorBrush(Color.Parse("#FF9CAC"));
            RequiredActionText.Text = "Required Action";
        }
        else if (health.UnavailableCount > 0)
        {
            RequiredActionBorder.Background = new SolidColorBrush(Color.Parse("#3A3014"));
            RequiredActionBorder.BorderBrush = new SolidColorBrush(Color.Parse("#FFD13D"));
            RequiredActionText.Foreground = new SolidColorBrush(Color.Parse("#FFD13D"));
            RequiredActionText.Text = "Coverage Review";
        }
        else if (health.State == "Assessment Ready")
        {
            RequiredActionBorder.Background = new SolidColorBrush(Color.Parse("#12283A"));
            RequiredActionBorder.BorderBrush = new SolidColorBrush(Color.Parse("#36516B"));
            RequiredActionText.Foreground = new SolidColorBrush(Color.Parse("#DDE7F0"));
            RequiredActionText.Text = "Assessment Required";
        }
        else
        {
            RequiredActionBorder.Background = new SolidColorBrush(Color.Parse("#12392F"));
            RequiredActionBorder.BorderBrush = new SolidColorBrush(Color.Parse("#5CFF88"));
            RequiredActionText.Foreground = new SolidColorBrush(Color.Parse("#5CFF88"));
            RequiredActionText.Text = "No Required Action";
        }
    }

    private void RenderDashboardDomains(IReadOnlyList<EvidenceResult> results)
    {
        ApplyDomain(results, ["linux.os-release", "linux.proc-foundation", "linux.systemd"],
            PlatformDomainDot, PlatformDomainStateText, PlatformDomainScoreText, PlatformDomainFill);
        ApplyDomain(results, ["linux.dpkg-audit", "linux.dpkg-inventory", "linux.apt-cached-updates", "linux.apt-security-updates", "linux.unattended-upgrades", "linux.reboot-required"],
            PackageDomainDot, PackageDomainStateText, PackageDomainScoreText, PackageDomainFill);
        ApplyDomain(results, ["linux.network-posture", "linux.firewall-indicator"],
            NetworkDomainDot, NetworkDomainStateText, NetworkDomainScoreText, NetworkDomainFill);
        ApplyDomain(results, ["linux.storage-root", "linux.root-mount", "linux.inode-capacity", "linux.drive-health", "linux.luks-indicator", "linux.backup-posture"],
            StorageDomainDot, StorageDomainStateText, StorageDomainScoreText, StorageDomainFill);
        ApplyDomain(results, ["linux.apparmor", "linux.firewall-indicator", "linux.luks-indicator", "linux.unattended-upgrades"],
            SecurityDomainDot, SecurityDomainStateText, SecurityDomainScoreText, SecurityDomainFill);
        ApplyDomain(results, ["linux.journal-reliability", "linux.systemd", "linux.dpkg-audit"],
            ReliabilityDomainDot, ReliabilityDomainStateText, ReliabilityDomainScoreText, ReliabilityDomainFill);
    }

    private static void ApplyDomain(
        IReadOnlyList<EvidenceResult> results,
        IReadOnlyList<string> providerIds,
        Border dot,
        TextBlock stateText,
        TextBlock scoreText,
        Border fill)
    {
        var domainEvidence = results
            .Where(item => providerIds.Contains(item.ProviderId, StringComparer.Ordinal))
            .ToArray();
        if (domainEvidence.Length == 0)
        {
            var pending = BrushForHealth("Assessment Ready");
            dot.Background = pending;
            stateText.Foreground = pending;
            stateText.Text = "Pending";
            scoreText.Text = "--";
            fill.Width = 0;
            return;
        }

        var health = PulseHealthInterpreter.Interpret(domainEvidence);
        var brush = BrushForHealth(health.State);
        dot.Background = brush;
        stateText.Foreground = brush;
        stateText.Text = health.State;
        scoreText.Text = health.Score.ToString();
        fill.Background = brush;
        fill.Width = health.Score / 100d * 135d;
    }

    private void RenderSystemTrend(IReadOnlyList<AssessmentSnapshot> snapshots)
    {
        SystemTrendPlotCanvas.Children.Clear();
        var points = snapshots.Take(10).Reverse()
            .Select(snapshot => new
            {
                snapshot.AssessedAtUtc,
                Score = PulseHealthInterpreter.Interpret(snapshot.Evidence).Score
            })
            .ToArray();
        if (points.Length == 0)
        {
            return;
        }

        const double width = 820;
        var step = points.Length == 1 ? 0 : width / (points.Length - 1);
        var plotted = points.Select((point, index) => new Point(
            index * step + 6,
            8 + ((100 - point.Score) * 0.72))).ToArray();

        for (var index = 1; index < plotted.Length; index++)
        {
            SystemTrendPlotCanvas.Children.Add(new Avalonia.Controls.Shapes.Line
            {
                StartPoint = plotted[index - 1],
                EndPoint = plotted[index],
                Stroke = new SolidColorBrush(Color.Parse("#00A6FF")),
                StrokeThickness = 2
            });
        }

        foreach (var point in plotted)
        {
            var dot = new Border
            {
                Width = 8,
                Height = 8,
                CornerRadius = new CornerRadius(4),
                Background = new SolidColorBrush(Color.Parse("#5CFF88"))
            };
            Canvas.SetLeft(dot, point.X - 4);
            Canvas.SetTop(dot, point.Y - 4);
            SystemTrendPlotCanvas.Children.Add(dot);
        }

        var firstLabel = new TextBlock
        {
            Text = points[0].AssessedAtUtc.ToLocalTime().ToString("MMM d"),
            Foreground = new SolidColorBrush(Color.Parse("#718497")),
            FontSize = 9
        };
        Canvas.SetLeft(firstLabel, 0);
        Canvas.SetTop(firstLabel, 86);
        SystemTrendPlotCanvas.Children.Add(firstLabel);

        if (points.Length > 1)
        {
            var lastLabel = new TextBlock
            {
                Text = points[^1].AssessedAtUtc.ToLocalTime().ToString("MMM d"),
                Foreground = new SolidColorBrush(Color.Parse("#718497")),
                FontSize = 9
            };
            Canvas.SetLeft(lastLabel, width - 35);
            Canvas.SetTop(lastLabel, 86);
            SystemTrendPlotCanvas.Children.Add(lastLabel);
        }
    }

    private async void RunAssessmentButton_OnClick(object? sender, RoutedEventArgs e)
    {
        DashboardAssessButton.IsEnabled = false;
        AssessmentRunButton.IsEnabled = false;
        PackageAssessButton.IsEnabled = false;
        StorageAssessButton.IsEnabled = false;
        DashboardAssessButton.Content = "Assessing…";
        AssessmentRunButton.Content = "Assessing…";
        PackageAssessButton.Content = "Assessing…";
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
            PackageAssessButton.Content = "Run Assessment";
            StorageAssessButton.Content = "Run Assessment";
            var supported = _support.Level == DistributionSupportLevel.Supported;
            DashboardAssessButton.IsEnabled = supported;
            AssessmentRunButton.IsEnabled = supported;
            PackageAssessButton.IsEnabled = supported;
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
            FindEvidence(results, "linux.root-mount"),
            FindEvidence(results, "linux.inode-capacity"),
            FindEvidence(results, "linux.drive-health"),
            FindEvidence(results, "linux.luks-indicator"),
            FindEvidence(results, "linux.backup-posture")
        };

        ApplyStorageCard(storageItems[0], StorageCapacityStateText, StorageCapacityDetailText);
        ApplyStorageCard(storageItems[1], StorageMountStateText, StorageMountDetailText);
        ApplyStorageCard(storageItems[2], StorageInodeStateText, StorageInodeDetailText);
        ApplyStorageCard(storageItems[3], StorageDriveStateText, StorageDriveDetailText);
        ApplyStorageCard(storageItems[4], StorageEncryptionStateText, StorageEncryptionDetailText);
        ApplyStorageCard(storageItems[5], StorageBackupStateText, StorageBackupDetailText);

        var available = storageItems.Where(item => item is not null).Select(item => item!).ToArray();
        if (available.Length == 0)
        {
            StorageExecutiveStateText.Text = "Pending Assessment";
            StorageExecutiveStateText.Foreground = BrushForHealth("Attention Recommended");
            StorageExecutiveDetailText.Text = "Run an assessment to evaluate capacity, root mount integrity, inode pressure, physical-drive indicators, encryption, and detectable backup posture.";
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

    private void RenderPackageIntelligence(IReadOnlyList<EvidenceResult> results)
    {
        var packageItems = new[]
        {
            FindEvidence(results, "linux.dpkg-audit"),
            FindEvidence(results, "linux.dpkg-inventory"),
            FindEvidence(results, "linux.apt-cached-updates"),
            FindEvidence(results, "linux.apt-security-updates"),
            FindEvidence(results, "linux.unattended-upgrades"),
            FindEvidence(results, "linux.reboot-required")
        };

        ApplyStorageCard(packageItems[0], PackageDatabaseStateText, PackageDatabaseDetailText);
        ApplyStorageCard(packageItems[1], PackageInventoryStateText, PackageInventoryDetailText);
        ApplyStorageCard(packageItems[2], PackageUpdatesStateText, PackageUpdatesDetailText);
        ApplyStorageCard(packageItems[3], PackageSecurityStateText, PackageSecurityDetailText);
        ApplyStorageCard(packageItems[4], PackageAutomaticStateText, PackageAutomaticDetailText);
        ApplyStorageCard(packageItems[5], PackageRestartStateText, PackageRestartDetailText);

        var available = packageItems.Where(item => item is not null).Select(item => item!).ToArray();
        if (available.Length == 0)
        {
            PackageExecutiveStateText.Text = "Pending Assessment";
            PackageExecutiveStateText.Foreground = BrushForHealth("Attention Recommended");
            PackageExecutiveDetailText.Text = "Run an assessment to evaluate the local dpkg/APT package state without refreshing repositories or installing updates.";
            PackageRecommendationText.Text = "Run an assessment to establish Package Intelligence.";
            return;
        }

        var health = PulseHealthInterpreter.Interpret(available);
        PackageExecutiveStateText.Text = health.State;
        PackageExecutiveStateText.Foreground = BrushForHealth(health.State);
        PackageExecutiveDetailText.Text = health.Detail;
        PackageRecommendationText.Text = available
            .OrderBy(item => EvidencePriority(item.State))
            .Select(item => item.Guidance)
            .FirstOrDefault() ?? "No package recommendation is available.";
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
        PackageOpenReportButton.IsEnabled = enabled;
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
