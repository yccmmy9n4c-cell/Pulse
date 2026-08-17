# Backup Intelligence

Pulse Linux Beta 0.0.0.30 adds a dedicated Backup Intelligence page and matching Dashboard domain. It separates evidence that a backup mechanism may exist from proof that data can actually be restored.

| Card | Evidence | Interpretation |
| --- | --- | --- |
| Tools and Configuration | Known executable and user-configuration paths | Detects supported backup applications without opening repositories |
| Backup Schedules | `systemctl --user list-timers --all` | Recognizes backup-related user timers; other schedulers may remain outside coverage |
| Recent Backup Activity | User-journal source metadata from the last 30 days | Counts recognized application/unit activity without retaining message bodies |
| Mounted Destination Context | `findmnt` filesystem types and `lsblk` removable/mounted flags | Counts network and removable mounted storage without retaining mount paths |
| System Snapshots | Readable Timeshift scheduling keys and standard snapshot-directory presence | Distinguishes local rollback context from independent user-data backup |
| Restore Readiness | Pulse safety boundary | Explicitly states that Pulse has not performed a restore test and cannot claim recoverability |

## Supported tool discovery

The existing posture provider recognizes Déjà Dup, Pika Backup, Back In Time, BorgBackup, Restic, Duplicity, and Timeshift through conventional executable and configuration locations. Discovery is intentionally conservative and is not proof that a tool is active.

## Privacy boundary

Pulse does not retain journal message bodies, repository addresses, mount paths, device UUIDs, filenames, backup contents, credentials, or encryption keys. Counts and recognized application/unit names are sufficient for the user-facing explanation.

## Health interpretation

- Configuration and installed tools are context, not proof of success.
- A timer is context, not proof that its last job completed.
- Journal activity is context, not a success record.
- Mounted storage is context, not proof that it is the configured destination.
- A system snapshot is not treated as an independent backup.
- Missing optional evidence remains visible coverage and does not reduce health.

## Guided review and safety

Pulse can open an installed Déjà Dup, Pika Backup, Back In Time, or Timeshift graphical application. It never runs or schedules a backup, initiates a restore, opens a repository, mounts or unmounts storage, changes a timer, or creates/deletes a snapshot. Any action remains inside the chosen application with its own confirmations and authentication.

## Physical validation

1. Confirm all six cards render and the Dashboard Backup row matches the page score and state.
2. Compare recognized user timers with `systemctl --user list-timers --all`.
3. Confirm activity cards never display journal messages or private repository details.
4. Confirm destination context reports only counts and never mount paths.
5. Confirm the guided action opens an installed backup application or falls back to detailed Assessment guidance.
6. Confirm refreshing Pulse does not alter backups, timers, mounts, repositories, or snapshots.
