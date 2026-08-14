# Reliability Intelligence

Pulse Linux Beta 0.0.0.24 promotes Reliability Intelligence from a Dashboard-only summary into a dedicated six-card domain for supported Debian-family desktops.

| Card | Evidence | Meaning |
| --- | --- | --- |
| Current-Boot Journal | `journalctl --boot=0 --priority=0..3` metadata | Aggregate error severity and leading sources without message bodies |
| Failed System Services | `systemctl --failed --type=service` | Names of system services currently marked failed |
| Failed User Services | `systemctl --user --failed --type=service` | Names of signed-in-user services currently marked failed |
| Boot Timing | `systemd-analyze time --no-pager` | Current boot-duration baseline, informational by itself |
| System Uptime | `/proc/uptime` | Current running-time context, not an automatic restart recommendation |
| Restart Posture | `/var/run/reboot-required` | Debian package-maintenance restart request |

The Dashboard Reliability score uses these exact six provider IDs. Missing visibility is reported as unavailable coverage and does not become proof of a fault.

## Safety and privacy boundary

- Pulse never reads or retains journal message bodies.
- Failed-service evidence retains unit names only, not service descriptions or log content.
- Pulse never starts, stops, restarts, enables, disables, or resets a service.
- Pulse never reboots automatically.
- **Open System Logs** launches `gnome-logs` or `ksystemlog` when installed. Otherwise Pulse shows the evidence and guidance inside the Linux Assessment page.
