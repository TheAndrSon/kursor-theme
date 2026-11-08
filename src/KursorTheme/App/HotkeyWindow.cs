using KursorTheme.Interop;

namespace KursorTheme.App;

internal sealed class HotkeyWindow : NativeWindow, IDisposable
{
    private readonly List<int> _registered = new();
    private bool _disposed;

    public event Action<int>? Pressed;

    public HotkeyWindow()
    {
        CreateHandle(new CreateParams());
    }

    public bool Register(int id, Keys key, bool control = false, bool alt = false, bool shift = false, bool windows = false)
    {
        uint modifiers = 0;
        if (control) modifiers |= NativeMethods.MOD_CONTROL;
        if (alt) modifiers |= NativeMethods.MOD_ALT;
        if (shift) modifiers |= NativeMethods.MOD_SHIFT;
        if (windows) modifiers |= NativeMethods.MOD_WIN;
        modifiers |= NativeMethods.MOD_NOREPEAT;

        var ok = NativeMethods.RegisterHotKey(Handle, id, modifiers, (uint)key);
        if (ok) _registered.Add(id);
        return ok;
    }

    protected override void WndProc(ref Message message)
    {
        if (message.Msg == NativeMethods.WM_HOTKEY)
        {
            Pressed?.Invoke(message.WParam.ToInt32());
        }

        base.WndProc(ref message);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        UnregisterAll();

        if (Handle != IntPtr.Zero) DestroyHandle();
    }

    public void UnregisterAll()
    {
        foreach (var id in _registered) NativeMethods.UnregisterHotKey(Handle, id);
        _registered.Clear();
    }
}
