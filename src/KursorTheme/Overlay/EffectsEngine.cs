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
        public float Angle;
        public float Seed;
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
            bounds = Union(bounds, Circle(new PointF(particle.X, particle.Y), particle.Size * 8f + 10f));

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
            DrawRipples(graphics, settings.RippleStyle);
            DrawParticles(graphics, settings.ParticleStyle);
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
            var seed = (float)_random.NextDouble();
            double angle;
            if (settings.ParticleStyle == ParticleStyle.Code)
            {
                // Code particles rain downward with a small horizontal spread.
                angle = Math.PI / 2 + (_random.NextDouble() - 0.5) * 0.7;
            }
            else
            {
                angle = _random.NextDouble() * Math.PI * 2.0;
            }

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
                Angle = (float)angle,
                Seed = seed,
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

        var color = settings.Trail.Color;
        var thickness = Math.Clamp(settings.Trail.Thickness, 0.5f, 60f);
        var opacity = Math.Clamp(settings.Trail.Opacity, 0f, 1f);
        if (opacity <= 0.004f) return;

        switch (settings.TrailStyle)
        {
            case TrailStyle.Comet:
                DrawTrailComet(graphics, points, color, thickness, opacity);
                break;
            case TrailStyle.Sparks:
                DrawTrailSparks(graphics, points, color, thickness, opacity);
                break;
            case TrailStyle.Pixels:
                DrawTrailPixels(graphics, points, color, thickness, opacity);
                break;
            case TrailStyle.Ink:
                DrawTrailInk(graphics, points, color, thickness, opacity);
                break;
            case TrailStyle.Neon:
                DrawTrailNeon(graphics, points, color, thickness, opacity);
                break;
            case TrailStyle.Smoke:
                DrawTrailSmoke(graphics, points, color, thickness, opacity);
                break;
            case TrailStyle.Lightning:
                DrawTrailLightning(graphics, points, color, thickness, opacity);
                break;
            case TrailStyle.Dotted:
                DrawTrailDotted(graphics, points, color, thickness, opacity);
                break;
            default:
                DrawTrailRibbon(graphics, points, color, thickness, opacity);
                break;
        }
    }

    private static void DrawTrailRibbon(Graphics graphics, List<(PointF Point, float Weight)> points, Color color, float thickness, float opacity)
    {
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

    private static void DrawTrailComet(Graphics graphics, List<(PointF Point, float Weight)> points, Color color, float thickness, float opacity)
    {
        for (var i = 1; i < points.Count; i++)
        {
            var t = points[i].Weight;
            var alpha = opacity * MathF.Pow(t, 1.15f);
            if (alpha <= 0.004f) continue;

            var radius = MathF.Max(0.7f, thickness * (0.18f + 0.82f * t) * 0.55f);
            if (i % 2 == 0)
            {
                FillRadial(graphics, points[i].Point.X, points[i].Point.Y, radius * 2.6f,
                    OverlayPaint.WithAlpha(color, alpha * 0.35f),
                    OverlayPaint.WithAlpha(color, 0f));
            }

            using var brush = new SolidBrush(OverlayPaint.WithAlpha(color, alpha));
            graphics.FillEllipse(brush,
                points[i].Point.X - radius, points[i].Point.Y - radius,
                radius * 2f, radius * 2f);
        }
    }

    private static void DrawTrailSparks(Graphics graphics, List<(PointF Point, float Weight)> points, Color color, float thickness, float opacity)
    {
        for (var i = 1; i < points.Count; i++)
        {
            var previous = points[i - 1].Point;
            var current = points[i].Point;
            var dx = current.X - previous.X;
            var dy = current.Y - previous.Y;
            var length = MathF.Sqrt(dx * dx + dy * dy);
            if (length < 0.01f) continue;

            var nx = dx / length;
            var ny = dy / length;
            var px = -ny;
            var py = nx;
            var t = points[i].Weight;
            var alpha = opacity * MathF.Pow(t, 1.35f);
            var spread = thickness * (0.35f + 0.65f * t);

            for (var k = -1; k <= 1; k++)
            {
                var jitter = Noise(i * 7 + k, 0.7f) * spread * 0.5f;
                var offset = k * spread * 0.55f + jitter;
                var start = new PointF(
                    previous.X + px * offset - nx * length * 0.15f,
                    previous.Y + py * offset - ny * length * 0.15f);
                var end = new PointF(
                    current.X + px * offset + nx * length * 0.25f,
                    current.Y + py * offset + ny * length * 0.25f);

                using var pen = new Pen(
                    OverlayPaint.WithAlpha(color, alpha * (k == 0 ? 0.95f : 0.45f)),
                    MathF.Max(0.8f, thickness * 0.18f))
                {
                    StartCap = LineCap.Round,
                    EndCap = LineCap.Round,
                };
                graphics.DrawLine(pen, start, end);
            }
        }
    }

    private static void DrawTrailPixels(Graphics graphics, List<(PointF Point, float Weight)> points, Color color, float thickness, float opacity)
    {
        for (var i = 1; i < points.Count; i++)
        {
            var t = points[i].Weight;
            var alpha = opacity * MathF.Pow(t, 1.2f);
            if (alpha <= 0.004f) continue;

            var size = MathF.Max(2f, thickness * (0.25f + 0.65f * t));
            var x = MathF.Round(points[i].Point.X - size / 2f);
            var y = MathF.Round(points[i].Point.Y - size / 2f);

            if (i % 2 == 0)
            {
                using var glow = new SolidBrush(OverlayPaint.WithAlpha(color, alpha * 0.12f));
                graphics.FillRectangle(glow, x - size * 0.5f, y - size * 0.5f, size * 2f, size * 2f);
            }

            using var brush = new SolidBrush(OverlayPaint.WithAlpha(color, alpha));
            graphics.FillRectangle(brush, x, y, size, size);
        }
    }

    private static void DrawTrailInk(Graphics graphics, List<(PointF Point, float Weight)> points, Color color, float thickness, float opacity)
    {
        for (var i = 1; i < points.Count; i++)
        {
            var t = points[i].Weight;
            var alpha = opacity * MathF.Pow(t, 0.9f) * 0.65f;
            if (alpha <= 0.004f) continue;

            var point = points[i].Point;
            var radius = thickness * (0.35f + 0.45f * MathF.Abs(MathF.Sin(i * 1.7f)));
            using (var brush = new SolidBrush(OverlayPaint.WithAlpha(color, alpha)))
            {
                graphics.FillEllipse(brush, point.X - radius, point.Y - radius, radius * 2f, radius * 2f);
            }

            var secondary = radius * 0.55f;
            using var secondaryBrush = new SolidBrush(OverlayPaint.WithAlpha(color, alpha * 0.5f));
            graphics.FillEllipse(secondaryBrush,
                point.X + radius * 0.4f - secondary, point.Y - radius * 0.3f - secondary,
                secondary * 2f, secondary * 2f);
        }
    }

    private static void DrawTrailNeon(Graphics graphics, List<(PointF Point, float Weight)> points, Color color, float thickness, float opacity)
    {
        for (var i = 1; i < points.Count; i++)
        {
            var t = points[i].Weight;
            var glowAlpha = opacity * MathF.Pow(t, 1.4f) * 0.35f;
            if (glowAlpha <= 0.004f) continue;

            using var glow = new Pen(OverlayPaint.WithAlpha(color, glowAlpha),
                MathF.Max(2f, thickness * (0.4f + 1.1f * t) * 2.6f))
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
                LineJoin = LineJoin.Round,
            };
            graphics.DrawLine(glow, points[i - 1].Point, points[i].Point);
        }

        for (var i = 1; i < points.Count; i++)
        {
            var t = points[i].Weight;
            var alpha = opacity * MathF.Pow(t, 1.05f);
            if (alpha <= 0.004f) continue;

            using var core = new Pen(OverlayPaint.WithAlpha(color, alpha),
                MathF.Max(0.8f, thickness * (0.18f + 0.55f * t)))
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
                LineJoin = LineJoin.Round,
            };
            graphics.DrawLine(core, points[i - 1].Point, points[i].Point);
        }
    }

    private static void DrawTrailSmoke(Graphics graphics, List<(PointF Point, float Weight)> points, Color color, float thickness, float opacity)
    {
        for (var i = 1; i < points.Count; i++)
        {
            var t = points[i].Weight;
            var alpha = opacity * MathF.Pow(t, 1.1f) * 0.12f;
            if (alpha <= 0.004f) continue;

            var radius = thickness * (0.9f + 0.6f * MathF.Abs(MathF.Sin(i * 0.8f)));
            FillRadial(graphics, points[i].Point.X, points[i].Point.Y, radius * 1.4f,
                OverlayPaint.WithAlpha(color, alpha),
                OverlayPaint.WithAlpha(color, 0f));
        }
    }

    private void DrawTrailLightning(Graphics graphics, List<(PointF Point, float Weight)> points, Color color, float thickness, float opacity)
    {
        var jittered = new PointF[points.Count];
        for (var i = 0; i < points.Count; i++)
        {
            var point = points[i].Point;
            if (i > 0 && i < points.Count - 1)
            {
                var previous = points[i - 1].Point;
                var next = points[i + 1].Point;
                var dx = next.X - previous.X;
                var dy = next.Y - previous.Y;
                var length = MathF.Sqrt(dx * dx + dy * dy);
                if (length > 0.01f)
                {
                    var px = -dy / length;
                    var py = dx / length;
                    var offset = Noise(i, (float)_clock * 6f) * thickness * 1.1f;
                    point = new PointF(point.X + px * offset, point.Y + py * offset);
                }
            }

            jittered[i] = point;
        }

        using (var glow = new Pen(OverlayPaint.WithAlpha(color, opacity * 0.2f),
                   MathF.Max(2f, thickness * 2.2f))
               {
                   StartCap = LineCap.Round,
                   EndCap = LineCap.Round,
                   LineJoin = LineJoin.Round,
               })
        {
            graphics.DrawLines(glow, jittered);
        }

        using (var core = new Pen(OverlayPaint.WithAlpha(color, opacity * 0.95f),
                   MathF.Max(0.8f, thickness * 0.3f))
               {
                   StartCap = LineCap.Round,
                   EndCap = LineCap.Round,
                   LineJoin = LineJoin.Round,
               })
        {
            graphics.DrawLines(core, jittered);
        }
    }

    private static void DrawTrailDotted(Graphics graphics, List<(PointF Point, float Weight)> points, Color color, float thickness, float opacity)
    {
        var spacing = MathF.Max(4f, thickness * 0.9f);
        var hasPrevious = false;
        var previous = PointF.Empty;

        for (var i = 1; i < points.Count; i++)
        {
            var point = points[i].Point;
            if (hasPrevious)
            {
                var dx = point.X - previous.X;
                var dy = point.Y - previous.Y;
                if (MathF.Sqrt(dx * dx + dy * dy) < spacing) continue;
            }

            var t = points[i].Weight;
            var radius = MathF.Max(1f, thickness * (0.15f + 0.18f * t));
            using var brush = new SolidBrush(OverlayPaint.WithAlpha(color, opacity * MathF.Pow(t, 1.2f)));
            graphics.FillEllipse(brush, point.X - radius, point.Y - radius, radius * 2f, radius * 2f);
            previous = point;
            hasPrevious = true;
        }
    }

    private static float Noise(float index, float salt) =>
        MathF.Sin(index * 12.9898f + salt * 78.233f) * 0.5f;

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

    private void DrawRipples(Graphics graphics, RippleStyle style)
    {
        foreach (var ripple in _ripples)
        {
            var progress = (float)((_clock - ripple.Start) / ripple.Duration);
            if (progress <= 0f || progress >= 1f) continue;

            var eased = 1f - MathF.Pow(1f - progress, 3f);
            var radius = MathF.Max(1f, ripple.MaxRadius * eased);
            var alpha = (1f - progress) * (1f - progress);

            switch (style)
            {
                case RippleStyle.DoubleRing:
                    DrawRippleCircle(graphics, ripple, radius, alpha, progress, Math.Max(2, ripple.Rings));
                    break;
                case RippleStyle.Shockwave:
                    DrawRippleShockwave(graphics, ripple, radius, alpha, progress);
                    break;
                case RippleStyle.PixelSquare:
                    DrawRippleSquare(graphics, ripple, radius, alpha, progress);
                    break;
                case RippleStyle.Ink:
                    DrawRippleInk(graphics, ripple, radius, alpha, progress);
                    break;
                case RippleStyle.Glitch:
                    DrawRippleGlitch(graphics, ripple, radius, alpha, progress);
                    break;
                case RippleStyle.Pulse:
                    DrawRipplePulse(graphics, ripple, radius, alpha, progress);
                    break;
                case RippleStyle.Spiral:
                    DrawRippleSpiral(graphics, ripple, radius, alpha, progress);
                    break;
                default:
                    DrawRippleCircle(graphics, ripple, radius, alpha, progress, ripple.Rings);
                    break;
            }
        }
    }

    private static void DrawRippleCircle(Graphics graphics, RippleState ripple, float radius, float alpha, float progress, int rings)
    {
        using (var glow = new Pen(OverlayPaint.WithAlpha(ripple.Color, alpha * 0.22f),
                   MathF.Max(1.5f, 7f * (1f - progress) + 1f)))
        {
            graphics.DrawEllipse(glow, ripple.X - radius, ripple.Y - radius, radius * 2f, radius * 2f);
        }

        using (var pen = new Pen(OverlayPaint.WithAlpha(ripple.Color, alpha),
                   MathF.Max(0.6f, ripple.Thickness * (1f - progress) + 0.6f)))
        {
            graphics.DrawEllipse(pen, ripple.X - radius, ripple.Y - radius, radius * 2f, radius * 2f);
        }

        if (rings <= 1) return;

        var innerRadius = radius * 0.62f;
        using var innerPen = new Pen(OverlayPaint.WithAlpha(ripple.Color, alpha * 0.5f),
            MathF.Max(0.5f, ripple.Thickness * 0.7f * (1f - progress) + 0.5f));
        graphics.DrawEllipse(innerPen,
            ripple.X - innerRadius, ripple.Y - innerRadius, innerRadius * 2f, innerRadius * 2f);
    }

    private static void DrawRippleShockwave(Graphics graphics, RippleState ripple, float radius, float alpha, float progress)
    {
        using (var glow = new Pen(OverlayPaint.WithAlpha(ripple.Color, alpha * 0.12f),
                   MathF.Max(3f, 12f * (1f - progress) + 3f)))
        {
            graphics.DrawEllipse(glow, ripple.X - radius, ripple.Y - radius, radius * 2f, radius * 2f);
        }

        using (var pen = new Pen(OverlayPaint.WithAlpha(ripple.Color, alpha * 0.9f),
                   MathF.Max(1f, ripple.Thickness * (1f - progress) + 1f)))
        {
            graphics.DrawEllipse(pen, ripple.X - radius, ripple.Y - radius, radius * 2f, radius * 2f);
        }

        var innerRadius = radius * 0.9f;
        using var inner = new Pen(OverlayPaint.WithAlpha(ripple.Color, alpha * 0.35f),
            MathF.Max(0.7f, ripple.Thickness * 0.6f * (1f - progress) + 0.5f));
        graphics.DrawEllipse(inner,
            ripple.X - innerRadius, ripple.Y - innerRadius, innerRadius * 2f, innerRadius * 2f);
    }

    private static void DrawRippleSquare(Graphics graphics, RippleState ripple, float radius, float alpha, float progress)
    {
        var size = MathF.Max(2f, MathF.Round(radius / 8f) * 8f);
        var rect = new RectangleF(ripple.X - size, ripple.Y - size, size * 2f, size * 2f);

        using (var fill = new SolidBrush(OverlayPaint.WithAlpha(ripple.Color, alpha * 0.04f)))
        {
            graphics.FillRectangle(fill, rect);
        }

        using (var pen = new Pen(OverlayPaint.WithAlpha(ripple.Color, alpha),
                   MathF.Max(0.8f, ripple.Thickness * (1f - progress) + 0.8f)))
        {
            graphics.DrawRectangle(pen, rect);
        }

        var block = MathF.Max(3f, ripple.Thickness * 2f);
        using (var brush = new SolidBrush(OverlayPaint.WithAlpha(ripple.Color, alpha * 0.85f)))
        {
            graphics.FillRectangle(brush, rect.Left - block / 2f, rect.Top - block / 2f, block, block);
            graphics.FillRectangle(brush, rect.Right - block / 2f, rect.Top - block / 2f, block, block);
            graphics.FillRectangle(brush, rect.Left - block / 2f, rect.Bottom - block / 2f, block, block);
            graphics.FillRectangle(brush, rect.Right - block / 2f, rect.Bottom - block / 2f, block, block);
        }
    }

    private static void DrawRippleInk(Graphics graphics, RippleState ripple, float radius, float alpha, float progress)
    {
        for (var k = 0; k < 3; k++)
        {
            var jitter = Noise((float)(k * 3f + ripple.Start * 0.37f), progress * 3f) * radius * 0.06f;
            var ringRadius = radius * (0.9f + 0.1f * k) + jitter;
            using var pen = new Pen(OverlayPaint.WithAlpha(ripple.Color, alpha * (0.65f - 0.15f * k)),
                MathF.Max(0.6f, ripple.Thickness * (1f - progress) * (1.2f - 0.25f * k) + 0.5f));
            graphics.DrawEllipse(pen,
                ripple.X - ringRadius, ripple.Y - ringRadius, ringRadius * 2f, ringRadius * 2f);
        }
    }

    private static void DrawRippleGlitch(Graphics graphics, RippleState ripple, float radius, float alpha, float progress)
    {
        for (var k = 0; k < 4; k++)
        {
            var offsetY = (k - 1.5f) * radius * 0.07f;
            using var pen = new Pen(OverlayPaint.WithAlpha(ripple.Color, alpha * (0.35f + 0.16f * k)),
                MathF.Max(1f, ripple.Thickness * (1f - progress) + 1f));
            graphics.DrawArc(pen,
                ripple.X - radius, ripple.Y - radius + offsetY,
                radius * 2f, radius * 2f, 190 + k * 37, 34);
        }

        var barWidth = radius * 0.55f;
        using var brush = new SolidBrush(OverlayPaint.WithAlpha(ripple.Color, alpha * 0.4f));
        graphics.FillRectangle(brush, ripple.X - barWidth / 2f, ripple.Y - radius * 0.3f, barWidth, 1.5f);
        graphics.FillRectangle(brush, ripple.X - barWidth / 3f, ripple.Y + radius * 0.25f, barWidth * 0.7f, 1.2f);
    }

    private static void DrawRipplePulse(Graphics graphics, RippleState ripple, float radius, float alpha, float progress)
    {
        FillRadial(graphics, ripple.X, ripple.Y, radius,
            OverlayPaint.WithAlpha(ripple.Color, alpha * 0.28f),
            OverlayPaint.WithAlpha(ripple.Color, 0f));

        using var pen = new Pen(OverlayPaint.WithAlpha(ripple.Color, alpha),
            MathF.Max(0.6f, ripple.Thickness * (1f - progress) + 0.6f));
        graphics.DrawEllipse(pen, ripple.X - radius, ripple.Y - radius, radius * 2f, radius * 2f);
    }

    private static void DrawRippleSpiral(Graphics graphics, RippleState ripple, float radius, float alpha, float progress)
    {
        var start = -90f + progress * 400f;
        using (var pen = new Pen(OverlayPaint.WithAlpha(ripple.Color, alpha * 0.9f),
                   MathF.Max(0.8f, ripple.Thickness * (1f - progress) + 0.8f))
               {
                   StartCap = LineCap.Round,
                   EndCap = LineCap.Round,
               })
        {
            graphics.DrawArc(pen, ripple.X - radius, ripple.Y - radius, radius * 2f, radius * 2f, start, 280);
        }

        var innerRadius = radius * 0.7f;
        using var inner = new Pen(OverlayPaint.WithAlpha(ripple.Color, alpha * 0.5f),
            MathF.Max(0.6f, ripple.Thickness * 0.7f * (1f - progress) + 0.6f))
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
        };
        graphics.DrawArc(inner,
            ripple.X - innerRadius, ripple.Y - innerRadius,
            innerRadius * 2f, innerRadius * 2f, start + 140, 200);
    }

    private void DrawParticles(Graphics graphics, ParticleStyle style)
    {
        foreach (var particle in _particles)
        {
            var life = Math.Clamp(particle.Age / MathF.Max(0.001f, particle.Lifetime), 0f, 1f);
            var alpha = MathF.Pow(1f - life, 1.4f);
            if (alpha <= 0.01f) continue;

            var radius = particle.Size * (0.35f + 0.65f * (1f - life));

            switch (style)
            {
                case ParticleStyle.Sparks:
                    DrawParticleSparks(graphics, particle, alpha, radius);
                    break;
                case ParticleStyle.Pixels:
                    DrawParticlePixels(graphics, particle, alpha, radius);
                    break;
                case ParticleStyle.Code:
                    DrawParticleCode(graphics, particle, alpha);
                    break;
                case ParticleStyle.Paint:
                    DrawParticlePaint(graphics, particle, alpha, life);
                    break;
                case ParticleStyle.Glitch:
                    DrawParticleGlitch(graphics, particle, alpha);
                    break;
                case ParticleStyle.Stars:
                    DrawParticleStars(graphics, particle, alpha, life);
                    break;
                case ParticleStyle.Bubbles:
                    DrawParticleBubbles(graphics, particle, alpha, life);
                    break;
                default:
                    DrawParticleDots(graphics, particle, alpha, radius);
                    break;
            }
        }
    }

    private static void DrawParticleDots(Graphics graphics, ParticleState particle, float alpha, float radius)
    {
        FillRadial(graphics, particle.X, particle.Y, radius * 2.6f,
            OverlayPaint.WithAlpha(particle.Color, alpha * 0.25f),
            OverlayPaint.WithAlpha(particle.Color, 0f));

        using var brush = new SolidBrush(OverlayPaint.WithAlpha(particle.Color, alpha));
        graphics.FillEllipse(brush,
            particle.X - radius, particle.Y - radius, radius * 2f, radius * 2f);
    }

    private static void DrawParticleSparks(Graphics graphics, ParticleState particle, float alpha, float radius)
    {
        var speed = MathF.Sqrt(particle.VelocityX * particle.VelocityX + particle.VelocityY * particle.VelocityY);
        if (speed < 1f)
        {
            DrawParticleDots(graphics, particle, alpha, radius);
            return;
        }

        var nx = particle.VelocityX / speed;
        var ny = particle.VelocityY / speed;
        var length = Math.Clamp(particle.Size * 4f + speed * 0.03f, 4f, 30f);

        using (var pen = new Pen(OverlayPaint.WithAlpha(particle.Color, alpha),
                   MathF.Max(0.8f, particle.Size * 0.35f))
               {
                   StartCap = LineCap.Round,
                   EndCap = LineCap.Round,
               })
        {
            graphics.DrawLine(pen,
                particle.X, particle.Y,
                particle.X - nx * length, particle.Y - ny * length);
        }

        using var head = new SolidBrush(OverlayPaint.WithAlpha(particle.Color, Math.Min(1f, alpha * 1.15f)));
        graphics.FillEllipse(head,
            particle.X - radius * 0.7f, particle.Y - radius * 0.7f, radius * 1.4f, radius * 1.4f);
    }

    private static void DrawParticlePixels(Graphics graphics, ParticleState particle, float alpha, float radius)
    {
        var size = MathF.Max(2f, radius * 1.3f);
        var x = MathF.Round(particle.X - size / 2f);
        var y = MathF.Round(particle.Y - size / 2f);

        if (particle.Seed > 0.6f)
        {
            using var glow = new SolidBrush(OverlayPaint.WithAlpha(particle.Color, alpha * 0.12f));
            graphics.FillRectangle(glow, x - size * 0.5f, y - size * 0.5f, size * 2f, size * 2f);
        }

        using var brush = new SolidBrush(OverlayPaint.WithAlpha(particle.Color, alpha));
        graphics.FillRectangle(brush, x, y, size, size);
    }

    private static void DrawParticleCode(Graphics graphics, ParticleState particle, float alpha)
    {
        var width = MathF.Max(1.2f, particle.Size * 0.45f);
        var height = particle.Size * (2f + particle.Seed * 4f);
        var x = MathF.Round(particle.X - width / 2f);
        var y = MathF.Round(particle.Y - height / 2f);

        using (var tail = new SolidBrush(OverlayPaint.WithAlpha(particle.Color, alpha * 0.18f)))
        {
            graphics.FillRectangle(tail, x, y - height * 0.9f, width, height * 1.8f);
        }

        using (var bar = new SolidBrush(OverlayPaint.WithAlpha(particle.Color, alpha)))
        {
            graphics.FillRectangle(bar, x, y, width, height);
        }

        var head = MathF.Max(1.5f, width * 1.6f);
        using var bright = new SolidBrush(OverlayPaint.WithAlpha(particle.Color, Math.Min(1f, alpha * 1.25f)));
        graphics.FillRectangle(bright, x - (head - width) / 2f, y + height - head, head, head);
    }

    private static void DrawParticlePaint(Graphics graphics, ParticleState particle, float alpha, float life)
    {
        var main = MathF.Max(1.5f, particle.Size * (0.7f + 0.4f * (1f - life)));
        using (var brush = new SolidBrush(OverlayPaint.WithAlpha(particle.Color, alpha * 0.8f)))
        {
            graphics.FillEllipse(brush,
                particle.X - main, particle.Y - main, main * 2f, main * 2f);
        }

        for (var k = 0; k < 2; k++)
        {
            var angle = particle.Seed * 6.283f + k * 2.1f;
            var distance = main * (0.7f + 0.5f * k);
            var radius = main * (0.35f - 0.1f * k);
            using var brush = new SolidBrush(OverlayPaint.WithAlpha(particle.Color, alpha * 0.45f));
            graphics.FillEllipse(brush,
                particle.X + MathF.Cos(angle) * distance - radius,
                particle.Y + MathF.Sin(angle) * distance - radius,
                radius * 2f, radius * 2f);
        }

        var speed = MathF.Sqrt(particle.VelocityX * particle.VelocityX + particle.VelocityY * particle.VelocityY);
        if (speed < 20f) return;

        var nx = particle.VelocityX / speed;
        var ny = particle.VelocityY / speed;
        using var streak = new Pen(OverlayPaint.WithAlpha(particle.Color, alpha * 0.4f), MathF.Max(0.8f, main * 0.3f))
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
        };
        graphics.DrawLine(streak, particle.X, particle.Y,
            particle.X - nx * main * 1.6f, particle.Y - ny * main * 1.6f);
    }

    private static void DrawParticleGlitch(Graphics graphics, ParticleState particle, float alpha)
    {
        var width = MathF.Max(4f, particle.Size * 3.2f);
        var height = MathF.Max(1.2f, particle.Size * 0.35f);
        var y = particle.Y + (particle.Seed - 0.5f) * particle.Size;

        using (var brush = new SolidBrush(OverlayPaint.WithAlpha(particle.Color, alpha)))
        {
            graphics.FillRectangle(brush, particle.X - width / 2f, y, width, height);
        }

        using (var ghost = new SolidBrush(OverlayPaint.WithAlpha(particle.Color, alpha * 0.45f)))
        {
            graphics.FillRectangle(ghost,
                particle.X - width / 3f + (particle.Seed - 0.5f) * width * 0.8f,
                y + height * 2.4f, width * 0.6f, height * 0.8f);
        }
    }

    private static void DrawParticleStars(Graphics graphics, ParticleState particle, float alpha, float life)
    {
        var radius = MathF.Max(1.5f, particle.Size * (0.7f + 0.5f * (1f - life)));

        using (var pen = new Pen(OverlayPaint.WithAlpha(particle.Color, alpha),
                   MathF.Max(0.8f, particle.Size * 0.22f))
               {
                   StartCap = LineCap.Round,
                   EndCap = LineCap.Round,
               })
        {
            graphics.DrawLine(pen, particle.X - radius, particle.Y, particle.X + radius, particle.Y);
            graphics.DrawLine(pen, particle.X, particle.Y - radius, particle.X, particle.Y + radius);
        }

        var diagonal = radius * 0.55f;
        using (var spark = new Pen(OverlayPaint.WithAlpha(particle.Color, alpha * 0.45f),
                   MathF.Max(0.6f, particle.Size * 0.14f)))
        {
            graphics.DrawLine(spark, particle.X - diagonal, particle.Y - diagonal,
                particle.X + diagonal, particle.Y + diagonal);
            graphics.DrawLine(spark, particle.X - diagonal, particle.Y + diagonal,
                particle.X + diagonal, particle.Y - diagonal);
        }

        using var core = new SolidBrush(OverlayPaint.WithAlpha(particle.Color, Math.Min(1f, alpha * 1.1f)));
        graphics.FillEllipse(core,
            particle.X - radius * 0.22f, particle.Y - radius * 0.22f,
            radius * 0.44f, radius * 0.44f);
    }

    private static void DrawParticleBubbles(Graphics graphics, ParticleState particle, float alpha, float life)
    {
        var radius = MathF.Max(1.5f, particle.Size * (0.7f + 0.5f * (1f - life)));

        using (var fill = new SolidBrush(OverlayPaint.WithAlpha(particle.Color, alpha * 0.1f)))
        {
            graphics.FillEllipse(fill,
                particle.X - radius, particle.Y - radius, radius * 2f, radius * 2f);
        }

        using (var pen = new Pen(OverlayPaint.WithAlpha(particle.Color, alpha),
                   MathF.Max(0.8f, particle.Size * 0.22f)))
        {
            graphics.DrawEllipse(pen,
                particle.X - radius, particle.Y - radius, radius * 2f, radius * 2f);
        }

        using var highlight = new SolidBrush(OverlayPaint.WithAlpha(particle.Color, alpha * 0.7f));
        graphics.FillEllipse(highlight,
            particle.X - radius * 0.45f, particle.Y - radius * 0.45f,
            radius * 0.35f, radius * 0.35f);
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
