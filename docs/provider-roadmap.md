# Linux-native provider roadmap

| Evidence source | Phase | Access policy | User meaning |
| --- | --- | --- | --- |
| `/etc/os-release` | 1 | Direct read | Distribution identity and support boundary |
| `/proc`, `/sys` | 1–2 | Direct read | Kernel, CPU, memory, storage, thermal, and device foundation; baseline implemented |
| systemd | 1–2 | Read status; approved user-unit writes | Boot/service health; opt-in weekly user scheduling implemented in Piece 5 |
| `journalctl` | 2 | User-readable queries | Recent system and application reliability signals |
| dpkg/APT | 2 | Read package/update state | dpkg audit and cached upgrade list implemented; deeper history deferred |
| NetworkManager / `ip` | 2 | Read configuration/status | Plain-language network state |
| UFW / nftables | 2 | Read detectable posture | Service indicators implemented without claiming rule coverage |
| SMART / NVMe tools | 2 | Optional, read-only | Drive-health evidence when tooling and permissions permit |
| LUKS | 2 | Read block metadata | Detectable LUKS layer implemented; coverage confirmation deferred |
| AppArmor | 2 | Read status | Kernel enablement implemented; profile coverage deferred |
| unattended-upgrades | 2 | Read configuration | Standard APT periodic configuration implemented; success history deferred |
| Backup tools | 2 | Detect known tools/config | Evidence of detectable backup tooling, never proof of recoverability |

Missing commands are capabilities to explain, not system-health failures. Every provider must return evidence provenance, confidence, plain-language summary, and safe guidance when merged into Pulse Core.
