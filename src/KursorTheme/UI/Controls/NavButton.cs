using System.Drawing;
using System.Drawing.Drawing2D;

namespace KursorTheme.UI.Controls;

internal sealed class NavButton : Control
{
    private bool _hover;

    public NavButton()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint
                 | ControlStyles.UserPaint
                 | ControlStyles.OptimizedDoubleBuffer
                 | ControlStyles.SupportsTransparentBackColor, true);

        Height = 42;
        BackColor = Theme.BgElevated;
        Font = Theme.Body;
        Cursor = Cursors.Hand;
    }

    public bool Selected { get; set; }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        _hover = true;
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _hover = false;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var graphics = e.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(BackColor);

        var rect = new RectangleF(0, 1, Width - 1, Height - 2);

        if (Selected || _hover)
        {
            using var path = Theme.Rounded(rect, 9f);
            using var brush = new SolidBrush(Selected ? Theme.SurfaceActive : Theme.SurfaceHover);
            graphics.FillPath(brush, path);
        }

        if (Selected)
        {
            Theme.FillRounded(graphics, new RectangleF(6f, Height / 2f - 9f, 3f, 18f), 1.5f, Theme.Accent);
        }

        using var textBrush = new SolidBrush(Selected ? Theme.Text : _hover ? Theme.Text : Theme.TextDim);
        using var format = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center };
        graphics.DrawString(Text, Font, textBrush, new RectangleF(20f, 0, Width - 28f, Height), format);
    }
}
