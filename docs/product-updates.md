# Pulse product updates

Pulse Supernova Linux 0.0.0.19 introduces a user-controlled update path for supported Debian-family desktops.

## What the Updates page does

1. **Check for Updates** reads the public releases from `yccmmy9n4c-cell/Pulse` on GitHub. Pulse makes no update request at startup or in the background.
2. Pulse ignores drafts and unrelated product assets, then selects the newest release containing both the exact architecture-specific Debian package and `SHA256SUMS`.
3. **Download Update** saves the package to `~/Downloads` and verifies its SHA-256 digest. A missing or mismatched checksum blocks the package and removes the partial download.
4. **Open Installer** hands the verified `.deb` to the desktop. The normal graphical package installer remains responsible for showing the package, asking for confirmation, and requesting authentication if needed.

Pulse never silently installs, elevates, changes repositories, refreshes APT, or restarts the computer through this feature.

The Updates page follows the same platform boundary as assessment: it is enabled only on verified Debian, Ubuntu, and Linux Mint desktops. An unverified derivative or explicitly excluded distribution receives a clear unsupported message and no package is selected or downloaded.

## Publishing the release assets

A normal push runs all build, test, package, install, render, and checksum gates. It does not create a release.

After that push is green, run **Actions → Pulse Linux x64 Build → Run workflow** on `main` with the source version. A successful manual run publishes or replaces these assets under the tag `linux-v<version>`:

- `pulse-platform_<version>_amd64.deb`
- `pulse-platform-<version>-linux-x64.tar.gz`
- `SHA256SUMS`

The release title is `Pulse Linux Beta <version>` and the release is marked as a prerelease while the Linux product remains in Beta. The updater intentionally reads compatible prereleases so Beta users can move forward.

The app can discover a release only after this publishing step. GitHub Actions artifacts alone are not the public release assets used by the updater.
