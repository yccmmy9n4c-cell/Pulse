# GitHub x64 build workflow

`Pulse Linux x64 Build` produces the first executable Linux artifacts without requiring a local .NET SDK.

## Run it

1. Place the project contents at the repository root, including the hidden `.github` folder.
2. Push the changes to the `main` branch.
3. In GitHub, open **Actions** and select **Pulse Linux x64 Build**.
4. Select **Run workflow**, enter `0.0.0.25`, and start the run.
5. Open the completed run and download `pulse-linux-beta-0.0.0.25-linux-x64` from **Artifacts**.
6. The successful manual run on `main` also creates or updates the `linux-v0.0.0.25` GitHub prerelease with the three updater assets.

## Expected artifact contents

- `pulse-platform_0.0.0.25_amd64.deb`
- `pulse-platform-0.0.0.25-linux-x64.tar.gz`
- `SHA256SUMS`
- `gui-launch.log`
- `pulse-standard-shell.png`

GitHub wraps those files in its own download ZIP. Extract that ZIP before transferring the `.deb` or `.tar.gz` to the Linux test computer.

A normal push validates the source but does not publish release assets. The manual `Run workflow` path publishes only after every build, smoke-test, package, install, render, and checksum gate succeeds. The in-app updater reads these GitHub Release assets, not the temporary Actions artifact.

## Gates enforced by the workflow

- Explicit `linux-x64` restore for both project graphs, direct no-restore MSBuild Build/Publish targets, and direct execution of the built smoke-test and headless DLLs
- Debian/Ubuntu/Linux Mint support-boundary smoke tests
- Fedora and unverified-derivative rejection tests
- Portable archive integrity check
- Debian control and payload inspection
- SHA-256 verification
- Installation of the generated `.deb` on the Ubuntu build runner
- Visible Pulse-window detection under Xvfb
- Fatal-output rejection and non-blank screenshot validation
- Assessment snapshot, HTML-escaping, activity-log, and latest-report smoke tests
- User-unit generation, enable/disable, no-elevation, and headless assessment tests
- Pulse Standard navigation/data smoke tests and an automated Aurora shell screenshot

The Xvfb check proves that the installed executable creates a visible, non-blank window without fatal startup output on the build host. It does not replace visual inspection on the physical Debian/Ubuntu desktop.
