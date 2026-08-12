# Security Intelligence

Pulse Linux Beta 0.0.0.18 includes a dedicated Security Intelligence page with six local, read-only evidence cards for supported Debian-family desktops and a guided review-details action.

## Evidence contract

| Card | Provider | Read-only source |
| --- | --- | --- |
| AppArmor | `linux.apparmor` | `/sys/module/apparmor/parameters/enabled` |
| Firewall Posture | `linux.firewall-indicator` | `systemctl is-active` for UFW and nftables service indicators |
| Security Updates | `linux.apt-security-updates` | APT's existing local cache through simulation only |
| Automatic Security Updates | `linux.unattended-upgrades` | standard APT periodic configuration files |
| Disk Encryption | `linux.luks-indicator` | readable `lsblk` metadata for a LUKS layer |
| Secure Boot | `linux.secure-boot` | UEFI `SecureBoot-*` efivar state byte when visible |

## Interpretation

- An enabled protection layer is positive evidence, but it is not proof of complete policy or device coverage.
- Cached security updates request review because maintenance is pending.
- AppArmor disabled, automatic security updates disabled, and firmware-reported Secure Boot disabled are informational hardening choices. They do not reduce current system-health scores or imply that Linux is malfunctioning.
- Missing tools, unreadable firmware state, and absent service indicators are incomplete or informational coverage unless Pulse has direct evidence of a disabled control.
- An inactive UFW/nftables service does not prove that no firewall rules exist; another service or direct rules may provide protection.
- LUKS presence does not prove that every user-data path is encrypted.
- Cached APT data may be stale because Pulse does not refresh repositories during an assessment.

## Safety boundary

Security Intelligence does not change AppArmor policy, inspect private journal messages, refresh APT, install packages, modify firewall rules, alter encryption, write firmware variables, change Secure Boot, or elevate privileges.

## Physical validation

1. Confirm **Security Intelligence** appears in the navigation tree and opens its dedicated six-card page.
2. Run a new assessment and compare AppArmor with `cat /sys/module/apparmor/parameters/enabled` when that path exists.
3. Compare firewall service indicators with `systemctl is-active ufw.service` and `systemctl is-active nftables.service`, while retaining Pulse's limited-coverage wording.
4. Compare cached security-update results with the distribution update tool without asking Pulse to refresh repositories.
5. Confirm the LUKS card does not overstate encryption coverage.
6. Compare Secure Boot with the firmware or distribution system-information screen; confirm unavailable UEFI evidence is not presented as disabled.
7. Confirm no elevation, firmware change, firewall change, package installation, or automatic repair occurs.
8. Confirm the Dashboard Security Intelligence state matches the six dedicated-page sources.
