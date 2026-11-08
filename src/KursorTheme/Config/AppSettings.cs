using System.Drawing;

namespace KursorTheme.Config;

public sealed class AppSettings
{
    public int Version { get; set; } = 1;

    // General
    public bool EffectsEnabled { get; set; } = true;
    public int TargetFps { get; set; } = 120;
    public bool HideInFullscreen { get; set; } = true;
    public bool RunAtStartup { get; set; }
    public bool ShowNotifications { get; set; } = true;

    // Feature blocks
    public bool PresetsEnabled { get; set; } = true;
    public bool SelectionEnabled { get; set; } = false;
    public bool HotkeysEnabled { get; set; } = false;

    public string ActivePreset { get; set; } = "base";

    public GlowSettings Glow { get; set; } = new();
    public TrailSettings Trail { get; set; } = new();
    public RippleSettings Ripple { get; set; } = new();
    public ParticleSettings Particles { get; set; } = new();
    public SelectionSettings Selection { get; set; } = new();

    // Independent shape presets (not overwritten by colour presets).
    public TrailStyle TrailStyle { get; set; } = TrailStyle.Ribbon;
    public ParticleStyle ParticleStyle { get; set; } = ParticleStyle.Dots;
    public RippleStyle RippleStyle { get; set; } = RippleStyle.Ring;
}

public sealed class GlowSettings
{
    public bool Enabled { get; set; } = true;
    public Color Color { get; set; } = Color.FromArgb(0x36, 0xE5, 0xFF);
    public float Radius { get; set; } = 12f;
    public float Intensity { get; set; } = 0.20f;
    public float Pulse { get; set; } = 0.15f;
    public float PulseSpeed { get; set; } = 1f;
}

public sealed class TrailSettings
{
    public bool Enabled { get; set; } = true;
    public Color Color { get; set; } = Color.FromArgb(0x36, 0xE5, 0xFF);
    public int LengthMs { get; set; } = 240;
    public float Thickness { get; set; } = 3.5f;
    public float Opacity { get; set; } = 0.70f;
}

public sealed class RippleSettings
{
    public bool Enabled { get; set; } = true;
    public Color Color { get; set; } = Color.FromArgb(0x36, 0xE5, 0xFF);
    public float MaxRadius { get; set; } = 30f;
    public int DurationMs { get; set; } = 340;
    public float Thickness { get; set; } = 2.0f;
    public int Rings { get; set; } = 3;
}

public sealed class ParticleSettings
{
    public bool Enabled { get; set; } = true;
    public Color Color { get; set; } = Color.FromArgb(0x8B, 0x5C, 0xF6);
    public int Count { get; set; } = 15;
    public float Speed { get; set; } = 450f;
    public float Gravity { get; set; } = 1100f;
    public int LifetimeMs { get; set; } = 600;
    public float Size { get; set; } = 2.0f;
}

public sealed class SelectionSettings
{
    public Color FillColor { get; set; } = Color.FromArgb(0x36, 0xE5, 0xFF);
    public float FillOpacity { get; set; } = 0.10f;

    public Color HatchColor { get; set; } = Color.FromArgb(0x36, 0xE5, 0xFF);
    public float HatchOpacity { get; set; } = 0.55f;
    public float HatchAngle { get; set; } = 45f;
    public float HatchSpacing { get; set; } = 9f;
    public float HatchWidth { get; set; } = 1.4f;
    public bool AnimateHatch { get; set; } = true;
    public float HatchSpeed { get; set; } = 18f;

    public Color BorderColor { get; set; } = Color.FromArgb(0x36, 0xE5, 0xFF);
    public float BorderOpacity { get; set; } = 0.95f;
    public float BorderWidth { get; set; } = 1.6f;
    public bool CornerAccents { get; set; } = true;
    public bool ShowSize { get; set; } = true;

    public float DimOpacity { get; set; } = 0.55f;
    public bool PersistUntilEsc { get; set; }
    public int AutoHideMs { get; set; } = 2200;
}
