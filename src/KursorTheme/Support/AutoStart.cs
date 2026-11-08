using KursorTheme.Interop;

namespace KursorTheme.Support;

internal static class AutoStart
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "KursorTheme";

    public static bool IsEnabled()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RunKey);
            var value = key?.GetValue(ValueName) as string;
            return !string.IsNullOrWhiteSpace(value);
        }
        catch
        {
            return false;
        }
    }

    public static void Set(bool enabled)
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RunKey, writable: true)
                            ?? Microsoft.Win32.Registry.CurrentUser.CreateSubKey(RunKey);
            if (key == null) return;

            if (enabled)
            {
                var path = Environment.ProcessPath ?? Application.ExecutablePath;
                if (!string.IsNullOrWhiteSpace(path)) key.SetValue(ValueName, $"\"{path}\"");
            }
            else
            {
                key.DeleteValue(ValueName, throwOnMissingValue: false);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex);
        }
    }
}

internal static class FullscreenDetector
{
    public static bool IsForegroundFullscreen()
    {
        var hwnd = NativeMethods.GetForegroundWindow();
        if (hwnd == IntPtr.Zero) return false;

        NativeMethods.GetWindowThreadProcessId(hwnd, out var pid);
        if (pid == (uint)Environment.ProcessId) return false;

        var builder = new System.Text.StringBuilder(128);
        NativeMethods.GetClassName(hwnd, builder, builder.Capacity);
        var className = builder.ToString();
        if (className is "Progman" or "WorkerW" or "Shell_TrayWnd" or "Windows.UI.Core.CoreWindow" or "MultitaskingViewFrame")
            return false;

        if (!NativeMethods.GetWindowRect(hwnd, out var windowRect)) return false;

        var monitor = NativeMethods.MonitorFromWindow(hwnd, NativeMethods.MONITOR_DEFAULTTONEAREST);
        var info = new NativeMethods.MONITORINFO { cbSize = System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.MONITORINFO>() };
        if (!NativeMethods.GetMonitorInfo(monitor, ref info)) return false;

        var screen = info.rcMonitor;
        return windowRect.Left <= screen.Left + 1
               && windowRect.Top <= screen.Top + 1
               && windowRect.Right >= screen.Right - 1
               && windowRect.Bottom >= screen.Bottom - 1;
    }
}
