using System.Drawing;
using System.Drawing.Drawing2D;

namespace KursorTheme.UI.Controls;

internal sealed class PillButton : Control
{
    private bool _hover;
    private bool _pressed;

    public bool Primary { get; set; }

    public Color AccentColor { get; set; } = Theme.Accent;

    public PillButton()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint
                 | ControlStyles.UserPaint
                 | ControlStyles.OptimizedDoubleBuffer
                 | ControlStyles.SupportsTransparentBackColor, true);

        Size = new Size(130, 32);
        BackColor = Theme.Bg;
        Font = Theme.BodyStrong;
        Cursor = Cursors.Hand;
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
        _pressed = false;
        Invalidate();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button == MouseButtons.Left)
        {
            _pressed = true;
            Invalidate();
        }
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        _pressed = false;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var graphics = e.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(Theme.Bg);

        var rect = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
        var fill = Primary
            ? (_pressed ? Theme.Darken(AccentColor, 0.12f) : _hover ? Theme.Lighten(AccentColor, 0.12f) : AccentColor)
            : (_pressed ? Theme.SurfaceActive : _hover ? Theme.SurfaceHover : Theme.Surface);

        using (var path = Theme.Rounded(rect, 8f))
        using (var brush = new SolidBrush(fill))
        {
            graphics.FillPath(brush, path);
            if (!Primary)
            {
                using var border = new Pen(_hover ? Theme.Border : Theme.BorderSoft, 1f);
                graphics.DrawPath(border, path);
            }
        }

        var textColor = Primary ? Color.FromArgb(8, 14, 18) : Theme.Text;
        using var textBrush = new SolidBrush(textColor);
        using var format = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center,
        };
        graphics.DrawString(Text, Font, textBrush, rect, format);
    }
}
