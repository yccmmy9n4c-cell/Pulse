# Performance Intelligence

Pulse Linux Beta 0.0.0.25 adds a dedicated six-card Performance Intelligence domain for supported Debian-family desktops.

| Card | Evidence | Meaning |
| --- | --- | --- |
| Sustained System Load | `/proc/loadavg` | One-, five-, and fifteen-minute runnable/work-waiting load relative to logical processors |
| Available Memory | `/proc/meminfo` `MemAvailable` | Memory Linux estimates can satisfy without swapping; filesystem cache is not misreported as simply consumed |
| CPU Pressure | `/proc/pressure/cpu` | Recent time tasks waited for CPU execution |
| Memory Pressure | `/proc/pressure/memory` | Recent partial and full memory stalls |
| I/O Pressure | `/proc/pressure/io` | Recent partial and full storage-I/O stalls |
| Thermal Posture | `/sys/class/thermal/thermal_zone*/temp` | Hottest readable thermal zone and sensor count |

The Dashboard Performance score uses these exact six provider IDs. PSI and thermal coverage may be unavailable on some otherwise supported hardware; that is explained as a coverage limit rather than proof of a fault.

Beta 0.0.0.28 reads each PSI resource from `/proc/pressure` first and falls back to the matching cgroup v2 `cpu.pressure`, `memory.pressure`, or `io.pressure` file. If neither exists, Pulse checks readable kernel configuration and command-line context so it can distinguish PSI built but disabled by default, an explicit `psi=0`, a kernel built without PSI, and an otherwise unavailable interface. Pulse never modifies the bootloader or kernel settings.

## Interpretation boundary

- A single reading is context. Repeated pressure accompanied by visible slowdown is more meaningful.
- Pulse uses fifteen-minute load for sustained-load review and normalizes it to logical processors.
- Pulse uses `MemAvailable`, avoiding the common mistake of treating useful Linux filesystem cache as unavailable memory.
- Conservative thresholds request review only for sustained pressure or very low available memory; they are Pulse guidance thresholds, not manufacturer failure declarations.
- Unavailable optional signals remain visible as coverage limitations but do not subtract health points. A domain with healthy readable evidence and incomplete optional coverage is **Healthy**, not **Optimized**.

## Safe action boundary

**Open System Monitor** tries `gnome-system-monitor`, `mate-system-monitor`, and `plasma-systemmonitor`. If none is installed, Pulse opens the detailed assessment evidence. Pulse never ends a process, clears memory or caches, renices work, changes CPU governors, alters power policy, or controls fans.
