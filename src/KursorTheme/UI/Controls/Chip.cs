using System.Drawing;
using System.Drawing.Drawing2D;

namespace KursorTheme.UI.Controls;

internal sealed class Chip : Control
{
    private bool _hover;
    private bool _selected;

    public Chip()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint
                 | ControlStyles.UserPaint
                 | ControlStyles.OptimizedDoubleBuffer
                 | ControlStyles.SupportsTransparentBackColor, true);

        Size = new Size(84, 26);
        BackColor = Theme.Bg;
        Font = Theme.Small;
        Cursor = Cursors.Hand;
    }

    public bool Selected
    {
        get => _selected;
        set
        {
            if (_selected == value) return;
            _selected = value;
            Invalidate();
        }
    }

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
        graphics.Clear(Theme.Bg);

        var rect = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
        var fill = Selected
            ? Color.FromArgb(28, Theme.Accent)
            : _hover ? Theme.SurfaceHover : Theme.Surface;

        using (var path = Theme.Rounded(rect, Height / 2f))
        using (var brush = new SolidBrush(fill))
        {
            graphics.FillPath(brush, path);

            using var border = new Pen(Selected ? Theme.Accent : _hover ? Theme.Border : Theme.BorderSoft, 1f);
            graphics.DrawPath(border, path);
        }

        var textColor = Selected ? Theme.Accent : _hover ? Theme.Text : Theme.TextDim;
        using var textBrush = new SolidBrush(textColor);
        using var format = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center,
        };
        graphics.DrawString(Text, Font, textBrush, rect, format);
    }
}
