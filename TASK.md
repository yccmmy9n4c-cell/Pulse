# Pulse Linux FE development task

## Release target

- Product: Pulse Supernova Linux
- Version: `8.0.1.2FE`
- Branch: `linux-fe`
- Public tag: `linux-v8.0.1.2FE`
- Package: `pulse-platform-8.0.1.2-1.x86_64.rpm`
- Portable test archive: `pulse-platform-8.0.1.2FE-linux-x64.tar.gz`

## Implemented foundation

- Fedora-only support gate with unverified derivative isolation
- 54-provider assessment contract using Fedora-native package/security sources
- RPM/DNF, SELinux, firewalld/nftables, and DNF restart-hint intelligence
- FE-only updater stream and checksum-verified RPM download
- Fedora-container GitHub workflow with compile, smoke, package, install, GUI-render, and release gates

## Acceptance still required

1. Push this source to the repository's `linux-fe` branch.
2. Require every GitHub Actions gate to pass.
3. Install the RPM on a separate Fedora Workstation x64 computer.
4. Capture Dashboard, Package, Security, Compatibility, Updates, and Mission Control screenshots.
5. Record assessment logs, score behavior, review actions, updater download/open behavior, and RPM upgrade/removal results.
6. Add derivatives or RHEL-family systems only through a later explicit compatibility decision.
