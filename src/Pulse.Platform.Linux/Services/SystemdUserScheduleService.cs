using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using Pulse.Platform.Linux.Platform;

namespace Pulse.Platform.Linux.Services;

public enum UserScheduleState
{
    Enabled,
    Disabled,
    Unavailable
}

public sealed record UserScheduleStatus(UserScheduleState State, string Message);
public sealed record UserScheduleOperationResult(bool Succeeded, string Message);
public sealed record SystemdUserCommandResult(bool Started, bool TimedOut, int ExitCode, string Output, string Error);

public interface ISystemdUserCommandRunner
{
    Task<SystemdUserCommandResult> RunAsync(
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken = default);
}

public sealed class SystemdUserCommandRunner : ISystemdUserCommandRunner
{
    public async Task<SystemdUserCommandResult> RunAsync(
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken = default)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "systemctl",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add("--user");
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = new Process { StartInfo = startInfo };
        try
        {
            if (!process.Start())
            {
                return new(false, false, -1, string.Empty, "systemctl did not start.");
            }
        }
        catch (Win32Exception ex)
        {
            return new(false, false, -1, string.Empty, ex.Message);
        }

        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(TimeSpan.FromSeconds(12));
        var timedOut = false;
        try
        {
            await process.WaitForExitAsync(timeoutSource.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            timedOut = true;
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException)
            {
                // The command exited between the timeout and kill request.
            }

            await process.WaitForExitAsync(CancellationToken.None);
        }

        cancellationToken.ThrowIfCancellationRequested();
        return new(true, timedOut, timedOut ? -1 : process.ExitCode,
            await outputTask, await errorTask);
    }
}

public sealed class SystemdUserScheduleService
{
    public const string ServiceUnitName = "pulse-platform-assessment.service";
    public const string TimerUnitName = "pulse-platform-assessment.timer";

    private readonly string _unitDirectory;
    private readonly ISystemdUserCommandRunner _commandRunner;

    public SystemdUserScheduleService(
        string? unitDirectory = null,
        ISystemdUserCommandRunner? commandRunner = null)
    {
        _unitDirectory = unitDirectory ?? LinuxUserPaths.SystemdUserDirectory;
        _commandRunner = commandRunner ?? new SystemdUserCommandRunner();
    }

    public async Task<UserScheduleStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var result = await _commandRunner.RunAsync(["is-enabled", TimerUnitName], cancellationToken);
        if (!result.Started || result.TimedOut)
        {
            return new(UserScheduleState.Unavailable,
                "User scheduling is unavailable because systemd --user could not be reached.");
        }

        if (result.ExitCode == 0 && result.Output.Trim().Equals("enabled", StringComparison.OrdinalIgnoreCase))
        {
            return new(UserScheduleState.Enabled, "Weekly assessments are enabled for this user.");
        }

        var combined = $"{result.Output} {result.Error}";
        if (combined.Contains("disabled", StringComparison.OrdinalIgnoreCase) ||
            combined.Contains("not-found", StringComparison.OrdinalIgnoreCase) ||
            combined.Contains("No such file", StringComparison.OrdinalIgnoreCase))
        {
            return new(UserScheduleState.Disabled, "Weekly assessments are not enabled.");
        }

