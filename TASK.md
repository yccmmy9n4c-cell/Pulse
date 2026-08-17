# Pulse Linux AE development task

## Release target

- Product: Pulse Supernova Linux
- Version: `8.0.1.2AE`
- Branch: `linux-ae`
- Public tag: `linux-v8.0.1.2AE`
- Package: `pulse-platform-8.0.1.2-1-x86_64.pkg.tar.zst`
- Portable test archive: `pulse-platform-8.0.1.2AE-linux-x64.tar.gz`

## Implemented foundation

- Arch-only support gate with derivative isolation
- 54-provider assessment contract using Arch-native package/security sources
- Pacman database/inventory/update, security-coverage boundary, MAC, firewall, and running-kernel intelligence
- AE-only updater stream and checksum-verified pacman-package download
- Arch-container GitHub workflow with compile, smoke, package, install, GUI-render, and release gates

## Acceptance still required

1. Push this source to the repository's `linux-ae` branch.
2. Require every GitHub Actions gate to pass.
3. Install the package on a separate Arch Linux x64 computer.
4. Capture Dashboard, Package, Security, Compatibility, Updates, and Mission Control screenshots.
5. Record assessment logs, score behavior, review actions, updater download/open behavior, and pacman upgrade/removal results.
6. Add Manjaro or another derivative only through a later explicit compatibility decision.
