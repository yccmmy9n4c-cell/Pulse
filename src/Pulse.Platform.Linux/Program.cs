using Avalonia;
using Avalonia.Media;
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
        .With(new FontManagerOptions
        {
            // Minimal Arch installations and CI containers do not always expose a
            // platform default. Keep Pulse startup deterministic even when the
            // native font manager returns null.
            DefaultFamilyName = "DejaVu Sans"
        })
        .LogToTrace();
}
