using System.Runtime.InteropServices;

namespace Pulse.Platform.Linux.Providers;

public sealed class ProcEvidenceProvider : ILinuxEvidenceProvider
{
    public string Id => "linux.proc-foundation";

    public async Task<EvidenceResult> CollectAsync(CancellationToken cancellationToken = default)
    {
        var cpuLines = await File.ReadAllLinesAsync("/proc/cpuinfo", cancellationToken);
        var model = cpuLines.FirstOrDefault(line => line.StartsWith("model name", StringComparison.OrdinalIgnoreCase))?
            .Split(':', 2)[1].Trim() ?? RuntimeInformation.ProcessArchitecture.ToString();

        var memoryLines = await File.ReadAllLinesAsync("/proc/meminfo", cancellationToken);
        var totalKbText = memoryLines.FirstOrDefault(line => line.StartsWith("MemTotal:", StringComparison.OrdinalIgnoreCase))?
            .Split(' ', StringSplitOptions.RemoveEmptyEntries).ElementAtOrDefault(1);
        var totalGiB = long.TryParse(totalKbText, out var totalKb) ? totalKb / 1024d / 1024d : 0;

        var kernel = await File.ReadAllTextAsync("/proc/sys/kernel/osrelease", cancellationToken);
        var summary = $"Kernel {kernel.Trim()}\n{model}\n{totalGiB:F1} GiB installed memory";
        return new(Id, "Kernel and hardware foundation", summary,
            "This is baseline hardware and kernel evidence. Pulse has not changed performance, power, or kernel settings.");
    }
}
