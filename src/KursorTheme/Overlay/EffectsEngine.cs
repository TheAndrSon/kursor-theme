using System.Drawing;
using System.Drawing.Drawing2D;
using KursorTheme.Config;
using KursorTheme.Interop;
using KursorTheme.Rendering;

namespace KursorTheme.Overlay;

/// <summary>
/// Owns the short-lived state (trail, ripples, particles) and paints all cursor
/// effects. Everything is drawn in virtual-screen coordinates; the native
/// cursor itself is never touched or hidden.
/// </summary>
internal sealed class EffectsEngine
{
    private sealed class TrailPoint
    {
        public float X;
        public float Y;
        public double Time;
    }

    private sealed class RippleState
    {
        public float X;
        public float Y;
        public float MaxRadius;
        public float Duration;
        public double Start;
        public Color Color;
        public int Rings;
        public float Thickness;
    }

    private sealed class ParticleState
    {
        public float X;
        public float Y;
        public float VelocityX;
        public float VelocityY;
        public float Gravity;
        public float Lifetime;
        public float Age;
        public float Size;
        public Color Color;
    }

    private const int MaxTrailPoints = 160;
    private const int MaxParticles = 160;

    private readonly List<TrailPoint> _trail = new(MaxTrailPoints);
    private readonly List<RippleState> _ripples = new(16);
    private readonly List<ParticleState> _particles = new(MaxParticles);
    private readonly Random _random = new();

    private PointF _smoothed;
    private Point _cursor;
    private Point _lastCursor;
    private bool _hasCursor;
    private double _clock;
    private double _idle;
    private float _press;
    private bool _previousLeft;
    private bool _previousRight;
    private bool _previousMiddle;

    public void Reset()
    {
        _trail.Clear();
        _ripples.Clear();
        _particles.Clear();
        _hasCursor = false;
        _press = 0f;
        _previousLeft = false;
        _previousRight = false;
        _previousMiddle = false;
    }

    public void Update(double deltaTime, AppSettings settings)
    {
        if (deltaTime <= 0) return;
        _clock += deltaTime;

        if (!NativeMethods.GetCursorPos(out var position)) return;
        _cursor = new Point(position.X, position.Y);

        if (!_hasCursor)
        {
            _hasCursor = true;
            _smoothed = _cursor;
            _lastCursor = _cursor;
        }

        var moved = Math.Abs(_cursor.X - _lastCursor.X) + Math.Abs(_cursor.Y - _lastCursor.Y);
        if (moved == 0) _idle += deltaTime;
        else
        {
            _idle = 0;
            _lastCursor = _cursor;
        }

        var follow = 1f - MathF.Exp((float)(-deltaTime * 26.0));
        _smoothed = new PointF(
            _smoothed.X + (_cursor.X - _smoothed.X) * follow,
            _smoothed.Y + (_cursor.Y - _smoothed.Y) * follow);

        var left = (NativeMethods.GetAsyncKeyState(NativeMethods.VK_LBUTTON) & 0x8000) != 0;
        var right = (NativeMethods.GetAsyncKeyState(NativeMethods.VK_RBUTTON) & 0x8000) != 0;
        var middle = (NativeMethods.GetAsyncKeyState(NativeMethods.VK_MBUTTON) & 0x8000) != 0;

        var pressTarget = left || right || middle ? 1f : 0f;
        _press += (pressTarget - _press) * MathF.Min(1f, (float)deltaTime * 16f);

        if (left && !_previousLeft) SpawnClick(settings, secondary: false);
        if (right && !_previousRight) SpawnClick(settings, secondary: true);
        _previousLeft = left;
        _previousRight = right;
        _previousMiddle = middle;

        if (settings.Trail.Enabled)
        {
            if (_idle < 1.4)
            {
                var last = _trail.Count > 0 ? _trail[^1] : null;
                if (last == null ||
                    Math.Abs(last.X - _smoothed.X) + Math.Abs(last.Y - _smoothed.Y) > 0.35f)
                {
                    _trail.Add(new TrailPoint { X = _smoothed.X, Y = _smoothed.Y, Time = _clock });
                    if (_trail.Count > MaxTrailPoints) _trail.RemoveAt(0);
                }
            }
        }
        else
        {
            _trail.Clear();
        }

        var trailLife = Math.Clamp(settings.Trail.LengthMs, 40, 4000) / 1000.0;
        while (_trail.Count > 0 && _clock - _trail[0].Time > trailLife) _trail.RemoveAt(0);

        for (var i = _ripples.Count - 1; i >= 0; i--)
        {
            if (_clock - _ripples[i].Start > _ripples[i].Duration) _ripples.RemoveAt(i);
        }

        for (var i = _particles.Count - 1; i >= 0; i--)
        {
            var particle = _particles[i];
            particle.Age += (float)deltaTime;
            if (particle.Age >= particle.Lifetime)
            {
                _particles.RemoveAt(i);
                continue;
            }

            particle.VelocityY += particle.Gravity * (float)deltaTime;
            var drag = MathF.Pow(0.87f, (float)deltaTime * 60f);
            particle.VelocityX *= drag;
            particle.VelocityY *= drag;
            particle.X += particle.VelocityX * (float)deltaTime;
            particle.Y += particle.VelocityY * (float)deltaTime;
        }
    }

