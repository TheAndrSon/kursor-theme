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
    public bool SelectionEnabled { get; set; } = true;
    public bool HotkeysEnabled { get; set; } = true;

    public string ActivePreset { get; set; } = "neon";

    public GlowSettings Glow { get; set; } = new();
    public TrailSettings Trail { get; set; } = new();
    public RippleSettings Ripple { get; set; } = new();
    public ParticleSettings Particles { get; set; } = new();
    public SelectionSettings Selection { get; set; } = new();
}

public sealed class GlowSettings
{
    public bool Enabled { get; set; } = true;
    public Color Color { get; set; } = Color.FromArgb(0x36, 0xE5, 0xFF);
    public float Radius { get; set; } = 46f;
    public float Intensity { get; set; } = 0.55f;
    public float Pulse { get; set; } = 0.25f;
    public float PulseSpeed { get; set; } = 0.9f;
}

public sealed class TrailSettings
{
    public bool Enabled { get; set; } = true;
    public Color Color { get; set; } = Color.FromArgb(0x36, 0xE5, 0xFF);
    public int LengthMs { get; set; } = 420;
    public float Thickness { get; set; } = 10f;
    public float Opacity { get; set; } = 0.5f;
}

public sealed class RippleSettings
{
    public bool Enabled { get; set; } = true;
    public Color Color { get; set; } = Color.FromArgb(0x36, 0xE5, 0xFF);
    public float MaxRadius { get; set; } = 90f;
    public int DurationMs { get; set; } = 620;
    public float Thickness { get; set; } = 2.4f;
    public int Rings { get; set; } = 2;
}

public sealed class ParticleSettings
{
    public bool Enabled { get; set; } = true;
    public Color Color { get; set; } = Color.FromArgb(0x8B, 0x5C, 0xF6);
    public int Count { get; set; } = 12;
    public float Speed { get; set; } = 340f;
    public float Gravity { get; set; } = 700f;
    public int LifetimeMs { get; set; } = 720;
    public float Size { get; set; } = 3.4f;
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
