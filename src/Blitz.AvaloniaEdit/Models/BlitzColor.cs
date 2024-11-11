using System.Text.Json.Serialization;
using Avalonia.Media;
namespace Blitz.AvaloniaEdit.Models;

public class BlitzColor
{

    public BlitzColor(Color color)
    {
        A = color.A;
        R = color.R;
        G = color.G;
        B = color.B;
    }

    public BlitzColor(byte a, byte r, byte g, byte b)
    {
        A = a;
        R = r;
        G = g;
        B = b;
    }

    public BlitzColor()
    {
    }

    public byte A { get; set; }

    /// <summary>
    /// Gets the Red component of the color.
    /// </summary>
    public byte R { get; set; }

    /// <summary>
    /// Gets the Green component of the color.
    /// </summary>
    public byte G { get; set; }

    /// <summary>
    /// Gets the Blue component of the color.
    /// </summary>
    public byte B { get; set; }

    [JsonIgnore] public Avalonia.Media.Color AvaloniaColor => new Color(A, R, G, B);

    public static implicit operator Avalonia.Media.Color(BlitzColor blitzColor) => blitzColor.AvaloniaColor;
    public static implicit operator BlitzColor(Avalonia.Media.Color mediaColor) => new BlitzColor(mediaColor);
}
