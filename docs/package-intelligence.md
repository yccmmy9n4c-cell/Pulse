# Package Intelligence

Pulse Linux Beta 0.0.0.13 adds a dedicated Package Intelligence page for supported Debian-family desktops.

## Evidence contract

| Card | Provider | Read-only source |
| --- | --- | --- |
| Package Database | `linux.dpkg-audit` | `dpkg --audit` |
| Installed Inventory | `linux.dpkg-inventory` | `dpkg-query -W` against the local database |
| Available Updates | `linux.apt-cached-updates` | `apt list --upgradable` using the current local cache |
| Security Updates | `linux.apt-security-updates` | `apt-get --simulate --no-download upgrade` using the current local cache |
| Automatic Security Updates | `linux.unattended-upgrades` | standard files under `/etc/apt/apt.conf.d` |
| Restart Requirement | `linux.reboot-required` | `/var/run/reboot-required` and optional package list |

## Safety boundary

Package Intelligence never runs `apt update`, downloads packages, installs or removes packages, repairs dpkg, performs a distribution upgrade, changes update preferences, or restarts the computer. Cached update results can be older than the repositories; the interface and report state that limitation explicitly.

## Physical validation

1. Run an assessment on Debian, Ubuntu, or Linux Mint.
2. Confirm all six Package Intelligence cards populate or explain why evidence is unavailable.
3. Compare Installed Inventory with `dpkg-query -W -f='${binary:Package}\t${Status}\n'` without modifying the package database.
4. Compare Available Updates with `apt list --upgradable` without first running `apt update` solely for Pulse testing.
5. If the desktop already reports a restart requirement, confirm Pulse advises a convenient restart but does not initiate one.
6. Confirm Dashboard Package Intelligence matches the combined state shown on the dedicated page.
