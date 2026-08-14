# Pulse Linux Beta 0.0.0.23

This release corrects the checksum-manifest contract that prevented the verified in-app updater from downloading 0.0.0.22.

- Generates `SHA256SUMS` with exact GitHub release-asset basenames instead of absolute GitHub runner paths.
- Adds a build gate that rejects checksum entries containing any directory path.
- Makes the updater safely recognize matching basenames in legacy path-qualified manifests while still requiring the exact package filename and SHA-256 hash.
- Adds regression coverage for the path-qualified 0.0.0.22 failure.

The 0.0.0.22 Avalonia render-gate correction is retained:

- Waits for Avalonia to finish painting after the X11 window becomes visible.
- Retries the screenshot up to five times instead of failing on the first incomplete frame.
- Logs every grayscale-deviation result and emits an explicit diagnostic if rendering remains blank.

- Adds **Firewall Is Off by Choice** to the Network Intelligence firewall card when an assessment finds no active UFW or nftables service indicator.
- Records the choice in `~/.config/Pulse Platform/settings.json` with the acknowledgment time.
- Retains the observed firewall evidence but marks the posture accepted so Pulse no longer requests review for it.
- Adds **Restore Firewall Review** so the decision can be reversed at any time without changing the firewall.
- Applies the preference consistently to Dashboard, Network Intelligence, Security Intelligence, reports, and scheduled assessments.
- Never masks an active firewall result, an unavailable query, or a different firewall finding.
- Adds regression tests for safe defaults, persistence, evidence transformation, active-evidence protection, and restoration.

Pulse Linux Beta 0.0.0.22 discovered correctly in the updater but its published checksum manifest contained build-runner paths, so Pulse safely refused the download and installed nothing. Version 0.0.0.23 supersedes that manifest and is the next updater acceptance target.
