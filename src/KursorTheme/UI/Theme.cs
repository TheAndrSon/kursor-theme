using System.Drawing;
using System.Drawing.Drawing2D;

namespace KursorTheme.UI;

internal static class Theme
{
    public static readonly Color Bg = Color.FromArgb(14, 16, 21);
    public static readonly Color BgElevated = Color.FromArgb(18, 21, 27);
    public static readonly Color Surface = Color.FromArgb(23, 27, 34);
    public static readonly Color SurfaceHover = Color.FromArgb(29, 35, 44);
    public static readonly Color SurfaceActive = Color.FromArgb(33, 40, 51);
    public static readonly Color Border = Color.FromArgb(40, 47, 59);
    public static readonly Color BorderSoft = Color.FromArgb(30, 36, 45);
    public static readonly Color Text = Color.FromArgb(231, 237, 243);
    public static readonly Color TextDim = Color.FromArgb(140, 153, 170);
    public static readonly Color TextFaint = Color.FromArgb(96, 107, 122);
    public static readonly Color Accent = Color.FromArgb(54, 229, 255);
    public static readonly Color AccentDeep = Color.FromArgb(26, 158, 190);
    public static readonly Color Violet = Color.FromArgb(139, 92, 246);
    public static readonly Color Green = Color.FromArgb(74, 222, 128);
    public static readonly Color Danger = Color.FromArgb(255, 109, 132);

    public static readonly Font H1 = new("Segoe UI Semibold", 19f, FontStyle.Regular, GraphicsUnit.Point);
    public static readonly Font H2 = new("Segoe UI Semibold", 11.5f, FontStyle.Regular, GraphicsUnit.Point);
    public static readonly Font Body = new("Segoe UI", 9.75f, FontStyle.Regular, GraphicsUnit.Point);
    public static readonly Font BodyStrong = new("Segoe UI Semibold", 9.75f, FontStyle.Regular, GraphicsUnit.Point);
    public static readonly Font Small = new("Segoe UI", 8.75f, FontStyle.Regular, GraphicsUnit.Point);
    public static readonly Font Tiny = new("Segoe UI", 8f, FontStyle.Regular, GraphicsUnit.Point);
    public static readonly Font Logo = new("Segoe UI Semibold", 14f, FontStyle.Regular, GraphicsUnit.Point);
    public static readonly Font Mono = new("Consolas", 9f, FontStyle.Regular, GraphicsUnit.Point);
    public static readonly Font Badge = new("Segoe UI Semibold", 8.25f, FontStyle.Regular, GraphicsUnit.Point);

    public static GraphicsPath Rounded(RectangleF rectangle, float radius)
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

    public static void FillRounded(Graphics graphics, RectangleF rectangle, float radius, Color color)
    {
        using var path = Rounded(rectangle, radius);
        using var brush = new SolidBrush(color);
        graphics.FillPath(brush, path);
    }

    public static void DrawRounded(Graphics graphics, RectangleF rectangle, float radius, Color color, float width = 1f)
    {
        using var path = Rounded(rectangle, radius);
        using var pen = new Pen(color, width);
        graphics.DrawPath(pen, path);
    }

    public static Color Mix(Color from, Color to, float amount)
    {
        amount = Math.Clamp(amount, 0f, 1f);
        return Color.FromArgb(
            (int)(from.A + (to.A - from.A) * amount),
            (int)(from.R + (to.R - from.R) * amount),
            (int)(from.G + (to.G - from.G) * amount),
            (int)(from.B + (to.B - from.B) * amount));
    }

    public static Color Lighten(Color color, float amount) => Mix(color, Color.White, amount);

    public static Color Darken(Color color, float amount) => Mix(color, Color.Black, amount);

    public static string Hex(Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";
}
