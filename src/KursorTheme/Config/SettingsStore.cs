using System.Drawing;
using System.Text.Json;

namespace KursorTheme.Config;

public static class SettingsStore
{
    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new ColorJsonConverter() },
    };

    public static string Folder => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "KursorTheme");

    public static string FilePath => Path.Combine(Folder, "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(FilePath)) return new AppSettings();
            var json = File.ReadAllText(FilePath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, Json);
            return Normalize(settings ?? new AppSettings());
        }
        catch (Exception ex)
        {
            Support.Log.Error(ex);
            return new AppSettings();
        }
    }

    public static void Save(AppSettings settings)
    {
        try
        {
            Directory.CreateDirectory(Folder);
            var temp = FilePath + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(settings, Json));
            File.Move(temp, FilePath, overwrite: true);
        }
        catch (Exception ex)
        {
            Support.Log.Error(ex);
        }
    }

    public static AppSettings Clone(AppSettings settings) =>
        JsonSerializer.Deserialize<AppSettings>(JsonSerializer.Serialize(settings, Json), Json)
        ?? new AppSettings();

    public static AppSettings Normalize(AppSettings s)
    {
        s.Glow ??= new GlowSettings();
        s.Trail ??= new TrailSettings();
        s.Ripple ??= new RippleSettings();
        s.Particles ??= new ParticleSettings();
        s.Selection ??= new SelectionSettings();

        s.TargetFps = Math.Clamp(s.TargetFps, 30, 240);

        s.Glow.Radius = Math.Clamp(s.Glow.Radius, 4f, 240f);
        s.Glow.Intensity = Math.Clamp(s.Glow.Intensity, 0f, 1f);
        s.Glow.Pulse = Math.Clamp(s.Glow.Pulse, 0f, 1f);
        s.Glow.PulseSpeed = Math.Clamp(s.Glow.PulseSpeed, 0f, 4f);

        s.Trail.LengthMs = Math.Clamp(s.Trail.LengthMs, 40, 4000);
        s.Trail.Thickness = Math.Clamp(s.Trail.Thickness, 0.5f, 60f);
        s.Trail.Opacity = Math.Clamp(s.Trail.Opacity, 0f, 1f);

        s.Ripple.MaxRadius = Math.Clamp(s.Ripple.MaxRadius, 8f, 600f);
        s.Ripple.DurationMs = Math.Clamp(s.Ripple.DurationMs, 80, 4000);
        s.Ripple.Thickness = Math.Clamp(s.Ripple.Thickness, 0.3f, 12f);
        s.Ripple.Rings = Math.Clamp(s.Ripple.Rings, 1, 3);

        s.Particles.Count = Math.Clamp(s.Particles.Count, 0, 80);
        s.Particles.Speed = Math.Clamp(s.Particles.Speed, 0f, 2000f);
        s.Particles.Gravity = Math.Clamp(s.Particles.Gravity, -2000f, 4000f);
        s.Particles.LifetimeMs = Math.Clamp(s.Particles.LifetimeMs, 80, 4000);
        s.Particles.Size = Math.Clamp(s.Particles.Size, 0.5f, 24f);

        if (!Enum.IsDefined(s.TrailStyle)) s.TrailStyle = TrailStyle.Ribbon;
        if (!Enum.IsDefined(s.ParticleStyle)) s.ParticleStyle = ParticleStyle.Dots;
        if (!Enum.IsDefined(s.RippleStyle)) s.RippleStyle = RippleStyle.Ring;

        var sel = s.Selection;
        sel.FillOpacity = Math.Clamp(sel.FillOpacity, 0f, 1f);
        sel.HatchOpacity = Math.Clamp(sel.HatchOpacity, 0f, 1f);
        sel.HatchAngle = Math.Clamp(sel.HatchAngle, 0f, 90f);
        sel.HatchSpacing = Math.Clamp(sel.HatchSpacing, 2f, 40f);
        sel.HatchWidth = Math.Clamp(sel.HatchWidth, 0.3f, 8f);
        sel.HatchSpeed = Math.Clamp(sel.HatchSpeed, 0f, 120f);
        sel.BorderOpacity = Math.Clamp(sel.BorderOpacity, 0f, 1f);
        sel.BorderWidth = Math.Clamp(sel.BorderWidth, 0.4f, 8f);
        sel.DimOpacity = Math.Clamp(sel.DimOpacity, 0f, 0.95f);
        sel.AutoHideMs = Math.Clamp(sel.AutoHideMs, 300, 20000);

        if (string.IsNullOrWhiteSpace(s.ActivePreset)) s.ActivePreset = "base";
        return s;
    }
}
