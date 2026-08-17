# Linux Compatibility

Pulse Linux Release 8.0.1.2DE restores the Pulse Standard Compatibility page for the Debian-family edition.

| Card | Evidence | User meaning |
| --- | --- | --- |
| Distribution Support | `/etc/os-release` through the strict support detector | Debian, Ubuntu, and Linux Mint are verified; compatible derivatives remain unverified until tested |
| Architecture | .NET process architecture | linux-x64 is the accepted primary package target; linux-arm64 remains a later acceptance target |
| Desktop Environment | Standard desktop session variables | Recognizes GNOME, Cinnamon, MATE, KDE/Plasma, XFCE, and LXQt context |
| Display Session | `XDG_SESSION_TYPE`, `WAYLAND_DISPLAY`, `DISPLAY` | Distinguishes interactive X11/Wayland sessions from expected headless assessments |
| User-Service Readiness | `systemctl --user is-system-running` | Confirms whether the signed-in user manager is reachable for explicitly approved scheduling |
| Intelligence Tool Coverage | Conventional executable paths | Reports core and optional native evidence coverage without installing anything |

Compatibility is not a health domain and does not add another Dashboard score. Notes remain visible in the Compatibility page, Assessment, history, and reports without reducing Executive Health.

The DE package continues to reject Fedora/RHEL, Arch, BSD, and unrelated systems. That is intentional: FE and AE will use separate native providers and packages instead of pretending Debian commands apply everywhere.

Pulse never installs missing tools, changes desktop/display sessions, modifies user services, or substitutes a package built for another architecture.
