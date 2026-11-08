using System.Drawing;
using System.Drawing.Drawing2D;

namespace KursorTheme.UI.Controls;

internal sealed class PresetCard : Control
{
    private bool _hover;
    private bool _selected;

    public string Title { get; set; } = string.Empty;
    public string Tagline { get; set; } = string.Empty;
    public string TagText { get; set; } = string.Empty;
    public string PresetId { get; set; } = string.Empty;
    public Color[] Dots { get; set; } = Array.Empty<Color>();
    public Color AccentColor { get; set; } = Theme.Accent;
    public bool Dimmed { get; set; }

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

    public event EventHandler? Activated;

    public PresetCard()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint
                 | ControlStyles.UserPaint
                 | ControlStyles.OptimizedDoubleBuffer
                 | ControlStyles.SupportsTransparentBackColor, true);

        Size = new Size(330, 108);
        BackColor = Theme.Bg;
        Cursor = Cursors.Hand;
    }

    protected override void OnClick(EventArgs e)
    {
        base.OnClick(e);
        if (Dimmed) return;
        Activated?.Invoke(this, EventArgs.Empty);
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
        using (var path = Theme.Rounded(rect, 11f))
        {
            using (var brush = new SolidBrush(_hover ? Theme.SurfaceHover : Theme.Surface))
                graphics.FillPath(brush, path);

            if (Selected || _hover)
            {
                using var border = new Pen(Selected ? AccentColor : Theme.Border, Selected ? 1.5f : 1f);
                graphics.DrawPath(border, path);
            }
            else
            {
                using var border = new Pen(Theme.BorderSoft, 1f);
                graphics.DrawPath(border, path);
            }
        }

        if (Selected)
        {
            Theme.FillRounded(graphics, new RectangleF(14f, 14f, 3f, 16f), 1.5f, AccentColor);
        }

        using var titleBrush = new SolidBrush(Theme.Text);
        graphics.DrawString(Title, Theme.H2, titleBrush, new PointF(25f, 13f));

        if (!string.IsNullOrWhiteSpace(TagText))
        {
            using var tagBrush = new SolidBrush(Theme.TextFaint);
            using var tagFormat = new StringFormat { Alignment = StringAlignment.Far };
            graphics.DrawString(TagText, Theme.Badge, tagBrush,
                new RectangleF(0, 16f, Width - 16f, 16f), tagFormat);
        }

        using var taglineBrush = new SolidBrush(Theme.TextDim);
        var taglineRect = new RectangleF(25f, 39f, Width - 44f, 34f);
        using var format = new StringFormat { Trimming = StringTrimming.EllipsisCharacter };
        graphics.DrawString(Tagline, Theme.Small, taglineBrush, taglineRect, format);

        var x = 25f;
        foreach (var dot in Dots)
        {
            using var brush = new SolidBrush(dot);
            graphics.FillEllipse(brush, x, Height - 30f, 10f, 10f);
            x += 16f;
        }

        if (Selected)
        {
            using var checkBrush = new SolidBrush(AccentColor);
            using var checkFormat = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center };
            graphics.DrawString("активен", Theme.Badge, checkBrush,
                new RectangleF(0, Height - 34f, Width - 16f, 20f), checkFormat);
        }

        if (Dimmed)
        {
            using var veilPath = Theme.Rounded(new RectangleF(0, 0, Width, Height), 11f);
            using var veil = new SolidBrush(Color.FromArgb(168, Theme.Bg));
            graphics.FillPath(veil, veilPath);
        }
    }
}
