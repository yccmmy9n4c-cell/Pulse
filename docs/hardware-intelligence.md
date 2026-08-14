# Hardware Intelligence

Pulse Linux Beta 0.0.0.28 adds a dedicated six-card Hardware Intelligence domain for supported Debian-family desktops.

| Card | Primary evidence | Interpretation |
| --- | --- | --- |
| Processor | `/proc/cpuinfo` | Model and exposed logical/core topology; informational context |
| Installed Memory | `/proc/meminfo` `MemTotal` | Physical memory capacity; current availability remains in Performance Intelligence |
| Firmware and System Identity | `/sys/class/dmi/id` | Readable system vendor/product and firmware identity; informational context |
| Battery Condition | `/sys/class/power_supply` | Presence, status, charge, and full-versus-design capacity when exposed |
| Graphics Hardware | `/sys/class/drm/card*/device` | DRM adapter identifiers and active kernel-driver name when readable |
| Virtualization Posture | `systemd-detect-virt --vm` | Physical versus virtual-machine context |

## Health boundary

- Processor, installed capacity, firmware identity, graphics identity, and virtualization are context, not health judgments.
- No battery is normal on desktop hardware and is informational rather than unavailable.
- Battery full-charge capacity below 60% of the exposed design value requests review; 60–79% is informational and 80% or above is healthy.
- Battery estimates vary with calibration, firmware, charge cycle, and temperature. Pulse does not claim a battery has failed from one reading.
- Unavailable optional hardware evidence remains visible as a coverage limitation and does not deduct health points.

## Safe-action boundary

When battery evidence deserves review, **Open Power Settings** tries the installed GNOME, MATE, or KDE settings application. Pulse never installs a driver, flashes firmware, changes UEFI settings, modifies charging thresholds, enables virtualization, changes CPU governors, or applies power-policy changes.
