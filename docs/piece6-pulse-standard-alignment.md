# Piece 6 Pulse Standard alignment

Pulse Linux Beta 0.0.0.6 replaces the temporary engineering scroll shell with the established Pulse Aurora application structure.

## Authoritative references

The alignment pass is grounded in the maintained Pulse materials:

- Aurora shell/navigation implementation in `MainForm.cs`
- Pulse Electric tokens in `PulseColors.cs`
- Pulse 7.5.4 through 7.5.5.1 release-history decisions
- Pulse Executive Gauge v1 HTML mockup
- Official dark Pulse logo supplied for the Linux shell

The exact macOS Preview 0.52.0/shared Pulse Core source bundle remains a later merge dependency. Linux-native providers stay isolated so that merge does not require rewriting evidence collection.

## Aurora shell

The application now has the permanent Pulse logo and two navigation groups:

### Pulse Health Platform

- Dashboard
- Linux Assessment
- Reports

### Pulse Administration

- Scheduler
- Logs
- About Pulse

The header carries the page title, slogan, version name, and Build ID. A compact activity strip holds current-session feedback plus **Refresh Pulse** and **Exit**.

## Page placement

- **Dashboard** uses user-facing health state language, a minimal live indicator, Current System State, Top Risk, Recent Changes, Recommendations, and System Trend.
- **Linux Assessment** owns the Debian-family boundary, Run Assessment, evidence cards, provenance, and guidance.
- **Reports** is HTML-first, opens recent reports, and explains browser Print / Save as PDF as the archived-PDF path.
- **Scheduler** owns the Piece 5 systemd user schedule and its confirmation boundary.
- **Logs** shows recent activity and provides Refresh, Open Logs Folder, and confirmed Clear Event Log actions.
- **About Pulse** is limited to version and stability presentation.

## Health presentation

Application UI remains status-first and uses the Pulse states Optimized, Healthy, Attention Recommended, Degraded, and Critical. Rich numeric gauge presentation is reserved for HTML reports.

Until the shared Pulse Core weighting model is imported, the Linux Beta report score is a documented preview interpretation of evidence states. Any attention evidence prevents an Optimized/Healthy presentation, and unavailable evidence reduces coverage. Provider data and snapshots remain unchanged for later Core recalculation.

## Visual verification gate

GitHub Actions now captures `pulse-standard-shell.png` after the Avalonia launch-survival check and includes it with the executable artifacts. No further Linux intelligence work begins until the physical Linux screenshot and page-by-page layout are reviewed against the Pulse Standard.