        return new(UserScheduleState.Unavailable,
            $"Pulse could not determine the user schedule state. {result.Error.Trim()}");
    }

    public async Task<UserScheduleOperationResult> EnableAsync(
        string executablePath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(executablePath) || executablePath.Contains('\n') ||
            executablePath.Contains('\r') || !Path.IsPathFullyQualified(executablePath) ||
            !File.Exists(executablePath))
        {
            return new(false, "Pulse could not identify its installed executable, so no schedule was created.");
        }

        Directory.CreateDirectory(_unitDirectory);
        var servicePath = Path.Combine(_unitDirectory, ServiceUnitName);
        var timerPath = Path.Combine(_unitDirectory, TimerUnitName);
        try
        {
            await WriteAtomicallyAsync(servicePath, BuildServiceUnit(executablePath), cancellationToken);
            await WriteAtomicallyAsync(timerPath, BuildTimerUnit(), cancellationToken);

            var reload = await _commandRunner.RunAsync(["daemon-reload"], cancellationToken);
            if (!Succeeded(reload))
            {
                RollBackUnitFiles(servicePath, timerPath);
                return new(false, ExplainFailure("reload the user service manager", reload));
            }

            var enable = await _commandRunner.RunAsync(["enable", "--now", TimerUnitName], cancellationToken);
            if (!Succeeded(enable))
            {
                await _commandRunner.RunAsync(["disable", "--now", TimerUnitName], CancellationToken.None);
                RollBackUnitFiles(servicePath, timerPath);
                await _commandRunner.RunAsync(["daemon-reload"], CancellationToken.None);
                return new(false, ExplainFailure("enable the weekly timer", enable));
            }

            return new(true, "Weekly read-only assessments are now enabled for this user.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            RollBackUnitFiles(servicePath, timerPath);
            return new(false, $"Pulse could not create the user schedule. {ex.Message}");
        }
    }

    public async Task<UserScheduleOperationResult> DisableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var disable = await _commandRunner.RunAsync(["disable", "--now", TimerUnitName], cancellationToken);
            if (!Succeeded(disable) && !IsAlreadyAbsent(disable))
            {
                return new(false, ExplainFailure("disable the weekly timer", disable));
            }

            File.Delete(Path.Combine(_unitDirectory, TimerUnitName));
            File.Delete(Path.Combine(_unitDirectory, ServiceUnitName));
            var reload = await _commandRunner.RunAsync(["daemon-reload"], cancellationToken);
            return Succeeded(reload)
                ? new(true, "Weekly assessments are disabled and Pulse's user schedule files were removed.")
                : new(false, ExplainFailure("reload the user service manager after removing the schedule", reload));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new(false, $"Pulse could not remove the user schedule. {ex.Message}");
        }
    }

    private static string BuildServiceUnit(string executablePath)
    {
        var escapedPath = executablePath.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal);
        return $"""
            [Unit]
            Description=Pulse Supernova Linux read-only assessment
            Documentation=https://github.com/yccmmy9n4c-cell/Pulse

            [Service]
            Type=oneshot
            ExecStart="{escapedPath}" --assess-once
            Nice=10
            NoNewPrivileges=true
            UMask=0077

            """;
    }

    private static string BuildTimerUnit() => $"""
        [Unit]
        Description=Run a weekly Pulse Supernova Linux read-only assessment

        [Timer]
        OnCalendar=weekly
        Persistent=true
        RandomizedDelaySec=30m
        Unit={ServiceUnitName}

        [Install]
        WantedBy=timers.target

        """;

    private static async Task WriteAtomicallyAsync(
        string path,
        string contents,
        CancellationToken cancellationToken)
    {
        var temporaryPath = $"{path}.{Guid.NewGuid():N}.tmp";
        try
        {
            await File.WriteAllTextAsync(temporaryPath, contents, new UTF8Encoding(false), cancellationToken);
            File.Move(temporaryPath, path, true);
            if (OperatingSystem.IsLinux())
            {
                File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            }
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private static bool Succeeded(SystemdUserCommandResult result) =>
        result.Started && !result.TimedOut && result.ExitCode == 0;

    private static bool IsAlreadyAbsent(SystemdUserCommandResult result) =>
        result.Started && !result.TimedOut &&
        $"{result.Output} {result.Error}".Contains("not loaded", StringComparison.OrdinalIgnoreCase);

    private static string ExplainFailure(string action, SystemdUserCommandResult result)
    {
        var detail = result.TimedOut ? "The command timed out." : result.Error.Trim();
        return $"Pulse could not {action}. No elevation was attempted. {detail}";
    }

    private static void RollBackUnitFiles(string servicePath, string timerPath)
    {
        File.Delete(timerPath);
        File.Delete(servicePath);
    }
}
