# Pulse Linux Beta 0.0.0.21

This release adds a reversible, user-approved exception for an intentionally inactive firewall while preserving all Network Intelligence evidence.

- Adds **Firewall Is Off by Choice** to the Network Intelligence firewall card when an assessment finds no active UFW or nftables service indicator.
- Records the choice in `~/.config/Pulse Platform/settings.json` with the acknowledgment time.
- Retains the observed firewall evidence but marks the posture accepted so Pulse no longer requests review for it.
- Adds **Restore Firewall Review** so the decision can be reversed at any time without changing the firewall.
- Applies the preference consistently to Dashboard, Network Intelligence, Security Intelligence, reports, and scheduled assessments.
- Never masks an active firewall result, an unavailable query, or a different firewall finding.
- Adds regression tests for safe defaults, persistence, evidence transformation, active-evidence protection, and restoration.

Pulse Linux Beta 0.0.0.20 Network Intelligence was physically validated as functioning as expected. Version 0.0.0.20 can now be used to validate the complete in-app upgrade to 0.0.0.21.
