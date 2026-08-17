# Pulse Linux 8.0.1.2FE

This is the first Fedora-native Pulse Supernova Linux release candidate aligned to the shared Pulse `8.0.1.2` product version.

- Preserves the accepted Pulse Standard shell and shared read-only intelligence foundation.
- Adds Fedora-only `/etc/os-release` gating; Fedora derivatives remain disabled until verified.
- Replaces dpkg/APT evidence with RPM database/inventory and cache-only DNF update/security evidence.
- Replaces AppArmor/UFW integration with SELinux and firewalld/nftables posture.
- Adds `dnf-automatic` and DNF restart-hint evidence.
- Builds a native x86_64 RPM and FE-suffixed portable archive.
- Isolates updater selection to `linux-v8.0.1.2FE` and the exact RPM asset contract.
- Publishes from the dedicated `linux-fe` branch without replacing the accepted DE source on `main`.

This release remains pending physical validation on Fedora Workstation hardware. RHEL and unverified Fedora derivatives are not supported by this release.
