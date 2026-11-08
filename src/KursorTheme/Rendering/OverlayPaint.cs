using System.Drawing;
using System.Drawing.Drawing2D;

namespace KursorTheme.Rendering;

/// <summary>Shared painting helpers for the selection marquee.</summary>
internal static class OverlayPaint
{
    public static Color WithAlpha(Color color, float opacity)
    {
        var alpha = Math.Clamp((int)MathF.Round(opacity * 255f), 0, 255);
        return Color.FromArgb(alpha, color.R, color.G, color.B);
    }

    public static void DrawHatch(
        Graphics graphics,
        RectangleF rect,
        Color color,
        float opacity,
        float angleDegrees,
        float spacing,
        float thickness,
        float phase)
    {
        if (opacity <= 0.002f || rect.Width < 1 || rect.Height < 1) return;

        spacing = Math.Clamp(spacing, 2f, 60f);
        thickness = Math.Clamp(thickness, 0.2f, 12f);

        var state = graphics.Save();
        try
        {
            graphics.SetClip(rect);
            graphics.TranslateTransform(rect.X + rect.Width / 2f, rect.Y + rect.Height / 2f);
            graphics.RotateTransform(angleDegrees);

            var diagonal = MathF.Sqrt(rect.Width * rect.Width + rect.Height * rect.Height) + spacing * 2f;
            var offset = ((phase % spacing) + spacing) % spacing;
            using var pen = new Pen(WithAlpha(color, opacity), thickness) { StartCap = LineCap.Round, EndCap = LineCap.Round };

            for (var x = -diagonal + offset; x <= diagonal; x += spacing)
            {
                graphics.DrawLine(pen, x, -diagonal, x, diagonal);
            }
        }
        finally
        {
            graphics.Restore(state);
        }
    }

    public static void DrawCornerAccents(
        Graphics graphics,
        RectangleF rect,
        Color color,
        float opacity,
        float length,
        float thickness,
        float inset)
    {
        if (opacity <= 0.002f || rect.Width < 8 || rect.Height < 8) return;

        length = Math.Clamp(length, 4f, Math.Min(rect.Width, rect.Height) * 0.4f);
        using var pen = new Pen(WithAlpha(color, opacity), thickness)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
        };

        var left = rect.Left + inset;
        var top = rect.Top + inset;
        var right = rect.Right - inset;
        var bottom = rect.Bottom - inset;

        // top-left
        graphics.DrawLine(pen, left, top, left + length, top);
        graphics.DrawLine(pen, left, top, left, top + length);
        // top-right
        graphics.DrawLine(pen, right - length, top, right, top);
        graphics.DrawLine(pen, right, top, right, top + length);
        // bottom-left
        graphics.DrawLine(pen, left, bottom - length, left, bottom);
        graphics.DrawLine(pen, left, bottom, left + length, bottom);
        // bottom-right
        graphics.DrawLine(pen, right, bottom - length, right, bottom);
        graphics.DrawLine(pen, right - length, bottom, right, bottom);
    }
}
