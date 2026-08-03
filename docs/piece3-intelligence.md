# Piece 3 intelligence contract

Pulse Linux Beta 0.0.0.3 is the first functional Linux intelligence layer.

## Result contract

Every provider returns:

- Stable provider identifier
- Plain-language title and summary
- `Healthy`, `Attention`, `Informational`, or `Unavailable` state
- Safe guidance
- Evidence source or command provenance

An unavailable provider cannot stop the rest of the assessment.

## Command boundary

Command-backed providers use `ProcessStartInfo.ArgumentList` without a shell. Commands receive fixed arguments, have an eight-second default timeout, capture standard output/error, and never invoke `sudo`, `pkexec`, Polkit, or a privileged helper.

Piece 3 commands are read-only:

- `dpkg --audit`
- `apt list --upgradable` using the existing local cache
- `systemctl is-active ufw.service`
- `systemctl is-active nftables.service`
- `lsblk --json --output NAME,TYPE,FSTYPE,MOUNTPOINTS`

The APT provider does not run `apt update`, contact repositories intentionally, install packages, or modify package state.

## Interpretation limits

- An active firewall service does not prove that its rules are sufficient.
- Absence of a detected LUKS layer does not prove that all data is unencrypted.
- An enabled unattended-upgrades setting does not prove that recent updates succeeded.
- Cached package updates may be stale until the distribution's normal updater refreshes them.
- AppArmor kernel enablement does not prove complete application profile coverage.
