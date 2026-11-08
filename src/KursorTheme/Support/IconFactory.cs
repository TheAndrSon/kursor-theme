using System.Drawing;

namespace KursorTheme.Support;

internal static class IconFactory
{
    public static Icon LoadAppIcon()
    {
        try
        {
            var icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            if (icon != null) return icon;
        }
        catch
        {
            // Fall through to the system icon.
        }

        return (Icon)SystemIcons.Application.Clone();
    }

    public static void Destroy(Icon? icon)
    {
        try
        {
            icon?.Dispose();
        }
        catch
        {
            // Ignore.
        }
    }
}
