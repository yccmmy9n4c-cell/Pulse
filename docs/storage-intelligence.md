# Storage Intelligence

Pulse Linux Beta 0.0.0.16 presents a dedicated Storage Intelligence page with six evidence-backed cards for supported Debian-family desktops.

## Evidence contract

| Card | Provider | Read-only source |
| --- | --- | --- |
| System Storage | `linux.storage-root` | .NET `DriveInfo` for `/` capacity |
| Root Filesystem Mount | `linux.root-mount` | `findmnt --json --target /` source, type, and options |
| Filesystem Inode Capacity | `linux.inode-capacity` | `df --portability --inodes /` |
| Physical Drive Health | `linux.drive-health` | `lsblk`; optional standby-safe `smartctl --nocheck=standby,3 --health` or `nvme smart-log` |
| Disk Encryption | `linux.luks-indicator` | readable `lsblk` metadata for a LUKS layer |
| Backup Posture | `linux.backup-posture` | known local tool and configuration paths |

## Interpretation

- Root capacity and inode use request attention at 85% or above.
- A root filesystem mounted read-only requests attention; Pulse does not attempt a remount or repair.
- Missing SMART/NVMe tooling or permissions mean incomplete coverage, not a detected drive failure.
- A current SMART overall-health failure, an active pre-failure threshold, or an active NVMe critical warning, very low spare capacity, or very high wear requests attention.
- Historical SMART attribute/error/self-test records and a nonzero lifetime NVMe media-error count are informational. They do not by themselves prove that the drive is currently failing; Pulse recommends maintaining a verified backup and using the manufacturer's diagnostic tool if errors recur or symptoms appear.
- LUKS detection is a positive indicator but does not claim every user-data path is encrypted.
- Backup tool/configuration detection never claims that a recent backup succeeded or can be restored.

## Safety boundary

Storage Intelligence never deletes files, repairs filesystems, remounts storage, wakes a sleeping drive, starts a SMART self-test, unlocks encryption, creates a backup, or elevates privileges.

## Physical validation

1. Run a new assessment and confirm all six cards populate or clearly explain unavailable coverage.
2. Compare System Storage with `df -h /` and inode capacity with `df -Pi /`.
3. Compare the root source, filesystem type, and `ro`/`rw` state with `findmnt /`.
4. Confirm sleeping disks remain asleep and no SMART/NVMe self-test starts.
5. Confirm historical diagnostic records are described as informational and an active SMART failure still requests attention.
6. Confirm backup wording does not claim success or recoverability.
7. Confirm the Dashboard Storage Intelligence state matches the combined dedicated-page state.
