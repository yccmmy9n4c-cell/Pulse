# Pulse Linux 8.0.1.2DE

This is the feature-complete Debian-family release checkpoint, aligned to the shared Pulse 8.0.1.2 product version. The `DE` suffix identifies the Debian-family edition without changing the numeric .NET or Debian package version.

- Promotes Pulse Supernova Linux from Beta to the full Release channel.
- Adds the Pulse Standard Linux Compatibility page for distribution support, architecture, desktop environment, display session, systemd user-service readiness, and native evidence-tool coverage.
- Expands the assessment from 48 to 54 isolated providers while ensuring compatibility notes never lower system-health scores.
- Preserves the complete validated Startup Intelligence and Backup Intelligence milestones.
- Publishes `linux-v8.0.1.2DE` as a full GitHub release with the amd64 `.deb`, portable `.tar.gz`, and `SHA256SUMS` updater assets.
- Isolates the DE updater to DE-suffixed GitHub releases while retaining read-only discovery of earlier unsuffixed Linux beta releases as fallback history.
- Keeps Fedora/RHEL and Arch unsupported by the DE package; future `8.0.1.2FE` and `8.0.1.2AE` editions will receive their own native providers and packaging.
- Freezes new Debian-family feature development after this checkpoint until enhancements are deliberately resumed.
