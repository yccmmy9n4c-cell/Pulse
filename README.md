# Pulse Supernova Linux — Arch Edition 8.0.1.2AE

Pulse Linux AE carries the unified Pulse Standard interface and read-only intelligence foundation into a deliberately isolated Arch-native edition.

## Identity and boundary

- Shared product version: `8.0.1.2`
- GitHub and updater identity: `8.0.1.2AE`
- GitHub tag: `linux-v8.0.1.2AE`
- GitHub Desktop commit summary: `Pulse Linux 8.0.1.2AE`
- Supported first: Arch Linux desktop on `linux-x64`
- Detected but disabled pending verification: derivatives reporting `ID_LIKE=arch`
- Explicitly outside AE: Debian-family, Fedora/RHEL, BSD, and unrelated systems

Pulse reads `/etc/os-release` before enabling assessment, scheduling, or update selection. Manjaro and other derivatives are not silently treated as Arch.

## Arch-native intelligence

- Pacman database consistency and installed inventory
- `pacman -Qu` against the existing local sync database without `pacman -Sy`
- Explicit plain-language boundary where standard pacman metadata cannot classify security-only updates
- Arch full-system update policy guidance without enabling automatic partial upgrades
- SELinux/AppArmor kernel posture when either is present
- UFW, firewalld, and nftables service indicators
- Running-kernel module-tree restart hint
- Shared `/proc`, `/sys`, systemd, journal, NetworkManager, SMART/NVMe, LUKS, Secure Boot, storage, hardware, performance, startup, backup, and reliability evidence

All discovery is local and read-only. Pulse does not synchronize pacman databases, install packages, alter security/firewall policy, enable timers, or elevate privileges.

## Build locally on Arch

Install .NET 10 SDK, `base-devel`, and the graphical runtime dependencies. Run the packaging script as a normal non-root user:

```bash
dotnet restore src/Pulse.Platform.Linux/Pulse.Platform.Linux.csproj --runtime linux-x64
dotnet restore tests/Pulse.Platform.Linux.SmokeTests/Pulse.Platform.Linux.SmokeTests.csproj --runtime linux-x64
./packaging/build-arch.sh linux-x64 8.0.1.2
```

Outputs:

- `artifacts/linux-x64/pulse-platform-8.0.1.2-1-x86_64.pkg.tar.zst`
- `artifacts/linux-x64/pulse-platform-8.0.1.2AE-linux-x64.tar.gz`
- `artifacts/linux-x64/SHA256SUMS`

## Publish without replacing DE or FE

Keep DE on `main` and FE on `linux-fe`. Create a branch named `linux-ae`, place this bundle at that branch's repository root (including `.github`), and push with the summary `Pulse Linux 8.0.1.2AE`.

The `Pulse Linux AE x64 Build` workflow runs inside Arch Linux, compiles and smoke-tests the application, creates and installs the pacman package as appropriate, launches the installed GUI under Xvfb, verifies a non-blank screenshot and checksums, then creates the full public GitHub release.

Physical Arch validation remains required after the workflow succeeds. See `docs/arch-compatibility.md` and `docs/arch-packaging.md`.
