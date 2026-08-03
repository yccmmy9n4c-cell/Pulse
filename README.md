# Pulse Linux Beta 0.0.0.8

Dedicated Debian-family port of Pulse Platform, continuing from the macOS Preview 0.52.0 engineering foundation.

- GitHub release version: `0.0.0.8`
- GitHub release title: `Pulse Linux Beta 0.0.0.8`

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
./packaging/build-linux.sh linux-x64 0.0.0.8
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
3. Select **Run workflow** and keep version `0.0.0.8`.
4. After the run succeeds, download **pulse-linux-beta-0.0.0.8-linux-x64** from the run's **Artifacts** section.

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

The temporary Linux engineering window has been replaced by the Aurora application shell. Working Linux features now live in dedicated Dashboard, Linux Assessment, Reports, Scheduler, Logs, and About Pulse pages under the established Pulse Health Platform and Pulse Administration navigation groups. The official dark Pulse logo, Pulse Electric palette, status-first health language, compact activity area, and HTML executive gauge are incorporated.

Feature expansion is frozen until the generated and physical Linux screenshots pass the Pulse Standard review. See `docs/piece6-pulse-standard-alignment.md`.

Beta 0.0.0.7 corrects the 0.0.0.6 X11 startup failure by using a real PNG for the Avalonia window icon. The SVG remains the conventional desktop-menu icon. The workflow now installs the generated `.deb`, requires a visible window, rejects fatal output and blank captures, and then publishes the screenshot.

## Piece 7 connectivity and reliability intelligence

After physical approval of the corrected Pulse Standard shell, Beta 0.0.0.8 resumes provider work with local network posture and privacy-conscious current-boot journal intelligence. It does not restore the unreliable active internet test. The Dashboard now reports actual evidence-state transitions between assessments.

See `docs/piece7-connectivity-reliability.md` for the evidence and privacy contracts.

See `docs/github-build-workflow.md` for the expected contents and failure checks.