    public RectangleF ComputeBounds(AppSettings settings)
    {
        if (!_hasCursor) return RectangleF.Empty;

        var bounds = RectangleF.Empty;

        if (settings.Glow.Enabled)
        {
            var pulse = MathF.Sin((float)(_clock * Math.PI * 2.0 * MathF.Max(0.01f, settings.Glow.PulseSpeed)));
            var radius = settings.Glow.Radius
                         * (1f + settings.Glow.Pulse * 0.22f * pulse)
                         * (1f + _press * 0.22f);
            bounds = Union(bounds, Circle(_smoothed, radius + 8f));
        }

        if (settings.Trail.Enabled && _trail.Count > 1)
        {
            var minX = float.MaxValue;
            var minY = float.MaxValue;
            var maxX = float.MinValue;
            var maxY = float.MinValue;
            foreach (var point in _trail)
            {
                minX = MathF.Min(minX, point.X);
                minY = MathF.Min(minY, point.Y);
                maxX = MathF.Max(maxX, point.X);
                maxY = MathF.Max(maxY, point.Y);
            }

            var padding = MathF.Max(8f, settings.Trail.Thickness * 2.2f + 6f);
            bounds = Union(bounds, RectangleF.FromLTRB(minX - padding, minY - padding, maxX + padding, maxY + padding));
        }

        foreach (var ripple in _ripples)
            bounds = Union(bounds, Circle(new PointF(ripple.X, ripple.Y), ripple.MaxRadius + 10f));

        foreach (var particle in _particles)
            bounds = Union(bounds, Circle(new PointF(particle.X, particle.Y), particle.Size * 4f + 6f));

        return bounds;
    }

    public void Draw(Graphics graphics, AppSettings settings, Rectangle virtualScreen)
    {
        var state = graphics.Save();
        try
        {
            graphics.TranslateTransform(-virtualScreen.X, -virtualScreen.Y);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.CompositingMode = CompositingMode.SourceOver;

            if (settings.Trail.Enabled) DrawTrail(graphics, settings);
            if (settings.Glow.Enabled) DrawGlow(graphics, settings);
            DrawRipples(graphics);
            DrawParticles(graphics);
        }
        finally
        {
            graphics.Restore(state);
        }
    }

    private void SpawnClick(AppSettings settings, bool secondary)
    {
        if (settings.Ripple.Enabled)
        {
            _ripples.Add(new RippleState
            {
                X = _cursor.X,
                Y = _cursor.Y,
                MaxRadius = Math.Clamp(settings.Ripple.MaxRadius, 8f, 600f),
                Duration = Math.Clamp(settings.Ripple.DurationMs, 80, 4000) / 1000f,
                Start = _clock,
                Color = secondary ? settings.Particles.Color : settings.Ripple.Color,
                Rings = Math.Clamp(settings.Ripple.Rings, 1, 3),
                Thickness = Math.Clamp(settings.Ripple.Thickness, 0.3f, 12f),
            });
        }

        if (!settings.Particles.Enabled || settings.Particles.Count <= 0) return;

        var count = Math.Clamp(settings.Particles.Count, 0, 40);
        for (var i = 0; i < count && _particles.Count < MaxParticles; i++)
        {
            var angle = _random.NextDouble() * Math.PI * 2.0;
            var speed = settings.Particles.Speed * (0.35f + (float)_random.NextDouble() * 0.9f);
            _particles.Add(new ParticleState
            {
                X = _cursor.X,
                Y = _cursor.Y,
                VelocityX = (float)Math.Cos(angle) * speed,
                VelocityY = (float)Math.Sin(angle) * speed - 40f,
                Gravity = settings.Particles.Gravity,
                Lifetime = Math.Clamp(settings.Particles.LifetimeMs, 80, 4000) / 1000f,
                Age = 0f,
                Size = Math.Clamp(settings.Particles.Size, 0.5f, 24f),
                Color = secondary ? settings.Ripple.Color : settings.Particles.Color,
            });
        }
    }

