# Pulse Supernova Linux — Fedora Edition 8.0.1.2FE

Pulse Linux FE carries the unified Pulse Standard interface and read-only intelligence foundation into a deliberately isolated Fedora-native edition.

## Identity and boundary

- Shared product version: `8.0.1.2`
- GitHub and updater identity: `8.0.1.2FE`
- GitHub tag: `linux-v8.0.1.2FE`
- GitHub Desktop commit summary: `Pulse Linux 8.0.1.2FE`
- Supported first: Fedora Workstation on `linux-x64`
- Detected but disabled pending verification: Fedora derivatives reporting `ID_LIKE=fedora`
- Explicitly outside FE: RHEL, Debian-family, Arch-family, BSD, and unrelated systems

Pulse reads `/etc/os-release` before enabling assessment, scheduling, or update selection. A related distribution is never silently treated as Fedora.

## Fedora-native intelligence

- RPM database verification and installed inventory
- Cache-only DNF available-update and security-advisory evidence
- `dnf-automatic.timer` posture
- SELinux enforcement posture
- firewalld/nftables service indicators
- Shared `/proc`, `/sys`, systemd, journal, NetworkManager, SMART/NVMe, LUKS, Secure Boot, storage, hardware, performance, startup, backup, and reliability evidence

All discovery is local and read-only. Pulse does not refresh DNF metadata, install packages, alter SELinux/firewall policy, enable timers, or elevate privileges.

## Build locally on Fedora

Install .NET 10 SDK, `rpm-build`, and the graphical runtime dependencies, then run:

```bash
dotnet restore src/Pulse.Platform.Linux/Pulse.Platform.Linux.csproj --runtime linux-x64
dotnet restore tests/Pulse.Platform.Linux.SmokeTests/Pulse.Platform.Linux.SmokeTests.csproj --runtime linux-x64
./packaging/build-fedora.sh linux-x64 8.0.1.2
```

Outputs:

- `artifacts/linux-x64/pulse-platform-8.0.1.2-1.x86_64.rpm`
- `artifacts/linux-x64/pulse-platform-8.0.1.2FE-linux-x64.tar.gz`
- `artifacts/linux-x64/SHA256SUMS`

## Publish without replacing DE

Keep the accepted DE source on `main`. Create a repository branch named `linux-fe`, place this bundle at that branch's repository root (including `.github`), and push with the summary `Pulse Linux 8.0.1.2FE`.

The `Pulse Linux FE x64 Build` workflow runs inside Fedora, compiles and smoke-tests the application, builds and installs the RPM, launches the installed GUI under Xvfb, verifies a non-blank screenshot and checksums, then creates the full public GitHub release.

Physical Fedora validation remains required after the workflow succeeds. See `docs/fedora-compatibility.md` and `docs/fedora-packaging.md`.
