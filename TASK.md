# Pulse Supernova Linux — Development Task

## Task identity

- Continuation point: Pulse macOS Preview 0.52.0
- macOS status: on hold for Apple Developer Program signing/notarization and deferred physical validation
- Linux status: active, Phase 1
- Linux version: `0.0.0.16`
- GitHub upload/commit comment: `Pulse Linux Beta 0.0.0.16`
- Distribution policy: Debian-family desktop only
- Architecture order: `linux-x64`, then `linux-arm64`
- Release transport: GitHub Releases, `.tar.gz`, or `.deb`

## Non-negotiable rules

- Preserve unified Pulse product identity and UX.
- Reuse shared .NET 10 Pulse Core and Avalonia design once the macOS source bundle is present.
- Keep Linux intelligence in Linux-native providers behind shared contracts.
- Discover, translate into plain language, and advise before offering actions.
- Phase 1 performs read-only discovery only.
- Future elevated actions must be minimal, explicit, user-approved, and implemented through a narrow Polkit/helper boundary.
- Never treat `ID_LIKE=debian` as automatic support.

## Phase 1 workboard

| Work item | Status | Acceptance condition |
| --- | --- | --- |
| Platform boundary | Implemented | Debian/Ubuntu/Linux Mint accepted; excluded and unverified systems clearly refused |
| Provider foundation | Implemented | Providers do not depend on Avalonia and return plain-language evidence |
| Compatibility matrix | Drafted | x64 target versions and verification states are explicit |
| Packaging workflow | Drafted | Repeatable self-contained `.tar.gz` and `.deb` build script exists |
| GitHub x64 build pipeline | Validated | Piece 2 compile, smoke test, GUI launch test, packaging, checksum, and artifact upload passed |
| Piece 2 physical/build validation | Passed | User confirmed the core/framework function; GitHub x64 workflow passed |
| Piece 3 intelligence layer | Physically validated | User installed Piece 3 on Linux and confirmed expected operation |
| Piece 4 history/reporting | Physically validated | User confirmed report creation and opening through the existing browser |
| Piece 5 user scheduling | Implemented | Headless assessment and confirmed weekly systemd user timer work without elevation |
| Piece 6 Pulse Standard alignment | Physically approved in 0.0.0.7 | User confirmed the corrected shell looks substantially like Pulse Standard |
| Piece 7 connectivity/reliability | Implemented | Local-only network posture and privacy-conscious journal metadata providers |
| Piece 9 storage/Mission Control | Refined in 0.0.0.16, awaiting physical validation | Supernova identity, Mission Control, Storage Intelligence, current-vs-historical drive health, and backup posture |
| Package Intelligence | Physically validated in 0.0.0.13 | User confirmed the dedicated page and Package Dashboard intelligence function correctly |
| Unified shell | Rebuilt to Pulse Standard | Dashboard, domain intelligence, Reports, Scheduler, Logs, and Mission Control replace the engineering scroll shell |
| Shared Core/macOS merge | Blocked | macOS 0.52.0 source bundle or repository checkout supplied |
| linux-x64 build | Passed through 0.0.0.7 | Restore, compile, installed-package window/render validation, packaging, and launch succeeded |
| Debian physical test | Pending | Screenshot, logs, package install/remove, and evidence results recorded |
| Ubuntu physical test | Pending | Same acceptance record completed |
| Linux Mint physical test | Pending | Same acceptance record completed |
| linux-arm64 | Deferred | Starts only after x64 acceptance gate |

## Change log

### 2026-08-02 — Phase 1 task created

- Fixed the support boundary to verified Debian-family desktops.
- Added strict `/etc/os-release` classification.
- Added provider and evidence result contracts.
- Added initial OS, kernel, CPU/memory, and systemd evidence providers.
- Added a branded read-only Avalonia shell.
- Added x64-first portable and Debian package workflow.
- Recorded the missing macOS/shared-core source as the only merge blocker.

### 2026-08-02 — Release identity fixed

- Set the official initial Linux version to `0.0.0.1`.
- Set the GitHub release title to `Pulse Linux Beta 0.0.0.1`.
- Updated project, shell, documentation, and packaging defaults to use the official version.

### 2026-08-02 — Beta 0.0.0.2 build pipeline

- Added the GitHub Actions `linux-x64` build-and-package workflow.
- Added dependency-free distribution-boundary smoke tests.
- Added a virtual-display GUI launch check.
- Added `.deb` inspection, archive integrity checks, and checksum verification.
- Added a GitHub workflow handoff describing how to download the first executable artifacts.

### 2026-08-03 — Beta 0.0.0.3 Linux intelligence foundation

- Recorded Piece 2 core/framework and GitHub workflow validation as successful.
- Added a no-shell, no-elevation command runner with cancellation and timeout controls.
- Added storage, dpkg audit, cached APT update, AppArmor, firewall indicator, unattended-upgrades, and LUKS providers.
- Added explicit evidence states, provenance, plain-language guidance, and per-provider fault isolation.
- Expanded smoke tests to prove a failed provider cannot abort an assessment.

