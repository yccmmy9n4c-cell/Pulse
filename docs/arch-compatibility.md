# Arch compatibility boundary

| System | AE result | Reason |
| --- | --- | --- |
| Arch Linux x64 desktop | Supported candidate | Primary AE validation target |
| Arch with GNOME/KDE/XFCE and X11/Wayland | Pending physical verification | Shared UI support exists; desktop behavior needs evidence |
| Manjaro, EndeavourOS, Garuda, other derivatives | Detected, assessment disabled | `ID_LIKE=arch` is not proof of compatible lifecycle or tooling |
| Debian/Ubuntu/Mint | Unsupported by AE | Use the DE release |
| Fedora/RHEL | Unsupported by AE | Use FE only where explicitly verified |

Compatibility notes describe Pulse coverage and never reduce health scores. Missing optional tools are reported as coverage notes and never installed automatically.
