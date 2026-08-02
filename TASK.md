# Pulse Platform Linux — Development Task

## Task identity

- Continuation point: Pulse macOS Preview 0.52.0
- macOS status: on hold for Apple Developer Program signing/notarization and deferred physical validation
- Linux status: active, Phase 1
- Linux version: `0.0.0.2`
- GitHub release title: `Pulse Linux Beta 0.0.0.2`
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
| GitHub x64 build pipeline | Implemented, awaiting first run | Compile, smoke-test, GUI-launch-test, package, inspect, checksum, and upload |
| Unified shell | Implemented, uncompiled here | Avalonia shell loads support status and first read-only evidence |
| Shared Core/macOS merge | Blocked | macOS 0.52.0 source bundle or repository checkout supplied |
| linux-x64 build | Pending | Restore, compile, launch, and package succeed on .NET 10 host |
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

## Next engineering checkpoint

1. Import macOS Preview 0.52.0/shared Pulse Core.
2. Reconcile project references, namespaces, Avalonia version, resources, and exact visual tokens.
3. Compile on a .NET 10 host.
4. Produce `linux-x64` `.tar.gz` and `.deb`.
5. Run the first Ubuntu/Debian physical validation and attach screenshots and logs.
