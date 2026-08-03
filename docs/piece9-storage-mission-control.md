# Piece 9 storage intelligence and Mission Control

Pulse Linux Beta 0.0.0.9 introduces the visible **Pulse Supernova Linux** product name while preserving `pulse-platform` package identifiers, `/opt/pulse-platform`, and existing user-data locations for upgrade compatibility.

## Supernova Mission Control

Mission Control is restored as the product intelligence center using the supplied Pulse Supernova reference screenshots. It contains:

- Current Mission and mission status;
- What Pulse Does and the no-silent-change promise;
- development version, release channel, framework, and Build ID;
- computer, verified distribution, reports folder, and settings folder; and
- the Linux launch-identity statement.

Runtime identity and version presentation now come from `AppInfo.cs` rather than separate constants in the graphical and headless paths.

## Storage Intelligence

The new Storage page presents four established evidence areas:

- root filesystem capacity;
- physical SMART/NVMe health indicators;
- readable LUKS posture; and
- detectable backup posture.

### Drive-health safety

Pulse enumerates physical disks through `lsblk`. When optional tooling is installed and readable, it uses `smartctl --nocheck=standby,3 --health` or `nvme smart-log`. It does not wake standby drives, start a self-test, write device settings, or elevate privileges. Models may be shown; serial numbers and raw SMART output are not retained.

### Backup meaning

Pulse detects known executable and configuration paths for Déjà Dup, Pika Backup, Back In Time, BorgBackup, Restic, Duplicity, and Timeshift. Detection is informational and never claims that a backup ran recently or can be restored.

## Upload comment

The GitHub upload/commit comment is intentionally limited to `Pulse Linux Beta 0.0.0.9`.
