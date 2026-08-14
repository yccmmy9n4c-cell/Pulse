# Pulse Linux Beta 0.0.0.22

This release carries forward the 0.0.0.21 firewall preference and corrects the headless graphical validation gate that prevented 0.0.0.21 from being published.

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

Pulse Linux Beta 0.0.0.20 Network Intelligence was physically validated as functioning as expected. Version 0.0.0.21 did not pass the GitHub render gate and is superseded. Version 0.0.0.20 can now be used to validate the complete in-app upgrade to 0.0.0.22.
