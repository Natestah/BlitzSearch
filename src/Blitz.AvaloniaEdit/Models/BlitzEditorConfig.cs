namespace Blitz.AvaloniaEdit.Models;
using System.ComponentModel;

public class BlitzEditorConfig
{
    public string FontFamily { get; set; } = "Courier New";
    public double FontSize { get; set; } = 15.0;
    public double LineSpacing { get; set; } = 0.0;

    public bool ResultsFileNameScopeTrim { get; set; } = false;
    public bool ShowDonationButton { get; set; } = true;
    
    [DefaultValue(24.0)] 
    public double GeneralIconSize { get; set; } = 24.0;

}