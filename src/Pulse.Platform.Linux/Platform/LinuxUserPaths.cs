namespace Pulse.Platform.Linux.Platform;

public static class LinuxUserPaths
{
    public static string SettingsDirectory => Resolve("XDG_CONFIG_HOME", ".config", "Pulse Platform");
    public static string DataDirectory => Resolve("XDG_DATA_HOME", Path.Combine(".local", "share"), "Pulse Platform");

    private static string Resolve(string xdgVariable, string homeRelativeFallback, string productDirectory)
    {
        var xdgRoot = Environment.GetEnvironmentVariable(xdgVariable);
        var root = !string.IsNullOrWhiteSpace(xdgRoot)
            ? xdgRoot
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), homeRelativeFallback);
        return Path.Combine(root, productDirectory);
    }
}
