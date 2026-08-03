using System.Reflection;
using System.Runtime.InteropServices;

namespace Pulse.Platform.Linux;

public static class AppInfo
{
    public const string ProductName = "Pulse Supernova Linux";
    public const string ReleaseChannel = "Beta";
    public const string ReleaseName = "Nebula Intelligence";

    public static string Version =>
        Assembly.GetEntryAssembly()?.GetName().Version?.ToString(4) ?? "0.0.0.17";

    public static string BuildId =>
        $"linux-{RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant()}-{Version}";

    public static string VersionLine => $"{ProductName} • {ReleaseChannel} {Version}";
}
