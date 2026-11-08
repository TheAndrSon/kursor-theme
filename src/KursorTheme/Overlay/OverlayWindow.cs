using System.Diagnostics;
using System.Drawing;
using KursorTheme.Config;
using KursorTheme.Interop;
using KursorTheme.Rendering;
using KursorTheme.Support;
using Microsoft.Win32;

namespace KursorTheme.Overlay;

/// <summary>
/// A full-virtual-screen, per-pixel-alpha, click-through window that paints
/// effects around the untouched native cursor.
/// </summary>
internal sealed class OverlayWindow : Form
{
    private readonly SettingsHub _hub;
    private readonly EffectsEngine _engine = new();
    private readonly object _surfaceGate = new();

    private LayeredCanvas? _canvas;
    private Rectangle _virtualScreen;
    private Rectangle _lastDirty;
    private Thread? _renderThread;
    private volatile bool _running;
    private IntPtr _windowHandle;

    public OverlayWindow(SettingsHub hub)
    {
        _hub = hub;

        AutoScaleMode = AutoScaleMode.None;
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = false;
        MinimizeBox = false;
        MaximizeBox = false;
        TopMost = true;
        BackColor = Color.Black;
        SetStyle(ControlStyles.Opaque, true);

        _virtualScreen = SystemInfo.VirtualScreen();
        Bounds = _virtualScreen;

        SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
    }

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            var parameters = base.CreateParams;
            parameters.ExStyle |= NativeMethods.WS_EX_LAYERED
                                  | NativeMethods.WS_EX_TRANSPARENT
                                  | NativeMethods.WS_EX_TOOLWINDOW
                                  | NativeMethods.WS_EX_NOACTIVATE;
            return parameters;
        }
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);

        _windowHandle = Handle;
        RecreateSurface();

        _running = true;
        _renderThread = new Thread(RenderLoop)
        {
            IsBackground = true,
            Name = "KursorTheme.Render",
            Priority = ThreadPriority.AboveNormal,
        };
        _renderThread.Start();
    }

    private void OnDisplaySettingsChanged(object? sender, EventArgs e)
    {
        if (!IsHandleCreated || IsDisposed) return;
        try
        {
            BeginInvoke(new Action(RecreateSurface));
        }
        catch (ObjectDisposedException)
        {
            // Window is closing.
        }
        catch (InvalidOperationException)
        {
            // Handle was recreated.
        }
    }

    private void RecreateSurface()
    {
        lock (_surfaceGate)
        {
            if (_windowHandle == IntPtr.Zero) return;

            _virtualScreen = SystemInfo.VirtualScreen();
            Bounds = _virtualScreen;

            _canvas?.Dispose();
            _canvas = new LayeredCanvas(_virtualScreen.Width, _virtualScreen.Height);
            _lastDirty = Rectangle.Empty;
            _engine.Reset();

            _canvas.ClearAll();
            _canvas.Present(_windowHandle, _virtualScreen.Location,
                new Rectangle(0, 0, _virtualScreen.Width, _virtualScreen.Height));
        }
    }

    private void RenderLoop()
    {
        NativeMethods.timeBeginPeriod(1);
        var clock = Stopwatch.StartNew();
        var previous = 0.0;

        try
        {
            while (_running)
            {
                var now = clock.Elapsed.TotalSeconds;
                var deltaTime = Math.Clamp(now - previous, 0.0005, 0.1);
                previous = now;

                var settings = _hub.Current;
                try
                {
                    RenderFrame(deltaTime, settings);
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                }

                var frameTime = 1.0 / Math.Clamp(settings.TargetFps, 30, 240);
                var elapsed = clock.Elapsed.TotalSeconds - now;
                var wait = frameTime - elapsed;
                if (wait > 0.0015) Thread.Sleep(TimeSpan.FromSeconds(wait - 0.0008));
                else if (wait > 0) Thread.Yield();
            }
        }
        finally
        {
            NativeMethods.timeEndPeriod(1);
        }
    }

    private void RenderFrame(double deltaTime, AppSettings settings)
    {
        lock (_surfaceGate)
        {
            var canvas = _canvas;
            if (canvas == null || _windowHandle == IntPtr.Zero) return;

            var active = settings.EffectsEnabled && !_hub.IsSuspended;

            if (!active)
            {
                _engine.Reset();
                if (_lastDirty.Width > 0 && _lastDirty.Height > 0)
                {
                    canvas.ClearArea(_lastDirty);
                    canvas.Present(_windowHandle, _virtualScreen.Location, _lastDirty);
                    _lastDirty = Rectangle.Empty;
                }

                return;
            }

            _engine.Update(deltaTime, settings);

            var bounds = _engine.ComputeBounds(settings);
            var current = ToLocal(bounds);

            var dirty = Rectangle.Union(_lastDirty, current);
            dirty.Inflate(4, 4);
            dirty.Intersect(new Rectangle(0, 0, canvas.Width, canvas.Height));
            if (dirty.Width <= 0 || dirty.Height <= 0) return;

            canvas.ClearArea(dirty);
            _engine.Draw(canvas.Graphics, settings, _virtualScreen);
            canvas.Present(_windowHandle, _virtualScreen.Location, dirty);

            _lastDirty = current;
        }
    }

    private Rectangle ToLocal(RectangleF screenBounds)
    {
        if (screenBounds.Width <= 0 || screenBounds.Height <= 0) return Rectangle.Empty;

        var rectangle = Rectangle.Round(screenBounds);
        rectangle.Offset(-_virtualScreen.X, -_virtualScreen.Y);
        return rectangle;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;

            _running = false;
            var stopped = true;
            try
            {
                if (_renderThread is { IsAlive: true }) stopped = _renderThread.Join(700);
            }
            catch
            {
                stopped = false;
            }

            if (stopped)
            {
                lock (_surfaceGate)
                {
                    _canvas?.Dispose();
                    _canvas = null;
                }
            }
            else
            {
                Log.Info("Render thread did not stop in time; leaving the layered surface to process exit.");
            }
        }

        base.Dispose(disposing);
    }
}
