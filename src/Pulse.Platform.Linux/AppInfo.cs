using System.Reflection;
using System.Runtime.InteropServices;

namespace Pulse.Platform.Linux;

public static class AppInfo
{
    public const string ProductName = "Pulse Supernova Linux";
    public const string ReleaseChannel = "Release";
    public const string ReleaseName = "Fedora Family Release";
    public const string EditionCode = "FE";

    public static string Version =>
        Assembly.GetEntryAssembly()?.GetName().Version?.ToString(4) ?? "8.0.1.2";

    public static string BuildId =>
        $"linux-{EditionCode.ToLowerInvariant()}-{RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant()}-{Version}";

    public static string DisplayVersion => $"{Version}{EditionCode}";

    public static string VersionLine => $"{ProductName} • {ReleaseChannel} {DisplayVersion}";
}
