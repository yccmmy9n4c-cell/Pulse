# Arch packaging and update contract

The AE GitHub identity is `8.0.1.2AE`; the native package version remains numeric and conventional:

- Tag: `linux-v8.0.1.2AE`
- Pacman package: `pulse-platform-8.0.1.2-1-x86_64.pkg.tar.zst`
- Portable: `pulse-platform-8.0.1.2AE-linux-x64.tar.gz`
- Manifest: `SHA256SUMS`

The updater ignores DE and FE releases even if an incorrectly named pacman package is attached. It downloads only the exact AE package for the running architecture, verifies SHA-256, saves it in Downloads, and asks the desktop to open it. Installation remains a separate user-approved pacman/package-installer action.

The package installs the application under `/opt/pulse-platform`, the launcher under `/usr/bin/pulse-platform`, and desktop/icon integration under `/usr/share`. User settings and data remain under `~/.config/Pulse Platform` and `~/.local/share/Pulse Platform`.
