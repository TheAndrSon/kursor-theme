using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using KursorTheme.Config;
using KursorTheme.Interop;
using KursorTheme.Rendering;
using KursorTheme.Support;

namespace KursorTheme.Overlay;

/// <summary>
/// Interactive, per-pixel-alpha selection marquee. Activated with Ctrl+Alt+S.
/// Dims the screen, lets the user drag a rectangle, then renders the styled
/// selection (fill, diagonal hatch, border, corner accents, size badge).
/// This is our own marquee — the native Windows selection rectangle cannot be
/// restyled, so the two coexist.
/// </summary>
internal sealed class SelectionOverlay : Form
{
    private static readonly Font HintFont = new("Segoe UI Semibold", 10f, FontStyle.Regular, GraphicsUnit.Point);
    private static readonly Font SizeFont = new("Segoe UI Semibold", 8.75f, FontStyle.Regular, GraphicsUnit.Point);

    private readonly SettingsHub _hub;
    private readonly Action _onClosed;
    private readonly System.Windows.Forms.Timer _timer;
    private readonly Stopwatch _clock = Stopwatch.StartNew();

    private LayeredCanvas? _canvas;
    private Rectangle _virtualScreen;
    private Rectangle _lastSelection = Rectangle.Empty;
    private Rectangle _hintRect = Rectangle.Empty;
    private Point _dragStart;
    private Point _dragCurrent;
    private bool _dragging;
    private bool _finalized;
    private double _finalizedAt;
    private bool _closed;

