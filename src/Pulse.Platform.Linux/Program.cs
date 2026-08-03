using Avalonia;
using Pulse.Platform.Linux.Services;

namespace Pulse.Platform.Linux;

internal static class Program
{
    [STAThread]
    public static async Task<int> Main(string[] args)
    {
        if (args.Contains("--assess-once", StringComparer.Ordinal))
        {
            return await new HeadlessAssessmentRunner().RunAsync();
        }

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        return 0;
    }

    public static AppBuilder BuildAvaloniaApp() => AppBuilder
        .Configure<App>()
        .UsePlatformDetect()
        .WithInterFont()
        .LogToTrace();
}
