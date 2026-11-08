using System.Drawing;
using System.Drawing.Drawing2D;
using KursorTheme.Config;
using KursorTheme.Rendering;

namespace KursorTheme.UI.Controls;

internal sealed class SelectionPreview : Control
{
    public Func<SelectionSettings>? SettingsProvider { get; set; }

    public SelectionPreview()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint
                 | ControlStyles.UserPaint
                 | ControlStyles.OptimizedDoubleBuffer, true);

        BackColor = Theme.Bg;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var settings = SettingsProvider?.Invoke();
        var graphics = e.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(Theme.Bg);
        if (settings == null) return;

        var area = new RectangleF(0, 0, Width, Height);

        using (var background = new LinearGradientBrush(area,
                   Color.FromArgb(21, 25, 32), Color.FromArgb(9, 11, 15), 35f))
        {
            graphics.FillRectangle(background, area);
        }

        using (var dotBrush = new SolidBrush(Color.FromArgb(37, 45, 57)))
        {
            for (var y = 8; y < Height; y += 18)
            for (var x = 8; x < Width; x += 18)
                graphics.FillEllipse(dotBrush, x, y, 1.6f, 1.6f);
        }

        var window = new RectangleF(16, 14, Width - 32f, Height - 28f);
        using (var path = Theme.Rounded(window, 11f))
        {
            using (var brush = new SolidBrush(Color.FromArgb(21, 25, 33)))
                graphics.FillPath(brush, path);

            using var border = new Pen(Theme.BorderSoft, 1f);
            graphics.DrawPath(border, path);
        }

        // Window chrome
        var dotY = window.Y + 14f;
        DrawDot(graphics, window.X + 15f, dotY, Color.FromArgb(255, 107, 107));
        DrawDot(graphics, window.X + 27f, dotY, Color.FromArgb(255, 196, 87));
        DrawDot(graphics, window.X + 39f, dotY, Color.FromArgb(89, 214, 141));

        using (var lineBrush = new SolidBrush(Color.FromArgb(42, 50, 62)))
        {
            graphics.FillRectangle(lineBrush, window.X + 16f, window.Y + 34f, window.Width * 0.46f, 5f);
            graphics.FillRectangle(lineBrush, window.X + 16f, window.Y + 46f, window.Width * 0.62f, 5f);
            graphics.FillRectangle(lineBrush, window.X + 16f, window.Y + 58f, window.Width * 0.34f, 5f);
        }

        var selection = new RectangleF(
            window.X + (window.Width - window.Width * 0.56f) / 2f,
            window.Y + window.Height * 0.32f,
            window.Width * 0.56f,
            window.Height * 0.52f);

        // Dim everything outside the marquee (the real overlay punches the same hole).
        using (var dim = new SolidBrush(OverlayPaint.WithAlpha(Color.Black, settings.DimOpacity)))
        {
            graphics.FillRectangle(dim, 0, 0, Width, selection.Top);
            graphics.FillRectangle(dim, 0, selection.Bottom, Width, Height - selection.Bottom);
            graphics.FillRectangle(dim, 0, selection.Top, selection.Left, selection.Height);
            graphics.FillRectangle(dim, selection.Right, selection.Top, Width - selection.Right, selection.Height);
        }

        if (settings.FillOpacity > 0.002f)
        {
            using var fill = new SolidBrush(OverlayPaint.WithAlpha(settings.FillColor, settings.FillOpacity));
            graphics.FillRectangle(fill, selection);
        }

        OverlayPaint.DrawHatch(graphics, selection, settings.HatchColor, settings.HatchOpacity,
            settings.HatchAngle, settings.HatchSpacing, settings.HatchWidth, 0f);

        var borderAlpha = settings.BorderOpacity;
        if (borderAlpha > 0.002f)
        {
            using (var glow = new Pen(OverlayPaint.WithAlpha(settings.BorderColor, borderAlpha * 0.16f),
                       settings.BorderWidth * 4f + 2f))
            {
                graphics.DrawRectangle(glow, selection.X, selection.Y, selection.Width, selection.Height);
            }

            using (var border = new Pen(OverlayPaint.WithAlpha(settings.BorderColor, borderAlpha), settings.BorderWidth))
            {
                graphics.DrawRectangle(border, selection.X, selection.Y, selection.Width, selection.Height);
            }
        }

        if (settings.CornerAccents)
        {
            OverlayPaint.DrawCornerAccents(graphics, selection, settings.BorderColor,
                Math.Min(1f, borderAlpha + 0.15f), 14f, settings.BorderWidth + 1.2f, 1.4f);
        }

        if (settings.ShowSize)
        {
            var text = $"{selection.Width:0} × {selection.Height:0}";
            var size = graphics.MeasureString(text, Theme.Badge);
            var badge = new RectangleF(selection.Left + 8f, selection.Bottom - size.Height - 13f,
                size.Width + 12f, size.Height + 5f);
            Theme.FillRounded(graphics, badge, 5f, Color.FromArgb(158, 0, 0, 0));
            using var textBrush = new SolidBrush(Color.FromArgb(230, 255, 255, 255));
            graphics.DrawString(text, Theme.Badge, textBrush, badge.X + 6f, badge.Y + 2f);
        }
    }

    private static void DrawDot(Graphics graphics, float x, float y, Color color)
    {
        using var brush = new SolidBrush(color);
        graphics.FillEllipse(brush, x, y, 7f, 7f);
    }
}
