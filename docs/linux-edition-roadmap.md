# Pulse Linux edition roadmap

All release editions align to the shared Pulse product version `8.0.1.2` while retaining an explicit platform-edition suffix.

The suffix belongs to the GitHub release identity and updater stream. Native package-manager versions remain numeric: the DE updater follows `linux-v8.0.1.2DE` and selects the matching numeric-version `.deb`; future FE and AE updaters will follow only their own suffixed release streams and native package formats.

| Edition | Visible version | Native package | Support boundary | Status |
| --- | --- | --- | --- | --- |
| Debian-family Edition | `8.0.1.2DE` | `.deb` and portable `.tar.gz` | Debian, Ubuntu, Linux Mint, and deliberately verified derivatives | Feature-complete release checkpoint |
| Fedora-family Edition | `8.0.1.2FE` | `.rpm` and portable `.tar.gz` | Fedora first; RHEL-family systems only after deliberate verification | Separate FE source candidate |
| Arch-family Edition | `8.0.1.2AE` | `pkg.tar.zst` and portable `.tar.gz` | Arch Linux first; derivatives only after deliberate verification | Source-complete candidate; physical validation pending |

## Shared foundation

All editions retain Pulse Supernova identity, .NET 10, Avalonia, user-owned settings/data, read-only discovery, plain-language guidance, explicit user approval, checksum-verified updates, and the narrow future elevation boundary.

## Native separation

- DE uses dpkg/APT, AppArmor, UFW/nftables, Debian update conventions, and `.deb` packaging.
- FE uses rpm/DNF, SELinux, firewalld/nftables, Fedora update conventions, and `.rpm` packaging.
- AE uses pacman/libalpm conventions, Arch security and service posture, and `pkg.tar.zst` packaging.

Providers that are genuinely portable (`/proc`, `/sys`, systemd, journal metadata, hardware, performance, and Avalonia UI) can be shared behind edition-specific contracts. Package, security, compatibility, updater, and native-action providers must never be copied across editions without verification.
