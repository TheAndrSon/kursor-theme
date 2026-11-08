namespace KursorTheme.Config;

public enum TrailStyle
{
    Ribbon,
    Comet,
    Sparks,
    Pixels,
    Ink,
    Neon,
    Smoke,
    Lightning,
    Dotted,
}

public enum ParticleStyle
{
    Dots,
    Sparks,
    Pixels,
    Code,
    Paint,
    Glitch,
    Stars,
    Bubbles,
}

public enum RippleStyle
{
    Ring,
    DoubleRing,
    Shockwave,
    PixelSquare,
    Ink,
    Glitch,
    Pulse,
    Spiral,
}

// Style presets intentionally carry no numeric settings: switching the shape
// must not override the user's radius, thickness, count, lifetime, etc.
public sealed record TrailStylePreset(TrailStyle Style, string Name);

public sealed record ParticleStylePreset(ParticleStyle Style, string Name);

public sealed record RippleStylePreset(RippleStyle Style, string Name);

public static class TrailStylePresets
{
    public static readonly IReadOnlyList<TrailStylePreset> All = new[]
    {
        new TrailStylePreset(TrailStyle.Ribbon, "Лента"),
        new TrailStylePreset(TrailStyle.Comet, "Комета"),
        new TrailStylePreset(TrailStyle.Sparks, "Искры"),
        new TrailStylePreset(TrailStyle.Pixels, "Пиксели"),
        new TrailStylePreset(TrailStyle.Ink, "Чернила"),
        new TrailStylePreset(TrailStyle.Neon, "Неон"),
        new TrailStylePreset(TrailStyle.Smoke, "Дым"),
        new TrailStylePreset(TrailStyle.Lightning, "Молния"),
        new TrailStylePreset(TrailStyle.Dotted, "Пунктир"),
    };
}

public static class ParticleStylePresets
{
    public static readonly IReadOnlyList<ParticleStylePreset> All = new[]
    {
        new ParticleStylePreset(ParticleStyle.Dots, "Точки"),
        new ParticleStylePreset(ParticleStyle.Sparks, "Искры"),
        new ParticleStylePreset(ParticleStyle.Pixels, "Пиксели"),
        new ParticleStylePreset(ParticleStyle.Code, "Код"),
        new ParticleStylePreset(ParticleStyle.Paint, "Брызги"),
        new ParticleStylePreset(ParticleStyle.Glitch, "Глитч"),
        new ParticleStylePreset(ParticleStyle.Stars, "Звёзды"),
        new ParticleStylePreset(ParticleStyle.Bubbles, "Пузыри"),
    };
}

public static class RippleStylePresets
{
    public static readonly IReadOnlyList<RippleStylePreset> All = new[]
    {
        new RippleStylePreset(RippleStyle.Ring, "Кольцо"),
        new RippleStylePreset(RippleStyle.DoubleRing, "Двойное"),
        new RippleStylePreset(RippleStyle.Shockwave, "Удар"),
        new RippleStylePreset(RippleStyle.PixelSquare, "Квадрат"),
        new RippleStylePreset(RippleStyle.Ink, "Чернила"),
        new RippleStylePreset(RippleStyle.Glitch, "Глитч"),
        new RippleStylePreset(RippleStyle.Pulse, "Пульс"),
        new RippleStylePreset(RippleStyle.Spiral, "Спираль"),
    };
}
