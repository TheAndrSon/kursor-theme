using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using KursorTheme.Interop;
using KursorTheme.Support;

namespace KursorTheme.Rendering;

/// <summary>
/// A per-pixel alpha surface used with WS_EX_LAYERED windows.
/// Keeps a top-down 32bpp DIB section and presents it through
/// UpdateLayeredWindowIndirect using dirty rectangles.
/// </summary>
internal sealed unsafe class LayeredCanvas : IDisposable
{
    private readonly IntPtr _memoryDc;
    private readonly IntPtr _dib;
    private readonly IntPtr _previousBitmap;
    private readonly Bitmap _bitmap;
    private bool _disposed;
    private int _presentFailures;

    public LayeredCanvas(int width, int height)
    {
        Width = Math.Max(1, width);
        Height = Math.Max(1, height);

        var info = new NativeMethods.BITMAPINFO();
        info.bmiHeader.biSize = sizeof(NativeMethods.BITMAPINFOHEADER);
        info.bmiHeader.biWidth = Width;
        info.bmiHeader.biHeight = -Height; // top-down
        info.bmiHeader.biPlanes = 1;
        info.bmiHeader.biBitCount = 32;
        info.bmiHeader.biCompression = NativeMethods.BI_RGB;

        _memoryDc = NativeMethods.CreateCompatibleDC(IntPtr.Zero);
        if (_memoryDc == IntPtr.Zero)
            throw new Win32Exception(Marshal.GetLastWin32Error(), "CreateCompatibleDC failed");

        _dib = NativeMethods.CreateDIBSection(
            _memoryDc, ref info, NativeMethods.DIB_RGB_COLORS, out var bits, IntPtr.Zero, 0);

        if (_dib == IntPtr.Zero || bits == IntPtr.Zero)
            throw new Win32Exception(Marshal.GetLastWin32Error(), "CreateDIBSection failed");

        _previousBitmap = NativeMethods.SelectObject(_memoryDc, _dib);

        _bitmap = new Bitmap(Width, Height, Width * 4, PixelFormat.Format32bppPArgb, bits);
        Graphics = Graphics.FromImage(_bitmap);
        Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        Graphics.CompositingMode = CompositingMode.SourceOver;
        Graphics.CompositingQuality = CompositingQuality.HighSpeed;
        Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

        ClearAll();
    }

    public int Width { get; }

    public int Height { get; }

    public Graphics Graphics { get; }

    public void ClearAll()
    {
        Graphics.CompositingMode = CompositingMode.SourceCopy;
        Graphics.FillRectangle(Brushes.Transparent, 0, 0, Width, Height);
        Graphics.CompositingMode = CompositingMode.SourceOver;
    }

    public void ClearArea(Rectangle area)
    {
        if (area.Width <= 0 || area.Height <= 0) return;
        Graphics.CompositingMode = CompositingMode.SourceCopy;
        Graphics.FillRectangle(Brushes.Transparent, area);
        Graphics.CompositingMode = CompositingMode.SourceOver;
    }

    public void Present(IntPtr window, Point destination, Rectangle dirty)
    {
        if (_disposed) return;

        dirty.Intersect(new Rectangle(0, 0, Width, Height));
        if (dirty.Width <= 0 || dirty.Height <= 0) return;

        Graphics.Flush(FlushIntention.Sync);

        var destinationPoint = new NativeMethods.POINT { X = destination.X, Y = destination.Y };
        var sourcePoint = new NativeMethods.POINT { X = 0, Y = 0 };
        var size = new NativeMethods.SIZE { CX = Width, CY = Height };
        var blend = new NativeMethods.BLENDFUNCTION
        {
            BlendOp = NativeMethods.AC_SRC_OVER,
            BlendFlags = 0,
            SourceConstantAlpha = 255,
            AlphaFormat = NativeMethods.AC_SRC_ALPHA,
        };
        var dirtyRect = new NativeMethods.RECT
        {
            Left = dirty.Left,
            Top = dirty.Top,
            Right = dirty.Right,
            Bottom = dirty.Bottom,
        };

        var layerInfo = new NativeMethods.UPDATELAYEREDWINDOWINFO
        {
            cbSize = sizeof(NativeMethods.UPDATELAYEREDWINDOWINFO),
            hdcDst = IntPtr.Zero,
            pptDst = (IntPtr)(&destinationPoint),
            psize = (IntPtr)(&size),
            hdcSrc = _memoryDc,
            pptSrc = (IntPtr)(&sourcePoint),
            crKey = 0,
            pblend = (IntPtr)(&blend),
            dwFlags = (int)NativeMethods.ULW_ALPHA,
            prcDirty = (IntPtr)(&dirtyRect),
        };

        if (NativeMethods.UpdateLayeredWindowIndirect(window, ref layerInfo)) return;

        var error = Marshal.GetLastWin32Error();
        _presentFailures++;
        if (error != 0 && _presentFailures is 1 or 60)
        {
            Log.Error(new Win32Exception(error, $"UpdateLayeredWindowIndirect failed ({_presentFailures} times)"));
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        Graphics.Dispose();
        _bitmap.Dispose();

        if (_previousBitmap != IntPtr.Zero) NativeMethods.SelectObject(_memoryDc, _previousBitmap);
        if (_dib != IntPtr.Zero) NativeMethods.DeleteObject(_dib);
        if (_memoryDc != IntPtr.Zero) NativeMethods.DeleteDC(_memoryDc);
    }
}