    public SelectionOverlay(SettingsHub hub, Action onClosed)
    {
        _hub = hub;
        _onClosed = onClosed;

        AutoScaleMode = AutoScaleMode.None;
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = false;
        TopMost = true;
        BackColor = Color.Black;
        KeyPreview = true;
        Cursor = Cursors.Cross;
        SetStyle(ControlStyles.Opaque, true);

        _virtualScreen = SystemInfo.VirtualScreen();
        Bounds = _virtualScreen;

        _timer = new System.Windows.Forms.Timer { Interval = 16 };
        _timer.Tick += (_, _) => RenderFrame();
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var parameters = base.CreateParams;
            parameters.ExStyle |= NativeMethods.WS_EX_LAYERED | NativeMethods.WS_EX_TOOLWINDOW;
            parameters.ClassStyle |= NativeMethods.CS_DROPSHADOW;
            return parameters;
        }
    }

    public void Cancel() => Close();

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);

        _hub.SetSuspend(SuspendReason.Selection, true);
        _canvas = new LayeredCanvas(_virtualScreen.Width, _virtualScreen.Height);
        _canvas.ClearAll();

        PaintInitial();
        _timer.Start();

        Activate();
        BringToFront();
        Focus();
    }

    private void PaintInitial()
    {
        if (_canvas == null) return;

        var settings = _hub.Current.Selection;
        var graphics = _canvas.Graphics;
        using (var dim = new SolidBrush(OverlayPaint.WithAlpha(Color.Black, settings.DimOpacity)))
        {
            graphics.CompositingMode = CompositingMode.SourceCopy;
            graphics.FillRectangle(dim, 0, 0, _canvas.Width, _canvas.Height);
            graphics.CompositingMode = CompositingMode.SourceOver;
        }

        DrawHint(graphics, 1f, settings);
        _canvas.Present(Handle, _virtualScreen.Location,
            new Rectangle(0, 0, _canvas.Width, _canvas.Height));
    }

    private void RenderFrame()
    {
        if (_canvas == null || _closed) return;

        var settings = _hub.Current.Selection;
        var elapsed = _clock.Elapsed.TotalSeconds;

        var phase = settings.AnimateHatch
            ? (float)(elapsed * Math.Max(0f, settings.HatchSpeed))
            : 0f;

        var selection = GetSelectionScreenRect();
        if (_dragging && (selection.Width < 2 || selection.Height < 2)) selection = Rectangle.Empty;
        if (!_dragging && !_finalized) selection = Rectangle.Empty;

        var fade = 1f;
        if (_finalized && !settings.PersistUntilEsc)
        {
            var hold = Math.Clamp(settings.AutoHideMs, 300, 20000) / 1000.0;
            var after = elapsed - _finalizedAt;
            if (after > hold) fade = (float)(1.0 - (after - hold) / 0.35);
            if (fade <= 0f)
            {
                Close();
                return;
            }
        }

        var currentLocal = ToLocal(selection);
        var previousLocal = ToLocal(_lastSelection);

        var dirty = Rectangle.Union(currentLocal, previousLocal);
        dirty.Inflate(48, 48);

        var needsHint = fade < 0.999f || dirty.IntersectsWith(_hintRect);
        if (needsHint && !_hintRect.IsEmpty) dirty = Rectangle.Union(dirty, _hintRect);

        dirty.Intersect(new Rectangle(0, 0, _canvas.Width, _canvas.Height));
        if (dirty.Width <= 0 || dirty.Height <= 0) return;

        var graphics = _canvas.Graphics;

        // Repaint the dim layer inside the dirty rectangle.
        graphics.CompositingMode = CompositingMode.SourceCopy;
        using (var dim = new SolidBrush(OverlayPaint.WithAlpha(Color.Black, settings.DimOpacity * fade)))
        {
            graphics.FillRectangle(dim, dirty);
        }
        graphics.CompositingMode = CompositingMode.SourceOver;

        if (selection.Width > 0 && selection.Height > 0)
            DrawSelection(graphics, selection, settings, phase, fade);

        if (needsHint) DrawHint(graphics, fade, settings);

        _canvas.Present(Handle, _virtualScreen.Location, dirty);
        _lastSelection = selection;
    }

    private void DrawSelection(Graphics graphics, Rectangle selection, SelectionSettings settings, float phase, float fade)
    {
        var rect = new RectangleF(selection.X, selection.Y, selection.Width, selection.Height);
        var state = graphics.Save();
        try
        {
            graphics.TranslateTransform(-_virtualScreen.X, -_virtualScreen.Y);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;

            // Punch a hole in the dim layer.
            graphics.CompositingMode = CompositingMode.SourceCopy;
            graphics.FillRectangle(Brushes.Transparent, rect);
            graphics.CompositingMode = CompositingMode.SourceOver;

            if (settings.FillOpacity > 0.002f)
            {
                using var fill = new SolidBrush(OverlayPaint.WithAlpha(settings.FillColor, settings.FillOpacity * fade));
                graphics.FillRectangle(fill, rect);
            }

            OverlayPaint.DrawHatch(
                graphics,
                rect,
                settings.HatchColor,
                settings.HatchOpacity * fade,
                settings.HatchAngle,
                settings.HatchSpacing,
                settings.HatchWidth,
                phase);

            var borderAlpha = settings.BorderOpacity * fade;
            if (borderAlpha > 0.002f)
            {
                using (var glow = new Pen(
                           OverlayPaint.WithAlpha(settings.BorderColor, borderAlpha * 0.16f),
                           settings.BorderWidth * 4f + 2f))
                {
                    graphics.DrawRectangle(glow, rect.X, rect.Y, rect.Width, rect.Height);
                }

                using (var border = new Pen(
                           OverlayPaint.WithAlpha(settings.BorderColor, borderAlpha),
                           settings.BorderWidth))
                {
                    graphics.DrawRectangle(border, rect.X, rect.Y, rect.Width, rect.Height);
                }
            }

            if (settings.CornerAccents)
            {
                OverlayPaint.DrawCornerAccents(
                    graphics,
                    rect,
                    settings.BorderColor,
                    Math.Min(1f, borderAlpha + 0.15f),
                    16f,
                    settings.BorderWidth + 1.2f,
                    1.4f);
            }

            if (settings.ShowSize && selection.Width > 100 && selection.Height > 52)
                DrawSizeBadge(graphics, rect, fade);
        }
        finally
        {
            graphics.Restore(state);
        }
    }

    private void DrawSizeBadge(Graphics graphics, RectangleF selection, float fade)
    {
        var text = $"{selection.Width:0} × {selection.Height:0}";
        var textSize = graphics.MeasureString(text, SizeFont);
        var padding = new SizeF(9f, 3.5f);
        var badge = new RectangleF(
            selection.Left + 10f,
            selection.Bottom - textSize.Height - padding.Height * 2f - 10f,
            textSize.Width + padding.Width * 2f,
            textSize.Height + padding.Height * 2f);

        using (var path = Rounded(badge, 5f))
        using (var background = new SolidBrush(OverlayPaint.WithAlpha(Color.Black, 0.62f * fade)))
        {
            graphics.FillPath(background, path);
        }

        using (var textBrush = new SolidBrush(OverlayPaint.WithAlpha(Color.White, 0.92f * fade)))
        {
            graphics.DrawString(text, SizeFont, textBrush,
                badge.Left + padding.Width,
                badge.Top + padding.Height - 1f);
        }
    }

    private void DrawHint(Graphics graphics, float fade, SelectionSettings settings)
    {
        const string text = "Выделите область мышью    ·    Esc — отмена";
        var textSize = graphics.MeasureString(text, HintFont);
        var padding = new SizeF(16f, 8f);
        var width = textSize.Width + padding.Width * 2f;
        var height = textSize.Height + padding.Height * 2f;

        var primary = Screen.PrimaryScreen?.Bounds ?? new Rectangle(0, 0, Width, Height);
        var centerX = primary.Left + primary.Width / 2f - _virtualScreen.X;
        var top = primary.Top + 28f - _virtualScreen.Y;

        var rect = new RectangleF(centerX - width / 2f, top, width, height);

        using (var path = Rounded(rect, height / 2f))
        {
            using (var background = new SolidBrush(OverlayPaint.WithAlpha(Color.Black, 0.62f * fade)))
                graphics.FillPath(background, path);

            using var border = new Pen(OverlayPaint.WithAlpha(settings.BorderColor, 0.22f * fade), 1f);
            graphics.DrawPath(border, path);
        }

        using (var brush = new SolidBrush(OverlayPaint.WithAlpha(Color.White, 0.9f * fade)))
        {
            graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            graphics.DrawString(text, HintFont, brush,
                rect.Left + padding.Width,
                rect.Top + padding.Height - 1f);
        }

        _hintRect = Rectangle.Round(rect);
        _hintRect.Inflate(4, 4);
    }

    private static GraphicsPath Rounded(RectangleF rectangle, float radius)
    {
        var path = new GraphicsPath();
        var diameter = Math.Min(radius * 2f, Math.Min(rectangle.Width, rectangle.Height));
        if (diameter <= 0f)
        {
            path.AddRectangle(rectangle);
            return path;
        }

        path.AddArc(rectangle.X, rectangle.Y, diameter, diameter, 180, 90);
        path.AddArc(rectangle.Right - diameter, rectangle.Y, diameter, diameter, 270, 90);
        path.AddArc(rectangle.Right - diameter, rectangle.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rectangle.X, rectangle.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }

    private Rectangle GetSelectionScreenRect()
    {
        var left = Math.Min(_dragStart.X, _dragCurrent.X);
        var top = Math.Min(_dragStart.Y, _dragCurrent.Y);
        var right = Math.Max(_dragStart.X, _dragCurrent.X);
        var bottom = Math.Max(_dragStart.Y, _dragCurrent.Y);
        return Rectangle.FromLTRB(left, top, right, bottom);
    }

    private Point ToScreenPoint(Point client) =>
        new(_virtualScreen.X + client.X, _virtualScreen.Y + client.Y);

    private Rectangle ToLocal(Rectangle screenRect)
    {
        if (screenRect.Width <= 0 || screenRect.Height <= 0) return Rectangle.Empty;
        var local = screenRect;
        local.Offset(-_virtualScreen.X, -_virtualScreen.Y);
        return local;
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);

        if (e.Button == MouseButtons.Right)
        {
            Close();
            return;
        }

        if (e.Button != MouseButtons.Left) return;

        if (_finalized)
        {
            _finalized = false;
            _lastSelection = Rectangle.Empty;
        }

        _dragging = true;
        _dragStart = _dragCurrent = ToScreenPoint(e.Location);
        Capture = true;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (!_dragging) return;
        _dragCurrent = ToScreenPoint(e.Location);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (e.Button != MouseButtons.Left || !_dragging) return;

        _dragging = false;
        Capture = false;
        _dragCurrent = ToScreenPoint(e.Location);

        var selection = GetSelectionScreenRect();
        if (selection.Width < 6 || selection.Height < 6)
        {
            Close();
            return;
        }

        _finalized = true;
        _finalizedAt = _clock.Elapsed.TotalSeconds;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode == Keys.Escape)
        {
            e.Handled = true;
            Close();
        }
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _closed = true;
        _timer.Stop();
        _timer.Dispose();

        _canvas?.Dispose();
        _canvas = null;

        _hub.SetSuspend(SuspendReason.Selection, false);
        _onClosed();

        base.OnFormClosed(e);
    }
}
