namespace Blitz.Interfacing;


/// <summary>
/// PlaceHolder for presentation ( Youtubes and TikToks about my CoD/MOHAA content
/// Ultimately Languages should seek to get added to AvaloniaEdit's built in support
/// but it might be nice to provide some way ( undetermined ) to use this.
/// </summary>
public static class ExtensionAsMap
{
    private static readonly Dictionary<string, string> AsMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { ".gsc", ".cpp" },
        { ".scr", ".cpp" },
    };
    public static string Get(string extension) => AsMap.GetValueOrDefault(extension, extension);
}