# Pulse Supernova Linux — Beta 0.0.0.29

Dedicated Debian-family port of Pulse Supernova, continuing from the macOS Preview 0.52.0 engineering foundation.

- GitHub release version: `0.0.0.29`
- GitHub release title/comment: `Pulse Linux Beta 0.0.0.29`

## Product boundary

- Supported first: Debian, Ubuntu, and Linux Mint desktop editions.
- Compatible derivatives are unsupported until they are deliberately verified and added to the compatibility matrix.
- Excluded: Fedora/RHEL, Arch, BSD, and unrelated distributions.
- Detection is based on `/etc/os-release`; `ID_LIKE=debian` alone never grants supported status.
- Phase 1 system discovery is read-only. Pulse writes only user-owned reports and explicitly approved user-schedule files; it never invokes `sudo`, Polkit, or a privileged helper.

## Current milestone

Linux Phase 1 establishes:

1. The platform boundary and support gate.
2. Linux provider contracts and first native evidence providers.
3. A unified Avalonia Pulse shell.
4. An x64-first `.deb` and portable `.tar.gz` workflow.
5. A compatibility matrix and repeatable physical-test record.

The shell targets .NET 10 and Avalonia 12.1.0 provisionally. Before the first release build, align the Avalonia package version and shared project references with the macOS 0.52.0 source bundle.

## Build on Debian/Ubuntu

```bash
dotnet restore src/Pulse.Platform.Linux/Pulse.Platform.Linux.csproj
dotnet run --project src/Pulse.Platform.Linux/Pulse.Platform.Linux.csproj
```

Build test packages:

```bash
dotnet restore src/Pulse.Platform.Linux/Pulse.Platform.Linux.csproj --runtime linux-x64
./packaging/build-linux.sh linux-x64 0.0.0.29
```

Outputs are written beneath `artifacts/`. Build `linux-arm64` only after the x64 acceptance gate passes.

## Conventional locations

| Purpose | Location |
| --- | --- |
| Installed application | `/opt/pulse-platform` |
| User settings | `~/.config/Pulse Platform` |
| User data and reports | `~/.local/share/Pulse Platform` |
| Desktop launcher | `/usr/share/applications/pulse-platform.desktop` |
| Icon | `/usr/share/icons/hicolor/scalable/apps/pulse-platform.svg` |
| Optional user schedule | `~/.config/systemd/user/` |

No systemd user unit is installed or enabled automatically. Piece 5 creates the two Pulse user units only after an explicit two-step confirmation in the application.

## Build without a local Linux SDK

The included GitHub Actions workflow compiles and packages the project on an Ubuntu runner:

1. Push this project to the GitHub repository.
2. Open **Actions** and choose **Pulse Linux x64 Build**.
3. A push to `main` builds version `0.0.0.29` automatically; **Run workflow** remains available for an explicit rebuild.
4. After the run succeeds, download **pulse-linux-beta-0.0.0.29-linux-x64** from the run's **Artifacts** section. Every successful `main` build also publishes the verified packages as GitHub prerelease assets for the in-app updater.

## Piece 3 intelligence

The first functional intelligence layer now evaluates:

- Root filesystem capacity
- dpkg package-database consistency
- APT's cached list of available upgrades without refreshing repositories
- AppArmor kernel posture
- UFW/nftables service indicators
- Standard unattended-upgrades configuration
- Detectable LUKS block-device layers

Each provider is isolated. Missing tools, unreadable optional evidence, or a provider error are reported as unavailable without stopping the assessment.

## Piece 4 reporting and history

Each completed assessment now creates a structured JSON snapshot, a branded HTML report, and a compact activity-log entry beneath `~/.local/share/Pulse Platform`. Writes are user-level and require no elevation. The shell can open the latest report and rediscovers it after Pulse restarts.

See `docs/piece4-reporting.md` for the storage contract and reporting smoke tests.

## Piece 5 user-approved scheduling

