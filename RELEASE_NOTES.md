# Pulse Linux Beta 0.0.0.24

This release adds the first complete Reliability Intelligence domain while preserving the validated 0.0.0.23 updater and checksum contract.

- Adds **Reliability Intelligence** to Pulse Health Platform navigation with a dedicated six-card Supernova page.
- Separates current-boot journal metadata, failed system services, failed user services, boot timing, system uptime, and Debian restart posture.
- Expands the Dashboard Reliability score to use exactly those same six sources.
- Adds **Open System Logs** when a journal or failed-service finding needs review, with detailed in-app evidence when no supported graphical viewer is installed.
- Retains only unit names and aggregate journal severity/source counts; journal message bodies and service descriptions are not copied.
- Never starts, stops, restarts, enables, disables, or resets a systemd unit and never reboots automatically.
- Expands the assessment inventory from 24 to 28 providers and adds deterministic privacy, separation, and no-mutation regression coverage.

Pulse Linux Beta 0.0.0.23 was physically validated as functioning correctly, including discovery, download, SHA-256 verification, and graphical installer handoff through the in-app updater.
