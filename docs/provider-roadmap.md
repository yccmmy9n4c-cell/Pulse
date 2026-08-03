# Linux-native provider roadmap

| Evidence source | Phase | Access policy | User meaning |
| --- | --- | --- | --- |
| `/etc/os-release` | 1 | Direct read | Distribution identity and support boundary |
| `/proc`, `/sys` | 1–2 | Direct read | Kernel, CPU, memory, storage, thermal, and device foundation; baseline implemented |
| systemd | 1–2 | Read status; approved user-unit writes | Boot/service health; opt-in weekly user scheduling implemented in Piece 5 |
| `journalctl` | 2 | User-readable queries | Current-boot severity/source summary implemented in Piece 7; message bodies excluded |
| dpkg/APT | 2 | Read package/update state | dpkg audit and cached upgrade list implemented; deeper history deferred |
| NetworkManager / `ip` | 2 | Read configuration/status | Active interfaces, default-route posture, and existing NetworkManager state implemented in Piece 7; no active internet probe |
| UFW / nftables | 2 | Read detectable posture | Service indicators implemented without claiming rule coverage |
| SMART / NVMe tools | 2 | Optional, read-only | Standby-safe health indicators implemented in Piece 9; no self-tests or raw output retention |
| LUKS | 2 | Read block metadata | Detectable LUKS layer implemented; coverage confirmation deferred |
| AppArmor | 2 | Read status | Kernel enablement implemented; profile coverage deferred |
| unattended-upgrades | 2 | Read configuration | Standard APT periodic configuration implemented; success history deferred |
| Backup tools | 2 | Detect known tools/config | Déjà Dup, Pika, Back In Time, Borg, Restic, Duplicity, and Timeshift posture implemented in Piece 9; never proof of recoverability |
| UEFI Secure Boot | 2 | Read visible efivar state | Firmware-reported enabled/disabled posture implemented in Beta 0.0.0.17; missing visibility is not treated as disabled |

Missing commands are capabilities to explain, not system-health failures. Every provider must return evidence provenance, confidence, plain-language summary, and safe guidance when merged into Pulse Core.
