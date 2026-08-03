# GitHub x64 build workflow

`Pulse Linux x64 Build` produces the first executable Linux artifacts without requiring a local .NET SDK.

## Run it

1. Place the project contents at the repository root, including the hidden `.github` folder.
2. Push the changes to the `main` branch.
3. In GitHub, open **Actions** and select **Pulse Linux x64 Build**.
4. Select **Run workflow**, enter `0.0.0.6`, and start the run.
5. Open the completed run and download `pulse-linux-beta-0.0.0.6-linux-x64` from **Artifacts**.

## Expected artifact contents

- `pulse-platform_0.0.0.6_amd64.deb`
- `pulse-platform-0.0.0.6-linux-x64.tar.gz`
- `SHA256SUMS`
- `gui-launch.log`
- `pulse-standard-shell.png`

GitHub wraps those files in its own download ZIP. Extract that ZIP before transferring the `.deb` or `.tar.gz` to the Linux test computer.

## Gates enforced by the workflow

- .NET restore and warnings-as-errors compilation
- Debian/Ubuntu/Linux Mint support-boundary smoke tests
- Fedora and unverified-derivative rejection tests
- Portable archive integrity check
- Debian control and payload inspection
- SHA-256 verification
- Eight-second Avalonia launch-survival check under Xvfb
- Assessment snapshot, HTML-escaping, activity-log, and latest-report smoke tests
- User-unit generation, enable/disable, no-elevation, and headless assessment tests
- Pulse Standard navigation/data smoke tests and an automated Aurora shell screenshot

The Xvfb check proves that the executable starts and remains running on the build host. It does not replace visual inspection on the physical Debian/Ubuntu desktop.
