# Pulse Linux 8.0.3.0DE Development Candidate

This internal Debian-family candidate begins the controlled transfer of shared Pulse 8.0.3.0 correctness and release-quality advances without reproducing Windows-only functionality.

- Corrects the pearOS/PowerDevil journal false positive by filtering only two exact known-benign timing diagnostics.
- Continues reporting genuine PowerDevil/DDC I2C permission failures.
- Reads journal message text transiently for classification and duplicate detection, but never returns, reports, archives, or persists message bodies.
- Consolidates identical journal rows while preserving occurrence counts and review status for genuine repeated failure bursts.
- Says `at least 100` when the journal query reaches its 100-row cap.
- Prefers the real process identifier over generic `user@1000.service` attribution.
- Removes failed-service providers from the Reliability Dashboard score because Startup Intelligence already owns those findings.
- Limits the stable updater to published, non-draft, non-prerelease `DE` releases with the required Debian assets.
- Retains all 54 Debian-family evidence providers and the accepted 8.0.1.2DE interface.
- Leaves Outlook, Office, registry, Windows Event Log, DISM, WinSxS, Defender, UAC, and Windows service behavior in Pulse Windows.

This candidate must remain on the `Testing` branch until compilation, installed-package launch, pearOS validation, and the stable release gate are complete.