Pulse can optionally run the same read-only assessment each week through `systemd --user`. The schedule is disabled by default, requires a second confirmation click, uses the current user's permissions, and can be disabled from the same control. The `--assess-once` execution path produces reports without opening the GUI.

See `docs/piece5-user-scheduling.md` for the unit contract and physical validation commands.

## Piece 6 Pulse Standard alignment

The temporary Linux engineering window has been replaced by the Aurora application shell. Working Linux features now live in dedicated Dashboard, Linux Assessment, domain intelligence, Reports, Scheduler, Logs, and Mission Control pages under the established Pulse Health Platform and Pulse Administration navigation groups. The official dark Pulse logo, Pulse Electric palette, status-first health language, compact activity area, and HTML executive gauge are incorporated.

Feature expansion is frozen until the generated and physical Linux screenshots pass the Pulse Standard review. See `docs/piece6-pulse-standard-alignment.md`.

Beta 0.0.0.7 corrects the 0.0.0.6 X11 startup failure by using a real PNG for the Avalonia window icon. The SVG remains the conventional desktop-menu icon. The workflow now installs the generated `.deb`, requires a visible window, rejects fatal output and blank captures, and then publishes the screenshot.

## Piece 7 connectivity and reliability intelligence

After physical approval of the corrected Pulse Standard shell, Beta 0.0.0.8 resumes provider work with local network posture and privacy-conscious current-boot journal intelligence. It does not restore the unreliable active internet test. The Dashboard now reports actual evidence-state transitions between assessments.

See `docs/piece7-connectivity-reliability.md` for the evidence and privacy contracts.

## Piece 9 storage intelligence and Mission Control

Beta 0.0.0.10 supersedes the failed 0.0.0.9 build, adopts the visible **Pulse Supernova Linux** identity, restores the complete Supernova Mission Control page, adds a dedicated Storage Intelligence page, and expands the default assessment to fourteen providers with optional SMART/NVMe drive-health and detectable backup posture. Existing package IDs, installed paths, report history, and schedule unit IDs remain stable.

See `docs/piece9-storage-mission-control.md` for the safety and identity contracts.

## Package Intelligence

Beta 0.0.0.13 adds the first complete Debian-native domain page: Package Intelligence. Six cards cover dpkg consistency, installed inventory, cached available updates, cached security updates, automatic security-update configuration, and restart requirement. The Package Intelligence Dashboard score is computed from those same six sources.

See `docs/package-intelligence.md` for the evidence, safety, and physical-validation contract.

## Storage Intelligence

Beta 0.0.0.16 presents Storage Intelligence consistently in the navigation tree and its dedicated page. Its six cards cover root capacity, root filesystem mount integrity, inode capacity, standby-safe physical-drive health, LUKS posture, and detectable backup posture. Current SMART/NVMe failures request attention; historical diagnostic records are identified as informational evidence and do not by themselves lower the current-health result.

See `docs/storage-intelligence.md` for the interpretation, safety, and physical-validation contract.

## Security Intelligence

Beta 0.0.0.17 adds a dedicated six-card Security Intelligence page covering AppArmor, firewall service posture, cached security updates, automatic security updates, LUKS visibility, and Secure Boot. The page uses the same evidence-backed score shown on Dashboard and distinguishes unavailable coverage from a directly observed disabled control.

See `docs/security-intelligence.md` for the interpretation, safety, and physical-validation contract.

## Network Intelligence

Beta 0.0.0.20 adds a dedicated six-card Network Intelligence page covering active interfaces, IPv4/IPv6 default routes, NetworkManager state, DNS configuration, listening-service counts, and firewall service indicators. The Dashboard score uses those same six sources. Collection is local and read-only: Pulse sends no ping, DNS query, speed test, or public reachability request and retains no listening endpoint, port, process, payload, or resolver-address details.

See `docs/network-intelligence.md` for the interpretation, privacy, review-action, and physical-validation contract.

Beta 0.0.0.21 adds a reversible **Firewall Is Off by Choice** acknowledgment. Pulse saves only the user's intent in its conventional settings folder, keeps reporting the detected inactive service posture, and stops requesting review for that one accepted condition. **Restore Firewall Review** removes the exception; neither action changes firewall configuration.

