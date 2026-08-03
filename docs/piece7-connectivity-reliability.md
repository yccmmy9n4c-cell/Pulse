# Piece 7 connectivity and reliability intelligence

Pulse Linux Beta 0.0.0.8 resumes Linux-native intelligence after the Pulse Standard shell passed physical visual review.

## Network posture

`NetworkPostureEvidenceProvider` reads local structure through:

- `ip -json link show up`
- IPv4 and IPv6 default-route metadata
- NetworkManager's existing state through `nmcli general` when available

Pulse does not ping a host, perform a speed test, resolve a public name, contact a repository, or otherwise probe the internet. An active interface and default route are positive local indicators, not proof of internet access. A deliberately offline or isolated computer is explained without being treated as a failure.

## Current-boot reliability

`JournalReliabilityEvidenceProvider` asks `journalctl` for metadata fields only and reads up to 100 error-or-higher entries from the current boot. It reports only:

- the number of readable error-or-higher events;
- the number at critical priority or higher; and
- leading service/application source identifiers.

Pulse does not copy journal message bodies into evidence, snapshots, or reports. This reduces accidental collection of account names, paths, device details, and application content. A small isolated error count remains informational; critical events or repeated errors request review.

## Trend intelligence

The Dashboard's **Recent Changes** card now compares provider states with the previous assessment and lists up to three transitions. If no state changed, Pulse says so directly.

## Safety boundary

Both providers use the existing fixed-argument, no-shell command runner. They are read-only, operate as the current user, tolerate missing tools or permissions, and never invoke `sudo` or Polkit.