    private void DrawGlow(Graphics graphics, AppSettings settings)
    {
        var pulseWave = MathF.Sin((float)(_clock * Math.PI * 2.0 * MathF.Max(0.01f, settings.Glow.PulseSpeed)));
        var radius = settings.Glow.Radius
                     * (1f + settings.Glow.Pulse * 0.22f * pulseWave)
                     * (1f + _press * 0.22f);

        var alpha = Math.Clamp(settings.Glow.Intensity, 0f, 1f)
                    * (0.85f + 0.15f * pulseWave)
                    * (1f + _press * 0.28f);

        if (radius < 1f || alpha <= 0.004f) return;

        FillRadial(graphics, _smoothed.X, _smoothed.Y, radius,
            OverlayPaint.WithAlpha(settings.Glow.Color, alpha * 0.8f),
            OverlayPaint.WithAlpha(settings.Glow.Color, 0f));

        FillRadial(graphics, _smoothed.X, _smoothed.Y, radius * 0.42f,
            OverlayPaint.WithAlpha(settings.Glow.Color, alpha * 0.9f),
            OverlayPaint.WithAlpha(settings.Glow.Color, 0f));
    }

    private void DrawTrail(Graphics graphics, AppSettings settings)
    {
        var points = BuildTrail(26);
        if (points.Count < 2) return;

        var opacity = Math.Clamp(settings.Trail.Opacity, 0f, 1f);
        var thickness = Math.Clamp(settings.Trail.Thickness, 0.5f, 60f);
        var color = settings.Trail.Color;

        // Soft outer glow pass.
        for (var i = 1; i < points.Count; i++)
        {
            var t = points[i].Weight;
            var alpha = opacity * MathF.Pow(t, 1.7f) * 0.22f;
            if (alpha <= 0.004f) continue;

            var width = MathF.Max(1f, thickness * (0.3f + 0.7f * t) * 2.2f);
            using var pen = new Pen(OverlayPaint.WithAlpha(color, alpha), width)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
                LineJoin = LineJoin.Round,
            };
            graphics.DrawLine(pen, points[i - 1].Point, points[i].Point);
        }

