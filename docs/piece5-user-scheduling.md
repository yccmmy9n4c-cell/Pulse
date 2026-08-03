# Piece 5 user-approved scheduling

Pulse Linux Beta 0.0.0.5 adds optional weekly assessments through the logged-in user's `systemd --user` service manager.

## Safety boundary

- Scheduling is off by default.
- The first button press explains the schedule; a second **Confirm Weekly Schedule** press is required.
- Pulse never invokes `sudo`, `pkexec`, Polkit, or a system-wide service.
- Only the current user's Pulse service and timer files are created.
- **Disable Weekly Assessments** stops the timer and removes those two files.
- The scheduled command uses the installed Pulse executable with `--assess-once`; it does not start Avalonia or require a display.
- The service uses `NoNewPrivileges=true` and a user-only file-creation mask.

## User units

Pulse writes these files beneath `~/.config/systemd/user` (or the matching `XDG_CONFIG_HOME`):

- `pulse-platform-assessment.service`
- `pulse-platform-assessment.timer`

The timer uses `OnCalendar=weekly`, `Persistent=true`, and a 30-minute randomized delay. A missed run can occur after the user service manager starts again, while the randomized delay avoids making every installation assess at exactly the same moment.

The oneshot service creates the same JSON snapshot, HTML report, and activity-log event as an interactive assessment under `~/.local/share/Pulse Platform`.
Pulse restricts its assessment directories and generated records to the current user on Linux.

## Commands for validation

After enabling the schedule in Pulse:

```bash
systemctl --user status pulse-platform-assessment.timer
systemctl --user list-timers pulse-platform-assessment.timer
```

To exercise the headless path immediately without changing the timer:

```bash
/opt/pulse-platform/pulse-platform --assess-once
```

The GitHub workflow runs this headless command in an isolated data directory and requires both its JSON snapshot and HTML report before packaging.
