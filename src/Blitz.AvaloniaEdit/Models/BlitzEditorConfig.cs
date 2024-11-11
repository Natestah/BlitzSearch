namespace Blitz.AvaloniaEdit.Models;
using System.ComponentModel;

public class BlitzEditorConfig
{
    public string FontFamily { get; set; } = "Courier New";
    public double FontSize { get; set; } = 15.0;
    public double LineSpacing { get; set; } = 0.0;
    
    [DefaultValue(24.0)] 
    public double GeneralIconSize { get; set; } = 24.0;

}