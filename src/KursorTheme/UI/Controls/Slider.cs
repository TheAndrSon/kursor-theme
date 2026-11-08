using System.Drawing;
using System.Drawing.Drawing2D;

namespace KursorTheme.UI.Controls;

internal sealed class Slider : Control
{
    private double _minimum;
    private double _maximum = 1;
    private double _value;
    private double _step = 0.01;
    private bool _dragging;
    private bool _hover;

    private float ThumbRadius => Math.Max(5f, Height * 0.27f);

    public event EventHandler? ValueChanged;

    public Slider()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint
                 | ControlStyles.UserPaint
                 | ControlStyles.OptimizedDoubleBuffer
                 | ControlStyles.SupportsTransparentBackColor
                 | ControlStyles.Selectable, true);

        Height = 26;
        BackColor = Theme.Bg;
        Cursor = Cursors.Hand;
        TabStop = true;
    }

    public double Minimum
    {
        get => _minimum;
        set
        {
            _minimum = value;
            if (_maximum <= _minimum) _maximum = _minimum + 1;
            Value = _value;
        }
    }

    public double Maximum
    {
        get => _maximum;
        set
        {
            _maximum = value;
            if (_maximum <= _minimum) _minimum = _maximum - 1;
            Value = _value;
        }
    }

    public double Step
    {
        get => _step;
        set => _step = Math.Max(0.0001, value);
    }

    public double Value
    {
        get => _value;
        set
        {
            var clamped = Math.Clamp(value, Math.Min(_minimum, _maximum), Math.Max(_minimum, _maximum));
            if (_step > 0)
            {
                var steps = Math.Round((clamped - _minimum) / _step);
                clamped = _minimum + steps * _step;
            }

            clamped = Math.Clamp(clamped, Math.Min(_minimum, _maximum), Math.Max(_minimum, _maximum));

            if (Math.Abs(clamped - _value) < 1e-9) return;
            _value = clamped;
            Invalidate();
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var graphics = e.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(Theme.Bg);

        var usable = Width - ThumbRadius * 2f;
        var ratio = Ratio;
        var centerY = Height / 2f;

        var track = new RectangleF(ThumbRadius, centerY - 2f, usable, 4f);
        Theme.FillRounded(graphics, track, 2f, Theme.BorderSoft);

        var filledWidth = (float)(usable * ratio);
        if (filledWidth > 0.5f)
        {
            var filled = new RectangleF(ThumbRadius, centerY - 2f, filledWidth, 4f);
            using var gradient = new LinearGradientBrush(
                new RectangleF(track.X, track.Y, Math.Max(1f, track.Width), track.Height),
                Theme.AccentDeep,
                Theme.Accent,
                LinearGradientMode.Horizontal);
            using var path = Theme.Rounded(filled, 2f);
            graphics.FillPath(gradient, path);
        }

        var thumbX = ThumbRadius + usable * (float)ratio;
        if (_dragging || _hover)
        {
            using var glow = new SolidBrush(Color.FromArgb(46, Theme.Accent));
            graphics.FillEllipse(glow, thumbX - ThumbRadius - 3f, centerY - ThumbRadius - 3f,
                (ThumbRadius + 3f) * 2f, (ThumbRadius + 3f) * 2f);
        }

        using (var ring = new Pen(Theme.Accent, 1.6f))
            graphics.DrawEllipse(ring, thumbX - ThumbRadius, centerY - ThumbRadius,
                ThumbRadius * 2f, ThumbRadius * 2f);

        using (var thumb = new SolidBrush(Color.White))
            graphics.FillEllipse(thumb, thumbX - ThumbRadius + 2f, centerY - ThumbRadius + 2f,
                (ThumbRadius - 2f) * 2f, (ThumbRadius - 2f) * 2f);
    }

    private double Ratio
    {
        get
        {
            var span = _maximum - _minimum;
            if (Math.Abs(span) < 1e-9) return 0;
            return Math.Clamp((_value - _minimum) / span, 0, 1);
        }
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left) return;

        _dragging = true;
        Capture = true;
        Focus();
        SetFromX(e.X);
        Invalidate();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (_dragging) SetFromX(e.X);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (e.Button != MouseButtons.Left) return;
        _dragging = false;
        Capture = false;
        Invalidate();
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

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        var delta = e.KeyCode switch
        {
            Keys.Left or Keys.Down => -_step,
            Keys.Right or Keys.Up => _step,
            Keys.PageDown => -_step * 10,
            Keys.PageUp => _step * 10,
            Keys.Home => _minimum - _value,
            Keys.End => _maximum - _value,
            _ => 0,
        };

        if (Math.Abs(delta) > 1e-9)
        {
            Value += delta;
            e.Handled = true;
        }
    }

    private void SetFromX(int x)
    {
        var usable = Width - ThumbRadius * 2f;
        if (usable <= 0) return;
        var ratio = Math.Clamp((x - ThumbRadius) / usable, 0f, 1f);
        Value = _minimum + (_maximum - _minimum) * ratio;
    }
}
