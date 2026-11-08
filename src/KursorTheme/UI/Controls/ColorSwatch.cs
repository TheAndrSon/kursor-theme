using System.Drawing;
using System.Drawing.Drawing2D;

namespace KursorTheme.UI.Controls;

internal sealed class ColorSwatch : Control
{
    private Color _color = Theme.Accent;
    private bool _hover;

    public event EventHandler? ColorChanged;

    public ColorSwatch()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint
                 | ControlStyles.UserPaint
                 | ControlStyles.OptimizedDoubleBuffer
                 | ControlStyles.SupportsTransparentBackColor, true);

        Size = new Size(56, 26);
        BackColor = Theme.Bg;
        Cursor = Cursors.Hand;
    }

    public Color Color
    {
        get => _color;
        set
        {
            if (_color == value) return;
            _color = value;
            Invalidate();
            ColorChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    protected override void OnClick(EventArgs e)
    {
        base.OnClick(e);

        using var dialog = new ColorDialog
        {
            Color = _color,
            FullOpen = true,
            AnyColor = true,
        };

        if (dialog.ShowDialog(FindForm()) == DialogResult.OK) Color = dialog.Color;
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

        var rect = new RectangleF(0.5f, 2.5f, Width - 1f, Height - 5f);
        using (var path = Theme.Rounded(rect, 7f))
        {
            using (var checkers = new HatchBrush(HatchStyle.LargeCheckerBoard, Color.FromArgb(44, 48, 56), Color.FromArgb(26, 29, 35)))
            {
                graphics.FillPath(checkers, path);
            }

            using (var brush = new SolidBrush(_color))
            {
                graphics.FillPath(brush, path);
            }

            using (var shine = new Pen(Color.FromArgb(56, Color.White), 1f))
            {
                graphics.DrawLine(shine, rect.Left + 6f, rect.Top + 1.5f, rect.Right - 6f, rect.Top + 1.5f);
            }

            using var border = new Pen(_hover ? Theme.Accent : Theme.Border, _hover ? 1.6f : 1f);
            graphics.DrawPath(border, path);
        }
    }
}
