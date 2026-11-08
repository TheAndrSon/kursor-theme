using System.Drawing;
using System.Windows.Forms;
using KursorTheme.UI;

namespace KursorTheme.App;

internal sealed class DarkColorTable : ProfessionalColorTable
{
    public override Color ToolStripDropDownBackground => Theme.Surface;
    public override Color ToolStripBorder => Theme.Border;
    public override Color ImageMarginGradientBegin => Theme.Surface;
    public override Color ImageMarginGradientMiddle => Theme.Surface;
    public override Color ImageMarginGradientEnd => Theme.Surface;
    public override Color MenuBorder => Theme.Border;
    public override Color MenuItemBorder => Theme.Accent;
    public override Color MenuItemSelected => Theme.SurfaceActive;
    public override Color MenuItemSelectedGradientBegin => Theme.SurfaceActive;
    public override Color MenuItemSelectedGradientEnd => Theme.SurfaceHover;
    public override Color MenuItemPressedGradientBegin => Theme.Surface;
    public override Color MenuItemPressedGradientEnd => Theme.Surface;
    public override Color SeparatorDark => Theme.BorderSoft;
    public override Color SeparatorLight => Theme.BorderSoft;
    public override Color CheckBackground => Theme.SurfaceActive;
    public override Color CheckSelectedBackground => Theme.AccentDeep;
    public override Color CheckPressedBackground => Theme.AccentDeep;
}
