using System.Drawing;
using System.Drawing.Drawing2D;

namespace KursorTheme.UI.Controls;

internal sealed class ThinScrollBar : Control
{
    private int _maximum;
    private int _value;
    private float _viewportRatio = 1f;
    private bool _hover;
    private bool _dragging;
    private float _dragOffset;

    public event EventHandler? ValueChanged;

    public ThinScrollBar()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint
                 | ControlStyles.UserPaint
                 | ControlStyles.OptimizedDoubleBuffer
                 | ControlStyles.SupportsTransparentBackColor, true);

        Width = 10;
        BackColor = Theme.Bg;
        Cursor = Cursors.Hand;
    }

    public int Maximum
    {
        get => _maximum;
        set
        {
            var clamped = Math.Max(0, value);
            if (_maximum == clamped) return;
            _maximum = clamped;
            Invalidate();
        }
    }

    public int Value
    {
        get => _value;
        set
        {
            var clamped = Math.Clamp(value, 0, _maximum);
            if (_value == clamped) return;
            _value = clamped;
            Invalidate();
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public float ViewportRatio
    {
        get => _viewportRatio;
        set
        {
            var clamped = Math.Clamp(value, 0.06f, 1f);
            if (Math.Abs(_viewportRatio - clamped) < 0.0001f) return;
            _viewportRatio = clamped;
            Invalidate();
        }
    }

    public bool IsNeeded => _maximum > 0;

    protected override void OnPaint(PaintEventArgs e)
    {
        var graphics = e.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(Theme.Bg);

        if (!IsNeeded) return;

        var track = new RectangleF((Width - 6f) / 2f, 0f, 6f, Height);
        Theme.FillRounded(graphics, track, 3f, Color.FromArgb(22, 26, 33));

        var thumbHeight = Math.Max(32f, Height * _viewportRatio);
        var progress = _maximum <= 0 ? 0f : _value / (float)_maximum;
        var top = (Height - thumbHeight) * Math.Clamp(progress, 0f, 1f);
        var thumb = new RectangleF(track.X, top, 6f, thumbHeight);

        var color = _dragging || _hover ? Theme.Accent : Color.FromArgb(74, 85, 101);
        Theme.FillRounded(graphics, thumb, 3f, color);
    }

    private float ThumbHeight => Math.Max(32f, Height * _viewportRatio);

    private float ThumbTop => (Height - ThumbHeight) *
                              (_maximum <= 0 ? 0f : Math.Clamp(_value / (float)_maximum, 0f, 1f));

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left || !IsNeeded) return;

        _dragging = true;
        Capture = true;

        var thumbTop = ThumbTop;
        var thumbHeight = ThumbHeight;
        _dragOffset = e.Y >= thumbTop && e.Y <= thumbTop + thumbHeight
            ? e.Y - thumbTop
            : thumbHeight / 2f;

        SetFromY(e.Y);
        Invalidate();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (!_dragging) return;
        SetFromY(e.Y);
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

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);
        if (!IsNeeded) return;
        Value = Math.Clamp(Value - Math.Sign(e.Delta) * 96, 0, Maximum);
    }

    private void SetFromY(int y)
    {
        var travel = Height - ThumbHeight;
        if (travel <= 0 || _maximum <= 0) return;

        var position = Math.Clamp((y - _dragOffset) / travel, 0f, 1f);
        Value = (int)Math.Round(position * _maximum);
    }
}
