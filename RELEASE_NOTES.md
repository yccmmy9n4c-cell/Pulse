# Pulse Linux Beta 0.0.0.27

This release makes updater-visible Linux publishing automatic and unambiguous while preserving the accepted 0.0.0.26 Linux Assessment layout and all 34 evidence providers.

- Automatically creates or refreshes the matching GitHub prerelease after every successful `main` push build as well as a manual workflow run.
- Keeps pull-request builds validation-only and publishes nothing until every package, smoke-test, checksum, install, and GUI-render gate succeeds.
- Verifies the published release contains the architecture-specific `.deb`, portable `.tar.gz`, and `SHA256SUMS`; a missing updater asset now fails the workflow visibly.
- Expands the release query from 30 to 100 records and requests fresh GitHub metadata.
- Distinguishes an installed development build that is newer than GitHub's newest published compatible package and never offers a downgrade.
- Adds deterministic coverage for the installed-newer-than-published case that exposed the v24–v26 publication gap.

The accepted 0.0.0.26 Information, Healthy, and Guidance navigation remains unchanged. Assessment collection, Dashboard scoring, history, reports, scheduling, and safe native actions remain intact.