### 2026-08-03 — Beta 0.0.0.4 assessment history and reporting

- Recorded successful installation and expected operation of Piece 3 on Linux.
- Added atomic, timestamped JSON assessment snapshots under the conventional user-data location.
- Added branded, HTML-encoded reports with state totals, evidence sources, and plain-language guidance.
- Added a compact JSON Lines activity log and latest-report rediscovery across application launches.
- Added an explicit **Open Latest Report** action that uses the desktop's default browser without elevation.
- Added smoke tests for persistence, JSON readability, HTML escaping, activity logging, and latest-report discovery.

### 2026-08-03 — Beta 0.0.0.5 user-approved weekly assessments

- Recorded successful Piece 4 installation, assessment persistence, and HTML report opening on Linux.
- Added `--assess-once` so the existing support gate, ten providers, archive, report, and activity log can run without Avalonia or a display.
- Added opt-in weekly `systemd --user` service/timer generation with persistent catch-up and randomized delay.
- Required an explanatory first click and explicit confirmation click before enabling the timer.
- Added in-app schedule status and disable/removal for Pulse's two user units.
- Added fixed-argument, no-shell `systemctl --user` execution without sudo, Polkit, or system-wide writes.
- Added smoke tests for unit contents, enable/status/disable behavior, and the no-elevation boundary.
- Added a GitHub headless-run gate that requires a generated JSON snapshot and HTML report.

### 2026-08-03 — Beta 0.0.0.6 Pulse Standard alignment

- Froze new Linux intelligence work after the user identified increasing UX drift from Pulse Standard.
- Reclassified Pieces 1–5 as the Linux engine/platform foundation rather than the completed unified product shell.
- Grounded the rebuild in the Aurora shell source, PulseColors, Pulse 7.5.4–7.5.5.1 history, executive-gauge mockup, and official dark logo.
- Replaced the single scrolling engineering window with Pulse Health Platform and Pulse Administration navigation groups.
- Added dedicated Dashboard, Linux Assessment, Reports, Scheduler, Logs, and About Pulse pages.
- Moved Piece 5 scheduling into Scheduler and reporting/history into Reports and Logs.
- Restored status-first health language, compact activity feedback, version name/Build ID header placement, and version/stability-only About presentation.
- Added the report-only Pulse Executive Gauge and kept the live Dashboard indicator minimal.
- Added GitHub capture of `pulse-standard-shell.png` as a mandatory visual review artifact.

### 2026-08-03 — Beta 0.0.0.7 X11 startup correction

- Recorded the physical Linux fatal trace: Avalonia's X11 window-icon loader could not decode the SVG declared as the window icon.
- Replaced only the Avalonia window icon with a verified 256×256 PNG; retained the SVG for desktop launcher integration and the official dark logo for the application shell.
- Replaced the process-survival-only workflow gate with generated `.deb` installation, visible-window detection, fatal-output rejection, and non-blank capture validation.
- Superseded 0.0.0.6; it must not be promoted or reinstalled.

### 2026-08-03 — Beta 0.0.0.8 Piece 7 Nebula intelligence

- Recorded physical approval that the corrected Aurora Linux shell now looks substantially like Pulse Standard.
- Resumed intelligence work without changing the approved navigation or page structure.
- Added local network posture from active `ip` interfaces, IPv4/IPv6 default routes, and existing NetworkManager state when available.
- Explicitly prohibited ping, public reachability, speed, DNS, and repository probes; the unreliable Windows-style Internet test is not restored.
- Added current-boot `journalctl` reliability counts and source identifiers while excluding journal message bodies from Pulse evidence and reports.
- Added real provider-state comparison to the Dashboard Recent Changes card.
- Expanded smoke tests to cover the two providers, privacy boundary, unique IDs, and twelve-provider default assessment.

### 2026-08-03 — Beta 0.0.0.9 Piece 9 storage and Supernova identity

- Adopted **Pulse Supernova Linux** as the visible Linux application name while preserving package IDs and conventional installed/data paths.
- Standardized the upload/commit comment to the version-only format `Pulse Linux Beta 0.0.0.9`.
- Centralized runtime product/version/Build ID presentation in `AppInfo.cs`.
- Restored Supernova Mission Control from the supplied reference screenshots with mission, product purpose, developer information, system information, paths, and launch identity.
- Added a dedicated Storage Intelligence page using root capacity, drive health, LUKS, and backup evidence.
- Added standby-safe SMART/NVMe health collection without self-tests, raw-output retention, elevation, or device changes.
- Added informational backup posture for seven known Linux backup families without claiming a recent or recoverable backup.
- Expanded the default assessment from twelve to fourteen providers.

### 2026-08-03 — Beta 0.0.0.10 compile correction

