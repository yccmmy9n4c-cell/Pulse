namespace Pulse.Platform.Linux.Platform;

public static class LinuxUserPaths
{
    public static string SettingsDirectory => Resolve("XDG_CONFIG_HOME", ".config", "Pulse Platform");
    public static string DataDirectory => Resolve("XDG_DATA_HOME", Path.Combine(".local", "share"), "Pulse Platform");
    public static string SystemdUserDirectory => Path.Combine(ResolveRoot("XDG_CONFIG_HOME", ".config"), "systemd", "user");

    private static string Resolve(string xdgVariable, string homeRelativeFallback, string productDirectory)
    {
        return Path.Combine(ResolveRoot(xdgVariable, homeRelativeFallback), productDirectory);
    }

    private static string ResolveRoot(string xdgVariable, string homeRelativeFallback)
    {
        var xdgRoot = Environment.GetEnvironmentVariable(xdgVariable);
        return !string.IsNullOrWhiteSpace(xdgRoot)
            ? xdgRoot
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), homeRelativeFallback);
    }
}
