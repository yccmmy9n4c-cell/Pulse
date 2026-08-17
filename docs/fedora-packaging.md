# Fedora packaging and update contract

The FE GitHub identity is `8.0.1.2FE`; the native RPM version remains numeric and conventional:

- Tag: `linux-v8.0.1.2FE`
- RPM: `pulse-platform-8.0.1.2-1.x86_64.rpm`
- Portable: `pulse-platform-8.0.1.2FE-linux-x64.tar.gz`
- Manifest: `SHA256SUMS`

The updater ignores DE and AE releases even if an incorrectly named RPM is attached. It downloads only the exact FE RPM for the running architecture, verifies SHA-256, saves it in Downloads, and then asks the desktop to open it. Installation remains a separate user-approved package-manager action.

The RPM installs the application under `/opt/pulse-platform`, the launcher under `/usr/bin/pulse-platform`, and desktop/icon integration under `/usr/share`. User settings and data remain under `~/.config/Pulse Platform` and `~/.local/share/Pulse Platform`.
