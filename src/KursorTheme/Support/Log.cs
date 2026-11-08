using System.Text;

namespace KursorTheme.Support;

internal static class Log
{
    private static readonly object Gate = new();

    public static string Folder => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "KursorTheme",
        "logs");

    public static string FilePath => Path.Combine(Folder, "app.log");

    public static void Info(string message) => Write("INFO", message);

    public static void Error(Exception exception) => Write("ERROR", exception.ToString());

    private static void Write(string level, string message)
    {
        try
        {
            Directory.CreateDirectory(Folder);
            lock (Gate)
            {
                var file = new FileInfo(FilePath);
                if (file.Exists && file.Length > 1_000_000) file.Delete();
                File.AppendAllText(
                    FilePath,
                    $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}{Environment.NewLine}",
                    Encoding.UTF8);
            }
        }
        catch
        {
            // Logging must never break the app.
        }
    }
}
