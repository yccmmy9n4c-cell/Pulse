# Debian-family compatibility matrix

Support is earned by physical or representative-VM verification; family resemblance alone is insufficient.

| Distribution | Target release | Architecture | Phase 1 state | Notes |
| --- | --- | --- | --- | --- |
| Ubuntu Desktop | 24.04 LTS | x64 | Required first gate | Primary packaging/build baseline |
| Ubuntu Desktop | 26.04 LTS | x64 | Planned | Current LTS validation after 24.04 |
| Debian Desktop | 13 | x64 | Required first gate | Test GNOME; record KDE separately if available |
| Linux Mint | 22.x | x64 | Required first gate | Cinnamon desktop launcher validation |
| LMDE | Current supported | x64 | Planned | Accepted by `ID=linuxmint`; requires separate record |
| Ubuntu/Debian | Supported targets | arm64 | Deferred | Starts after all x64 acceptance gates |
| Pop!_OS and other derivatives | Any | Any | Unverified/unsupported | Do not enable from `ID_LIKE` alone |
| Fedora/RHEL | Any | Any | Explicitly excluded | No RPM scope |
| Arch family | Any | Any | Explicitly excluded | No pacman scope |
| BSD family | Any | Any | Explicitly excluded | Not Linux and outside product boundary |

## Per-target acceptance gate

- `/etc/os-release` classification is correct.
- App launches from terminal and desktop menu.
- UI is visually consistent at 100% and 150% scaling.
- Pulse Standard navigation, official logo, Dashboard hierarchy, header, activity strip, and all six pages match the approved Aurora baseline.
- Workflow and physical `pulse-standard-shell.png` screenshots are attached to the test record.
- Read-only assessment completes without elevation.
- Optional weekly scheduling requires confirmation and operates only through `systemd --user`.
- Scheduled `--assess-once` execution produces a report without a graphical session.
- Missing optional tools are explained, not treated as failures.
- Settings and data resolve to the documented user directories.
- `.deb` install, upgrade, and remove behavior is recorded.
- Portable `.tar.gz` launches without writing outside user locations.
- Screenshot and `journalctl --user`/terminal logs are attached to the test record.
