using System.Drawing;

namespace KursorTheme.Config;

public sealed class Preset
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Tagline { get; init; }
    public string Tag { get; init; } = string.Empty;
    public required Color Accent { get; init; }
    public required Color Secondary { get; init; }
    public required Action<AppSettings> Apply { get; init; }
}

public static class Presets
{
    private static Color C(int r, int g, int b) => Color.FromArgb(r, g, b);

    public static readonly IReadOnlyList<Preset> All = new List<Preset>
    {
        new()
        {
            Id = "neon",
            Name = "Neon",
            Tagline = "Циан и фиолетовый, выразительный шлейф",
            Tag = "неон",
            Accent = C(0x36, 0xE5, 0xFF),
            Secondary = C(0x8B, 0x5C, 0xF6),
            Apply = s =>
            {
                s.Glow.Enabled = true;
                s.Glow.Color = C(0x36, 0xE5, 0xFF);
                s.Glow.Radius = 46f;
                s.Glow.Intensity = 0.55f;
                s.Glow.Pulse = 0.25f;
                s.Glow.PulseSpeed = 0.9f;

                s.Trail.Enabled = true;
                s.Trail.Color = C(0x36, 0xE5, 0xFF);
                s.Trail.LengthMs = 420;
                s.Trail.Thickness = 10f;
                s.Trail.Opacity = 0.5f;

                s.Ripple.Enabled = true;
                s.Ripple.Color = C(0x36, 0xE5, 0xFF);
                s.Ripple.MaxRadius = 90f;
                s.Ripple.DurationMs = 620;
                s.Ripple.Thickness = 2.4f;
                s.Ripple.Rings = 2;

                s.Particles.Enabled = true;
                s.Particles.Color = C(0x8B, 0x5C, 0xF6);
                s.Particles.Count = 12;
                s.Particles.Speed = 340f;
                s.Particles.Gravity = 700f;
                s.Particles.LifetimeMs = 720;
                s.Particles.Size = 3.4f;

                s.Selection.FillColor = C(0x36, 0xE5, 0xFF);
                s.Selection.FillOpacity = 0.10f;
                s.Selection.HatchColor = C(0x36, 0xE5, 0xFF);
                s.Selection.HatchOpacity = 0.55f;
                s.Selection.BorderColor = C(0x36, 0xE5, 0xFF);
                s.Selection.BorderOpacity = 0.95f;
            },
        },
        new()
        {
            Id = "ember",
            Name = "Ember",
            Tagline = "Тёплое оранжевое свечение и искры",
            Tag = "огонь",
            Accent = C(0xFF, 0x8A, 0x3D),
            Secondary = C(0xFF, 0x4D, 0x4D),
            Apply = s =>
            {
                s.Glow.Enabled = true;
                s.Glow.Color = C(0xFF, 0x8A, 0x3D);
                s.Glow.Radius = 40f;
                s.Glow.Intensity = 0.5f;
                s.Glow.Pulse = 0.18f;
                s.Glow.PulseSpeed = 0.6f;

                s.Trail.Enabled = true;
                s.Trail.Color = C(0xFF, 0x5C, 0x2B);
                s.Trail.LengthMs = 380;
                s.Trail.Thickness = 9f;
                s.Trail.Opacity = 0.55f;

                s.Ripple.Enabled = true;
                s.Ripple.Color = C(0xFF, 0xB1, 0x5C);
                s.Ripple.MaxRadius = 100f;
                s.Ripple.DurationMs = 700;
                s.Ripple.Thickness = 2.6f;
                s.Ripple.Rings = 2;

                s.Particles.Enabled = true;
                s.Particles.Color = C(0xFF, 0x4D, 0x4D);
                s.Particles.Count = 16;
                s.Particles.Speed = 300f;
                s.Particles.Gravity = 900f;
                s.Particles.LifetimeMs = 820;
                s.Particles.Size = 3.1f;

                s.Selection.FillColor = C(0xFF, 0x8A, 0x3D);
                s.Selection.FillOpacity = 0.12f;
                s.Selection.HatchColor = C(0xFF, 0x8A, 0x3D);
                s.Selection.HatchOpacity = 0.6f;
                s.Selection.BorderColor = C(0xFF, 0x8A, 0x3D);
                s.Selection.BorderOpacity = 1f;
            },
        },
        new()
        {
            Id = "aurora",
            Name = "Aurora",
            Tagline = "Мягкое северное сияние: мята и лаванда",
            Tag = "природа",
            Accent = C(0x3B, 0xFF, 0xC1),
            Secondary = C(0xA7, 0x8B, 0xFA),
            Apply = s =>
            {
                s.Glow.Enabled = true;
                s.Glow.Color = C(0x3B, 0xFF, 0xC1);
                s.Glow.Radius = 62f;
                s.Glow.Intensity = 0.45f;
                s.Glow.Pulse = 0.32f;
                s.Glow.PulseSpeed = 0.5f;

                s.Trail.Enabled = true;
                s.Trail.Color = C(0x3B, 0xFF, 0xC1);
                s.Trail.LengthMs = 520;
                s.Trail.Thickness = 12f;
                s.Trail.Opacity = 0.45f;

                s.Ripple.Enabled = true;
                s.Ripple.Color = C(0x7C, 0xFF, 0xE0);
                s.Ripple.MaxRadius = 112f;
                s.Ripple.DurationMs = 820;
                s.Ripple.Thickness = 2.2f;
                s.Ripple.Rings = 2;

                s.Particles.Enabled = true;
                s.Particles.Color = C(0xA7, 0x8B, 0xFA);
                s.Particles.Count = 16;
                s.Particles.Speed = 280f;
                s.Particles.Gravity = 520f;
                s.Particles.LifetimeMs = 900;
                s.Particles.Size = 2.9f;

                s.Selection.FillColor = C(0x3B, 0xFF, 0xC1);
                s.Selection.FillOpacity = 0.10f;
                s.Selection.HatchColor = C(0x7C, 0xFF, 0xE0);
                s.Selection.HatchOpacity = 0.55f;
                s.Selection.BorderColor = C(0x7C, 0xFF, 0xE0);
                s.Selection.BorderOpacity = 0.95f;
            },
        },
        new()
        {
            Id = "minimal",
            Name = "Minimal",
            Tagline = "Едва заметное белое свечение без частиц",
            Tag = "минимализм",
            Accent = C(0xFF, 0xFF, 0xFF),
            Secondary = C(0xB8, 0xC2, 0xD0),
            Apply = s =>
            {
                s.Glow.Enabled = true;
                s.Glow.Color = C(0xFF, 0xFF, 0xFF);
                s.Glow.Radius = 28f;
                s.Glow.Intensity = 0.22f;
                s.Glow.Pulse = 0.08f;
                s.Glow.PulseSpeed = 0.4f;

                s.Trail.Enabled = true;
                s.Trail.Color = C(0xFF, 0xFF, 0xFF);
                s.Trail.LengthMs = 260;
                s.Trail.Thickness = 7f;
                s.Trail.Opacity = 0.3f;

                s.Ripple.Enabled = true;
                s.Ripple.Color = C(0xFF, 0xFF, 0xFF);
                s.Ripple.MaxRadius = 70f;
                s.Ripple.DurationMs = 480;
                s.Ripple.Thickness = 1.5f;
                s.Ripple.Rings = 1;

                s.Particles.Enabled = false;
                s.Particles.Color = C(0xFF, 0xFF, 0xFF);
                s.Particles.Count = 0;
                s.Particles.Speed = 240f;
                s.Particles.Gravity = 500f;
                s.Particles.LifetimeMs = 500;
                s.Particles.Size = 2f;

                s.Selection.FillColor = C(0xFF, 0xFF, 0xFF);
                s.Selection.FillOpacity = 0.08f;
                s.Selection.HatchColor = C(0xFF, 0xFF, 0xFF);
                s.Selection.HatchOpacity = 0.35f;
                s.Selection.BorderColor = C(0xFF, 0xFF, 0xFF);
                s.Selection.BorderOpacity = 0.8f;
            },
        },
        new()
        {
            Id = "cyberpunk",
            Name = "Cyberpunk",
            Tagline = "Жёлтый неон, циан и глитч-частицы",
            Tag = "гик",
            Accent = C(0xFF, 0xE1, 0x4D),
            Secondary = C(0x00, 0xE5, 0xFF),
            Apply = s =>
            {
                s.Glow.Enabled = true;
                s.Glow.Color = C(0xFF, 0xE1, 0x4D);
                s.Glow.Radius = 52f;
                s.Glow.Intensity = 0.55f;
                s.Glow.Pulse = 0.3f;
                s.Glow.PulseSpeed = 1.1f;

                s.Trail.Enabled = true;
                s.Trail.Color = C(0x00, 0xE5, 0xFF);
                s.Trail.LengthMs = 480;
                s.Trail.Thickness = 11f;
                s.Trail.Opacity = 0.55f;

                s.Ripple.Enabled = true;
                s.Ripple.Color = C(0xFF, 0xE1, 0x4D);
                s.Ripple.MaxRadius = 100f;
                s.Ripple.DurationMs = 650;
                s.Ripple.Thickness = 2.6f;
                s.Ripple.Rings = 2;

                s.Particles.Enabled = true;
                s.Particles.Color = C(0xFF, 0x2B, 0xD6);
                s.Particles.Count = 16;
                s.Particles.Speed = 380f;
                s.Particles.Gravity = 650f;
                s.Particles.LifetimeMs = 800;
                s.Particles.Size = 3.2f;

                s.Selection.FillColor = C(0xFF, 0xE1, 0x4D);
                s.Selection.FillOpacity = 0.09f;
                s.Selection.HatchColor = C(0x00, 0xE5, 0xFF);
                s.Selection.HatchOpacity = 0.6f;
                s.Selection.HatchAngle = 45f;
                s.Selection.HatchSpacing = 8f;
                s.Selection.HatchWidth = 1.4f;
                s.Selection.BorderColor = C(0xFF, 0xE1, 0x4D);
                s.Selection.BorderOpacity = 0.95f;
                s.Selection.DimOpacity = 0.55f;
            },
        },
        new()
        {
            Id = "synthwave",
            Name = "Synthwave",
            Tagline = "Закат, магента и длинный шлейф",
            Tag = "поп",
            Accent = C(0xFF, 0x2E, 0x88),
            Secondary = C(0xFF, 0x9E, 0x00),
            Apply = s =>
            {
                s.Glow.Enabled = true;
                s.Glow.Color = C(0xFF, 0x2E, 0x88);
                s.Glow.Radius = 56f;
                s.Glow.Intensity = 0.5f;
                s.Glow.Pulse = 0.32f;
                s.Glow.PulseSpeed = 0.7f;

                s.Trail.Enabled = true;
                s.Trail.Color = C(0xFF, 0x9E, 0x00);
                s.Trail.LengthMs = 600;
                s.Trail.Thickness = 13f;
                s.Trail.Opacity = 0.5f;

                s.Ripple.Enabled = true;
                s.Ripple.Color = C(0xFF, 0x2E, 0x88);
                s.Ripple.MaxRadius = 120f;
                s.Ripple.DurationMs = 850;
                s.Ripple.Thickness = 2.8f;
                s.Ripple.Rings = 2;

                s.Particles.Enabled = true;
                s.Particles.Color = C(0x00, 0xE5, 0xFF);
                s.Particles.Count = 18;
                s.Particles.Speed = 300f;
                s.Particles.Gravity = 450f;
                s.Particles.LifetimeMs = 950;
                s.Particles.Size = 3f;

                s.Selection.FillColor = C(0xFF, 0x2E, 0x88);
                s.Selection.FillOpacity = 0.10f;
                s.Selection.HatchColor = C(0xFF, 0x9E, 0x00);
                s.Selection.HatchOpacity = 0.6f;
                s.Selection.HatchAngle = 45f;
                s.Selection.HatchSpacing = 10f;
                s.Selection.BorderColor = C(0xFF, 0x2E, 0x88);
                s.Selection.BorderOpacity = 0.95f;
                s.Selection.DimOpacity = 0.55f;
            },
        },
        new()
        {
            Id = "matrix",
            Name = "Matrix",
            Tagline = "Зелёный код и тонкий след",
            Tag = "гик",
            Accent = C(0x00, 0xFF, 0x66),
            Secondary = C(0x7C, 0xFF, 0xB2),
            Apply = s =>
            {
                s.Glow.Enabled = true;
                s.Glow.Color = C(0x00, 0xFF, 0x66);
                s.Glow.Radius = 34f;
                s.Glow.Intensity = 0.5f;
                s.Glow.Pulse = 0.12f;
                s.Glow.PulseSpeed = 0.8f;

                s.Trail.Enabled = true;
                s.Trail.Color = C(0x00, 0xFF, 0x66);
                s.Trail.LengthMs = 300;
                s.Trail.Thickness = 7f;
                s.Trail.Opacity = 0.6f;

                s.Ripple.Enabled = true;
                s.Ripple.Color = C(0x7C, 0xFF, 0xB2);
                s.Ripple.MaxRadius = 80f;
                s.Ripple.DurationMs = 560;
                s.Ripple.Thickness = 2f;
                s.Ripple.Rings = 1;

                s.Particles.Enabled = true;
                s.Particles.Color = C(0x00, 0xFF, 0x66);
                s.Particles.Count = 10;
                s.Particles.Speed = 260f;
                s.Particles.Gravity = 900f;
                s.Particles.LifetimeMs = 650;
                s.Particles.Size = 2.4f;

                s.Selection.FillColor = C(0x00, 0xFF, 0x66);
                s.Selection.FillOpacity = 0.08f;
                s.Selection.HatchColor = C(0x00, 0xFF, 0x66);
                s.Selection.HatchOpacity = 0.55f;
                s.Selection.HatchAngle = 45f;
                s.Selection.HatchSpacing = 6f;
                s.Selection.HatchWidth = 1f;
                s.Selection.BorderColor = C(0x00, 0xFF, 0x66);
                s.Selection.BorderOpacity = 0.95f;
                s.Selection.DimOpacity = 0.62f;
            },
        },
        new()
        {
            Id = "vaporwave",
            Name = "Vaporwave",
            Tagline = "Пастель, туман и большие кольца",
            Tag = "поп",
            Accent = C(0xFF, 0x6E, 0xC7),
            Secondary = C(0x72, 0xF1, 0xD5),
            Apply = s =>
            {
                s.Glow.Enabled = true;
                s.Glow.Color = C(0xFF, 0x6E, 0xC7);
                s.Glow.Radius = 64f;
                s.Glow.Intensity = 0.42f;
                s.Glow.Pulse = 0.35f;
                s.Glow.PulseSpeed = 0.45f;

                s.Trail.Enabled = true;
                s.Trail.Color = C(0x72, 0xF1, 0xD5);
                s.Trail.LengthMs = 700;
                s.Trail.Thickness = 14f;
                s.Trail.Opacity = 0.4f;

                s.Ripple.Enabled = true;
                s.Ripple.Color = C(0xFF, 0x6E, 0xC7);
                s.Ripple.MaxRadius = 130f;
                s.Ripple.DurationMs = 900;
                s.Ripple.Thickness = 2.2f;
                s.Ripple.Rings = 3;

                s.Particles.Enabled = true;
                s.Particles.Color = C(0xB3, 0x88, 0xFF);
                s.Particles.Count = 20;
                s.Particles.Speed = 250f;
                s.Particles.Gravity = 300f;
                s.Particles.LifetimeMs = 1100;
                s.Particles.Size = 2.6f;

                s.Selection.FillColor = C(0xFF, 0x6E, 0xC7);
                s.Selection.FillOpacity = 0.10f;
                s.Selection.HatchColor = C(0x72, 0xF1, 0xD5);
                s.Selection.HatchOpacity = 0.5f;
                s.Selection.HatchAngle = 60f;
                s.Selection.HatchSpacing = 11f;
                s.Selection.HatchWidth = 1.2f;
                s.Selection.BorderColor = C(0xFF, 0x6E, 0xC7);
                s.Selection.BorderOpacity = 0.95f;
                s.Selection.DimOpacity = 0.5f;
            },
        },
        new()
        {
            Id = "arcade",
            Name = "Arcade",
            Tagline = "Кислотные цвета и быстрый отклик",
            Tag = "поп",
            Accent = C(0xFF, 0xD2, 0x3F),
            Secondary = C(0x2D, 0x7D, 0xFF),
            Apply = s =>
            {
                s.Glow.Enabled = true;
                s.Glow.Color = C(0xFF, 0xD2, 0x3F);
                s.Glow.Radius = 38f;
                s.Glow.Intensity = 0.62f;
                s.Glow.Pulse = 0.35f;
                s.Glow.PulseSpeed = 1.6f;

                s.Trail.Enabled = true;
                s.Trail.Color = C(0xFF, 0x3B, 0x3B);
                s.Trail.LengthMs = 240;
                s.Trail.Thickness = 9f;
                s.Trail.Opacity = 0.7f;

                s.Ripple.Enabled = true;
                s.Ripple.Color = C(0x2D, 0x7D, 0xFF);
                s.Ripple.MaxRadius = 95f;
                s.Ripple.DurationMs = 480;
                s.Ripple.Thickness = 3f;
                s.Ripple.Rings = 2;

                s.Particles.Enabled = true;
                s.Particles.Color = C(0xFF, 0xD2, 0x3F);
                s.Particles.Count = 24;
                s.Particles.Speed = 450f;
                s.Particles.Gravity = 1100f;
                s.Particles.LifetimeMs = 600;
                s.Particles.Size = 3f;

                s.Selection.FillColor = C(0x2D, 0x7D, 0xFF);
                s.Selection.FillOpacity = 0.12f;
                s.Selection.HatchColor = C(0xFF, 0xD2, 0x3F);
                s.Selection.HatchOpacity = 0.65f;
                s.Selection.HatchAngle = 45f;
                s.Selection.HatchSpacing = 7f;
                s.Selection.HatchWidth = 1.6f;
                s.Selection.BorderColor = C(0x2D, 0x7D, 0xFF);
                s.Selection.BorderOpacity = 1f;
                s.Selection.DimOpacity = 0.5f;
            },
        },
        new()
        {
            Id = "hacker",
            Name = "Hacker",
            Tagline = "Янтарный терминал, минимум шума",
            Tag = "гик",
            Accent = C(0xFF, 0xB0, 0x00),
            Secondary = C(0xFF, 0xD9, 0x80),
            Apply = s =>
            {
                s.Glow.Enabled = true;
                s.Glow.Color = C(0xFF, 0xB0, 0x00);
                s.Glow.Radius = 30f;
                s.Glow.Intensity = 0.35f;
                s.Glow.Pulse = 0.1f;
                s.Glow.PulseSpeed = 0.6f;

                s.Trail.Enabled = true;
                s.Trail.Color = C(0xFF, 0xB0, 0x00);
                s.Trail.LengthMs = 220;
                s.Trail.Thickness = 5f;
                s.Trail.Opacity = 0.5f;

                s.Ripple.Enabled = true;
                s.Ripple.Color = C(0xFF, 0xB0, 0x00);
                s.Ripple.MaxRadius = 70f;
                s.Ripple.DurationMs = 420;
                s.Ripple.Thickness = 1.5f;
                s.Ripple.Rings = 1;

                s.Particles.Enabled = true;
                s.Particles.Color = C(0xFF, 0xB0, 0x00);
                s.Particles.Count = 6;
                s.Particles.Speed = 200f;
                s.Particles.Gravity = 700f;
                s.Particles.LifetimeMs = 500;
                s.Particles.Size = 2f;

                s.Selection.FillColor = C(0xFF, 0xB0, 0x00);
                s.Selection.FillOpacity = 0.07f;
                s.Selection.HatchColor = C(0xFF, 0xB0, 0x00);
                s.Selection.HatchOpacity = 0.5f;
                s.Selection.HatchAngle = 45f;
                s.Selection.HatchSpacing = 5f;
                s.Selection.HatchWidth = 0.8f;
                s.Selection.HatchSpeed = 8f;
                s.Selection.BorderColor = C(0xFF, 0xB0, 0x00);
                s.Selection.BorderOpacity = 0.95f;
                s.Selection.DimOpacity = 0.7f;
            },
        },
        new()
        {
            Id = "graffiti",
            Name = "Graffiti",
            Tagline = "Спрей, толстые линии и брызги",
            Tag = "граффити",
            Accent = C(0xFF, 0x4F, 0xD8),
            Secondary = C(0x5C, 0xFF, 0x6B),
            Apply = s =>
            {
                s.Glow.Enabled = true;
                s.Glow.Color = C(0xFF, 0x4F, 0xD8);
                s.Glow.Radius = 58f;
                s.Glow.Intensity = 0.5f;
                s.Glow.Pulse = 0.2f;
                s.Glow.PulseSpeed = 0.9f;

                s.Trail.Enabled = true;
                s.Trail.Color = C(0x5C, 0xFF, 0x6B);
                s.Trail.LengthMs = 520;
                s.Trail.Thickness = 16f;
                s.Trail.Opacity = 0.6f;

                s.Ripple.Enabled = true;
                s.Ripple.Color = C(0xFF, 0xE1, 0x4D);
                s.Ripple.MaxRadius = 140f;
                s.Ripple.DurationMs = 750;
                s.Ripple.Thickness = 3.4f;
                s.Ripple.Rings = 2;

                s.Particles.Enabled = true;
                s.Particles.Color = C(0x00, 0xE5, 0xFF);
                s.Particles.Count = 22;
                s.Particles.Speed = 420f;
                s.Particles.Gravity = 800f;
                s.Particles.LifetimeMs = 850;
                s.Particles.Size = 4f;

                s.Selection.FillColor = C(0xFF, 0x4F, 0xD8);
                s.Selection.FillOpacity = 0.14f;
                s.Selection.HatchColor = C(0xFF, 0xE1, 0x4D);
                s.Selection.HatchOpacity = 0.7f;
                s.Selection.HatchAngle = 30f;
                s.Selection.HatchSpacing = 12f;
                s.Selection.HatchWidth = 2f;
                s.Selection.BorderColor = C(0x5C, 0xFF, 0x6B);
                s.Selection.BorderOpacity = 1f;
                s.Selection.DimOpacity = 0.58f;
            },
        },
        new()
        {
            Id = "tattoo",
            Name = "Tattoo",
            Tagline = "Чёрные чернила и красный акцент",
            Tag = "тату",
            Accent = C(0xE8, 0xE6, 0xE3),
            Secondary = C(0xD7, 0x26, 0x3D),
            Apply = s =>
            {
                s.Glow.Enabled = true;
                s.Glow.Color = C(0xE8, 0xE6, 0xE3);
                s.Glow.Radius = 26f;
                s.Glow.Intensity = 0.28f;
                s.Glow.Pulse = 0.05f;
                s.Glow.PulseSpeed = 0.3f;

                s.Trail.Enabled = true;
                s.Trail.Color = C(0xE8, 0xE6, 0xE3);
                s.Trail.LengthMs = 240;
                s.Trail.Thickness = 6f;
                s.Trail.Opacity = 0.35f;

                s.Ripple.Enabled = true;
                s.Ripple.Color = C(0xD7, 0x26, 0x3D);
                s.Ripple.MaxRadius = 60f;
                s.Ripple.DurationMs = 500;
                s.Ripple.Thickness = 1.4f;
                s.Ripple.Rings = 1;

                s.Particles.Enabled = false;
                s.Particles.Color = C(0xD7, 0x26, 0x3D);
                s.Particles.Count = 0;
                s.Particles.Speed = 200f;
                s.Particles.Gravity = 600f;
                s.Particles.LifetimeMs = 500;
                s.Particles.Size = 2f;

                s.Selection.FillColor = C(0xD7, 0x26, 0x3D);
                s.Selection.FillOpacity = 0.06f;
                s.Selection.HatchColor = C(0xD7, 0x26, 0x3D);
                s.Selection.HatchOpacity = 0.7f;
                s.Selection.HatchAngle = 45f;
                s.Selection.HatchSpacing = 4f;
                s.Selection.HatchWidth = 0.9f;
                s.Selection.AnimateHatch = false;
                s.Selection.HatchSpeed = 0f;
                s.Selection.BorderColor = C(0xE8, 0xE6, 0xE3);
                s.Selection.BorderOpacity = 0.9f;
                s.Selection.DimOpacity = 0.68f;
            },
        },
    };

    public static Preset? Find(string? id) =>
        All.FirstOrDefault(p => string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase));

    public static void ApplyTo(AppSettings settings, string id)
    {
        var preset = Find(id) ?? All[0];
        preset.Apply(settings);
        settings.ActivePreset = preset.Id;
    }

    public static AppSettings Create(string id)
    {
        var settings = new AppSettings();
        ApplyTo(settings, id);
        return settings;
    }
}
