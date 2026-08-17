# Pulse Linux 8.0.1.2AE

This is the first Arch-native Pulse Supernova Linux release candidate aligned to the shared Pulse `8.0.1.2` product version.

- Preserves the accepted Pulse Standard shell and shared read-only intelligence foundation.
- Adds Arch-only `/etc/os-release` gating; derivatives remain disabled until verified.
- Replaces dpkg/APT evidence with pacman database, inventory, and local update evidence.
- States honestly that standard pacman metadata does not classify security-only updates.
- Adds optional SELinux/AppArmor detection, multi-service firewall posture, and running-kernel module checks.
- Builds a native x86_64 `pkg.tar.zst` and AE-suffixed portable archive.
- Isolates updater selection to `linux-v8.0.1.2AE` and the exact pacman-package asset contract.
- Publishes from the dedicated `linux-ae` branch without replacing DE or FE source branches.

This release remains pending physical validation on Arch Linux x64 hardware. Arch derivatives are not supported until separately verified.
