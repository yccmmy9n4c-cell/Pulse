# Pulse Linux Beta 0.0.0.28

This release adds Hardware Intelligence and corrects Performance Intelligence coverage handling while preserving the accepted Linux Assessment layout and updater publication contract.

- Adds a dedicated six-card **Hardware Intelligence** page covering processor identity, installed memory, firmware/system identity, battery condition, graphics hardware, and virtualization posture.
- Adds exact Hardware Intelligence parity to the Dashboard as the eighth Linux intelligence domain.
- Uses only local read-only evidence from `/proc`, `/sys`, DMI, DRM, power-supply sysfs, and `systemd-detect-virt`.
- Identifies materially reduced readable battery capacity conservatively while treating processor, memory, firmware, graphics, and virtualization as context rather than faults.
- Adds a user-directed **Open Power Settings** action when battery evidence deserves review; Pulse never changes charging or power policy.
- Stops unavailable evidence from deducting health points. Coverage limitations remain visible and prevent an `Optimized` label, but only actual `REVIEW` evidence lowers the score.
- Falls back from `/proc/pressure/{cpu,memory,io}` to cgroup v2 `*.pressure` files when available.
- Detects `CONFIG_PSI=y` with `CONFIG_PSI_DEFAULT_DISABLED=y` and explains that PSI is available but requires the user-selected `psi=1` boot setting.
- Preserves clear explanations for kernels built without PSI, systems using `psi=0`, and systems exposing no PSI interface.
- Expands the default assessment from 34 to 40 isolated evidence providers with regression coverage for hardware parsing, cgroup PSI fallback, default-disabled PSI, and coverage-neutral scoring.

Pulse does not install drivers, flash firmware, change UEFI settings, modify the bootloader, alter charging limits, enable virtualization, control fans, or change CPU/power policy.