## Reliability Intelligence

Beta 0.0.0.24 adds a dedicated six-card Reliability Intelligence page covering current-boot journal metadata, failed system services, failed signed-in-user services, systemd boot timing, Linux uptime, and Debian's restart-required marker. The Dashboard score uses the same six sources. Pulse can open an installed graphical log viewer for relevant findings, but it never copies journal message bodies, changes service state, resets failures, or reboots the computer.

## Performance Intelligence

Beta 0.0.0.25 adds a dedicated six-card Performance Intelligence page covering sustained load, available memory, CPU pressure, memory pressure, I/O pressure, and the hottest readable Linux thermal zone. The Dashboard score uses those same six providers. Pulse can open an installed system monitor for guided review, but it never ends processes, clears caches, changes priorities, alters power policy, or controls cooling.

## Linux Assessment navigation

Beta 0.0.0.26 replaces the long mixed evidence list with three large choices: **Information** for system facts and context, **Healthy** for checks that passed, and **Guidance** for review items, unavailable coverage, and safe next steps. Each choice opens a dedicated card page with a clear return to the overview. All 34 providers remain in the assessment, reports, history, and Dashboard scoring.

## Updater publication reliability

Beta 0.0.0.27 closes the gap between temporary Actions artifacts and updater-visible GitHub Releases. A successful build on `main` now creates or refreshes the matching Linux prerelease automatically and fails if the `.deb`, portable archive, or `SHA256SUMS` asset is missing. Pulse also explains when the installed development build is newer than GitHub's newest compatible published package and never offers a downgrade.

## Hardware Intelligence and Performance coverage

Beta 0.0.0.28 adds a dedicated six-card Hardware Intelligence page and matching Dashboard domain for processor, physical memory, firmware/system identity, battery condition, graphics hardware, and virtualization posture. It expands the assessment to 40 providers while keeping all discovery local and read-only.

Performance Intelligence now reads PSI from `/proc/pressure` or cgroup v2, explains kernels where PSI is compiled but disabled by default, and treats missing optional evidence as incomplete coverage rather than negative health. Unavailable items stay visible, but only actual review findings deduct health points.

Beta 0.0.0.29 supersedes the failed 0.0.0.28 build and corrects the Hardware Intelligence battery-capacity nullable value declaration required by the C# compiler. No accepted v28 feature or safety boundary was removed.

See `docs/hardware-intelligence.md` and `docs/performance-intelligence.md` for the interpretation and safety boundaries.

## Guided review actions

Beta 0.0.0.18 begins the shared Pulse review-action framework. Package, Storage, and Security Intelligence now provide a contextual button beside the recommended next step. Pulse opens an installed Software Updater or GNOME Disks when that is a safe match; otherwise it takes the user to detailed in-app evidence and guidance. It never uses these buttons to elevate, repair, install, or change policy automatically.

See `docs/review-actions.md` for the initial routing and safety contract.

## Product updates

Beta 0.0.0.19 adds a dedicated Updates page. Pulse checks the public GitHub releases only after the user presses **Check for Updates**, selects the exact Debian architecture, downloads to `~/Downloads`, and verifies the package against the published SHA-256 checksum. **Open Installer** uses the normal graphical package installer, which retains all confirmation and authentication.

See `docs/product-updates.md` for the updater and release-publishing contract.

## Supernova Dashboard standard

Beta 0.0.0.12 rebuilds Dashboard around the supplied Pulse Supernova reference: six evidence-backed Linux domain rows, Executive Health gauge, Top Risk, Recent Changes, Supernova Advisor, action state, and a plotted assessment trend. Linux does not display fabricated Messaging, Office, or Windows scores. It also corrects the invalid three-value Avalonia margin that prevented 0.0.0.11 from compiling and creates the portable archive from the stable staged application copy.

See `docs/supernova-dashboard-standard.md`.

See `docs/github-build-workflow.md` for the expected contents and failure checks.
