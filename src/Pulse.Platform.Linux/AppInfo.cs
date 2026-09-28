using System.Reflection;
using System.Runtime.InteropServices;

namespace Pulse.Platform.Linux;

public static class AppInfo
{
    public const string ProductName = "Pulse Supernova Linux";
    public const string ReleaseChannel = "Development";
    public const string ReleaseName = "Debian Family Development Candidate";
    public const string EditionCode = "DE";

    public static string Version =>
        Assembly.GetEntryAssembly()?.GetName().Version?.ToString(4) ?? "8.0.3.0";

    public static string BuildId =>
        $"linux-{EditionCode.ToLowerInvariant()}-{RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant()}-{Version}";

    public static string DisplayVersion => $"{Version}{EditionCode}";

    public static string VersionLine => $"{ProductName} • {ReleaseChannel} {DisplayVersion}";
}
