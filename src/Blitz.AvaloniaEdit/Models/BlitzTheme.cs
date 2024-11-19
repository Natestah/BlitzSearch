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
        SelectedItemBackground =Colors.LightGray,
        ThemeName = "LightPlus"
    };

    
    public BlitzColor PassiveIcon { get; set; } = Colors.Goldenrod;
    
    public BlitzColor TextForeground { get; set; }= Colors.Black;
    public BlitzColor WindowBackground { get; set; }= Colors.White;

    public BlitzColor ContentHighlightBackground { get; set; } = Colors.Khaki;
    public BlitzColor ContentHighlightBorder { get; set; } = Colors.DarkCyan;
    public BlitzColor ContentHighlightReplaceBackground { get; set; } = Colors.LightBlue;
    public BlitzColor ContentHighlightReplaceBorder { get; set; }= Colors.Lime;
    public BlitzColor SelectedItemBackground { get; set; } = Colors.LightGray;

    public string AvaloniaThemeVariant { get; set; } ="Light";
    public string ThemeName { get; set; } = "LightPlus";
}