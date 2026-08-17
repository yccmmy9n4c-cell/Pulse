# Fedora compatibility boundary

| System | FE result | Reason |
| --- | --- | --- |
| Fedora Workstation x64 | Supported candidate | Primary FE validation target |
| Fedora KDE/Cinnamon/MATE/XFCE spins | Pending physical verification | Fedora ID is accepted; desktop behavior still needs evidence |
| Nobara and other Fedora derivatives | Detected, assessment disabled | `ID_LIKE=fedora` is not proof of compatibility |
| RHEL, CentOS Stream, Rocky, Alma | Not verified | Different lifecycle, repositories, policy, and support expectations |
| Debian/Ubuntu/Mint | Unsupported by FE | Use the DE release |
| Arch and derivatives | Unsupported by FE | Use the future AE release |

Both X11 and Wayland are detected. Compatibility notes describe coverage and never reduce health scores. Missing optional tools reduce evidence coverage but are never installed automatically.
