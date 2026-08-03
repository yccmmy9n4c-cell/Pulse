# Piece 4 assessment history and reporting

Pulse Linux Beta 0.0.0.4 turns the Piece 3 read-only evidence into durable, user-owned records.

## Saved artifacts

Every successful assessment writes three local records beneath `~/.local/share/Pulse Platform` (or the matching `XDG_DATA_HOME` location):

| Record | Location | Purpose |
| --- | --- | --- |
| JSON snapshot | `Assessments/pulse-assessment-<timestamp>.json` | Structured platform and evidence data for later Pulse features |
| HTML report | `Reports/pulse-assessment-<timestamp>.html` | Branded, plain-language report that opens in the default browser |
| Activity log | `Logs/activity.jsonl` | One compact `assessment.saved` event per completed archive operation |

Snapshot and report writes use a temporary file in the destination directory and an atomic rename. Dynamic report content is HTML-encoded before it is written. The activity log contains paths and result counts, not a second copy of the evidence.

## User experience

- **Run Read-Only Assessment** still performs the ten fault-isolated Piece 3 checks.
- Pulse saves the completed result without requesting elevation.
- **Open Latest Report** opens the newest report through the desktop's configured browser.
- On a later launch, Pulse rediscovers the newest saved report and enables the button.
- A report-write failure is shown separately and does not misrepresent the evidence collection as failed.

## Test coverage

The Linux smoke-test executable now verifies creation of all three records, JSON readability, HTML escaping, the activity event, and latest-report rediscovery. GitHub Actions runs those checks before packaging.
