using System.Drawing;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KursorTheme.Config;

internal sealed class ColorJsonConverter : JsonConverter<Color>
{
    public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var text = reader.GetString();
        if (string.IsNullOrWhiteSpace(text)) return Color.White;

        text = text.Trim().TrimStart('#');
        try
        {
            return text.Length switch
            {
                8 => Color.FromArgb(
                    int.Parse(text.Substring(0, 2), NumberStyles.HexNumber),
                    int.Parse(text.Substring(2, 2), NumberStyles.HexNumber),
                    int.Parse(text.Substring(4, 2), NumberStyles.HexNumber),
                    int.Parse(text.Substring(6, 2), NumberStyles.HexNumber)),
                6 => Color.FromArgb(
                    int.Parse(text.Substring(0, 2), NumberStyles.HexNumber),
                    int.Parse(text.Substring(2, 2), NumberStyles.HexNumber),
                    int.Parse(text.Substring(4, 2), NumberStyles.HexNumber)),
                _ => Color.White,
            };
        }
        catch
        {
            return Color.White;
        }
    }

    public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
    {
        writer.WriteStringValue($"#{value.A:X2}{value.R:X2}{value.G:X2}{value.B:X2}");
    }
}
