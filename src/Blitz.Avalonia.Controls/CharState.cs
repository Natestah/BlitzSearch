using Avalonia.Media;

namespace Blitz.Avalonia.Controls;

public class CharState
{
    public IBrush Foreground { get; set; } = Brushes.Black; 
    public int Index { get; set; }
    public HighLightState BackGroundState { get; set; }
    
    public string? ReplacedFrom { get; set; }
    public enum HighLightState 
    {
        None,
        WordMatch,
        DimWordMatch,
        RegexGroup
    }
}