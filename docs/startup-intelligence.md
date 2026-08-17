# Startup Intelligence

Pulse Linux Beta 0.0.0.29 adds a dedicated Startup Intelligence page and a matching Dashboard domain. It translates six read-only Linux evidence sources into plain language without assuming that a long boot or an enabled startup item is automatically a fault.

| Card | Evidence | Interpretation |
| --- | --- | --- |
| Boot Duration | `systemd-analyze time` | Informational duration baseline for the current boot |
| Critical Startup Chain | `systemd-analyze critical-chain --no-pager` | Leading dependency/timing entries for comparison across boots |
| Failed System Services | `systemctl --failed` | Requests review only when system services are currently failed |
| Failed User Services | `systemctl --user --failed` | Requests review only when signed-in-user services are currently failed |
| Desktop Autostart | `/etc/xdg/autostart`, `~/.config/autostart` | Counts effective enabled entries and disabled user overrides |
| Enabled User Services and Timers | `systemctl --user list-unit-files --state=enabled` | Counts enabled user services, timers, and other units as context |

## Plain-language rules

- Boot timing and critical-chain entries are baselines, not proof of a defective service.
- Autostart entries and enabled user units are normal system context. Pulse advises review only when the user recognizes something as unwanted or when startup is visibly affected.
- Failed system or user services retain their accepted Reliability Intelligence states and are the only startup sources that can directly lower the Startup score.
- Missing optional commands or unreadable session data are shown as unavailable coverage and do not reduce health.

## Guided review

When desktop-autostart or enabled-user-unit context is selected for review, Pulse tries an installed Startup Applications utility for Cinnamon, GNOME, MATE, or KDE. If none is available, Pulse opens the detailed in-app Assessment guidance.

## Safety boundary

Startup Intelligence never enables, disables, starts, stops, restarts, masks, removes, or resets a systemd unit. It never creates, edits, or deletes a desktop-autostart file. It does not request elevation. Any external settings utility remains responsible for its own confirmation and authentication.

## Physical validation

1. Confirm all six cards render and the Dashboard Startup row matches the page score and state.
2. Compare Boot Duration and Critical Startup Chain with the corresponding `systemd-analyze` commands.
3. Confirm Desktop Autostart reflects the desktop's Startup Applications list and user-disabled overrides.
4. Confirm the guided action opens the installed desktop utility or falls back to Assessment guidance.
5. Confirm no unit or desktop entry changes after refreshing Pulse.
