# Pulse Supernova Linux — Development Task

## Task identity

- Continuation point: Pulse macOS Preview 0.52.0
- macOS status: on hold for Apple Developer Program signing/notarization and deferred physical validation
- Linux status: active, Phase 1
- Linux version: `0.0.0.28`
- GitHub upload/commit comment: `Pulse Linux Beta 0.0.0.28`
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
| Piece 9 storage/Mission Control | Physically validated through 0.0.0.18 | Supernova identity, Mission Control, Storage Intelligence, current-vs-historical drive health, and backup posture |
| Package Intelligence | Physically validated in 0.0.0.13 | User confirmed the dedicated page and Package Dashboard intelligence function correctly |
| Security Intelligence | Physically validated in 0.0.0.18 | Dedicated six-card page, Dashboard parity, Secure Boot posture, and read-only safety boundary |
| Guided review actions | Physically validated in 0.0.0.18 | Domain recommendation opens a safe native tool when available or detailed in-app evidence otherwise |
| Product updater | Publication correction implemented in 0.0.0.27 | Successful main builds automatically publish the exact-architecture package, portable archive, and basename-only checksum; installed-newer state is explicit and never offers a downgrade |
| Network Intelligence | Physically validated in 0.0.0.20 | Dedicated six-card page, Dashboard parity, local-only evidence, privacy boundary, and guided network-settings review |
| Firewall intent acknowledgment | Carried into 0.0.0.26, awaiting physical validation | Reversible user preference suppresses review only for directly observed, intentionally inactive UFW/nftables service posture |
| Reliability Intelligence | Physically validated in 0.0.0.24 | Dedicated six-card page, Dashboard parity, metadata-only journal evidence, system/user service separation, and guided log review |
| Performance Intelligence | Scoring/PSI correction implemented in 0.0.0.28 | Dedicated six-card page, `/proc` and cgroup v2 PSI fallback, explicit default-disabled explanation, coverage-neutral scoring, and guided system-monitor review |
| Hardware Intelligence | Implemented in 0.0.0.28, awaiting physical validation | Six-card processor, memory, firmware, battery, graphics, and virtualization page with Dashboard parity and read-only safety boundary |
| Linux Assessment navigation | Physically validated in 0.0.0.26 | User confirmed the three large overview choices make important information substantially easier to find |
| Unified shell | Rebuilt to Pulse Standard | Dashboard, domain intelligence, Reports, Scheduler, Logs, and Mission Control replace the engineering scroll shell |
| Shared Core/macOS merge | Blocked | macOS 0.52.0 source bundle or repository checkout supplied |
| linux-x64 build | Passed through 0.0.0.24 | Restore, compile, installed-package window/render validation, packaging, checksum publication, updater, and launch succeeded |
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

### 2026-08-03 — Beta 0.0.0.17 Security Intelligence

- Added **Security Intelligence** to the Pulse Health Platform navigation tree and created a dedicated six-card Supernova page.
- Unified AppArmor, firewall service posture, cached security updates, automatic security updates, LUKS visibility, and Secure Boot into one evidence-backed domain.
- Added a read-only Secure Boot provider that reads the UEFI efivar state when visible and distinguishes missing coverage from firmware-reported disabled state.
- Expanded the Dashboard Security Intelligence score to use the same six sources displayed on the dedicated page.
- Kept the security boundary non-elevated and non-mutating: no firewall, AppArmor, package, encryption, firmware, or boot-policy changes.
- Added deterministic Secure Boot enabled/disabled regression coverage and expanded the default assessment to twenty providers.

### 2026-08-12 — Beta 0.0.0.18 drive permission correction and guided review foundation

- Recorded physical validation that the NVMe reports SMART overall health passed, zero critical warnings, zero media/data-integrity errors, and completed self-tests without error.
- Reproduced Pulse's unprivileged command result: `smartctl open device ... failed: Permission denied` with exit code 2.
- Corrected the parser so query/open failures are classified before textual health interpretation and generic uses of `failed` cannot become drive-health failures.
- Restricted textual failure detection to explicit SMART overall-health result fields while preserving current-failure exit bits and historical status handling.
- Made permission-limited drive evidence informational incomplete coverage with a visible reason and no Storage score penalty.
- Reclassified disabled AppArmor, Secure Boot, and unattended-upgrades automation as informational hardening choices rather than current system errors, preventing optional configuration from degrading Executive Health.
- Added contextual **Open Software Updater**, **Open Disk Utility**, and **Review Details** actions to Package, Storage, and Security recommendations.
- Added a safe fallback to the Linux Assessment evidence when the relevant graphical utility is not installed.
- Added a GitHub source-baseline gate that rejects a requested release version when the repository root contains an older `Directory.Build.props` version.
- Recovered the 0.0.0.17 baseline from the maintained Library artifact after the active local source folder was discovered at 0.0.0.11; the stale folder was left untouched.

### 2026-08-12 — Beta 0.0.0.19 product updater

