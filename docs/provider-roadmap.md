# Linux-native provider roadmap

| Evidence source | Phase | Access policy | User meaning |
| --- | --- | --- | --- |
| `/etc/os-release` | 1 | Direct read | Distribution identity and support boundary |
| `/proc`, `/sys` | 1–2 | Direct read | Kernel, CPU, memory, storage, thermal, and device foundation |
| systemd | 1–2 | Read status | Boot/service health; user scheduling only by approval |
| `journalctl` | 2 | User-readable queries | Recent system and application reliability signals |
| dpkg/APT | 2 | Read package/update state | Update posture and held/broken package advice |
| NetworkManager / `ip` | 2 | Read configuration/status | Plain-language network state |
| UFW / nftables | 2 | Read detectable posture | Firewall posture without claiming protection from presence alone |
| SMART / NVMe tools | 2 | Optional, read-only | Drive-health evidence when tooling and permissions permit |
| LUKS | 2 | Read block metadata | Encryption posture, clearly distinguishing active vs configured |
| AppArmor | 2 | Read status | Mandatory access-control posture |
| unattended-upgrades | 2 | Read configuration | Automatic security-update posture |
| Backup tools | 2 | Detect known tools/config | Evidence of detectable backup tooling, never proof of recoverability |

Missing commands are capabilities to explain, not system-health failures. Every provider must return evidence provenance, confidence, plain-language summary, and safe guidance when merged into Pulse Core.

