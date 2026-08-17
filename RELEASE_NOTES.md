# Pulse Linux Beta 0.0.0.30

This build adds dedicated Backup Intelligence while preserving the physically validated 0.0.0.29 Startup Intelligence milestone.

- Adds a six-card Backup Intelligence page for tools/configuration, schedules, recent activity metadata, mounted destination context, system snapshots, and restore readiness.
- Adds Backup Intelligence as the tenth evidence-backed Dashboard domain.
- Expands the assessment from 43 to 48 isolated providers by adding five Linux-native, read-only backup sources and reusing the accepted backup-posture provider.
- Excludes journal message bodies, private repository paths, mount paths, device UUIDs, and backup contents from Pulse evidence.
- Opens an installed graphical backup application when available; Pulse never runs a backup, initiates a restore, mounts storage, changes a timer, or creates/deletes a snapshot.
- Never claims that configuration, scheduling, activity, a mounted destination, or a snapshot proves successful backup or recoverability.
- Preserves automatic GitHub prerelease publication, checksum verification, coverage-neutral scoring, and every existing Pulse safety boundary.