- Recorded physical validation of 0.0.0.18: after the remaining package updates, Executive Health and all six Dashboard domain scores reached 100.
- Recorded that **Open Software Updater** functions correctly; an already-running Linux utility may reuse its existing window without bringing it to the foreground, while a clean start opens normally.
- Added a dedicated Pulse Standard **Updates** page with installed version, architecture, available release, release notes, download progress, and explicit installer handoff.
- Kept all network activity user-requested: Pulse performs no startup or background update check.
- Added GitHub public-release discovery that ignores drafts and unrelated products and requires an exact architecture-specific `.deb` plus `SHA256SUMS`.
- Added download-to-`~/Downloads`, SHA-256 verification, partial-file cleanup, and mismatch rejection before **Open Installer** can be enabled.
- Preserved the authority boundary: Pulse never silently elevates or installs; the desktop's graphical package installer owns confirmation and authentication.
- Extended the manual GitHub Actions run on `main` to publish the verified `.deb`, portable `.tar.gz`, and checksum file as a Beta prerelease after all existing gates pass.
- Added deterministic smoke coverage for release selection, Beta prereleases, Windows-release exclusion, successful verification, and checksum mismatch rejection.

### 2026-08-14 — Beta 0.0.0.20 Network Intelligence

- Recorded user confirmation that Pulse Linux Beta 0.0.0.19 functions as expected, completing physical validation of the first updater baseline.
- Added **Network Intelligence** to the Pulse Health Platform navigation and created a dedicated six-card Supernova page.
- Split the earlier combined provider into separate active-interface, IPv4/IPv6 default-route, and NetworkManager evidence so Pulse can explain exactly what needs review.
- Added direct `/etc/resolv.conf` posture that counts resolver entries and recognizes local-stub use without sending a DNS query or retaining resolver addresses.
- Added optional `ss -H -lntu` listening-service posture that reports only aggregate listener and all-address counts, never endpoints, port numbers, process names, or payloads.
- Expanded the Dashboard Network Intelligence score to use the same six sources shown on the dedicated page, including the existing firewall indicator.
- Treated missing optional NetworkManager and socket tooling as informational coverage limits rather than network failures.
- Added **Open Network Settings** for installed NetworkManager, GNOME, or KDE graphical settings with an in-app evidence fallback and no elevation or automatic changes.
- Added deterministic regression coverage for provider separation, local DNS privacy, listening-socket privacy, prohibited active network/DNS commands, and the 24-provider assessment inventory.

### 2026-08-14 — Beta 0.0.0.21 intentional firewall posture

- Recorded user confirmation that Pulse Linux Beta 0.0.0.20 functions as expected, completing physical validation of the dedicated Network Intelligence milestone.
- Added **Firewall Is Off by Choice** to the Network Intelligence firewall card only when assessment evidence finds no active UFW or nftables service indicator.
- Persisted the explicit choice and its timestamp in the conventional user settings file at `~/.config/Pulse Platform/settings.json`.
- Preserved the detected evidence while converting only that exact inactive-service posture to an accepted healthy state so it cannot become the recommended review item.
- Applied the preference consistently during interactive, headless, scheduled, Dashboard, Network Intelligence, Security Intelligence, and report flows.
- Added **Restore Firewall Review** to reconstruct the original informational evidence and cautious guidance without touching the system firewall.
- Prevented the preference from rewriting an active firewall result, an unavailable query, or any unrelated security evidence.
- Added atomic user-settings writes, safe defaults for missing/malformed settings, and deterministic persistence/policy/restoration regression tests.
- Defined the first consecutive in-app updater acceptance test: 0.0.0.20 discovers, verifies, and opens the 0.0.0.21 Debian package.

### 2026-08-14 — Beta 0.0.0.22 render-gate correction

- Recorded that 0.0.0.21 reached the visible-window capture but failed the silent first-frame image-deviation assertion in GitHub Actions.
- Preserved the complete intentional-firewall feature while superseding the unpublished 0.0.0.21 package identity.
- Added five delayed capture attempts after the X11 window is mapped, with a logged grayscale-deviation value for every attempt.
- Added explicit diagnostics for capture errors, empty images, and windows that remain incompletely rendered.
- Updated the in-app updater acceptance path so installed 0.0.0.20 discovers and opens 0.0.0.22.

### 2026-08-14 — Beta 0.0.0.23 updater checksum correction

- Confirmed that 0.0.0.22 update discovery worked but download verification safely stopped because `SHA256SUMS` contained absolute GitHub runner paths rather than release-asset basenames.
- Changed package generation so checksum entries contain only the exact `.deb` and `.tar.gz` asset filenames.
- Added a GitHub build assertion that rejects checksum manifests containing directory separators.
- Retained strict SHA-256 verification while allowing the updater to compare an exact package basename from legacy path-qualified checksum entries.
- Added deterministic regression coverage for the observed failure and recorded that no package was installed during the failed attempt.

### 2026-08-14 — Beta 0.0.0.24 Reliability Intelligence

