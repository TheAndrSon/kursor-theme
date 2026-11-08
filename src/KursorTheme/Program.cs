using KursorTheme.App;
using KursorTheme.Config;
using KursorTheme.Support;

namespace KursorTheme;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        using var mutex = new Mutex(true, @"Local\KursorTheme_SingleInstance", out var isFirstInstance);
        if (!isFirstInstance)
        {
            MessageBox.Show(
                "Kursor Theme уже запущен.\nИконка приложения находится в системном трее.",
                "Kursor Theme",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, args) => Log.Error(args.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            Log.Error(args.ExceptionObject as Exception ?? new Exception("Unknown unhandled exception"));

        using var hub = new SettingsHub(SettingsStore.Load());
        var openSettings = Environment.GetCommandLineArgs()
            .Any(argument => argument.Equals("--settings", StringComparison.OrdinalIgnoreCase));
        using var context = new TrayContext(hub, openSettings);
        Application.Run(context);
    }
}
