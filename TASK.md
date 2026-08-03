# Pulse Platform Linux — Development Task

## Task identity

- Continuation point: Pulse macOS Preview 0.52.0
- macOS status: on hold for Apple Developer Program signing/notarization and deferred physical validation
- Linux status: active, Phase 1
- Linux version: `0.0.0.5`
- GitHub release title: `Pulse Linux Beta 0.0.0.5`
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
| Piece 5 user scheduling | Implemented, awaiting build | Headless assessment and confirmed weekly systemd user timer work without elevation |
| Unified shell | Implemented, uncompiled here | Avalonia shell loads support status and first read-only evidence |
| Shared Core/macOS merge | Blocked | macOS 0.52.0 source bundle or repository checkout supplied |
| linux-x64 build | Passed through Piece 4 | Restore, compile, launch, package, installation, assessment, and report opening succeeded |
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

## Next engineering checkpoint

1. Push Piece 5 to `main` and let the Linux x64 workflow compile, run the headless gate, and package it.
2. Upgrade the current Linux test computer with the resulting `.deb`.
3. Confirm the initial schedule status is disabled, then complete both enable-confirmation clicks.
4. Record `systemctl --user status pulse-platform-assessment.timer` and verify the timer survives a Pulse restart.
5. Run `/opt/pulse-platform/pulse-platform --assess-once` and open its newest report.
6. Disable the schedule in Pulse and confirm both Pulse user units are removed.
