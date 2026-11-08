using System.Drawing;
using System.Drawing.Drawing2D;

namespace KursorTheme.UI.Controls;

internal sealed class ToggleSwitch : Control
{
    private readonly System.Windows.Forms.Timer _animation;
    private bool _checked;
    private float _position;

    public event EventHandler? CheckedChanged;

    public ToggleSwitch()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint
                 | ControlStyles.UserPaint
                 | ControlStyles.OptimizedDoubleBuffer
                 | ControlStyles.SupportsTransparentBackColor, true);

        Size = new Size(44, 24);
        BackColor = Theme.Bg;
        Cursor = Cursors.Hand;

        _animation = new System.Windows.Forms.Timer { Interval = 16 };
        _animation.Tick += (_, _) =>
        {
            var target = _checked ? 1f : 0f;
            _position += (target - _position) * 0.35f;
            if (MathF.Abs(target - _position) < 0.02f)
            {
                _position = target;
                _animation.Stop();
            }

            Invalidate();
        };
    }

    public bool Checked
    {
        get => _checked;
        set
        {
            if (_checked == value) return;
            _checked = value;
            _animation.Start();
            Invalidate();
            CheckedChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    protected override void OnClick(EventArgs e)
    {
        base.OnClick(e);
        Checked = !Checked;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var graphics = e.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(Theme.Bg);

        var track = new RectangleF(1f, 2f, Width - 2f, Height - 4f);
        var radius = track.Height / 2f;
        var trackColor = Theme.Mix(Theme.Border, Theme.Accent, _position);

        if (_position > 0.05f)
        {
            using var glowPath = Theme.Rounded(track, radius);
            using var glow = new SolidBrush(Color.FromArgb((int)(38 * _position), Theme.Accent));
            graphics.FillPath(glow, glowPath);
        }

        using (var path = Theme.Rounded(track, radius))
        using (var brush = new SolidBrush(trackColor))
        {
            graphics.FillPath(brush, path);
        }

        var knobRadius = radius - 3f;
        var travel = track.Width - knobRadius * 2f - 6f;
        var centerX = track.Left + 3f + knobRadius + travel * _position;
        var centerY = track.Top + track.Height / 2f;

        using (var shadow = new SolidBrush(Color.FromArgb(55, 0, 0, 0)))
            graphics.FillEllipse(shadow, centerX - knobRadius, centerY - knobRadius + 1f,
                knobRadius * 2f, knobRadius * 2f);

        var knobColor = Theme.Mix(Color.FromArgb(196, 207, 220), Color.White, _position);
        using (var knob = new SolidBrush(knobColor))
            graphics.FillEllipse(knob, centerX - knobRadius, centerY - knobRadius,
                knobRadius * 2f, knobRadius * 2f);
    }
}
