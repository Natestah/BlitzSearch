using Avalonia.Markup.Xaml.Converters;
using Avalonia.Media;

namespace Blitz.AvaloniaEdit.Models;

public class BlitzTheme
{
    public static BlitzTheme Dark = new BlitzTheme
    {
        TextForeground = Colors.WhiteSmoke,
        WindowBackground = new Color(255, 37, 37, 37),
        PassiveIcon = Colors.Aqua,
        ContentHighlightBackground = Colors.Black,
        ContentHighlightBorder = Colors.Cyan,
        ContentHighlightReplaceBackground = Colors.Black,
        ContentHighlightReplaceBorder = Colors.Gold,
        SelectedItemBackground =Colors.Black,
        AvaloniaThemeVariant = "Dark",
        ThemeName = "DarkPlus"
    };

    public static BlitzTheme Light = new BlitzTheme
    {
        TextForeground = Colors.Black,
        WindowBackground = Colors.White,
        PassiveIcon = Colors.Goldenrod,
        ContentHighlightBackground = Colors.Khaki,
        ContentHighlightBorder = Colors.DarkCyan,
        ContentHighlightReplaceBackground = Colors.LightBlue,
        ContentHighlightReplaceBorder = Colors.Lime,
        AvaloniaThemeVariant = "Light",
        ThemeName = "LightPlus"
    };

    public BlitzColor PassiveIcon { get; set; }
    
    public BlitzColor TextForeground { get; set; }
    public BlitzColor WindowBackground { get; set; }

    public BlitzColor ContentHighlightBackground { get; set; }
    public BlitzColor ContentHighlightBorder { get; set; }
    public BlitzColor ContentHighlightReplaceBackground { get; set; }
    public BlitzColor ContentHighlightReplaceBorder { get; set; }
    public BlitzColor SelectedItemBackground { get; set; }

    public string AvaloniaThemeVariant { get; set; }
    public string ThemeName { get; set; }
}