- Recorded user confirmation that 0.0.0.23 functions correctly, physically validating the corrected updater checksum and graphical installer handoff.
- Added a dedicated six-card **Reliability Intelligence** page and navigation entry using the established Pulse Supernova Linux shell.
- Separated current-boot journal metadata, failed system services, failed user services, boot timing, uptime, and restart-required posture.
- Replaced the Dashboard's mixed three-source Reliability score with exact parity to the six dedicated-page sources.
- Added safe **Open System Logs** guidance for journal and failed-service findings with an in-app fallback.
- Restricted journal evidence to aggregate severity/source metadata and failed-service evidence to unit names; descriptions and journal message bodies are not retained.
- Added regression coverage proving system/user separation, boot/uptime context, 28-provider inventory, and the prohibition on service or reboot changes.

### 2026-08-14 — Beta 0.0.0.25 Performance Intelligence

- Recorded user confirmation that 0.0.0.24 functions correctly, physically validating the Reliability Intelligence milestone.
- Added a dedicated six-card **Performance Intelligence** page and navigation entry using the established Pulse Supernova Linux shell.
- Added separate providers for load average, available memory, CPU PSI, memory PSI, I/O PSI, and thermal-zone posture.
- Added exact Dashboard parity and expanded the executive layout from six to seven Linux intelligence domains.
- Added safe **Open System Monitor** guidance for GNOME, MATE, and KDE with an in-app evidence fallback.
- Used conservative review thresholds and explicitly treated a single reading as context rather than proof of an application problem.
- Added deterministic threshold and provider-separation tests and expanded the default assessment inventory to 34 sources.

### 2026-08-14 — Beta 0.0.0.26 Linux Assessment refresh

- Replaced the difficult-to-navigate mixed evidence wall with large **Information**, **Healthy**, and **Guidance** button-cards.
- Added live evidence counts and dedicated filtered pages with a clear **Back to Assessment** action.
- Defined Information as informational context, Healthy as confirmed healthy evidence, and Guidance as Attention plus unavailable coverage.
- Ordered Guidance with review items before coverage limitations and displayed each item's safe next step directly on its card.
- Routed review-action fallbacks from intelligence pages to the matching Assessment section.
- Added `AssessmentEvidenceOrganizer` and deterministic coverage proving every evidence result is assigned exactly once with no loss or duplication.

### 2026-08-14 — Beta 0.0.0.27 updater publication reliability

- Recorded user acceptance of the 0.0.0.26 Linux Assessment navigation and preserved it unchanged.
- Confirmed through the live GitHub Releases API that 0.0.0.23 was the newest updater-visible Linux release even though 0.0.0.24–0.0.0.26 had passed Actions builds and were available as temporary artifacts.
- Changed successful `main` push builds to create or refresh their matching Linux prerelease automatically; manual workflow runs retain the same behavior and pull requests remain validation-only.
- Added a post-publication contract gate requiring the exact `.deb`, portable `.tar.gz`, and `SHA256SUMS` release assets.
- Expanded updater discovery from 30 to 100 releases and requested uncached GitHub metadata.
- Added an explicit installed-newer state so development builds newer than the latest published compatible package are explained clearly and never treated as an update or downgrade candidate.
- Added deterministic coverage reproducing the installed 0.0.0.27 versus published 0.0.0.23 scenario.

### 2026-08-14 — Beta 0.0.0.28 Hardware Intelligence and Performance correction

- Recorded the physical Performance Intelligence finding that CPU, memory, and I/O PSI were all unavailable because the tested kernel had `CONFIG_PSI=y` and `CONFIG_PSI_DEFAULT_DISABLED=y` without the `psi=1` boot setting.
- Corrected the health model so unavailable evidence remains a visible coverage limitation but does not subtract score; only actual Attention findings reduce health.
- Added `/proc/pressure` to cgroup v2 PSI fallback and plain-language distinctions for default-disabled PSI, explicit `psi=0`, kernels built without PSI, and otherwise absent interfaces.
- Added a dedicated six-card Hardware Intelligence page for processor identity, physical memory, DMI firmware/system identity, battery condition, DRM graphics context, and virtualization posture.
- Added Hardware Intelligence to the Dashboard as the eighth domain and expanded the default assessment from 34 to 40 isolated providers.
- Added a user-directed power-settings action for battery review while preserving the no-driver, no-firmware, no-bootloader, no-power-policy safety boundary.
- Added deterministic tests for cgroup PSI fallback, the observed default-disabled kernel, coverage-neutral scoring, hardware parsing, battery threshold separation, provider count, and provider-ID uniqueness.

## Next engineering checkpoint

1. Push build 0.0.0.28 using the comment `Pulse Linux Beta 0.0.0.28` and require every compile, 40-provider, updater, package, checksum, install, and GUI-render gate to pass.
2. Confirm the successful push automatically creates `linux-v0.0.0.28` with the `.deb`, `.tar.gz`, and `SHA256SUMS` updater assets.
3. Confirm Performance PSI cards populate after the user-enabled `psi=1` boot setting and that unavailable optional coverage never reduces a domain or executive score.
4. Confirm all six Hardware Intelligence cards populate appropriately on the physical laptop, including battery capacity, graphics adapter, and physical-versus-virtual context.
5. Confirm the eighth Dashboard domain fits the Pulse Standard layout and Hardware review opens installed power settings only when appropriate.
