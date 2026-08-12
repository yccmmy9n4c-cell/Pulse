# Guided review actions

Pulse Linux Beta 0.0.0.18 establishes the first shared review-action pattern for Package, Storage, and Security Intelligence.

## User contract

Every domain recommendation must provide a useful path forward without assuming that the user understands Linux commands or administrative tooling:

1. Explain what Pulse found in plain language.
2. Distinguish a current problem from optional hardening, historical information, and unavailable coverage.
3. Present the safest relevant action beside the recommendation.
4. Open an installed graphical utility when a supported match exists.
5. Fall back to the full in-app evidence source and guidance when no safe graphical destination exists.
6. Never claim that opening a utility completed a repair or changed the finding.

## Initial routes

| Evidence | Button | Preferred graphical destination |
| --- | --- | --- |
| Cached package/security updates or unattended-upgrades configuration | Open Software Updater | Linux Mint Update Manager, Ubuntu Update Manager, then GNOME Software |
| Physical drive health | Open Disk Utility | GNOME Disks |
| Other Package, Storage, or Security evidence | Review Details | Pulse Linux Assessment evidence and safe guidance |

Pulse searches only conventional absolute executable paths and launches a fixed, argument-free graphical program. It does not invoke a shell.

## Safety boundary

Review actions do not use `sudo`, request elevation, install updates, repair filesystems, start SMART self-tests, change firewall/AppArmor policy, modify encryption, write firmware variables, or change Secure Boot. Any future action that changes the system requires its own explicit explanation, confirmation, and narrowly scoped helper design.

## Physical validation

1. Confirm each of the three domain pages shows a review-action button after assessment.
2. For available package updates, confirm **Open Software Updater** opens an installed supported graphical updater.
3. For Physical Drive Health, confirm **Open Disk Utility** opens GNOME Disks when installed.
4. When the relevant utility is absent, confirm Pulse opens Linux Assessment details and explains the evidence instead of failing silently.
5. Confirm no action button triggers elevation or changes the system automatically.
