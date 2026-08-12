# Pulse Linux Beta 0.0.0.19

This release adds the first complete Pulse Supernova Linux update experience.

- A dedicated **Updates** page checks the public Pulse GitHub releases only when the user requests it.
- Pulse selects only the exact Debian package for the running architecture.
- Update selection remains disabled on unsupported or unverified distributions.
- Downloads are saved to the user's Downloads folder and must pass the published SHA-256 checksum before Pulse enables **Open Installer**.
- Installation remains visible and user-approved through the desktop's normal graphical package installer; Pulse does not silently elevate or install.
- The GitHub build now publishes verified `.deb`, portable `.tar.gz`, and `SHA256SUMS` release assets when the workflow is run manually on `main`.

The 0.0.0.18 physical test also reached an Executive Health score of 100 after the laptop's remaining package updates were installed. Package, Storage, and Security Intelligence all reported 100.
