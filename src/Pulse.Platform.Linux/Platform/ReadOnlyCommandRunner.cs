using System.ComponentModel;
using System.Diagnostics;

namespace Pulse.Platform.Linux.Platform;

public sealed record ReadOnlyCommandResult(
    bool Started,
    bool TimedOut,
    int ExitCode,
    string StandardOutput,
    string StandardError);

public interface IReadOnlyCommandRunner
{
    Task<ReadOnlyCommandResult> RunAsync(
        string executable,
        IReadOnlyList<string> arguments,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default);
}

public sealed class ReadOnlyCommandRunner : IReadOnlyCommandRunner
{
    public async Task<ReadOnlyCommandResult> RunAsync(
        string executable,
        IReadOnlyList<string> arguments,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = executable,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = new Process { StartInfo = startInfo };
        try
        {
            if (!process.Start())
            {
                return new(false, false, -1, string.Empty, "The process did not start.");
            }
        }
        catch (Win32Exception ex)
        {
            return new(false, false, -1, string.Empty, ex.Message);
        }

        var standardOutput = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var standardError = process.StandardError.ReadToEndAsync(cancellationToken);
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(timeout ?? TimeSpan.FromSeconds(8));

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
                // The process exited between the timeout and kill request.
            }

            await process.WaitForExitAsync(CancellationToken.None);
        }

        cancellationToken.ThrowIfCancellationRequested();
        return new(true, timedOut, timedOut ? -1 : process.ExitCode,
            await standardOutput, await standardError);
    }
}