        // Bright core pass.
        for (var i = 1; i < points.Count; i++)
        {
            var t = points[i].Weight;
            var alpha = opacity * MathF.Pow(t, 1.25f);
            if (alpha <= 0.004f) continue;

            var width = MathF.Max(0.6f, thickness * (0.22f + 0.78f * t));
            using var pen = new Pen(OverlayPaint.WithAlpha(color, alpha), width)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
                LineJoin = LineJoin.Round,
            };
            graphics.DrawLine(pen, points[i - 1].Point, points[i].Point);
        }
    }

    private List<(PointF Point, float Weight)> BuildTrail(int maxPoints)
    {
        var result = new List<(PointF Point, float Weight)>(maxPoints);
        var count = _trail.Count;
        if (count == 0) return result;

        var sampleCount = Math.Min(maxPoints, count);
        for (var i = 0; i < sampleCount; i++)
        {
            var index = sampleCount == 1
                ? count - 1
                : (int)Math.Round(i * (count - 1) / (double)(sampleCount - 1));
            var source = _trail[Math.Clamp(index, 0, count - 1)];
            var weight = sampleCount == 1 ? 1f : i / (float)(sampleCount - 1);
            result.Add((new PointF(source.X, source.Y), weight));
        }

        // Light smoothing to hide sampling jitter.
        for (var i = 1; i < result.Count - 1; i++)
        {
            var previous = result[i - 1].Point;
            var current = result[i].Point;
            var next = result[i + 1].Point;
            var smoothed = new PointF(
                (previous.X + current.X * 2f + next.X) / 4f,
                (previous.Y + current.Y * 2f + next.Y) / 4f);
            result[i] = (smoothed, result[i].Weight);
        }

        return result;
    }

    private void DrawRipples(Graphics graphics)
    {
        foreach (var ripple in _ripples)
        {
            var progress = (float)((_clock - ripple.Start) / ripple.Duration);
            if (progress <= 0f || progress >= 1f) continue;

            var eased = 1f - MathF.Pow(1f - progress, 3f);
            var radius = MathF.Max(1f, ripple.MaxRadius * eased);
            var alpha = (1f - progress) * (1f - progress);

            using (var glowPen = new Pen(OverlayPaint.WithAlpha(ripple.Color, alpha * 0.22f),
                       MathF.Max(1.5f, 7f * (1f - progress) + 1f)))
            {
                graphics.DrawEllipse(glowPen,
                    ripple.X - radius, ripple.Y - radius, radius * 2f, radius * 2f);
            }

            using (var pen = new Pen(OverlayPaint.WithAlpha(ripple.Color, alpha),
                       MathF.Max(0.6f, ripple.Thickness * (1f - progress) + 0.6f)))
            {
                graphics.DrawEllipse(pen,
                    ripple.X - radius, ripple.Y - radius, radius * 2f, radius * 2f);
            }

            if (ripple.Rings <= 1) continue;

            var innerRadius = radius * 0.62f;
            using var innerPen = new Pen(OverlayPaint.WithAlpha(ripple.Color, alpha * 0.5f),
                MathF.Max(0.5f, ripple.Thickness * 0.7f * (1f - progress) + 0.5f));
            graphics.DrawEllipse(innerPen,
                ripple.X - innerRadius, ripple.Y - innerRadius, innerRadius * 2f, innerRadius * 2f);
        }
    }

    private void DrawParticles(Graphics graphics)
    {
        foreach (var particle in _particles)
        {
            var life = Math.Clamp(particle.Age / MathF.Max(0.001f, particle.Lifetime), 0f, 1f);
            var alpha = MathF.Pow(1f - life, 1.4f);
            if (alpha <= 0.01f) continue;

            var radius = particle.Size * (0.35f + 0.65f * (1f - life));

            FillRadial(graphics, particle.X, particle.Y, radius * 2.6f,
                OverlayPaint.WithAlpha(particle.Color, alpha * 0.25f),
                OverlayPaint.WithAlpha(particle.Color, 0f));

            using var brush = new SolidBrush(OverlayPaint.WithAlpha(particle.Color, alpha));
            graphics.FillEllipse(brush,
                particle.X - radius, particle.Y - radius, radius * 2f, radius * 2f);
        }
    }

    private static void FillRadial(Graphics graphics, float centerX, float centerY, float radius, Color center, Color edge)
    {
        if (radius <= 0.5f || center.A == 0) return;

        using var path = new GraphicsPath();
        path.AddEllipse(centerX - radius, centerY - radius, radius * 2f, radius * 2f);

        using var brush = new PathGradientBrush(path)
        {
            CenterColor = center,
            CenterPoint = new PointF(centerX, centerY),
        };

        var surround = new Color[path.PointCount];
        for (var i = 0; i < surround.Length; i++) surround[i] = edge;
        brush.SurroundColors = surround;

        graphics.FillPath(brush, path);
    }

    private static RectangleF Circle(PointF center, float radius) =>
        new(center.X - radius, center.Y - radius, radius * 2f, radius * 2f);

    private static RectangleF Union(RectangleF a, RectangleF b)
    {
        if (a.Width <= 0 || a.Height <= 0) return b;
        if (b.Width <= 0 || b.Height <= 0) return a;

        var left = MathF.Min(a.Left, b.Left);
        var top = MathF.Min(a.Top, b.Top);
        var right = MathF.Max(a.Right, b.Right);
        var bottom = MathF.Max(a.Bottom, b.Bottom);
        return RectangleF.FromLTRB(left, top, right, bottom);
    }
}
