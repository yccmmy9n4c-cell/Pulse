# Pulse Linux Beta 0.0.0.2

Dedicated Debian-family port of Pulse Platform, continuing from the macOS Preview 0.52.0 engineering foundation.

- GitHub release version: `0.0.0.2`
- GitHub release title: `Pulse Linux Beta 0.0.0.2`

## Product boundary

- Supported first: Debian, Ubuntu, and Linux Mint desktop editions.
- Compatible derivatives are unsupported until they are deliberately verified and added to the compatibility matrix.
- Excluded: Fedora/RHEL, Arch, BSD, and unrelated distributions.
- Detection is based on `/etc/os-release`; `ID_LIKE=debian` alone never grants supported status.
- Phase 1 is read-only. Pulse never invokes `sudo`, Polkit, or a privileged helper.

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
./packaging/build-linux.sh linux-x64 0.0.0.2
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

No systemd user unit is installed or enabled automatically in Phase 1.

## Build without a local Linux SDK

The included GitHub Actions workflow compiles and packages the project on an Ubuntu runner:

1. Push this project to the GitHub repository.
2. Open **Actions** and choose **Pulse Linux x64 Build**.
3. Select **Run workflow** and keep version `0.0.0.2`.
4. After the run succeeds, download **pulse-linux-beta-0.0.0.2-linux-x64** from the run's **Artifacts** section.

See `docs/github-build-workflow.md` for the expected contents and failure checks.