- Recorded the GitHub compile failure in the smoke-test project: `AppInfo` was referenced without importing the root `Pulse.Platform.Linux` namespace.
- Added the missing namespace import and advanced the correction to 0.0.0.10 so it cannot be confused with the failed 0.0.0.9 upload.

### 2026-08-03 — Beta 0.0.0.11 Supernova Dashboard standard

- Recorded user validation that 0.0.0.10 looks substantially better while Dashboard still differs from the supplied Supernova standard.
- Rebuilt Current System State with six evidence-backed Linux intelligence domains, status language, bars, and scores.
- Added the in-app Executive Health score and four-zone gauge requested by the supplied reference.
- Restored the Top Risk, Recent Changes, Supernova Advisor, and Required Action hierarchy.
- Added an actual multi-assessment trend plot rather than a text-only trend statement.
- Explicitly avoided fabricated Messaging, Office, Windows, or unsupported performance scores.

### 2026-08-03 — Beta 0.0.0.12 compile correction

- Recorded the 0.0.0.11 GitHub failure at Compile release shell: Avalonia `AVLN2005` rejected `Margin="0,8,0"` as an invalid `Thickness`.
- Corrected the Supernova Advisor margin to the valid four-value form `Margin="0,8,0,0"` and advanced the version so the corrected build is unambiguous.
- Rehearsed the full package build and corrected the portable archive source to use the immutable staged application copy, preventing a later `tar: file changed as we read it` failure.

### 2026-08-03 — Beta 0.0.0.13 Package Intelligence

- Added a dedicated Package Intelligence page in the Pulse Supernova shell.
- Expanded Package Intelligence from three to six local, read-only evidence sources: dpkg consistency, installed inventory, cached upgrades, cached security updates, automatic security-update configuration, and restart requirement.
- Kept package discovery non-elevated and offline: no repository refresh, download, install, repair, distribution upgrade, or automatic restart.
- Added deterministic smoke coverage for installed-package counting, security-update classification, command safety, and Debian restart markers.
- Hardened portable packaging by enumerating staged top-level entries instead of archiving mutable `.` directory metadata.

### 2026-08-03 — Beta 0.0.0.14 Storage Intelligence

- Recorded user validation that Package Intelligence 0.0.0.13 functions correctly.
- Expanded Storage Intelligence from four to six evidence-backed cards.
- Added root filesystem source/type/mount-mode evidence through `findmnt` and flags a read-only root for review.
- Added root inode-capacity evidence through `df --inodes` with the same 85% attention threshold used for storage capacity.
- Preserved standby-safe SMART/NVMe queries, cautious LUKS language, and non-assertive backup detection.
- Expanded the Storage Dashboard score to the same six sources shown on the dedicated page.
- Made packaging consume the explicit runtime restore with `--no-restore`, eliminating a redundant package-source connection during publish.

### 2026-08-03 — Beta 0.0.0.15 runtime-assets correction

- Recorded the GitHub `NETSDK1047` packaging failure from a missing `net10.0/linux-x64` target.
- Identified the cause: the smoke-test `dotnet run` performed an implicit restore through its project reference and replaced the application's runtime-specific assets file.
- Declared `linux-x64` and future `linux-arm64` as application runtime identifiers.
- Restored both the application and smoke-test graph explicitly for `linux-x64`.
- Made compile, smoke-test, headless, and package stages use the same runtime and prohibit implicit restore.
- Replaced the higher-level publish command with the SDK's explicit no-restore MSBuild Publish target so packaging cannot initiate another restore.
- Disabled SDK workload-update notifications and Avalonia build telemetry in CI so validation does not introduce unrelated network activity.
- Replaced higher-level build/run steps with direct MSBuild targets and direct DLL execution, leaving the two named Restore commands as the only stages allowed to resolve packages.

### 2026-08-03 — Beta 0.0.0.16 Storage diagnostic refinement

- Renamed the navigation-tree item from **Storage** to **Storage Intelligence** so the domain name remains consistent with Dashboard and the dedicated page.
- Recorded the physical-test report that Pulse showed a drive issue which was not reported elsewhere in the operating system.
- Corrected SMART exit-status interpretation so only a current overall-health failure or active pre-failure threshold requests attention.
- Classified historical SMART attribute/error/self-test records and lifetime NVMe media-error counts as informational evidence rather than proof of a current drive failure.
- Preserved attention for active SMART failure and current NVMe critical-warning, low-spare, and high-wear conditions.
- Added deterministic regression tests for both historical-only and active-failure results.

## Next engineering checkpoint

1. Push build 0.0.0.16 using the comment `Pulse Linux Beta 0.0.0.16` and require every installed-package/render gate to pass.
2. Confirm the navigation tree, page title, and Dashboard use **Storage Intelligence** consistently.
3. Run a new assessment and compare the Physical Drive Health card with the operating system and, when available, the drive manufacturer's diagnostic tool.
4. Confirm historical SMART/NVMe records are informational and do not claim a current failure or lower the current-health result.
5. Confirm an active SMART/NVMe failure still requests attention and recommends a verified backup before diagnostics.
