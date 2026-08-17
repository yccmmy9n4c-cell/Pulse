using System.Diagnostics;
using System.Runtime.InteropServices;
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
    private readonly GitHubUpdateService _updates = new();
    private readonly PulseUserPreferencesService _preferencesStore = new();
    private PulseUserPreferences _userPreferences = new();
    private DistributionSupportResult _support;
    private string? _latestReportPath;
    private UserScheduleState _scheduleState = UserScheduleState.Unavailable;
    private bool _scheduleConfirmationPending;
    private bool _clearLogConfirmationPending;
    private EvidenceResult? _packageReviewEvidence;
    private EvidenceResult? _networkReviewEvidence;
    private EvidenceResult? _storageReviewEvidence;
    private EvidenceResult? _backupReviewEvidence;
    private EvidenceResult? _securityReviewEvidence;
    private EvidenceResult? _performanceReviewEvidence;
    private EvidenceResult? _hardwareReviewEvidence;
    private EvidenceResult? _startupReviewEvidence;
    private EvidenceResult? _reliabilityReviewEvidence;
    private EvidenceResult? _compatibilityReviewEvidence;
    private PulseUpdateResult? _availableUpdate;
    private string? _downloadedUpdatePath;
    private bool _inactiveFirewallDetected;

    public MainWindow()
    {
        InitializeComponent();
        _userPreferences = _preferencesStore.Load();
        _support = _detector.Detect();
        _latestReportPath = _archive.FindLatestReportPath();
        Title = AppInfo.ProductName;
        BuildIdText.Text = $"Build ID: {AppInfo.BuildId}";
        VersionNameText.Text = AppInfo.VersionLine;
        MissionVersionText.Text = AppInfo.DisplayVersion;
        MissionBuildIdText.Text = AppInfo.BuildId;
        MissionComputerText.Text = Environment.MachineName;
        MissionReportsFolderText.Text = _archive.ReportsDirectoryPath;
        MissionSettingsFolderText.Text = LinuxUserPaths.SettingsDirectory;
        UpdateInstalledVersionText.Text = AppInfo.DisplayVersion;
        UpdateArchitectureText.Text = RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant();
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
        NetworkPage.IsVisible = page == "Network";
        StoragePage.IsVisible = page == "Storage";
        BackupPage.IsVisible = page == "Backup";
        SecurityPage.IsVisible = page == "Security";
        PerformancePage.IsVisible = page == "Performance";
        HardwarePage.IsVisible = page == "Hardware";
        StartupPage.IsVisible = page == "Startup";
        ReliabilityPage.IsVisible = page == "Reliability";
        ReportsPage.IsVisible = page == "Reports";
        CompatibilityPage.IsVisible = page == "Compatibility";
        SchedulerPage.IsVisible = page == "Scheduler";
        LogsPage.IsVisible = page == "Logs";
        UpdatesPage.IsVisible = page == "Updates";
        MissionControlPage.IsVisible = page == "MissionControl";
        PageTitle.Text = page switch
        {
            "Assessment" => "Linux Assessment",
            "Package" => "Package Intelligence",
            "Network" => "Network Intelligence",
            "Storage" => "Storage Intelligence",
            "Backup" => "Backup Intelligence",
            "Security" => "Security Intelligence",
            "Performance" => "Performance Intelligence",
            "Hardware" => "Hardware Intelligence",
            "Startup" => "Startup Intelligence",
            "Reliability" => "Reliability Intelligence",
            "Compatibility" => "Compatibility",
            "Scheduler" => "Scheduler",
            "Logs" => "Logs",
            "Updates" => "Updates",
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
        else if (page == "Assessment")
        {
            ShowAssessmentSection("Overview");
        }
    }

    private void AssessmentSectionButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string section })
        {
            ShowAssessmentSection(section);
        }
    }

    private void ShowAssessmentSection(string section)
    {
        AssessmentOverviewPanel.IsVisible = section == "Overview";
        AssessmentInformationPanel.IsVisible = section == "Information";
        AssessmentHealthyPanel.IsVisible = section == "Healthy";
        AssessmentGuidancePanel.IsVisible = section == "Guidance";
    }

    private IEnumerable<(Button Button, string Page)> NavigationButtons()
    {
        yield return (DashboardNavButton, "Dashboard");
        yield return (AssessmentNavButton, "Assessment");
        yield return (PackageNavButton, "Package");
        yield return (NetworkNavButton, "Network");
        yield return (StorageNavButton, "Storage");
        yield return (BackupNavButton, "Backup");
        yield return (SecurityNavButton, "Security");
        yield return (PerformanceNavButton, "Performance");
        yield return (HardwareNavButton, "Hardware");
        yield return (StartupNavButton, "Startup");
        yield return (ReliabilityNavButton, "Reliability");
        yield return (ReportsNavButton, "Reports");
        yield return (CompatibilityNavButton, "Compatibility");
        yield return (SchedulerNavButton, "Scheduler");
        yield return (LogsNavButton, "Logs");
        yield return (UpdatesNavButton, "Updates");
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
        NetworkAssessButton.IsEnabled = supported;
        StorageAssessButton.IsEnabled = supported;
        BackupAssessButton.IsEnabled = supported;
        SecurityAssessButton.IsEnabled = supported;
        PerformanceAssessButton.IsEnabled = supported;
        HardwareAssessButton.IsEnabled = supported;
        StartupAssessButton.IsEnabled = supported;
        ReliabilityAssessButton.IsEnabled = supported;
        CompatibilityAssessButton.IsEnabled = supported;
        CheckForUpdatesButton.IsEnabled = supported;
        if (!supported)
        {
            UpdateStateText.Text = "Platform Not Supported";
            UpdateStateText.Foreground = statusColor;
            UpdateDetailText.Text = $"Pulse updates are restricted to verified Debian, Ubuntu, and Linux Mint desktops. {_support.Message}";
            UpdateLatestVersionText.Text = "Not checked";
            UpdatePackageText.Text = "Pulse will not select or download a package on this system.";
        }
    }

    private void RefreshDashboardFromHistory()
    {
        _userPreferences = _preferencesStore.Load();
        IReadOnlyList<AssessmentSnapshot> snapshots = _archive.LoadRecentSnapshots(20)
            .Select(snapshot => snapshot with
            {
                Evidence = EvidencePreferencePolicy.Apply(snapshot.Evidence, _userPreferences)
            })
            .ToArray();
        _latestReportPath = _archive.FindLatestReportPath();
        SetReportButtonsEnabled(_latestReportPath is not null);

        if (snapshots.Count == 0)
        {
            _inactiveFirewallDetected = false;
            UpdateFirewallIntentControl();
            ApplyDashboardHealth(PulseHealthInterpreter.Interpret([]));
            DashboardLastAssessmentText.Text = "No saved assessment yet.";
            TopRiskText.Text = "Assessment data has not been collected.";
            RecentChangesText.Text = "No assessment history yet.";
            RecommendationsText.Text = "Run an assessment for plain-language guidance.";
            SystemTrendText.Text = "Trend data will appear after additional assessments.";
            DashboardEvidenceCountText.Text = "0 evidence sources";
            RenderDashboardDomains([]);
            RenderSystemTrend([]);
            RenderAssessmentEvidence([]);
            RenderPackageIntelligence([]);
            RenderNetworkIntelligence([]);
            RenderStorageIntelligence([]);
            RenderBackupIntelligence([]);
            RenderSecurityIntelligence([]);
            RenderPerformanceIntelligence([]);
            RenderHardwareIntelligence([]);
            RenderStartupIntelligence([]);
            RenderReliabilityIntelligence([]);
            RenderCompatibility([]);
            return;
        }

        var latest = snapshots[0];
        _inactiveFirewallDetected = EvidencePreferencePolicy.ContainsInactiveFirewall(latest.Evidence);
        UpdateFirewallIntentControl();
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
        RenderNetworkIntelligence(latest.Evidence);
        RenderStorageIntelligence(latest.Evidence);
        RenderBackupIntelligence(latest.Evidence);
        RenderSecurityIntelligence(latest.Evidence);
        RenderPerformanceIntelligence(latest.Evidence);
        RenderHardwareIntelligence(latest.Evidence);
        RenderStartupIntelligence(latest.Evidence);
        RenderReliabilityIntelligence(latest.Evidence);
        RenderCompatibility(latest.Evidence);
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
        ApplyDomain(results, ["linux.network-posture", "linux.default-route", "linux.network-manager", "linux.dns-configuration", "linux.listening-services", "linux.firewall-indicator"],
            NetworkDomainDot, NetworkDomainStateText, NetworkDomainScoreText, NetworkDomainFill);
        ApplyDomain(results, ["linux.storage-root", "linux.root-mount", "linux.inode-capacity", "linux.drive-health", "linux.luks-indicator", "linux.backup-posture"],
            StorageDomainDot, StorageDomainStateText, StorageDomainScoreText, StorageDomainFill);
        ApplyDomain(results, ["linux.backup-posture", "linux.backup-schedule", "linux.backup-activity", "linux.backup-destination-mounts", "linux.backup-system-snapshots", "linux.backup-restore-readiness"],
            BackupDomainDot, BackupDomainStateText, BackupDomainScoreText, BackupDomainFill);
        ApplyDomain(results, ["linux.apparmor", "linux.firewall-indicator", "linux.apt-security-updates", "linux.unattended-upgrades", "linux.luks-indicator", "linux.secure-boot"],
            SecurityDomainDot, SecurityDomainStateText, SecurityDomainScoreText, SecurityDomainFill);
        ApplyDomain(results, ["linux.performance-load", "linux.performance-memory", "linux.performance-cpu-pressure", "linux.performance-memory-pressure", "linux.performance-io-pressure", "linux.performance-thermal"],
            PerformanceDomainDot, PerformanceDomainStateText, PerformanceDomainScoreText, PerformanceDomainFill);
        ApplyDomain(results, ["linux.hardware-processor", "linux.hardware-memory", "linux.hardware-firmware", "linux.hardware-battery", "linux.hardware-graphics", "linux.hardware-virtualization"],
            HardwareDomainDot, HardwareDomainStateText, HardwareDomainScoreText, HardwareDomainFill);
        ApplyDomain(results, ["linux.systemd-boot-timing", "linux.startup-critical-chain", "linux.systemd-system-failed", "linux.systemd-user-failed", "linux.startup-desktop-autostart", "linux.startup-enabled-user-units"],
            StartupDomainDot, StartupDomainStateText, StartupDomainScoreText, StartupDomainFill);
        ApplyDomain(results, ["linux.journal-reliability", "linux.systemd-system-failed", "linux.systemd-user-failed", "linux.systemd-boot-timing", "linux.uptime", "linux.reboot-required"],
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
        NetworkAssessButton.IsEnabled = false;
        StorageAssessButton.IsEnabled = false;
        BackupAssessButton.IsEnabled = false;
        SecurityAssessButton.IsEnabled = false;
        PerformanceAssessButton.IsEnabled = false;
        HardwareAssessButton.IsEnabled = false;
        StartupAssessButton.IsEnabled = false;
        ReliabilityAssessButton.IsEnabled = false;
        CompatibilityAssessButton.IsEnabled = false;
        DashboardAssessButton.Content = "Assessing…";
        AssessmentRunButton.Content = "Assessing…";
        PackageAssessButton.Content = "Assessing…";
        NetworkAssessButton.Content = "Assessing…";
        StorageAssessButton.Content = "Assessing…";
        BackupAssessButton.Content = "Assessing…";
        SecurityAssessButton.Content = "Assessing…";
        PerformanceAssessButton.Content = "Assessing…";
        HardwareAssessButton.Content = "Assessing…";
        StartupAssessButton.Content = "Assessing…";
        ReliabilityAssessButton.Content = "Assessing…";
        CompatibilityAssessButton.Content = "Assessing…";
        SetActivity("Read-only Linux assessment started.");

        try
        {
            var results = await _assessment.RunAsync();
            RenderAssessmentEvidence(results);

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
            AssessmentGuidanceResultsPanel.Children.Clear();
            AssessmentGuidanceResultsPanel.Children.Add(AssessmentEmptyMessage(
                "Pulse could not complete the read-only assessment.", BrushForHealth("Critical")));
            AssessmentGuidanceText.Text = $"No system changes were made. Technical detail: {ex.Message}";
            ShowAssessmentSection("Guidance");
            SetActivity("Assessment failed without changing the system.");
        }
        finally
        {
            DashboardAssessButton.Content = "Run Assessment";
            AssessmentRunButton.Content = "Run Read-Only Assessment";
            PackageAssessButton.Content = "Run Assessment";
            NetworkAssessButton.Content = "Run Assessment";
            StorageAssessButton.Content = "Run Assessment";
            BackupAssessButton.Content = "Run Assessment";
            SecurityAssessButton.Content = "Run Assessment";
            PerformanceAssessButton.Content = "Run Assessment";
            HardwareAssessButton.Content = "Run Assessment";
            StartupAssessButton.Content = "Run Assessment";
            ReliabilityAssessButton.Content = "Run Assessment";
            CompatibilityAssessButton.Content = "Run Assessment";
            var supported = _support.Level == DistributionSupportLevel.Supported;
            DashboardAssessButton.IsEnabled = supported;
            AssessmentRunButton.IsEnabled = supported;
            PackageAssessButton.IsEnabled = supported;
            NetworkAssessButton.IsEnabled = supported;
            StorageAssessButton.IsEnabled = supported;
            BackupAssessButton.IsEnabled = supported;
            SecurityAssessButton.IsEnabled = supported;
            PerformanceAssessButton.IsEnabled = supported;
            HardwareAssessButton.IsEnabled = supported;
            StartupAssessButton.IsEnabled = supported;
            ReliabilityAssessButton.IsEnabled = supported;
            CompatibilityAssessButton.IsEnabled = supported;
        }
    }

    private void RenderAssessmentEvidence(IReadOnlyList<EvidenceResult> results)
    {
        var sections = AssessmentEvidenceOrganizer.Organize(results);

        AssessmentInformationCountText.Text = sections.Information.Count.ToString();
        AssessmentHealthyCountText.Text = sections.Healthy.Count.ToString();
        AssessmentGuidanceCountText.Text = sections.Guidance.Count.ToString();
        AssessmentOverviewStatusText.Text = results.Count == 0
            ? "Run an assessment to populate these views."
            : $"{results.Count} evidence sources are organized into clear, focused views.";

        PopulateAssessmentPanel(AssessmentInformationResultsPanel, sections.Information,
            "No informational evidence is available in the latest assessment.", includeGuidance: false);
        PopulateAssessmentPanel(AssessmentHealthyResultsPanel, sections.Healthy,
            "No evidence is currently classified as healthy.", includeGuidance: false);
        PopulateAssessmentPanel(AssessmentGuidanceResultsPanel, sections.Guidance,
            results.Count == 0
                ? "Run an assessment to receive plain-language guidance."
                : "No review items or unavailable coverage were found.", includeGuidance: true);
        AssessmentGuidanceText.Text = sections.Guidance.Count == 0
            ? results.Count == 0
                ? "Review items and unavailable coverage will appear here with plain-language next steps."
                : "Pulse found no review items or unavailable coverage in the latest assessment."
            : BuildGuidance(sections.Guidance);
    }

    private static void PopulateAssessmentPanel(
        StackPanel panel,
        IReadOnlyList<EvidenceResult> results,
        string emptyMessage,
        bool includeGuidance)
    {
        panel.Children.Clear();
        if (results.Count == 0)
        {
            panel.Children.Add(AssessmentEmptyMessage(emptyMessage, new SolidColorBrush(Color.Parse("#9FB0C0"))));
            return;
        }

        foreach (var result in results)
        {
            panel.Children.Add(BuildAssessmentEvidenceCard(result, includeGuidance));
        }
    }

    private static Border BuildAssessmentEvidenceCard(EvidenceResult result, bool includeGuidance)
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
        if (includeGuidance)
        {
            content.Children.Add(new TextBlock
            {
                Text = $"Next step: {result.Guidance}",
                Foreground = new SolidColorBrush(Color.Parse("#FFD13D")),
                TextWrapping = TextWrapping.Wrap
            });
        }
        content.Children.Add(new TextBlock
        {
            Text = $"Evidence source: {result.Source}",
            Foreground = new SolidColorBrush(Color.Parse("#718497")),
            FontSize = 10,
            TextWrapping = TextWrapping.Wrap
        });

        var card = new Border { Child = content };
        card.Classes.Add("evidenceCard");
        return card;
    }

    private static TextBlock AssessmentEmptyMessage(string message, IBrush foreground) => new()
    {
        Text = message,
        Foreground = foreground,
        TextWrapping = TextWrapping.Wrap,
        Margin = new Thickness(4, 8)
    };

    private static string BuildGuidance(IReadOnlyList<EvidenceResult> results)
    {
        var guidance = results
            .OrderBy(item => EvidencePriority(item.State))
            .Select(item => $"{item.Title}: {item.Guidance}")
            .Distinct()
            .Take(6);
        return $"Pulse found {results.Count} item(s) that deserve review or explain unavailable coverage.\n\n{string.Join("\n\n", guidance)}";
    }

    private void RenderNetworkIntelligence(IReadOnlyList<EvidenceResult> results)
    {
        var networkItems = new[]
        {
            FindEvidence(results, "linux.network-posture"),
            FindEvidence(results, "linux.default-route"),
            FindEvidence(results, "linux.network-manager"),
            FindEvidence(results, "linux.dns-configuration"),
            FindEvidence(results, "linux.listening-services"),
            FindEvidence(results, "linux.firewall-indicator")
        };

        ApplyIntelligenceCard(networkItems[0], NetworkInterfaceStateText, NetworkInterfaceDetailText);
        ApplyIntelligenceCard(networkItems[1], NetworkRouteStateText, NetworkRouteDetailText);
        ApplyIntelligenceCard(networkItems[2], NetworkManagerStateText, NetworkManagerDetailText);
        ApplyIntelligenceCard(networkItems[3], NetworkDnsStateText, NetworkDnsDetailText);
        ApplyIntelligenceCard(networkItems[4], NetworkListeningStateText, NetworkListeningDetailText);
        ApplyIntelligenceCard(networkItems[5], NetworkFirewallStateText, NetworkFirewallDetailText);

        var available = networkItems.Where(item => item is not null).Select(item => item!).ToArray();
        if (available.Length == 0)
        {
            _networkReviewEvidence = null;
            NetworkReviewActionButton.IsEnabled = false;
            NetworkReviewActionButton.Content = "Open Network Settings";
            NetworkExecutiveStateText.Text = "Pending Assessment";
            NetworkExecutiveStateText.Foreground = BrushForHealth("Attention Recommended");
            NetworkExecutiveDetailText.Text = "Run an assessment to review local interface, route, network-manager, DNS, listening-service, and firewall posture without contacting the internet.";
            NetworkRecommendationText.Text = "Run an assessment to establish Network Intelligence.";
            return;
        }

        var health = PulseHealthInterpreter.Interpret(available);
        NetworkExecutiveStateText.Text = health.State;
        NetworkExecutiveStateText.Foreground = BrushForHealth(health.State);
        NetworkExecutiveDetailText.Text = health.Detail;
        _networkReviewEvidence = SelectReviewEvidence(available);
        NetworkRecommendationText.Text = _networkReviewEvidence?.Guidance ?? "No network recommendation is available.";
        ConfigureReviewAction(NetworkReviewActionButton, _networkReviewEvidence);
    }

    private void UpdateFirewallIntentControl()
    {
        if (_userPreferences.IgnoreInactiveFirewall)
        {
            FirewallIntentButton.Content = "Restore Firewall Review";
            FirewallIntentButton.IsEnabled = true;
            var recorded = _userPreferences.InactiveFirewallAcknowledgedAtUtc?.ToLocalTime().ToString("g") ?? "an earlier session";
            FirewallIntentStatusText.Text = $"Intentional firewall-off choice recorded {recorded}. Pulse will retain the evidence but will not request review for this posture.";
            return;
        }

        FirewallIntentButton.Content = "Firewall Is Off by Choice";
        FirewallIntentButton.IsEnabled = _inactiveFirewallDetected;
        FirewallIntentStatusText.Text = _inactiveFirewallDetected
            ? "If this firewall state is intentional, record that choice so Pulse retains the evidence without requesting review. This does not change firewall configuration."
            : "No inactive UFW/nftables service posture currently needs an intentional exception.";
    }

    private void FirewallIntentButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (!_userPreferences.IgnoreInactiveFirewall && !_inactiveFirewallDetected)
        {
            SetActivity("Pulse can record this choice only after an assessment finds the firewall service posture inactive.");
            return;
        }

        try
        {
            var acknowledge = !_userPreferences.IgnoreInactiveFirewall;
            _userPreferences = _preferencesStore.SetInactiveFirewallAcknowledged(acknowledge);
            RefreshDashboardFromHistory();
            SetActivity(acknowledge
                ? "Recorded that the inactive firewall posture is intentional. Pulse did not change the firewall."
                : "Restored firewall review. Pulse did not change the firewall.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            SetActivity($"Pulse could not save the firewall preference: {ex.Message}");
        }
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

        ApplyIntelligenceCard(storageItems[0], StorageCapacityStateText, StorageCapacityDetailText);
        ApplyIntelligenceCard(storageItems[1], StorageMountStateText, StorageMountDetailText);
        ApplyIntelligenceCard(storageItems[2], StorageInodeStateText, StorageInodeDetailText);
        ApplyIntelligenceCard(storageItems[3], StorageDriveStateText, StorageDriveDetailText);
        ApplyIntelligenceCard(storageItems[4], StorageEncryptionStateText, StorageEncryptionDetailText);
        ApplyIntelligenceCard(storageItems[5], StorageBackupStateText, StorageBackupDetailText);

        var available = storageItems.Where(item => item is not null).Select(item => item!).ToArray();
        if (available.Length == 0)
        {
            _storageReviewEvidence = null;
            StorageReviewActionButton.IsEnabled = false;
            StorageReviewActionButton.Content = "Review Details";
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
        _storageReviewEvidence = SelectReviewEvidence(available);
        StorageRecommendationText.Text = _storageReviewEvidence?.Guidance ?? "No storage recommendation is available.";
        ConfigureReviewAction(StorageReviewActionButton, _storageReviewEvidence);
    }

    private void RenderBackupIntelligence(IReadOnlyList<EvidenceResult> results)
    {
        var backupItems = new[]
        {
            FindEvidence(results, "linux.backup-posture"),
            FindEvidence(results, "linux.backup-schedule"),
            FindEvidence(results, "linux.backup-activity"),
            FindEvidence(results, "linux.backup-destination-mounts"),
            FindEvidence(results, "linux.backup-system-snapshots"),
            FindEvidence(results, "linux.backup-restore-readiness")
        };

        ApplyIntelligenceCard(backupItems[0], BackupPostureStateText, BackupPostureDetailText);
        ApplyIntelligenceCard(backupItems[1], BackupScheduleStateText, BackupScheduleDetailText);
        ApplyIntelligenceCard(backupItems[2], BackupActivityStateText, BackupActivityDetailText);
        ApplyIntelligenceCard(backupItems[3], BackupDestinationStateText, BackupDestinationDetailText);
        ApplyIntelligenceCard(backupItems[4], BackupSnapshotStateText, BackupSnapshotDetailText);
        ApplyIntelligenceCard(backupItems[5], BackupRestoreStateText, BackupRestoreDetailText);

        var available = backupItems.Where(item => item is not null).Select(item => item!).ToArray();
        if (available.Length == 0)
        {
            _backupReviewEvidence = null;
            BackupReviewActionButton.IsEnabled = false;
            BackupReviewActionButton.Content = "Open Backup Application";
            BackupExecutiveStateText.Text = "Pending Assessment";
            BackupExecutiveStateText.Foreground = BrushForHealth("Attention Recommended");
            BackupExecutiveDetailText.Text = "Run an assessment to review detectable backup tooling, schedules, activity metadata, mounted destination context, snapshots, and restore readiness.";
            BackupRecommendationText.Text = "Run an assessment to establish Backup Intelligence.";
            return;
        }

        var health = PulseHealthInterpreter.Interpret(available);
        BackupExecutiveStateText.Text = health.State;
        BackupExecutiveStateText.Foreground = BrushForHealth(health.State);
        BackupExecutiveDetailText.Text = health.Detail;
        _backupReviewEvidence = SelectReviewEvidence(available);
        BackupRecommendationText.Text = _backupReviewEvidence?.Guidance ?? "No backup recommendation is available.";
        ConfigureReviewAction(BackupReviewActionButton, _backupReviewEvidence);
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

        ApplyIntelligenceCard(packageItems[0], PackageDatabaseStateText, PackageDatabaseDetailText);
        ApplyIntelligenceCard(packageItems[1], PackageInventoryStateText, PackageInventoryDetailText);
        ApplyIntelligenceCard(packageItems[2], PackageUpdatesStateText, PackageUpdatesDetailText);
        ApplyIntelligenceCard(packageItems[3], PackageSecurityStateText, PackageSecurityDetailText);
        ApplyIntelligenceCard(packageItems[4], PackageAutomaticStateText, PackageAutomaticDetailText);
        ApplyIntelligenceCard(packageItems[5], PackageRestartStateText, PackageRestartDetailText);

        var available = packageItems.Where(item => item is not null).Select(item => item!).ToArray();
        if (available.Length == 0)
        {
            _packageReviewEvidence = null;
            PackageReviewActionButton.IsEnabled = false;
            PackageReviewActionButton.Content = "Review Details";
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
        _packageReviewEvidence = SelectReviewEvidence(available);
        PackageRecommendationText.Text = _packageReviewEvidence?.Guidance ?? "No package recommendation is available.";
        ConfigureReviewAction(PackageReviewActionButton, _packageReviewEvidence);
    }

    private void RenderSecurityIntelligence(IReadOnlyList<EvidenceResult> results)
    {
        var securityItems = new[]
        {
            FindEvidence(results, "linux.apparmor"),
            FindEvidence(results, "linux.firewall-indicator"),
            FindEvidence(results, "linux.apt-security-updates"),
            FindEvidence(results, "linux.unattended-upgrades"),
            FindEvidence(results, "linux.luks-indicator"),
            FindEvidence(results, "linux.secure-boot")
        };

        ApplyIntelligenceCard(securityItems[0], SecurityAppArmorStateText, SecurityAppArmorDetailText);
        ApplyIntelligenceCard(securityItems[1], SecurityFirewallStateText, SecurityFirewallDetailText);
        ApplyIntelligenceCard(securityItems[2], SecurityUpdatesStateText, SecurityUpdatesDetailText);
        ApplyIntelligenceCard(securityItems[3], SecurityAutomaticStateText, SecurityAutomaticDetailText);
        ApplyIntelligenceCard(securityItems[4], SecurityEncryptionStateText, SecurityEncryptionDetailText);
        ApplyIntelligenceCard(securityItems[5], SecuritySecureBootStateText, SecuritySecureBootDetailText);

        var available = securityItems.Where(item => item is not null).Select(item => item!).ToArray();
        if (available.Length == 0)
        {
            _securityReviewEvidence = null;
            SecurityReviewActionButton.IsEnabled = false;
            SecurityReviewActionButton.Content = "Review Details";
            SecurityExecutiveStateText.Text = "Pending Assessment";
            SecurityExecutiveStateText.Foreground = BrushForHealth("Attention Recommended");
            SecurityExecutiveDetailText.Text = "Run an assessment to review system protection layers, cached security maintenance, disk encryption, and Secure Boot posture.";
            SecurityRecommendationText.Text = "Run an assessment to establish Security Intelligence.";
            return;
        }

        var health = PulseHealthInterpreter.Interpret(available);
        SecurityExecutiveStateText.Text = health.State;
        SecurityExecutiveStateText.Foreground = BrushForHealth(health.State);
        SecurityExecutiveDetailText.Text = health.Detail;
        _securityReviewEvidence = SelectReviewEvidence(available);
        SecurityRecommendationText.Text = _securityReviewEvidence?.Guidance ?? "No security recommendation is available.";
        ConfigureReviewAction(SecurityReviewActionButton, _securityReviewEvidence);
    }

    private void RenderPerformanceIntelligence(IReadOnlyList<EvidenceResult> results)
    {
        var performanceItems = new[]
        {
            FindEvidence(results, "linux.performance-load"),
            FindEvidence(results, "linux.performance-memory"),
            FindEvidence(results, "linux.performance-cpu-pressure"),
            FindEvidence(results, "linux.performance-memory-pressure"),
            FindEvidence(results, "linux.performance-io-pressure"),
            FindEvidence(results, "linux.performance-thermal")
        };

        ApplyIntelligenceCard(performanceItems[0], PerformanceLoadStateText, PerformanceLoadDetailText);
        ApplyIntelligenceCard(performanceItems[1], PerformanceMemoryStateText, PerformanceMemoryDetailText);
        ApplyIntelligenceCard(performanceItems[2], PerformanceCpuPressureStateText, PerformanceCpuPressureDetailText);
        ApplyIntelligenceCard(performanceItems[3], PerformanceMemoryPressureStateText, PerformanceMemoryPressureDetailText);
        ApplyIntelligenceCard(performanceItems[4], PerformanceIoPressureStateText, PerformanceIoPressureDetailText);
        ApplyIntelligenceCard(performanceItems[5], PerformanceThermalStateText, PerformanceThermalDetailText);

        var available = performanceItems.Where(item => item is not null).Select(item => item!).ToArray();
        if (available.Length == 0)
        {
            _performanceReviewEvidence = null;
            PerformanceReviewActionButton.IsEnabled = false;
            PerformanceReviewActionButton.Content = "Review Details";
            PerformanceExecutiveStateText.Text = "Pending Assessment";
            PerformanceExecutiveStateText.Foreground = BrushForHealth("Attention Recommended");
            PerformanceExecutiveDetailText.Text = "Run an assessment to review sustained load, available memory, Linux pressure signals, and thermal posture.";
            PerformanceRecommendationText.Text = "Run an assessment to establish Performance Intelligence.";
            return;
        }

        var health = PulseHealthInterpreter.Interpret(available);
        PerformanceExecutiveStateText.Text = health.State;
        PerformanceExecutiveStateText.Foreground = BrushForHealth(health.State);
        PerformanceExecutiveDetailText.Text = health.Detail;
        _performanceReviewEvidence = SelectReviewEvidence(available);
        PerformanceRecommendationText.Text = _performanceReviewEvidence?.Guidance ?? "No performance recommendation is available.";
        ConfigureReviewAction(PerformanceReviewActionButton, _performanceReviewEvidence);
    }

    private void RenderHardwareIntelligence(IReadOnlyList<EvidenceResult> results)
    {
        var hardwareItems = new[]
        {
            FindEvidence(results, "linux.hardware-processor"),
            FindEvidence(results, "linux.hardware-memory"),
            FindEvidence(results, "linux.hardware-firmware"),
            FindEvidence(results, "linux.hardware-battery"),
            FindEvidence(results, "linux.hardware-graphics"),
            FindEvidence(results, "linux.hardware-virtualization")
        };

        ApplyIntelligenceCard(hardwareItems[0], HardwareProcessorStateText, HardwareProcessorDetailText);
        ApplyIntelligenceCard(hardwareItems[1], HardwareMemoryStateText, HardwareMemoryDetailText);
        ApplyIntelligenceCard(hardwareItems[2], HardwareFirmwareStateText, HardwareFirmwareDetailText);
        ApplyIntelligenceCard(hardwareItems[3], HardwareBatteryStateText, HardwareBatteryDetailText);
        ApplyIntelligenceCard(hardwareItems[4], HardwareGraphicsStateText, HardwareGraphicsDetailText);
        ApplyIntelligenceCard(hardwareItems[5], HardwareVirtualizationStateText, HardwareVirtualizationDetailText);

        var available = hardwareItems.Where(item => item is not null).Select(item => item!).ToArray();
        if (available.Length == 0)
        {
            _hardwareReviewEvidence = null;
            HardwareReviewActionButton.IsEnabled = false;
            HardwareReviewActionButton.Content = "Review Details";
            HardwareExecutiveStateText.Text = "Pending Assessment";
            HardwareExecutiveStateText.Foreground = BrushForHealth("Attention Recommended");
            HardwareExecutiveDetailText.Text = "Run an assessment to review processor, memory, firmware, battery, graphics, and virtualization context.";
            HardwareRecommendationText.Text = "Run an assessment to establish Hardware Intelligence.";
            return;
        }

        var health = PulseHealthInterpreter.Interpret(available);
        HardwareExecutiveStateText.Text = health.State;
        HardwareExecutiveStateText.Foreground = BrushForHealth(health.State);
        HardwareExecutiveDetailText.Text = health.Detail;
        _hardwareReviewEvidence = SelectReviewEvidence(available);
        HardwareRecommendationText.Text = _hardwareReviewEvidence?.Guidance ?? "No hardware recommendation is available.";
        ConfigureReviewAction(HardwareReviewActionButton, _hardwareReviewEvidence);
    }

    private void RenderStartupIntelligence(IReadOnlyList<EvidenceResult> results)
    {
        var items = new[]
        {
            FindEvidence(results, "linux.systemd-boot-timing"),
            FindEvidence(results, "linux.startup-critical-chain"),
            FindEvidence(results, "linux.systemd-system-failed"),
            FindEvidence(results, "linux.systemd-user-failed"),
            FindEvidence(results, "linux.startup-desktop-autostart"),
            FindEvidence(results, "linux.startup-enabled-user-units")
        };

        ApplyIntelligenceCard(items[0], StartupBootStateText, StartupBootDetailText);
        ApplyIntelligenceCard(items[1], StartupCriticalStateText, StartupCriticalDetailText);
        ApplyIntelligenceCard(items[2], StartupSystemFailedStateText, StartupSystemFailedDetailText);
        ApplyIntelligenceCard(items[3], StartupUserFailedStateText, StartupUserFailedDetailText);
        ApplyIntelligenceCard(items[4], StartupDesktopStateText, StartupDesktopDetailText);
        ApplyIntelligenceCard(items[5], StartupEnabledUserStateText, StartupEnabledUserDetailText);

        var available = items.Where(item => item is not null).Select(item => item!).ToArray();
        if (available.Length == 0)
        {
            _startupReviewEvidence = null;
            StartupReviewActionButton.IsEnabled = false;
            StartupReviewActionButton.Content = "Review Details";
            StartupExecutiveStateText.Text = "Pending Assessment";
            StartupExecutiveStateText.Foreground = BrushForHealth("Attention Recommended");
            StartupExecutiveDetailText.Text = "Run an assessment to review boot timing, the critical chain, failed services, desktop autostart, and enabled user units.";
            StartupRecommendationText.Text = "Run an assessment to establish Startup Intelligence.";
            return;
        }

        var health = PulseHealthInterpreter.Interpret(available);
        StartupExecutiveStateText.Text = health.State;
        StartupExecutiveStateText.Foreground = BrushForHealth(health.State);
        StartupExecutiveDetailText.Text = health.Detail;
        _startupReviewEvidence = SelectReviewEvidence(available);
        StartupRecommendationText.Text = _startupReviewEvidence?.Guidance ?? "No startup recommendation is available.";
        ConfigureReviewAction(StartupReviewActionButton, _startupReviewEvidence);
    }

    private void RenderReliabilityIntelligence(IReadOnlyList<EvidenceResult> results)
    {
        var reliabilityItems = new[]
        {
            FindEvidence(results, "linux.journal-reliability"),
            FindEvidence(results, "linux.systemd-system-failed"),
            FindEvidence(results, "linux.systemd-user-failed"),
            FindEvidence(results, "linux.systemd-boot-timing"),
            FindEvidence(results, "linux.uptime"),
            FindEvidence(results, "linux.reboot-required")
        };

        ApplyIntelligenceCard(reliabilityItems[0], ReliabilityJournalStateText, ReliabilityJournalDetailText);
        ApplyIntelligenceCard(reliabilityItems[1], ReliabilitySystemServicesStateText, ReliabilitySystemServicesDetailText);
        ApplyIntelligenceCard(reliabilityItems[2], ReliabilityUserServicesStateText, ReliabilityUserServicesDetailText);
        ApplyIntelligenceCard(reliabilityItems[3], ReliabilityBootStateText, ReliabilityBootDetailText);
        ApplyIntelligenceCard(reliabilityItems[4], ReliabilityUptimeStateText, ReliabilityUptimeDetailText);
        ApplyIntelligenceCard(reliabilityItems[5], ReliabilityRestartStateText, ReliabilityRestartDetailText);

        var available = reliabilityItems.Where(item => item is not null).Select(item => item!).ToArray();
        if (available.Length == 0)
        {
            _reliabilityReviewEvidence = null;
            ReliabilityReviewActionButton.IsEnabled = false;
            ReliabilityReviewActionButton.Content = "Review Details";
            ReliabilityExecutiveStateText.Text = "Pending Assessment";
            ReliabilityExecutiveStateText.Foreground = BrushForHealth("Attention Recommended");
            ReliabilityExecutiveDetailText.Text = "Run an assessment to review current-boot errors, failed services, boot timing, uptime, and restart posture.";
            ReliabilityRecommendationText.Text = "Run an assessment to establish Reliability Intelligence.";
            return;
        }

        var health = PulseHealthInterpreter.Interpret(available);
        ReliabilityExecutiveStateText.Text = health.State;
        ReliabilityExecutiveStateText.Foreground = BrushForHealth(health.State);
        ReliabilityExecutiveDetailText.Text = health.Detail;
        _reliabilityReviewEvidence = SelectReviewEvidence(available);
        ReliabilityRecommendationText.Text = _reliabilityReviewEvidence?.Guidance ?? "No reliability recommendation is available.";
        ConfigureReviewAction(ReliabilityReviewActionButton, _reliabilityReviewEvidence);
    }

    private void RenderCompatibility(IReadOnlyList<EvidenceResult> results)
    {
        var compatibilityItems = new[]
        {
            FindEvidence(results, "linux.compatibility-distribution"),
            FindEvidence(results, "linux.compatibility-architecture"),
            FindEvidence(results, "linux.compatibility-desktop"),
            FindEvidence(results, "linux.compatibility-display"),
            FindEvidence(results, "linux.compatibility-user-services"),
            FindEvidence(results, "linux.compatibility-tool-coverage")
        };

        ApplyIntelligenceCard(compatibilityItems[0], CompatibilityDistributionStateText, CompatibilityDistributionDetailText);
        ApplyIntelligenceCard(compatibilityItems[1], CompatibilityArchitectureStateText, CompatibilityArchitectureDetailText);
        ApplyIntelligenceCard(compatibilityItems[2], CompatibilityDesktopStateText, CompatibilityDesktopDetailText);
        ApplyIntelligenceCard(compatibilityItems[3], CompatibilityDisplayStateText, CompatibilityDisplayDetailText);
        ApplyIntelligenceCard(compatibilityItems[4], CompatibilityUserServiceStateText, CompatibilityUserServiceDetailText);
        ApplyIntelligenceCard(compatibilityItems[5], CompatibilityToolsStateText, CompatibilityToolsDetailText);

        var available = compatibilityItems.Where(item => item is not null).Select(item => item!).ToArray();
        if (available.Length == 0)
        {
            _compatibilityReviewEvidence = null;
            CompatibilityReviewActionButton.IsEnabled = false;
            CompatibilityExecutiveStateText.Text = "Pending Assessment";
            CompatibilityExecutiveStateText.Foreground = BrushForHealth("Attention Recommended");
            CompatibilityExecutiveDetailText.Text = "Run an assessment to confirm the distribution boundary, architecture, desktop/display session, user-service readiness, and native evidence-tool coverage.";
            CompatibilityRecommendationText.Text = "Run an assessment to establish Linux Compatibility.";
            return;
        }

        var hasNotes = available.Any(item => item.State != EvidenceState.Healthy);
        CompatibilityExecutiveStateText.Text = hasNotes ? "Compatible with Notes" : "Compatible";
        CompatibilityExecutiveStateText.Foreground = BrushForHealth(hasNotes ? "Healthy" : "Optimized");
        CompatibilityExecutiveDetailText.Text = hasNotes
            ? "Pulse is operating inside the verified distribution gate, with one or more compatibility or coverage notes shown below. These notes do not lower system health."
            : "The available distribution, architecture, desktop, display, user-service, and evidence-tool checks match the current Pulse Linux release boundary.";
        _compatibilityReviewEvidence = SelectReviewEvidence(available);
        CompatibilityRecommendationText.Text = _compatibilityReviewEvidence?.Guidance ?? "No compatibility guidance is available.";
        ConfigureReviewAction(CompatibilityReviewActionButton, _compatibilityReviewEvidence);
    }

    private static EvidenceResult? SelectReviewEvidence(IReadOnlyList<EvidenceResult> evidence) =>
        evidence.OrderBy(item => EvidencePriority(item.State)).FirstOrDefault();

    private static void ConfigureReviewAction(Button button, EvidenceResult? evidence)
    {
        button.IsEnabled = evidence is not null;
        button.Content = evidence?.ProviderId switch
        {
            "linux.apt-cached-updates" or "linux.apt-security-updates" or "linux.unattended-upgrades" => "Open Software Updater",
            "linux.drive-health" => "Open Disk Utility",
            "linux.backup-posture" or "linux.backup-schedule" or "linux.backup-activity" or
                "linux.backup-destination-mounts" or "linux.backup-system-snapshots" or
                "linux.backup-restore-readiness" => "Open Backup Application",
            "linux.network-posture" or "linux.default-route" or "linux.network-manager" or "linux.dns-configuration" or "linux.listening-services" => "Open Network Settings",
            "linux.firewall-indicator" => "Open Firewall Settings",
            "linux.performance-load" or "linux.performance-memory" or "linux.performance-cpu-pressure" or
                "linux.performance-memory-pressure" or "linux.performance-io-pressure" or "linux.performance-thermal" => "Open System Monitor",
            "linux.hardware-battery" => "Open Power Settings",
            "linux.startup-desktop-autostart" or "linux.startup-enabled-user-units" => "Open Startup Settings",
            "linux.journal-reliability" or "linux.systemd-system-failed" or "linux.systemd-user-failed" => "Open System Logs",
            null => "Review Details",
            _ => "Review Details"
        };
    }

    private void ReviewActionButton_OnClick(object? sender, RoutedEventArgs e)
    {
        EvidenceResult? evidence;
        if (ReferenceEquals(sender, PackageReviewActionButton))
        {
            evidence = _packageReviewEvidence;
        }
        else if (ReferenceEquals(sender, NetworkReviewActionButton))
        {
            evidence = _networkReviewEvidence;
        }
        else if (ReferenceEquals(sender, StorageReviewActionButton))
        {
            evidence = _storageReviewEvidence;
        }
        else if (ReferenceEquals(sender, BackupReviewActionButton))
        {
            evidence = _backupReviewEvidence;
        }
        else if (ReferenceEquals(sender, SecurityReviewActionButton))
        {
            evidence = _securityReviewEvidence;
        }
        else if (ReferenceEquals(sender, ReliabilityReviewActionButton))
        {
            evidence = _reliabilityReviewEvidence;
        }
        else if (ReferenceEquals(sender, PerformanceReviewActionButton))
        {
            evidence = _performanceReviewEvidence;
        }
        else if (ReferenceEquals(sender, HardwareReviewActionButton))
        {
            evidence = _hardwareReviewEvidence;
        }
        else if (ReferenceEquals(sender, StartupReviewActionButton))
        {
            evidence = _startupReviewEvidence;
        }
        else if (ReferenceEquals(sender, CompatibilityReviewActionButton))
        {
            evidence = _compatibilityReviewEvidence;
        }
        else
        {
            evidence = null;
        }
        if (evidence is null)
        {
            SetActivity("Run an assessment before reviewing evidence.");
            return;
        }

        if (evidence.ProviderId is "linux.apt-cached-updates" or "linux.apt-security-updates" or "linux.unattended-upgrades" &&
            TryLaunchInstalledTool(["mintupdate", "update-manager", "gnome-software"], "software updater"))
        {
            return;
        }

        if (evidence.ProviderId == "linux.drive-health" &&
            TryLaunchInstalledTool(["gnome-disks"], "disk utility"))
        {
            return;
        }

        if (evidence.ProviderId is "linux.backup-posture" or "linux.backup-schedule" or
            "linux.backup-activity" or "linux.backup-destination-mounts" or
            "linux.backup-system-snapshots" or "linux.backup-restore-readiness" &&
            TryLaunchInstalledTool(["deja-dup", "pika-backup", "backintime-qt", "timeshift-gtk"], "backup application"))
        {
            return;
        }

        if (evidence.ProviderId is "linux.network-posture" or "linux.default-route" or "linux.network-manager" or
            "linux.dns-configuration" or "linux.listening-services" &&
            TryLaunchNetworkSettings())
        {
            return;
        }

        if (evidence.ProviderId == "linux.firewall-indicator" &&
            TryLaunchInstalledTool(["gufw"], "firewall settings"))
        {
            return;
        }

        if (evidence.ProviderId is "linux.performance-load" or "linux.performance-memory" or
            "linux.performance-cpu-pressure" or "linux.performance-memory-pressure" or
            "linux.performance-io-pressure" or "linux.performance-thermal" &&
            TryLaunchInstalledTool(["gnome-system-monitor", "mate-system-monitor", "plasma-systemmonitor"], "system monitor"))
        {
            return;
        }

        if (evidence.ProviderId is "linux.journal-reliability" or "linux.systemd-system-failed" or "linux.systemd-user-failed" &&
            TryLaunchInstalledTool(["gnome-logs", "ksystemlog"], "system logs"))
        {
            return;
        }

        if (evidence.ProviderId == "linux.hardware-battery" && TryLaunchPowerSettings())
        {
            return;
        }

        if (evidence.ProviderId is "linux.startup-desktop-autostart" or "linux.startup-enabled-user-units" &&
            TryLaunchStartupSettings())
        {
            return;
        }

        ShowPage("Assessment");
        ShowAssessmentSection(evidence.State switch
        {
            EvidenceState.Healthy => "Healthy",
            EvidenceState.Informational => "Information",
            _ => "Guidance"
        });
        SetActivity($"Reviewing {evidence.Title}. Pulse shows the finding, evidence source, and safe guidance without changing the system.");
    }

    private bool TryLaunchInstalledTool(IReadOnlyList<string> toolNames, string description)
    {
        foreach (var toolName in toolNames)
        {
            var executable = new[] { $"/usr/bin/{toolName}", $"/usr/sbin/{toolName}", $"/bin/{toolName}" }
                .FirstOrDefault(File.Exists);
            if (executable is null)
            {
                continue;
            }

            try
            {
                Process.Start(new ProcessStartInfo { FileName = executable, UseShellExecute = false });
                SetActivity($"Opened the installed {description} for {toolName}.");
                return true;
            }
            catch (Exception ex)
            {
                SetActivity($"Pulse could not open the {description}: {ex.Message}");
                return false;
            }
        }

        SetActivity($"No supported graphical {description} was found. Pulse is showing the detailed evidence instead.");
        return false;
    }

    private bool TryLaunchNetworkSettings()
    {
        var candidates = new[]
        {
            (Executable: "/usr/bin/nm-connection-editor", Arguments: Array.Empty<string>()),
            (Executable: "/usr/bin/gnome-control-center", Arguments: new[] { "network" }),
            (Executable: "/usr/bin/systemsettings", Arguments: new[] { "kcm_networkmanagement" })
        };
        foreach (var candidate in candidates.Where(candidate => File.Exists(candidate.Executable)))
        {
            try
            {
                var startInfo = new ProcessStartInfo { FileName = candidate.Executable, UseShellExecute = false };
                foreach (var argument in candidate.Arguments)
                {
                    startInfo.ArgumentList.Add(argument);
                }

                Process.Start(startInfo);
                SetActivity("Opened the installed desktop network settings. If it was already open, check its existing window.");
                return true;
            }
            catch (Exception ex)
            {
                SetActivity($"Pulse could not open the network settings: {ex.Message}");
                return false;
            }
        }

        SetActivity("No supported graphical network-settings utility was found. Pulse is showing the detailed evidence instead.");
        return false;
    }

    private bool TryLaunchPowerSettings()
    {
        var candidates = new[]
        {
            (Executable: "/usr/bin/gnome-control-center", Arguments: new[] { "power" }),
            (Executable: "/usr/bin/mate-power-preferences", Arguments: Array.Empty<string>()),
            (Executable: "/usr/bin/systemsettings", Arguments: new[] { "kcm_powerdevilprofilesconfig" })
        };
        foreach (var candidate in candidates.Where(candidate => File.Exists(candidate.Executable)))
        {
            try
            {
                var startInfo = new ProcessStartInfo { FileName = candidate.Executable, UseShellExecute = false };
                foreach (var argument in candidate.Arguments)
                {
                    startInfo.ArgumentList.Add(argument);
                }

                Process.Start(startInfo);
                SetActivity("Opened the installed desktop power settings. If it was already open, check its existing window.");
                return true;
            }
            catch (Exception ex)
            {
                SetActivity($"Pulse could not open the power settings: {ex.Message}");
                return false;
            }
        }

        SetActivity("No supported graphical power-settings utility was found. Pulse is showing the detailed evidence instead.");
        return false;
    }

    private bool TryLaunchStartupSettings()
    {
        var candidates = new[]
        {
            (Executable: "/usr/bin/cinnamon-settings", Arguments: new[] { "startup" }),
            (Executable: "/usr/bin/gnome-session-properties", Arguments: Array.Empty<string>()),
            (Executable: "/usr/bin/mate-session-properties", Arguments: Array.Empty<string>()),
            (Executable: "/usr/bin/systemsettings", Arguments: new[] { "kcm_autostart" })
        };
        foreach (var candidate in candidates.Where(candidate => File.Exists(candidate.Executable)))
        {
            try
            {
                var startInfo = new ProcessStartInfo { FileName = candidate.Executable, UseShellExecute = false };
                foreach (var argument in candidate.Arguments)
                {
                    startInfo.ArgumentList.Add(argument);
                }

                Process.Start(startInfo);
                SetActivity("Opened the installed desktop startup settings. If it was already open, check its existing window.");
                return true;
            }
            catch (Exception ex)
            {
                SetActivity($"Pulse could not open the startup settings: {ex.Message}");
                return false;
            }
        }

        SetActivity("No supported graphical startup-settings utility was found. Pulse is showing the detailed evidence instead.");
        return false;
    }

    private static EvidenceResult? FindEvidence(IReadOnlyList<EvidenceResult> results, string providerId) =>
        results.FirstOrDefault(item => item.ProviderId.Equals(providerId, StringComparison.Ordinal));

    private static void ApplyIntelligenceCard(EvidenceResult? evidence, TextBlock stateText, TextBlock detailText)
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
        NetworkOpenReportButton.IsEnabled = enabled;
        StorageOpenReportButton.IsEnabled = enabled;
        BackupOpenReportButton.IsEnabled = enabled;
        SecurityOpenReportButton.IsEnabled = enabled;
        PerformanceOpenReportButton.IsEnabled = enabled;
        HardwareOpenReportButton.IsEnabled = enabled;
        StartupOpenReportButton.IsEnabled = enabled;
        ReliabilityOpenReportButton.IsEnabled = enabled;
        CompatibilityOpenReportButton.IsEnabled = enabled;
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

    private bool OpenPath(string path, string description)
    {
        try
        {
            Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
            SetActivity($"Opened {description}.");
            return true;
        }
        catch (Exception ex)
        {
            SetActivity($"Pulse could not open the {description}: {ex.Message}");
            return false;
        }
    }

    private async void CheckForUpdatesButton_OnClick(object? sender, RoutedEventArgs e)
    {
        CheckForUpdatesButton.IsEnabled = false;
        CheckForUpdatesButton.Content = "Checking…";
        DownloadUpdateButton.IsEnabled = false;
        OpenUpdateInstallerButton.IsEnabled = false;
        ViewReleaseButton.IsEnabled = false;
        UpdateStateText.Text = "Checking GitHub";
        UpdateStateText.Foreground = BrushForEvidence(EvidenceState.Informational);
        UpdateDetailText.Text = "Reading published Pulse releases for this architecture…";
        UpdateLatestVersionText.Text = "Checking…";
        UpdatePackageText.Text = "Selecting a compatible Debian package and checksum file.";
        UpdateReleaseNotesText.Text = "Waiting for release information…";
        UpdateDownloadStatusText.Text = "No download has started.";
        UpdateDownloadProgress.IsVisible = false;
        _availableUpdate = null;
        _downloadedUpdatePath = null;
        SetActivity("User-requested GitHub update check started.");

        try
        {
            var result = await _updates.CheckAsync(AppInfo.Version, RuntimeInformation.ProcessArchitecture);
            _availableUpdate = result;
            UpdateDetailText.Text = result.Message;
            UpdateLatestVersionText.Text = result.LatestVersion is null
                ? "Unavailable"
                : $"Version {result.LatestVersion}{AppInfo.EditionCode}";
            UpdateReleaseNotesText.Text = string.IsNullOrWhiteSpace(result.ReleaseNotes)
                ? "No release notes were published for this release."
                : result.ReleaseNotes.Trim();
            ViewReleaseButton.IsEnabled = !string.IsNullOrWhiteSpace(result.ReleasePageUrl);

            switch (result.Availability)
            {
                case UpdateAvailability.Available:
                    UpdateStateText.Text = "Update Available";
                    UpdateStateText.Foreground = BrushForEvidence(EvidenceState.Attention);
                    UpdatePackageText.Text = $"Selected and ready to verify: {result.PackageAssetName}";
                    DownloadUpdateButton.IsEnabled = true;
                    SetActivity($"Pulse Linux {result.LatestVersion}{AppInfo.EditionCode} is available for download.");
                    break;
                case UpdateAvailability.Current:
                    UpdateStateText.Text = "Pulse Is Current";
                    UpdateStateText.Foreground = BrushForEvidence(EvidenceState.Healthy);
                    UpdatePackageText.Text = "No newer compatible Pulse Linux package is published.";
                    SetActivity("The installed Pulse Linux version is current.");
                    break;
                case UpdateAvailability.Ahead:
                    UpdateStateText.Text = "Installed Build Is Newer";
                    UpdateStateText.Foreground = BrushForEvidence(EvidenceState.Informational);
                    UpdatePackageText.Text = "No downgrade was selected. GitHub has not published a newer compatible Linux package.";
                    SetActivity("The installed Pulse Linux build is newer than the newest compatible published release.");
                    break;
                case UpdateAvailability.UnsupportedArchitecture:
                    UpdateStateText.Text = "Architecture Unsupported";
                    UpdateStateText.Foreground = BrushForEvidence(EvidenceState.Attention);
                    UpdatePackageText.Text = "Pulse did not select or download a package.";
                    SetActivity("The current architecture is not supported by the updater.");
                    break;
                default:
                    UpdateStateText.Text = "Update Check Unavailable";
                    UpdateStateText.Foreground = BrushForEvidence(EvidenceState.Unavailable);
                    UpdatePackageText.Text = "No package was selected or downloaded.";
                    SetActivity("The user-requested GitHub update check was unavailable.");
                    break;
            }
        }
        catch (Exception ex)
        {
            UpdateStateText.Text = "Update Check Unavailable";
            UpdateStateText.Foreground = BrushForEvidence(EvidenceState.Unavailable);
            UpdateDetailText.Text = $"Pulse could not complete the update check. {ex.Message}";
            UpdateLatestVersionText.Text = "Unavailable";
            UpdatePackageText.Text = "No package was selected or downloaded.";
            UpdateReleaseNotesText.Text = "No release information is available.";
            SetActivity("The update check ended safely without downloading or installing anything.");
        }
        finally
        {
            CheckForUpdatesButton.Content = "Check for Updates";
            CheckForUpdatesButton.IsEnabled = true;
        }
    }

    private async void DownloadUpdateButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_availableUpdate is not { Availability: UpdateAvailability.Available })
        {
            UpdateDownloadStatusText.Text = "Check for updates before downloading.";
            return;
        }

        DownloadUpdateButton.IsEnabled = false;
        CheckForUpdatesButton.IsEnabled = false;
        DownloadUpdateButton.Content = "Downloading…";
        UpdateDownloadProgress.Value = 0;
        UpdateDownloadProgress.IsVisible = true;
        UpdateDownloadStatusText.Text = "Downloading the Debian package and preparing SHA-256 verification…";
        SetActivity("User-approved Pulse update download started.");

        try
        {
            var downloadsDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            var progress = new Progress<int>(value =>
            {
                UpdateDownloadProgress.Value = value;
                UpdateDownloadStatusText.Text = $"Downloading and verifying… {value}%";
            });
            var result = await _updates.DownloadAndVerifyAsync(_availableUpdate, downloadsDirectory, progress);
            UpdateDownloadStatusText.Text = result.Message;
            _downloadedUpdatePath = result.PackagePath;
            OpenUpdateInstallerButton.IsEnabled = result.Succeeded && File.Exists(result.PackagePath);
            if (result.Succeeded)
            {
                UpdateDownloadProgress.Value = 100;
                SetActivity("Pulse downloaded and verified the update package. Installation still requires user approval.");
            }
            else
            {
                SetActivity("The update was not downloaded or verified; no installation was started.");
            }
        }
        catch (Exception ex)
        {
            _downloadedUpdatePath = null;
            OpenUpdateInstallerButton.IsEnabled = false;
            UpdateDownloadStatusText.Text = $"Pulse could not complete the download. {ex.Message}";
            SetActivity("The update download ended safely; no installation was started.");
        }
        finally
        {
            DownloadUpdateButton.Content = "Download Update";
            DownloadUpdateButton.IsEnabled = _availableUpdate?.Availability == UpdateAvailability.Available;
            CheckForUpdatesButton.IsEnabled = true;
        }
    }

    private void OpenUpdateInstallerButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_downloadedUpdatePath) || !File.Exists(_downloadedUpdatePath))
        {
            OpenUpdateInstallerButton.IsEnabled = false;
            UpdateDownloadStatusText.Text = "The verified package is no longer available. Download it again.";
            return;
        }

        UpdateDownloadStatusText.Text = OpenPath(_downloadedUpdatePath, "verified Pulse update package in the graphical installer")
            ? "The verified package was handed to the desktop. Approve installation in the graphical installer when it appears. If the installer was already open, check its existing window."
            : "Pulse could not open the graphical package installer. The verified package remains in Downloads.";
    }

    private void ViewReleaseButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var releaseUrl = _availableUpdate?.ReleasePageUrl ?? GitHubUpdateService.ReleasesPageUrl;
        OpenPath(releaseUrl, "Pulse release page");
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
