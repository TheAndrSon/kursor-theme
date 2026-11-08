using System.Drawing;
using System.Drawing.Drawing2D;

namespace KursorTheme.UI.Controls;

internal sealed class BrandPanel : Control
{
    public BrandPanel()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint
                 | ControlStyles.UserPaint
                 | ControlStyles.OptimizedDoubleBuffer, true);

        BackColor = Theme.BgElevated;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var graphics = e.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(BackColor);

        // Cursor glyph
        var points = new[]
        {
            new PointF(26f, 30f),
            new PointF(26f, 62f),
            new PointF(34f, 53.5f),
            new PointF(40f, 66f),
            new PointF(47f, 63f),
            new PointF(41f, 50.5f),
            new PointF(52f, 50.5f),
        };

        using (var glowBrush = new SolidBrush(Color.FromArgb(34, Theme.Accent)))
        {
            graphics.FillEllipse(glowBrush, 14f, 24f, 50f, 50f);
        }

        using (var path = new GraphicsPath())
        {
            path.AddPolygon(points);
            using var fill = new LinearGradientBrush(
                new PointF(26f, 30f), new PointF(52f, 66f), Theme.Accent, Theme.Violet);
            graphics.FillPath(fill, path);
        }

        var x = 66f;
        using (var logoBrush = new SolidBrush(Theme.Text))
        {
            graphics.DrawString("Kursor", Theme.Logo, logoBrush, new PointF(x, 36f));
        }

        var width = graphics.MeasureString("Kursor ", Theme.Logo).Width;
        using (var accentBrush = new SolidBrush(Theme.Accent))
        {
            graphics.DrawString("Theme", Theme.Logo, accentBrush, new PointF(x + width, 36f));
        }

        using var subtitleBrush = new SolidBrush(Theme.TextFaint);
        graphics.DrawString("cursor effects  ·  v0.2", Theme.Tiny, subtitleBrush, new PointF(x, 60f));
    }
}
